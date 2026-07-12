using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using MalbersAnimations;

public class AIKopkariRider : MonoBehaviour
{
    public enum UlakRole
    {
        Competitor,
        Orbit
    }

    public enum PreparationState
    {
        Idle,
        WaitingForDeparture,
        MovingThroughGate,
        MovingToStart,
        AligningAtStart,
        Ready,
        Released
    }

    [Header("NPC Info")]
    [SerializeField] private int id = 0;
    [SerializeField] private string nameNpc;
    [SerializeField] private string teamName;
    [SerializeField] private string countryName;
    [SerializeField] private Sprite flagIcon;
    [SerializeField] private string horseName;
    [SerializeField, Min(0)] private int winnings;
    [Header("Dependencies")]
    [SerializeField] private MPickUp pickUp;
    [SerializeField] private MAnimalBrain brain;
    [SerializeField] private MAnimalAIControl ai;
    [SerializeField] private ObstacleTouchSensor obstacleSensor;

    [Header("Pickup Timing")]
    [SerializeField, Min(0f)] private float pickupFocusMinDuration = 4f;
    [SerializeField, Min(0f)] private float pickupFocusMaxDuration = 5f;
    [Tooltip("Normally disabled: grip is lost from hazards instead of an automatic timer.")]
    [SerializeField] private bool useHoldTimeout;
    [SerializeField] private float itemPickedDuration = 20f; // qo‘lda ushlab turish vaqti

    [Header("Ulak Gameplay")]
    [SerializeField] private int normalGameplaySpeedIndex = 4;
    [SerializeField] private int mainRivalGameplaySpeedIndex = 5;
    [SerializeField, Min(1f)] private float orbitRadius = 7f;
    [SerializeField, Range(6, 24)] private int orbitPointCount = 12;
    [SerializeField, Min(0.1f)] private float orbitStoppingDistance = 0.8f;
    [SerializeField, Min(1f)] private float orbitPointTimeout = 8f;
    [SerializeField, Min(0.1f)] private float movingUlakStoppingDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float salymStoppingDistance = 0.7f;

    [Header("Grip Loss")]
    [SerializeField] private bool dropOnWalkZone = true;
    [SerializeField] private bool dropOnWebSnare = true;
    [SerializeField] private bool dropOnObstacle = true;

    private int currentCheckpointIndex = -1;
    public bool hasLamb = false;
    private bool timeFinishedProcessed = false;
    // ⏱ coroutinelar
    private Coroutine waitCoroutine;
    private Coroutine itemTimerCoroutine;
    private Coroutine orbitCoroutine;
    private float currentItemTime;
    private float pickupFocusElapsed;
    private float pickupFocusRequired;
    private bool pickupRequestPending;
    private bool allCheckpointsDone = false; // hamma checkpointlar uloq bilan tugaganmi?

    public bool HasLamb => hasLamb;   // faqat o‘qish uchun
    private bool[] npcPassedCheckpoints;

    [Header("Projectiles")]
    [SerializeField] private BoostersContainer boosterContainer;

    [Header("Rider-Specific Targets")]
    [FormerlySerializedAs("targetPoint")]
    [Tooltip("This rider's unique assigned position before gameplay begins.")]
    [SerializeField] private Transform startingPoint;
    [SerializeField] private float slowDuration = 5f;     // necha sekund sekin yuradi
    [SerializeField] private int slowSpeedIndex = 2;      // slow paytidagi speed index

    [Header("Pregame Presentation (Intro Only)")]
    [Tooltip("Waypoints used before the final start slot. Put the gate-pass waypoint first.")]
    [SerializeField] private Transform[] pregameRoute;
    [Tooltip("Optional camera anchor used when the intro inspects a rider that is not ready.")]
    [SerializeField] private Transform inspectionCameraPoint;
    [Tooltip("Arrival distance for intro route waypoints.")]
    [SerializeField, Min(0.1f)] private float routeStoppingDistance = 0.65f;
    [Tooltip("Distance at which a route waypoint is treated as passed and the next target is assigned without stopping.")]
    [SerializeField, Min(0.25f)] private float routePassThroughDistance = 2f;
    [Tooltip("Arrival distance for this rider's final starting slot.")]
    [SerializeField, Min(0.1f)] private float startStoppingDistance = 0.55f;
    [Tooltip("Rotation alignment time after reaching the starting slot.")]
    [SerializeField, Min(0f)] private float startAlignmentDuration = 0.35f;
    [Tooltip("Intro movement speed for normal AI riders.")]
    [SerializeField] private int canterSpeedIndex = 3;
    [Tooltip("Intro movement speed for the main rival.")]
    [SerializeField] private int rivalGallopSpeedIndex = 4;

    private bool isFinished = false;
    private Coroutine preparationCoroutine;
    private Coroutine alignmentCoroutine;
    private Coroutine presentationNeighCoroutine;
    private Transform currentPreparationTarget;
    private int preparationRouteIndex;
    private bool resultRegistered;
    private bool gateReported;
    private bool isMainRival;
    private bool isGameplayActive;
    private bool inspectionFacingHeld;
    private bool previousLockMovement;
    private bool previousRotateAtDirection;
    private Vector3 inspectionLookPosition;
    private bool hasInspectionLookPosition;
    private UlakRole ulakRole = UlakRole.Competitor;
    private int orbitSlotIndex;
    private int orbitSlotCount = 1;
    private bool hasCarrierHistory;
    private PreparationState preparationState = PreparationState.Idle;

