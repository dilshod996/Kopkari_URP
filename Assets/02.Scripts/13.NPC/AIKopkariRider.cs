using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using MalbersAnimations;
using UnityEngine.AI;

public class AIKopkariRider : MonoBehaviour
{
    private static readonly int NeighAnimatorStateHash = Animator.StringToHash("Neigh");
    private static readonly List<AIKopkariRider> ActiveRiders = new List<AIKopkariRider>(16);
    private static readonly List<AIKopkariRider> ActiveGuards = new List<AIKopkariRider>(2);

    public enum UlakRole
    {
        Competitor,
        Orbit,
        Guard,
        TrapSetter
    }

    public enum GripDamageSource
    {
        WalkTrap,
        GuardRiderMelee,
        GuardHorseAttack,
        GuardContact,
        MainRivalSideAttack,
        TrapSetterContact,
        OtherRiderContact,
        PlayerTouch
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

    [Header("AI Carrier Escape Boost")]
    [Tooltip("Short AI-only speed boost immediately after taking the Uloq.")]
    [SerializeField, Min(0f)] private float carrierEscapeSpeedBoostDuration = 1.75f;

    [Header("Ulak Gameplay")]
    [SerializeField, Min(1f)] private float orbitRadius = 7f;
    [SerializeField, Range(6, 24)] private int orbitPointCount = 12;
    [SerializeField, Min(0.1f)] private float orbitStoppingDistance = 0.8f;
    [SerializeField, Min(1f)] private float orbitPointTimeout = 8f;
    [Tooltip("Distance kept between a Guard and the Ulak. Two Guards use opposite slots.")]
    [SerializeField, Min(0.5f)] private float guardRadius = 2.4f;
    [SerializeField, Min(0.05f)] private float guardStoppingDistance = 0.45f;
    [Tooltip("Low-frequency Guard navigation refresh. Two Guards at 0.25 seconds is mobile friendly.")]
    [SerializeField, Min(0.1f)] private float guardRepathInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float guardRepathDistance = 0.65f;

    [Header("Trap Setter")]
    [Tooltip("A trap is placed after the current carrier has travelled this far.")]
    [SerializeField, Min(5f)] private float trapPlacementDistance = 20f;
    [Tooltip("How far ahead of the carrier the Trap Setter tries to ride.")]
    [SerializeField, Min(1f)] private float trapLeadDistance = 10f;
    [SerializeField, Min(0f)] private float trapLateralOffset = 1.5f;
    [SerializeField, Min(0f)] private float trapDropBehindDistance = 3f;
    [SerializeField, Min(0.1f)] private float trapSetterStoppingDistance = 1.25f;
    [SerializeField, Min(0.5f)] private float trapSetterContactDistance = 1.8f;
    [SerializeField, Min(0.5f)] private float trapSetterContactResetDistance = 3f;
    [FormerlySerializedAs("trapSetterIdlePatrolRadius")]
    [Tooltip("Distance from the grounded Uloq toward the current finish where the Trap Setter waits for a carrier.")]
    [SerializeField, Min(1f)] private float trapSetterWaitingDistance = 5f;
    [Tooltip("Low-frequency navigation and distance sample. This does not run in Update.")]
    [SerializeField, Min(0.2f)] private float trapSetterRepathInterval = 0.4f;
    [SerializeField, Min(1f)] private float trapFinishExclusionRadius = 6f;
    [SerializeField, Min(0.1f)] private float trapNavMeshSampleRadius = 2f;
    [SerializeField, Min(1f)] private float trapLifetime = 14f;
    [Header("Guard Attack")]
    [Tooltip("Malbers Attack1 Mode ID on the horse prefab.")]
    [SerializeField] private int guardAttackModeId = 1;
    [Tooltip("Maximum horizontal distance from a focusing rider at which a Guard may attack.")]
    [SerializeField, Min(0.5f)] private float guardAttackRange = 2.4f;
    [SerializeField, Min(0.1f)] private float guardAttackCooldown = 2.5f;
    [Tooltip("Distance kept from a rider while the Guard is actively defending the Ulak.")]
    [SerializeField, Min(0.25f)] private float guardEngageStoppingDistance = 1.25f;
    [Tooltip("Horizontal holder-to-Uloq distance that counts as Guard body contact. Grip protection and per-Guard cooldown still apply.")]
    [SerializeField, Min(0.1f)] private float guardCarrierContactDistance = 1f;
    [Tooltip("The Guard faces the picker before playing the forward attack.")]
    [SerializeField, Range(5f, 60f)] private float guardAttackFacingAngle = 25f;
    [Tooltip("A Guard stops pursuing if the focusing rider moves this far from the Ulak.")]
    [SerializeField, Min(1f)] private float guardMaxEngagementDistanceFromUlak = 7f;
    [Tooltip("Failsafe used if an unfocus event is missed.")]
    [SerializeField, Min(1f)] private float guardEngagementTimeout = 6f;
    [Tooltip("Attack1 front-leg ability used when the target is in front of the horse.")]
    [SerializeField, Min(1)] private int guardFrontAttackAbility = 1;
    [Tooltip("Inactive Malbers horse damage-trigger root. Auto-found by the name 'Attack Triggers'.")]
    [SerializeField] private GameObject guardHorseAttackTriggersRoot;

    [Header("Guard Rider Melee")]
    [Tooltip("Mounted rider Animator. Auto-found by the configured upper-body layer when empty.")]
    [SerializeField] private Animator guardRiderAnimator;
    [SerializeField] private string guardRiderMeleeLayer = "Upper Body (Weapons)";
    [SerializeField] private string guardRiderMeleeLeftState = "Riding Melee Left Forward";
    [SerializeField] private string guardRiderMeleeRightState = "Riding Melee Right Forward";
    [SerializeField, Min(0.5f)] private float guardRiderMeleeRange = 3f;
    [SerializeField, Min(0.1f)] private float guardRiderMeleeCooldown = 1.5f;
    [SerializeField, Range(0f, 0.5f)] private float guardRiderMeleeBlend = 0.08f;
    [Tooltip("Optional hand weapon/hitbox object enabled only during the melee hit window.")]
    [SerializeField] private GameObject guardRiderMeleeAttackObject;
    [SerializeField, Min(0f)] private float guardRiderMeleeHitDelay = 0.15f;
    [SerializeField, Min(0.05f)] private float guardRiderMeleeHitDuration = 0.45f;

    [SerializeField, Min(0.1f)] private float movingUlakStoppingDistance = 1.2f;
    [Tooltip("Close stopping distance used by non-carriers while pursuing the carrier horse.")]
    [SerializeField, Min(0.05f)] private float carrierChaseStoppingDistance = 0.15f;
    [Tooltip("Horizontal range in which a competitor can build pickup focus against a carrier.")]
    [SerializeField, Min(0.5f)] private float carrierTakeoverFocusRange = 2f;
    [Tooltip("Low-cost sampling interval for held-Uloq pickup focus.")]
    [SerializeField, Range(0.05f, 0.5f)] private float carrierTakeoverFocusInterval = 0.1f;
    [Tooltip("Tighter distance used only when a pushed rider must re-enter its pickup trigger.")]
    [SerializeField, Min(0.05f)] private float ulakReturnStoppingDistance = 0.25f;
    [Tooltip("Cheap fallback for competitors pushed out after their pickup trigger stopped navigation.")]
    [SerializeField, Min(0.25f)] private float ulakRecoveryCheckInterval = 0.5f;
    [SerializeField, Min(0.5f)] private float ulakRecoveryDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float salymStoppingDistance = 0.7f;

    [Header("Round Transition")]
    [SerializeField, Min(0.1f)] private float roundWarmupStoppingDistance = 1.5f;
    [SerializeField, Min(0f)] private float roundWinnerNeighDuration = 1.25f;

    [Header("Navigation Avoidance")]
    [Tooltip("Medium gives the small Kopkari field better steering without using the expensive highest quality mode.")]
    [SerializeField] private ObstacleAvoidanceType gameplayAvoidanceQuality =
        ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    [Tooltip("Normal AI separation radius. Keep this close to the horse body's horizontal half-width.")]
    [SerializeField, Min(0.1f)] private float baseAvoidanceRadius = 0.78f;
    [Tooltip("Chase radius for ordinary riders. With the active carrier radius, this keeps about 1.1 metres of combined avoidance.")]
    [SerializeField, Min(0.1f)] private float ordinaryChaserAvoidanceRadius = 0.75f;
    [Tooltip("Closer chase radius for the Main Rival, Guards, and Trap Setter. Runtime also clamps this against Close Role Carrier Approach Distance.")]
    [SerializeField, Min(0.1f)] private float closeRoleChaserAvoidanceRadius = 0.15f;
    [Tooltip("Required close approach to the carried Uloq for the Main Rival, Guards, and Trap Setter. Ordinary riders keep their existing spacing.")]
    [SerializeField, Range(0.4f, 0.5f)] private float closeRoleCarrierApproachDistance = 0.45f;
    [Tooltip("Larger carrier radius used only during the pickup grip-protection period.")]
    [SerializeField, Min(0.1f)] private float carrierAvoidanceRadius = 1.05f;
    [Tooltip("Carrier radius after pickup protection expires, allowing selected roles to make close contact.")]
    [SerializeField, Min(0.1f)] private float activeCarrierAvoidanceRadius = 0.3f;
    [Tooltip("Lower values have higher NavMesh avoidance priority.")]
    [SerializeField, Range(0, 99)] private int carrierAvoidancePriority = 20;
    [SerializeField, Range(0, 99)] private int mainRivalAvoidancePriority = 35;
    [SerializeField, Range(0, 99)] private int guardAvoidancePriority = 38;
    [SerializeField, Range(0, 99)] private int trapSetterAvoidancePriority = 42;
    [SerializeField, Range(0, 99)] private int competitorAvoidancePriorityMin = 45;
    [SerializeField, Range(0, 99)] private int competitorAvoidancePriorityMax = 60;
    [SerializeField, Range(0, 99)] private int orbitAvoidancePriorityMin = 65;
    [SerializeField, Range(0, 99)] private int orbitAvoidancePriorityMax = 80;

    [Header("Carrier Escape")]
    [SerializeField] private bool useCarrierEscapeWaypoint = true;
    [SerializeField, Min(1f)] private float carrierEscapeDistance = 5f;
    [SerializeField, Min(0f)] private float carrierEscapeSideOffset = 1.5f;
    [SerializeField, Min(0.1f)] private float carrierEscapeNavMeshSampleRadius = 2f;
    [SerializeField, Min(0.1f)] private float carrierEscapeStoppingDistance = 0.8f;

    [Header("Chase Obstacle Steering")]
    [SerializeField] private bool useChaseObstacleSteering = true;
    [Tooltip("Cheap progress sampling interval. Physics probes run only after the rider is considered stuck.")]
    [SerializeField, Min(0.25f)] private float chaseAvoidanceInterval = 0.5f;
    [SerializeField, Min(0.05f)] private float chaseStuckMovementThreshold = 0.3f;
    [SerializeField, Min(0.25f)] private float chaseStuckDuration = 0.9f;
    [SerializeField, Min(0.1f)] private float chaseProbeRadius = 0.55f;
    [SerializeField, Min(0.5f)] private float chaseProbeDistance = 3f;
    [SerializeField, Min(0f)] private float chaseProbeHeight = 0.8f;
    [SerializeField, Min(0.5f)] private float chaseEngagementDistance = 2.5f;
    [SerializeField, Min(0.5f)] private float chaseDetourForwardDistance = 3f;
    [SerializeField, Min(0.25f)] private float chaseDetourSideDistance = 1.75f;
    [SerializeField, Min(0.1f)] private float chaseDetourNavMeshSampleRadius = 1.5f;
    [SerializeField, Min(0f)] private float chaseDetourCooldown = 1.25f;
    [SerializeField] private LayerMask chaseAvoidanceLayers = ~0;

    [Header("Grip Loss")]
    [SerializeField] private bool dropOnWalkZone = true;
    [SerializeField] private bool dropOnWebSnare = true;
    [SerializeField] private bool dropOnObstacle = true;

    [Header("Carrier Grip")]
    [SerializeField, Min(1f)] private float maximumGrip = 100f;
    [SerializeField, Min(0f)] private float guardRiderMeleeGripDamage = 35f;
    [SerializeField, Min(0f)] private float walkTrapGripDamage = 50f;
    [SerializeField, Min(0f)] private float mainRivalSideAttackGripDamage = 20f;
    [SerializeField, Min(0f)] private float trapSetterContactGripDamage = 20f;
    [SerializeField, Min(0f)] private float otherRiderContactGripDamage = 10f;
    [SerializeField, Min(0f)] private float playerTouchGripDamage = 20f;
    [SerializeField, Min(0f)] private float guardHorseAttackGripDamage = 10f;
    [SerializeField, Min(0f)] private float guardContactGripDamage = 20f;
    [Tooltip("Prevents a newly picked Uloq from being dropped immediately by an overlapping hitbox.")]
    [SerializeField, Min(0f)] private float gripPickupProtectionDuration = 1.5f;
    [Tooltip("Per attacker and damage type. Multiple colliders cannot drain grip every physics step.")]
    [SerializeField, Min(0.1f)] private float gripDamageCooldown = 1f;
    [Tooltip("Only the current AI carrier checks nearby rider contact, at this low-frequency interval.")]
    [SerializeField, Min(0.6f)] private float playerTouchCheckInterval = 0.6f;
    [SerializeField, Min(0.25f)] private float playerTouchCheckRadius = 1.65f;
    [SerializeField] private Vector3 playerTouchCheckOffset = new Vector3(0f, 0.8f, 0.25f);
    [SerializeField] private LayerMask gripContactLayers = ~0;

