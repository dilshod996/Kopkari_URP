using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.HAP;
using Michsky.UI.ModernUIPack;
using UnluckSoftware;
using UnityEngine.Serialization;

public class KopkariManager : MonoBehaviour
{
    [Serializable]
    public sealed class RoundPoints
    {
        [SerializeField] private Transform warmupPosition;
        [SerializeField] private Transform ulakPosition;
        [SerializeField] private Transform targetPosition;
        [SerializeField, Min(1f)] private float roundTime = 120f;
        [SerializeField, Min(1f)] private float warmupDuration = 10f;
        [SerializeField, Min(0)] private int coinAmount;
        [SerializeField, Min(0)] private int nyufiyAmount;
        [SerializeField, Min(0f)] private float comboTime = 10f;
        [SerializeField, Min(0)] private int comboPrize;

        public Transform WarmupPosition => warmupPosition;
        public Transform UlakPosition => ulakPosition;
        public Transform TargetPosition => targetPosition;
        public float RoundTime => roundTime;
        public float WarmupDuration => warmupDuration;
        public int CoinAmount => coinAmount;
        public int NyufiyAmount => nyufiyAmount;
        public float ComboTime => comboTime;
        public int ComboPrize => comboPrize;
        public bool IsValid => warmupPosition != null && ulakPosition != null && targetPosition != null;
    }

    [Header("-------------HorseAnimal---------------")]
    public MAnimal horseAnimal;
    public MAnimal LocalRiderAnimal { get; private set; }
    public PlayerDataManager playerDataManager;
    [SerializeField] private ModalWindowManager modalWindowPopup;

    [Header("Gameplay Cameras")]
    [SerializeField] private ThirdPersonFollowTarget mainCam;
    [SerializeField] private ThirdPersonFollowTarget sprintCam;
    public ThirdPersonFollowTarget GameplayFollowTarget => mainCam;

    [Header("Mobile Performance")]
    [Tooltip("Mobile frame-rate cap. 30 FPS substantially reduces sustained battery use and device heat.")]
    [SerializeField, Range(30, 60)] private int mobileTargetFrameRate = 30;

    [Header("Main Time Fallback")]
    public float mainTime = 0f;
    private float totalMainTime;

    [Header("Lamp Realated")]
    public string LambOwner;
    [SerializeField] private GameObject myLamb;
    [FormerlySerializedAs("targetPos"), FormerlySerializedAs("legacyTargetPosition")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Transform ulakBottomObject;
    [Header("Round Points")]
    [Tooltip("Each entry keeps one round's Warmup, Ulak and Target positions together.")]
    [SerializeField] private List<RoundPoints> rounds = new List<RoundPoints>();
    [SerializeField, Min(0f)] private float roundStartCountdown = 3f;
    [SerializeField, Min(0f)] private float winnerNeighHoldDuration = 1.25f;
    [SerializeField] private KopkariWarmupTrigger warmupTrigger;

    // Hidden migration fallback for scenes created before grouped Round Points.
    [FormerlySerializedAs("firstSalymPosition"), SerializeField, HideInInspector]
    private Transform legacyFirstSalymPosition;
    [FormerlySerializedAs("secondRoundWarmupPoint"), SerializeField, HideInInspector]
    private Transform legacyWarmupPosition;

    [Header("Gameplay")]
    public Pickable pickableObj;
    [SerializeField, Min(0f)] private float carrierTakeoverWindow = 1.5f;

    [Header("Uloq Crowd Weather")]
    [Tooltip("Optional. If empty, the scene's Stylized Weather Controller is found automatically once.")]
    [SerializeField] private StylizedWeatherController weatherController;
    [SerializeField] private string crowdedUloqWeatherName = "Dust Storm";
    [SerializeField] private string clearUloqWeatherName = "No Weather";
    [Tooltip("Total nearby riders, including the local player, required while the Uloq is on the ground.")]
    [SerializeField, Min(2)] private int crowdedUloqRiderThreshold = 4;
    [Tooltip("Matches the local player's competitive pickup radius by default.")]
    [SerializeField, Min(0.1f)] private float crowdedUloqRadius = 6f;
    [SerializeField, Min(0.1f)] private float crowdedUloqCheckInterval = 0.25f;

    public GameObject currentGoatOwner;
    private GameObject lastReleasedGoatOwner;
    private float lastGoatOwnerReleaseTime = float.NegativeInfinity;
    private CapsuleCollider carrierRiderMainCollider;
    private bool carrierRiderMainColliderWasTrigger;
    public Transform UlakTransform => pickableObj != null
        ? pickableObj.transform
        : (myLamb != null ? myLamb.transform : null);
    public Transform CurrentWarmupPosition => CurrentRound != null
        ? CurrentRound.WarmupPosition
        : legacyWarmupPosition;
    public Transform CurrentUlakPosition => CurrentRound != null
        ? CurrentRound.UlakPosition
        : UlakTransform;
    public Transform CurrentTargetPosition => CurrentRound != null
        ? CurrentRound.TargetPosition
        : (legacyFirstSalymPosition != null
            ? legacyFirstSalymPosition
            : (targetObject != null ? targetObject.transform : null));
    public Transform FirstSalymPosition => CurrentTargetPosition;
    public int CurrentRoundIndex => currentRoundIndex;
    public int CurrentRoundNumber => completedRoundCount + 1;
    public int TotalRoundCount => GetValidRoundCount();
    public float CurrentWarmupDuration => CurrentRound != null ? CurrentRound.WarmupDuration : 10f;
    public float CurrentComboTime => CurrentRound != null ? CurrentRound.ComboTime : 0f;
    public int CurrentComboPrize => CurrentRound != null ? CurrentRound.ComboPrize : 0;
    public bool HasPreparedNextRound => nextRoundPrepared;
    public bool IsRoundWarmupActive { get; private set; }
    public IReadOnlyList<CheckpointTrigger> Checkpoints => checkpoints;

    [SerializeField] private bool isCatched;
    public bool IsCatched { get => isCatched; set => isCatched = value; }

    public enum RoomState
    {
        None = 0,
        GameStarted = 2,
        WaterDropped = 3,
        TimeFinished = 4,
        HorseStamenaFinished = 5,
        RiderStamenaFinished = 6,
        LambReachTarget = 7,
        GameFinished = 8,
        PlayerEliminated = 9,
        Won = 10
    }
    public RoomState roomState = RoomState.None;

    [Header("Pooled VFX")]
    public VFXPool pool;

    public enum PlayerCondition
    {
        Start,
        GettingTarget,
        GotTarget,
        NearTarget,
        AwayTarget,
        DroppedTarget,
        TakenTargetOthers,
        WinnerSession,
        LoserSession,
        SpeedUp,
        StaminaLimit,
        HealthLimit,
        CatchLimit,
        WaterEntered,
        EagleWatching,
        MapLimit,
        MainTimeOver,
        None
    }
    public PlayerCondition currentCondition = PlayerCondition.None;

    #region Camera Details
    [SerializeField] private float frontDistance = 6f;
    [SerializeField] private float backDistance = 3f;
    [SerializeField] private float backOffsetY = 0.4f;
    #endregion

    #region Events
    public static Action<bool> OnGameStartFinishState;
    public static Action OnGameStarted;
    public static Action OnMainGameStarted;
    public static Action OnResetTarget;
    public static Action<bool> OnGoatPicked;
    public static Action<GameObject> OnGoatOwnerChanged;
    public static Action OnTimeFinished;
    public static Action OnSceneReady;
    public static bool IsSceneReady { get; private set; }
    #endregion

    [Header("Checkpoints")]
    [SerializeField] private List<CheckpointTrigger> checkpoints = new List<CheckpointTrigger>();

    [Header("Local Player")]
    [SerializeField] private GameObject LocalRiderRoot;
    [SerializeField] private KopkariCarrierGrip localPlayerGrip;
    [SerializeField, Min(0.6f)] private float localPlayerGripContactInterval = 0.6f;
    [SerializeField, Min(0.25f)] private float localPlayerGripContactRadius = 1.65f;
    [SerializeField] private Vector3 localPlayerGripContactOffset = new Vector3(0f, 0.8f, 0.25f);
    [SerializeField] private LayerMask localPlayerGripContactLayers = ~0;
    private Coroutine localPlayerGripContactCoroutine;
    private Coroutine uloqCrowdWeatherCoroutine;
    private readonly Collider[] localPlayerGripContactBuffer = new Collider[24];
    private float lastLocalPlayerGripValue;
    private bool isCrowdedUloqWeatherActive;
    private BoostersContainer localPlayerBoosters;
    private bool localPlayerSprinting;

    [Header("Room Resources")]
    public GameObject walkZonePrefab;
    public GameObject oneTimeGetEffect;
    public GameObject walkZoneFlash;

    public static Action<Transform> OnHorseTransform;

    [Header("Popup Data")]
    public UISpeechBuble speechBubble;

    private int UserId;
    private bool roundEnded = false;
    private bool timeFinishedHandled = false;
    private readonly List<int> unusedRoundIndices = new List<int>();
    private readonly List<AIKopkariRider> roundRiders = new List<AIKopkariRider>();
    private int currentRoundIndex = -1;
    private int completedRoundCount;
    private bool roundPoolInitialized;
    private bool nextRoundPrepared;
    private bool localPlayerWarmupQualified;
    private float configuredRoundTime;
    private Coroutine roundTransitionCoroutine;
    // ✅ Eski BaseManager.Instance o‘rniga shu ishlaydi
    public static KopkariManager Instance { get; private set; }

    private bool poolsCreated = false;
    private bool sceneReadySignaled = false;
    private Coroutine sceneReadyRoutine;

    //Horse Statistics
    private float webSnareDamageTime;
    private float boostTime;
    public GameOverTypes gameOverTypes = GameOverTypes.None;

    private RoundPoints CurrentRound => currentRoundIndex >= 0 && currentRoundIndex < rounds.Count
        ? rounds[currentRoundIndex]
        : null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        IsSceneReady = false;
        sceneReadySignaled = false;
#if UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = Mathf.Clamp(mobileTargetFrameRate, 30, 60);
#else
        Application.targetFrameRate = 60;
#endif
        QualitySettings.vSyncCount = 0;
        configuredRoundTime = mainTime;

        if (modalWindowPopup != null)
            modalWindowPopup.onConfirm.AddListener(MoveLobby);

        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();
        warmupTrigger?.Deactivate();
    }

