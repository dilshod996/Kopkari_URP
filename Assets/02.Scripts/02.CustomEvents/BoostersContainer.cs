using MalbersAnimations;
using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class BoostersContainer : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private Stats playerStats;

    [SerializeField] private int playerInitialSpeed=5;
    [SerializeField] private string staminaStatName = "Stamina";
    [Header("Walk Zone Details")]
   // [SerializeField] private GameObject walkZonePrefab;
    public float dropDistanceBehind = 5.3f; // Inspector’da sozlasa bo‘ladi

    [Header("Defend Objects")]
    public GameObject defendQobiq;
    public bool isDefend=false;
    [SerializeField] private float defendTime = 5f;
    public event Action OnDefendActivated;
    public static event Action<bool> OnDefendState;

    private Coroutine defendCoroutine;
    public int walkZoneCount = 2;
    public int defendCount = 2;
    public int hitCount = 0;
    public static event Action<int> OnWalkZoneAdded;
    public static event Action<int> OnWalkZoneRemoved;

    // events for Defend
    public static event Action<int> OnDefendAdded;
    public static event Action<int> OnDefendRemoved;
    [Header("Npc info")]
    public bool isNpc = false; // Npc bo‘lsa, bu true bo‘ladi

    [Header("Horse ning animation componenti")]
    public MAnimal horseAnimal;

    [Header("Speed Improver")]
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;
    public static Action OnSprintEffectStart;
    public static Action OnSprintEffectEnd;

    [Header("NPC WalkTrap by Checkpoint (Simple)")]
    [SerializeField] private bool npcAutoDropByCheckpoint = true;
    [SerializeField] private int dropEveryN = 2;          // har 2-checkpointda
    [SerializeField] private float dropChance = 0.6f;     // 60% ehtimol
    [SerializeField] private float dropCooldown = 2.0f;   // sekund
    [SerializeField] private float delayAfterCheckpoint = 0.25f; // biroz kechikish
    [SerializeField] private int[] blacklistCheckpoints;  // (ixtiyoriy) bu checkpointlarda tashlamasin
    [SerializeField] private float groundRay = 3.0f;
    [SerializeField] private LayerMask groundMask;

    private float _nextAllowedDropTime = 0f;
    [Header("Damage / Hit Settings")]
    [SerializeField] private MDamageable damageable;      // Malbersning damage componenti
    [SerializeField] private GameObject slowEffectObj;    // Slow effekt uchun UI yoki FX
    [SerializeField] private float slowDuration = 5f;     // necha sekund sekin yuradi
    [SerializeField] private int slowSpeedIndex = 2;      // slow paytidagi speed index

    private bool isUnderSlow = false;                     // slow aktivmi yoki yo‘q

    public static Action<float> OnPenaltyTime;
    public static Action<float> OnBoostTime;

    private float boostTime;
    private float penaltyTime;

    #region Starting Events

    private void Start()
    {

        if (!isNpc && UIButtonActions.Instance != null)
        {
            UIButtonActions.Instance.Bind(this);
        }
       

    }

    private void OnEnable()
    {
        if (!damageable)
            damageable = GetComponent<MDamageable>();

        if (damageable != null)
        {
            // ⚠️ OnReceiveDamage ning imzosiga qarab moslashtirasan!
            damageable.events.OnReceivingDamage.AddListener(OnReceiveDamageHandler);
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.events.OnReceivingDamage.RemoveListener(OnReceiveDamageHandler);
        }
    }

    private int GetPrefs(string key)
    {
        return PlayerPrefs.GetInt(key);
    }

    #endregion

    #region Boost Speed

    public void TriggerBoostSpeed()
    {
        if (!maxSpeed)
        {
            StartCoroutine(ImproveSpeed());
        }
    }
    private IEnumerator ImproveSpeed()
    {
        maxSpeed = true;
        horseAnimal.Speed_CurrentIndex_Set(6);
        if(!isNpc) OnSprintEffectStart?.Invoke();
        yield return new WaitForSeconds(maxSpeedDuration);
        if (maxSpeed && !isNpc) { boostTime += maxSpeedDuration;
            OnBoostTime?.Invoke(boostTime);
        }
        // Avvalgi speedni qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(5);
        if(!isNpc) OnSprintEffectEnd?.Invoke();
        maxSpeed = false;
        //Debug.Log($"{horseAnimal.name} recovered from penalty.");
    }
    #endregion

    #region Walk Zone
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
        walkZoneCount = Mathf.Max(0, walkZoneCount - 1);            
        if (!isNpc)
        {
            int playerSlowDown = GetPrefs(Constants.PlayerItems.SlowDown);
            playerSlowDown -= 1;
            OnWalkZoneRemoved?.Invoke(playerSlowDown);
        }
            
    }
    public void DropWalkTrap()
    {
        if (walkZoneCount <= 0)
        {
            Debug.Log("WalkZone mavjud emas, tushirib bo'lmaydi.");
            return;
        }
        DecreaseWalkZone();
        if (TryAlignDropToGround(out var pos, out var rot))
        {
            Debug.Log("///////////Drop it////////////");
            var zone = SimplePool.Spawn(RacingController.Instance.walkZonePrefab, pos, rot);
            //Instantiate(walkZonePrefab, pos, rot);
        }
    }
    /// <summary>
    /// Bu method faqat Npc lar uchun Walk Zone Trap tashlashlari uchun
    /// </summary>
    /// <param name="cpIndex"></param>
    /// <param name="totalCp"></param>
    public void NotifyCheckpointPassed(int cpIndex, int totalCp)
    {
        
        if (!isNpc) return;
        if (!npcAutoDropByCheckpoint) return;
        if (walkZoneCount <= 0) return;
        if (Time.time < _nextAllowedDropTime) return;
       
        // blacklist (ixtiyoriy)
        if (blacklistCheckpoints != null && blacklistCheckpoints.Length > 0)
        {
            for (int i = 0; i < blacklistCheckpoints.Length; i++)
                if (blacklistCheckpoints[i] == cpIndex) return;
        }

        // har N-chi checkpoint
        if (dropEveryN > 0 && (cpIndex % dropEveryN) != 0) return;

        // ehtimol
        if (UnityEngine.Random.value > dropChance) return;

        // trap tashlash (ixtiyoriy kechikish bilan)
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
        if (walkZoneCount <= 0)
        {
            return;
        }
        
        DecreaseWalkZone();
        if (TryAlignDropToGround(out var pos, out var rot))
        {
            Debug.Log("///////////Drop it////////////");
            var zone = SimplePool.Spawn(RacingController.Instance.walkZonePrefab, pos, rot);
        }
            
        //else
        //    TrapPoolManager.Instance.Spawn(walkZonePrefab, pos, rot);
        //Vector3 dropPosition = transform.position - transform.forward * dropDistanceBehind;
        //Instantiate(walkZonePrefab, dropPosition, Quaternion.identity);
    }
    private bool TryAlignDropToGround(out Vector3 p, out Quaternion r)
    {
        Vector3 basePos = transform.position - transform.forward * dropDistanceBehind;
        Vector3 rayStart = basePos + Vector3.up * 1.5f;
        float rayLen = groundRay;

        // Agar groundMask tanlanmagan bo‘lsa -> Everything (hamma layer)
        int mask = (groundMask.value == 0) ? Physics.DefaultRaycastLayers : groundMask.value;

        if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayLen, mask, QueryTriggerInteraction.Ignore))
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

    #endregion

    #region Defend Details
    public void AddDefend()
    {
        defendCount++;
        if (!isNpc)
        {
            int defendPlayer =  GetPrefs(Constants.PlayerItems.Defense);
            defendPlayer += 1;
            OnDefendAdded?.Invoke(defendPlayer);
            Debug.Log("Defend Count" +  defendPlayer);
        }
    }
    public void DecreaseDefend()
    {
        defendCount = Mathf.Max(0, defendCount - 1);
        if (!isNpc)
        {
            int defendPlayer = GetPrefs(Constants.PlayerItems.Defense);
            defendPlayer -= 1;
            OnDefendRemoved?.Invoke(defendPlayer);
        }
            
    }
    public void DefendPlayer()
    {
        if (defendCount <= 0)
        {
            return;
        }
        DecreaseDefend();
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendQobiq.SetActive(false);
        }
        if(isUnderSlow) isUnderSlow = false;
        defendCoroutine = StartCoroutine(DefendObject());
        OnDefendActivated?.Invoke();
        if(!isNpc)
            OnDefendState?.Invoke(false);
    }
    public void DefendPlayerNpc()
    {
        if (defendCount <= 0)
        {
            return;
        }
        DecreaseDefend();
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendQobiq.SetActive(false);
        }
        defendCoroutine = StartCoroutine(DefendObject());
        OnDefendActivated?.Invoke();   // <-- NPCda ham EVENT shart!
    }

    private IEnumerator DefendObject()
    {
        isDefend = true;
        defendQobiq.SetActive(true);
        if (horseAnimal != null && horseAnimal.CurrentSpeedIndex != playerInitialSpeed)
        {
            horseAnimal.Speed_CurrentIndex_Set(playerInitialSpeed);
        }
        yield return new WaitForSeconds(defendTime);
        defendQobiq.SetActive(false);
        if(!isNpc)
            OnDefendState.Invoke(true);
        isDefend = false;
        defendCoroutine = null;
        //OnDefendDeactivated?.Invoke(); // (ixtiyoriy) if kerak bo‘lsa boshqa tizimlarga xabar
    }
    #endregion

    #region Hit Details(Qamchi)
    public void AddHit()
    {
        hitCount++;
    }
    public void RemoveHit() { hitCount--; }
    #endregion

    #region Damage / Hit Reaction 

    // ⚠️ Parametrlarni MDamageable.OnReceiveDamage imzosiga moslashtir!
    private void OnReceiveDamageHandler(/* masalan: MDamageable dam, Hit hit */ float dmg)
    {
        // 1) Agar allaqachon slow ishlayotgan bo‘lsa, boshqasini qo‘ymaymiz
        if (isUnderSlow)
            return;

        // 2) Agar defend allaqachon aktiv bo‘lsa -> slow ishlatmaymiz
        if (isDefend)
        {
            // faqat vizual effektlar bo‘lsa shu yerda qilsa bo‘ladi
            return;
        }

        // 3) Agar defend count bor bo‘lsa -> avtomatik DefendPlayer chaqiramiz
        if (defendCount > 0)
        {
            DefendPlayer();   // 1 ta defend sarflanadi, shield yoqiladi
            return;
        }

        // 4) Umuman defend yo‘q bo‘lsa -> slow effektni yoqamiz
        if (horseAnimal != null)
        {
            StartCoroutine(ApplyHitSlow());
        }
    }

    private IEnumerator ApplyHitSlow()
    {
        isUnderSlow = true;

        // Effektni yoqamiz (masalan particle, UI icon va h.k.)
        if (slowEffectObj != null)
            slowEffectObj.SetActive(true);

        // Avvalgi speed indexni saqlab qo‘yamiz
        int prevSpeedIndex = horseAnimal.CurrentSpeedIndex;

        // Slow speedga tushiramiz
        horseAnimal.Speed_CurrentIndex_Set(slowSpeedIndex);

        yield return new WaitForSeconds(slowDuration);
        if (!isNpc && isUnderSlow) { penaltyTime += slowDuration;
            OnPenaltyTime?.Invoke(penaltyTime);
        }
        // Avvalgi speedga qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(prevSpeedIndex);

        if (slowEffectObj != null)
            slowEffectObj.SetActive(false);

        isUnderSlow = false;
    }

    #endregion


}