    private int currentCheckpointIndex = -1;
    public bool hasLamb = false;
    // ⏱ coroutinelar
    private Coroutine waitCoroutine;
    private Coroutine itemTimerCoroutine;
    private Coroutine orbitCoroutine;
    private Coroutine guardCoroutine;
    private Coroutine trapSetterCoroutine;
    private Coroutine gripContactCoroutine;
    private Coroutine ulakRecoveryCoroutine;
    private Coroutine guardRiderMeleeObjectCoroutine;
    private bool guardRiderMeleeDamageWindowActive;
    private MAttackTrigger[] guardRiderMeleeAttackTriggers = Array.Empty<MAttackTrigger>();
    private Coroutine returnToUlakCoroutine;
    private Coroutine chaseAvoidanceCoroutine;
    private Coroutine carrierEscapeSpeedBoostCoroutine;
    private Coroutine carrierAvoidanceTransitionCoroutine;
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
    [Tooltip("Maximum distance used to place the final start slot safely on the baked NavMesh.")]
    [SerializeField, Min(0.1f)] private float startSlotNavMeshSampleRadius = 2f;
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
    private bool isFakeUlakDistracted;
    private Transform fakeUlakDiversionTarget;
    private bool inspectionFacingHeld;
    private bool previousLockMovement;
    private bool previousRotateAtDirection;
    private Vector3 inspectionLookPosition;
    private bool hasInspectionLookPosition;
    private UlakRole ulakRole = UlakRole.Competitor;
    private int orbitSlotIndex;
    private int orbitSlotCount = 1;
    private float nextGuardAttackTime;
    private Transform guardEngagementTarget;
    private AIKopkariRider guardEngagementAIRider;
    private GameObject guardCarrierOwner;
    private float guardEngagementDeadline;
    private float nextGuardRiderMeleeTime;
    private bool guardHorseAttackTriggersInitialActive;
    private bool guardHorseAttackTriggersStateCached;
    private readonly List<GameObject> trapSetterPrefabs = new List<GameObject>(4);
    private GameObject trapSetterCarrierOwner;
    private Vector3 trapSetterLastTravelPosition;
    private float trapSetterTravel;
    private float trapSetterSideSign = 1f;
    private bool trapSetterContactLatched;
    private int lastTrapPrefabIndex = -1;
    private bool trapSetterMissingPrefabWarningShown;
    private readonly Dictionary<int, float> nextGripDamageTimes = new Dictionary<int, float>(12);
    private readonly Collider[] gripContactBuffer = new Collider[24];
    private float currentGrip = 100f;
    private float gripProtectionUntil;
    private bool hasCarrierHistory;
    private bool isCarrierEscaping;
    private bool carrierEscapeCompletedForCurrentHold;
    private bool carrierEscapeSpeedBoostActive;
    private Transform carrierEscapeTarget;
    private NavMeshPath carrierEscapePath;
    private bool isUsingChaseDetour;
    private Transform chaseDetourTarget;
    private NavMeshPath chaseDetourPath;
    private RaycastHit[] chaseProbeHits;
    private float nextChaseDetourTime;
    private float chaseDetourSideSign;
    private bool chaseProgressInitialized;
    private Vector3 chaseLastProgressPosition;
    private float chaseLastProgressTime;
    private float chaseStuckElapsed;
    private bool wonCurrentRound;
    private bool isMovingToRoundWarmup;
    private bool isRoundWarmupQualified;
    private bool isEliminatedFromRounds;
    private Coroutine roundWinnerRoutine;
    private PreparationState preparationState = PreparationState.Idle;

    public static event Action<AIKopkariRider> OnRiderReady;
    public static event Action<AIKopkariRider> OnRiderPassedGate;
    public static event Action<AIKopkariRider, float, float> OnCarrierGripChanged;

    public int Id => id;
    public string RiderName => nameNpc;
    public string TeamName => teamName;
    public string CountryName => countryName;
    public Sprite FlagIcon => flagIcon;
    public string HorseName => string.IsNullOrWhiteSpace(horseName) ? "Horse" : horseName;
    public int Winnings => Mathf.Max(0, winnings);
    public bool HasGuardRiderMeleeAttack => guardRiderMeleeAttackObject != null;
    public bool IsMainRival => isMainRival;
    public bool CanTakeCarriedUlak => ulakRole == UlakRole.Competitor || ulakRole == UlakRole.Guard;
    public bool IsReadyAtStart => preparationState == PreparationState.Ready;
    public bool IsPreparing => preparationState != PreparationState.Idle &&
                               preparationState != PreparationState.Ready &&
                               preparationState != PreparationState.Released;
    public PreparationState CurrentPreparationState => preparationState;
    public Transform StartPoint => startingPoint;
    public Transform InspectionCameraPoint => inspectionCameraPoint;
    public MAnimal Animal => ai != null ? ai.animal : null;
    public MPickUp PickupController => pickUp;
    public UlakRole GameplayUlakRole => ulakRole;
    public bool IsRoundWarmupQualified => isRoundWarmupQualified;
    public bool IsEliminatedFromRounds => isEliminatedFromRounds;
    public float PickupFocusElapsed => pickupFocusElapsed;
    public float PickupFocusRequired => pickupFocusRequired;
    public float PickupFocusProgress01 => pickupFocusRequired > 0f
        ? Mathf.Clamp01(pickupFocusElapsed / pickupFocusRequired)
        : 0f;
    public float CurrentGrip => currentGrip;
    public float MaximumGrip => Mathf.Max(1f, maximumGrip);
    public float GripNormalized => Mathf.Clamp01(currentGrip / MaximumGrip);

    /// <summary>
    /// Counts active gameplay AI near a point without allocations. Used by the
    /// local player's contested Uloq pickup and sampled at a low frequency.
    /// </summary>
    public static int CountActiveRidersNear(Vector3 position, float radius)
    {
        float clampedRadius = Mathf.Max(0f, radius);
        float radiusSqr = clampedRadius * clampedRadius;
        int count = 0;

        for (int i = ActiveRiders.Count - 1; i >= 0; i--)
        {
            AIKopkariRider rider = ActiveRiders[i];
            if (rider == null)
            {
                ActiveRiders.RemoveAt(i);
                continue;
            }

            if (!rider.isActiveAndEnabled || !rider.isGameplayActive || rider.isFinished)
                continue;

            Transform riderTransform = rider.Animal != null
                ? rider.Animal.transform
                : rider.transform;
            if ((riderTransform.position - position).sqrMagnitude <= radiusSqr)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Cheap collider-independent contact fallback. Rider count is small and
    /// callers sample this at low frequency, so this avoids per-frame physics work.
    /// </summary>
    public static bool TryGetNearestActiveRider(
        Vector3 position,
        float radius,
        AIKopkariRider excludedRider,
        out AIKopkariRider nearestRider)
    {
        position.y = 0f;
        float radiusSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        float nearestSqr = float.PositiveInfinity;
        nearestRider = null;

        for (int i = ActiveRiders.Count - 1; i >= 0; i--)
        {
            AIKopkariRider candidate = ActiveRiders[i];
            if (candidate == null)
            {
                ActiveRiders.RemoveAt(i);
                continue;
            }

            if (candidate == excludedRider || !candidate.isActiveAndEnabled ||
                !candidate.isGameplayActive || candidate.isFinished || candidate.Animal == null)
            {
                continue;
            }

            Vector3 candidatePosition = candidate.Animal.transform.position;
            candidatePosition.y = 0f;
            float distanceSqr = (candidatePosition - position).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= nearestSqr)
                continue;

            nearestSqr = distanceSqr;
            nearestRider = candidate;
        }

        return nearestRider != null;
    }

    private void Awake()
    {
        if (!brain) brain = GetComponentInParent<MAnimalBrain>();
        if (!ai) ai = GetComponentInParent<MAnimalAIControl>();
        if (!pickUp) pickUp = GetComponentInChildren<MPickUp>(true);
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
        if (!ActiveRiders.Contains(this))
            ActiveRiders.Add(this);
        RefreshGuardRegistration();
        if (ulakRole == UlakRole.Guard)
        {
            ResolveGuardAttackDependencies();
            SetGuardHorseAttackTriggersActive(true);
        }
        KopkariManager.OnMainGameStarted += BeginGameplay;
        TargetReachEvent.OnRoundEnded += HandleFinish;
        KopkariManager.OnGoatOwnerChanged += HandleGoatOwnerChanged;
        KopkariManager.OnFakeUlakDiversionStarted += HandleFakeUlakDiversionStarted;
        KopkariManager.OnFakeUlakDiversionEnded += HandleFakeUlakDiversionEnded;
        if (ai != null)
            ai.OnTargetArrived.AddListener(HandleAiTargetArrived);
        if (pickUp != null)
        {
            pickUp.OnFocusedItem.AddListener(HandleFocusedItemChanged);
            pickUp.OnItemPicked.AddListener(HandleItemPicked);
        }
        if (boosterContainer != null)
        {
            boosterContainer.OnNpcGripBreakDamage += HandleNpcGripBreakDamage;
            boosterContainer.OnNpcAttackDamageReceived += HandleNpcAttackDamageReceived;
        }
        if (obstacleSensor != null)
            obstacleSensor.OnTouched += HandleObstacleTouched;
    }

    private void OnDisable()
    {
        ActiveRiders.Remove(this);
        ActiveGuards.Remove(this);
        ClearGuardEngagement();
        StopUlakRecovery();
        StopGuardRiderMeleeObject();
        RestoreGuardHorseAttackTriggers();
        StopPresentationNeigh();
        StopGuardingUlak();
        StopTrapSetterRole();
        StopGripContactMonitoring();
        CancelCarrierEscape();
        CancelReturnToUlak();
        StopChaseObstacleSteering();
        StopCarrierEscapeSpeedBoost();
        StopCarrierAvoidanceTransition();
        if (roundWinnerRoutine != null)
        {
            StopCoroutine(roundWinnerRoutine);
            roundWinnerRoutine = null;
        }
        ReleaseInspectionFacingHold();
        KopkariManager.OnMainGameStarted -= BeginGameplay;
        TargetReachEvent.OnRoundEnded -= HandleFinish;
        KopkariManager.OnGoatOwnerChanged -= HandleGoatOwnerChanged;
        KopkariManager.OnFakeUlakDiversionStarted -= HandleFakeUlakDiversionStarted;
        KopkariManager.OnFakeUlakDiversionEnded -= HandleFakeUlakDiversionEnded;
        isFakeUlakDistracted = false;
        fakeUlakDiversionTarget = null;
        if (ai != null)
            ai.OnTargetArrived.RemoveListener(HandleAiTargetArrived);
        if (pickUp != null)
        {
            pickUp.OnFocusedItem.RemoveListener(HandleFocusedItemChanged);
            pickUp.OnItemPicked.RemoveListener(HandleItemPicked);
        }
        if (boosterContainer != null)
        {
            boosterContainer.OnNpcGripBreakDamage -= HandleNpcGripBreakDamage;
            boosterContainer.OnNpcAttackDamageReceived -= HandleNpcAttackDamageReceived;
        }
        if (obstacleSensor != null)
            obstacleSensor.OnTouched -= HandleObstacleTouched;

        // xavfsizlik uchun
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        if (itemTimerCoroutine != null) StopCoroutine(itemTimerCoroutine);
        if (preparationCoroutine != null) StopCoroutine(preparationCoroutine);
        if (alignmentCoroutine != null) StopCoroutine(alignmentCoroutine);
        if (orbitCoroutine != null) StopCoroutine(orbitCoroutine);
        KopkariRiderSpeedController.RestoreUnmodifiedSpeed(Animal);
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
        if (role != UlakRole.TrapSetter)
            StopTrapSetterRole();

        ulakRole = role;
        orbitSlotIndex = Mathf.Max(0, slotIndex);
        orbitSlotCount = Mathf.Max(1, slotCount);
        ClearGuardEngagement();
        RefreshGuardRegistration();

        if (ulakRole == UlakRole.Guard)
        {
            ResolveGuardAttackDependencies();
            SetGuardHorseAttackTriggersActive(true);
        }
        else
        {
            RestoreGuardHorseAttackTriggers();
        }

        if (ulakRole != UlakRole.Competitor)
            CancelPickupFocus();

        if (!hasLamb)
            ApplyBaseNavigationAvoidance();

        if (isGameplayActive)
            ApplyConfiguredGameplaySpeed();
    }

    private void ApplyConfiguredGameplaySpeed()
    {
        if (Animal == null)
            return;

        if (Animal.CurrentSpeedSet != null)
            Animal.CurrentSpeedSet.LockSpeed = false;
        Animal.Sprint = false;
        if (hasLamb && carrierEscapeSpeedBoostActive)
            KopkariRiderSpeedController.ApplyAICarrierEscapeBoost(Animal);
        else
            KopkariRiderSpeedController.ApplyAI(Animal, ulakRole, isMainRival, hasLamb);
    }

    private void StartCarrierEscapeSpeedBoost()
    {
        if (carrierEscapeSpeedBoostActive)
            return;

        float duration = Mathf.Max(0f, carrierEscapeSpeedBoostDuration);
        if (!isGameplayActive || ulakRole != UlakRole.Competitor || !hasLamb || duration <= 0f)
        {
            ApplyConfiguredGameplaySpeed();
            return;
        }

        carrierEscapeSpeedBoostActive = true;
        ApplyConfiguredGameplaySpeed();
        carrierEscapeSpeedBoostCoroutine = StartCoroutine(CarrierEscapeSpeedBoostRoutine(duration));
    }

    private IEnumerator CarrierEscapeSpeedBoostRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        carrierEscapeSpeedBoostCoroutine = null;
        carrierEscapeSpeedBoostActive = false;

        if (isGameplayActive && hasLamb)
            ApplyConfiguredGameplaySpeed();
    }

    private void StopCarrierEscapeSpeedBoost()
    {
        if (carrierEscapeSpeedBoostCoroutine != null)
        {
            StopCoroutine(carrierEscapeSpeedBoostCoroutine);
            carrierEscapeSpeedBoostCoroutine = null;
        }

        carrierEscapeSpeedBoostActive = false;
    }

