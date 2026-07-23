using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using UnityEngine;
using System.Collections;

public class NPCGetLamb_CodeAI : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private int id = 0;
    [SerializeField] private string nameNpc;
    [SerializeField] private string teamName;
    [Header("Dependencies")]
    [SerializeField] private MPickUp pickUp;
    [SerializeField] private MAnimalBrain brain;
    [SerializeField] private MAnimalAIControl ai;

    [Header("States (Scriptable Objects)")]
    [SerializeField] private MAIState moveState;      // universal yurish state

    [Header("Targets")]
    [SerializeField] private GameObject lambPoint;     // uloq turgan nuqta
    [SerializeField] private Transform finishPoint;   // finish nuqta
    [SerializeField] private Transform[] checkpoints; // 1-round, 2-round, 3-round bo‘yicha punktlar

    [Header("Pickup Timing")]
    [SerializeField] private float waitToPickUp = 2f;      // uloq zonasida turib, necha sekunddan keyin olish
    [SerializeField] private float itemPickedDuration = 20f; // qo‘lda ushlab turish vaqti

    [Header("Lamb Parent (optional)")]
    [SerializeField] private GameObject lambParentObj; // uloq parenti (bo‘sh emasligini tekshirish uchun)

    private int currentCheckpointIndex = -1;
    public bool hasLamb = false;
    // ⏱ coroutinelar
    private Coroutine waitCoroutine;
    private Coroutine itemTimerCoroutine;
    private float currentItemTime;
    private bool allCheckpointsDone = false; // hamma checkpointlar uloq bilan tugaganmi?

    public bool HasLamb => hasLamb;   // faqat o‘qish uchun
    private bool[] npcPassedCheckpoints;

    [Header("Projectiles")]
    [SerializeField] private BoostersContainer boosterContainer;

    [Header("Warmup / Start point / Finish Point")]
    [SerializeField] private Transform targetPoint;   // NPC borishi kerak bo'lgan start nuqta
    [SerializeField] private Transform secondRoundWarmPoint;
    [SerializeField] private float slowDuration = 5f;     // necha sekund sekin yuradi
    [SerializeField] private int slowSpeedIndex = 2;      // slow paytidagi speed index

    private bool isFinished = false;

    private void Awake()
    {
        if (!brain) brain = GetComponentInParent<MAnimalBrain>();
        if (!ai) ai = GetComponentInParent<MAnimalAIControl>();
        if (!pickUp) pickUp = GetComponentInChildren<MPickUp>();

    }

    private void OnEnable()
    {
        KopkariManager.OnGameStarted += OnGameStart;
        HorseMine.OnReachedStartTarget += OnPlayerReachedStart;
        TargetReachEvent.OnReachedTargetWithLamb += HandleReachedTargetWithLamb;
        TargetReachEvent.OnRoundEnded += HandleFinish;
        //BaseManager.OnGoatPicked += HandleGoatOwnership;
    }

    private void OnDisable()
    {
        KopkariManager.OnGameStarted -= OnGameStart;
        HorseMine.OnReachedStartTarget -= OnPlayerReachedStart;
        TargetReachEvent.OnReachedTargetWithLamb -= HandleReachedTargetWithLamb;
        TargetReachEvent.OnRoundEnded -= HandleFinish;
        //BaseManager.OnGoatPicked -= HandleGoatOwnership;

        // xavfsizlik uchun
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        if (itemTimerCoroutine != null) StopCoroutine(itemTimerCoroutine);
    }
    public int GetId()
    {
        return id;
    }



    #region Movers

    // 🔹 Oddiy helper: target + state ni bir joyda chaqiramiz
    private void MoveTo(Transform target)
    {
        if (!ai || !brain || !target) return;
        // Har turdagi targetga alohida stop distance
        if (target == lambPoint)
        {
            ai.StoppingDistance = 0.4f;
        }
        else if (target == finishPoint)
        {
            KopkariManager.Instance?.FinalPosState(true);
            ai.StoppingDistance = 0.7f;
        }
        else if(target == targetPoint)
        {
            ai.StoppingDistance = 1.5f;
        }
        else if (target == secondRoundWarmPoint)
        {
            ai.StoppingDistance = 1.5f;
            MoveSecondWarmUpLocation(ai.animal);
        }
        else
        {
            // checkpointlar
            ai.StoppingDistance = 0;
        }
        ai.SetTarget(target, true); // AIControl targetga path hisoblaydi
        moveState?.Play(brain);     // Brain shu moveState’ga o‘tadi
    }
    private void MoveToNextPoint()
    {
        // Checkpointlar umuman bo‘lmasa → to‘g‘ri finishga bor
        if (checkpoints == null || checkpoints.Length == 0)
        {
            if (finishPoint != null)
                MoveTo(finishPoint);
            return;
        }

        // Agar hali boshlangan bo‘lmasa, 0 dan start qilamiz
        if (currentCheckpointIndex < 0)
            currentCheckpointIndex = 0;

        // Hali checkpointlar tugamagan bo‘lsa
        if (currentCheckpointIndex < checkpoints.Length)
        {
            Transform target = checkpoints[currentCheckpointIndex];
            //KopkariResultsManager.Instance.OnTriggerPoint(id);
            // MUHIM: targetni olayapmiz → keyin indexni +1 qilamiz
            currentCheckpointIndex++;
            //Debug.Log("CheckPoint index: " + currentCheckpointIndex);
            MoveTo(target);
        }
        else
        {
            // Barcha checkpoint tugadi → finishga
            if (finishPoint != null)
                MoveTo(finishPoint);
        }
    }
    #endregion

    #region Start Events
    // 🔹 O‘yin boshlanganda – lambga bor
    public void OnGameStart()
    {
        hasLamb = false;
        isFinished = false;
        currentCheckpointIndex = -1;

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
        allCheckpointsDone = false;

        if (checkpoints != null && checkpoints.Length > 0)
        {
            npcPassedCheckpoints = new bool[checkpoints.Length];
            for (int i = 0; i < npcPassedCheckpoints.Length; i++)
                npcPassedCheckpoints[i] = false;
        }

        if (targetPoint != null)
        {
            MoveTo(targetPoint);
        }
        else if (lambPoint != null)
        {
            // fallback: startpoint berilmagan bo'lsa eski xulq-atvor
            MoveTo(lambPoint.transform);
        }
        KopkariResultsManager.Instance?.Register(id, nameNpc, teamName);
    }
    private void OnPlayerReachedStart()
    {
        // Player start joyiga yetib keldi → endi NPC uloqqa qarab yugursa bo'ladi
        StartCoroutine(DelayReachStart());
    }
    private IEnumerator DelayReachStart()
    {
        yield return new WaitForSeconds(2f);
        if (lambPoint != null)
        {
            MoveTo(lambPoint.transform);
        }
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
            MoveTo(secondRoundWarmPoint);
        }
        if (hasLamb || waitCoroutine != null) return;
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
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
        // ❗ NPC hali uloqni olmagan bo‘lsa – yana lambga qaytadi
        if (!hasLamb && lambPoint != null)
        {
            MoveTo(lambPoint.transform);
        }
    }
    #endregion

    #region Timer

    // =========================
    // 2) ULOQ QO‘LDA TURISH TIMERI
    // =========================
    private void StartItemTimer()
    {
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
        // Agar MPickUp hali ham itemni ushlab turgan bo‘lsa – tashlab yuborish ixtiyoriy
        if (pickUp != null && pickUp.Item!=null)
        {
            pickUp.DropItem();  // agar sen timeoutda tashlashni xohlamasang, bu qatorni o‘chirib tashlashing mumkin
            KopkariResultsManager.Instance.OnLambDropped(id);
        }

        hasLamb = false;

        // BaseManager’ga xabar berish (ixtiyoriy)
        KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);

        // Timeout → yana uloqqa yuramiz
        if (lambPoint != null)
            MoveTo(lambPoint.transform);
    }

    private IEnumerator WaitToPickUpLamb()
    {
        yield return new WaitForSeconds(waitToPickUp);
        // bu paytda ham hanuzgacha item fokusda va qo‘lda yo‘qligini tekshiramiz
        if (pickUp != null && pickUp.Item == null && pickUp.FocusedItem != null)
        {
            pickUp.PickUpItem();
            KopkariResultsManager.Instance.OnLambPicked(id);
            hasLamb = true;
            // BaseManager’ga xabar berish (agar sendagi NotifyGoatOwner bo‘lsa)
            KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, true);

            // 1-checkpointdan boshlaymiz
            currentCheckpointIndex = FindNextCheckpointIndex();
            if (currentCheckpointIndex >= 0)
            {
                // Hali o'tilmagan checkpointlar bor -> o‘sha tarafga
                MoveToNextPoint();
            }
            else
            {
                // Hamma checkpointlar allaqachon uloq bilan o‘tilgan -> to‘g‘ri finishga
                if (finishPoint != null)
                    MoveTo(finishPoint);
            }

            // Uloq qo‘lda turgan vaqtni ishga tushiramiz
            StartItemTimer();
        }
        else
        {
            // item yo‘q bo‘lib qolgan bo‘lsa – yana lambPointga yurish
            hasLamb = false;
            if (lambPoint != null)
                MoveTo(lambPoint.transform);
        }

        waitCoroutine = null;
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
        if (checkpoints == null || checkpoints.Length == 0) return;
        // qaysi indexdagi checkpoint ekanini topamiz
        int idx = -1;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] == checkpoint.transform)
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
            if (finishPoint != null)
                MoveTo(finishPoint);
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
        if (checkpoints == null || checkpoints.Length == 0)
            return -1;

        // Debug uchun:
        // Debug.Log("=== NPC FindNextCheckpointIndex ===");
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (!npcPassedCheckpoints[i])
            {
                // Debug.Log($"NPC CP[{i}] {checkpoints[i].name} -> NOT passed");
                return i;
            }
            // else Debug.Log($"NPC CP[{i}] {checkpoints[i].name} -> already passed");
        }

        // hammasi o'tilgan
        return -1;
    }

    private bool AreAllNpcCheckpointsPassed()
    {
        if (checkpoints == null || checkpoints.Length == 0 || npcPassedCheckpoints == null)
            return false;

        for (int i = 0; i < npcPassedCheckpoints.Length; i++)
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
        lambPoint.gameObject.SetActive(false);
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
        if (secondRoundWarmPoint != null)
        {
            MoveTo(secondRoundWarmPoint);
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
        if (!hasLamb) return;

        hasLamb = false;
        StopItemTimer();

        // agar pickUp hali ham has_Item bo‘lsa – tashlab yuboramiz
        if (pickUp != null && pickUp.Has_Item)
        {
            pickUp.DropItem();
        }

        KopkariManager.Instance?.NotifyGoatOwner(transform.root.gameObject, false);

        // Endi yana uloqqa qaytamiz
        if (lambPoint != null)
            MoveTo(lambPoint.transform);
    }
    private void HandleGoatOwnership(bool ownerHasGoat)
    {
        // 1) Agar men uni ushlab turmasam → har doim lambPointga qaytaman
        if (!pickUp.Has_Item)
        {
            // Uloq kimga o'tganidan qat’i nazar men endi egasi emasman
            hasLamb = false;

            StopItemTimer(); // agar timer ishlayotgan bo‘lsa

            // darhol uloqqa qaytamiz
            MoveTo(lambPoint.transform);
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

    #region Stop Rider
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
