using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Controller;

[RequireComponent(typeof(Collider))]
public class Booster : MonoBehaviour
{
    public enum BoosterType
    {
        SprintFull,
        SetSpeedSprint,
        Defend,
        WalkZone,
        TimeBooster,
        Hit,
        WebSnare,

        // obstacles
        WallObstacle,
        GetUlak,
        TriggerPoint,
        SpeedState
    }

    public enum BoosterMode
    {
        Pickup,   // 1 marta oladi va yo'q bo'ladi
        Zone      // trap/zone: kirganda effect, durationdan keyin restore + despawn
    }

    [Header("Booster")]
    public BoosterType boosterType;
    [SerializeField] private BoosterMode mode = BoosterMode.Pickup;

    [Header("Refs")]
    [SerializeField] private GameObject visuals;
    private Collider triggerCol;

    [Header("Pickup Feedback (Player Only)")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField] private Color pickupColor = Color.white;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float vfxYOffset = 0.15f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private Sprite boosterIcon;

    [Header("WalkZone Settings (Zone Mode)")]
    [SerializeField] private float slowDuration = 2.5f;
    [SerializeField] private int slowSpeedIndex = 3;

    public static Action OnSprintFull;

    private bool pickedOrTriggered;

    // Zone ichida bir hayvonni 1 marta slow qilish
    private readonly HashSet<BoostersContainer> _affected = new();

    // ✅ NEW: WalkZone coroutine & handler tracking (1 trigger -> 1 rider)
    private Coroutine walkZoneCo;
    private BoostersContainer walkZoneTarget;
    private Action defendHandlerCached;


    private float walkZoneStartTime;
    private float walkZoneFinishTime;

    private bool walkZoneActive;
    private bool walkZoneTimeAdded;
    private bool walkZoneAffectsLocalPlayer;
    public static float TotalWalkZoneDamagedTime { get; private set; }
    public static event Action<float> OnWalkZoneDamagedTime;

    public static void ResetWalkZoneDamagedTime()
    {
        TotalWalkZoneDamagedTime = 0f;
    }

    private void Awake()
    {
        triggerCol = GetComponent<Collider>();
        triggerCol.isTrigger = true;
    }

    private void OnEnable()
    {
        pickedOrTriggered = false;
        _affected.Clear();

        ClearZoneSubscriptions();  // ✅ mana shu yetadi

        if (triggerCol) triggerCol.enabled = true;
        if (visuals) visuals.SetActive(true);

        StopAllCoroutines();
    }