    public static event Action<AIKopkariRider> OnRiderReady;
    public static event Action<AIKopkariRider> OnRiderPassedGate;

    public int Id => id;
    public string RiderName => nameNpc;
    public string TeamName => teamName;
    public string CountryName => countryName;
    public Sprite FlagIcon => flagIcon;
    public string HorseName => string.IsNullOrWhiteSpace(horseName) ? "Horse" : horseName;
    public int Winnings => Mathf.Max(0, winnings);
    public bool IsReadyAtStart => preparationState == PreparationState.Ready;
    public bool IsPreparing => preparationState != PreparationState.Idle &&
                               preparationState != PreparationState.Ready &&
                               preparationState != PreparationState.Released;
    public PreparationState CurrentPreparationState => preparationState;
    public Transform StartPoint => startingPoint;
    public Transform InspectionCameraPoint => inspectionCameraPoint;
    public MAnimal Animal => ai != null ? ai.animal : null;
    public UlakRole GameplayUlakRole => ulakRole;
    public float PickupFocusElapsed => pickupFocusElapsed;
    public float PickupFocusRequired => pickupFocusRequired;
    public float PickupFocusProgress01 => pickupFocusRequired > 0f
        ? Mathf.Clamp01(pickupFocusElapsed / pickupFocusRequired)
        : 0f;

    private void Awake()
    {
        if (!brain) brain = GetComponentInParent<MAnimalBrain>();
        if (!ai) ai = GetComponentInParent<MAnimalAIControl>();
        if (!pickUp) pickUp = GetComponentInChildren<MPickUp>();
        if (!boosterContainer) boosterContainer = GetComponentInParent<BoostersContainer>();
        if (!obstacleSensor) obstacleSensor = GetComponentInChildren<ObstacleTouchSensor>(true);

    }

    private void LateUpdate()
    {
        if (inspectionFacingHeld && preparationState == PreparationState.Ready)
            FaceInspectionCamera();
    }

    private void OnEnable()
    {
        KopkariManager.OnMainGameStarted += BeginGameplay;
        TargetReachEvent.OnReachedTargetWithLamb += HandleReachedTargetWithLamb;
        TargetReachEvent.OnRoundEnded += HandleFinish;
        KopkariManager.OnTimeFinished += HandleTimeFinished;
        KopkariManager.OnGoatOwnerChanged += HandleGoatOwnerChanged;
        if (ai != null)
            ai.OnTargetArrived.AddListener(HandleAiTargetArrived);
        if (pickUp != null)
        {
            pickUp.OnFocusedItem.AddListener(HandleFocusedItemChanged);
            pickUp.OnItemPicked.AddListener(HandleItemPicked);
        }
        if (boosterContainer != null)
            boosterContainer.OnNpcGripBreakDamage += HandleNpcGripBreakDamage;
        if (obstacleSensor != null)
            obstacleSensor.OnTouched += HandleObstacleTouched;
    }

    private void OnDisable()
    {
        StopPresentationNeigh();
        ReleaseInspectionFacingHold();
        KopkariManager.OnMainGameStarted -= BeginGameplay;
        TargetReachEvent.OnReachedTargetWithLamb -= HandleReachedTargetWithLamb;
        TargetReachEvent.OnRoundEnded -= HandleFinish;
        KopkariManager.OnTimeFinished -= HandleTimeFinished;
        KopkariManager.OnGoatOwnerChanged -= HandleGoatOwnerChanged;
        if (ai != null)
            ai.OnTargetArrived.RemoveListener(HandleAiTargetArrived);
        if (pickUp != null)
        {
            pickUp.OnFocusedItem.RemoveListener(HandleFocusedItemChanged);
            pickUp.OnItemPicked.RemoveListener(HandleItemPicked);
        }
        if (boosterContainer != null)
            boosterContainer.OnNpcGripBreakDamage -= HandleNpcGripBreakDamage;
        if (obstacleSensor != null)
            obstacleSensor.OnTouched -= HandleObstacleTouched;

        // xavfsizlik uchun
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        if (itemTimerCoroutine != null) StopCoroutine(itemTimerCoroutine);
        if (preparationCoroutine != null) StopCoroutine(preparationCoroutine);
        if (alignmentCoroutine != null) StopCoroutine(alignmentCoroutine);
        if (orbitCoroutine != null) StopCoroutine(orbitCoroutine);
    }

    public int GetId()
    {
        return id;
    }

    public void SetIdentity(
        string riderName,
        string riderTeam,
        string riderCountry,
        Sprite riderFlag = null,
        string riderHorseName = null,
        int riderWinnings = 0)
    {
        if (!string.IsNullOrWhiteSpace(riderName))
            nameNpc = riderName;
        if (!string.IsNullOrWhiteSpace(riderTeam))
            teamName = riderTeam;
        countryName = riderCountry ?? string.Empty;
        flagIcon = riderFlag;
        if (!string.IsNullOrWhiteSpace(riderHorseName))
            horseName = riderHorseName;
        winnings = Mathf.Max(0, riderWinnings);
    }

