using Cinemachine;
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


public class BaseManager : MonoBehaviour
{

    [Header("-------------HorseAnimal---------------")]

    public MAnimal horseAnimal;

    [Header("----------------------------")]


    [Header("-------------Main Camera---------------")]
    public CinemachineVirtualCamera mainVirtualCamera;

    [Header("----------------------------")]
    [Header("-------------Back Camera---------------")]
    public BackViewCam backviewCam;

    [Header("----------------------------")]

    [Header("-------------Speed info---------------")]
    public GameObject speedShader;
    //[Header("-------------Camera Pivot Point---------------")]
    //public Transform camPivotPoint;

    //[Header("----------------------------")]
    public SlideTextWithTimer lambCatchTimer;
    //public NotificationManager TimeNotification;
    public ModalWindowManager modalWindowPopup;
    [Header("UI")]
    public GameObject mobileCanvas;
    public GameObject roomCanvas;


    [Header("Main Time")]
    public TMP_Text timeText;
    public float mainTime = 0f;

    [Header("Lamp Realated")]
    public string LambOwner;
    private Coroutine lampTimerCoroutine;
    [SerializeField] protected List<Transform> lambPositions;
    [SerializeField] protected GameObject myLamb;
    [SerializeField] protected GameObject lampVFX;
    [SerializeField] protected GameObject myTargetPos;
    [SerializeField] protected GameObject targetVFX;

    [Header("Gameplay")]
    public Pickable pickableObj;
    public GameObject arrowObj;
    //[SerializeField] protected DollyCameraController dollyCamera;
    public PlayerDataManager playerData;
    public SlideAndFadeImage speedUxScript;

    [Header("Win or Lose Pages")]
    [SerializeField] protected UIFinalResult WinPanel;
    [SerializeField] protected UIWinSession smallWinPage;
    [SerializeField] protected UIUserLoose LoosePanel;

    [Header("Prizes")]
    [SerializeField] protected PrizeManager prizeManager;


    protected Coroutine boboyNpcCoroutine;
    protected bool isCatched;
    protected bool isFinished;
    public float lampCatchTime = 30f;
    public bool IsCatched { get => isCatched; set => isCatched = value; }

    public int catchCounter = 0;