    private void OnDisable()
    {
        ClearZoneSubscriptions();  // ✅ pool reuse’da event osilib qolmaydi
    }


    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.CompareTag("Player");
        bool isNpc = other.CompareTag("NPC");
        if (!isPlayer && !isNpc) return;
        if (isNpc && RacingController.Instance?.mapType == RacingController.RacingType.Training && boosterType == BoosterType.WebSnare) return;
        if (mode == BoosterMode.Pickup)
        {
            HandlePickup(other, isPlayer, isNpc);
        }
        else // Zone
        {
            HandleZone(other, isPlayer, isNpc);
            //RacingTutorials.OnItemPickedMode?.Invoke(mode);
        }
    }

    private void HandlePickup(Collider other, bool isPlayer, bool isNpc)
    {
        if (pickedOrTriggered) return;

        var boosters = other.GetComponentInChildren<BoostersContainer>();
        if (boosters == null) return;

        pickedOrTriggered = true;

        if (isPlayer)
            PlayPickupFeedback();
        DisableAndDespawn();
        if (RacingController.Instance!= null && RacingController.Instance.mapType==RacingController.RacingType.Training)
        {
            RacingTutorials.OnItemPicked?.Invoke(boosterType, mode);
            if(boosterType == BoosterType.SprintFull)
            {
                if (isPlayer) OnSprintFull?.Invoke();
            }
            else if(boosterType == BoosterType.SetSpeedSprint)
            {
                boosters.TriggerAutoBoostSpeed();
            }
            return;
        }
        ApplyPickupEffect(boosters, isPlayer);


    }

    private void HandleZone(Collider other, bool isPlayer, bool isNpc)
    {
        if (pickedOrTriggered) return;

        var boosters = other.GetComponentInChildren<BoostersContainer>();
        if (!boosters)
        {
            pickedOrTriggered = true;
            DisableAndDespawn();
            return;
        }

        if (boosterType != BoosterType.WalkZone)
            return;

        // NPC defend bo‘lsa skip
        if (boosters.isNpc && boosters.defendCount > 0)
        {
            boosters.DefendPlayerNpc();
            return;
        }

        // Player defend qobiq ON bo‘lsa skip
        if (boosters.defendQobiq != null && boosters.defendQobiq.activeSelf)
            return;

        // 🔒 1-marta trigger bo‘ldi
        pickedOrTriggered = true;

        // ✅ ZONE uchun Player feedback
        if (isPlayer)
        {
            PlayPickupFeedback();
            RacingTutorials.OnItemPicked?.Invoke(BoosterType.WalkZone, mode);
        }

        boosters.SetDebuff(BoostersContainer.DebuffState.WalkZone);
        if (triggerCol) triggerCol.enabled = false;
        if (visuals) visuals.SetActive(false);

        TryApplyWalkZoneSlow(boosters);
    }

    private void PlayPickupFeedback()
    {
        if (pickupVfxPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * vfxYOffset;
            var vfxGo = SimplePool.Spawn(pickupVfxPrefab, pos, Quaternion.identity);

            var colorSetter = vfxGo.GetComponent<PickupVfxColor>();
            if (colorSetter != null)
                colorSetter.SetColor(pickupColor);
        }
        //Hozircha walk zone ga kirganda sound va hech qanday ui anim yoq

        if (pickupSfx != null)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);

        HomeHapticsManager.Instance?.Play(GetPickupHapticId());
        BoosterUIAnimator.RaiseBoosterPicked(boosterType, boosterIcon);
    }

    private HomeHapticId GetPickupHapticId()
    {
        switch (boosterType)
        {
            case BoosterType.SprintFull:
            case BoosterType.SetSpeedSprint:
                return HomeHapticId.BoosterUse;

            default:
                return HomeHapticId.ItemPickup;
        }
    }

    private void ApplyPickupEffect(BoostersContainer target, bool isPlayer)
    {
        switch (boosterType)
        {
            case BoosterType.SprintFull:
                if (isPlayer) OnSprintFull?.Invoke();
                else target.TriggerBoostSpeed();
                break;

            case BoosterType.SetSpeedSprint:
                target.TriggerAutoBoostSpeed();
                break;

            case BoosterType.Defend:
                target.AddDefend();
                break;

            case BoosterType.WalkZone:
                // Pickup rejimida WalkZone ishlatmoqchi bo'lsangiz:
                if (isPlayer) target.AddWalkZone();
               
                break;

            case BoosterType.TimeBooster:
                break;

            case BoosterType.Hit:
                if (isPlayer) target.AddHit();
                break;

            case BoosterType.WebSnare:
                target.AddWebSnare();
                break;
        }
    }
    #region Walk Zone

    private void TryApplyWalkZoneSlow(BoostersContainer boosters)
    {
        if (!boosters || !boosters.horseAnimal) { DisableAndDespawn(); return; }
        if (_affected.Contains(boosters)) { DisableAndDespawn(); return; }

        _affected.Add(boosters);

        var animal = boosters.horseAnimal;

        int originalIndex = 5; // SEN AYTGANIDEK: qolsin
        int appliedIndex = Mathf.Min(originalIndex, slowSpeedIndex);

        walkZoneTarget = boosters;

        // ✅ effected time start
        walkZoneStartTime = Time.time;
        walkZoneFinishTime = 0f;
        walkZoneActive = true;
        walkZoneTimeAdded = false;
        walkZoneAffectsLocalPlayer = !boosters.isNpc;

        void CancelZone(bool despawn)
        {
            if (walkZoneCo != null)
            {
                StopCoroutine(walkZoneCo);
                walkZoneCo = null;
            }

            // ✅ effected time ni bir marta yuboradi
            AddWalkZoneDamagedTimeOnce();

            // ✅ speed restore
            if (animal != null && animal.CurrentSpeedIndex == appliedIndex)
                animal.Speed_CurrentIndex_Set(originalIndex);

            if (walkZoneTarget != null && defendHandlerCached != null)
            {
                walkZoneTarget.OnDefendActivated -= defendHandlerCached;
                defendHandlerCached = null;
            }

            _affected.Remove(walkZoneTarget);

            walkZoneTarget?.SetDebuff(BoostersContainer.DebuffState.None);
            walkZoneTarget?.NormalSpeedInvoke();

            walkZoneTarget = null;
            walkZoneActive = false;

            if (despawn)
                SimplePool.Despawn(gameObject);
        }

        // ✅ defend handler
        Action defendHandler = null;
        defendHandler = () => CancelZone(despawn: true);

        defendHandlerCached = defendHandler;
        boosters.OnDefendActivated += defendHandler;

        boosters.EnteredSpeedInvoke();
        animal.Speed_CurrentIndex_Set(appliedIndex);

        if (walkZoneCo != null)
            StopCoroutine(walkZoneCo);

        walkZoneCo = StartCoroutine(WalkZoneRoutine(animal, originalIndex, appliedIndex, slowDuration, CancelZone));
    }

    private IEnumerator WalkZoneRoutine(
        MAnimal animal,
        int originalIndex,
        int appliedIndex,
        float duration,
        Action<bool> CancelZone)
    {
        yield return new WaitForSeconds(duration);

        CancelZone?.Invoke(true);
    }

    private void AddWalkZoneDamagedTimeOnce()
    {
        if (!walkZoneActive) return;
        if (walkZoneTimeAdded) return;

        walkZoneTimeAdded = true;

        if (!walkZoneAffectsLocalPlayer)
            return;

        walkZoneFinishTime = Time.time;

        float damagedTime = walkZoneFinishTime - walkZoneStartTime;
        damagedTime = Mathf.Clamp(damagedTime, 0f, slowDuration);

        TotalWalkZoneDamagedTime += damagedTime;
        OnWalkZoneDamagedTime?.Invoke(damagedTime);
    }
    #endregion

    private void DisableAndDespawn()
    {
        if (triggerCol) triggerCol.enabled = false;
        if (visuals) visuals.SetActive(false);
        SimplePool.Despawn(gameObject);
    }
    private void ClearZoneSubscriptions()
    {
        AddWalkZoneDamagedTimeOnce();

        if (walkZoneTarget != null)
        {
            if (defendHandlerCached != null)
                walkZoneTarget.OnDefendActivated -= defendHandlerCached;

        }

        defendHandlerCached = null;
        walkZoneTarget = null;
        walkZoneActive = false;
        walkZoneAffectsLocalPlayer = false;

        if (walkZoneCo != null)
        {
            StopCoroutine(walkZoneCo);
            walkZoneCo = null;
        }
    }

}