    public void ConfigureUlakRole(UlakRole role, int slotIndex = 0, int slotCount = 1)
    {
        ulakRole = role;
        orbitSlotIndex = Mathf.Max(0, slotIndex);
        orbitSlotCount = Mathf.Max(1, slotCount);

        if (ulakRole == UlakRole.Orbit)
            CancelPickupFocus();
    }



    #region Movers

    // 🔹 Oddiy helper: target + state ni bir joyda chaqiramiz
    private void MoveTo(Transform target)
    {
        if (!ai || !target) return;
        // Har turdagi targetga alohida stop distance
        if (target == GetCurrentUlakTransform())
        {
            ai.StoppingDistance = 0.4f;
        }
        else if (target == GetFinishPoint())
        {
            KopkariManager.Instance?.FinalPosState(true);
            ai.StoppingDistance = 0.7f;
        }
        else if(target == startingPoint)
        {
            ai.StoppingDistance = startStoppingDistance;
        }
        else if (target == GetSecondRoundWarmupPoint())
        {
            ai.StoppingDistance = 1.5f;
            MoveSecondWarmUpLocation(ai.animal);
        }
        else
        {
            // checkpointlar
            ai.StoppingDistance = 0;
        }
        ai.SetTarget(target, true); // AIControl targetga path hisoblaydi va harakatni boshlaydi
    }
    private void MoveToNextPoint()
    {
        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        Transform finish = GetFinishPoint();

        // Checkpointlar umuman bo‘lmasa → to‘g‘ri finishga bor
        if (sharedCheckpoints == null || sharedCheckpoints.Count == 0)
        {
            if (finish != null)
                MoveTo(finish);
            return;
        }

        // Agar hali boshlangan bo‘lmasa, 0 dan start qilamiz
        if (currentCheckpointIndex < 0)
            currentCheckpointIndex = 0;

        // Hali checkpointlar tugamagan bo‘lsa
        while (currentCheckpointIndex < sharedCheckpoints.Count && sharedCheckpoints[currentCheckpointIndex] == null)
            currentCheckpointIndex++;

        if (currentCheckpointIndex < sharedCheckpoints.Count)
        {
            Transform target = sharedCheckpoints[currentCheckpointIndex].transform;
            //KopkariResultsManager.Instance.OnTriggerPoint(id);
            // MUHIM: targetni olayapmiz → keyin indexni +1 qilamiz
            currentCheckpointIndex++;
            //Debug.Log("CheckPoint index: " + currentCheckpointIndex);
            MoveTo(target);
        }
        else
        {
            // Barcha checkpoint tugadi → finishga
            if (finish != null)
                MoveTo(finish);
        }
    }
    #endregion

    #region Start Events
    public void BeginPregame(bool isMainRival, float departureDelay = 0f)
    {
        if (!isActiveAndEnabled)
            return;

        if (preparationCoroutine != null)
            StopCoroutine(preparationCoroutine);
        if (alignmentCoroutine != null)
            StopCoroutine(alignmentCoroutine);

        preparationCoroutine = StartCoroutine(BeginPregameRoutine(isMainRival, departureDelay));
    }

    private IEnumerator BeginPregameRoutine(bool isMainRival, float departureDelay)
    {
        ReleaseInspectionFacingHold();
        CacheInspectionLookPosition();
        this.isMainRival = isMainRival;
        ResetGameplayState();
        preparationState = PreparationState.WaitingForDeparture;
        gateReported = false;
        currentPreparationTarget = null;
        preparationRouteIndex = 0;

        if (Animal != null)
        {
            if (Animal.CurrentSpeedSet != null)
                Animal.CurrentSpeedSet.LockSpeed = false;
            Animal.Sprint = false;

            int requestedSpeed = isMainRival ? rivalGallopSpeedIndex : canterSpeedIndex;
            Animal.Speed_CurrentIndex_Set(requestedSpeed);
            if (Animal.CurrentSpeedIndex != requestedSpeed)
            {
                Debug.LogWarning(
                    $"[{nameof(AIKopkariRider)}] {name} requested speed {requestedSpeed}, " +
                    $"but Malbers selected {Animal.CurrentSpeedIndex}. Check the active speed set.",
                    this);
            }
        }

        if (departureDelay > 0f)
            yield return new WaitForSecondsRealtime(departureDelay);

        yield return FollowPregameRoute();
        preparationCoroutine = null;
    }

    private void ResetGameplayState()
    {
        hasLamb = false;
        isFinished = false;
        timeFinishedProcessed = false;
        currentCheckpointIndex = -1;
        isGameplayActive = false;
        hasCarrierHistory = false;
        pickupRequestPending = false;
        ResetPickupFocusProgress();

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
        if (itemTimerCoroutine != null)
        {
            StopCoroutine(itemTimerCoroutine);
            itemTimerCoroutine = null;
        }
        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }
        allCheckpointsDone = false;

        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        if (sharedCheckpoints != null && sharedCheckpoints.Count > 0)
        {
            npcPassedCheckpoints = new bool[sharedCheckpoints.Count];
            for (int i = 0; i < npcPassedCheckpoints.Length; i++)
                npcPassedCheckpoints[i] = false;
        }

