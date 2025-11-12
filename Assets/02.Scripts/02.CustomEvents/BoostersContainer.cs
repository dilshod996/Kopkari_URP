using MalbersAnimations;
using MalbersAnimations.Controller;
using System;
using System.Collections;
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

    private Coroutine defendCoroutine;
    public int walkZoneCount = 2;
    public int defendCount = 2;
    public int hitCount = 0;
    public event Action OnWalkZoneAdded;
    public event Action OnWalkZoneRemoved;

    // events for Defend
    public event Action OnDefendAdded;
    public event Action OnDefendRemoved;
    [Header("Npc info")]
    public bool isNpc = false; // Npc bo‘lsa, bu true bo‘ladi

    [Header("Horse ning animation componenti")]
    public MAnimal horseAnimal;

    [Header("Speed Improver")]
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;

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

    #region Starting Events

    private void Start()
    {
        //playerInitialSpeed = horseAnimal.CurrentSpeedIndex;
        InitialButtonsData();

        if (!isNpc && UIButtonActions.Instance != null)
        {
            UIButtonActions.Instance.Bind(this);
            // Birinchi holatni darhol qo'yamiz
            UpdateUIStates();
        }
       

    }

    private void OnEnable()
    {
        if (!isNpc && UIButtonActions.Instance != null)
        {
            OnWalkZoneAdded += HandleWalkZoneChanged;
            OnWalkZoneRemoved += HandleWalkZoneChanged;
            OnDefendAdded += HandleDefendChanged;
            OnDefendRemoved += HandleDefendChanged;
        }
    }

    private void OnDisable()
    {
        if (!isNpc && UIButtonActions.Instance != null)
        {
            OnWalkZoneAdded -= HandleWalkZoneChanged;
            OnWalkZoneRemoved -= HandleWalkZoneChanged;
            OnDefendAdded -= HandleDefendChanged;
            OnDefendRemoved -= HandleDefendChanged;
        }
    }

    private void HandleWalkZoneChanged()
    {
        UIButtonActions.Instance?.SetWalkZoneState(walkZoneCount > 0);
    }

    private void HandleDefendChanged()
    {
        UIButtonActions.Instance?.SetDefendState(defendCount > 0);
    }

    private void UpdateUIStates()
    {
        // Hamma holatlarni bir joyda qo‘yamiz
        UIButtonActions.Instance?.SetWalkZoneState(walkZoneCount > 0);
        UIButtonActions.Instance?.SetDefendState(defendCount > 0);
    }
    #endregion

    #region Buttons Data Update
    private void InitialButtonsData()
    {
        UIButtonActions.Instance.InitializeData(defendCount, walkZoneCount, hitCount);
    }
    #endregion

    #region Player Stats Speed
    public void SprintStatFull()
    {
        if(playerStats != null)
        {
            Debug.Log("Pinned " + playerStats.PinnedStat.Name);
            //playerStats.Stat_ModifyValue(staminaStatName, 100f, StatOption.SetMaxValue);
            playerStats.Stat_Pin_ModifyValue(100f);
        }
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

        // Eski speed ni saqlaymiz
        horseAnimal.Speed_CurrentIndex_Set(6);

        yield return new WaitForSeconds(maxSpeedDuration);

        // Avvalgi speedni qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(5);
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
            UIButtonActions.Instance.UpdateWalkZoneText(walkZoneCount);
            OnWalkZoneAdded?.Invoke();
        }
            
    }
    public void DecreaseWalkZone()
    {
        walkZoneCount = Mathf.Max(0, walkZoneCount - 1);            
        if (!isNpc)
        {
            UIButtonActions.Instance.UpdateWalkZoneText(walkZoneCount);
            OnWalkZoneRemoved?.Invoke();
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
            UIButtonActions.Instance.UpdateDefendText(defendCount);
            OnDefendAdded?.Invoke();
        }
    }
    public void DecreaseDefend()
    {
        defendCount = Mathf.Max(0, defendCount - 1);
        if (!isNpc)
        {
            UIButtonActions.Instance.UpdateDefendText(defendCount);
            OnDefendRemoved?.Invoke();
        }
            
    }
    public void DefendPlayer()
    {
        if (defendCount <= 0)
        {
            Debug.Log("❌ Defend mavjud emas");
            return;
        }
        DecreaseDefend();
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendQobiq.SetActive(false);
        }
        defendCoroutine = StartCoroutine(DefendObject());
        OnDefendActivated?.Invoke();
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
        defendQobiq.SetActive(true);
        if (horseAnimal != null && horseAnimal.CurrentSpeedIndex != playerInitialSpeed)
        {
            horseAnimal.Speed_CurrentIndex_Set(playerInitialSpeed);
        }
        yield return new WaitForSeconds(defendTime);
        defendQobiq.SetActive(false);
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
}