    public void ConfigureTrapSetterPrefabs(IReadOnlyList<GameObject> prefabs)
    {
        trapSetterPrefabs.Clear();
        lastTrapPrefabIndex = -1;
        trapSetterMissingPrefabWarningShown = false;

        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab != null && !trapSetterPrefabs.Contains(prefab))
                trapSetterPrefabs.Add(prefab);
        }
    }

    private void ApplyBaseNavigationAvoidance()
    {
        ConfigureNavigationAvoidance(GetBaseAvoidancePriority(), baseAvoidanceRadius);
    }

    private void ApplyCarrierNavigationAvoidance()
    {
        StopCarrierAvoidanceTransition();

        float protectionRemaining = gripProtectionUntil - Time.time;
        if (protectionRemaining <= 0f)
        {
            ApplyActiveCarrierNavigationAvoidance();
            return;
        }

        ConfigureNavigationAvoidance(carrierAvoidancePriority, carrierAvoidanceRadius);
        carrierAvoidanceTransitionCoroutine = StartCoroutine(
            ReduceCarrierAvoidanceAfterProtection(protectionRemaining));
    }

    private void ApplyChaseNavigationAvoidance()
    {
        float chaseRadius;
        if (UsesCloseCarrierChaseRadius())
        {
            // The active carrier contributes its own radius. Clamp the selected
            // roles so their combined avoidance is about 0.4-0.5 m. During the
            // pickup protection window the carrier intentionally remains wider.
            float remainingRadius = GetCloseRoleCarrierApproachDistance() -
                                    Mathf.Max(0.1f, activeCarrierAvoidanceRadius);
            chaseRadius = Mathf.Min(
                closeRoleChaserAvoidanceRadius,
                Mathf.Max(0.1f, remainingRadius));
        }
        else
        {
            chaseRadius = ordinaryChaserAvoidanceRadius;
        }

        ConfigureNavigationAvoidance(
            GetBaseAvoidancePriority(),
            Mathf.Min(baseAvoidanceRadius, chaseRadius));
    }

    private void ApplyGuardCarrierNavigationAvoidance(GameObject ownerRoot)
    {
        ApplyChaseNavigationAvoidance();

        KopkariManager manager = KopkariManager.Instance;
        if (ownerRoot == null || manager == null ||
            !manager.IsLocalRiderTransform(ownerRoot.transform) || ai == null || ai.Agent == null)
            return;

        // The player's horse uses a non-carving NavMeshObstacle. Disabling local
        // avoidance only on the pursuing Guards lets them pass that dynamic
        // obstacle and make contact without changing the player or the NavMesh.
        ai.Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    private void ApplyActiveCarrierNavigationAvoidance()
    {
        ConfigureNavigationAvoidance(carrierAvoidancePriority, activeCarrierAvoidanceRadius);
    }

    private IEnumerator ReduceCarrierAvoidanceAfterProtection(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        carrierAvoidanceTransitionCoroutine = null;

        if (isGameplayActive && hasLamb)
            ApplyActiveCarrierNavigationAvoidance();
    }

    private void StopCarrierAvoidanceTransition()
    {
        if (carrierAvoidanceTransitionCoroutine == null)
            return;

        StopCoroutine(carrierAvoidanceTransitionCoroutine);
        carrierAvoidanceTransitionCoroutine = null;
    }

    private bool UsesCloseCarrierChaseRadius()
    {
        return isMainRival || ulakRole == UlakRole.Guard || ulakRole == UlakRole.TrapSetter;
    }

    private float GetCloseRoleCarrierApproachDistance()
    {
        return Mathf.Clamp(closeRoleCarrierApproachDistance, 0.4f, 0.5f);
    }

    private Transform GetCloseRoleApproachOrigin()
    {
        if (pickUp != null && pickUp.Holder != null)
            return pickUp.Holder;

        return Animal != null ? Animal.transform : transform;
    }

    private Transform GetCarriedUlakApproachTarget(Transform fallback)
    {
        Transform heldUlak = GetCurrentUlakTransform();
        return heldUlak != null ? heldUlak : fallback;
    }

    private void ConfigureNavigationAvoidance(int priority, float radius)
    {
        if (ai == null || ai.Agent == null)
            return;

        ai.Agent.obstacleAvoidanceType = gameplayAvoidanceQuality;
        ai.Agent.avoidancePriority = Mathf.Clamp(priority, 0, 99);
        ai.Agent.radius = Mathf.Max(0.1f, radius);
    }

    private int GetBaseAvoidancePriority()
    {
        if (isMainRival)
            return Mathf.Clamp(mainRivalAvoidancePriority, 0, 99);

        if (ulakRole == UlakRole.Guard)
            return Mathf.Clamp(guardAvoidancePriority, 0, 99);

        if (ulakRole == UlakRole.TrapSetter)
            return Mathf.Clamp(trapSetterAvoidancePriority, 0, 99);

        if (ulakRole == UlakRole.Orbit)
        {
            int minimum = Mathf.Min(orbitAvoidancePriorityMin, orbitAvoidancePriorityMax);
            int maximum = Mathf.Max(orbitAvoidancePriorityMin, orbitAvoidancePriorityMax);
            float slot01 = orbitSlotCount > 1
                ? Mathf.Clamp01((float)orbitSlotIndex / (orbitSlotCount - 1))
                : 0.5f;
            return Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, slot01));
        }

        int competitorMinimum = Mathf.Min(competitorAvoidancePriorityMin, competitorAvoidancePriorityMax);
        int competitorMaximum = Mathf.Max(competitorAvoidancePriorityMin, competitorAvoidancePriorityMax);
        int range = competitorMaximum - competitorMinimum + 1;
        int stableSeed = GetInstanceID() & int.MaxValue;
        return competitorMinimum + stableSeed % Mathf.Max(1, range);
    }

    private void RefreshGuardRegistration()
    {
        ActiveGuards.Remove(this);
        if (isActiveAndEnabled && ulakRole == UlakRole.Guard)
            ActiveGuards.Add(this);
    }

    public static bool IsCarrierEngagedByGuard(GameObject ownerRoot)
    {
        if (ownerRoot == null)
            return false;

        Transform ownerTransform = ownerRoot.transform;
        for (int i = ActiveGuards.Count - 1; i >= 0; i--)
        {
            AIKopkariRider guard = ActiveGuards[i];
            if (guard == null)
            {
                ActiveGuards.RemoveAt(i);
                continue;
            }

            GameObject engagedOwner = guard.guardCarrierOwner;
            if (!guard.isGameplayActive || !guard.guardRiderMeleeDamageWindowActive || engagedOwner == null)
                continue;

            Transform engagedTransform = engagedOwner.transform;
            if (engagedTransform == ownerTransform || engagedTransform.IsChildOf(ownerTransform) ||
                ownerTransform.IsChildOf(engagedTransform))
                return true;
        }

        return false;
    }

    private void ResolveGuardAttackDependencies()
    {
        Transform[] children = transform.root.GetComponentsInChildren<Transform>(true);
        if (guardHorseAttackTriggersRoot == null)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == "Attack Triggers")
                {
                    guardHorseAttackTriggersRoot = children[i].gameObject;
                    break;
                }
            }
        }

        if (guardRiderAnimator == null)
        {
            Animator[] animators = transform.root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate != null && candidate.GetLayerIndex(guardRiderMeleeLayer) >= 0)
                {
                    guardRiderAnimator = candidate;
                    break;
                }
            }
        }

        if (guardRiderMeleeAttackObject != null)
        {
            guardRiderMeleeAttackTriggers =
                guardRiderMeleeAttackObject.GetComponentsInChildren<MAttackTrigger>(true);
            SetGuardRiderMeleeHitboxActive(false);
        }
    }

    private void SetGuardHorseAttackTriggersActive(bool active)
    {
        if (guardHorseAttackTriggersRoot == null)
            return;

        if (!guardHorseAttackTriggersStateCached)
        {
            guardHorseAttackTriggersInitialActive = guardHorseAttackTriggersRoot.activeSelf;
            guardHorseAttackTriggersStateCached = true;
        }

        guardHorseAttackTriggersRoot.SetActive(active);
    }

    private void RestoreGuardHorseAttackTriggers()
    {
        if (guardHorseAttackTriggersRoot != null && guardHorseAttackTriggersStateCached)
            guardHorseAttackTriggersRoot.SetActive(guardHorseAttackTriggersInitialActive);
        guardHorseAttackTriggersStateCached = false;
    }

    public static void NotifyUlakFocusedBy(GameObject focusOwner)
    {
        if (focusOwner == null)
            return;

        Transform targetRoot = focusOwner.transform.root;
        AIKopkariRider closestGuard = null;
        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = ActiveGuards.Count - 1; i >= 0; i--)
        {
            AIKopkariRider guard = ActiveGuards[i];
            if (guard == null)
            {
                ActiveGuards.RemoveAt(i);
                continue;
            }

            if (guard.guardEngagementTarget == targetRoot)
            {
                guard.BeginGuardEngagement(targetRoot);
                return;
            }

            // One Guard handles one focusing rider. This lets the two Guards react
            // independently when two competitors enter the pickup area together.
            if (guard.guardEngagementTarget != null)
                continue;

            float distanceSqr;
            if (!guard.CanEngageFocusedRider(targetRoot, out distanceSqr) || distanceSqr >= closestDistanceSqr)
                continue;

            closestGuard = guard;
            closestDistanceSqr = distanceSqr;
        }

        closestGuard?.BeginGuardEngagement(targetRoot);
    }

    public static void NotifyUlakUnfocusedBy(GameObject focusOwner)
    {
        if (focusOwner == null)
            return;

        Transform targetRoot = focusOwner.transform.root;
        for (int i = ActiveGuards.Count - 1; i >= 0; i--)
        {
            AIKopkariRider guard = ActiveGuards[i];
            if (guard == null)
            {
                ActiveGuards.RemoveAt(i);
                continue;
            }

            if (guard.guardEngagementTarget == targetRoot)
                guard.ClearGuardEngagement();
        }
    }

    private bool CanEngageFocusedRider(Transform targetRoot, out float distanceSqr)
    {
        distanceSqr = float.PositiveInfinity;
        if (targetRoot == null || !isGameplayActive || isFinished || isEliminatedFromRounds ||
            ulakRole != UlakRole.Guard || Animal == null || targetRoot == transform.root)
            return false;

        Transform ulak = GetCurrentUlakTransform();
        if (ulak == null)
            return false;

        Vector3 targetFromUlak = targetRoot.position - ulak.position;
        targetFromUlak.y = 0f;
        float maximumUlakDistance = Mathf.Max(1f, guardMaxEngagementDistanceFromUlak);
        if (targetFromUlak.sqrMagnitude > maximumUlakDistance * maximumUlakDistance)
            return false;

        Vector3 offset = targetRoot.position - Animal.transform.position;
        offset.y = 0f;
        distanceSqr = offset.sqrMagnitude;
        return true;
    }

    private void BeginGuardEngagement(Transform targetRoot, AIKopkariRider focusedAIRider = null)
    {
        if (!CanEngageFocusedRider(targetRoot, out _))
            return;

        guardEngagementTarget = targetRoot;
        guardEngagementAIRider = focusedAIRider;
        guardEngagementDeadline = Time.time + Mathf.Max(1f, guardEngagementTimeout);
        TryAttackFocusedRider(targetRoot);
    }

    private void ClearGuardEngagement()
    {
        guardEngagementTarget = null;
        guardEngagementAIRider = null;
        guardCarrierOwner = null;
        guardEngagementDeadline = 0f;
    }

    private bool TryGetActiveGuardEngagement(out Transform targetRoot)
    {
        targetRoot = guardEngagementTarget;
        if (guardCarrierOwner != null)
        {
            KopkariManager manager = KopkariManager.Instance;
            if (manager == null || manager.currentGoatOwner != guardCarrierOwner || targetRoot == null)
            {
                ClearGuardEngagement();
                targetRoot = null;
                return false;
            }

            return true;
        }

        if (guardEngagementAIRider != null)
        {
            Pickable currentUlak = KopkariManager.Instance != null
                ? KopkariManager.Instance.pickableObj
                : null;
            if (!guardEngagementAIRider.isGameplayActive || guardEngagementAIRider.hasLamb ||
                guardEngagementAIRider.pickUp == null ||
                guardEngagementAIRider.pickUp.FocusedItem != currentUlak)
            {
                ClearGuardEngagement();
                targetRoot = null;
                return false;
            }
        }

        if (targetRoot == null || Time.time >= guardEngagementDeadline ||
            !CanEngageFocusedRider(targetRoot, out _))
        {
            ClearGuardEngagement();
            targetRoot = null;
            return false;
        }

        return true;
    }

    private bool TryFindFocusedAIRider(out AIKopkariRider focusedRider, out Transform targetTransform)
    {
        focusedRider = null;
        targetTransform = null;
        Pickable currentUlak = KopkariManager.Instance != null
            ? KopkariManager.Instance.pickableObj
            : null;
        if (currentUlak == null || Animal == null)
            return false;

        float closestDistanceSqr = float.PositiveInfinity;
        for (int i = ActiveRiders.Count - 1; i >= 0; i--)
        {
            AIKopkariRider candidate = ActiveRiders[i];
            if (candidate == null)
            {
                ActiveRiders.RemoveAt(i);
                continue;
            }

            if (candidate == this || !candidate.isGameplayActive || candidate.isFinished ||
                candidate.ulakRole != UlakRole.Competitor || candidate.hasLamb ||
                candidate.pickUp == null || candidate.pickUp.FocusedItem != currentUlak ||
                candidate.Animal == null)
                continue;

            Transform candidateTarget = candidate.Animal.transform;
            bool alreadyEngaged = false;
            for (int guardIndex = 0; guardIndex < ActiveGuards.Count; guardIndex++)
            {
                AIKopkariRider otherGuard = ActiveGuards[guardIndex];
                if (otherGuard != null && otherGuard != this &&
                    otherGuard.guardEngagementAIRider == candidate)
                {
                    alreadyEngaged = true;
                    break;
                }
            }
            if (alreadyEngaged || !CanEngageFocusedRider(candidateTarget, out float distanceSqr) ||
                distanceSqr >= closestDistanceSqr)
                continue;

            focusedRider = candidate;
            targetTransform = candidateTarget;
            closestDistanceSqr = distanceSqr;
        }

        return focusedRider != null && targetTransform != null;
    }

    private bool CanAttackFocusedRider(Transform targetRoot, out float distanceSqr)
    {
        distanceSqr = float.PositiveInfinity;
        if (targetRoot == null || !isGameplayActive || isFinished || isEliminatedFromRounds ||
            ulakRole != UlakRole.Guard || Animal == null || Time.time < nextGuardAttackTime ||
            targetRoot == transform.root)
            return false;

        Vector3 offset = targetRoot.position - Animal.transform.position;
        offset.y = 0f;
        distanceSqr = offset.sqrMagnitude;
        // Keep the strike close enough to make visual contact even if an older
        // scene still has the previous, larger serialized range.
        float range = Mathf.Clamp(guardAttackRange, 0.5f, 2.4f);
        return distanceSqr <= range * range;
    }

    private void TryAttackFocusedRider(Transform targetRoot)
    {
        if (!CanAttackFocusedRider(targetRoot, out _))
            return;

        Vector3 direction = targetRoot.position - Animal.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Animal.RotateAtDirection(direction.normalized);
        float facingAngle = Vector3.Angle(Animal.transform.forward, direction);
        if (facingAngle > Mathf.Clamp(guardAttackFacingAngle, 5f, 60f))
            return;

        int ability = Mathf.Max(1, guardFrontAttackAbility);

        // Mounted locomotion can reject a guarded mode request even though the
        // horse prefab owns the ability. Fall back to the direct Malbers mode
        // activation so a valid Attack1 ability is visibly played.
        if (!Animal.Mode_TryActivate(guardAttackModeId, ability))
            Animal.Mode_Activate(guardAttackModeId, ability);
        nextGuardAttackTime = Time.time + Mathf.Max(0.1f, guardAttackCooldown);
    }

    private void TryGuardRiderMeleeAttack(Transform carrierTarget)
    {
        if (carrierTarget == null || guardRiderAnimator == null || Animal == null ||
            Time.time < nextGuardRiderMeleeTime)
            return;

        Vector3 toCarrier = carrierTarget.position - Animal.transform.position;
        toCarrier.y = 0f;
        float range = guardCarrierOwner != null
            ? GetCloseRoleCarrierApproachDistance()
            : Mathf.Max(0.5f, guardRiderMeleeRange);
        Transform approachOrigin = GetCloseRoleApproachOrigin();
        Transform approachTarget = GetCarriedUlakApproachTarget(carrierTarget);
        Vector3 closeOffset = approachTarget.position - approachOrigin.position;
        closeOffset.y = 0f;
        if (closeOffset.sqrMagnitude > range * range || toCarrier.sqrMagnitude < 0.001f)
            return;

        Animal.RotateAtDirection(toCarrier.normalized);

        Transform ulak = GetCurrentUlakTransform();
        Vector3 sideReference = ulak != null ? ulak.position : carrierTarget.position;
        float localSide = Animal.transform.InverseTransformPoint(sideReference).x;
        string stateName = localSide < 0f
            ? guardRiderMeleeLeftState
            : guardRiderMeleeRightState;
        int layerIndex = guardRiderAnimator.GetLayerIndex(guardRiderMeleeLayer);
        if (layerIndex < 0 || string.IsNullOrWhiteSpace(stateName))
            return;

        int fullPathHash = Animator.StringToHash($"{guardRiderMeleeLayer}.{stateName}");
        if (!guardRiderAnimator.HasState(layerIndex, fullPathHash))
            return;

        guardRiderAnimator.SetLayerWeight(layerIndex, 1f);
        guardRiderAnimator.CrossFade(
            fullPathHash,
            Mathf.Clamp(guardRiderMeleeBlend, 0f, 0.5f),
            layerIndex,
            0f);
        nextGuardRiderMeleeTime = Time.time + Mathf.Max(0.1f, guardRiderMeleeCooldown);
        StartGuardRiderMeleeObjectWindow();
    }

    private void StartGuardRiderMeleeObjectWindow()
    {
        StopGuardRiderMeleeObject();
        if (guardRiderMeleeAttackObject != null)
            guardRiderMeleeObjectCoroutine = StartCoroutine(GuardRiderMeleeObjectWindow());
    }

    private void StopGuardRiderMeleeObject()
    {
        guardRiderMeleeDamageWindowActive = false;
        if (guardRiderMeleeObjectCoroutine != null)
        {
            StopCoroutine(guardRiderMeleeObjectCoroutine);
            guardRiderMeleeObjectCoroutine = null;
        }

        SetGuardRiderMeleeHitboxActive(false);
    }

    private void SetGuardRiderMeleeHitboxActive(bool active)
    {
        if (guardRiderMeleeAttackObject == null)
            return;

        if (active)
            guardRiderMeleeAttackObject.SetActive(true);

        for (int i = 0; i < guardRiderMeleeAttackTriggers.Length; i++)
        {
            MAttackTrigger attackTrigger = guardRiderMeleeAttackTriggers[i];
            if (attackTrigger == null)
                continue;

            // Rider melee is Animator-driven, not MAnimal Mode-driven, so it
            // must explicitly open the Malbers damage window for the hitbox.
            attackTrigger.CanCauseDamage = active;
        }

        if (!active)
            guardRiderMeleeAttackObject.SetActive(false);
    }

    private IEnumerator GuardRiderMeleeObjectWindow()
    {
        if (guardRiderMeleeHitDelay > 0f)
            yield return new WaitForSeconds(guardRiderMeleeHitDelay);

        guardRiderMeleeDamageWindowActive = true;
        SetGuardRiderMeleeHitboxActive(true);
        yield return new WaitForSeconds(Mathf.Max(0.05f, guardRiderMeleeHitDuration));
        guardRiderMeleeDamageWindowActive = false;
        SetGuardRiderMeleeHitboxActive(false);
        guardRiderMeleeObjectCoroutine = null;
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
        else if (target == GetCurrentRoundWarmupPoint())
        {
            ai.StoppingDistance = roundWarmupStoppingDistance;
            ApplyRoundWarmupSpeed();
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
            KopkariRiderSpeedController.RestoreUnmodifiedSpeed(Animal);
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
        StopGripContactMonitoring();
        ResetCarrierGrip(clearProtection: true);
        hasLamb = false;
        isFinished = false;
        currentCheckpointIndex = -1;
        isGameplayActive = false;
        isFakeUlakDistracted = false;
        fakeUlakDiversionTarget = null;
        hasCarrierHistory = false;
        CancelCarrierEscape();
        CancelReturnToUlak();
        StopChaseObstacleSteering();
        pickupRequestPending = false;
        ResetPickupFocusProgress();
        ApplyBaseNavigationAvoidance();

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
        if (isMovingToRoundWarmup && arrivedTarget != null &&
            arrivedTarget == GetCurrentRoundWarmupPoint())
        {
            isMovingToRoundWarmup = false;
            isRoundWarmupQualified = true;
            ai?.Stop();
            return;
        }

        if (isUsingChaseDetour && arrivedTarget != null && arrivedTarget == chaseDetourTarget)
        {
            isUsingChaseDetour = false;
            if (ShouldRunChaseObstacleSteering())
            {
                KopkariManager manager = KopkariManager.Instance;
                GameObject owner = manager != null ? manager.currentGoatOwner : null;
                if (owner != null)
                    MoveToCarrier(owner);
                else
                    MoveToCurrentUlak();
            }
            return;
        }

        if (isCarrierEscaping && arrivedTarget != null && arrivedTarget == carrierEscapeTarget)
        {
            isCarrierEscaping = false;
            carrierEscapeCompletedForCurrentHold = true;

            if (IsCurrentUlakCarrier())
                MoveOwnerDirectlyToFirstSalym();
            else
                MoveToCurrentUlak();
            return;
        }

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

        SettleAtStartingPointOnNavMesh();
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

    private void SettleAtStartingPointOnNavMesh()
    {
        if (Animal == null || startingPoint == null || ai == null || ai.Agent == null)
            return;

        int areaMask = ai.Agent.areaMask;
        if (NavMesh.SamplePosition(
                startingPoint.position,
                out NavMeshHit hit,
                Mathf.Max(0.1f, startSlotNavMeshSampleRadius),
                areaMask))
        {
            Animal.Rotation = startingPoint.rotation;
            Animal.Teleport(hit.position);
            return;
        }

        Debug.LogWarning(
            $"[{nameof(AIKopkariRider)}] '{name}' could not find NavMesh near start slot " +
            $"'{startingPoint.name}'. Keeping its current valid arrival position.",
            this);
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

        if (Animal == null || !IsReadyAtStart || isGameplayActive)
        {
            presentationNeighCoroutine = null;
            yield break;
        }

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

        // Neigh is the zero-movement profile of the Horse Jump state. Re-entering
        // that Malbers state for the second play also re-enters mounted rider
        // behaviours, which can visually dismount and mount the rider again.
        // Restart only the already active animator state instead.
        Animator animalAnimator = Animal.Anim;
        float enterDeadline = Time.unscaledTime + 1f;
        while (animalAnimator != null &&
               Time.unscaledTime < enterDeadline &&
               Animal != null &&
               Animal.ActiveStateID.ID == StateEnum.Jump &&
               animalAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != NeighAnimatorStateHash &&
               IsReadyAtStart &&
               !isGameplayActive)
        {
            yield return null;
        }

        float replayDeadline = Time.unscaledTime + 2.25f;
        while (animalAnimator != null &&
               Time.unscaledTime < replayDeadline &&
               Animal != null &&
               Animal.ActiveStateID.ID == StateEnum.Jump &&
               IsReadyAtStart &&
               !isGameplayActive)
        {
            AnimatorStateInfo stateInfo = animalAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == NeighAnimatorStateHash && stateInfo.normalizedTime >= 0.72f)
            {
                animalAnimator.Play(stateInfo.fullPathHash, 0, 0f);
                break;
            }

            yield return null;
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

        SettleAtStartingPointOnNavMesh();
        ai?.Stop();
        MarkReady();
        return true;
    }

    public void BeginGameplay()
    {
        if (isEliminatedFromRounds)
            return;

        bool isInitialRelease = preparationState != PreparationState.Released;
        if (isInitialRelease && !IsReadyAtStart)
        {
            Debug.LogWarning($"[{nameof(AIKopkariRider)}] {name} was asked to start before reaching its slot.", this);
            return;
        }

        StopPresentationNeigh();
        if (isInitialRelease)
            ReleaseInspectionFacingHold();

        ResetForRoundGameplay();
        EnablePickupInteraction();
        if (ulakRole != UlakRole.Competitor && pickUp != null)
            pickUp.enabled = false;
        ApplyBaseNavigationAvoidance();
        preparationState = PreparationState.Released;
        isGameplayActive = true;
        if (ulakRole == UlakRole.Competitor)
            StartUlakRecovery();
        ApplyConfiguredGameplaySpeed();

        if (ulakRole == UlakRole.Guard)
        {
            StartGuardingUlak();
        }
        else if (ulakRole == UlakRole.TrapSetter)
        {
            StartTrapSetterRole();
        }
        else if (KopkariManager.Instance != null && KopkariManager.Instance.currentGoatOwner != null)
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

    private void ResetForRoundGameplay()
    {
        if (roundWinnerRoutine != null)
        {
            StopCoroutine(roundWinnerRoutine);
            roundWinnerRoutine = null;
        }

        isGameplayActive = false;
        isFinished = false;
        isFakeUlakDistracted = false;
        fakeUlakDiversionTarget = null;
        StopCarrierEscapeSpeedBoost();
        hasLamb = false;
        hasCarrierHistory = false;
        wonCurrentRound = false;
        isMovingToRoundWarmup = false;
        isRoundWarmupQualified = false;
        currentCheckpointIndex = -1;
        allCheckpointsDone = false;
        pickupRequestPending = false;
        ClearGuardEngagement();
        StopGuardRiderMeleeObject();
        CancelCarrierEscape();
        CancelReturnToUlak();
        StopUlakRecovery();
        StopGuardingUlak();
        StopTrapSetterRole();
        StopGripContactMonitoring();
        ResetCarrierGrip(clearProtection: true);
        StopChaseObstacleSteering();
        CancelPickupFocus();
        StopItemTimer();

        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }

        IReadOnlyList<CheckpointTrigger> sharedCheckpoints = GetSharedCheckpoints();
        if (sharedCheckpoints != null)
            npcPassedCheckpoints = new bool[sharedCheckpoints.Count];
    }

    private void EnablePickupInteraction()
    {
        if (pickUp == null)
            pickUp = GetComponentInChildren<MPickUp>(true);

        if (pickUp == null)
        {
            Debug.LogWarning(
                $"[{nameof(AIKopkariRider)}] '{name}' has no {nameof(MPickUp)} component to enable for gameplay.",
                this);
            return;
        }

        if (!pickUp.gameObject.activeSelf)
            pickUp.gameObject.SetActive(true);
        if (!pickUp.enabled)
            pickUp.enabled = true;

        if (!pickUp.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                $"[{nameof(AIKopkariRider)}] '{name}' pickup object is enabled but an inactive parent still blocks it.",
                pickUp);
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
        CancelReturnToUlak();

        if (isFakeUlakDistracted)
            return;

        // allaqachon kutayotgan bo‘lsa yoki uloq bor bo‘lsa – qayta boshlama
        if (isFinished)
        {
            MoveTo(GetCurrentRoundWarmupPoint());
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
        if (isFakeUlakDistracted) return;
        if (ulakRole != UlakRole.Competitor) return;
        // ❗ NPC hali uloqni olmagan bo‘lsa – yana lambga qaytadi
        if (!hasLamb)
            ScheduleReturnToUlak();
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

    private void StartCarrierTakeoverFocus(GameObject ownerRoot)
    {
        if (!CanAttemptCarrierTakeover(ownerRoot) || waitCoroutine != null)
        {
            return;
        }

        waitCoroutine = StartCoroutine(WaitToTakeUlakFromCarrier(ownerRoot));
    }

    private IEnumerator WaitToTakeUlakFromCarrier(GameObject expectedOwner)
    {
        float minimum = Mathf.Max(0f, Mathf.Min(pickupFocusMinDuration, pickupFocusMaxDuration));
        float maximum = Mathf.Max(minimum, Mathf.Max(pickupFocusMinDuration, pickupFocusMaxDuration));
        pickupFocusRequired = Random.Range(minimum, maximum);
        pickupFocusElapsed = 0f;

        float interval = Mathf.Clamp(carrierTakeoverFocusInterval, 0.05f, 0.5f);
        WaitForSeconds wait = new WaitForSeconds(interval);
        while (CanAttemptCarrierTakeover(expectedOwner))
        {
            KopkariManager manager = KopkariManager.Instance;
            if (manager == null || manager.currentGoatOwner == null ||
                manager.currentGoatOwner != expectedOwner)
            {
                break;
            }

            Transform carrier = manager.ResolveGoatOwnerTarget(expectedOwner);
            if (carrier == null || Animal == null)
                break;

            Transform heldUlak = manager.UlakTransform;
            bool focusHeldUlak = isMainRival || ulakRole == UlakRole.Guard;
            Transform focusOrigin = focusHeldUlak && pickUp != null && pickUp.Holder != null
                ? pickUp.Holder
                : Animal.transform;
            Vector3 focusTargetPosition = focusHeldUlak && heldUlak != null
                ? heldUlak.position
                : carrier.position;
            float focusRange = isMainRival
                ? GetCloseRoleCarrierApproachDistance()
                : Mathf.Max(0.5f, carrierTakeoverFocusRange);
            float distance = HorizontalDistance(focusOrigin.position, focusTargetPosition);
            if (distance <= focusRange)
                pickupFocusElapsed += interval;
            else
                pickupFocusElapsed = 0f;

            if (pickupFocusElapsed >= pickupFocusRequired)
            {
                // Ownership callbacks are synchronous. Clear the coroutine handle
                // first so the callback cannot stop the currently executing routine.
                waitCoroutine = null;
                pickupRequestPending = true;
                bool transferred = manager.TryTransferUlakToAIRider(this);
                pickupRequestPending = false;
                ResetPickupFocusProgress();

                if (!transferred && isGameplayActive && !hasLamb &&
                    manager.currentGoatOwner != null)
                {
                    StartCarrierTakeoverFocus(manager.currentGoatOwner);
                }
                yield break;
            }

            yield return wait;
        }

        waitCoroutine = null;
        pickupRequestPending = false;
        ResetPickupFocusProgress();
    }

    private bool CanAttemptCarrierTakeover(GameObject ownerRoot)
    {
        if (!isGameplayActive || hasLamb || ownerRoot == null ||
            ownerRoot == transform.root.gameObject || !CanTakeCarriedUlak)
        {
            return false;
        }

        KopkariManager manager = KopkariManager.Instance;
        return manager != null && manager.currentGoatOwner == ownerRoot;
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
        if (isFakeUlakDistracted)
        {
            CancelPickupFocus();
            return;
        }

        if (focusedObject == null)
        {
            CancelPickupFocus();

            // MPickUp fires this event when its pickup trigger leaves the Ulak.
            // Reissue the live Ulak target so a pushed-away competitor returns
            // instead of remaining stopped at its previous pickup position.
            // Has_Item is already true when Malbers clears focus after a
            // successful pickup, so the new carrier keeps its Salym target.
            if (isGameplayActive &&
                ulakRole == UlakRole.Competitor &&
                !hasLamb &&
                pickUp != null &&
                !pickUp.Has_Item)
            {
                ScheduleReturnToUlak();
            }

            return;
        }

        GameObject carrierOwner = KopkariManager.Instance != null
            ? KopkariManager.Instance.currentGoatOwner
            : null;
        bool canUsePickupFlow = ulakRole == UlakRole.Competitor ||
                                (ulakRole == UlakRole.Guard &&
                                 CanAttemptCarrierTakeover(carrierOwner));
        if (!canUsePickupFlow || !isGameplayActive)
        {
            CancelPickupFocus();
            return;
        }

        CancelReturnToUlak();

        if (!hasLamb && !pickupRequestPending && waitCoroutine == null && pickUp != null && !pickUp.Has_Item)
            waitCoroutine = StartCoroutine(WaitToPickUpLamb());
    }

    private void HandleItemPicked(GameObject pickedObject)
    {
        if (!isGameplayActive || !CanTakeCarriedUlak || pickedObject == null)
            return;

        bool gripWasInitialized = hasLamb && gripContactCoroutine != null;
        pickupRequestPending = false;
        ResetPickupFocusProgress();
        hasLamb = true;
        StartCarrierEscapeSpeedBoost();
        if (!gripWasInitialized)
        {
            ResetCarrierGrip(clearProtection: false);
            gripProtectionUntil = Time.time + Mathf.Max(0f, gripPickupProtectionDuration);
            StartGripContactMonitoring();
        }
        CancelReturnToUlak();
        CancelCarrierEscape();
        ApplyCarrierNavigationAvoidance();
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

    private void StartGuardingUlak()
    {
        StopGuardingUlak();
        if (isGameplayActive && ulakRole == UlakRole.Guard)
            guardCoroutine = StartCoroutine(GuardUlak());
    }

    private void StopGuardingUlak()
    {
        if (guardCoroutine == null)
            return;

        StopCoroutine(guardCoroutine);
        guardCoroutine = null;
    }

    private IEnumerator GuardUlak()
    {
        if (ai == null || Animal == null)
        {
            guardCoroutine = null;
            yield break;
        }

        WaitForSeconds repathWait = new WaitForSeconds(Mathf.Max(0.1f, guardRepathInterval));
        float angle = 360f * orbitSlotIndex / Mathf.Max(1, orbitSlotCount);
        Vector3 lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);

        while (isGameplayActive && ulakRole == UlakRole.Guard)
        {
            KopkariManager manager = KopkariManager.Instance;
            GameObject liveCarrierOwner = manager != null ? manager.currentGoatOwner : null;
            if (liveCarrierOwner != guardCarrierOwner)
            {
                if (liveCarrierOwner != null && liveCarrierOwner != transform.root.gameObject)
                {
                    EnablePickupInteraction();
                    guardCarrierOwner = liveCarrierOwner;
                    Transform liveCarrier = manager.ResolveGoatOwnerTarget(liveCarrierOwner);
                    guardEngagementTarget = GetCarriedUlakApproachTarget(liveCarrier);
                    guardEngagementAIRider = null;
                    guardEngagementDeadline = float.PositiveInfinity;
                    ApplyGuardCarrierNavigationAvoidance(liveCarrierOwner);
                }
                else if (guardCarrierOwner != null)
                {
                    ClearGuardEngagement();
                    ApplyBaseNavigationAvoidance();
                    CancelPickupFocus();
                    if (pickUp != null && !pickUp.Has_Item)
                        pickUp.enabled = false;
                }

                lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);
            }

            Transform ulak = GetCurrentUlakTransform();
            if (ulak != null)
            {
                if (guardEngagementTarget == null &&
                    TryFindFocusedAIRider(out AIKopkariRider focusedRider, out Transform focusedTarget))
                {
                    BeginGuardEngagement(focusedTarget, focusedRider);
                }

                bool isEngaging = TryGetActiveGuardEngagement(out Transform engagementTarget);
                Vector3 destination;
                float stoppingDistance;

                if (isEngaging)
                {
                    if (guardCarrierOwner != null)
                    {
                        Transform carrier = manager != null
                            ? manager.ResolveGoatOwnerTarget(guardCarrierOwner)
                            : guardCarrierOwner.transform;
                        Transform approachTarget = GetCarriedUlakApproachTarget(carrier);
                        ai.StoppingDistance = 0f;
                        if (ai.Target != approachTarget || ai.HasArrived || !ai.IsMoving)
                            ai.SetTarget(approachTarget, true);
                        if (ai.Agent != null)
                            ai.CurrentStoppingDistance = 0f;
                        StartCarrierTakeoverFocus(guardCarrierOwner);
                        TryGuardRiderMeleeAttack(approachTarget);
                        TryApplyGuardCarrierContact(manager, guardCarrierOwner, carrier);
                        lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);
                        yield return repathWait;
                        continue;
                    }
                    else
                    {
                        destination = engagementTarget.position;
                        stoppingDistance = Mathf.Clamp(guardEngageStoppingDistance, 0.25f, 1.25f);
                        TryAttackFocusedRider(engagementTarget);
                    }
                }
                else
                {
                    // Opposite slots are only idle/home positions. A Guard leaves
                    // this slot while engaging and returns after focus is lost.
                    Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    destination = ulak.position + radial * Mathf.Max(0.5f, guardRadius);
                    stoppingDistance = Mathf.Max(0.05f, guardStoppingDistance);
                }

                Vector3 destinationDelta = destination - lastDestination;
                destinationDelta.y = 0f;
                Vector3 guardDelta = destination - Animal.transform.position;
                guardDelta.y = 0f;

                float repathThreshold = Mathf.Max(0.1f, guardRepathDistance);
                float returnThreshold = stoppingDistance + repathThreshold;
                if (destinationDelta.sqrMagnitude >= repathThreshold * repathThreshold ||
                    guardDelta.sqrMagnitude >= returnThreshold * returnThreshold)
                {
                    SetGuardDestination(destination, stoppingDistance);
                    lastDestination = destination;
                }
            }

            yield return repathWait;
        }

        guardCoroutine = null;
    }

    private void SetGuardDestination(Vector3 destination, float stoppingDistance)
    {
        float closeDistance = Mathf.Max(0.05f, stoppingDistance);
        ai.StoppingDistance = closeDistance;
        ai.SetDestinationClearTarget(destination);

        // MAnimalAIControl.SetDestination replaces the requested target distance
        // with its serialized PointStoppingDistance. Re-apply the role-specific
        // value so Guards do not stop several metres short of the Uloq.
        if (ai.Agent != null)
            ai.CurrentStoppingDistance = closeDistance;
    }

    private void TryApplyGuardCarrierContact(
        KopkariManager manager,
        GameObject ownerRoot,
        Transform carrier)
    {
        if (manager == null || ownerRoot == null || carrier == null || Animal == null)
            return;

        Transform approachOrigin = GetCloseRoleApproachOrigin();
        Transform approachTarget = GetCarriedUlakApproachTarget(carrier);
        float contactDistance = Mathf.Max(0.1f, guardCarrierContactDistance);
        if (HorizontalDistance(approachOrigin.position, approachTarget.position) > contactDistance)
            return;

        GameObject guardRoot = transform.root.gameObject;
        if (manager.IsLocalRiderTransform(ownerRoot.transform))
        {
            manager.ApplyGuardContactDamage(guardRoot);
            return;
        }

        AIKopkariRider carrierRider = ownerRoot.GetComponentInChildren<AIKopkariRider>(true);
        if (carrierRider != null && carrierRider != this)
            carrierRider.ApplyGripDamage(GripDamageSource.GuardContact, guardRoot);
    }

    #region Trap Setter
    private void StartTrapSetterRole()
    {
        StopTrapSetterRole();
        if (!isGameplayActive || ulakRole != UlakRole.TrapSetter || ai == null || Animal == null)
            return;

        trapSetterSideSign = Random.value < 0.5f ? -1f : 1f;
        trapSetterCoroutine = StartCoroutine(TrapSetterRoutine());
    }

    private void StopTrapSetterRole()
    {
        if (trapSetterCoroutine != null)
        {
            StopCoroutine(trapSetterCoroutine);
            trapSetterCoroutine = null;
        }

        trapSetterCarrierOwner = null;
        trapSetterTravel = 0f;
        trapSetterLastTravelPosition = Vector3.zero;
        trapSetterContactLatched = false;
    }

    private IEnumerator TrapSetterRoutine()
    {
        WaitForSeconds repathWait = new WaitForSeconds(Mathf.Max(0.2f, trapSetterRepathInterval));
        Vector3 lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);
        while (isGameplayActive && !isFinished && ulakRole == UlakRole.TrapSetter)
        {
            KopkariManager manager = KopkariManager.Instance;
            GameObject ownerRoot = manager != null ? manager.currentGoatOwner : null;
            Transform carrier = ownerRoot != null && manager != null
                ? manager.ResolveGoatOwnerTarget(ownerRoot)
                : null;

            if (ownerRoot != trapSetterCarrierOwner)
            {
                ResetTrapSetterCarrierProgress(ownerRoot, carrier);
                lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);
            }

            if (carrier == null)
            {
                trapSetterContactLatched = false;
                Transform ulak = GetCurrentUlakTransform();
                Transform waitingFinish = manager != null ? manager.FirstSalymPosition : null;
                if (ulak != null)
                {
                    Vector3 towardFinish = waitingFinish != null
                        ? waitingFinish.position - ulak.position
                        : ulak.forward;
                    towardFinish.y = 0f;
                    if (towardFinish.sqrMagnitude < 0.01f)
                    {
                        towardFinish = ulak.forward;
                        towardFinish.y = 0f;
                    }
                    if (towardFinish.sqrMagnitude < 0.01f)
                        towardFinish = Vector3.forward;
                    towardFinish.Normalize();

                    Vector3 right = Vector3.Cross(Vector3.up, towardFinish);
                    Vector3 requested = ulak.position +
                                        towardFinish * Mathf.Max(1f, trapSetterWaitingDistance) +
                                        right * (trapSetterSideSign * Mathf.Max(0f, trapLateralOffset));
                    if (TrySampleTrapPoint(requested, out Vector3 waitingDestination))
                    {
                        Vector3 destinationDelta = waitingDestination - lastDestination;
                        destinationDelta.y = 0f;
                        float stoppingDistance = Mathf.Max(0.5f, trapSetterStoppingDistance);
                        bool wasDisplaced = ai.HasArrived &&
                                            HorizontalDistance(Animal.transform.position, waitingDestination) >
                                            stoppingDistance + 0.75f;
                        if (destinationDelta.sqrMagnitude >= 1f || wasDisplaced)
                        {
                            ai.StoppingDistance = stoppingDistance;
                            ai.SetDestination(waitingDestination, true);
                            lastDestination = waitingDestination;
                        }
                    }
                }

                yield return repathWait;
                continue;
            }

            Transform finish = manager != null ? manager.FirstSalymPosition : null;
            if (finish != null &&
                TryGetTrapSetterDestination(carrier.position, finish.position, out Vector3 destination,
                    out Vector3 routeDirection, out float distanceToFinish))
            {
                if (!trapSetterContactLatched)
                {
                    // Close directly on the live carrier until actual contact.
                    ai.StoppingDistance = Mathf.Max(0.05f, carrierChaseStoppingDistance);
                    Transform approachTarget = GetCarriedUlakApproachTarget(carrier);
                    ai.SetTarget(approachTarget, true);
                    lastDestination = approachTarget.position;
                }
                else
                {
                    Vector3 destinationDelta = destination - lastDestination;
                    destinationDelta.y = 0f;
                    if (destinationDelta.sqrMagnitude >= 0.25f)
                    {
                        ai.StoppingDistance = Mathf.Max(0.1f, trapSetterStoppingDistance);
                        ai.SetDestination(destination, true);
                        lastDestination = destination;
                    }
                }

                AccumulateTrapSetterTravel(Animal.transform.position);
                TryApplyTrapSetterContact(manager, ownerRoot, carrier);

                if (trapSetterTravel >= Mathf.Max(5f, trapPlacementDistance) &&
                    distanceToFinish > Mathf.Max(1f, trapFinishExclusionRadius))
                {
                    if (TryPlaceTrap(routeDirection))
                    {
                        trapSetterTravel = 0f;
                        trapSetterSideSign = Random.value < 0.5f ? -1f : 1f;
                    }
                }
            }
            else
            {
                ai.StoppingDistance = Mathf.Max(0.05f, carrierChaseStoppingDistance);
                ai.SetTarget(GetCarriedUlakApproachTarget(carrier), true);
                TryApplyTrapSetterContact(manager, ownerRoot, carrier);
            }

            yield return repathWait;
        }

        trapSetterCoroutine = null;
    }

    private void ResetTrapSetterCarrierProgress(GameObject ownerRoot, Transform carrier)
    {
        trapSetterCarrierOwner = ownerRoot;
        trapSetterTravel = 0f;
        trapSetterLastTravelPosition = Animal != null ? Animal.transform.position : Vector3.zero;
        trapSetterSideSign = Random.value < 0.5f ? -1f : 1f;
        trapSetterContactLatched = false;
    }

    private void AccumulateTrapSetterTravel(Vector3 setterPosition)
    {
        if (trapSetterLastTravelPosition == Vector3.zero)
        {
            trapSetterLastTravelPosition = setterPosition;
            return;
        }

        float moved = HorizontalDistance(setterPosition, trapSetterLastTravelPosition);
        // Ignore teleports and round resets; normal movement remains accumulated.
        if (moved <= Mathf.Max(5f, trapPlacementDistance * 0.5f))
            trapSetterTravel += moved;

        trapSetterLastTravelPosition = setterPosition;
    }

    private void TryApplyTrapSetterContact(
        KopkariManager manager,
        GameObject ownerRoot,
        Transform carrier)
    {
        if (manager == null || ownerRoot == null || carrier == null || Animal == null)
            return;

        float contactDistance = Mathf.Min(
            Mathf.Max(0.1f, trapSetterContactDistance),
            GetCloseRoleCarrierApproachDistance());
        float resetDistance = Mathf.Max(contactDistance + 0.25f, trapSetterContactResetDistance);
        Transform approachOrigin = GetCloseRoleApproachOrigin();
        Transform approachTarget = GetCarriedUlakApproachTarget(carrier);
        float distance = HorizontalDistance(approachOrigin.position, approachTarget.position);
        if (distance > resetDistance)
        {
            trapSetterContactLatched = false;
            return;
        }

        if (trapSetterContactLatched || distance > contactDistance)
            return;

        bool damageApplied;
        if (manager.IsLocalRiderTransform(ownerRoot.transform))
        {
            damageApplied = manager.ApplyTrapSetterContactDamage(transform.root.gameObject);
        }
        else
        {
            AIKopkariRider carrierRider = ownerRoot.GetComponentInChildren<AIKopkariRider>(true);
            damageApplied = carrierRider != null && carrierRider != this &&
                            carrierRider.ApplyGripDamage(
                                GripDamageSource.TrapSetterContact,
                                transform.root.gameObject);
        }

        if (damageApplied)
            trapSetterContactLatched = true;
    }

    private bool TryGetTrapSetterDestination(
        Vector3 origin,
        Vector3 finish,
        out Vector3 destination,
        out Vector3 routeDirection,
        out float distanceToFinish)
    {
        Vector3 toFinish = finish - origin;
        toFinish.y = 0f;
        distanceToFinish = toFinish.magnitude;
        routeDirection = distanceToFinish > 0.01f ? toFinish / distanceToFinish : Animal.transform.forward;
        routeDirection.y = 0f;
        routeDirection.Normalize();

        float usableLead = trapSetterContactLatched
            ? Mathf.Max(0f, Mathf.Min(
                Mathf.Min(trapLeadDistance, 1.5f),
                distanceToFinish - Mathf.Max(1f, trapFinishExclusionRadius)))
            : 0f;
        Vector3 right = Vector3.Cross(Vector3.up, routeDirection);
        float lateral = trapSetterContactLatched
            ? trapSetterSideSign * Mathf.Min(0.75f, Mathf.Max(0f, trapLateralOffset))
            : 0f;
        Vector3 requested = origin + routeDirection * usableLead +
                            right * lateral;
        return TrySampleTrapPoint(requested, out destination);
    }

    private bool TryPlaceTrap(Vector3 routeDirection)
    {
        GameObject prefab = SelectTrapPrefab();
        if (prefab == null)
        {
            if (!trapSetterMissingPrefabWarningShown)
            {
                trapSetterMissingPrefabWarningShown = true;
                Debug.LogWarning(
                    $"[{nameof(AIKopkariRider)}] Trap Setter '{name}' has no trap prefab. " +
                    "Assign Random Trap Prefabs on KopkariIntroFlowController or a Walk Zone on BoostersContainer.",
                    this);
            }
            return false;
        }

        Vector3 requested = Animal.transform.position -
                            routeDirection * Mathf.Max(0f, trapDropBehindDistance);
        if (!TrySampleTrapPoint(requested, out Vector3 spawnPosition))
            return false;

        Quaternion rotation = routeDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(routeDirection, Vector3.up)
            : Quaternion.identity;
        SimplePool.Spawn(prefab, spawnPosition, rotation, lifeTime: Mathf.Max(1f, trapLifetime));
        return true;
    }

    private GameObject SelectTrapPrefab()
    {
        if (trapSetterPrefabs.Count == 0)
        {
            if (boosterContainer != null && boosterContainer.walkzonePrefab != null)
                return boosterContainer.walkzonePrefab;

            return KopkariManager.Instance != null
                ? KopkariManager.Instance.walkZonePrefab
                : null;
        }

        int index = Random.Range(0, trapSetterPrefabs.Count);
        if (trapSetterPrefabs.Count > 1 && index == lastTrapPrefabIndex)
            index = (index + Random.Range(1, trapSetterPrefabs.Count)) % trapSetterPrefabs.Count;

        lastTrapPrefabIndex = index;
        return trapSetterPrefabs[index];
    }

    private bool TrySampleTrapPoint(Vector3 requested, out Vector3 sampled)
    {
        int areaMask = ai != null && ai.Agent != null ? ai.Agent.areaMask : NavMesh.AllAreas;
        if (NavMesh.SamplePosition(requested, out NavMeshHit hit,
                Mathf.Max(0.1f, trapNavMeshSampleRadius), areaMask))
        {
            sampled = hit.position;
            return true;
        }

        sampled = requested;
        return false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
    #endregion

    private void HandleFakeUlakDiversionStarted(Transform finalTarget)
    {
        if (!isGameplayActive || isFinished || finalTarget == null || ai == null)
            return;

        KopkariManager manager = KopkariManager.Instance;
        GameObject liveOwner = manager != null ? manager.currentGoatOwner : null;
        bool isActualCarrier = hasLamb ||
                               (liveOwner != null && liveOwner == transform.root.gameObject);
        if (isActualCarrier)
            return;

        isFakeUlakDistracted = true;
        fakeUlakDiversionTarget = finalTarget;

        CancelPickupFocus();
        CancelReturnToUlak();
        StopUlakRecovery();
        CancelCarrierEscape();
        StopChaseObstacleSteering();
        ClearGuardEngagement();
        StopGuardRiderMeleeObject();
        StopGuardingUlak();
        StopTrapSetterRole();

        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }

        ApplyConfiguredGameplaySpeed();
        ApplyBaseNavigationAvoidance();
        ai.StoppingDistance = Mathf.Max(0.1f, salymStoppingDistance);
        ai.SetTarget(fakeUlakDiversionTarget, true);
    }

    private void HandleFakeUlakDiversionEnded()
    {
        if (!isFakeUlakDistracted)
            return;

        isFakeUlakDistracted = false;
        fakeUlakDiversionTarget = null;

        if (!isGameplayActive || isFinished)
            return;

        KopkariManager manager = KopkariManager.Instance;
        GameObject liveOwner = manager != null ? manager.currentGoatOwner : null;
        HandleGoatOwnerChanged(liveOwner);

        if (ulakRole == UlakRole.Competitor && !hasLamb)
            StartUlakRecovery();
    }

    private void HandleGoatOwnerChanged(GameObject ownerRoot)
    {
        if (!isGameplayActive || isFinished)
            return;

        KopkariManager diversionManager = KopkariManager.Instance;
        bool isThisRiderNow = ownerRoot != null && ownerRoot == transform.root.gameObject;
        if (!isFakeUlakDistracted && !isThisRiderNow &&
            diversionManager != null && diversionManager.IsFakeUlakDiversionActive)
        {
            HandleFakeUlakDiversionStarted(diversionManager.CurrentTargetPosition);
            return;
        }

        if (isFakeUlakDistracted)
        {
            bool becameActualCarrier = ownerRoot != null &&
                                       ownerRoot == transform.root.gameObject;
            if (!becameActualCarrier)
                return;

            isFakeUlakDistracted = false;
            fakeUlakDiversionTarget = null;
        }

        if (ulakRole == UlakRole.Guard)
        {
            CancelPickupFocus();
            CancelCarrierEscape();
            StopChaseObstacleSteering();

            bool isThisGuard = ownerRoot != null && ownerRoot == transform.root.gameObject;
            if (isThisGuard)
            {
                ClearGuardEngagement();
                StopGuardingUlak();
                EnablePickupInteraction();
                hasCarrierHistory = true;
                hasLamb = true;
                StartCarrierEscapeSpeedBoost();
                ApplyCarrierNavigationAvoidance();
                MoveOwnerToFirstSalym();
                return;
            }

            if (hasLamb)
            {
                hasLamb = false;
                StopCarrierEscapeSpeedBoost();
                StopGripContactMonitoring();
                StopItemTimer();
            }

            if (ownerRoot != null && ownerRoot != transform.root.gameObject)
            {
                EnablePickupInteraction();
                ApplyConfiguredGameplaySpeed();
                ApplyGuardCarrierNavigationAvoidance(ownerRoot);
                guardCarrierOwner = ownerRoot;
                KopkariManager manager = KopkariManager.Instance;
                Transform carrierTarget = manager != null
                    ? manager.ResolveGoatOwnerTarget(ownerRoot)
                    : ownerRoot.transform;
                guardEngagementTarget = GetCarriedUlakApproachTarget(carrierTarget);
                guardEngagementAIRider = null;
                guardEngagementDeadline = float.PositiveInfinity;
                MoveToCarrier(ownerRoot);
                StartCarrierTakeoverFocus(ownerRoot);
            }
            else
            {
                ClearGuardEngagement();
                ApplyConfiguredGameplaySpeed();
                ApplyBaseNavigationAvoidance();
                if (pickUp != null && !pickUp.Has_Item)
                    pickUp.enabled = false;
            }

            if (guardCoroutine == null)
                StartGuardingUlak();
            return;
        }

        if (ulakRole == UlakRole.TrapSetter)
        {
            ApplyConfiguredGameplaySpeed();
            CancelPickupFocus();
            CancelCarrierEscape();
            StopChaseObstacleSteering();
            if (ownerRoot != null)
                ApplyChaseNavigationAvoidance();
            else
                ApplyBaseNavigationAvoidance();
            KopkariManager manager = KopkariManager.Instance;
            Transform carrier = ownerRoot != null && manager != null
                ? manager.ResolveGoatOwnerTarget(ownerRoot)
                : null;
            ResetTrapSetterCarrierProgress(ownerRoot, carrier);
            if (trapSetterCoroutine == null)
                StartTrapSetterRole();
            return;
        }

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
            CancelPickupFocus();
            hasLamb = true;
            StartCarrierEscapeSpeedBoost();
            StopChaseObstacleSteering();
            ApplyCarrierNavigationAvoidance();
            MoveOwnerToFirstSalym();
            return;
        }

        CancelCarrierEscape();
        CancelPickupFocus();

        if (hasLamb)
        {
            hasLamb = false;
            StopCarrierEscapeSpeedBoost();
            StopGripContactMonitoring();
            StopItemTimer();
        }

        ApplyConfiguredGameplaySpeed();

        if (ownerRoot != null)
            ApplyChaseNavigationAvoidance();
        else
            ApplyBaseNavigationAvoidance();

        if (ownerRoot != null)
            StartChaseObstacleSteering();
        else
            StopChaseObstacleSteering();

        // A dropped Uloq must always restart the Orbit role. hasCarrierHistory
        // remains useful for carrier/chase behavior, but must not permanently
        // block the orbit coroutine after the first pickup of the round.
        if (ownerRoot == null && ulakRole == UlakRole.Orbit)
            orbitCoroutine = StartCoroutine(OrbitUlak());
        else if (ownerRoot != null)
        {
            MoveToCarrier(ownerRoot);
            StartCarrierTakeoverFocus(ownerRoot);
        }
        else
            MoveToCurrentUlak();
    }

    private void MoveOwnerToFirstSalym()
    {
        if (ai == null)
            return;

        if (TryBeginCarrierEscape())
            return;

        MoveOwnerDirectlyToFirstSalym();
    }

    private void MoveOwnerDirectlyToFirstSalym()
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

    private bool TryBeginCarrierEscape()
    {
        if (!useCarrierEscapeWaypoint || carrierEscapeCompletedForCurrentHold)
            return false;
        if (isCarrierEscaping)
            return true;

        Transform salym = KopkariManager.Instance != null
            ? KopkariManager.Instance.FirstSalymPosition
            : null;
        Transform horseTransform = Animal != null ? Animal.transform : transform.root;
        NavMeshAgent agent = ai != null ? ai.Agent : null;

        if (salym == null || horseTransform == null || agent == null ||
            !agent.enabled || !agent.isOnNavMesh)
        {
            carrierEscapeCompletedForCurrentHold = true;
            return false;
        }

        Vector3 forward = salym.position - horseTransform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = horseTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float preferredSide = (GetInstanceID() & 1) == 0 ? 1f : -1f;

        if (!TryFindCarrierEscapePoint(
                horseTransform.position,
                forward,
                right,
                preferredSide,
                agent,
                out Vector3 destination) &&
            !TryFindCarrierEscapePoint(
                horseTransform.position,
                forward,
                right,
                -preferredSide,
                agent,
                out destination) &&
            !TryFindCarrierEscapePoint(
                horseTransform.position,
                forward,
                right,
                0f,
                agent,
                out destination))
        {
            carrierEscapeCompletedForCurrentHold = true;
            return false;
        }

        if (carrierEscapeTarget == null)
        {
            GameObject targetObject = new GameObject($"{name} Carrier Escape Target");
            targetObject.hideFlags = HideFlags.HideInHierarchy;
            carrierEscapeTarget = targetObject.transform;
        }

        carrierEscapeTarget.SetPositionAndRotation(destination, Quaternion.LookRotation(forward, Vector3.up));
        isCarrierEscaping = true;
        ai.StoppingDistance = carrierEscapeStoppingDistance;
        ai.SetTarget(carrierEscapeTarget, true);
        return true;
    }

    private bool TryFindCarrierEscapePoint(
        Vector3 origin,
        Vector3 forward,
        Vector3 right,
        float sideSign,
        NavMeshAgent agent,
        out Vector3 destination)
    {
        Vector3 candidate = origin + forward * carrierEscapeDistance +
                            right * (carrierEscapeSideOffset * sideSign);
        if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                carrierEscapeNavMeshSampleRadius,
                agent.areaMask))
        {
            destination = default;
            return false;
        }

        if (carrierEscapePath == null)
            carrierEscapePath = new NavMeshPath();
        else
            carrierEscapePath.ClearCorners();

        if (!agent.CalculatePath(hit.position, carrierEscapePath) ||
            carrierEscapePath.status != NavMeshPathStatus.PathComplete)
        {
            destination = default;
            return false;
        }

        destination = hit.position;
        return true;
    }

    private bool IsCurrentUlakCarrier()
    {
        if (!hasLamb)
            return false;

        KopkariManager manager = KopkariManager.Instance;
        return manager == null ||
               manager.currentGoatOwner == null ||
               manager.currentGoatOwner == transform.root.gameObject;
    }

    private void CancelCarrierEscape()
    {
        isCarrierEscaping = false;
        carrierEscapeCompletedForCurrentHold = false;
    }

    private void StartChaseObstacleSteering()
    {
        if (!ShouldRunChaseObstacleSteering() || chaseAvoidanceCoroutine != null)
            return;

        ResetChaseProgressTracking();
        chaseAvoidanceCoroutine = StartCoroutine(ChaseObstacleSteeringRoutine());
    }

    private void StopChaseObstacleSteering()
    {
        if (chaseAvoidanceCoroutine != null)
        {
            StopCoroutine(chaseAvoidanceCoroutine);
            chaseAvoidanceCoroutine = null;
        }

        isUsingChaseDetour = false;
        ResetChaseProgressTracking();
    }

    private bool ShouldRunChaseObstacleSteering()
    {
        if (!useChaseObstacleSteering || !isGameplayActive || isFinished || hasLamb)
            return false;

        KopkariManager manager = KopkariManager.Instance;
        GameObject owner = manager != null ? manager.currentGoatOwner : null;
        return owner != null && owner != transform.root.gameObject;
    }

    private IEnumerator ChaseObstacleSteeringRoutine()
    {
        float interval = Mathf.Max(0.25f, chaseAvoidanceInterval);
        float stagger01 = (GetInstanceID() & 3) * 0.25f;
        if (stagger01 > 0f)
            yield return new WaitForSeconds(interval * stagger01);

        WaitForSeconds wait = new WaitForSeconds(interval);
        chaseDetourSideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;

        while (ShouldRunChaseObstacleSteering())
        {
            EvaluateChaseObstacleSteering();
            yield return wait;
        }

        isUsingChaseDetour = false;
        ResetChaseProgressTracking();
        chaseAvoidanceCoroutine = null;
    }

    private void EvaluateChaseObstacleSteering()
    {
        Transform horseTransform = Animal != null ? Animal.transform : transform.root;
        KopkariManager manager = KopkariManager.Instance;
        GameObject owner = manager != null ? manager.currentGoatOwner : null;
        Transform pursuitTarget = owner != null && manager != null
            ? manager.ResolveGoatOwnerTarget(owner)
            : GetCurrentUlakTransform();
        if (horseTransform == null || pursuitTarget == null)
            return;

        Vector3 toTarget = pursuitTarget.position - horseTransform.position;
        toTarget.y = 0f;
        float engagementDistance = Mathf.Max(0.5f, chaseEngagementDistance);
        if (toTarget.sqrMagnitude <= engagementDistance * engagementDistance)
        {
            ResetChaseProgressTracking();
            if (isUsingChaseDetour)
            {
                isUsingChaseDetour = false;
                if (owner != null)
                    MoveToCarrier(owner);
                else
                    MoveToCurrentUlak();
            }
            return;
        }

        if (isUsingChaseDetour || toTarget.sqrMagnitude < 0.0001f)
            return;

        if (!HasChaserBeenStuck(horseTransform.position) || Time.time < nextChaseDetourTime)
            return;

        Vector3 pursuitDirection = toTarget.normalized;
        if (!HasBlockingObjectAhead(horseTransform, pursuitDirection))
            return;

        NavMeshAgent agent = ai != null ? ai.Agent : null;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 right = Vector3.Cross(Vector3.up, pursuitDirection).normalized;
        if (!TryFindChaseDetourPoint(
                horseTransform.position,
                pursuitDirection,
                right,
                chaseDetourSideSign,
                agent,
                out Vector3 destination) &&
            !TryFindChaseDetourPoint(
                horseTransform.position,
                pursuitDirection,
                right,
                -chaseDetourSideSign,
                agent,
                out destination))
            return;

        chaseDetourSideSign = -chaseDetourSideSign;
        nextChaseDetourTime = Time.time + Mathf.Max(0f, chaseDetourCooldown);

        if (chaseDetourTarget == null)
        {
            GameObject targetObject = new GameObject($"{name} Chase Detour Target");
            targetObject.hideFlags = HideFlags.HideInHierarchy;
            chaseDetourTarget = targetObject.transform;
        }

        chaseDetourTarget.SetPositionAndRotation(
            destination,
            Quaternion.LookRotation(pursuitDirection, Vector3.up));
        isUsingChaseDetour = true;
        ResetChaseProgressTracking();
        ai.StoppingDistance = 0f;
        ai.SetTarget(chaseDetourTarget, true);
    }

    private bool HasChaserBeenStuck(Vector3 currentPosition)
    {
        float now = Time.time;
        if (!chaseProgressInitialized)
        {
            chaseProgressInitialized = true;
            chaseLastProgressPosition = currentPosition;
            chaseLastProgressTime = now;
            chaseStuckElapsed = 0f;
            return false;
        }

        Vector3 movement = currentPosition - chaseLastProgressPosition;
        movement.y = 0f;
        float elapsed = Mathf.Max(0f, now - chaseLastProgressTime);
        float movementThreshold = Mathf.Max(0.05f, chaseStuckMovementThreshold);

        if (movement.sqrMagnitude >= movementThreshold * movementThreshold)
            chaseStuckElapsed = 0f;
        else
            chaseStuckElapsed += elapsed;

        chaseLastProgressPosition = currentPosition;
        chaseLastProgressTime = now;

        if (chaseStuckElapsed < Mathf.Max(0.25f, chaseStuckDuration))
            return false;

        chaseStuckElapsed = 0f;
        return true;
    }

    private void ResetChaseProgressTracking()
    {
        chaseProgressInitialized = false;
        chaseLastProgressPosition = Vector3.zero;
        chaseLastProgressTime = 0f;
        chaseStuckElapsed = 0f;
    }

    private bool HasBlockingObjectAhead(Transform horseTransform, Vector3 pursuitDirection)
    {
        if (chaseProbeHits == null || chaseProbeHits.Length < 12)
            chaseProbeHits = new RaycastHit[12];

        Vector3 origin = horseTransform.position + Vector3.up * chaseProbeHeight +
                         pursuitDirection * chaseProbeRadius;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            chaseProbeRadius,
            pursuitDirection,
            chaseProbeHits,
            chaseProbeDistance,
            chaseAvoidanceLayers,
            QueryTriggerInteraction.Ignore);

        Transform ownRoot = transform.root;
        GameObject owner = KopkariManager.Instance != null
            ? KopkariManager.Instance.currentGoatOwner
            : null;

        bool foundBlocker = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = chaseProbeHits[i].collider;
            if (hitCollider == null)
                continue;

            Transform hitRoot = hitCollider.transform.root;
            if (hitRoot == ownRoot)
                continue;

            // The carrier is the desired engagement target, never an obstacle to avoid.
            if (owner != null && hitRoot.gameObject == owner)
                return false;

            foundBlocker = true;
        }

        return foundBlocker;
    }

    private bool TryFindChaseDetourPoint(
        Vector3 origin,
        Vector3 pursuitDirection,
        Vector3 right,
        float sideSign,
        NavMeshAgent agent,
        out Vector3 destination)
    {
        Vector3 candidate = origin + pursuitDirection * chaseDetourForwardDistance +
                            right * (chaseDetourSideDistance * sideSign);
        if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                chaseDetourNavMeshSampleRadius,
                agent.areaMask))
        {
            destination = default;
            return false;
        }

        if (chaseDetourPath == null)
            chaseDetourPath = new NavMeshPath();
        else
            chaseDetourPath.ClearCorners();

        if (!agent.CalculatePath(hit.position, chaseDetourPath) ||
            chaseDetourPath.status != NavMeshPathStatus.PathComplete)
        {
            destination = default;
            return false;
        }

        destination = hit.position;
        return true;
    }

    private void MoveToCurrentUlak(bool returningAfterZoneExit = false)
    {
        if (isFakeUlakDistracted)
            return;

        isUsingChaseDetour = false;

        if (ai == null)
            return;

        Transform ulak = GetCurrentUlakTransform();
        if (ulak == null)
            return;

        ai.StoppingDistance = returningAfterZoneExit
            ? ulakReturnStoppingDistance
            : movingUlakStoppingDistance;
        // Keep the live Ulak Transform as the Malbers target. A one-shot world
        // position becomes stale after the Ulak moves and does not reliably
        // resume an AI that had already reached/stopped at the pickup area.
        ai.SetTarget(ulak, true);

        if (ai.Agent != null && (!ai.Agent.enabled || !ai.Agent.isOnNavMesh))
        {
            Debug.LogWarning(
                $"[{nameof(AIKopkariRider)}] '{name}' cannot move to Ulak because its NavMeshAgent " +
                "is not active on the baked NavMesh.",
                this);
        }
    }

    private void MoveToCarrier(GameObject ownerRoot)
    {
        if (isFakeUlakDistracted)
            return;

        if (ai == null || ownerRoot == null)
            return;

        KopkariManager manager = KopkariManager.Instance;
        Transform carrier = manager != null
            ? manager.ResolveGoatOwnerTarget(ownerRoot)
            : ownerRoot.transform;
        if (carrier == null)
            return;

        Transform heldUlak = GetCurrentUlakTransform();
        ai.StoppingDistance = 0f;
        ai.SetTarget(heldUlak != null ? heldUlak : carrier, true);
    }

    private void ScheduleReturnToUlak()
    {
        if (isFakeUlakDistracted || !isGameplayActive || ulakRole != UlakRole.Competitor || hasLamb ||
            pickUp == null || pickUp.Has_Item)
            return;

        if (returnToUlakCoroutine != null)
            StopCoroutine(returnToUlakCoroutine);
        returnToUlakCoroutine = StartCoroutine(ReturnToUlakAfterPhysicsExit());
    }

    private void StartUlakRecovery()
    {
        if (isFakeUlakDistracted)
            return;

        StopUlakRecovery();
        ulakRecoveryCoroutine = StartCoroutine(UlakRecoveryRoutine());
    }

    private void StopUlakRecovery()
    {
        if (ulakRecoveryCoroutine == null)
            return;

        StopCoroutine(ulakRecoveryCoroutine);
        ulakRecoveryCoroutine = null;
    }

    private IEnumerator UlakRecoveryRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.25f, ulakRecoveryCheckInterval));
        while (isGameplayActive && ulakRole == UlakRole.Competitor)
        {
            KopkariManager manager = KopkariManager.Instance;
            Transform ulak = GetCurrentUlakTransform();
            if (!hasLamb && manager != null && manager.currentGoatOwner == null &&
                ulak != null && Animal != null && pickUp != null &&
                !pickUp.Has_Item)
            {
                Vector3 offset = ulak.position - Animal.transform.position;
                offset.y = 0f;
                float recoveryDistance = Mathf.Max(0.5f, ulakRecoveryDistance);
                NavMeshAgent agent = ai != null ? ai.Agent : null;
                bool needsTargetRepair = ai == null || ai.Target != ulak || ai.HasArrived || !ai.IsMoving ||
                                         agent == null || !agent.enabled || !agent.isOnNavMesh ||
                                         agent.velocity.sqrMagnitude < 0.04f;
                if (needsTargetRepair && offset.sqrMagnitude > recoveryDistance * recoveryDistance)
                {
                    // Repair a missed TriggerProxy exit as well as navigation.
                    if (pickUp.FocusedItem != null)
                    {
                        pickUp.FocusedItem.SetFocused(pickUp.Owner, false);
                        pickUp.FocusedItem = null;
                        CancelPickupFocus();
                    }
                    MoveToCurrentUlak(true);
                }
            }

            yield return wait;
        }

        ulakRecoveryCoroutine = null;
    }

    private IEnumerator ReturnToUlakAfterPhysicsExit()
    {
        yield return new WaitForFixedUpdate();
        yield return null;
        returnToUlakCoroutine = null;

        if (!isGameplayActive || ulakRole != UlakRole.Competitor || hasLamb ||
            pickUp == null || pickUp.Has_Item || pickUp.FocusedItem != null)
            yield break;

        MoveToCurrentUlak(true);
    }

    private void CancelReturnToUlak()
    {
        if (returnToUlakCoroutine == null)
            return;

        StopCoroutine(returnToUlakCoroutine);
        returnToUlakCoroutine = null;
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

    private Transform GetCurrentRoundWarmupPoint()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.CurrentWarmupPosition
            : null;
    }

    private IReadOnlyList<CheckpointTrigger> GetSharedCheckpoints()
    {
        return KopkariManager.Instance != null
            ? KopkariManager.Instance.Checkpoints
            : null;
    }

    public bool ApplyGripDamage(GripDamageSource source, GameObject attacker = null)
    {
        if (!isGameplayActive || !hasLamb || Time.time < gripProtectionUntil)
            return false;

        float damage = GetGripDamage(source);
        if (damage <= 0f)
            return false;

        GameObject attackerRoot = attacker != null && attacker.transform.root != null
            ? attacker.transform.root.gameObject
            : null;
        int attackerId = attackerRoot != null ? attackerRoot.GetInstanceID() : 0;
        int cooldownKey = unchecked(attackerId * 397 ^ ((int)source + 1) * 7919);
        if (nextGripDamageTimes.TryGetValue(cooldownKey, out float nextAllowedTime) &&
            Time.time < nextAllowedTime)
            return false;

        nextGripDamageTimes[cooldownKey] = Time.time + Mathf.Max(0.1f, gripDamageCooldown);
        currentGrip = Mathf.Max(0f, currentGrip - damage);
        OnCarrierGripChanged?.Invoke(this, currentGrip, MaximumGrip);
        if (currentGrip <= 0f)
            DropOwnedUlak();
        return true;
    }

    private float GetGripDamage(GripDamageSource source)
    {
        switch (source)
        {
            case GripDamageSource.WalkTrap:
                return Mathf.Max(0f, walkTrapGripDamage);
            case GripDamageSource.GuardRiderMelee:
                return Mathf.Max(0f, guardRiderMeleeGripDamage);
            case GripDamageSource.GuardHorseAttack:
                return Mathf.Max(0f, guardHorseAttackGripDamage);
            case GripDamageSource.GuardContact:
                return Mathf.Max(0f, guardContactGripDamage);
            case GripDamageSource.MainRivalSideAttack:
                return Mathf.Max(0f, mainRivalSideAttackGripDamage);
            case GripDamageSource.TrapSetterContact:
                return Mathf.Max(0f, trapSetterContactGripDamage);
            case GripDamageSource.OtherRiderContact:
                return Mathf.Max(0f, otherRiderContactGripDamage);
            case GripDamageSource.PlayerTouch:
                return Mathf.Max(0f, playerTouchGripDamage);
            default:
                return 0f;
        }
    }

    private void ResetCarrierGrip(bool clearProtection)
    {
        currentGrip = Mathf.Max(1f, maximumGrip);
        OnCarrierGripChanged?.Invoke(this, currentGrip, MaximumGrip);
        nextGripDamageTimes.Clear();
        if (clearProtection)
            gripProtectionUntil = 0f;
    }

    private void StartGripContactMonitoring()
    {
        StopGripContactMonitoring();
        if (isGameplayActive && hasLamb && Animal != null)
            gripContactCoroutine = StartCoroutine(GripContactRoutine());
    }

    private void StopGripContactMonitoring()
    {
        if (gripContactCoroutine == null)
            return;

        StopCoroutine(gripContactCoroutine);
        gripContactCoroutine = null;
    }

    private IEnumerator GripContactRoutine()
    {
        WaitForSeconds checkWait = new WaitForSeconds(Mathf.Max(0.6f, playerTouchCheckInterval));
        while (isGameplayActive && hasLamb && Animal != null)
        {
            KopkariManager manager = KopkariManager.Instance;
            if (manager != null && manager.currentGoatOwner == transform.root.gameObject)
            {
                Vector3 center = Animal.transform.TransformPoint(playerTouchCheckOffset);
                int count = Physics.OverlapSphereNonAlloc(
                    center,
                    Mathf.Max(0.25f, playerTouchCheckRadius),
                    gripContactBuffer,
                    gripContactLayers,
                    QueryTriggerInteraction.Ignore);

                bool contactDamageApplied = false;
                for (int i = 0; i < count; i++)
                {
                    Collider touched = gripContactBuffer[i];
                    gripContactBuffer[i] = null;
                    if (touched == null || touched.transform.root == transform.root)
                        continue;

                    GripDamageSource source;
                    GameObject attackerRoot = touched.transform.root.gameObject;
                    if (manager.IsLocalRiderTransform(touched.transform))
                    {
                        source = GripDamageSource.PlayerTouch;
                    }
                    else
                    {
                        AIKopkariRider touchingRider = ResolveAttackingAIRider(touched.gameObject);
                        if (touchingRider == null || touchingRider == this)
                            continue;

                        attackerRoot = touchingRider.transform.root.gameObject;

                        if (touchingRider.ulakRole == UlakRole.TrapSetter)
                            source = GripDamageSource.TrapSetterContact;
                        else if (touchingRider.ulakRole == UlakRole.Guard)
                            source = GripDamageSource.GuardContact;
                        else
                            source = GripDamageSource.OtherRiderContact;
                    }

                    if (ApplyGripDamage(source, attackerRoot))
                    {
                        contactDamageApplied = true;
                        break;
                    }
                }

                // Remaining gameplay contact must not depend on optional horse
                // colliders. This low-frequency position fallback also survives
                // collider cleanup and per-layer collision filtering.
                float centerContactRadius = Mathf.Max(2f, playerTouchCheckRadius);
                if (!contactDamageApplied && manager.horseAnimal != null)
                {
                    Vector3 playerPosition = manager.horseAnimal.transform.position;
                    if (HorizontalDistance(Animal.transform.position, playerPosition) <= centerContactRadius)
                    {
                        contactDamageApplied = ApplyGripDamage(
                            GripDamageSource.PlayerTouch,
                            manager.horseAnimal.transform.root.gameObject);
                    }
                }

                if (!contactDamageApplied && TryGetNearestActiveRider(
                        Animal.transform.position,
                        centerContactRadius,
                        this,
                        out AIKopkariRider nearbyRider))
                {
                    GripDamageSource fallbackSource = nearbyRider.ulakRole == UlakRole.TrapSetter
                        ? GripDamageSource.TrapSetterContact
                        : nearbyRider.ulakRole == UlakRole.Guard
                            ? GripDamageSource.GuardContact
                            : GripDamageSource.OtherRiderContact;
                    ApplyGripDamage(fallbackSource, nearbyRider.transform.root.gameObject);
                }
            }

            yield return checkWait;
        }

        gripContactCoroutine = null;
    }

    private void HandleNpcGripBreakDamage(BoostersContainer.DebuffState damageType)
    {
        if (!hasLamb)
            return;

        if (damageType == BoostersContainer.DebuffState.WalkZone && dropOnWalkZone)
        {
            ApplyGripDamage(GripDamageSource.WalkTrap);
            return;
        }

        if (damageType == BoostersContainer.DebuffState.WebSnare && dropOnWebSnare &&
            Time.time >= gripProtectionUntil)
            DropOwnedUlak();
    }

    private void HandleObstacleTouched()
    {
        if (hasLamb && dropOnObstacle && Time.time >= gripProtectionUntil)
            DropOwnedUlak();
    }

    private void DropOwnedUlak()
    {
        if (!hasLamb)
            return;

        hasLamb = false;
        StopCarrierEscapeSpeedBoost();
        StopGripContactMonitoring();
        nextGripDamageTimes.Clear();
        CancelCarrierEscape();
        ApplyBaseNavigationAvoidance();
        CancelPickupFocus();
        StopItemTimer();

        if (pickUp != null && pickUp.Has_Item)
            pickUp.DropItem();

        KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);
        if (ulakRole == UlakRole.Guard)
            StartGuardingUlak();
        else
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
    private void HandleNpcAttackDamageReceived()
    {
        if (!isGameplayActive || ulakRole != UlakRole.Competitor)
            return;

        if (hasLamb)
        {
            GameObject damager = boosterContainer != null ? boosterContainer.LastAttackDamager : null;
            AIKopkariRider attackingRider = ResolveAttackingAIRider(damager);

            if (attackingRider != null && attackingRider.ulakRole == UlakRole.Guard)
            {
                GripDamageSource guardSource = IsCarrierEngagedByGuard(transform.root.gameObject)
                    ? GripDamageSource.GuardRiderMelee
                    : GripDamageSource.GuardHorseAttack;
                ApplyGripDamage(guardSource, attackingRider.gameObject);
            }
            else if (attackingRider != null && attackingRider.isMainRival)
            {
                ApplyGripDamage(GripDamageSource.MainRivalSideAttack, attackingRider.gameObject);
            }
            else if (damager != null && KopkariManager.Instance != null &&
                     KopkariManager.Instance.IsLocalRiderTransform(damager.transform))
            {
                ApplyGripDamage(GripDamageSource.PlayerTouch, damager);
            }
            else if (IsCarrierEngagedByGuard(transform.root.gameObject))
            {
                // Fallback for an older Guard hitbox whose Malbers Owner is not configured.
                ApplyGripDamage(GripDamageSource.GuardRiderMelee, damager);
            }
            return;
        }

        if (pickUp == null || pickUp.Has_Item)
            return;

        Pickable focusedUlak = pickUp.FocusedItem;
        CancelPickupFocus();

        // Remaining inside the trigger does not fire another focus-enter event,
        // so explicitly restart the full randomized pickup duration after a hit.
        if (focusedUlak != null && focusedUlak.CanBePicked && waitCoroutine == null)
            waitCoroutine = StartCoroutine(WaitToPickUpLamb());
    }

    private static AIKopkariRider ResolveAttackingAIRider(GameObject damager)
    {
        if (damager == null)
            return null;

        AIKopkariRider rider = damager.GetComponentInParent<AIKopkariRider>();
        if (rider != null)
            return rider;

        Transform root = damager.transform.root;
        return root != null ? root.GetComponentInChildren<AIKopkariRider>(true) : null;
    }

    public void CompleteRoundAtTarget()
    {
        if (wonCurrentRound)
            return;

        wonCurrentRound = true;
        // Ownership is cleared before the manager's round-winner callback. Mark
        // gameplay inactive now so that OnGoatOwnerChanged cannot assign the
        // winner a fresh Uloq target during that short event-ordering window.
        isGameplayActive = false;
        hasLamb = false;
        StopCarrierEscapeSpeedBoost();
        StopGripContactMonitoring();
        CancelCarrierEscape();
        ApplyBaseNavigationAvoidance();
        StopItemTimer();

        DropPhysicalUlakFromActualPicker();

        var bm = KopkariManager.Instance;
        bm?.NotifyGoatOwner(transform.root.gameObject, false);
    }

    private void DropPhysicalUlakFromActualPicker()
    {
        // Preserve the legacy NPCGetLamb_CodeAI path first. Registon already
        // serializes this exact MPickUp reference and it is the proven holder.
        if (pickUp != null && pickUp.Item != null)
        {
            if (!pickUp.enabled)
                pickUp.enabled = true;
            pickUp.DropItem();
        }

        KopkariManager manager = KopkariManager.Instance;
        Pickable ulak = manager != null ? manager.pickableObj : null;
        MPickUp actualPicker = ulak != null ? ulak.Picker : null;

        if (actualPicker != null && actualPicker.Has_Item)
        {
            // MPickUp.DropItem silently returns while disabled. The carrier has
            // already finished, so enabling it here is safe and guarantees that
            // Malbers clears both the parent and the holder's Item reference.
            if (!actualPicker.enabled)
                actualPicker.enabled = true;
            actualPicker.DropItem();
        }

        if (ulak == null)
            return;

        // Fallback for a stale serialized pickup reference: find the actual
        // holder inside this rider and clear it through the same Malbers API.
        MPickUp[] riderPickups = transform.root.GetComponentsInChildren<MPickUp>(true);
        for (int i = 0; i < riderPickups.Length; i++)
        {
            MPickUp riderPickup = riderPickups[i];
            if (riderPickup == null || !riderPickup.Has_Item || riderPickup.Item != ulak)
                continue;

            if (!riderPickup.enabled)
                riderPickup.enabled = true;
            riderPickup.DropItem();
        }

        if (ulak.Picker != null)
        {
            // Last-resort consistency repair. This is only reached when Malbers
            // retained a stale picker despite DropItem, and the round is already
            // complete, so clear both sides before moving the Ulak next round.
            MPickUp stalePicker = ulak.Picker;
            ulak.Drop();
            if (stalePicker.Item == ulak)
                stalePicker.Item = null;
        }

        if (ulak.transform.parent != null)
            ulak.Drop();
    }

    private void HandleFinish()
    {
        if (isEliminatedFromRounds)
            return;

        isFinished = true;
        isGameplayActive = false;
        isFakeUlakDistracted = false;
        fakeUlakDiversionTarget = null;
        StopUlakRecovery();
        StopGuardRiderMeleeObject();
        StopGuardingUlak();
        StopTrapSetterRole();
        StopGripContactMonitoring();
        StopChaseObstacleSteering();
        StopCarrierEscapeSpeedBoost();
        CancelCarrierEscape();
        CancelReturnToUlak();
        CancelPickupFocus();
        StopItemTimer();

        if (orbitCoroutine != null)
        {
            StopCoroutine(orbitCoroutine);
            orbitCoroutine = null;
        }

        isRoundWarmupQualified = false;
        isMovingToRoundWarmup = false;

        if (roundWinnerRoutine != null)
        {
            StopCoroutine(roundWinnerRoutine);
            roundWinnerRoutine = null;
        }

        ai?.Stop();
        if (Animal != null)
        {
            Animal.StopMoving();
            Animal.Reset_Movement();
            Animal.Speed_CurrentIndex_Set(0);
        }
    }

    public void StopForRoundEnd()
    {
        HandleFinish();
    }

    public void BeginRoundWarmupMovement()
    {
        KopkariManager manager = KopkariManager.Instance;
        if (isEliminatedFromRounds || manager == null || !manager.HasPreparedNextRound ||
            !manager.IsRoundWarmupActive)
            return;

        if (roundWinnerRoutine != null)
        {
            StopCoroutine(roundWinnerRoutine);
            roundWinnerRoutine = null;
        }

        isRoundWarmupQualified = false;
        isMovingToRoundWarmup = false;
        MoveToRoundWarmup();
    }

    private void MoveToRoundWarmup()
    {
        Transform warmupPoint = GetCurrentRoundWarmupPoint();
        if (warmupPoint == null)
            return;

        isMovingToRoundWarmup = true;
        MoveTo(warmupPoint);
    }

    private void ApplyRoundWarmupSpeed()
    {
        StopCarrierEscapeSpeedBoost();
        hasLamb = false;
        ApplyConfiguredGameplaySpeed();
    }

    public void EliminateFromRounds()
    {
        if (isEliminatedFromRounds)
            return;

        isEliminatedFromRounds = true;
        isRoundWarmupQualified = false;
        isMovingToRoundWarmup = false;
        if (roundWinnerRoutine != null)
        {
            StopCoroutine(roundWinnerRoutine);
            roundWinnerRoutine = null;
        }
        StopRiderAI();
    }

    public void PauseAtWarmupTimeout()
    {
        if (isEliminatedFromRounds || isRoundWarmupQualified)
            return;

        isMovingToRoundWarmup = false;
        ai?.Stop();
        if (Animal != null)
        {
            Animal.StopMoving();
            Animal.Reset_Movement();
            Animal.Speed_CurrentIndex_Set(0);
        }
    }

    public void RefreshRoundWarmupQualification(Transform warmupPoint, float reachDistance)
    {
        if (isEliminatedFromRounds || isRoundWarmupQualified || warmupPoint == null || Animal == null)
            return;

        Vector3 offset = Animal.transform.position - warmupPoint.position;
        offset.y = 0f;
        float distance = Mathf.Max(0.25f, reachDistance);
        if (offset.sqrMagnitude > distance * distance)
            return;

        MarkRoundWarmupQualified();
    }

    public void MarkRoundWarmupQualified()
    {
        if (isEliminatedFromRounds || isRoundWarmupQualified)
            return;

        isMovingToRoundWarmup = false;
        isRoundWarmupQualified = true;
        ai?.Stop();
    }
    // 🔹 Finishga yetganda (finish triggerdan chaqiriladi)
    public void OnFinishReached()
    {
        if (!hasLamb) return;

        hasLamb = false;
        StopGripContactMonitoring();
        CancelCarrierEscape();
        ApplyBaseNavigationAvoidance();
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
            StopCarrierEscapeSpeedBoost();
            StopGripContactMonitoring();

            StopItemTimer(); // agar timer ishlayotgan bo‘lsa

            // darhol uloqqa qaytamiz
            MoveToCurrentUlak();
            return;
        }

        // 2) Agar men hozirgi egasi bo‘lsam → hech narsa qilinmaydi
        //    (MoveToNextPoint davom etadi)
    }
    #endregion

    #region Stop Rider
    private void StopRiderAI()
    {
        StopTrapSetterRole();
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