        if (!resultRegistered)
        {
            KopkariResultsManager.Instance?.Register(id, nameNpc, teamName);
            resultRegistered = true;
        }
    }

    private IEnumerator FollowPregameRoute()
    {
        while (pregameRoute != null && preparationRouteIndex < pregameRoute.Length)
        {
            Transform routePoint = pregameRoute[preparationRouteIndex++];
            if (routePoint == null)
                continue;

            preparationState = PreparationState.MovingThroughGate;
            MoveToPreparationTarget(routePoint, Mathf.Min(routeStoppingDistance, 0.2f));

            while (currentPreparationTarget == routePoint)
            {
                Transform horseTransform = Animal != null ? Animal.transform : transform.root;
                if (horseTransform == null)
                    break;

                Vector3 offset = routePoint.position - horseTransform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= routePassThroughDistance * routePassThroughDistance ||
                    (ai != null && ai.HasArrived))
                    break;

                yield return null;
            }

            if (!gateReported)
            {
                gateReported = true;
                OnRiderPassedGate?.Invoke(this);
            }
        }

        if (startingPoint == null)
        {
            preparationState = PreparationState.Idle;
            Debug.LogError($"[{nameof(AIKopkariRider)}] {name} has no start target.", this);
            yield break;
        }

        preparationState = PreparationState.MovingToStart;
        MoveToPreparationTarget(startingPoint, startStoppingDistance);
    }

    private void MoveToPreparationTarget(Transform target, float stoppingDistance)
    {
        if (ai == null || target == null)
            return;

        currentPreparationTarget = target;
        ai.StoppingDistance = Mathf.Max(0.1f, stoppingDistance);
        ai.SetTarget(target, true);
    }

    private void HandleAiTargetArrived(Transform arrivedTarget)
    {
        if (!IsPreparing || arrivedTarget == null || arrivedTarget != currentPreparationTarget)
            return;
        if (preparationState == PreparationState.AligningAtStart)
            return;

        if (arrivedTarget != startingPoint)
            return;

        if (alignmentCoroutine != null)
            StopCoroutine(alignmentCoroutine);
        alignmentCoroutine = StartCoroutine(AlignAtStart());
    }

    private IEnumerator AlignAtStart()
    {
        preparationState = PreparationState.AligningAtStart;
        ai?.Stop();

        Transform horseTransform = Animal != null ? Animal.transform : transform.root;
        Quaternion startRotation = horseTransform.rotation;
        Quaternion targetRotation = GetInspectionFacingRotation(horseTransform);
        float duration = Mathf.Max(0f, startAlignmentDuration);

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ApplyInspectionRotation(Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
        }

        ApplyInspectionRotation(targetRotation);
        //if (Animal != null)
        //    Animal.TeleportRot(startingPoint);
        alignmentCoroutine = null;
        MarkReady();
    }

    private void MarkReady()
    {
        if (preparationState == PreparationState.Ready)
            return;

        currentPreparationTarget = startingPoint;
        preparationState = PreparationState.Ready;
        ai?.Stop();
        HoldInspectionFacing();
        OnRiderReady?.Invoke(this);
    }

    public void FaceInspectionCamera()
    {
        Transform horseTransform = Animal != null ? Animal.transform : transform.root;
        if (horseTransform == null)
            return;

        ApplyInspectionRotation(GetInspectionFacingRotation(horseTransform));
    }

    public void PlayPresentationNeighTwice(float maximumWaitForFirstNeigh = 0.75f)
    {
        StopPresentationNeigh();
        if (!isActiveAndEnabled || Animal == null || !IsReadyAtStart || isGameplayActive)
            return;

        presentationNeighCoroutine = StartCoroutine(
            PresentationNeighRoutine(Mathf.Max(0f, maximumWaitForFirstNeigh)));
    }

    public void StopPresentationNeigh()
    {
        if (presentationNeighCoroutine == null)
            return;

        StopCoroutine(presentationNeighCoroutine);
        presentationNeighCoroutine = null;
    }

    private IEnumerator PresentationNeighRoutine(float maximumWaitForFirstNeigh)
    {
        if (maximumWaitForFirstNeigh > 0f)
            yield return new WaitForSecondsRealtime(maximumWaitForFirstNeigh);

        for (int playIndex = 0; playIndex < 2; playIndex++)
        {
            if (Animal == null || !IsReadyAtStart || isGameplayActive)
                break;

            ai?.Stop();
            Animal.StopMoving();
            Animal.Reset_Movement();
            Animal.State_Activate(StateEnum.Jump);

            if (inspectionFacingHeld)
            {
                Animal.Rotate_at_Direction = false;
                Animal.LockMovement = true;
                FaceInspectionCamera();
            }

            if (playIndex == 0)
            {
                yield return null;
                float deadline = Time.unscaledTime + 2.25f;
                while (Animal != null &&
                       Animal.ActiveStateID.ID == StateEnum.Jump &&
                       Time.unscaledTime < deadline &&
                       IsReadyAtStart &&
                       !isGameplayActive)
                {
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        presentationNeighCoroutine = null;
    }

    private Quaternion GetInspectionFacingRotation(Transform horseTransform)
    {
        if (horseTransform != null && hasInspectionLookPosition)
        {
            Vector3 cameraDirection = inspectionLookPosition - horseTransform.position;
            cameraDirection.y = 0f;
            if (cameraDirection.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(cameraDirection.normalized, Vector3.up);
        }

        if (startingPoint != null)
            return startingPoint.rotation;
        return horseTransform != null ? horseTransform.rotation : Quaternion.identity;
    }

    private void CacheInspectionLookPosition()
    {
        hasInspectionLookPosition = inspectionCameraPoint != null;
        if (hasInspectionLookPosition)
            inspectionLookPosition = inspectionCameraPoint.position;
    }

    private void ApplyInspectionRotation(Quaternion rotation)
    {
        if (Animal != null)
            Animal.Rotation = rotation;
        else if (transform.root != null)
            transform.root.rotation = rotation;
    }

    private void HoldInspectionFacing()
    {
        if (Animal == null)
        {
            FaceInspectionCamera();
            return;
        }

        if (!inspectionFacingHeld)
        {
            previousLockMovement = Animal.LockMovement;
            previousRotateAtDirection = Animal.Rotate_at_Direction;
        }

        inspectionFacingHeld = true;
        Animal.StopMoving();
        Animal.Reset_Movement();
        Animal.Rotate_at_Direction = false;
        Animal.LockMovement = true;
        FaceInspectionCamera();
    }

    private void ReleaseInspectionFacingHold()
    {
        if (!inspectionFacingHeld)
            return;

        inspectionFacingHeld = false;
        if (Animal == null)
            return;

        Animal.LockMovement = previousLockMovement;
        Animal.Rotate_at_Direction = previousRotateAtDirection;
        Animal.Reset_Movement();
    }

    public bool ForceReadyAtStart()
    {
        if (startingPoint == null || Animal == null)
            return false;

        if (preparationCoroutine != null)
            StopCoroutine(preparationCoroutine);
        if (alignmentCoroutine != null)
            StopCoroutine(alignmentCoroutine);

        Animal.TeleportRot(startingPoint);
        ai?.Stop();
        MarkReady();
        return true;
    }

    public void BeginGameplay()
    {
        if (preparationState == PreparationState.Released)
            return;

        if (!IsReadyAtStart)
        {
            Debug.LogWarning($"[{nameof(AIKopkariRider)}] {name} was asked to start before reaching its slot.", this);
            return;
        }

        StopPresentationNeigh();
        ReleaseInspectionFacingHold();
        preparationState = PreparationState.Released;
        isGameplayActive = true;
        if (Animal != null)
        {
            if (Animal.CurrentSpeedSet != null)
                Animal.CurrentSpeedSet.LockSpeed = false;
            Animal.Sprint = false;

            int requestedSpeed = isMainRival
                ? mainRivalGameplaySpeedIndex
                : normalGameplaySpeedIndex;
            Animal.Speed_CurrentIndex_Set(requestedSpeed);
            if (Animal.CurrentSpeedIndex != requestedSpeed)
            {
                Debug.LogWarning(
                    $"[{nameof(AIKopkariRider)}] {name} requested gameplay speed {requestedSpeed}, " +
                    $"but Malbers selected {Animal.CurrentSpeedIndex}. Check the active speed set.",
                    this);
            }
        }

        if (KopkariManager.Instance != null && KopkariManager.Instance.currentGoatOwner != null)
        {
            HandleGoatOwnerChanged(KopkariManager.Instance.currentGoatOwner);
        }
        else if (ulakRole == UlakRole.Orbit)
        {
            if (orbitCoroutine != null)
                StopCoroutine(orbitCoroutine);
            orbitCoroutine = StartCoroutine(OrbitUlak());
        }
        else
        {
            MoveToCurrentUlak();
        }
    }

    // Compatibility entry point for existing UnityEvents while scenes migrate.
    public void OnGameStart()
    {
        BeginGameplay();
    }
    #endregion

    #region Lamb Take Zone
    // =======================
    // 1) ULOQNI OLISh LOGIKASI
    // =======================

    /// <summary>
    /// Uloq zonaga kirganda (Trigger / Mode Event orqali) chaqiriladi
    /// </summary>
    public void OnEnterLambZone()
    {
        // allaqachon kutayotgan bo‘lsa yoki uloq bor bo‘lsa – qayta boshlama
        if (isFinished)
        {
            MoveTo(GetSecondRoundWarmupPoint());
        }
        if (!isGameplayActive || ulakRole != UlakRole.Competitor) return;
        if (hasLamb || waitCoroutine != null || pickupRequestPending) return;
        if (pickUp != null && pickUp.FocusedItem != null && !pickUp.Has_Item && waitCoroutine == null)
        {
            waitCoroutine = StartCoroutine(WaitToPickUpLamb());
        }
    }

    /// <summary>
    /// Uloq zonasidan chiqib ketganda chaqirilsa – kutishni bekor qilamiz (ixtiyoriy)
    /// </summary>
    public void OnExitLambZone()
    {
        CancelPickupFocus();
        if (ulakRole == UlakRole.Orbit) return;
        // ❗ NPC hali uloqni olmagan bo‘lsa – yana lambga qaytadi
        if (!hasLamb)
            MoveToCurrentUlak();
    }
    #endregion

    #region Timer

    // =========================
    // 2) ULOQ QO‘LDA TURISH TIMERI
    // =========================
    private void StartItemTimer()
    {
        if (!useHoldTimeout)
            return;

        if (itemTimerCoroutine != null)
            StopCoroutine(itemTimerCoroutine);

        currentItemTime = itemPickedDuration;
        itemTimerCoroutine = StartCoroutine(ItemPickedCountdown());
    }

    private void StopItemTimer()
    {
        if (itemTimerCoroutine != null)
        {
            StopCoroutine(itemTimerCoroutine);
            itemTimerCoroutine = null;
        }
    }

    private IEnumerator ItemPickedCountdown()
    {
        // 1) Random vaqtlarni faqat coroutine ichida hisoblab olamiz
        int min = 3;
        int max = Mathf.Min(14, Mathf.FloorToInt(currentItemTime) - 1);

        int rndA = -1;
        int rndB = -1;

        if (max >= min)
        {
            rndA = Random.Range(min, max + 1);
            do
            {
                rndB = Random.Range(min, max + 1);
            }
            while (rndB == rndA);
        }

        bool usedA = false;
        bool usedB = false;

        //Debug.Log($"RANDOM TIMES → A={rndA}, B={rndB}");

        // 2) Timer ishlashi
        while (currentItemTime > 0f && hasLamb)
        {
            yield return new WaitForSeconds(1f);
            currentItemTime -= 1f;

            int t = Mathf.RoundToInt(currentItemTime);

            // random A triggerri
            if (!usedA && t == rndA)
            {
                usedA = true;
                //Debug.Log($"▶ RND A TRIGGER: {t}");
                boosterContainer.DropWalkTrapNpc();
                // EVENT A
            }

            // random B triggerri
            if (!usedB && t == rndB)
            {
                usedB = true;
                //Debug.Log($"▶ RND B TRIGGER: {t}");
                boosterContainer.DropWalkTrapNpc();
                // EVENT B
            }
        }

        itemTimerCoroutine = null;

        if (currentItemTime <= 0f && hasLamb)
        {
            HandleLambTimeout();
        }
    }


    /// <summary>
    /// Uloqni ushlab turish vaqti tugaganda chaqiriladi
    /// </summary>
    private void HandleLambTimeout()
    {
        DropOwnedUlak();
    }

    private IEnumerator WaitToPickUpLamb()
    {
        if (pickUp == null || pickUp.FocusedItem == null)
        {
            waitCoroutine = null;
            ResetPickupFocusProgress();
            yield break;
        }

        Pickable focusedAtStart = pickUp.FocusedItem;
        float minimum = Mathf.Max(0f, Mathf.Min(pickupFocusMinDuration, pickupFocusMaxDuration));
        float maximum = Mathf.Max(minimum, Mathf.Max(pickupFocusMinDuration, pickupFocusMaxDuration));
        pickupFocusRequired = Random.Range(minimum, maximum);
        pickupFocusElapsed = 0f;

        while (pickupFocusElapsed < pickupFocusRequired)
        {
            if (!CanContinuePickupFocus(focusedAtStart))
            {
                waitCoroutine = null;
                ResetPickupFocusProgress();
                yield break;
            }

            pickupFocusElapsed += Time.deltaTime;
            yield return null;
        }

        if (CanContinuePickupFocus(focusedAtStart))
        {
            pickupRequestPending = true;
            pickUp.TryPickUp();
        }

        waitCoroutine = null;
        ResetPickupFocusProgress();
    }

    private bool CanContinuePickupFocus(Pickable focusedAtStart)
    {
        return isGameplayActive &&
               ulakRole == UlakRole.Competitor &&
               !hasLamb &&
               !pickupRequestPending &&
               pickUp != null &&
               !pickUp.Has_Item &&
               focusedAtStart != null &&
               focusedAtStart.CanBePicked &&
               !focusedAtStart.InCoolDown &&
               pickUp.FocusedItem == focusedAtStart;
    }

    private void HandleFocusedItemChanged(GameObject focusedObject)
    {
        if (focusedObject == null || ulakRole != UlakRole.Competitor || !isGameplayActive)
        {
            CancelPickupFocus();
            return;
        }

        if (!hasLamb && !pickupRequestPending && waitCoroutine == null && pickUp != null && !pickUp.Has_Item)
            waitCoroutine = StartCoroutine(WaitToPickUpLamb());
    }

    private void HandleItemPicked(GameObject pickedObject)
    {
        if (!isGameplayActive || ulakRole != UlakRole.Competitor || hasLamb || pickedObject == null)
            return;

        pickupRequestPending = false;
        ResetPickupFocusProgress();
        hasLamb = true;
        KopkariResultsManager.Instance?.OnLambPicked(id);
        KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, true);

        MoveOwnerToFirstSalym();

        StartItemTimer();
    }

    private void CancelPickupFocus()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        pickupRequestPending = false;
        ResetPickupFocusProgress();
    }

    private void ResetPickupFocusProgress()
    {
        pickupFocusElapsed = 0f;
        pickupFocusRequired = 0f;
    }

    private IEnumerator OrbitUlak()
    {
        if (ai == null || GetCurrentUlakTransform() == null)
        {
            orbitCoroutine = null;
            yield break;
        }

        float angle = 360f * orbitSlotIndex / Mathf.Max(1, orbitSlotCount);
        float angleStep = 360f / Mathf.Max(6, orbitPointCount);

        while (isGameplayActive && ulakRole == UlakRole.Orbit)
        {
            Transform ulak = GetCurrentUlakTransform();
            if (ulak == null)
                break;

            Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            Vector3 destination = ulak.position + radial * orbitRadius;

            ai.StoppingDistance = orbitStoppingDistance;
            ai.SetDestination(destination, true);

            float deadline = Time.time + orbitPointTimeout;
            yield return null;
            while (isGameplayActive && ulakRole == UlakRole.Orbit && !ai.HasArrived && Time.time < deadline)
                yield return null;

            angle += angleStep;
        }

        orbitCoroutine = null;
    }

    private void HandleGoatOwnerChanged(GameObject ownerRoot)
    {
        if (!isGameplayActive || isFinished)
            return;

        bool isThisRider = ownerRoot != null && ownerRoot == transform.root.gameObject;
        if (ownerRoot != null)
            hasCarrierHistory = true;

        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }

        if (isThisRider)
        {
            MoveOwnerToFirstSalym();
            return;
        }

        if (hasLamb)
        {
            hasLamb = false;
            StopItemTimer();
        }

        if (ownerRoot == null && ulakRole == UlakRole.Orbit && !hasCarrierHistory)
            orbitCoroutine = StartCoroutine(OrbitUlak());
        else
            MoveToCurrentUlak();
    }

    private void MoveOwnerToFirstSalym()
    {
        if (ai == null)
            return;

        Transform salym = KopkariManager.Instance != null
            ? KopkariManager.Instance.FirstSalymPosition
            : null;

        if (salym != null)
        {
            KopkariManager.Instance?.FinalPosState(true);
            ai.StoppingDistance = salymStoppingDistance;
            ai.SetTarget(salym, true);
        }
        else
            MoveToNextPoint();
    }

    private void MoveToCurrentUlak()
    {
        if (ai == null)
            return;

        Transform ulak = GetCurrentUlakTransform();
        if (ulak == null)
            return;

        ai.StoppingDistance = movingUlakStoppingDistance;
        ai.SetTarget(ulak, true);
    }

    private Transform GetCurrentUlakTransform()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.UlakTransform
            : null;
    }

    private Transform GetFinishPoint()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.FirstSalymPosition
            : null;
    }

    private Transform GetSecondRoundWarmupPoint()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.SecondRoundWarmupPoint
            : null;
    }

    private IReadOnlyList<CheckpointTrigger> GetSharedCheckpoints()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.Checkpoints
            : null;
    }

    private void HandleNpcGripBreakDamage(BoostersContainer.DebuffState damageType)
    {
        if (!hasLamb)
            return;

        bool shouldDrop = (damageType == BoostersContainer.DebuffState.WalkZone && dropOnWalkZone) ||
                          (damageType == BoostersContainer.DebuffState.WebSnare && dropOnWebSnare);
        if (shouldDrop)
            DropOwnedUlak();
    }

    private void HandleObstacleTouched()
    {
        if (hasLamb && dropOnObstacle)
            DropOwnedUlak();
    }

    private void DropOwnedUlak()
    {
        if (!hasLamb)
            return;

        hasLamb = false;
        CancelPickupFocus();
        StopItemTimer();

        if (pickUp != null && pickUp.Has_Item)
            pickUp.DropItem();

        KopkariResultsManager.Instance?.OnLambDropped(id);
        KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);
        MoveToCurrentUlak();
    }

    #endregion

    #region CheckPoint Finish
    // ======================
    // 3) CHECKPOINT / FINISH
    // ======================

    // 🔹 Checkpoint trigger NPC uchun
    public void OnCheckpointReached(CheckpointTrigger checkpoint)
    {
        if (!hasLamb) return;
        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        if (sharedCheckpoints == null || sharedCheckpoints.Count == 0 || npcPassedCheckpoints == null) return;
        // qaysi indexdagi checkpoint ekanini topamiz
        int idx = -1;
        for (int i = 0; i < sharedCheckpoints.Count; i++)
        {
            if (sharedCheckpoints[i] == checkpoint)
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return; // bu checkpoint bizning ro‘yxatimizda emas

        // agar allaqachon o‘tgan bo‘lsa – qayta sanamaymiz, faqat keyingisini tanlaymiz
        if (!npcPassedCheckpoints[idx])
        {
            npcPassedCheckpoints[idx] = true;
            KopkariResultsManager.Instance.OnTriggerPoint(id);
            // Debug.Log($"[NPC] Checkpoint {idx} uloq bilan O'TILDI");
        }

        // hamma checkpointlar chiqarib bo'linganmi?
        if (AreAllNpcCheckpointsPassed())
        {
            allCheckpointsDone = true;
            Transform finish = GetFinishPoint();
            if (finish != null)
                MoveTo(finish);
        }
        else
        {
            // navbatdagi bo‘sh checkpointni topib, o‘sha tomonga ketamiz
           
            currentCheckpointIndex = FindNextCheckpointIndex();
            if (currentCheckpointIndex >= 0)
                MoveToNextPoint();
            Debug.Log("Move next point:" + currentCheckpointIndex);
        }
    }



   
    // CheckpointTrigger scriptlarni Transform emas, shu orqali tekshiramiz
    private int FindNextCheckpointIndex()
    {
        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        if (sharedCheckpoints == null || sharedCheckpoints.Count == 0 || npcPassedCheckpoints == null)
            return -1;

        // Debug uchun:
        // Debug.Log("=== NPC FindNextCheckpointIndex ===");
        int count = Mathf.Min(sharedCheckpoints.Count, npcPassedCheckpoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (!npcPassedCheckpoints[i])
            {
                return i;
            }
        }

        // hammasi o'tilgan
        return -1;
    }

    private bool AreAllNpcCheckpointsPassed()
    {
        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        if (sharedCheckpoints == null || sharedCheckpoints.Count == 0 || npcPassedCheckpoints == null)
            return false;

        int count = Mathf.Min(sharedCheckpoints.Count, npcPassedCheckpoints.Length);
        if (count != sharedCheckpoints.Count)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (!npcPassedCheckpoints[i])
                return false;
        }
        return true;
    }
    #endregion

    #region Finish Points
    private void HandleReachedTargetWithLamb(int riderId, bool isPlayer)
    {
        if (!hasLamb) return;
        if (isPlayer) return;                 // faqat npc
        if (riderId != GetId()) return;       // faqat o‘zi
        hasLamb = false;
        StopItemTimer();

        // agar pickUp hali ham has_Item bo‘lsa – tashlab yuboramiz
        if (pickUp != null && pickUp.Has_Item)
        {
            pickUp.DropItem();
        }
        Transform ulak = GetCurrentUlakTransform();
        if (ulak != null)
            ulak.gameObject.SetActive(false);
        //DropLamb(); // NPCning o‘z drop metodi
        var bm = KopkariManager.Instance;
        bm.NotifyGoatOwner(transform.root.gameObject, false);
        bm.roomState = KopkariManager.RoomState.GameFinished;
        KopkariManager.OnGameStartFinishState?.Invoke(false);

        // qo‘shimcha: AI stop, celebrate anim, state reset...
    }
    private void HandleFinish()
    {
        isFinished = true;
        Transform warmupPoint = GetSecondRoundWarmupPoint();
        if (warmupPoint != null)
        {
            MoveTo(warmupPoint);
        }
    }
    // 🔹 Finishga yetganda (finish triggerdan chaqiriladi)
    public void OnFinishReached()
    {
        if (!hasLamb) return;

        hasLamb = false;
        StopItemTimer();

        Debug.Log("[NPC] Finishga uloq bilan yetib keldi!");

        // BaseManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);
        // BaseManager.Instance?.NPCArrived(this); // o‘zingni metoding bo‘lsa
    }
    #endregion

    #region Not Used Yet
    // ==========================
    // 4) TASHQI SABAB BILAN ULOQ YO‘QOTISH
    // ==========================

    /// <summary>
    /// Masalan: qamchi bilan urib yuborildi, boshqa rider tortib oldi va h.k.
    /// </summary>
    public void OnLambDroppedExternally()
    {
        DropOwnedUlak();
    }
    private void HandleGoatOwnership(bool ownerHasGoat)
    {
        // Agar men uni ushlab turmasam, managerdagi live Ulakka qaytaman.
        if (!pickUp.Has_Item)
        {
            // Uloq kimga o'tganidan qat’i nazar men endi egasi emasman
            hasLamb = false;

            StopItemTimer(); // agar timer ishlayotgan bo‘lsa

            // darhol uloqqa qaytamiz
            MoveToCurrentUlak();
            return;
        }

        // 2) Agar men hozirgi egasi bo‘lsam → hech narsa qilinmaydi
        //    (MoveToNextPoint davom etadi)
    }
    #endregion

    #region Speed 
    public void MoveSecondWarmUpLocation(MAnimal horse)
    {
        StartCoroutine(ApplyLoverSpeed(horse));
    }
    private IEnumerator ApplyLoverSpeed(MAnimal horseAnimal)
    {
        int prevSpeedIndex = horseAnimal.CurrentSpeedIndex;

        // Slow speedga tushiramiz
        horseAnimal.Speed_CurrentIndex_Set(slowSpeedIndex);

        yield return new WaitForSeconds(slowDuration);
        // Avvalgi speedga qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(prevSpeedIndex);
    }
    #endregion

    #region Time finished Stop Rider
    private void HandleTimeFinished()
    {
        if (timeFinishedProcessed) return;
        timeFinishedProcessed = true;
        isGameplayActive = false;

        // coroutinelarni to'xtatamiz
        CancelPickupFocus();

        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }

        StopItemTimer();

        // agar uloq NPCda bo'lsa drop qilamiz
        if (hasLamb)
        {
            hasLamb = false;

            if (pickUp != null && pickUp.Has_Item)
            {
                pickUp.DropItem();
                KopkariResultsManager.Instance?.OnLambDropped(id);
            }

            KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);
        }

        StopRiderAI();
    }
    private void StopRiderAI()
    {
        isFinished = true;
        allCheckpointsDone = false;
        currentCheckpointIndex = -1;

        if (ai != null)
            ai.enabled = false;

        if (brain != null)
            brain.enabled = false;

        var animal = GetComponentInParent<MAnimal>();
        if (animal != null)
            animal.Speed_CurrentIndex_Set(0);
    }
    #endregion


}
