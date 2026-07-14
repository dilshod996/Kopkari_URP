using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using Michsky.UI.ModernUIPack;
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
    [SerializeField, Min(0.25f)] private float warmupReachDistance = 2.5f;
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

    public GameObject currentGoatOwner;
    private GameObject lastReleasedGoatOwner;
    private float lastGoatOwnerReleaseTime = float.NegativeInfinity;
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
    public IReadOnlyList<CheckpointTrigger> Checkpoints => checkpoints;

    [SerializeField] private bool isCatched;
    public float lampCatchTime = 30f;
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

    #region Horse and Player Data
    [SerializeField] private int defaultSpeedIndex = 5;
    [SerializeField] private int boostSpeedIndex = 6;
    #endregion

    #region Events
    public static Action<bool> OnGameStartFinishState;
    public static Action OnGameStarted;
    public static Action OnMainGameStarted;
    public static Action<float> OnGoatPickedTime;
    public static Action OnResetTarget;
    public static Action<bool> OnGoatPicked;
    public static Action<GameObject> OnGoatOwnerChanged;
    public static Action OnTimeFinished;
    public static Action OnSceneReady;
    public static bool IsSceneReady { get; private set; }
    #endregion

    [Header("Checkpoints")]
    [SerializeField] private List<CheckpointTrigger> checkpoints = new List<CheckpointTrigger>();
    private int passedCheckpointCount = 0;
    // Reserved for a future rules update. Current rounds do not process them.
    private bool CheckpointsEnabled => false;

    [Header("Local Player")]
    [SerializeField] private GameObject LocalRiderRoot;
    private Coroutine pickUpTimerCoroutine;
    private BoostersContainer localPlayerBoosters;

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
        Application.targetFrameRate = 60;
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
        BindLocalPlayerAttackDamage(FindLocalPlayerBoosters());
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
        UnbindLocalPlayerAttackDamage();

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
        }
        else
        {
            KopkariMainUI.Instance?.UpdateMainTime(0f);
            roomState = RoomState.TimeFinished;
            gameOverTypes = GameOverTypes.ByTime;
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
        BindLocalPlayerAttackDamage(FindLocalPlayerBoosters());

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
        if (state && target != null && target.activeSelf) return;

        target?.SetActive(state);
    }



    #region Pick Up Uloq
    public void StartPickUpTime()
    {
        if (pickUpTimerCoroutine != null)
        {
            StopCoroutine(pickUpTimerCoroutine);
            pickUpTimerCoroutine = null;
        }

        IsCatched = true;
        lampCatchTime = 30f;

        OnGoatPickedTime?.Invoke(lampCatchTime);
        pickUpTimerCoroutine = StartCoroutine(PickUpTimerRoutine());
    }

    private IEnumerator PickUpTimerRoutine()
    {
        float t = lampCatchTime;

        while (t > 0f && IsCatched)
        {
            yield return new WaitForSeconds(1f);
            t--;
            OnGoatPickedTime?.Invoke(t);
        }

        if (t <= 0f && IsCatched)
        {
            TriggerEvent();
        }

        StopPickUpTime();
    }

    private void OnUloqPicked(GameObject pickerObj)
    {
        if (pickerObj == null)
            return;

        NotifyGoatOwner(pickerObj, true);
        StartCoroutine(NotifyRoom());
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
            if (previousOwner != null && previousOwner == LocalRiderRoot)
                KopkariMainUI.Instance?.HideCombo();
            if (TryGetRiderId(ownerRoot, out int newOwnerId))
                KopkariResultsManager.Instance?.OnLambPicked(newOwnerId, takenFromCarrier);

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
                StartPickUpTime();
                if (roomState == RoomState.GameStarted)
                    KopkariMainUI.Instance?.ShowCombo();

            }
            else
            {
                if (IsCatched)
                    StopPickUpTime();

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
                {
                    StopPickUpTime();
                    KopkariMainUI.Instance?.HideCombo();
                }

                lastReleasedGoatOwner = ownerRoot;
                lastGoatOwnerReleaseTime = Time.time;
                SetCurrentGoatOwner(null);
                FinalPosState(false);
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
        OnGoatOwnerChanged?.Invoke(currentGoatOwner);
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

    public void StopPickUpTime()
    {
        IsCatched = false;

        if (pickUpTimerCoroutine != null)
        {
            StopCoroutine(pickUpTimerCoroutine);
            pickUpTimerCoroutine = null;
        }

        OnGoatPickedTime?.Invoke(0f);
        OnGoatPicked?.Invoke(false);

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
        return true;
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

        Transform targetPosition = CurrentTargetPosition;
        if (targetObject != null)
        {
            targetObject.SetActive(false);
            if (targetPosition != null)
                targetObject.transform.SetPositionAndRotation(targetPosition.position, targetPosition.rotation);
        }

        Transform ulak = UlakTransform;
        Transform ulakPosition = CurrentUlakPosition;
        if (ulakPosition == null)
            return;

        if (ulakBottomObject != null)
            ulakBottomObject.SetPositionAndRotation(ulakPosition.position, ulakPosition.rotation);

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
        KopkariMainUI.Instance?.SetMobileCanvasVisible(false);

        StopPickUpTime();
        if (isPlayer)
            TriggerEvent();
        else if (currentGoatOwner != null)
            NotifyGoatOwner(currentGoatOwner, false);

        if (targetObject != null)
            targetObject.SetActive(false);

        if (isPlayer && horseAnimal != null)
        {
            horseAnimal.StopMoving();
            horseAnimal.Reset_Movement();
            horseAnimal.State_Activate(StateEnum.Jump);
        }

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

        warmupTrigger?.Prepare(CurrentWarmupPosition, this);

        if (roundTransitionCoroutine != null)
            StopCoroutine(roundTransitionCoroutine);
        roundTransitionCoroutine = StartCoroutine(OfferNextRoundAfterWinner());
    }

    public void FinishMatch()
    {
        nextRoundPrepared = false;
        roundEnded = true;
        roomState = RoomState.GameFinished;
        warmupTrigger?.Deactivate();

        if (roundTransitionCoroutine != null)
        {
            StopCoroutine(roundTransitionCoroutine);
            roundTransitionCoroutine = null;
        }

        KopkariResultsManager.Instance?.EndRace();
    }

    private IEnumerator OfferNextRoundAfterWinner()
    {
        yield return null;
        if (pickableObj != null)
            pickableObj.gameObject.SetActive(false);
        else if (myLamb != null)
            myLamb.SetActive(false);

        if (winnerNeighHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(winnerNeighHoldDuration);

        KopkariMainUI.Instance?.ShowRoundChange();
        roundTransitionCoroutine = null;
    }

    public void BeginNextRoundWarmup()
    {
        if (!nextRoundPrepared || !roundEnded || CurrentWarmupPosition == null ||
            roundTransitionCoroutine != null)
            return;

        // The round winner only prepares the next points. Movement begins when
        // the player explicitly accepts the next round, so no rider can leave
        // while the round-change popup is still being shown.
        warmupTrigger?.Prepare(CurrentWarmupPosition, this);
        CacheRoundRiders();
        DispatchAIRidersToRoundWarmup();

        KopkariMainUI.Instance?.HideRoundChange();
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
        CacheRoundRiders();
        RefreshAIRiderWarmupQualification();
        localPlayerWarmupQualified = IsLocalPlayerAtWarmup();

        float deadline = Time.unscaledTime + Mathf.Max(1f, CurrentWarmupDuration);
        int displayedValue = -1;

        while (Time.unscaledTime < deadline)
        {
            localPlayerWarmupQualified |= IsLocalPlayerAtWarmup();
            if (localPlayerWarmupQualified && AreAllActiveAIRidersWarmupQualified())
                break;

            int nextValue = Mathf.Max(1, Mathf.CeilToInt(deadline - Time.unscaledTime));
            if (nextValue != displayedValue)
            {
                displayedValue = nextValue;
                RefreshAIRiderWarmupQualification();
                KopkariMainUI.Instance?.ShowRoundWarmupCountdown(displayedValue);
            }

            yield return null;
        }

        localPlayerWarmupQualified |= IsLocalPlayerAtWarmup();
        RefreshAIRiderWarmupQualification();
        bool everyoneQualified = localPlayerWarmupQualified && AreAllActiveAIRidersWarmupQualified();
        if (!everyoneQualified && Time.unscaledTime >= deadline)
        {
            KopkariMainUI.Instance?.ShowRoundWarmupCountdown(0);
            yield return null;
        }
        StopUnqualifiedAIRidersForWarmup();

        if (!localPlayerWarmupQualified)
        {
            KopkariMainUI.Instance?.HideRoundWarmupCountdown();
            roomState = RoomState.PlayerEliminated;
            nextRoundPrepared = false;
            roundTransitionCoroutine = null;
            KopkariResultsManager.Instance?.EndRace();
            KopkariMainUI.Instance?.GameOverShow();
            yield break;
        }

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

    private bool IsLocalPlayerAtWarmup()
    {
        // The local player must produce a fresh trigger entry for this round.
        // Distance remains an AI fallback below, but using it for the player
        // allowed a new round to start without entering the warmup trigger.
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

    private bool AreAllActiveAIRidersWarmupQualified()
    {
        for (int i = 0; i < roundRiders.Count; i++)
        {
            AIKopkariRider rider = roundRiders[i];
            if (rider != null && !rider.IsEliminatedFromRounds && !rider.IsRoundWarmupQualified)
                return false;
        }
        return true;
    }

    private void RefreshAIRiderWarmupQualification()
    {
        Transform warmup = CurrentWarmupPosition;
        for (int i = 0; i < roundRiders.Count; i++)
        {
            AIKopkariRider rider = roundRiders[i];
            if (rider == null || rider.IsEliminatedFromRounds)
                continue;

            if (warmupTrigger != null && warmupTrigger.HasAIRider(rider))
                rider.MarkRoundWarmupQualified();
            else
                rider.RefreshRoundWarmupQualification(warmup, warmupReachDistance);
        }
    }

    private void StopUnqualifiedAIRidersForWarmup()
    {
        for (int i = 0; i < roundRiders.Count; i++)
        {
            AIKopkariRider rider = roundRiders[i];
            if (rider != null && !rider.IsEliminatedFromRounds && !rider.IsRoundWarmupQualified)
                rider.PauseAtWarmupTimeout();
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
        warmupTrigger?.Deactivate();
        nextRoundPrepared = false;
        KopkariMainUI.Instance?.HideRoundChange();
        KopkariMainUI.Instance?.HideRoundWarmupCountdown();

        roundEnded = false;
        timeFinishedHandled = false;
        Booster.ResetWalkZoneDamagedTime();

        passedCheckpointCount = 0;
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
        if (roomState != RoomState.GameStarted || currentGoatOwner == null ||
            LocalRiderRoot == null || currentGoatOwner != LocalRiderRoot ||
            !AIKopkariRider.IsCarrierEngagedByGuard(currentGoatOwner))
            return;

        TriggerEvent();
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

    #region CheckPoints
    public void OnCheckpointReached(CheckpointTrigger checkpoint, GameObject riderObj)
    {
        if (!CheckpointsEnabled) return;
        if (roomState != RoomState.GameStarted) return;

        if (!IsCatched || currentGoatOwner == null)
        {
            Debug.Log("[Checkpoint] Uloqsiz o‘tildi – hisoblanmadi");
            return;
        }

        if (riderObj == null) return;
        if (riderObj.transform.root.gameObject != currentGoatOwner)
        {
            Debug.Log("[Checkpoint] Bu riderda uloq yo‘q – hisoblanmadi");
            return;
        }

        if (checkpoint == null) return;

        if (checkpoint.IsPassedWithGoat)
        {
            Debug.Log("[Checkpoint] Bu checkpoint oldin ham uloq bilan o‘tilgan");
            return;
        }

        checkpoint.MarkPassedWithGoat();
        passedCheckpointCount++;

        KopkariMainUI.Instance?.UpdateSlider();
        KopkariResultsManager.Instance?.OnTriggerPoint(UserId);

        Debug.Log($"[Checkpoint] {riderObj.name} uloq bilan checkpoint o'tdi. Jami: {passedCheckpointCount}/{checkpoints.Count}");

        if (passedCheckpointCount >= checkpoints.Count && checkpoints.Count > 0)
            OnAllCheckpointsCompleted();
    }

    public void OnAllCheckpointsCompleted()
    {
        if (!CheckpointsEnabled) return;
        Debug.Log("[Checkpoint] 🔥 Barcha checkpointlar uloq bilan o‘tilgan!");
        FinalPosState(true);
    }
    #endregion

    #region Horse Speed
    private void HorseSprint()
    {
        if (horseAnimal == null) return;
        horseAnimal.Speed_CurrentIndex_Set(boostSpeedIndex);
        SprintCameraEnable();
    }

    private void HorseDefaultSpeed()
    {
        if (horseAnimal == null) return;
        horseAnimal.Speed_CurrentIndex_Set(defaultSpeedIndex);
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
