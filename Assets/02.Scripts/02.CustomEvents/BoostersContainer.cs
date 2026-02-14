using MalbersAnimations;
using MalbersAnimations.Controller;
using System;
using System.Collections;
using UnityEngine;

public class BoostersContainer : MonoBehaviour
{
    #region ====== Inspector / Base ======
    [Header("Base Speed")]
    [SerializeField] private int playerInitialSpeed = 5;

    [Header("Walk Zone Details")]
    public float dropDistanceBehind = 3.3f;

    [Header("Npc info")]
    public bool isNpc = false;

    [Header("Horse ning animation componenti")]
    public MAnimal horseAnimal;
    #endregion

    #region ====== Events (DO NOT REMOVE) ======
    [Header("Defend Objects")]
    public GameObject defendQobiq;
    public bool isDefend = false;
    [SerializeField] private float defendTime = 5f;
    public event Action OnDefendActivated;
    public static event Action<bool> OnDefendState;

    public static event Action<int> OnWalkZoneAdded;
    public static event Action<int> OnWalkZoneRemoved;
    public static event Action<int> OnDefendAdded;
    public static event Action<int> OnDefendRemoved;
    public static event Action<int> OnWebSnareAdded;
    public static event Action<int> OnWebSnareRemoved;

    public static event Action OnNormalState;
    public static event Action OnSlowState;
    public static event Action OnVerySlowState;

    public static Action OnSprintEffectStart;
    public static Action OnSprintEffectEnd;

    public static Action<float> OnPenaltyTime;
    public static Action<float> OnBoostTime;

    //Walk Zone Damage
    public static Action<bool> OnWalkZoneDamaged;
    //WebSnare damage
    public static Action<bool> OnWebSnareDamaged;
    #endregion

    #region ====== Runtime / Counters ======
    private Coroutine defendCoroutine;
    private Coroutine applyHitSlowCoroutine;
    private Coroutine boostCoroutine;

    public int walkZoneCount = 2;
    public int defendCount = 2;
    public int hitCount = 0;

    private float boostTime;
    private float penaltyTime;

    public GameObject walkzonePrefab;
    #endregion

    #region ====== Boost (MaxSpeed) ======
    [Header("Speed Improver")]
    private Coroutine boostCo;
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;
    #endregion

    #region ====== NPC WalkTrap by Checkpoint (Simple) ======
    [Header("NPC WalkTrap by Checkpoint (Simple)")]
    [SerializeField] private bool npcAutoDropByCheckpoint = true;
    [SerializeField] private int dropEveryN = 2;
    [SerializeField] private float dropChance = 0.6f;
    [SerializeField] private float dropCooldown = 2.0f;
    [SerializeField] private float delayAfterCheckpoint = 0.25f;
    [SerializeField] private int[] blacklistCheckpoints;
    [SerializeField] private float groundRay = 3.0f;
    [SerializeField] private LayerMask groundMask;

    private float _nextAllowedDropTime = 0f;
    #endregion

    #region ====== Damage / Hit Settings ======
    [Header("Damage / Hit Settings")]
    [SerializeField] private GameObject slowEffectObj;
    [SerializeField] private float slowDuration = 5f;
    [SerializeField] private int slowSpeedIndex = 2;

    private bool isUnderSlow = false;

    [SerializeField] private MDamageable damageable;
    #endregion

    #region ====== Obstacle Penalty (from HorseMine) ======
    [Header("Obstacle Penalty (from HorseMine)")]
    [SerializeField] private int obstacleMaxHits = 3;
    [SerializeField] private float obstaclePenaltyDuration = 10f;
    [SerializeField] private int obstaclePenaltySpeedIndex = 4;
    public static Action<bool> OnObstacleDamage;

    public int obstacleHitCount = 0;
    private bool obstaclePenalized = false;
    private Coroutine obstaclePenaltyCoroutine;
    #endregion

    #region ====== Debuff State (WalkZone/WebSnare) ======
    public bool IsInWalkZone { get; private set; }
    public bool IsWebSnareDamage { get; private set; }

    public enum DebuffState { None, WalkZone, WebSnare }
    public DebuffState CurrentDebuff { get; private set; } = DebuffState.None;
    #endregion

    #region ====== Unity ======
    private void Start()
    {
        if (!isNpc)
        {
            UIButtonActions.OnBindRequested?.Invoke(this);
            KopkariMainUI.OnBindRequested?.Invoke(this);
        }
    }

    private int GetPrefs(string key) => PlayerPrefs.GetInt(key);

