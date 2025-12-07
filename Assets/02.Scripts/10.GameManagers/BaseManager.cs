using Cinemachine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.HAP;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static Constants;


//This is script Interface script

public class BaseManager : MonoBehaviour
{

    [Header("-------------HorseAnimal---------------")]

    public MAnimal horseAnimal;
    public MAnimal playerAnim;
    public PlayerDataManager playerDataManager;

    [Header("----------------------------")]

    [Header("-------------Speed info---------------")]
    public ModalWindowManager modalWindowPopup;

    [Header("Main Time")]
    public TMP_Text timeText;
    public float mainTime = 0f;

    [Header("Lamp Realated")]
    public string LambOwner;
    [SerializeField] protected List<Transform> lambPositions;
    [SerializeField] protected GameObject myLamb;
    [SerializeField] protected GameObject startVFX;
    [SerializeField] protected GameObject targetPos;
    [SerializeField] protected GameObject targetVFX;

    [Header("Gameplay")]
    public Pickable pickableObj;
    public GameObject arrowObj;

    public GameObject currentGoatOwner;   // Hozir uloq kimda (player yoki AI)


    protected bool isCatched;
    public float lampCatchTime = 30f;
    public bool IsCatched { get => isCatched; set => isCatched = value; }

    public int catchCounter = 0;

    public enum RoomState { None, Warmup, GameStarted, WaterDropped, TimeFinished, HorseStamenaFinished, RiderStamenaFinished, LambReachTarget, GameFinished,PlayerEliminated, Won }
    public RoomState roomState = RoomState.None;

    [Header("Player Conditions")]
    [SerializeField] protected NPCDialogueManager NPCDialogueManager;


    [Header("Pooled VFX")]
    public VFXPool pool;
    public enum PlayerCondition
    {
        Start ,         // Dastlabki holat
        GettingTarget,   // Uloq olinmoqda
        GotTarget,       // Uloq olindi
        NearTarget,      // Finishga yaqin
        AwayTarget,      // Juda uzoqlashdi
        DroppedTarget,   // Uloq tushdi
        TakenTargetOthers, // Boshqalar olib ketdi
        WinnerSession,    // Davrada g'olib bolish
        LoserSession,     // Davrada yutqazish
        SpeedUp,       // Tezlikni oshirish
        StaminaLimit,  // Stamina cheklash
        HealthLimit,   // Sog'liq cheklash
        CatchLimit,  // Uloqni tutish cheklash vaqti tugayotganini bildiradi
        WaterEntered, // Suvga kirish
        EagleWatching, // Qushni kuzatish
        MapLimit, // Xarita oxiriga kelib qolmoq
        MainTimeOver, // Asosiy vaqt tugash arafasi
        None             // Dastlabki holat
    }

    public PlayerCondition currentCondition = PlayerCondition.None;
    public PlayerCondition CurrentCondition
    {
        get => currentCondition;
        set
        {
            currentCondition = value;

            if (NPCDialogueManager != null)
            {
                NPCDialogueManager.OpenNPCPanel(currentCondition);
            }
        }
    }
    #region Camera Details
    [SerializeField] protected ThirdPersonFollowTarget mainCam;
    [SerializeField] protected ThirdPersonFollowTarget sprintCam;
    [SerializeField] protected float frontDistance = 6f;
    [SerializeField] protected float backDistance = 3f;
    [SerializeField] protected float backOffsetY = 0.4f;
    #endregion

    #region Horse and Player Data
    [SerializeField] protected int defaultSpeedIndex = 5;  // odatiy tezlik
    [SerializeField] protected int boostSpeedIndex = 6;    // max tezlik
    #endregion

    #region Events
    public static Action<bool> OnGameStartFinishState;
    public static Action OnGameStarted;
    public static Action OnGameEnded;
    public static Action<float> OnGoatPickedTime;
    public static Action OnHideCatchTime;
    public static Action OnResetTarget;

    public static Action<bool> OnGoatPicked;
    #endregion
    [Header("Checkpoints")]
    [SerializeField] protected List<CheckpointTrigger> checkpoints = new List<CheckpointTrigger>();
    protected int passedCheckpointCount = 0;

    [Header("Local Player")]
    [SerializeField] protected GameObject LocalRiderRoot;
    private Coroutine pickUpTimerCoroutine;

    [Header("Room Resources")]
    public GameObject walkZonePrefab;

    [Header("Player Start Point")]
    public Transform startTarget;
    public float warmUpTime;
    private bool playerReachedStart = false;
    public static Transform CurrentStartPoint { get; private set; }
    public static float CurrentWarmupTime { get; private set; }
    public static Action<Transform,float> OnStartPoint;



  
    public static BaseManager Instance { get; protected set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }

    protected virtual void Start()
    {
        if (pickableObj != null)
        {
            pickableObj.OnPicked.AddListener(OnUloqPicked);
        }
        SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);
        RegisterStartPoint(startTarget, warmUpTime);
        DisableMainGameObjects();
    }

    protected virtual void Update()
    {
        switch (roomState)
        {
            case RoomState.Warmup:
                WarmUpTick();
                break;

            case RoomState.GameStarted:
                MainGameTimeTick();
                break;

            case RoomState.GameFinished:
                // hech narsa qilmaydi
                break;
        }
    }
    protected virtual void OnEnable()
    {
        HorseMine.OnReachedStartTarget += HandlePlayerReachedStart;
        OnGameStartFinishState += GameStartedAction;
        OnGameStartFinishState += GameObjectsEnable;
        KopkariMainUI.OnSprintStart += HorseSprint;
        KopkariMainUI.OnSprintEnd += HorseDefaultSpeed;
        BoostersContainer.OnSprintEffectStart += SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd += SprintCameraDisable;
        PlayerDataManager.OnLocalPlayerObject += RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse += RegisterPlayerAndHorse;


    }
    protected virtual void OnDisable()
    {
        HorseMine.OnReachedStartTarget -= HandlePlayerReachedStart;
        OnGameStartFinishState -= GameStartedAction;
        OnGameStartFinishState -= GameObjectsEnable;
        KopkariMainUI.OnSprintStart -= HorseSprint;
        KopkariMainUI.OnSprintEnd -= HorseDefaultSpeed;
        BoostersContainer.OnSprintEffectStart -= SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd -= SprintCameraDisable;
        PlayerDataManager.OnLocalPlayerObject -= RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse -= RegisterPlayerAndHorse;
    }
    protected virtual void MainGameTimeTick()
    {
        if (mainTime > 0)
        {
            mainTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(mainTime / 60);
            int seconds = Mathf.FloorToInt(mainTime % 60);
            timeText.SetText($"{minutes:00}:{seconds:00}");

            if (mainTime <= 3)
            {
                timeText.color = Color.red;
                currentCondition = PlayerCondition.MainTimeOver;
            }
                
        }
        else
        {
            timeText?.SetText("00:00");
            roomState = RoomState.GameFinished;
        }
    }
    private void WarmUpTick()
    {
        // Agar player allaqachon start joyiga yetgan bo'lsa,
        // roomState event handlerda InGame bo'lib bo'lgan bo'ladi.
        // Lekin baribir tekshirib qo'yamiz:
        if (playerReachedStart)
        {
            roomState = RoomState.GameStarted;
            return;
        }

        if (warmUpTime > 0f)
        {
            warmUpTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(warmUpTime / 60);
            int seconds = Mathf.FloorToInt(warmUpTime % 60);
            timeText.SetText($"{minutes:00}:{seconds:00}");

            if (warmUpTime <= 3f)
            {
                timeText.color = Color.red;
                // xohlasang PlayerCondition.WarmUpAlmostOver degan holat qo'yasan
            }
        }
        else
        {
            // 3) Vaqt tugadi, lekin playerReachedStart = false
            HandlePlayerNotReachedInTime();
        }
    }
    public void DisableMainGameObjects()
    {
        myLamb?.SetActive(false);
        startVFX?.SetActive(false);
        targetPos?.SetActive(false);
        targetVFX?.SetActive(false);

    }
    public void EnableULoq()
    {
        myLamb?.SetActive(true);
        startVFX?.SetActive(true);
    }
    private void RegisterPlayerAndHorse(MAnimal horse, MAnimal player)
    {
        horseAnimal = horse;
        playerAnim = player;
    }
    public void RegisterStartPoint(Transform point, float time)
    {
        CurrentStartPoint = point;
        CurrentWarmupTime = time;

        // Hozir allaqachon subscribe bo‘lib turganlar uchun
        OnStartPoint?.Invoke(point, time);
    }
    private void HandlePlayerReachedStart()
    {
        playerReachedStart = true;
        EnableULoq();
        // WarmUp ni kutmasdan darhol main gamega o'tamiz
        roomState = RoomState.GameStarted;
        timeText.color = Color.white;   // agar warmupda rangni o'zgartirgan bo'lsang, reset qil
    }

    private void HandlePlayerNotReachedInTime()
    {
        roomState = RoomState.PlayerEliminated;
        timeText.SetText("DQ"); // yoki "ELIM", yoki "00:00" + popup

        // Bu yerda:
        // - Player controlni o'chirasan
        // - Horse movementni lock qilasan
        // - "Siz belgilangan joyga yetib bormadingiz" popup
        // - kerak bo'lsa: Main menu / Retry tugmalari
    }
    #region Pick Up Uloq
    public virtual void StartPickUpTime()
    {
        if (pickUpTimerCoroutine != null)
        {
            StopCoroutine(pickUpTimerCoroutine);
            pickUpTimerCoroutine = null;
        }

        IsCatched = true;
        lampCatchTime = 30f;

        // UI bildirishi uchun
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

            //// UI ga yangilangan vaqtni berish
            OnGoatPickedTime?.Invoke(t);
        }

        // Agar t 0 bo‘ldi va hali ham egasi local player bo‘lsa:
        if (t <= 0 && IsCatched)
        {
           // Debug.Log("⏳ Timer finished → TriggerEvent()");
            TriggerEvent();   // local playerda trigger ishlaydi
        }

        StopPickUpTime();
    }
    protected virtual void OnUloqPicked(GameObject pickerObj)
    {
        NotifyGoatOwner(pickerObj.transform.root.gameObject, true);
        string pickerName = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        if (pickerObj.name == pickerName)
        {
            catchCounter++;
            //StartPickUpTime();
        }
    }
    public void NotifyGoatOwner(GameObject ownerRoot, bool hasGoat)
    {
        bool isLocalPlayer = (LocalRiderRoot != null && ownerRoot == LocalRiderRoot);

        if (hasGoat)
        {
            currentGoatOwner = ownerRoot;

            if (isLocalPlayer)
            {
                // faqat local player uchun timer + UI
                OnGoatPicked?.Invoke(true);
                IsCatched = true;
                StartPickUpTime();

            }
            else
            {
                if (IsCatched)          // 🔹 faqat local avval "egasi" bo'lgan bo'lsa
                {
                    StopPickUpTime();   // ichida OnGoatPicked(false) bor
                }   // ichida OnGoatPicked(false) bor
                IsCatched = false;
            }
        }
        else
        {
            if (currentGoatOwner == ownerRoot)
            {
                if (isLocalPlayer)
                {
                    // local player uloqni yo‘qotdi
                    StopPickUpTime();  // OnGoatPicked(false) shu yerda bo‘ladi
                }

                currentGoatOwner = null;
                IsCatched = false;
            }
        }
    }
    #endregion

    #region Drop Uloq
    public virtual void TriggerEvent()
    {
        IsCatched = false;
        currentGoatOwner = null;
        playerDataManager.DropObject();
        OnGoatPicked?.Invoke(false);
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

        /*Debug.Log("⛔ StopPickUpTime: Local player uloqni yo‘qotdi, timer to‘xtadi.");*/
    }

    #endregion
    public virtual void WinOrLosePage() { Debug.Log("BaseManager: override WinOrLosePage"); }
    public virtual void HandleWin()
    { 
        currentCondition = PlayerCondition.WinnerSession;
        Debug.Log("BaseManager: override HandleWin"); 
    }
    public virtual void HandleLose() 
    { 
        currentCondition = PlayerCondition.LoserSession;
        Debug.Log("BaseManager: override HandleLose");
    }

    public virtual void GameStartedAction(bool state)
    {
        roomState = state ? RoomState.Warmup : RoomState.None;
    }

    public virtual void GameObjectsEnable(bool enable)
    {
        // Gameplay elementlar
        //arrowObj?.SetActive(enable);
        //myLamb?.SetActive(enable);
        if (!enable)
        {
            roomState = RoomState.None;
        }
        else
        {
            OnHideCatchTime?.Invoke();
        }
    }

    public virtual void StartGame()
    {
        OnResetTarget?.Invoke();
        CurrentCondition = PlayerCondition.Start;
        OnGameStartFinishState?.Invoke(true);
        OnGameStarted?.Invoke();
    }


    public virtual void ContinueGame()
    {
        StartGame();
        catchCounter = 0;
    }







    public virtual void ContinueGameChanger(Transform lambPos, Transform targetPos)
    {
        myLamb?.transform.SetPositionAndRotation(lambPos.position, lambPos.rotation);
        startVFX.transform.position = lambPos.position;
        targetPos?.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
        targetVFX.transform.position = targetPos.position;
    }

    public void MarkPlayerReachedTarget()
    {
        if (roomState == RoomState.GameStarted)
        {
            roomState = RoomState.LambReachTarget;
            WinOrLosePage();
        }
    }
    public void RegisterLocalRider(GameObject riderRoot)
    {
        LocalRiderRoot = riderRoot;
    }
    #region Camera Section
    public virtual void CameraBackState(bool state)
    {
        if (state) LookBack();
        else MainCam();
    }
    public void LookBack()
    {
        if (mainCam == null) return;

        horseAnimal.UseCameraInput = false;

        // masofani va offsetni biroz o'zgartirishni xohlasang:
        mainCam.SetCameraDistance(backDistance);
        mainCam.AddVerticalOffset(backOffsetY);

        // faqat flag'ni yoqamiz
        mainCam.SetLookBackMode(true);
    }

    public void MainCam()
    {
        if (mainCam == null) return;

        // masofani va offsetni front holatga qaytaramiz
        mainCam.SetCameraDistance(frontDistance);
        mainCam.AddVerticalOffset(0f); // yoki front uchun alohida offset bo'lsa o'shani

        mainCam.SetLookBackMode(false);

        StartCoroutine(EnableHorseInputDelayed());
    }
    private IEnumerator EnableHorseInputDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        horseAnimal.UseCameraInput = true;
    }

    private void SprintCameraEnable()
    {
        sprintCam.SetPriority(true);
    }
    private void SprintCameraDisable() { sprintCam.SetPriority(false); }
    #endregion

    #region CheckPoints
    /// <summary>
    /// Checkpoint triggeri chaqiradigan metod.
    /// Tartibi MUHIM EMAS, faqat uloq qo‘lida bo‘lishi kerak.
    /// </summary>
    public virtual void OnCheckpointReached(CheckpointTrigger checkpoint, GameObject riderObj)
    {
        // O'yin holati tekshiruvi
        if (roomState != RoomState.GameStarted)
            return;

        // Uloq qo'lda bo'lmasa umuman hisoblamaymiz
        if (!IsCatched || currentGoatOwner == null)
        {
            Debug.Log("[Checkpoint] Uloqsiz o‘tildi – hisoblanmadi");
            return;
        }

        // ✅ Faqat hozir uloqni ushlab turgan rider uchun hisoblaymiz
        // root bilan solishtiramiz
        if (riderObj.transform.root.gameObject != currentGoatOwner)
        {
            // Boshqa rider, lekin uloq unda emas
            Debug.Log("[Checkpoint] Bu riderda uloq yo‘q – hisoblanmadi");
            return;
        }

        if (checkpoint == null)
            return;

        // Agar bu checkpoint allaqachon uloq bilan o‘tilgan bo‘lsa – qayta sanamaymiz
        if (checkpoint.IsPassedWithGoat)
        {
            Debug.Log("[Checkpoint] Bu checkpoint oldin ham uloq bilan o‘tilgan");
            return;
        }

        // ✅ Birinchi marta, hozirgi goat owner bilan o‘tildi:
        checkpoint.MarkPassedWithGoat();
        passedCheckpointCount++;

        Debug.Log($"[Checkpoint] {riderObj.name} uloq bilan checkpoint o'tdi. Jami: {passedCheckpointCount}/{checkpoints.Count}");

        if (passedCheckpointCount >= checkpoints.Count && checkpoints.Count > 0)
        {
            OnAllCheckpointsCompleted();
        }
    }

    /// <summary>
    /// Barcha checkpointlar ULOQ bilan o‘tilgandan keyingi holat.
    /// Bu metodni override qilib, har bir roomda o‘zcha flow qilsa bo‘ladi.
    /// </summary>
    public virtual void OnAllCheckpointsCompleted()
    {
        Debug.Log("[Checkpoint] 🔥 Barcha checkpointlar uloq bilan o‘tilgan!");

        // Masalan:
        // - myTargetPos ni enable qilish
        // - targetVFX yoqish
        // - finish triggerni aktivlash
        // yoki darhol MarkPlayerReachedTarget();

        // Misol uchun shunchaki finishni ruxsat qilib qo‘yamiz:
        // CurrentCondition = PlayerCondition.NearTarget; // NPC aytadi: "Endi finishga bor!"
    }

    #endregion

    #region Horse Speed
    private void HorseSprint()
    {
        if (horseAnimal != null)
        {
            horseAnimal.Speed_CurrentIndex_Set(boostSpeedIndex);
            SprintCameraEnable();
        }
    }

    private void HorseDefaultSpeed()
    {
        if (horseAnimal != null) { horseAnimal.Speed_CurrentIndex_Set(defaultSpeedIndex); SprintCameraDisable(); }
    }
    #endregion

    

}