    private void Start()
    {
        if (pickableObj != null)
        {
            pickableObj.OnPicked.RemoveListener(OnUloqPicked);
            pickableObj.OnPicked.AddListener(OnUloqPicked);
            pickableObj.OnFocusedBy.RemoveListener(HandleUlakFocusedBy);
            pickableObj.OnFocusedBy.AddListener(HandleUlakFocusedBy);
            pickableObj.OnUnfocusedBy.RemoveListener(HandleUlakUnfocusedBy);
            pickableObj.OnUnfocusedBy.AddListener(HandleUlakUnfocusedBy);
        }

        // ✅ Pool create faqat bir marta
        CreatePoolsOnce();

        if(gameOverTypes != GameOverTypes.None)
        {
            gameOverTypes = GameOverTypes.None;
        }
    }

    private void Update()
    {
        switch (roomState)
        {
            case RoomState.GameStarted:
                MainGameTimeTick();
                break;
            case RoomState.TimeFinished:
                GameOverAction();
                break;
            case RoomState.GameFinished:
                break;
        }
    }

    private void OnEnable()
    {
        KopkariMainUI.OnSprintStart += HorseSprint;
        KopkariMainUI.OnSprintEnd += HorseDefaultSpeed;

        BoostersContainer.OnSprintEffectStart += SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd += SprintCameraDisable;

        PlayerDataManager.OnLocalPlayerObject += RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse += RegisterPlayerAndHorse;

        // Kopkari scene flow
        KopkariMainUI.OnEverythingReadyStart += StartGame;
        TargetReachEvent.OnReachedTargetWithLamb += HandleRoundWinner;
        UILookBackButton.OnCameraPressedState += CameraBackState;
        BoostersContainer.OnBoostTime += SetBoostTime;
        BoostersContainer.OnPenaltyTime += SetPenaltyTime;
        BoostersContainer.OnWalkZoneDamaged += HandleLocalPlayerWalkZoneDamaged;
        BindLocalPlayerAttackDamage(FindLocalPlayerBoosters());
        BindLocalPlayerGrip();
    }

    private void OnDisable()
    {
        KopkariMainUI.OnSprintStart -= HorseSprint;
        KopkariMainUI.OnSprintEnd -= HorseDefaultSpeed;

        BoostersContainer.OnSprintEffectStart -= SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd -= SprintCameraDisable;

        PlayerDataManager.OnLocalPlayerObject -= RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse -= RegisterPlayerAndHorse;

        // Kopkari scene flow
        KopkariMainUI.OnEverythingReadyStart -= StartGame;
        TargetReachEvent.OnReachedTargetWithLamb -= HandleRoundWinner;
        UILookBackButton.OnCameraPressedState -= CameraBackState;
        BoostersContainer.OnPenaltyTime -= SetPenaltyTime;
        BoostersContainer.OnBoostTime -= SetBoostTime;
        BoostersContainer.OnWalkZoneDamaged -= HandleLocalPlayerWalkZoneDamaged;
        UnbindLocalPlayerAttackDamage();
        StopLocalPlayerGripContactMonitoring();
        StopUloqCrowdWeatherMonitoring();
        RestoreCarrierRiderMainCollider();
        KopkariRiderSpeedController.RestoreUnmodifiedSpeed(horseAnimal);

        if (roundTransitionCoroutine != null)
        {
            StopCoroutine(roundTransitionCoroutine);
            roundTransitionCoroutine = null;
        }
        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();
    }

    private void OnDestroy()
    {
        if (pickableObj != null)
        {
            pickableObj.OnPicked.RemoveListener(OnUloqPicked);
            pickableObj.OnFocusedBy.RemoveListener(HandleUlakFocusedBy);
            pickableObj.OnUnfocusedBy.RemoveListener(HandleUlakUnfocusedBy);
        }

        if (modalWindowPopup != null)
            modalWindowPopup.onConfirm.RemoveListener(MoveLobby);

        UnbindLocalPlayerGrip();

        if (Instance == this) Instance = null;
        IsSceneReady = false;
        SimplePool.ClearAll();
    }
    private void StartMainGame()
    {
        float selectedRoundTime = CurrentRound != null ? CurrentRound.RoundTime : configuredRoundTime;
        if (selectedRoundTime <= 0f)
            selectedRoundTime = configuredRoundTime > 0f ? configuredRoundTime : mainTime;

        mainTime = Mathf.Max(1f, selectedRoundTime);
        totalMainTime = mainTime;
        roomState = RoomState.GameStarted;
        webSnareDamageTime = 0;
        StartUloqCrowdWeatherMonitoring();
        KopkariMainUI.Instance?.UpdateMainTime(mainTime);
        KopkariMainUI.Instance?.UpdateRoundProgress(CurrentRoundNumber, TotalRoundCount);
        OnMainGameStarted?.Invoke();
    }

    #region Game Starting
    private void CreatePoolsOnce()
    {
        if (poolsCreated) return;
        poolsCreated = true;

        if (walkZonePrefab != null)
            SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);

        if (oneTimeGetEffect != null)
            SimplePool.CreatePool(oneTimeGetEffect, prewarm: 5, maxSize: 40, expandable: true);