    private void OnEnable()
    {
        if (!isNpc)
        {
            UIButtonActions.OnSprintStart += TriggerBoostSpeed; // hozircha sen o'zing ulaysan
            UIButtonActions.OnSprintEnd += OnSprintButtonReleased;
        }
        else
        {
            if (damageable != null)
            {
                damageable.events.OnReceivingDamage.AddListener(OnReceiveDamageHandler);
            }
        }
    }

    private void OnDisable()
    {
        CancelSlow(forceRestoreSpeed: true);
        CancelDefend();
        CancelBoost(false);
        CancelObstaclePenalty(forceRestoreSpeed: true);

        if (!isNpc)
        {
            UIButtonActions.OnSprintStart -= TriggerBoostSpeed;
            UIButtonActions.OnSprintEnd -= OnSprintButtonReleased;
        }
        else
        {
            damageable.events.OnReceivingDamage.RemoveListener(OnReceiveDamageHandler);
        }
    }
    #endregion

    #region ====== Helpers (Speed Restore RULE) ======
    // ✅ SEN SO‘RAGAN QOIDA:
    // Debuff yo‘q bo‘lsa -> maxSpeed true bo‘lsa 6, bo‘lmasa playerInitialSpeed
    private void RestoreSpeedAfterDebuffClear()
    {
        if (horseAnimal == null) return;

        // Agar debuff bor bo‘lsa penalty/slow o‘zi turadi (tegmaymiz)
        if (!isNpc && CurrentDebuff != DebuffState.None) return;

        // Agar obstacle penalty aktiv bo‘lsa ham tegmaymiz
        //if (obstaclePenalized) return;

        // Agar slow coroutine ketayotgan bo‘lsa tegmaymiz
        if (isUnderSlow) return;

        horseAnimal.Speed_CurrentIndex_Set(maxSpeed ? 6 : playerInitialSpeed);
    }
    #endregion

    #region ========================= Boost Speed =========================
    public void TriggerBoostSpeed()
    {
        if (horseAnimal == null) return;

        // ✅ Debuff paytida boost bo‘lmasin
        if (!isNpc && CurrentDebuff != DebuffState.None)
            return;

        // refresh
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            boostCoroutine = null;
        }

        // ✅ Start event faqat 1 marta
        if (!maxSpeed)
        {
            maxSpeed = true;
            if (!isNpc)
                OnSprintEffectStart?.Invoke();
        }

        if (!isNpc)
            OnNormalState?.Invoke();

