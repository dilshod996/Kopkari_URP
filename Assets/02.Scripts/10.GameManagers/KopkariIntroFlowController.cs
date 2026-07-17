using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class KopkariIntroFlowController : MonoBehaviour
{
    public enum IntroState
    {
        Idle,
        GateShot,
        RandomShot,
        UlakShot,
        RivalShot,
        InspectingRiders,
        PlayerShot,
        WaitingForRiders,
        Completing,
        Complete
    }

    [Header("Riders")]
    [SerializeField] private List<AIKopkariRider> riders = new List<AIKopkariRider>();
    [Tooltip("When enabled, one active AI rider is selected as the rival every intro.")]
    [SerializeField] private bool selectRandomMainRival = true;
    [Tooltip("Used as a fixed rival only when random selection is disabled, or as a fallback.")]
    [SerializeField] private AIKopkariRider mainRival;
    [SerializeField] private bool autoFindActiveRiders = true;
    [SerializeField, Min(0f)] private float riderDepartureStagger = 0.18f;

    [Header("Ulak Roles")]
    [Tooltip("These riders circle the Ulak and do not attempt to pick it up. The main rival is always excluded.")]
    [SerializeField, Range(0, 3)] private int orbitRiderCount = 3;
    [Tooltip("Optional preferred orbit riders. Empty slots are filled randomly from the remaining riders.")]
    [SerializeField] private List<AIKopkariRider> preferredOrbitRiders = new List<AIKopkariRider>();
    [Tooltip("Random non-rival, non-orbit riders that guard the Ulak without picking it up.")]
    [SerializeField, Range(0, 4)] private int guardRiderCount = 2;
    [Tooltip("Optional preferred Guard riders. Empty slots are filled randomly from the remaining non-rival, non-orbit riders.")]
    [SerializeField] private List<AIKopkariRider> preferredGuardRiders = new List<AIKopkariRider>();
    [Tooltip("A non-rival rider that predicts the carrier route and places pooled traps at intervals.")]
    [SerializeField, Range(0, 1)] private int trapSetterRiderCount = 1;
    [Tooltip("Optional preferred Trap Setter. Guard and main-rival assignments still remain exclusive.")]
    [SerializeField] private List<AIKopkariRider> preferredTrapSetters = new List<AIKopkariRider>();
    [Tooltip("Random gameplay-ready ground traps. If empty, the selected rider's BoostersContainer Walk Zone is used.")]
    [SerializeField] private List<GameObject> randomTrapPrefabs = new List<GameObject>();

    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera introCamera;
    [SerializeField] private CinemachineVirtualCameraBase gameplayCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineImpulseSource gateImpulseSource;
    [SerializeField] private int introActivePriority = 100;
    [SerializeField] private int introInactivePriority = -1;
    [SerializeField, Min(0f)] private float gameplayBlendDuration = 0.65f;

    [Header("Gameplay Camera Start View")]
    [SerializeField] private ThirdPersonFollowTarget gameplayFollowTarget;
    [SerializeField] private float gameplayStartYaw;
    [SerializeField] private float gameplayStartPitch;

    [Header("Camera Anchors")]
    [SerializeField] private Transform aiGateCameraPosition;
    [SerializeField] private List<Transform> randomCameraPositions = new List<Transform>();
    [SerializeField] private Transform ulakCameraPosition;
    [Tooltip("Fallback only. The selected rival's own Inspection Camera Point is preferred.")]
    [SerializeField] private Transform mainRivalCameraPosition;
    [FormerlySerializedAs("waitingWideCameraPosition")]
    [Tooltip("Final player-facing or wide holding shot before gameplay starts.")]
    [SerializeField] private Transform playerCameraPosition;

    [Header("Blink")]
    [SerializeField] private CanvasGroup blinkCanvasGroup;
    [SerializeField, Min(0f)] private float blinkCloseDuration = 0.18f;
    [SerializeField, Min(0f)] private float blinkOpenDuration = 0.32f;
    [SerializeField, Min(0f)] private float blinkClosedHoldDuration = 0.05f;

    [Header("Timing")]
    [SerializeField] private Vector2 spectatorShotDurationRange = new Vector2(4f, 5f);
    [SerializeField] private Vector2 gateShotDurationRange = new Vector2(4f, 5f);
    [SerializeField] private Vector2 standardShotDurationRange = new Vector2(2f, 3f);
    [SerializeField, Min(0f)] private float mainRivalShotDuration = 5f;
    [SerializeField, Min(0f)] private float localPlayerShotDuration = 4f;
    [SerializeField, Min(0f)] private float riderInspectionDuration = 3f;
    [Tooltip("Delay after the gameplay camera opens on the player and before gameplay starts.")]
    [SerializeField, Min(0f)] private float gameplayStartCountdown = 3f;
    [SerializeField, Range(0, 3)] private int minimumRiderInspectionShots = 2;
    [SerializeField, Range(0, 3)] private int maximumRiderInspectionShots = 3;
    [SerializeField, Min(0f)] private float readyPlayerListDuration = 3f;
    [SerializeField, Min(1f)] private float riderReadyTimeout = 20f;
    [SerializeField] private bool teleportUnreadyRidersOnTimeout = true;

    [Header("UI")]
    [SerializeField] private KopkariMainUI mainUI;
    [SerializeField] private KopkariIntroPlayersList playersList;
    [SerializeField] private KopkariObjectCharacteristics objectCharacteristics;

    [Header("Gameplay Countdown UI")]
    [SerializeField] private GameObject countBackground;
    [SerializeField] private TMP_Text countText;

    [Header("Gate Shake")]
    [Tooltip("Multiplier applied to the gate impulse. Lower values produce a softer camera shake.")]
    [SerializeField, Range(0f, 1f)] private float gateImpulseStrength = 0.35f;
    [SerializeField, Min(0f)] private float minimumImpulseInterval = 0.12f;
    [Tooltip("Repeating impulse interval while the gate camera is active.")]
    [SerializeField, Min(0.05f)] private float activeGateShakeInterval = 0.45f;

    private readonly List<AIKopkariRider> pendingRiderBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> orbitSelectionBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> orbitCandidateBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> guardSelectionBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> guardCandidateBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> trapSetterSelectionBuffer = new List<AIKopkariRider>();
    private readonly List<AIKopkariRider> trapSetterCandidateBuffer = new List<AIKopkariRider>();
    private readonly Dictionary<GameObject, int> aiColliderOriginalLayers = new Dictionary<GameObject, int>();
    private Coroutine introRoutine;
    private Coroutine gateShakeRoutine;
    private Action completionCallback;
    private CinemachineBlendDefinition originalBlend;
    private int originalGameplayPriority;
    private bool hasOriginalBlend;
    private bool cameraStateCached;
    private bool isPlaying;
    private bool skipRequested;
    private bool completionInvoked;
    private bool introPlayerListVisible;
    private bool allowSkipDisplay;
    private float lastImpulseTime = float.NegativeInfinity;
    private int queuedGatePasses;
    private int aiHorseCollisionLayer = -1;
    private bool aiCollisionIsolationActive;
    private bool originalAIHorseSelfCollisionIgnored;
    private MAnimal localPlayerPresentationAnimal;
    private IntroState state = IntroState.Idle;

    public bool IsPlaying => isPlaying;
    public bool AllRidersReady => AreAllRidersReady();
    public IntroState State => state;
    public IReadOnlyList<AIKopkariRider> Riders => riders;
    public AIKopkariRider MainRival => mainRival;
    public string MainRivalName => mainRival != null ? mainRival.RiderName : string.Empty;

    private void Awake()
    {
        if (mainUI == null)
            mainUI = KopkariMainUI.Instance;
        if (cinemachineBrain == null && Camera.main != null)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        CacheCameraState();
        PrepareCameraState();
        SetIntroUiVisible(false);
        HideObjectCharacteristics();
        SetCountdownVisible(false);
    }

    private void OnEnable()
    {
        AIKopkariRider.OnRiderReady += HandleRiderReady;
        AIKopkariRider.OnRiderPassedGate += HandleRiderPassedGate;

        if (mainUI != null && mainUI.IntroSkipButton != null)
            mainUI.IntroSkipButton.onClick.AddListener(RequestSkip);
    }

    private void OnDisable()
    {
        AIKopkariRider.OnRiderReady -= HandleRiderReady;
        AIKopkariRider.OnRiderPassedGate -= HandleRiderPassedGate;

        if (mainUI != null && mainUI.IntroSkipButton != null)
            mainUI.IntroSkipButton.onClick.RemoveListener(RequestSkip);

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        StopGateShake();
        StopLocalPlayerPresentation();
        RestoreAIRiderCollisions();
        HideObjectCharacteristics();
        SetCountdownVisible(false);
        isPlaying = false;
        RestoreCameraState();
    }

    public void PlayIntro(Action onComplete)
    {
        if (isPlaying)
            return;

        completionCallback = onComplete;
        completionInvoked = false;
        skipRequested = false;
        queuedGatePasses = 0;
        lastImpulseTime = float.NegativeInfinity;
        introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    public void RequestSkip()
    {
        if (!isPlaying || !AreAllRidersReady())
            return;

        skipRequested = true;
        StopGateShake();
        StopLocalPlayerPresentation();
        HideObjectCharacteristics();
        if (mainUI != null)
            mainUI.SetIntroSkipVisible(false);
    }

    private IEnumerator PlayIntroRoutine()
    {
        isPlaying = true;
        ResolveRiders();
        IsolateAIRiderCollisions();
        SelectMainRival();
        AssignUlakRoles();
        CacheCameraState();
        PrepareCameraState();

        playersList?.BuildList(riders);
        SetIntroUiVisible(false);
        HideObjectCharacteristics();
        SetCountdownVisible(false);

        Transform openingSpectatorCamera = SelectRandomCameraPosition();
        bool hasOpeningSpectatorShot = openingSpectatorCamera != null;
        state = hasOpeningSpectatorShot ? IntroState.RandomShot : IntroState.GateShot;
        if (!hasOpeningSpectatorShot)
            ShowGateCharacteristics();
        MoveIntroCamera(hasOpeningSpectatorShot ? openingSpectatorCamera : aiGateCameraPosition);
        if (!hasOpeningSpectatorShot)
            StartGateShake();
        SetIntroCameraActive();
        BeginRiderPreparation();

        yield return BlinkOpen();
        yield return WaitRealtime(RandomDuration(
            hasOpeningSpectatorShot ? spectatorShotDurationRange : gateShotDurationRange), false);

        if (hasOpeningSpectatorShot && !skipRequested)
        {
            state = IntroState.GateShot;
            yield return BlinkClosed();
            ShowGateCharacteristics();
            MoveIntroCamera(aiGateCameraPosition);
            StartGateShake();
            yield return null;
            yield return BlinkOpen();
            FlushQueuedGateImpulse();
            yield return WaitRealtime(RandomDuration(gateShotDurationRange), false);
        }

        if (!skipRequested)
        {
            StopGateShake();
            state = IntroState.UlakShot;
            yield return TransitionToUlakShot(RandomDuration(standardShotDurationRange));
        }

        if (!skipRequested)
        {
            state = IntroState.RivalShot;
            yield return TransitionToRivalShot(mainRivalShotDuration);
        }

        if (!skipRequested)
        {
            state = IntroState.PlayerShot;
            yield return TransitionToPlayerCharacteristics();
        }

        if (!skipRequested)
        {
            state = IntroState.RandomShot;
            yield return TransitionToPlayerListShot(SelectRandomCameraPosition());
        }

        if (!skipRequested)
        {
            state = IntroState.InspectingRiders;
            yield return InspectRandomRiders();
        }

        if (!AreAllRidersReady())
        {
            state = IntroState.WaitingForRiders;
            yield return WaitForRidersOrTimeout();
        }

        RefreshIntroReadinessUi();

        state = IntroState.PlayerShot;
        yield return MoveToPlayerCamera();

        state = IntroState.Completing;
        CompleteIntro();
    }

    private void ResolveRiders()
    {
        if (autoFindActiveRiders)
        {
            AIKopkariRider[] found = FindObjectsOfType<AIKopkariRider>(true);
            riders.Clear();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].isActiveAndEnabled)
                    riders.Add(found[i]);
            }
        }
        else
        {
            riders.RemoveAll(rider => rider == null || !rider.isActiveAndEnabled);
        }

        riders.Sort((a, b) => a.Id.CompareTo(b.Id));
    }

    private void BeginRiderPreparation()
    {
        for (int i = 0; i < riders.Count; i++)
        {
            AIKopkariRider rider = riders[i];
            bool isRival = rider == mainRival;
            rider.BeginPregame(isRival, i * riderDepartureStagger);
        }
    }

    private void SelectMainRival()
    {
        if (riders.Count == 0)
        {
            mainRival = null;
            return;
        }

        if (!selectRandomMainRival && mainRival != null && riders.Contains(mainRival))
            return;

        // Keep explicitly preferred role riders and mounted-melee Guard candidates
        // available whenever another valid rival candidate exists.
        if (guardRiderCount > 0 || trapSetterRiderCount > 0)
        {
            int startIndex = UnityEngine.Random.Range(0, riders.Count);
            for (int offset = 0; offset < riders.Count; offset++)
            {
                AIKopkariRider candidate = riders[(startIndex + offset) % riders.Count];
                bool preserveForGuard = guardRiderCount > 0 && candidate != null &&
                                        candidate.HasGuardRiderMeleeAttack;
                bool preserveForTrapSetter = trapSetterRiderCount > 0 && candidate != null &&
                                             preferredTrapSetters.Contains(candidate);
                if (candidate != null && !preserveForGuard && !preserveForTrapSetter)
                {
                    mainRival = candidate;
                    return;
                }
            }
        }

        mainRival = riders[UnityEngine.Random.Range(0, riders.Count)];
    }

    private void AssignUlakRoles()
    {
        orbitSelectionBuffer.Clear();
        orbitCandidateBuffer.Clear();
        guardSelectionBuffer.Clear();
        guardCandidateBuffer.Clear();
        trapSetterSelectionBuffer.Clear();
        trapSetterCandidateBuffer.Clear();

        for (int i = 0; i < riders.Count; i++)
        {
            AIKopkariRider rider = riders[i];
            if (rider == null)
                continue;

            rider.ConfigureUlakRole(AIKopkariRider.UlakRole.Competitor);
            if (rider != mainRival)
                orbitCandidateBuffer.Add(rider);
        }

        int requestedGuardReservation = Mathf.Min(guardRiderCount, orbitCandidateBuffer.Count);

        // Explicit Guard choices have first priority over the Orbit list.
        for (int i = 0; i < preferredGuardRiders.Count &&
                        guardSelectionBuffer.Count < requestedGuardReservation; i++)
        {
            AIKopkariRider preferred = preferredGuardRiders[i];
            if (preferred == null || preferred == mainRival ||
                !orbitCandidateBuffer.Remove(preferred) || guardSelectionBuffer.Contains(preferred))
                continue;

            guardSelectionBuffer.Add(preferred);
        }

        // When no explicit entries fill the slots, reserve riders that already
        // have a melee object. Otherwise an old Preferred Orbit list can consume
        // every weapon-equipped rider before Guards are selected.
        for (int i = orbitCandidateBuffer.Count - 1;
             i >= 0 && guardSelectionBuffer.Count < requestedGuardReservation;
             i--)
        {
            AIKopkariRider candidate = orbitCandidateBuffer[i];
            if (candidate == null || !candidate.HasGuardRiderMeleeAttack)
                continue;

            guardSelectionBuffer.Add(candidate);
            orbitCandidateBuffer.RemoveAt(i);
        }

        int requestedTrapSetterCount = Mathf.Min(trapSetterRiderCount, orbitCandidateBuffer.Count);

        // A preferred Trap Setter is reserved before Orbit riders are chosen.
        // Explicit/pre-equipped Guards above keep priority if a rider was put in both lists.
        for (int i = 0; i < preferredTrapSetters.Count &&
                        trapSetterSelectionBuffer.Count < requestedTrapSetterCount; i++)
        {
            AIKopkariRider preferred = preferredTrapSetters[i];
            if (preferred == null || preferred == mainRival ||
                !orbitCandidateBuffer.Remove(preferred) || trapSetterSelectionBuffer.Contains(preferred))
                continue;

            trapSetterSelectionBuffer.Add(preferred);
        }

        trapSetterCandidateBuffer.AddRange(orbitCandidateBuffer);
        for (int i = trapSetterCandidateBuffer.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            AIKopkariRider temp = trapSetterCandidateBuffer[i];
            trapSetterCandidateBuffer[i] = trapSetterCandidateBuffer[swapIndex];
            trapSetterCandidateBuffer[swapIndex] = temp;
        }

        for (int i = 0; i < trapSetterCandidateBuffer.Count &&
                        trapSetterSelectionBuffer.Count < requestedTrapSetterCount; i++)
        {
            AIKopkariRider candidate = trapSetterCandidateBuffer[i];
            if (candidate == null || candidate == mainRival || preferredOrbitRiders.Contains(candidate) ||
                trapSetterSelectionBuffer.Contains(candidate))
                continue;

            trapSetterSelectionBuffer.Add(candidate);
            orbitCandidateBuffer.Remove(candidate);
        }

        // Only consume a preferred Orbit rider when no other exclusive rider is available.
        for (int i = 0; i < trapSetterCandidateBuffer.Count &&
                        trapSetterSelectionBuffer.Count < requestedTrapSetterCount; i++)
        {
            AIKopkariRider candidate = trapSetterCandidateBuffer[i];
            if (candidate == null || candidate == mainRival || trapSetterSelectionBuffer.Contains(candidate))
                continue;

            trapSetterSelectionBuffer.Add(candidate);
            orbitCandidateBuffer.Remove(candidate);
        }

        int assignedTrapSetterCount = trapSetterSelectionBuffer.Count;
        for (int i = 0; i < assignedTrapSetterCount; i++)
        {
            AIKopkariRider trapSetter = trapSetterSelectionBuffer[i];
            trapSetter.ConfigureTrapSetterPrefabs(randomTrapPrefabs);
            trapSetter.ConfigureUlakRole(AIKopkariRider.UlakRole.TrapSetter, i, assignedTrapSetterCount);
        }

        for (int i = 0; i < randomTrapPrefabs.Count; i++)
        {
            GameObject prefab = randomTrapPrefabs[i];
            if (prefab != null)
                SimplePool.CreatePool(prefab, prewarm: 2, maxSize: 8, expandable: true);
        }

        if (assignedTrapSetterCount < trapSetterRiderCount)
        {
            Debug.LogWarning(
                $"[{nameof(KopkariIntroFlowController)}] Requested {trapSetterRiderCount} Trap Setter, " +
                $"but only {assignedTrapSetterCount} exclusive non-rival rider is available.",
                this);
        }

        int requestedCount = Mathf.Min(orbitRiderCount, orbitCandidateBuffer.Count);

        for (int i = 0; i < preferredOrbitRiders.Count && orbitSelectionBuffer.Count < requestedCount; i++)
        {
            AIKopkariRider preferred = preferredOrbitRiders[i];
            if (preferred == null || preferred == mainRival || !orbitCandidateBuffer.Contains(preferred) ||
                orbitSelectionBuffer.Contains(preferred))
                continue;

            orbitSelectionBuffer.Add(preferred);
            orbitCandidateBuffer.Remove(preferred);
        }

        for (int i = orbitCandidateBuffer.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            AIKopkariRider temp = orbitCandidateBuffer[i];
            orbitCandidateBuffer[i] = orbitCandidateBuffer[swapIndex];
            orbitCandidateBuffer[swapIndex] = temp;
        }

        for (int i = 0; i < orbitCandidateBuffer.Count && orbitSelectionBuffer.Count < requestedCount; i++)
            orbitSelectionBuffer.Add(orbitCandidateBuffer[i]);

        int assignedCount = orbitSelectionBuffer.Count;
        for (int i = 0; i < assignedCount; i++)
            orbitSelectionBuffer[i].ConfigureUlakRole(AIKopkariRider.UlakRole.Orbit, i, assignedCount);

        if (assignedCount < orbitRiderCount)
        {
            Debug.LogWarning(
                $"[{nameof(KopkariIntroFlowController)}] Requested {orbitRiderCount} Ulak orbit riders, " +
                $"but only {assignedCount} non-rival riders are available.",
                this);
        }

        for (int i = 0; i < riders.Count; i++)
        {
            AIKopkariRider rider = riders[i];
            if (rider != null && rider != mainRival && !orbitSelectionBuffer.Contains(rider) &&
                !trapSetterSelectionBuffer.Contains(rider))
                guardCandidateBuffer.Add(rider);
        }

        int requestedGuardCount = Mathf.Min(guardRiderCount, guardCandidateBuffer.Count);

        for (int i = 0; i < preferredGuardRiders.Count && guardSelectionBuffer.Count < requestedGuardCount; i++)
        {
            AIKopkariRider preferred = preferredGuardRiders[i];
            if (preferred == null || preferred == mainRival || !guardCandidateBuffer.Contains(preferred) ||
                guardSelectionBuffer.Contains(preferred))
                continue;

            guardSelectionBuffer.Add(preferred);
            guardCandidateBuffer.Remove(preferred);
        }

        for (int i = guardCandidateBuffer.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            AIKopkariRider temp = guardCandidateBuffer[i];
            guardCandidateBuffer[i] = guardCandidateBuffer[swapIndex];
            guardCandidateBuffer[swapIndex] = temp;
        }

        // Prefer riders that have a configured mounted-melee object. This keeps
        // random Guard selection functional even when the optional preferred
        // list has not been filled in the scene yet.
        for (int i = 0; i < guardCandidateBuffer.Count && guardSelectionBuffer.Count < requestedGuardCount; i++)
        {
            AIKopkariRider candidate = guardCandidateBuffer[i];
            if (candidate != null && candidate.HasGuardRiderMeleeAttack &&
                !guardSelectionBuffer.Contains(candidate))
                guardSelectionBuffer.Add(candidate);
        }

        for (int i = 0; i < guardCandidateBuffer.Count && guardSelectionBuffer.Count < requestedGuardCount; i++)
        {
            AIKopkariRider candidate = guardCandidateBuffer[i];
            if (candidate != null && !guardSelectionBuffer.Contains(candidate))
                guardSelectionBuffer.Add(candidate);
        }

        int assignedGuardCount = guardSelectionBuffer.Count;
        for (int i = 0; i < assignedGuardCount; i++)
            guardSelectionBuffer[i].ConfigureUlakRole(AIKopkariRider.UlakRole.Guard, i, assignedGuardCount);

        if (assignedGuardCount < guardRiderCount)
        {
            Debug.LogWarning(
                $"[{nameof(KopkariIntroFlowController)}] Requested {guardRiderCount} Ulak Guards, " +
                $"but only {assignedGuardCount} non-rival, non-orbit, non-Trap-Setter riders are available.",
                this);
        }
    }

    private IEnumerator TransitionToShot(Transform cameraPosition, float holdDuration)
    {
        if (cameraPosition == null)
            yield break;

        yield return BlinkClosed();
        MoveIntroCamera(cameraPosition);
        yield return null;
        yield return BlinkOpen();
        yield return WaitRealtime(holdDuration, true);
    }

    private IEnumerator TransitionToRivalShot(float holdDuration)
    {
        if (mainRivalCameraPosition == null && mainRival == null)
            yield break;

        yield return BlinkClosed();
        ShowMainRivalCharacteristics();
        if (mainRival != null && mainRival.IsReadyAtStart)
            mainRival.FaceInspectionCamera();
        if (mainRival != null && mainRival.InspectionCameraPoint != null)
            MoveIntroCamera(mainRival.InspectionCameraPoint);
        else if (mainRivalCameraPosition != null)
            MoveIntroCamera(mainRivalCameraPosition);
        else
            FrameRider(mainRival);
        yield return null;
        yield return BlinkOpen();
        mainRival?.PlayPresentationNeighTwice();
        yield return WaitRealtime(holdDuration, true);
        mainRival?.StopPresentationNeigh();
    }

    private IEnumerator TransitionToUlakShot(float holdDuration)
    {
        if (ulakCameraPosition == null)
            yield break;

        yield return BlinkClosed();
        ShowUlakCharacteristics();
        MoveIntroCamera(ulakCameraPosition);
        yield return null;
        yield return BlinkOpen();
        yield return WaitRealtime(holdDuration, true);
    }

    private IEnumerator TransitionToPlayerCharacteristics()
    {
        if (playerCameraPosition == null)
            yield break;

        yield return BlinkClosed();
        ShowLocalPlayerCharacteristics();
        MoveIntroCamera(playerCameraPosition);
        yield return null;
        yield return BlinkOpen();
        StartLocalPlayerPresentation();
        yield return WaitRealtime(localPlayerShotDuration, true);
        StopLocalPlayerPresentation();
    }

    private IEnumerator TransitionToPlayerListShot(Transform cameraPosition)
    {
        if (cameraPosition == null)
            cameraPosition = playerCameraPosition;

        yield return BlinkClosed();
        HideObjectCharacteristics();
        MoveIntroCamera(cameraPosition);
        SetIntroUiVisible(true);
        RefreshIntroReadinessUi();
        yield return null;
        yield return BlinkOpen();

        if (readyPlayerListDuration > 0f)
            yield return WaitRealtime(readyPlayerListDuration, true);
    }

    private IEnumerator InspectRandomRiders()
    {
        pendingRiderBuffer.Clear();
        for (int i = 0; i < riders.Count; i++)
        {
            AIKopkariRider rider = riders[i];
            if (rider != null && rider != mainRival)
                pendingRiderBuffer.Add(rider);
        }
        ShuffleRiders(pendingRiderBuffer);

        int minimumShots = Mathf.Max(0, Mathf.Min(
            minimumRiderInspectionShots,
            maximumRiderInspectionShots));
        int maximumShots = Mathf.Max(minimumShots, Mathf.Max(
            minimumRiderInspectionShots,
            maximumRiderInspectionShots));
        int targetShots = UnityEngine.Random.Range(minimumShots, maximumShots + 1);
        int shown = 0;

        for (int i = 0; i < pendingRiderBuffer.Count && shown < targetShots; i++)
        {
            if (skipRequested)
                yield break;

            AIKopkariRider rider = pendingRiderBuffer[i];
            if (rider == null)
                continue;

            yield return BlinkClosed();
            HidePlayerListKeepSkip();
            ShowRiderCharacteristics(rider);
            if (rider.IsReadyAtStart && rider.InspectionCameraPoint != null)
            {
                rider.FaceInspectionCamera();
                MoveIntroCamera(rider.InspectionCameraPoint);
            }
            else
                FrameRider(rider);
            yield return null;
            yield return BlinkOpen();
            yield return WaitRealtime(riderInspectionDuration, true);
            RefreshIntroReadinessUi();
            shown++;
        }
    }

    private IEnumerator MoveToPlayerCamera()
    {
        yield return BlinkClosed();
        StopLocalPlayerPresentation();
        SetIntroUiVisible(false);
        HideObjectCharacteristics();
        RestoreCameraState();
        PrepareGameplayCameraStartView();
        yield return null;
        PrepareGameplayCameraStartView();
        yield return new WaitForEndOfFrame();
        PrepareGameplayCameraStartView();

        if (gameplayBlendDuration > 0f)
            yield return WaitRealtime(gameplayBlendDuration, false);

        PrepareGameplayCameraStartView();
        yield return BlinkOpen();

        yield return ShowGameplayStartCountdown();
    }

    private IEnumerator ShowGameplayStartCountdown()
    {
        if (gameplayStartCountdown <= 0f)
        {
            SetCountdownVisible(false);
            yield break;
        }

        SetCountdownVisible(true);
        float deadline = Time.unscaledTime + gameplayStartCountdown;
        int displayedValue = -1;

        while (Time.unscaledTime < deadline)
        {
            int nextValue = Mathf.Max(1, Mathf.CeilToInt(deadline - Time.unscaledTime));
            if (nextValue != displayedValue)
            {
                displayedValue = nextValue;
                if (countText != null)
                    countText.text = displayedValue.ToString();
            }

            yield return null;
        }

        SetCountdownVisible(false);
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countBackground != null)
            countBackground.SetActive(visible);

        if (!visible && countText != null)
            countText.text = string.Empty;
    }

    private static void ShuffleRiders(List<AIKopkariRider> source)
    {
        for (int i = source.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            AIKopkariRider temp = source[i];
            source[i] = source[swapIndex];
            source[swapIndex] = temp;
        }
    }

    private IEnumerator WaitForRidersOrTimeout()
    {
        float deadline = Time.unscaledTime + riderReadyTimeout;

        while (!AreAllRidersReady() && Time.unscaledTime < deadline)
        {
            RefreshIntroReadinessUi();
            yield return null;
        }

        if (AreAllRidersReady())
            yield break;

        BuildPendingRiderBuffer();
        for (int i = 0; i < pendingRiderBuffer.Count; i++)
        {
            AIKopkariRider rider = pendingRiderBuffer[i];
            Debug.LogWarning(
                $"[{nameof(KopkariIntroFlowController)}] Rider '{rider.name}' did not reach '{rider.StartPoint?.name}'.",
                rider);

            if (teleportUnreadyRidersOnTimeout)
                rider.ForceReadyAtStart();
        }

        while (!AreAllRidersReady())
        {
            RefreshIntroReadinessUi();
            yield return null;
        }
    }

    private void CompleteIntro()
    {
        RestoreAIRiderCollisions();
        state = IntroState.Complete;
        isPlaying = false;
        introRoutine = null;
        InvokeCompletionOnce();
    }

    private void IsolateAIRiderCollisions()
    {
        RestoreAIRiderCollisions();

        aiHorseCollisionLayer = LayerMask.NameToLayer("AIHorse");
        if (aiHorseCollisionLayer < 0)
        {
            Debug.LogWarning(
                $"[{nameof(KopkariIntroFlowController)}] AIHorse layer was not found; intro AI collision isolation is disabled.",
                this);
            return;
        }

        originalAIHorseSelfCollisionIgnored = Physics.GetIgnoreLayerCollision(
            aiHorseCollisionLayer,
            aiHorseCollisionLayer);
        Physics.IgnoreLayerCollision(aiHorseCollisionLayer, aiHorseCollisionLayer, true);

        for (int riderIndex = 0; riderIndex < riders.Count; riderIndex++)
        {
            AIKopkariRider rider = riders[riderIndex];
            if (rider == null)
                continue;

            Collider[] colliders = rider.GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider target = colliders[colliderIndex];
                if (target == null ||
                    !target.enabled ||
                    !target.gameObject.activeInHierarchy ||
                    target.isTrigger)
                    continue;

                GameObject colliderObject = target.gameObject;
                if (!aiColliderOriginalLayers.ContainsKey(colliderObject))
                    aiColliderOriginalLayers.Add(colliderObject, colliderObject.layer);

                colliderObject.layer = aiHorseCollisionLayer;
            }
        }

        aiCollisionIsolationActive = true;
    }

    private void RestoreAIRiderCollisions()
    {
        if (!aiCollisionIsolationActive)
            return;

        foreach (KeyValuePair<GameObject, int> entry in aiColliderOriginalLayers)
        {
            if (entry.Key != null)
                entry.Key.layer = entry.Value;
        }
        aiColliderOriginalLayers.Clear();

        if (aiHorseCollisionLayer >= 0)
        {
            Physics.IgnoreLayerCollision(
                aiHorseCollisionLayer,
                aiHorseCollisionLayer,
                originalAIHorseSelfCollisionIgnored);
        }

        aiHorseCollisionLayer = -1;
        aiCollisionIsolationActive = false;
    }

    private void StartLocalPlayerPresentation()
    {
        StopLocalPlayerPresentation();

        localPlayerPresentationAnimal = KopkariManager.Instance != null
            ? KopkariManager.Instance.LocalRiderAnimal
            : null;
        localPlayerPresentationAnimal?.Mode_Activate(16, 1);
    }

    private void StopLocalPlayerPresentation()
    {
        if (localPlayerPresentationAnimal == null)
            return;

        localPlayerPresentationAnimal.Mode_Interrupt();
        localPlayerPresentationAnimal = null;
    }

    private void ShowGateCharacteristics()
    {
        SetObjectCharacteristicsActive(true);
        objectCharacteristics?.ShowGateMap();
    }

    private void ShowUlakCharacteristics()
    {
        SetObjectCharacteristicsActive(true);
        objectCharacteristics?.ShowUlak();
    }

    private void ShowMainRivalCharacteristics()
    {
        SetObjectCharacteristicsActive(true);
        objectCharacteristics?.ShowMainRival(mainRival);
    }

    private void ShowLocalPlayerCharacteristics()
    {
        SetObjectCharacteristicsActive(true);
        objectCharacteristics?.ShowLocalPlayer(playersList);
    }

    private void ShowRiderCharacteristics(AIKopkariRider rider)
    {
        SetObjectCharacteristicsActive(true);
        objectCharacteristics?.ShowRider(rider);
    }

    private void HideObjectCharacteristics()
    {
        if (objectCharacteristics == null)
            return;

        objectCharacteristics.HideAll();
        SetObjectCharacteristicsActive(false);
    }

    private void SetObjectCharacteristicsActive(bool active)
    {
        if (objectCharacteristics == null)
            return;

        GameObject target = objectCharacteristics.gameObject;
        if (target.activeSelf != active)
            target.SetActive(active);
    }

    private void HandleRiderReady(AIKopkariRider rider)
    {
        RefreshIntroReadinessUi();
    }

    private void HandleRiderPassedGate(AIKopkariRider rider)
    {
        if (!isPlaying || gateImpulseSource == null)
            return;

        if (state != IntroState.GateShot)
        {
            queuedGatePasses++;
            return;
        }

        TryGenerateGateImpulse();
    }

    private void FlushQueuedGateImpulse()
    {
        if (queuedGatePasses <= 0)
            return;

        queuedGatePasses = 0;
        TryGenerateGateImpulse();
    }

    private void TryGenerateGateImpulse()
    {
        if (gateImpulseSource == null)
            return;
        if (Time.unscaledTime - lastImpulseTime < minimumImpulseInterval)
            return;

        lastImpulseTime = Time.unscaledTime;
        gateImpulseSource.GenerateImpulseWithForce(gateImpulseStrength);
    }

    private void StartGateShake()
    {
        StopGateShake();
        if (gateImpulseSource == null || gateImpulseStrength <= 0f)
            return;

        gateShakeRoutine = StartCoroutine(GateShakeRoutine());
    }

    private void StopGateShake()
    {
        if (gateShakeRoutine == null)
            return;

        StopCoroutine(gateShakeRoutine);
        gateShakeRoutine = null;
    }

    private IEnumerator GateShakeRoutine()
    {
        while (isPlaying && state == IntroState.GateShot)
        {
            TryGenerateGateImpulse();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, activeGateShakeInterval));
        }

        gateShakeRoutine = null;
    }

    private bool AreAllRidersReady()
    {
        for (int i = 0; i < riders.Count; i++)
        {
            if (riders[i] != null && !riders[i].IsReadyAtStart)
                return false;
        }
        return true;
    }

    private void BuildPendingRiderBuffer()
    {
        pendingRiderBuffer.Clear();
        for (int i = 0; i < riders.Count; i++)
        {
            AIKopkariRider rider = riders[i];
            if (rider != null && !rider.IsReadyAtStart)
                pendingRiderBuffer.Add(rider);
        }
    }

    private Transform SelectRandomCameraPosition()
    {
        if (randomCameraPositions == null || randomCameraPositions.Count == 0)
            return null;

        int start = UnityEngine.Random.Range(0, randomCameraPositions.Count);
        for (int offset = 0; offset < randomCameraPositions.Count; offset++)
        {
            Transform candidate = randomCameraPositions[(start + offset) % randomCameraPositions.Count];
            if (candidate != null)
                return candidate;
        }
        return null;
    }

    private void MoveIntroCamera(Transform cameraPosition)
    {
        if (introCamera == null || cameraPosition == null)
            return;

        introCamera.Follow = null;
        introCamera.LookAt = null;
        introCamera.transform.SetPositionAndRotation(cameraPosition.position, cameraPosition.rotation);
        introCamera.PreviousStateIsValid = false;
    }

    private void FrameRider(AIKopkariRider rider)
    {
        if (introCamera == null || rider == null || rider.Animal == null)
            return;

        Transform horse = rider.Animal.transform;
        Vector3 lookPoint = horse.position + Vector3.up * 1.35f;
        Vector3 cameraPosition =
            horse.position - horse.forward * 5f + horse.right * 2.4f + Vector3.up * 2.1f;

        introCamera.Follow = null;
        introCamera.LookAt = null;
        introCamera.transform.position = cameraPosition;
        introCamera.transform.rotation = Quaternion.LookRotation(lookPoint - cameraPosition, Vector3.up);
        introCamera.PreviousStateIsValid = false;
    }

    private void CacheCameraState()
    {
        if (cameraStateCached)
            return;

        hasOriginalBlend = false;
        if (cinemachineBrain == null && Camera.main != null)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        if (cinemachineBrain != null)
        {
            originalBlend = cinemachineBrain.m_DefaultBlend;
            hasOriginalBlend = true;
            cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.EaseInOut,
                gameplayBlendDuration);
        }

        if (gameplayCamera != null)
            originalGameplayPriority = gameplayCamera.Priority;

        cameraStateCached = true;
    }

    private void PrepareCameraState()
    {
        SetBlinkAlpha(1f);
        SetIntroCameraActive();
    }

    private void SetIntroCameraActive()
    {
        if (introCamera == null)
            return;

        int priority = introActivePriority;
        if (gameplayCamera != null)
            priority = Mathf.Max(priority, gameplayCamera.Priority + 1);
        introCamera.Priority = priority;
        introCamera.PreviousStateIsValid = false;
    }

    private void PrepareGameplayCameraStartView()
    {
        if (gameplayFollowTarget == null && KopkariManager.Instance != null)
            gameplayFollowTarget = KopkariManager.Instance.GameplayFollowTarget;
        if (gameplayFollowTarget == null && gameplayCamera != null)
            gameplayFollowTarget = gameplayCamera.GetComponent<ThirdPersonFollowTarget>();

        if (gameplayFollowTarget == null)
            return;

        gameplayFollowTarget.SetPriority(true);
        gameplayFollowTarget.SetLookBackMode(false);
        gameplayFollowTarget.SetLook(Vector2.zero);

        if (gameplayFollowTarget.Target == null || gameplayFollowTarget.Target.Value == null)
            return;

        gameplayFollowTarget.TargetTeleport(false);
        gameplayFollowTarget._cinemachineTargetYaw = gameplayStartYaw;
        gameplayFollowTarget._cinemachineTargetPitch = gameplayStartPitch;
        ApplyGameplayCameraPivotStartView();
    }

    private void ApplyGameplayCameraPivotStartView()
    {
        if (gameplayFollowTarget == null || gameplayFollowTarget.CamPivot == null)
            return;
        if (gameplayFollowTarget.Target == null || gameplayFollowTarget.Target.Value == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(gameplayStartPitch, gameplayStartYaw, 0f);
        if (gameplayFollowTarget.UseUpVector && gameplayFollowTarget.UpVector != null)
            targetRotation = Quaternion.FromToRotation(Vector3.up, gameplayFollowTarget.UpVector.up) * targetRotation;

        gameplayFollowTarget.CamPivot.SetPositionAndRotation(
            gameplayFollowTarget.Target.Value.position,
            targetRotation);

        if (gameplayCamera != null)
            gameplayCamera.PreviousStateIsValid = false;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateCached)
            return;

        if (introCamera != null)
        {
            introCamera.Priority = introInactivePriority;
            introCamera.PreviousStateIsValid = false;
        }
        if (gameplayCamera != null)
        {
            gameplayCamera.Priority = originalGameplayPriority;
            gameplayCamera.PreviousStateIsValid = false;
        }
        if (hasOriginalBlend && cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend = originalBlend;

        cameraStateCached = false;
    }

    private IEnumerator BlinkClosed()
    {
        yield return FadeBlink(1f, blinkCloseDuration);
        if (blinkClosedHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blinkClosedHoldDuration);
    }

    private IEnumerator BlinkOpen()
    {
        yield return FadeBlink(0f, blinkOpenDuration);
    }

    private IEnumerator FadeBlink(float targetAlpha, float duration)
    {
        if (blinkCanvasGroup == null)
            yield break;

        blinkCanvasGroup.gameObject.SetActive(true);
        float startAlpha = blinkCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blinkCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        blinkCanvasGroup.alpha = targetAlpha;
        blinkCanvasGroup.blocksRaycasts = targetAlpha > 0.99f;
    }

    private IEnumerator WaitRealtime(float duration, bool allowSkip)
    {
        float deadline = Time.unscaledTime + Mathf.Max(0f, duration);
        while (Time.unscaledTime < deadline)
        {
            if (allowSkip && skipRequested)
                yield break;
            yield return null;
        }
    }

    private void SetBlinkAlpha(float alpha)
    {
        if (blinkCanvasGroup == null)
            return;

        blinkCanvasGroup.gameObject.SetActive(true);
        blinkCanvasGroup.alpha = alpha;
        blinkCanvasGroup.interactable = false;
        blinkCanvasGroup.blocksRaycasts = alpha > 0.99f;
    }

    private void SetIntroUiVisible(bool visible)
    {
        introPlayerListVisible = visible;
        allowSkipDisplay = visible;
        if (mainUI == null)
            return;

        mainUI.SetIntroPlayerListVisible(visible);
        mainUI.SetIntroSkipVisible(visible && AreAllRidersReady());
    }

    private void HidePlayerListKeepSkip()
    {
        introPlayerListVisible = false;
        allowSkipDisplay = true;
        if (mainUI == null)
            return;

        mainUI.SetIntroPlayerListVisible(false);
        mainUI.SetIntroSkipVisible(isPlaying && AreAllRidersReady());
    }

    private void RefreshIntroReadinessUi()
    {
        playersList?.RefreshReadiness();
        if (mainUI != null)
            mainUI.SetIntroSkipVisible(isPlaying && allowSkipDisplay && AreAllRidersReady());
    }

    private static float RandomDuration(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return UnityEngine.Random.Range(Mathf.Max(0f, min), Mathf.Max(0f, max));
    }

    private void InvokeCompletionOnce()
    {
        if (completionInvoked)
            return;

        completionInvoked = true;
        Action callback = completionCallback;
        completionCallback = null;
        callback?.Invoke();
    }
}