        if (walkZoneFlash != null)
            SimplePool.CreatePool(walkZoneFlash, prewarm: 5, maxSize: 40, expandable: true);
    }
    private void MainGameTimeTick()
    {
        if (mainTime > 0f && !timeFinishedHandled)
        {
            mainTime -= Time.deltaTime;

            if (mainTime < 0f)
                mainTime = 0f;

            KopkariMainUI.Instance?.UpdateMainTime(mainTime);
            if (mainTime <= 0f)
                HandleRoundTimeExpired();
        }
        else if (!timeFinishedHandled)
        {
            KopkariMainUI.Instance?.UpdateMainTime(0f);
            HandleRoundTimeExpired();
        }
    }
    public float GetUsedMainTime()
    {
        return totalMainTime - mainTime;
    }

    #endregion
    private void RegisterPlayerAndHorse(MAnimal horse, MAnimal player)
    {
        horseAnimal = horse;
        LocalRiderAnimal = player;
        localPlayerSprinting = false;
        ApplyLocalPlayerSpeed(false);
        BindLocalPlayerAttackDamage(FindLocalPlayerBoosters());
        BindLocalPlayerGrip();

        if (sceneReadyRoutine != null)
            StopCoroutine(sceneReadyRoutine);

        sceneReadyRoutine = StartCoroutine(CompleteSceneReadyWhenCameraSettled());
    }

    private IEnumerator CompleteSceneReadyWhenCameraSettled()
    {
        if (sceneReadySignaled) yield break;

        yield return null;
        yield return new WaitForEndOfFrame();

        PrepareStartupCameraView();

        for (int i = 0; i < 3; i++)
            yield return null;

        var brain = mainCam != null ? mainCam.Brain : null;
        float waitUntil = Time.unscaledTime + 2f;

        while (brain != null && brain.IsBlending && Time.unscaledTime < waitUntil)
            yield return null;

        yield return new WaitForEndOfFrame();

        sceneReadySignaled = true;
        IsSceneReady = true;
        SceneLoadManager.Instance?.SetAssetInstantiationFinished(true);
        OnSceneReady?.Invoke();
    }

    private void PrepareStartupCameraView()
    {
        if (mainCam == null)
            return;

        if ((mainCam.Target == null || mainCam.Target.Value == null) && horseAnimal != null)
            mainCam.SetTarget(horseAnimal.transform);

        mainCam.SetPriority(true);
        mainCam.SetLookBackMode(false);

        if (mainCam.Target == null || mainCam.Target.Value == null)
            return;

    }

    public void FinalPosState(bool state)
    {
        GameObject target = targetObject;
        if (target != null && !target.activeSelf)
            target.SetActive(true);
    }



    #region Pick Up Uloq
    private void OnUloqPicked(GameObject pickerObj)
    {
        if (pickerObj == null)
            return;

        NotifyGoatOwner(pickerObj, true);
        StartCoroutine(NotifyRoom());
    }

    /// <summary>
    /// Completes an eligible AI rider's timed pickup attempt against a live carrier.
    /// The old and new holders are both changed through Malbers MPickUp so the
    /// physical parent, picker reference, events and ownership remain consistent.
    /// </summary>
    public bool TryTransferUlakToAIRider(AIKopkariRider challenger)
    {
        if (roomState != RoomState.GameStarted || challenger == null || pickableObj == null ||
            currentGoatOwner == null || !challenger.CanTakeCarriedUlak)
        {
            return false;
        }

        GameObject challengerRoot = NormalizeRiderOwner(challenger.gameObject);
        if (challengerRoot == null || challengerRoot == currentGoatOwner)
            return false;

        MPickUp newPicker = challenger.PickupController;
        MPickUp previousPicker = pickableObj.Picker;
        if (newPicker == null || newPicker.Has_Item || previousPicker == null ||
            previousPicker == newPicker || !previousPicker.Has_Item || previousPicker.Item != pickableObj)
        {
            return false;
        }

        GameObject previousOwner = currentGoatOwner;
        if (!previousPicker.enabled)
            previousPicker.enabled = true;
        previousPicker.DropItem();

        if (previousPicker.Has_Item || pickableObj.Picker != null)
            return false;

        if (!newPicker.enabled)
            newPicker.enabled = true;

        // Drop sets the normal ground cooldown. A completed carrier takeover has
        // already paid its full pickup-focus duration, so it may attach immediately.
        pickableObj.CurrentPickTime = float.NegativeInfinity;
        newPicker.FocusedItem = pickableObj;
        pickableObj.SetFocused(newPicker.Owner, true);
        newPicker.PickUpItem();

        bool transferred = newPicker.Has_Item && newPicker.Item == pickableObj &&
                           pickableObj.Picker == newPicker;
        if (!transferred && currentGoatOwner == previousOwner)
            NotifyGoatOwner(previousOwner, false);
        return transferred;
    }

    private void HandleUlakFocusedBy(GameObject focusOwner)
    {
        if (roomState == RoomState.GameStarted && focusOwner != null)
            AIKopkariRider.NotifyUlakFocusedBy(focusOwner);
    }

    private void HandleUlakUnfocusedBy(GameObject focusOwner)
    {
        if (focusOwner != null)
            AIKopkariRider.NotifyUlakUnfocusedBy(focusOwner);
    }

    private IEnumerator NotifyRoom()
    {
        yield return new WaitForSeconds(0.5f);

        var kopkariResult = KopkariResultsManager.Instance;
        string pickerName = PlayerPrefs.GetString(Constants.Player.UsernameKey);

        if (kopkariResult != null && kopkariResult.UloqOwner == pickerName)
        {
            if (speechBubble != null)
                speechBubble.ShowPopup(LanguageManager.Instance?.GetText(510)); // Lets go!!! Faster Polvon"
        }
        else
        {
            if (kopkariResult != null)
            {
                string ownerName = $"<color=#FFD700>{kopkariResult.UloqOwner} Polvon</color>";
                string message = string.Format(LanguageManager.Instance.GetText(511), ownerName);
                if (speechBubble != null)
                    speechBubble.ShowPopup(message);
            }
               // speechBubble?.ShowPopup($"<color=#FFD700>{kopkariResult.UloqOwner} Polvon</color> has taken the Ulak.");
        }
    }

    public void NotifyGoatOwner(GameObject ownerRoot, bool hasGoat)
    {
        bool isLocalPlayer = ownerRoot != null && IsLocalRiderTransform(ownerRoot.transform);
        ownerRoot = NormalizeRiderOwner(ownerRoot);
        isLocalPlayer |= LocalRiderRoot != null && ownerRoot == LocalRiderRoot;

        if (hasGoat)
        {
            if (ownerRoot == null || currentGoatOwner == ownerRoot)
                return;

            GameObject previousOwner = currentGoatOwner;
            if (previousOwner == null &&
                lastReleasedGoatOwner != null &&
                lastReleasedGoatOwner != ownerRoot &&
                Time.time - lastGoatOwnerReleaseTime <= Mathf.Max(0f, carrierTakeoverWindow))
            {
                previousOwner = lastReleasedGoatOwner;
            }
            bool takenFromCarrier = previousOwner != null && previousOwner != ownerRoot;
            if (TryGetRiderId(previousOwner, out int previousOwnerId))
                KopkariResultsManager.Instance?.OnLambDropped(previousOwnerId);
            if (TryGetRiderId(ownerRoot, out int newOwnerId))
                KopkariResultsManager.Instance?.OnLambPicked(newOwnerId, takenFromCarrier);

            if (previousOwner != null && previousOwner != ownerRoot &&
                IsLocalRiderTransform(previousOwner.transform))
            {
                EndLocalPlayerGrip();
            }

            SetCurrentGoatOwner(ownerRoot);
            // Checkpoints are intentionally inactive for the current game rules.
            // Every successful pickup immediately opens the selected round target.
            FinalPosState(true);
            lastReleasedGoatOwner = null;
            lastGoatOwnerReleaseTime = float.NegativeInfinity;

            if (isLocalPlayer)
            {
                OnGoatPicked?.Invoke(true);
                IsCatched = true;
                BeginLocalPlayerGrip(ownerRoot);
            }
            else
            {
                EndLocalPlayerGrip();
                IsCatched = false;
            }
        }
        else
        {
            if (currentGoatOwner == ownerRoot)
            {
                if (TryGetRiderId(ownerRoot, out int droppedOwnerId))
                    KopkariResultsManager.Instance?.OnLambDropped(droppedOwnerId);

                if (isLocalPlayer)
                    EndLocalPlayerGrip();

                lastReleasedGoatOwner = ownerRoot;
                lastGoatOwnerReleaseTime = Time.time;
                SetCurrentGoatOwner(null);
                IsCatched = false;
            }
        }
    }

    private bool TryGetRiderId(GameObject riderRoot, out int riderId)
    {
        if (riderRoot == null)
        {
            riderId = 0;
            return false;
        }

        if (LocalRiderRoot != null && riderRoot == LocalRiderRoot)
        {
            riderId = PlayerPrefs.GetInt(Constants.Player.Userid, UserId);
            return true;
        }

        AIKopkariRider rider = riderRoot.GetComponentInChildren<AIKopkariRider>(true);
        if (rider != null)
        {
            riderId = rider.GetId();
            return true;
        }

        riderId = 0;
        return false;
    }

    private void SetCurrentGoatOwner(GameObject ownerRoot)
    {
        if (currentGoatOwner == ownerRoot)
            return;

        currentGoatOwner = ownerRoot;
        ApplyCarrierRiderMainCollider(ownerRoot);
        if (ownerRoot != null)
            SetCrowdedUloqWeather(false);
        UpdateLocalPlayerComboForOwner();
        OnGoatOwnerChanged?.Invoke(currentGoatOwner);
    }

    private void StartUloqCrowdWeatherMonitoring()
    {
        StopUloqCrowdWeatherMonitoring();
        if (!ResolveWeatherController())
            return;

        uloqCrowdWeatherCoroutine = StartCoroutine(UloqCrowdWeatherRoutine());
    }

    private void StopUloqCrowdWeatherMonitoring()
    {
        if (uloqCrowdWeatherCoroutine != null)
        {
            StopCoroutine(uloqCrowdWeatherCoroutine);
            uloqCrowdWeatherCoroutine = null;
        }

        SetCrowdedUloqWeather(false);
    }

    private IEnumerator UloqCrowdWeatherRoutine()
    {
        WaitForSeconds delay = new WaitForSeconds(Mathf.Max(0.1f, crowdedUloqCheckInterval));

        while (roomState == RoomState.GameStarted && !roundEnded)
        {
            RefreshCrowdedUloqWeather();
            yield return delay;
        }

        uloqCrowdWeatherCoroutine = null;
        SetCrowdedUloqWeather(false);
    }

    private void RefreshCrowdedUloqWeather()
    {
        Transform ulak = UlakTransform;
        bool canShowDust = roomState == RoomState.GameStarted &&
                           !roundEnded &&
                           currentGoatOwner == null &&
                           ulak != null &&
                           ulak.gameObject.activeInHierarchy &&
                           (pickableObj == null || pickableObj.CanBePicked);

        if (!canShowDust)
        {
            SetCrowdedUloqWeather(false);
            return;
        }

        float radius = Mathf.Max(0.1f, crowdedUloqRadius);
        int nearbyRiderCount = AIKopkariRider.CountActiveRidersNear(ulak.position, radius);
        Transform localPlayer = GetLocalPlayerWeatherTarget();
        if (localPlayer != null &&
            (localPlayer.position - ulak.position).sqrMagnitude <= radius * radius)
        {
            nearbyRiderCount++;
        }

        SetCrowdedUloqWeather(nearbyRiderCount >= Mathf.Max(2, crowdedUloqRiderThreshold));
    }

    private Transform GetLocalPlayerWeatherTarget()
    {
        if (horseAnimal != null)
            return horseAnimal.transform;
        if (LocalRiderAnimal != null)
            return LocalRiderAnimal.transform;
        return LocalRiderRoot != null ? LocalRiderRoot.transform : null;
    }

    private bool ResolveWeatherController()
    {
        if (weatherController == null)
            weatherController = FindObjectOfType<StylizedWeatherController>(true);
        return weatherController != null;
    }

    private void SetCrowdedUloqWeather(bool active)
    {
        if (isCrowdedUloqWeatherActive == active || !ResolveWeatherController())
            return;

        string weatherName = active ? crowdedUloqWeatherName : clearUloqWeatherName;
        if (string.IsNullOrWhiteSpace(weatherName))
            return;

        weatherController.ChangeWeather(weatherName);
        isCrowdedUloqWeatherActive = active;
    }

    private void ApplyCarrierRiderMainCollider(GameObject ownerRoot)
    {
        RestoreCarrierRiderMainCollider();
        if (ownerRoot == null)
            return;

        MRider carrierRider = ownerRoot.GetComponentInChildren<MRider>(true);
        if (carrierRider == null)
            carrierRider = ownerRoot.GetComponentInParent<MRider>();

        CapsuleCollider mainCollider = carrierRider != null ? carrierRider.MainCollider : null;
        if (mainCollider == null)
            return;

        carrierRiderMainCollider = mainCollider;
        carrierRiderMainColliderWasTrigger = mainCollider.isTrigger;
        mainCollider.isTrigger = true;
    }

    private void RestoreCarrierRiderMainCollider()
    {
        CapsuleCollider previousCollider = carrierRiderMainCollider;
        bool previousTriggerState = carrierRiderMainColliderWasTrigger;
        carrierRiderMainCollider = null;
        carrierRiderMainColliderWasTrigger = false;

        if (previousCollider != null)
            previousCollider.isTrigger = previousTriggerState;
    }

    private void UpdateLocalPlayerComboForOwner()
    {
        KopkariMainUI mainUI = KopkariMainUI.Instance;
        if (mainUI == null)
            return;

        bool localPlayerOwnsUlak = roomState == RoomState.GameStarted &&
                                   currentGoatOwner != null &&
                                   IsLocalRiderTransform(currentGoatOwner.transform);
        if (localPlayerOwnsUlak)
            mainUI.ShowCombo();
        else
            mainUI.HideCombo();
    }

    private GameObject NormalizeRiderOwner(GameObject ownerCandidate)
    {
        if (ownerCandidate == null)
            return null;

        if (IsLocalRiderTransform(ownerCandidate.transform))
        {
            if (LocalRiderRoot != null)
                return LocalRiderRoot;
            if (horseAnimal != null)
                return horseAnimal.transform.root.gameObject;
            if (LocalRiderAnimal != null)
                return LocalRiderAnimal.transform.root.gameObject;
        }

        AIKopkariRider aiRider = ownerCandidate.GetComponentInParent<AIKopkariRider>();
        if (aiRider == null)
            aiRider = ownerCandidate.GetComponentInChildren<AIKopkariRider>(true);
        return aiRider != null ? aiRider.transform.root.gameObject : ownerCandidate.transform.root.gameObject;
    }

    public Transform ResolveGoatOwnerTarget(GameObject ownerRoot)
    {
        GameObject normalizedOwner = NormalizeRiderOwner(ownerRoot);
        if (normalizedOwner == null)
            return null;

        if (LocalRiderRoot != null && normalizedOwner == LocalRiderRoot)
            return horseAnimal != null ? horseAnimal.transform : LocalRiderRoot.transform;

        AIKopkariRider aiRider = normalizedOwner.GetComponentInChildren<AIKopkariRider>(true);
        return aiRider != null && aiRider.Animal != null
            ? aiRider.Animal.transform
            : normalizedOwner.transform;
    }

    public bool IsLocalRiderTransform(Transform candidate)
    {
        if (candidate == null)
            return false;

        MPickUp localPickup = playerDataManager != null ? playerDataManager.PickupController : null;
        if (localPickup != null && localPickup.Root != null &&
            IsSameHierarchy(candidate, localPickup.Root))
            return true;

        if (horseAnimal != null && IsSameHierarchy(candidate, horseAnimal.transform))
            return true;

        if (LocalRiderAnimal != null && IsSameHierarchy(candidate, LocalRiderAnimal.transform))
            return true;

        return LocalRiderRoot != null && IsSameHierarchy(candidate, LocalRiderRoot.transform);
    }

    public bool IsCurrentGoatOwnerTransform(Transform candidate)
    {
        if (candidate == null || currentGoatOwner == null)
            return false;

        GameObject normalizedCandidate = NormalizeRiderOwner(candidate.gameObject);
        return normalizedCandidate != null && normalizedCandidate == currentGoatOwner;
    }

    private static bool IsSameHierarchy(Transform first, Transform second)
    {
        return first != null && second != null &&
               (first == second || first.IsChildOf(second) || second.IsChildOf(first));
    }
    #endregion

    #region Drop Uloq
    public void TriggerEvent()
    {
        IsCatched = false;
        GameObject owner = currentGoatOwner;
        if (owner != null)
            NotifyGoatOwner(owner, false);
        playerDataManager?.DropObject();
        if (pickableObj != null && pickableObj.Picker != null)
            pickableObj.ForceDrop();
    }

    #endregion

    private bool SelectUnusedRound()
    {
        if (!roundPoolInitialized)
        {
            unusedRoundIndices.Clear();
            for (int i = 0; i < rounds.Count; i++)
            {
                RoundPoints candidate = rounds[i];
                if (candidate != null && candidate.IsValid)
                    unusedRoundIndices.Add(i);
                else
                    Debug.LogWarning($"[{nameof(KopkariManager)}] Round Points element {i} is incomplete and will be skipped.", this);
            }

            roundPoolInitialized = true;
        }

        if (unusedRoundIndices.Count == 0)
            return false;

        int poolIndex = UnityEngine.Random.Range(0, unusedRoundIndices.Count);
        currentRoundIndex = unusedRoundIndices[poolIndex];
        unusedRoundIndices.RemoveAt(poolIndex);
        PositionCurrentRoundMarkers();
        return true;
    }

    private void PositionCurrentRoundMarkers()
    {
        Transform targetPosition = CurrentTargetPosition;
        if (targetObject != null)
        {
            if (targetPosition != null)
                targetObject.transform.SetPositionAndRotation(targetPosition.position, targetPosition.rotation);
            targetObject.SetActive(true);
        }

        Transform ulakPosition = CurrentUlakPosition;
        if (ulakBottomObject != null)
        {
            if (ulakPosition != null)
                ulakBottomObject.SetPositionAndRotation(ulakPosition.position, ulakPosition.rotation);
            ulakBottomObject.gameObject.SetActive(true);
        }
    }

    private int GetValidRoundCount()
    {
        int count = 0;
        for (int i = 0; i < rounds.Count; i++)
        {
            if (rounds[i] != null && rounds[i].IsValid)
                count++;
        }

        return count;
    }

    private bool EnsureInitialRoundSelected()
    {
        if (CurrentRound != null)
            return true;

        if (rounds.Count > 0)
            return SelectUnusedRound();

        // Old scenes can still run once while their grouped list is being set up.
        return targetObject != null && UlakTransform != null;
    }

    private void PrepareCurrentRoundForGameplay()
    {
        GameObject owner = currentGoatOwner;
        if (owner != null)
            NotifyGoatOwner(owner, false);
        lastReleasedGoatOwner = null;
        lastGoatOwnerReleaseTime = float.NegativeInfinity;
        IsCatched = false;

        PositionCurrentRoundMarkers();

        Transform ulak = UlakTransform;
        Transform ulakPosition = CurrentUlakPosition;
        if (ulakPosition == null)
            return;

        if (ulak == null)
            return;

        GameObject ulakObject = ulak.gameObject;
        ulakObject.SetActive(false);
        ulak.SetPositionAndRotation(ulakPosition.position, ulakPosition.rotation);

        Rigidbody body = ulak.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = ulakPosition.position;
            body.rotation = ulakPosition.rotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        ulakObject.SetActive(true);
    }

    private void HandleRoundWinner(int riderId, bool isPlayer)
    {
        if (roundEnded || roomState != RoomState.GameStarted)
            return;

        roundEnded = true;
        StopUloqCrowdWeatherMonitoring();
        bool comboCompleted = isPlayer && KopkariMainUI.Instance != null &&
                              KopkariMainUI.Instance.TryCompleteCombo();
        KopkariResultsManager.Instance?.AwardRoundPrize(
            riderId,
            CurrentRound != null ? CurrentRound.CoinAmount : 0,
            CurrentRound != null ? CurrentRound.NyufiyAmount : 0);
        if (comboCompleted)
            KopkariResultsManager.Instance?.AwardComboPrize(riderId, CurrentComboPrize);

        completedRoundCount++;
        KopkariMainUI.Instance?.UpdateRoundProgress(completedRoundCount, TotalRoundCount);
        roomState = RoomState.GameFinished;
        timeFinishedHandled = true;
        IsRoundWarmupActive = false;
        KopkariMainUI.Instance?.SetMobileCanvasVisible(false);
        KopkariMainUI.Instance?.HidePickupForRoundTransition();

        EndLocalPlayerGrip();
        if (isPlayer)
            TriggerEvent();
        else if (currentGoatOwner != null)
            NotifyGoatOwner(currentGoatOwner, false);

        // Every AI is stopped by TargetReachEvent.OnRoundEnded. Stop the local
        // horse too, regardless of which rider won the round.
        if (horseAnimal != null)
        {
            horseAnimal.StopMoving();
            horseAnimal.Reset_Movement();
            horseAnimal.Speed_CurrentIndex_Set(0);
        }

        // Stop every AI before selecting/moving the next round markers. The
        // TargetReachEvent round-ended callback is invoked after this handler,
        // so relying on it leaves the current AI target alive while the marker
        // transforms are being repositioned.
        CacheRoundRiders();
        for (int i = 0; i < roundRiders.Count; i++)
            roundRiders[i]?.StopForRoundEnd();

        nextRoundPrepared = SelectUnusedRound();
        if (!nextRoundPrepared)
        {
            warmupTrigger?.Deactivate();
            if (pickableObj != null)
                pickableObj.gameObject.SetActive(false);

            if (KopkariMainUI.Instance != null)
                KopkariMainUI.Instance.ShowResult();
            else
                FinishMatch();
            return;
        }

        if (roundTransitionCoroutine != null)
            StopCoroutine(roundTransitionCoroutine);
        roundTransitionCoroutine = StartCoroutine(
            OfferNextRoundPopup(winnerNeighHoldDuration, null));
    }

    public void FinishMatch()
    {
        StopUloqCrowdWeatherMonitoring();
        nextRoundPrepared = false;
        IsRoundWarmupActive = false;
        roundEnded = true;
        roomState = RoomState.GameFinished;
        warmupTrigger?.Deactivate();
        EndLocalPlayerGrip();
        KopkariMainUI.Instance?.HidePickupForRoundTransition();

        if (horseAnimal != null)
        {
            horseAnimal.StopMoving();
            horseAnimal.Reset_Movement();
            horseAnimal.Speed_CurrentIndex_Set(0);
        }

        CacheRoundRiders();
        for (int i = 0; i < roundRiders.Count; i++)
            roundRiders[i]?.StopForRoundEnd();

        if (roundTransitionCoroutine != null)
        {
            StopCoroutine(roundTransitionCoroutine);
            roundTransitionCoroutine = null;
        }

        KopkariResultsManager.Instance?.EndRace();
    }

    private IEnumerator OfferNextRoundPopup(float delay, string details)
    {
        yield return null;
        if (pickableObj != null)
            pickableObj.gameObject.SetActive(false);
        else if (myLamb != null)
            myLamb.SetActive(false);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        KopkariMainUI.Instance?.ShowRoundChange(details);
        roundTransitionCoroutine = null;
    }

    private void HandleRoundTimeExpired()
    {
        if (timeFinishedHandled || roundEnded || roomState != RoomState.GameStarted)
            return;

        timeFinishedHandled = true;
        roundEnded = true;
        StopUloqCrowdWeatherMonitoring();
        roomState = RoomState.GameFinished;
        mainTime = 0f;
        IsRoundWarmupActive = false;
        KopkariMainUI.Instance?.UpdateMainTime(0f);
        KopkariMainUI.Instance?.SetMobileCanvasVisible(false);
        KopkariMainUI.Instance?.HideCombo();
        KopkariMainUI.Instance?.HidePickupForRoundTransition();

        KopkariResultsManager.Instance?.EndRoundWithoutWinner();

        // A timed-out round has no winner. Drop the current Ulak through the
        // existing Malbers path, then freeze the complete field for the popup.
        TriggerEvent();
        EndLocalPlayerGrip();

        if (horseAnimal != null)
        {
            horseAnimal.StopMoving();
            horseAnimal.Reset_Movement();
            horseAnimal.Speed_CurrentIndex_Set(0);
        }

        CacheRoundRiders();
        for (int i = 0; i < roundRiders.Count; i++)
            roundRiders[i]?.StopForRoundEnd();

        completedRoundCount++;
        KopkariMainUI.Instance?.UpdateRoundProgress(completedRoundCount, TotalRoundCount);
        nextRoundPrepared = SelectUnusedRound();
        if (!nextRoundPrepared)
        {
            warmupTrigger?.Deactivate();
            if (pickableObj != null)
                pickableObj.gameObject.SetActive(false);

            if (KopkariMainUI.Instance != null)
                KopkariMainUI.Instance.ShowResult();
            else
                FinishMatch();
            return;
        }

        if (roundTransitionCoroutine != null)
            StopCoroutine(roundTransitionCoroutine);
        roundTransitionCoroutine = StartCoroutine(OfferNextRoundPopup(
            0f,
            "Round time is over. Continue to the next round."));
    }

    public void BeginNextRoundWarmup()
    {
        if (!nextRoundPrepared || !roundEnded || CurrentWarmupPosition == null ||
            roundTransitionCoroutine != null)
            return;

        // The round winner only prepares the next points. Movement begins when
        // the player explicitly accepts the next round, so no rider can leave
        // while the round-change popup is still being shown.
        IsRoundWarmupActive = true;
        localPlayerWarmupQualified = false;
        warmupTrigger?.Prepare(CurrentWarmupPosition, this);
        CacheRoundRiders();
        DispatchAIRidersToRoundWarmup();

        // The local player rides to warmup manually. Restore normal movement
        // after the round-finish freeze; only the trigger qualifies the player.
        localPlayerSprinting = false;
        ApplyLocalPlayerSpeed(false);
        horseAnimal?.Reset_Movement();

        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HidePickupForRoundTransition();
        KopkariMainUI.Instance?.SetMobileCanvasVisible(true);
        roundTransitionCoroutine = StartCoroutine(NextRoundWarmupRoutine());
    }

    private void DispatchAIRidersToRoundWarmup()
    {
        for (int i = 0; i < roundRiders.Count; i++)
        {
            AIKopkariRider rider = roundRiders[i];
            if (rider != null && !rider.IsEliminatedFromRounds)
                rider.BeginRoundWarmupMovement();
        }
    }

    private IEnumerator NextRoundWarmupRoutine()
    {
        localPlayerWarmupQualified = IsLocalPlayerAtWarmup();

        float deadline = Time.unscaledTime + Mathf.Max(1f, CurrentWarmupDuration);
        int displayedValue = -1;

        while (Time.unscaledTime < deadline)
        {
            localPlayerWarmupQualified |= IsLocalPlayerAtWarmup();
            if (localPlayerWarmupQualified)
                break;

            int nextValue = Mathf.Max(1, Mathf.CeilToInt(deadline - Time.unscaledTime));
            if (nextValue != displayedValue)
            {
                displayedValue = nextValue;
                KopkariMainUI.Instance?.ShowRoundWarmupCountdown(displayedValue);
            }

            yield return null;
        }

        localPlayerWarmupQualified |= IsLocalPlayerAtWarmup();
        if (!localPlayerWarmupQualified && Time.unscaledTime >= deadline)
        {
            KopkariMainUI.Instance?.ShowRoundWarmupCountdown(0);
            yield return null;
        }

        if (!localPlayerWarmupQualified)
        {
            FinishMatchAfterWarmupFailure();
            yield break;
        }

        // Hide the warmup trigger and particle as soon as the player enters it.
        // The following countdown is the independent three-second round start.
        IsRoundWarmupActive = false;
        warmupTrigger?.Deactivate();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();
        yield return null;

        float startDeadline = Time.unscaledTime + Mathf.Max(0f, roundStartCountdown);
        displayedValue = -1;
        while (Time.unscaledTime < startDeadline)
        {
            int nextValue = Mathf.Max(1, Mathf.CeilToInt(startDeadline - Time.unscaledTime));
            if (nextValue != displayedValue)
            {
                displayedValue = nextValue;
                KopkariMainUI.Instance?.ShowRoundWarmupCountdown(displayedValue);
            }
            yield return null;
        }

        KopkariMainUI.Instance?.HideRoundWarmupCountdown();
        roundTransitionCoroutine = null;
        StartGame();
    }

    private void FinishMatchAfterWarmupFailure()
    {
        StopUloqCrowdWeatherMonitoring();
        roomState = RoomState.GameFinished;
        roundEnded = true;
        timeFinishedHandled = true;
        nextRoundPrepared = false;
        IsRoundWarmupActive = false;
        roundTransitionCoroutine = null;

        warmupTrigger?.Deactivate();
        if (pickableObj != null)
            pickableObj.gameObject.SetActive(false);

        EndLocalPlayerGrip();
        if (horseAnimal != null)
        {
            horseAnimal.StopMoving();
            horseAnimal.Reset_Movement();
            horseAnimal.Speed_CurrentIndex_Set(0);
        }

        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();
        KopkariMainUI.Instance?.SetMobileCanvasVisible(false);
        KopkariMainUI.Instance?.HidePickupForRoundTransition();

        CacheRoundRiders();
        for (int i = 0; i < roundRiders.Count; i++)
            roundRiders[i]?.StopForRoundEnd();

        if (KopkariMainUI.Instance != null)
            KopkariMainUI.Instance.ShowResult();
        else
            FinishMatch();
    }

    private bool IsLocalPlayerAtWarmup()
    {
        // A fresh player collider entry is the only warmup qualification.
        return warmupTrigger != null && warmupTrigger.LocalPlayerEntered;
    }

    private void CacheRoundRiders()
    {
        roundRiders.Clear();
        AIKopkariRider[] found = FindObjectsOfType<AIKopkariRider>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !found[i].IsEliminatedFromRounds)
                roundRiders.Add(found[i]);
        }
    }

    public void StartGame()
    {
        if (roomState == RoomState.GameStarted)
            return;

        if (!EnsureInitialRoundSelected())
        {
            Debug.LogError($"[{nameof(KopkariManager)}] No valid Round Points entry is available.", this);
            return;
        }

        PrepareCurrentRoundForGameplay();
        IsRoundWarmupActive = false;
        warmupTrigger?.Deactivate();
        nextRoundPrepared = false;
        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();

        roundEnded = false;
        timeFinishedHandled = false;
        Booster.ResetWalkZoneDamagedTime();
        localPlayerSprinting = false;
        ApplyLocalPlayerSpeed(false);

        foreach (var cp in checkpoints)
            cp?.ResetPassed();

        OnResetTarget?.Invoke();
        OnGameStartFinishState?.Invoke(true);

        if (horseAnimal != null)
            OnHorseTransform?.Invoke(horseAnimal.transform);

        StartMainGame();

        if (speechBubble != null)
            speechBubble.ShowPopup(LanguageManager.Instance?.GetText(508));
        UserId = PlayerPrefs.GetInt(Constants.Player.Userid, 0);
    }

    public void ContinueGame()
    {
        // kerak bo‘lsa implement qilasan
    }

    public void RegisterLocalRider(GameObject riderRoot)
    {
        LocalRiderRoot = riderRoot;
        BindLocalPlayerAttackDamage(FindLocalPlayerBoosters());
        BindLocalPlayerGrip();
    }

    private BoostersContainer FindLocalPlayerBoosters()
    {
        if (horseAnimal != null)
        {
            BoostersContainer fromHorse = horseAnimal.GetComponentInParent<BoostersContainer>();
            if (fromHorse != null)
                return fromHorse;

            fromHorse = horseAnimal.transform.root.GetComponentInChildren<BoostersContainer>(true);
            if (fromHorse != null)
                return fromHorse;
        }

        return LocalRiderRoot != null
            ? LocalRiderRoot.GetComponentInChildren<BoostersContainer>(true)
            : null;
    }

    private void BindLocalPlayerAttackDamage(BoostersContainer source)
    {
        if (localPlayerBoosters == source)
        {
            if (localPlayerBoosters != null)
            {
                localPlayerBoosters.OnNpcAttackDamageReceived -= HandleLocalPlayerAttackDamageReceived;
                localPlayerBoosters.OnNpcAttackDamageReceived += HandleLocalPlayerAttackDamageReceived;
            }
            return;
        }

        UnbindLocalPlayerAttackDamage();
        localPlayerBoosters = source;
        if (localPlayerBoosters != null)
            localPlayerBoosters.OnNpcAttackDamageReceived += HandleLocalPlayerAttackDamageReceived;
    }

    private void UnbindLocalPlayerAttackDamage()
    {
        if (localPlayerBoosters == null)
            return;

        localPlayerBoosters.OnNpcAttackDamageReceived -= HandleLocalPlayerAttackDamageReceived;
        localPlayerBoosters = null;
    }

    private void HandleLocalPlayerAttackDamageReceived()
    {
        if (!IsLocalPlayerCurrentCarrier || localPlayerGrip == null)
            return;

        GameObject damager = localPlayerBoosters != null ? localPlayerBoosters.LastAttackDamager : null;
        AIKopkariRider attackingRider = ResolveAttackingAIRider(damager);

        if (attackingRider != null && attackingRider.GameplayUlakRole == AIKopkariRider.UlakRole.Guard)
        {
            KopkariCarrierGrip.DamageSource source = AIKopkariRider.IsCarrierEngagedByGuard(currentGoatOwner)
                ? KopkariCarrierGrip.DamageSource.GuardRiderMelee
                : KopkariCarrierGrip.DamageSource.GuardHorseAttack;
            localPlayerGrip.ApplyDamage(source, attackingRider.gameObject);
        }
        else if (attackingRider != null && attackingRider.IsMainRival)
        {
            localPlayerGrip.ApplyDamage(KopkariCarrierGrip.DamageSource.MainRivalSideAttack,
                attackingRider.gameObject);
        }
        else if (attackingRider != null)
        {
            localPlayerGrip.ApplyDamage(KopkariCarrierGrip.DamageSource.OtherRiderContact,
                attackingRider.gameObject);
        }
        else if (AIKopkariRider.IsCarrierEngagedByGuard(currentGoatOwner))
        {
            // Supports older Guard hitboxes whose Malbers Owner is not configured.
            localPlayerGrip.ApplyDamage(KopkariCarrierGrip.DamageSource.GuardRiderMelee, damager);
        }
    }

    private bool IsLocalPlayerCurrentCarrier => roomState == RoomState.GameStarted &&
                                                currentGoatOwner != null &&
                                                IsLocalRiderTransform(currentGoatOwner.transform);

    private void BindLocalPlayerGrip()
    {
        GameObject host = LocalRiderRoot;
        if (host == null && LocalRiderAnimal != null)
            host = LocalRiderAnimal.transform.root.gameObject;
        if (host == null && horseAnimal != null)
            host = horseAnimal.transform.root.gameObject;
        if (host == null)
            return;

        KopkariCarrierGrip resolvedGrip = localPlayerGrip;
        if (resolvedGrip == null || !IsSameHierarchy(resolvedGrip.transform, host.transform))
            resolvedGrip = host.GetComponentInChildren<KopkariCarrierGrip>(true);
        if (resolvedGrip == null)
            resolvedGrip = host.AddComponent<KopkariCarrierGrip>();

        if (localPlayerGrip != resolvedGrip)
        {
            UnbindLocalPlayerGrip();
            localPlayerGrip = resolvedGrip;
        }

        localPlayerGrip.GripChanged -= HandleLocalPlayerGripChanged;
        localPlayerGrip.GripChanged += HandleLocalPlayerGripChanged;
        localPlayerGrip.GripDepleted -= HandleLocalPlayerGripDepleted;
        localPlayerGrip.GripDepleted += HandleLocalPlayerGripDepleted;
    }

    private void UnbindLocalPlayerGrip()
    {
        if (localPlayerGrip == null)
            return;

        localPlayerGrip.GripChanged -= HandleLocalPlayerGripChanged;
        localPlayerGrip.GripDepleted -= HandleLocalPlayerGripDepleted;
    }

    private void BeginLocalPlayerGrip(GameObject ownerRoot)
    {
        ApplyLocalPlayerSpeed(true);
        BindLocalPlayerGrip();
        if (localPlayerGrip == null)
            return;

        lastLocalPlayerGripValue = localPlayerGrip.MaximumGrip;
        localPlayerGrip.BeginHold(ownerRoot);
        KopkariMainUI.Instance?.ShowCarrierGrip(localPlayerGrip.CurrentGrip, localPlayerGrip.MaximumGrip);
        StartLocalPlayerGripContactMonitoring();
    }

    private void EndLocalPlayerGrip()
    {
        bool wasLocalCarrier = (localPlayerGrip != null && localPlayerGrip.IsHolding) ||
                               (currentGoatOwner != null &&
                                IsLocalRiderTransform(currentGoatOwner.transform));
        StopLocalPlayerGripContactMonitoring();
        if (localPlayerGrip != null)
            localPlayerGrip.EndHold();
        lastLocalPlayerGripValue = 0f;
        ApplyLocalPlayerSpeed(false);

        bool anotherCarrierIsDisplayed = currentGoatOwner != null &&
                                         !IsLocalRiderTransform(currentGoatOwner.transform);
        if (!anotherCarrierIsDisplayed)
            KopkariMainUI.Instance?.HideCarrierGrip();
        if (wasLocalCarrier)
            OnGoatPicked?.Invoke(false);
    }

    private void HandleLocalPlayerGripChanged(float currentGrip, float maximumGrip)
    {
        if (!IsLocalPlayerCurrentCarrier)
            return;

        bool gripWasLost = currentGrip < lastLocalPlayerGripValue - 0.001f;
        lastLocalPlayerGripValue = currentGrip;

        KopkariMainUI mainUI = KopkariMainUI.Instance;
        mainUI?.ShowCarrierGrip(currentGrip, maximumGrip);
        if (gripWasLost)
            mainUI?.PlayLocalCarrierGripLossFeedback(currentGrip <= 0.001f);
    }

    private void HandleLocalPlayerGripDepleted()
    {
        if (IsLocalPlayerCurrentCarrier)
            TriggerEvent();
    }

    private void HandleLocalPlayerWalkZoneDamaged(bool damaged)
    {
        if (damaged && IsLocalPlayerCurrentCarrier && localPlayerGrip != null)
            localPlayerGrip.ApplyDamage(KopkariCarrierGrip.DamageSource.WalkTrap);
    }

    public bool ApplyTrapSetterContactDamage(GameObject trapSetterRoot)
    {
        return IsLocalPlayerCurrentCarrier && localPlayerGrip != null &&
               localPlayerGrip.ApplyDamage(
                   KopkariCarrierGrip.DamageSource.TrapSetterContact,
                   trapSetterRoot);
    }

    public bool ApplyGuardContactDamage(GameObject guardRoot)
    {
        return IsLocalPlayerCurrentCarrier && localPlayerGrip != null &&
               localPlayerGrip.ApplyDamage(
                   KopkariCarrierGrip.DamageSource.GuardContact,
                   guardRoot);
    }

    private void StartLocalPlayerGripContactMonitoring()
    {
        StopLocalPlayerGripContactMonitoring();
        if (isActiveAndEnabled)
            localPlayerGripContactCoroutine = StartCoroutine(LocalPlayerGripContactRoutine());
    }

    private void StopLocalPlayerGripContactMonitoring()
    {
        if (localPlayerGripContactCoroutine == null)
            return;

        StopCoroutine(localPlayerGripContactCoroutine);
        localPlayerGripContactCoroutine = null;
    }

    private IEnumerator LocalPlayerGripContactRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.6f, localPlayerGripContactInterval));
        while (IsLocalPlayerCurrentCarrier && localPlayerGrip != null && localPlayerGrip.IsHolding)
        {
            yield return wait;
            CheckLocalPlayerCarrierContacts();
        }

        localPlayerGripContactCoroutine = null;
    }

    private void CheckLocalPlayerCarrierContacts()
    {
        if (!IsLocalPlayerCurrentCarrier || localPlayerGrip == null || localPlayerGrip.IsProtected)
            return;

        Transform contactOrigin = horseAnimal != null
            ? horseAnimal.transform
            : (LocalRiderAnimal != null ? LocalRiderAnimal.transform : LocalRiderRoot?.transform);
        if (contactOrigin == null)
            return;

        Vector3 center = contactOrigin.TransformPoint(localPlayerGripContactOffset);
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            Mathf.Max(0.25f, localPlayerGripContactRadius),
            localPlayerGripContactBuffer,
            localPlayerGripContactLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = localPlayerGripContactBuffer[i];
            localPlayerGripContactBuffer[i] = null;
            AIKopkariRider rider = ResolveAttackingAIRider(hit != null ? hit.gameObject : null);
            if (rider == null)
                continue;

            // Trap Setter contact is handled by its low-frequency role routine,
            // which applies one hit per approach instead of repeated overlap hits.
            if (rider.GameplayUlakRole == AIKopkariRider.UlakRole.TrapSetter)
                continue;

            KopkariCarrierGrip.DamageSource source;
            if (rider.GameplayUlakRole == AIKopkariRider.UlakRole.Guard)
                source = KopkariCarrierGrip.DamageSource.GuardContact;
            else if (rider.IsMainRival)
                source = KopkariCarrierGrip.DamageSource.MainRivalSideAttack;
            else
                source = KopkariCarrierGrip.DamageSource.OtherRiderContact;

            if (localPlayerGrip.ApplyDamage(source, rider.gameObject))
                return;
        }

        // Collider-independent fallback for horses whose optional body colliders
        // were disabled during scene cleanup. Sampled only every 0.6 seconds.
        if (AIKopkariRider.TryGetNearestActiveRider(
                contactOrigin.position,
                Mathf.Max(2f, localPlayerGripContactRadius),
                null,
                out AIKopkariRider nearbyRider))
        {
            KopkariCarrierGrip.DamageSource fallbackSource;
            if (nearbyRider.GameplayUlakRole == AIKopkariRider.UlakRole.Guard)
                fallbackSource = KopkariCarrierGrip.DamageSource.GuardContact;
            else if (nearbyRider.GameplayUlakRole == AIKopkariRider.UlakRole.TrapSetter)
                fallbackSource = KopkariCarrierGrip.DamageSource.TrapSetterContact;
            else if (nearbyRider.IsMainRival)
                fallbackSource = KopkariCarrierGrip.DamageSource.MainRivalSideAttack;
            else
                fallbackSource = KopkariCarrierGrip.DamageSource.OtherRiderContact;

            localPlayerGrip.ApplyDamage(fallbackSource, nearbyRider.gameObject);
        }
    }

    private static AIKopkariRider ResolveAttackingAIRider(GameObject source)
    {
        if (source == null)
            return null;

        AIKopkariRider rider = source.GetComponentInParent<AIKopkariRider>();
        if (rider == null)
            rider = source.transform.root.GetComponentInChildren<AIKopkariRider>(true);
        return rider;
    }

    #region Camera Section
    public void CameraBackState(bool state)
    {
        if (state) LookBack();
        else MainCam();
    }

    public void LookBack()
    {
        if (mainCam == null) return;
        if (horseAnimal != null) horseAnimal.UseCameraInput = false;

        mainCam.SetCameraDistance(backDistance);
        mainCam.AddVerticalOffset(backOffsetY);
        mainCam.SetLookBackMode(true);
    }

    public void MainCam()
    {
        if (mainCam == null) return;

        mainCam.SetCameraDistance(frontDistance);
        mainCam.AddVerticalOffset(0f);
        mainCam.SetLookBackMode(false);

        StartCoroutine(EnableHorseInputDelayed());
    }

    private IEnumerator EnableHorseInputDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        if (horseAnimal != null) horseAnimal.UseCameraInput = true;
    }

    private void SprintCameraEnable()
    {
        if (sprintCam != null) sprintCam.SetPriority(true);
    }

    private void SprintCameraDisable()
    {
        if (sprintCam != null) sprintCam.SetPriority(false);
    }
    #endregion

    #region Horse Speed
    private void ApplyLocalPlayerSpeed(bool isCarrier)
    {
        KopkariRiderSpeedController.ApplyPlayer(horseAnimal, isCarrier, localPlayerSprinting);
    }

    private void HorseSprint()
    {
        if (horseAnimal == null) return;
        localPlayerSprinting = true;
        ApplyLocalPlayerSpeed(IsLocalPlayerCurrentCarrier);
        SprintCameraEnable();
    }

    private void HorseDefaultSpeed()
    {
        if (horseAnimal == null) return;
        localPlayerSprinting = false;
        ApplyLocalPlayerSpeed(IsLocalPlayerCurrentCarrier);
        SprintCameraDisable();
    }
    #endregion

    #region Game Over Actions
    private void GameOverAction()
    {
        if (timeFinishedHandled) return;
        timeFinishedHandled = true;

        OnTimeFinished?.Invoke();     // NPC, boshqa riderlar shu eventni ushlaydi

        //HandleLose();
    }
    public void OffsideAction()
    {
        gameOverTypes = GameOverTypes.Offside;
        GameOverAction();
    }
    #endregion

    #region Horse Statistics(Damages)

    public void SetPenaltyTime(float time)
    {
        webSnareDamageTime = webSnareDamageTime + time;
    }
    public float GetWebSnareDamageTime()
    {
        return webSnareDamageTime;
    }
    public float GetBoostTime()
    {
        return boostTime;
    }
    public void SetBoostTime(float time)
    {
        boostTime = boostTime + time;
    }
    #endregion
    // Scene navigation hooks
    public void MoveLobby()
    {
        SceneLoadManager.Instance?.LoadScene(SceneLoadManager.SceneType.Home);
    }

    public void BackMessage()
    {
        if (modalWindowPopup == null) return;

        string title = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(280) : string.Empty;
        string desc = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(281) : string.Empty;
        string confirm = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(1) : string.Empty;
        string cancel = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(2) : string.Empty;

        modalWindowPopup.UpdateUICustomWithButtons(title, desc, confirm, cancel);
    }

    public void SpeedShaderActive(bool state)
    {
    }
}