        boostCoroutine = StartCoroutine(ImproveSpeed());
    }

    private IEnumerator ImproveSpeed()
    {
        // slow effect off (player-only)
        if (!isNpc && slowEffectObj != null && slowEffectObj.activeSelf)
            slowEffectObj.SetActive(false);

        if (horseAnimal != null)
            horseAnimal.Speed_CurrentIndex_Set(6);

        yield return new WaitForSeconds(maxSpeedDuration);

        if (!isNpc)
        {
            boostTime += maxSpeedDuration;
            OnBoostTime?.Invoke(boostTime);
        }

        // ✅ End event + restore speed shu yerda
        CancelBoost(forceRestoreSpeed: true);
    }

    private void OnSprintButtonReleased()
    {
        // manual sprint qo‘yib yuborildi -> boost off (sen xohlaganing)
        CancelBoost(forceRestoreSpeed: true);
    }

    private void CancelBoost(bool forceRestoreSpeed)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            boostCoroutine = null;
        }

        if (maxSpeed)
        {
            maxSpeed = false;
            if (!isNpc)
                OnSprintEffectEnd?.Invoke();
        }

        // ✅ OLD: doim 5ga qaytarib yuborardi (defend/debuff paytida ham)
        // ✅ NEW: faqat kerak bo‘lsa, qoidaga ko‘ra tiklaymiz
        if (forceRestoreSpeed)
            RestoreSpeedAfterDebuffClear();
    }
    #endregion

    #region ========================= Walk Zone =========================
    public void AddWalkZone()
    {
        walkZoneCount++;

        if (!isNpc)
        {
            int playerSlowDown = GetPrefs(Constants.PlayerItems.SlowDown);
            playerSlowDown += 1;
            OnWalkZoneAdded?.Invoke(playerSlowDown);
        }
    }

    public void DecreaseWalkZone()
    {
        if (!isNpc)
        {
            int playerSlowDown = GetPrefs(Constants.PlayerItems.SlowDown);
            playerSlowDown -= 1;
            OnWalkZoneRemoved?.Invoke(playerSlowDown);
        }
        else
        {
            walkZoneCount = Mathf.Max(0, walkZoneCount - 1);
        }
    }

    public void DropWalkTrap()
    {
        int walkZone = GetPrefs(Constants.PlayerItems.SlowDown);
        if (walkZone <= 0) return;

        DecreaseWalkZone();
        if (TryAlignDropToGround(out var pos, out var rot))
            SimplePool.Spawn(walkzonePrefab, pos, rot);
    }

    public void EnteredSpeedInvoke()
    {
        if (!isNpc) OnSlowState?.Invoke();
    }

    public void NormalSpeedInvoke()
    {
        if (!isNpc) OnNormalState?.Invoke();
    }

    public void NotifyCheckpointPassed(int cpIndex, int totalCp)
    {
        if (!isNpc) return;
        if (!npcAutoDropByCheckpoint) return;
        if (walkZoneCount <= 0) return;
        if (Time.time < _nextAllowedDropTime) return;

        if (blacklistCheckpoints != null && blacklistCheckpoints.Length > 0)
        {
            for (int i = 0; i < blacklistCheckpoints.Length; i++)
                if (blacklistCheckpoints[i] == cpIndex) return;
        }

        if (dropEveryN > 0 && (cpIndex % dropEveryN) != 0) return;
        if (UnityEngine.Random.value > dropChance) return;

        if (delayAfterCheckpoint > 0f)
            StartCoroutine(DropAfterDelay(delayAfterCheckpoint));
        else
            DropWalkTrapNpc();

        _nextAllowedDropTime = Time.time + dropCooldown;
    }

    private IEnumerator DropAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DropWalkTrapNpc();
    }

    public void DropWalkTrapNpc()
    {
        if (walkZoneCount <= 0) return;

        DecreaseWalkZone();
        if (TryAlignDropToGround(out var pos, out var rot))
            SimplePool.Spawn(walkzonePrefab, pos, rot);
    }

    private bool TryAlignDropToGround(out Vector3 p, out Quaternion r)
    {
        Vector3 basePos = transform.position - transform.forward * dropDistanceBehind;
        Vector3 rayStart = basePos + Vector3.up * 1.5f;

        int mask = (groundMask.value == 0) ? Physics.DefaultRaycastLayers : groundMask.value;

        if (Physics.Raycast(rayStart, Vector3.down, out var hit, groundRay, mask, QueryTriggerInteraction.Ignore))
        {
            p = hit.point + hit.normal * 0.03f;
            Vector3 fwd = Vector3.ProjectOnPlane(-transform.forward, hit.normal);
            if (fwd.sqrMagnitude < 1e-4f) fwd = -transform.forward;
            r = Quaternion.LookRotation(fwd.normalized, hit.normal);
            return true;
        }

        p = basePos;
        r = Quaternion.LookRotation(-transform.forward, Vector3.up);
        return false;
    }

    public void SetDebuff(DebuffState state)
    {
        if (isNpc) return;
        if (CurrentDebuff == state) return;

        // old state OFF events
        if (CurrentDebuff == DebuffState.WalkZone)
            OnWalkZoneDamaged?.Invoke(false);
        if (CurrentDebuff == DebuffState.WebSnare)
            OnWebSnareDamaged?.Invoke(false);

        // reset flags
        IsInWalkZone = false;
        IsWebSnareDamage = false;

        // new state ON
        CurrentDebuff = state;

        if (state == DebuffState.WalkZone)
        {
            IsInWalkZone = true;
            OnWalkZoneDamaged?.Invoke(true);
        }
        else if (state == DebuffState.WebSnare)
        {
            IsWebSnareDamage = true;
            OnWebSnareDamaged?.Invoke(true);
        }

        // ✅ Debuff None bo‘lsa speedni to‘g‘ri tiklash (maxSpeed?6:5)
        if (state == DebuffState.None)
            RestoreSpeedAfterDebuffClear();
    }
    #endregion

    #region ========================= Defend =========================
    public void AddDefend()
    {
        if (!isNpc)
        {
            int defendPlayer = GetPrefs(Constants.PlayerItems.Defense);
            defendPlayer += 1;
            OnDefendAdded?.Invoke(defendPlayer);
        }
        else defendCount++;
    }

    public void DecreaseDefend()
    {
        if (!isNpc)
        {
            int defendPlayer = GetPrefs(Constants.PlayerItems.Defense);
            defendPlayer -= 1;
            OnDefendRemoved?.Invoke(defendPlayer);
        }
        else defendCount = Mathf.Max(0, defendCount - 1);
    }

    public void DefendPlayer()
    {
        int defenderCount = GetPrefs(Constants.PlayerItems.Defense);
        if (defenderCount <= 0) return;

        DecreaseDefend();

        // ✅ Defend faqat damage/debuffni yechadi (sprint/boostni buzmaydi)
        SetDebuff(DebuffState.None);

        // ✅ Slow coroutine stop bo‘lsin, lekin speedni 5ga “majbur” qaytarmasin
        CancelSlow(forceRestoreSpeed: true);
        CancelObstaclePenalty(forceRestoreSpeed: true);
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendCoroutine = null;
        }

        if (defendQobiq != null) defendQobiq.SetActive(false);

        defendCoroutine = StartCoroutine(DefendObject());
        OnDefendActivated?.Invoke();

        if (!isNpc)
            OnDefendState?.Invoke(false);
    }

    public void DefendPlayerNpc()
    {
        if (defendCount <= 0) return;

        DecreaseDefend();

        // NPCda ham slow stop, speedni majburlamasin
        CancelSlow(forceRestoreSpeed: true);
        CancelObstaclePenalty(true);

        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendCoroutine = null;
        }

        if (defendQobiq != null) defendQobiq.SetActive(false);

        defendCoroutine = StartCoroutine(DefendObject());
        OnDefendActivated?.Invoke();
    }

    private IEnumerator DefendObject()
    {
        isDefend = true;

        if (slowEffectObj != null && slowEffectObj.activeSelf)
            slowEffectObj.SetActive(false);

        if (defendQobiq != null)
            defendQobiq.SetActive(true);

        // ❌ OLD: doim playerInitialSpeed qilib sprintni buzardi
        // ✅ NEW: debuff clear bo‘lgani uchun speedni qoidaga ko‘ra tiklaymiz
        RestoreSpeedAfterDebuffClear();

        yield return new WaitForSeconds(defendTime);

        if (defendQobiq != null)
            defendQobiq.SetActive(false);

        if (!isNpc)
            OnDefendState?.Invoke(true);

        isDefend = false;
        defendCoroutine = null;
    }

    private void CancelDefend()
    {
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendCoroutine = null;
        }

        if (defendQobiq != null)
            defendQobiq.SetActive(false);

        isDefend = false;
    }
    #endregion

    #region ========================= Hit / Web Snare =========================
    public void AddHit() => hitCount++;
    public void RemoveHit() => hitCount--;

    public void AddWebSnare()
    {
        if (!isNpc)
        {
            int webSnare = GetPrefs(Constants.PlayerItems.WebSnare);
            webSnare += 1;
            OnWebSnareAdded?.Invoke(webSnare);
        }
        else walkZoneCount++;
    }

    public void OnReceiveDamageHandler(float dam=0)
    {
        if (isUnderSlow) return;

        // Defend aktiv bo‘lsa slow umuman yo‘q
        if (isDefend) return;

        // NPC’da defend bo‘lsa avtomatik ishlat
        if (defendCount > 0 && isNpc)
        {
            DefendPlayerNpc();
            return;
        }

        SetDebuff(DebuffState.WebSnare);

        if (horseAnimal != null)
        {
            // ✅ oldingi slow bo‘lsa tozalab, yangisini boshlash
            CancelSlow(forceRestoreSpeed: false);

            applyHitSlowCoroutine = StartCoroutine(ApplyHitSlow());
        }
    }

    private IEnumerator ApplyHitSlow()
    {
        isUnderSlow = true;

        // slow kelganda boost cancel bo‘lsin
        CancelBoost(forceRestoreSpeed: false);
        CancelObstaclePenalty(forceRestoreSpeed: false);

        if (!isNpc)
        {
            OnVerySlowState?.Invoke();
            horseAnimal.Mode_Activate(3, -99);
        }

        if (slowEffectObj != null)
            slowEffectObj.SetActive(true);

        if (horseAnimal != null)
            horseAnimal.Speed_CurrentIndex_Set(slowSpeedIndex);

        yield return new WaitForSeconds(slowDuration);

        // Agar defend bosilib bekor qilingan bo‘lsa, bu yerga kelganda isUnderSlow false bo‘ladi
        if (!isUnderSlow) yield break;

        if (!isNpc)
        {
            penaltyTime += slowDuration;
            OnPenaltyTime?.Invoke(penaltyTime);
            OnNormalState?.Invoke();
        }

        if (horseAnimal != null)
            horseAnimal.Speed_CurrentIndex_Set(playerInitialSpeed);

        if (slowEffectObj != null)
            slowEffectObj.SetActive(false);

        isUnderSlow = false;
        applyHitSlowCoroutine = null;

        SetDebuff(DebuffState.None);
    }

    private void CancelSlow(bool forceRestoreSpeed)
    {
        if (applyHitSlowCoroutine != null)
        {
            StopCoroutine(applyHitSlowCoroutine);
            applyHitSlowCoroutine = null;
        }

        isUnderSlow = false;

        // ✅ OLD: forceRestoreSpeed true bo‘lsa doim 5ga qaytarardi (sprintni buzardi)
        // ✅ NEW: forceRestoreSpeed bo‘lsa ham qoidaga ko‘ra tiklaymiz
        if (forceRestoreSpeed)
            RestoreSpeedAfterDebuffClear();
    }
    #endregion

    #region ========================= Obstacle Penalty =========================
    public void NotifyObstacleTouched()
    {
        Sprite obstacleIcon;
        if (UIButtonActions.Instance != null)
            obstacleIcon = UIButtonActions.Instance.obstacleHitSprite;
        else
            obstacleIcon = KopkariMainUI.Instance.obstacleHitSprite;

        if (!isNpc && obstacleIcon != null)
        {
            BoosterUIAnimator.RaiseBoosterPicked(Booster.BoosterType.WallObstacle, obstacleIcon);
        }

        ProcessObstacleHit();
    }

    public void NotifyObstacleTouched_Npc()
    {
        ProcessObstacleHit();
    }

    private void ProcessObstacleHit()
    {
        if (obstaclePenalized) return;

        obstacleHitCount++;

        if (obstacleHitCount >= obstacleMaxHits)
        {
            obstacleHitCount = 0;
            StartObstaclePenalty();
        }
    }

    private void StartObstaclePenalty()
    {
        if (obstaclePenalized) return;
        Debug.Log("Obstacle penalty");
        // Defend aktiv bo‘lsa penalty bermaymiz
        if (isDefend) return;
        if (defendCount > 0 && isNpc)
        {
            DefendPlayerNpc();
            return;
        }
        Debug.Log("Obstacle penalty1");
        CancelBoost(forceRestoreSpeed: false);
        CancelSlow(forceRestoreSpeed: false);

        if (obstaclePenaltyCoroutine != null)
            StopCoroutine(obstaclePenaltyCoroutine);

        obstaclePenaltyCoroutine = StartCoroutine(ObstaclePenaltyRoutine());
    }

    private IEnumerator ObstaclePenaltyRoutine()
    {
        obstaclePenalized = true;
        Debug.Log("Obstacle penalty 3");
        if (!isNpc)
        {
            OnSlowState?.Invoke();
            OnObstacleDamage?.Invoke(true);
        }

        if (horseAnimal != null)
            horseAnimal.Speed_CurrentIndex_Set(obstaclePenaltySpeedIndex);

        yield return new WaitForSeconds(obstaclePenaltyDuration);

        obstaclePenalized = false;
        if (!isNpc) { OnObstacleDamage?.Invoke(false); }
        // ✅ penalty tugadi -> qoidaga ko‘ra tikla (maxSpeed?6:5)
        RestoreSpeedAfterDebuffClear();

        if (!isNpc && UIButtonActions.Instance != null)
            UIButtonActions.Instance.SliderValueRestore();

        if (!isNpc) OnNormalState?.Invoke();

        obstaclePenaltyCoroutine = null;
    }

    private void CancelObstaclePenalty(bool forceRestoreSpeed)
    {
        bool hadPenalty = obstaclePenalized;  // old holatni saqlab ol
        if (obstaclePenaltyCoroutine != null)
        {
            StopCoroutine(obstaclePenaltyCoroutine);
            obstaclePenaltyCoroutine = null;
        }

        obstaclePenalized = false;
        obstacleHitCount = 0;

        if (forceRestoreSpeed && hadPenalty && !isNpc)
            UIButtonActions.Instance?.SliderValueRestore(); 

        if (forceRestoreSpeed)
            RestoreSpeedAfterDebuffClear();
    }
    public void StopAnimation()
    {
        if(isNpc)
        {
            horseAnimal.Always_Forward(false);
            horseAnimal.StopMoving();
        }
    }
    #endregion
}