    public enum RoomState { None, GameStarted, WaterDropped, TimeFinished, HorseStamenaFinished, RiderStamenaFinished, LambReachTarget, GameFinished, Won }
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
    public static BaseManager Instance { get; protected set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    protected virtual void Start()
    {
        if (pickableObj != null)
        {
            pickableObj.OnPicked.AddListener(OnUloqPicked);
        }
    }

    protected virtual void Update()
    {
        if (roomState == RoomState.GameStarted)
        {
            MainGameTimeTick();
        }
    }

    protected virtual void MainGameTimeTick()
    {
        if (mainTime > 0)
        {
            mainTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(mainTime / 60);
            int seconds = Mathf.FloorToInt(mainTime % 60);
            timeText?.SetText($"{minutes:00}:{seconds:00}");

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

    public virtual void StartPickUpTime()
    {
        if (lampTimerCoroutine != null) StopCoroutine(lampTimerCoroutine);
        if (IsCatched) return;

        IsCatched = true;
        isFinished = false;
        currentCondition = PlayerCondition.GotTarget;
        mainVirtualCamera.Priority = 15;
        lampCatchTime = 30f;
        lambCatchTimer.StartSlide(lampCatchTime);
       // TimeNotification?.CustomeUpdate("Olg'a", "00:30");
        backviewCam.SetBackViewState(true);
        //currentCondition = PlayerCondition.GotTarget;
        //lampTimerCoroutine = StartCoroutine(LampCatchCountdown());
    }

    public virtual void TriggerEvent()
    {
        IsCatched = false;
        playerData?.DropObject();
        //currentCondition = PlayerCondition.None; // Uloq tushdi
        //currentCondition = PlayerCondition.DroppedTarget;
        backviewCam.SetBackViewState(false);
    }

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
        roomState = state ? RoomState.GameStarted : RoomState.None;
    }

    public virtual void GameObjectsEnable(bool enable)
    {
        // UI elementlar
        mobileCanvas?.SetActive(enable);
        roomCanvas?.SetActive(enable);
        // Gameplay elementlar
        arrowObj?.SetActive(enable);
        myLamb?.SetActive(enable);
        if(mainVirtualCamera.Priority != 15)
        {
            mainVirtualCamera.Priority = 15; // Asosiy kamera faqat o'yin boshlanganda ishlaydi
        }

        // State faqat false bo‘lsa darhol reset qilinadi
        if (!enable)
        {
            roomState = RoomState.None;
        }
        if (lambCatchTimer != null && enable)
        {
            lambCatchTimer.ForceHide(); // Uloq olish vaqtini to'xtatamiz
        }
    }

    public virtual void StartGame()
    {
        myTargetPos.GetComponent<TargetReachEvent>()?.ResetTrigger();
        if(backviewCam != null)
        {
            backviewCam.SetBackViewState(false); // Orqa ko'rinishni o'chiramiz
        }
        
        // 🟡 1. Avval pozitsiyani yangilash
        var prize = prizeManager.GetCurrentPrize();
        if (prize != null)
        {
            int index = prizeManager.CurrentPrizeIndex;
            if (index > 0 && index < lambPositions.Count)
            {
                ContinueGameChanger(
                    lambPositions[index - 1],
                    lambPositions[index]
                );
            }

            mainTime = prize.roundTime;
        }

        // 🟢 2. Keyin obyektlarni yoqish
        GameObjectsEnable(true);

        // 🔵 3. RoomState holatini yangilash
        GameStartedAction(true);
        CurrentCondition = PlayerCondition.Start;
    }


    public virtual void ContinueGame()
    {
        var prize = prizeManager.GetCurrentPrize();
        if (mainTime <= 0 || currentCondition == PlayerCondition.LoserSession) 
        {
            List<Prize> losePrizes = prize.losePrizes;
            prizeManager.SaveRoundPrize(losePrizes, 0, catchCounter);
            //SavePrizeDataToPlayerPrefs(losePrizes);
            Debug.Log("lost round, no time left");
        }
        else if(currentCondition== PlayerCondition.WinnerSession)
        {
            float remainTime = prize.roundTime - mainTime;
            Debug.Log($"[Before Save] PrizeIndex: {prizeManager.CurrentPrizeIndex}, Total: {prizeManager.prizeConfig.prizes.Count}, RemainTime: {remainTime}");
            prizeManager.SaveRoundPrize(prize.winPrizes, remainTime, catchCounter);
            //SavePrizeDataToPlayerPrefs(prize.winPrizes);
        }

        prizeManager.MoveToNextPrize();

        Debug.Log($"[After Move] PrizeIndex: {prizeManager.CurrentPrizeIndex}, HasMore: {prizeManager.HasMorePrizes()}");
        // 1. Prize tugaganmi?
        if (!prizeManager.HasMorePrizes())
        {
            Debug.Log("O'yin tugadi. Barcha sovrinlar o'ynaldi.");
            WinPanel?.gameObject.SetActive(true); // Umumiy g‘alaba sahifasi
            return;
        }

        // 2. Prize indexni oshiramiz
        

        // 3. O'yinni yangi prize bilan boshlaymiz
        StartGame();
        catchCounter = 0;
    }
    #region Save prize data to PlayerPrefs

    public void SavePrizeDataToPlayerPrefs(List<Prize> prizes)
    {
        foreach (var prize in prizes)
        {
            string key = prize.prizeType.ToString().ToLower(); // Misol: "money", "sheep", ...

            float oldValue = PlayerPrefs.GetFloat(key); // float qiymatni o‘qish
            float newValue = oldValue + prize.rewardAmount; // float qo‘shish

            PlayerPrefs.SetFloat(key, newValue); // float saqlash
        }

        PlayerPrefs.Save(); // Doimiy saqlash
    }

    private bool PrizesContainsKey(string key)
    {
        return typeof(Prizes).GetFields().Any(f => f.GetRawConstantValue().ToString().Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    protected virtual void OnUloqPicked(GameObject pickerObj)
    {
        Debug.Log("Picked by: " + pickerObj.name);
        string pickerName = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        if (pickerObj.name == pickerName)
        {
            catchCounter++;
            //currentCondition = PlayerCondition.GotTarget;
            StartPickUpTime();
        }
    }

    public virtual void ContinueGameChanger(Transform lambPos, Transform targetPos)
    {
        myLamb?.transform.SetPositionAndRotation(lambPos.position, lambPos.rotation);
        lampVFX.transform.position = lambPos.position;
        myTargetPos?.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
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

    #region UI & UX Details
    public virtual void SpeedBoosterGet(float duration)
    {
        speedUxScript.gameObject.SetActive(true);
        speedUxScript.StartSlide(duration);
    }
    public virtual void SpeedShaderActive(bool value)
    {
        speedShader.SetActive(value);
    }
    #endregion
}
