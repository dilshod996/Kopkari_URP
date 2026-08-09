using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays Kopkari gameplay effects without coupling the gameplay
/// manager to a particular VFX package. Add one instance to the Kopkari scene
/// and assign prefab variants through the Inspector.
/// </summary>
[DisallowMultipleComponent]
public sealed class KopkariVFXController : MonoBehaviour
{
    [Serializable]
    private sealed class EffectSlot
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField, Min(0.01f)] private float scale = 1f;
        [SerializeField, Min(0)] private int prewarmCount = 1;
        [SerializeField, Min(1)] private int maximumPoolSize = 6;
        [SerializeField, Min(0.1f)] private float releaseTimeout = 8f;
        [SerializeField] private bool randomizeYaw;
        [SerializeField] private bool disablePrefabLights = true;

        [NonSerialized] private RuntimeEffectPool runtimePool;

        public bool IsConfigured => prefab != null;

        public void Initialize(Transform poolRoot)
        {
            if (runtimePool != null || prefab == null || poolRoot == null)
                return;

            runtimePool = new RuntimeEffectPool(
                prefab,
                poolRoot,
                Mathf.Clamp(prewarmCount, 0, Mathf.Max(1, maximumPoolSize)),
                Mathf.Max(1, maximumPoolSize),
                disablePrefabLights);
        }

        public GameObject Rent(Transform poolRoot, Vector3 position, Quaternion rotation)
        {
            Initialize(poolRoot);
            if (runtimePool == null)
                return null;

            GameObject instance = runtimePool.Rent();
            if (instance == null)
                return null;

            float yaw = randomizeYaw ? UnityEngine.Random.Range(0f, 360f) : 0f;
            instance.transform.SetPositionAndRotation(
                position + positionOffset,
                rotation * Quaternion.Euler(rotationOffset) * Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            return instance;
        }

        public float ReleaseTimeout => Mathf.Max(0.1f, releaseTimeout);

        public void Release(GameObject instance)
        {
            runtimePool?.Release(instance);
        }

        public void ReleaseAll()
        {
            runtimePool?.ReleaseAll();
        }
    }

    private sealed class RuntimeEffectPool
    {
        private readonly GameObject prefab;
        private readonly Transform root;
        private readonly int maximumSize;
        private readonly bool disableLights;
        private readonly Queue<GameObject> inactive = new Queue<GameObject>();
        private readonly HashSet<GameObject> active = new HashSet<GameObject>();
        private readonly List<GameObject> instances = new List<GameObject>();

        public RuntimeEffectPool(
            GameObject prefab,
            Transform root,
            int prewarmCount,
            int maximumSize,
            bool disableLights)
        {
            this.prefab = prefab;
            this.root = root;
            this.maximumSize = Mathf.Max(1, maximumSize);
            this.disableLights = disableLights;

            int count = Mathf.Min(prewarmCount, this.maximumSize);
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance();
                if (instance != null)
                    inactive.Enqueue(instance);
            }
        }

        public GameObject Rent()
        {
            GameObject instance = null;
            while (inactive.Count > 0 && instance == null)
                instance = inactive.Dequeue();

            if (instance == null && instances.Count < maximumSize)
                instance = CreateInstance();
            if (instance == null)
                return null;

            active.Add(instance);
            instance.SetActive(true);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null || !active.Remove(instance))
                return;

            StopAndClearParticles(instance);
            instance.SetActive(false);
            instance.transform.SetParent(root, false);
            instance.transform.localScale = Vector3.one;
            inactive.Enqueue(instance);
        }

        public void ReleaseAll()
        {
            if (active.Count == 0)
                return;

            GameObject[] snapshot = new GameObject[active.Count];
            active.CopyTo(snapshot);
            for (int i = 0; i < snapshot.Length; i++)
                Release(snapshot[i]);
        }

        private GameObject CreateInstance()
        {
            if (prefab == null || root == null)
                return null;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, root);
            instance.name = prefab.name + " (Pooled)";

            if (disableLights)
            {
                Light[] lights = instance.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                    lights[i].enabled = false;
            }

            StopAndClearParticles(instance);
            instance.SetActive(false);
            instances.Add(instance);
            return instance;
        }

        private static void StopAndClearParticles(GameObject instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Clear(true);
            }
        }
    }

    [Header("1. Any Rider Picks Up Ulak - Lines Poof")]
    [SerializeField] private EffectSlot ulakPickupEffect = new EffectSlot();

    [Header("2. Local Player Picks Up Ulak - Impact Contrast Blue")]
    [SerializeField] private EffectSlot localUlakPickupEffect = new EffectSlot();

    [Header("3. Ulak Drops - Smoke Poof Alt")]
    [SerializeField] private EffectSlot ulakDropEffect = new EffectSlot();

    [Header("4. Local Carrier Grip Is Damaged - Hit D 3D Yellow")]
    [SerializeField] private EffectSlot carrierGripHitEffect = new EffectSlot();

    [Header("5. Round Is Won - Firework 1 Yellow")]
    [SerializeField] private EffectSlot roundWinEffect = new EffectSlot();

    [Header("6. Final Round Is Won - Firework 3 Yellow")]
    [SerializeField] private EffectSlot finalRoundWinEffect = new EffectSlot();

    [Header("7. Ulak Carrier Is Being Chased - Ground Dust Pulse")]
    [SerializeField] private EffectSlot chasePressureEffect = new EffectSlot();

    [Header("8. Local Carrier Grip Is Low - Red Warning")]
    [SerializeField] private EffectSlot lowGripWarningEffect = new EffectSlot();

    [Header("9. Local Carrier Grip Breaks - Strong Impact")]
    [SerializeField] private EffectSlot gripBreakEffect = new EffectSlot();

    [Header("10. Local Player Activates Defense - Hit Light B Blue")]
    [SerializeField] private EffectSlot defendActivatedEffect = new EffectSlot();

    [Header("11. Local Player Starts Speed Boost - Flash Burst")]
    [SerializeField] private EffectSlot speedBoostEffect = new EffectSlot();

    [Header("12. Final Round Starts - Hit Light Fireworks Big")]
    [SerializeField] private EffectSlot finalRoundStartEffect = new EffectSlot();

    [Header("13. Unowned Ulak On Ground - Resurrection Light Loop")]
    [SerializeField] private EffectSlot groundUlakLoopEffect = new EffectSlot();

    [Header("14. Active Scoring Target - Sky Rays Loop")]
    [SerializeField] private EffectSlot scoringTargetLoopEffect = new EffectSlot();

    [Header("Timing")]
    [SerializeField, Min(0f)] private float gripHitCooldown = 0.18f;
    [SerializeField, Range(0.05f, 0.95f)] private float lowGripThreshold = 0.35f;
    [SerializeField] private bool playVictoryEffectWhenAIWins = true;

    [Header("Chase Pressure")]
    [SerializeField, Min(1)] private int minimumNearbyChasers = 2;
    [SerializeField, Min(0.5f)] private float chaseDetectionRadius = 7f;
    [SerializeField, Min(0.1f)] private float chaseSampleInterval = 0.25f;
    [SerializeField, Min(0.2f)] private float chasePulseCooldown = 0.9f;

    [Header("Ground Ulak Marker")]
    [Tooltip("The marker is hidden when the Ulak is this close to the active scoring target.")]
    [SerializeField, Min(0f)] private float groundUlakTargetExclusionRadius = 4f;

    private Transform poolRoot;
    private GameObject previousOwner;
    private float nextGripHitEffectTime;
    private float nextChaseSampleTime;
    private float nextChaseEffectTime;
    private bool lowGripWarningPlayed;
    private bool gripBreakPlayed;
    private GameObject groundUlakLoopInstance;
    private Vector3 lastGroundUlakPosition;
    private GameObject scoringTargetLoopInstance;
    private Vector3 lastScoringTargetPosition;
    private Coroutine pendingDropRoutine;
    private Coroutine pendingVictoryRoutine;

    private void Awake()
    {
        GameObject poolObject = new GameObject("Kopkari VFX Pool");
        poolObject.transform.SetParent(transform, false);
        poolRoot = poolObject.transform;

        InitializeSlots();
    }

    private void OnEnable()
    {
        KopkariManager.OnGoatOwnerChanged += HandleUlakOwnerChanged;
        KopkariManager.OnLocalPlayerGripDamaged += HandleLocalPlayerGripDamaged;
        KopkariManager.OnLocalPlayerGripDepleted += HandleLocalPlayerGripDepleted;
        KopkariManager.OnMainGameStarted += HandleRoundStarted;
        TargetReachEvent.OnReachedTargetWithLamb += HandleRoundWon;
        BoostersContainer.OnDefendState += HandleDefendStateChanged;
        BoostersContainer.OnSprintEffectStart += HandleSprintEffectStarted;

        KopkariManager manager = KopkariManager.Instance;
        previousOwner = manager != null ? manager.currentGoatOwner : null;
    }

    private void OnDisable()
    {
        KopkariManager.OnGoatOwnerChanged -= HandleUlakOwnerChanged;
        KopkariManager.OnLocalPlayerGripDamaged -= HandleLocalPlayerGripDamaged;
        KopkariManager.OnLocalPlayerGripDepleted -= HandleLocalPlayerGripDepleted;
        KopkariManager.OnMainGameStarted -= HandleRoundStarted;
        TargetReachEvent.OnReachedTargetWithLamb -= HandleRoundWon;
        BoostersContainer.OnDefendState -= HandleDefendStateChanged;
        BoostersContainer.OnSprintEffectStart -= HandleSprintEffectStarted;

        StopAllCoroutines();
        pendingDropRoutine = null;
        pendingVictoryRoutine = null;
        StopGroundUlakLoop();
        StopScoringTargetLoop();
        ReleaseAllEffects();
        previousOwner = null;
    }

    private void Update()
    {
        UpdateScoringTargetLoop();
        UpdateGroundUlakLoop();

        if (!chasePressureEffect.IsConfigured || Time.time < nextChaseSampleTime)
            return;

        nextChaseSampleTime = Time.time + Mathf.Max(0.1f, chaseSampleInterval);
        TryPlayChasePressureEffect();
    }

    private void HandleRoundStarted()
    {
        nextGripHitEffectTime = 0f;
        nextChaseSampleTime = 0f;
        nextChaseEffectTime = 0f;
        lowGripWarningPlayed = false;
        gripBreakPlayed = false;
        KopkariManager manager = KopkariManager.Instance;
        previousOwner = manager != null ? manager.currentGoatOwner : null;

        if (manager != null &&
            manager.TotalRoundCount > 1 &&
            manager.CurrentRoundNumber >= manager.TotalRoundCount)
        {
            Vector3 position = ResolveUlakPosition(manager, transform.position);
            Play(finalRoundStartEffect, position, Quaternion.identity);
        }
    }

    private void HandleUlakOwnerChanged(GameObject ownerRoot)
    {
        KopkariManager manager = KopkariManager.Instance;
        bool gameplayActive = manager != null &&
                              manager.roomState == KopkariManager.RoomState.GameStarted;

        if (ownerRoot != null)
        {
            StopGroundUlakLoop();
            previousOwner = ownerRoot;
            if (!gameplayActive)
                return;

            Vector3 position = ResolveUlakPosition(manager, ownerRoot.transform.position);
            Play(ulakPickupEffect, position, Quaternion.identity);

            if (manager.IsLocalRiderTransform(ownerRoot.transform))
            {
                lowGripWarningPlayed = false;
                gripBreakPlayed = false;
                Play(localUlakPickupEffect, position, ownerRoot.transform.rotation);
            }
            return;
        }

        bool wasCarried = previousOwner != null;
        previousOwner = null;
        if (!gameplayActive || !wasCarried)
            return;

        Vector3 fallbackPosition = ResolveUlakPosition(manager, transform.position);
        if (pendingDropRoutine != null)
            StopCoroutine(pendingDropRoutine);
        pendingDropRoutine = StartCoroutine(PlayDropAfterDetach(fallbackPosition));
    }

    private IEnumerator PlayDropAfterDetach(Vector3 fallbackPosition)
    {
        // Ownership changes before the Malbers pickup component physically
        // detaches the Ulak. One frame gives the object its true drop position.
        yield return null;

        KopkariManager manager = KopkariManager.Instance;
        Vector3 position = manager != null && manager.currentGoatOwner == null
            ? ResolveUlakPosition(manager, fallbackPosition)
            : fallbackPosition;

        Play(ulakDropEffect, position, Quaternion.identity);
        pendingDropRoutine = null;
    }

    private void HandleLocalPlayerGripDamaged(float currentGrip, float maximumGrip)
    {
        KopkariManager manager = KopkariManager.Instance;
        if (manager == null ||
            manager.currentGoatOwner == null ||
            !manager.IsLocalRiderTransform(manager.currentGoatOwner.transform))
            return;

        Vector3 position = ResolveUlakPosition(
            manager,
            manager.LocalRiderAnimal != null ? manager.LocalRiderAnimal.transform.position : transform.position);

        float normalizedGrip = maximumGrip > 0f
            ? Mathf.Clamp01(currentGrip / maximumGrip)
            : 0f;
        if (currentGrip > 0.001f &&
            normalizedGrip <= Mathf.Clamp01(lowGripThreshold) &&
            !lowGripWarningPlayed)
        {
            lowGripWarningPlayed = true;
            Play(lowGripWarningEffect, position, Quaternion.identity);
            return;
        }

        // Grip depletion has its own stronger effect immediately afterward.
        if (currentGrip <= 0.001f || Time.unscaledTime < nextGripHitEffectTime)
            return;

        nextGripHitEffectTime = Time.unscaledTime + Mathf.Max(0f, gripHitCooldown);
        Play(carrierGripHitEffect, position, Quaternion.identity);
    }

    private void HandleLocalPlayerGripDepleted()
    {
        if (gripBreakPlayed)
            return;

        KopkariManager manager = KopkariManager.Instance;
        if (!IsGameplayActive(manager))
            return;

        gripBreakPlayed = true;
        Vector3 position = ResolveUlakPosition(manager, ResolveLocalRiderPosition(manager));
        Play(gripBreakEffect, position, Quaternion.identity);
    }

    private void HandleDefendStateChanged(bool available)
    {
        // BoostersContainer reports false when local defense begins and true
        // when the button becomes available again.
        if (available)
            return;

        KopkariManager manager = KopkariManager.Instance;
        if (!IsGameplayActive(manager))
            return;

        Play(defendActivatedEffect, ResolveLocalRiderPosition(manager), ResolveLocalRiderRotation(manager));
    }

    private void HandleSprintEffectStarted()
    {
        KopkariManager manager = KopkariManager.Instance;
        if (!IsGameplayActive(manager))
            return;

        Play(speedBoostEffect, ResolveLocalRiderPosition(manager), ResolveLocalRiderRotation(manager));
    }

    private void TryPlayChasePressureEffect()
    {
        KopkariManager manager = KopkariManager.Instance;
        if (!IsGameplayActive(manager) ||
            manager.currentGoatOwner == null ||
            manager.UlakTransform == null ||
            Time.time < nextChaseEffectTime)
        {
            return;
        }

        Vector3 ulakPosition = manager.UlakTransform.position;
        float radius = Mathf.Max(0.5f, chaseDetectionRadius);
        int chaserCount = AIKopkariRider.CountActiveRidersNear(ulakPosition, radius);
        bool localPlayerIsCarrier = manager.IsLocalRiderTransform(manager.currentGoatOwner.transform);

        // A non-local carrier is one of the nearby AI riders, so exclude it.
        if (!localPlayerIsCarrier)
        {
            chaserCount = Mathf.Max(0, chaserCount - 1);
            Transform localRider = manager.LocalRiderAnimal != null
                ? manager.LocalRiderAnimal.transform
                : null;
            if (localRider != null &&
                (localRider.position - ulakPosition).sqrMagnitude <= radius * radius)
            {
                chaserCount++;
            }
        }

        if (chaserCount < Mathf.Max(1, minimumNearbyChasers))
            return;

        nextChaseEffectTime = Time.time + Mathf.Max(0.2f, chasePulseCooldown);
        Play(chasePressureEffect, ulakPosition, Quaternion.identity);
    }

    private void HandleRoundWon(int riderId, bool isPlayer)
    {
        StopGroundUlakLoop();
        StopScoringTargetLoop();

        if (!isPlayer && !playVictoryEffectWhenAIWins)
            return;

        KopkariManager manager = KopkariManager.Instance;
        Vector3 position = ResolveUlakPosition(
            manager,
            manager != null && manager.CurrentTargetPosition != null
                ? manager.CurrentTargetPosition.position
                : transform.position);

        if (pendingVictoryRoutine != null)
            StopCoroutine(pendingVictoryRoutine);
        pendingVictoryRoutine = StartCoroutine(PlayVictoryAfterRoundBookkeeping(position));
    }

    private IEnumerator PlayVictoryAfterRoundBookkeeping(Vector3 position)
    {
        // KopkariManager prepares the next round from the same winner event.
        // Waiting one frame makes HasPreparedNextRound authoritative regardless
        // of event subscription order.
        yield return null;

        KopkariManager manager = KopkariManager.Instance;
        bool finalRoundCompleted = manager == null || !manager.HasPreparedNextRound;
        Play(finalRoundCompleted ? finalRoundWinEffect : roundWinEffect, position, Quaternion.identity);
        pendingVictoryRoutine = null;
    }

    private void UpdateGroundUlakLoop()
    {
        KopkariManager manager = KopkariManager.Instance;
        Transform ulak = manager != null ? manager.UlakTransform : null;
        if (!ShouldShowGroundUlakLoop(manager, ulak))
        {
            StopGroundUlakLoop();
            return;
        }

        Vector3 ulakPosition = ulak.position;
        if (groundUlakLoopInstance == null)
        {
            groundUlakLoopInstance = groundUlakLoopEffect.Rent(
                poolRoot,
                ulakPosition,
                Quaternion.identity);
            if (groundUlakLoopInstance == null)
                return;

            ParticleSystem[] systems =
                groundUlakLoopInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }

            lastGroundUlakPosition = ulakPosition;
            return;
        }

        // Preserve the slot's configured offset while following a rolling or
        // repositioned Ulak on the ground.
        groundUlakLoopInstance.transform.position += ulakPosition - lastGroundUlakPosition;
        lastGroundUlakPosition = ulakPosition;
    }

    private bool ShouldShowGroundUlakLoop(KopkariManager manager, Transform ulak)
    {
        if (!groundUlakLoopEffect.IsConfigured ||
            poolRoot == null ||
            !IsGameplayActive(manager) ||
            manager.currentGoatOwner != null ||
            ulak == null ||
            !ulak.gameObject.activeInHierarchy)
        {
            return false;
        }

        Transform target = manager.CurrentTargetPosition;
        float exclusionRadius = Mathf.Max(0f, groundUlakTargetExclusionRadius);
        if (target == null || exclusionRadius <= 0f)
            return true;

        Vector3 delta = ulak.position - target.position;
        delta.y = 0f;
        return delta.sqrMagnitude > exclusionRadius * exclusionRadius;
    }

    private void StopGroundUlakLoop()
    {
        if (groundUlakLoopInstance == null)
            return;

        groundUlakLoopEffect.Release(groundUlakLoopInstance);
        groundUlakLoopInstance = null;
    }

    private void UpdateScoringTargetLoop()
    {
        KopkariManager manager = KopkariManager.Instance;
        Transform target = manager != null ? manager.CurrentTargetPosition : null;
        if (!scoringTargetLoopEffect.IsConfigured ||
            poolRoot == null ||
            !IsGameplayActive(manager) ||
            target == null)
        {
            StopScoringTargetLoop();
            return;
        }

        Vector3 targetPosition = target.position;
        if (scoringTargetLoopInstance == null)
        {
            scoringTargetLoopInstance = scoringTargetLoopEffect.Rent(
                poolRoot,
                targetPosition,
                target.rotation);
            if (scoringTargetLoopInstance == null)
                return;

            ParticleSystem[] systems =
                scoringTargetLoopInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }

            lastScoringTargetPosition = targetPosition;
            return;
        }

        scoringTargetLoopInstance.transform.position += targetPosition - lastScoringTargetPosition;
        lastScoringTargetPosition = targetPosition;
    }

    private void StopScoringTargetLoop()
    {
        if (scoringTargetLoopInstance == null)
            return;

        scoringTargetLoopEffect.Release(scoringTargetLoopInstance);
        scoringTargetLoopInstance = null;
    }

    private void Play(EffectSlot slot, Vector3 position, Quaternion rotation)
    {
        if (slot == null || !slot.IsConfigured || poolRoot == null)
            return;

        GameObject instance = slot.Rent(poolRoot, position, rotation);
        if (instance == null)
            return;

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            systems[i].Play(true);
        }

        StartCoroutine(ReleaseWhenFinished(slot, instance, systems));
    }

    private IEnumerator ReleaseWhenFinished(
        EffectSlot slot,
        GameObject instance,
        ParticleSystem[] systems)
    {
        float deadline = Time.unscaledTime + slot.ReleaseTimeout;
        bool alive;
        do
        {
            yield return null;
            alive = false;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].IsAlive(true))
                {
                    alive = true;
                    break;
                }
            }
        }
        while (alive && Time.unscaledTime < deadline);

        slot.Release(instance);
    }

    private static Vector3 ResolveUlakPosition(KopkariManager manager, Vector3 fallback)
    {
        return manager != null && manager.UlakTransform != null
            ? manager.UlakTransform.position
            : fallback;
    }

    private static Vector3 ResolveLocalRiderPosition(KopkariManager manager)
    {
        return manager != null && manager.LocalRiderAnimal != null
            ? manager.LocalRiderAnimal.transform.position
            : Vector3.zero;
    }

    private static Quaternion ResolveLocalRiderRotation(KopkariManager manager)
    {
        return manager != null && manager.LocalRiderAnimal != null
            ? manager.LocalRiderAnimal.transform.rotation
            : Quaternion.identity;
    }

    private static bool IsGameplayActive(KopkariManager manager)
    {
        return manager != null && manager.roomState == KopkariManager.RoomState.GameStarted;
    }

    private void InitializeSlots()
    {
        ulakPickupEffect.Initialize(poolRoot);
        localUlakPickupEffect.Initialize(poolRoot);
        ulakDropEffect.Initialize(poolRoot);
        carrierGripHitEffect.Initialize(poolRoot);
        roundWinEffect.Initialize(poolRoot);
        finalRoundWinEffect.Initialize(poolRoot);
        chasePressureEffect.Initialize(poolRoot);
        lowGripWarningEffect.Initialize(poolRoot);
        gripBreakEffect.Initialize(poolRoot);
        defendActivatedEffect.Initialize(poolRoot);
        speedBoostEffect.Initialize(poolRoot);
        finalRoundStartEffect.Initialize(poolRoot);
        groundUlakLoopEffect.Initialize(poolRoot);
        scoringTargetLoopEffect.Initialize(poolRoot);
    }

    private void ReleaseAllEffects()
    {
        ulakPickupEffect.ReleaseAll();
        localUlakPickupEffect.ReleaseAll();
        ulakDropEffect.ReleaseAll();
        carrierGripHitEffect.ReleaseAll();
        roundWinEffect.ReleaseAll();
        finalRoundWinEffect.ReleaseAll();
        chasePressureEffect.ReleaseAll();
        lowGripWarningEffect.ReleaseAll();
        gripBreakEffect.ReleaseAll();
        defendActivatedEffect.ReleaseAll();
        speedBoostEffect.ReleaseAll();
        finalRoundStartEffect.ReleaseAll();
        groundUlakLoopEffect.ReleaseAll();
        scoringTargetLoopEffect.ReleaseAll();
    }
}
