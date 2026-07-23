using MalbersAnimations.Controller;
using MalbersAnimations.Events;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class AIGameRoom : MonoBehaviour
{
    public static AIGameRoom Instance;

    //Game start actions
    [SerializeField] private GameObject mobileCanvas;
    [SerializeField] private GameObject roomCanvas;
    [SerializeField] private GameObject arrowObj;

    //Win Loose Panels

    [SerializeField] private UIUserLoose LoosePanel;
    [SerializeField] private UIWin WinPanel;
    [SerializeField] private DollyCameraController dollyCamera;

    // quydi ushlagandan keyin notification

    [SerializeField] private NotificationManager TimeNotification;
   // [SerializeField] private MPickUp pickUP;
    public float lampCatchTime = 30f;
    private string LampTimeText = string.Empty;
    private bool isCatched=false;
    private bool isCatchTimeOver= false;
    private bool lessTimeLeft=false;



    //Time 
    [SerializeField] private TMP_Text timeText;
    public float mainTime = 150f;
    public bool hasHandledGameOver = false;
    public bool hasHandleWin = false;


    //PlayerData
    [SerializeField] private PlayerDataManager playerDataManager;
    //HorseData
    [SerializeField] private HorseDataManager horseDataManager;

    //Round Counter
    private int lampCatchCount = 0;
    private float spentTime=0f;

    //Continue game

    [SerializeField] private GameObject myLamb;
    [SerializeField] private List<Transform> lambPositions;

    [SerializeField] private GameObject myTargetPos;

    //particles changes their pos looking lamp and target pos

    [SerializeField] private GameObject lampVFX;
    [SerializeField] private GameObject targetVFX;

    public string LambOwner=string.Empty;
    [SerializeField] private TMP_Text riderTextPopup;
    [SerializeField] private GameObject riderInfoPopup;
    private Coroutine boboyNpcCoroutine;

    //Tovoqlarni hisoblash yani tovoqlar soni
    [SerializeField] private int PrizeCount=3;
    [SerializeField] private UIWinSession smallWinPage;
    [SerializeField] private List<Sprite> winningOBjList;

    // Game logout popup
    [SerializeField] private ModalWindowManager Popup;

   
    public enum PrizeTypes
    {
        Carpet,
        LampWithMoney,
        SuperPrize,
        Finish
    }
    public enum RoomState
    {
        None,
        GameStarted,
        WaterDropped,
        TimeFinished,
        HorseStamenaFinished,
        RiderStamenaFinished,
        GameFinished,
        Won
    }
    public enum GroundState
    {
        None,
        SpeedLow,
        Sand,
        Ice
    }
    public RoomState roomState = RoomState.None;
    public PrizeTypes prizes = PrizeTypes.Carpet;
    public GroundState groundState = GroundState.None;

    // Uloq state
    private bool isWaterDropped = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }
    private void Start()
    {
        roomState = RoomState.None;
        //prizes = PrizeTypes.Money;
    }
    private void Update()
    {
        InitializeTime();
        
    }
    public void GameStartedAction(bool state, bool isFinish=false)
    {
        timeText.color = new Color32(41, 9, 219, 255);
        if (prizes == PrizeTypes.Finish)
        {
            isFinish = true;
        }
        if(state)
        {
            int indexOfPos= (int)prizes;
            switch (prizes)
            {
                case PrizeTypes.Carpet:
                    mainTime = 150;
                    break;
                case PrizeTypes.LampWithMoney:
                    mainTime = 120;
                    ContinueGameChanger(lambPositions[indexOfPos - 1], lambPositions[indexOfPos]);
                    break;
                case PrizeTypes.SuperPrize:
                    mainTime = 100;
                    ContinueGameChanger(lambPositions[indexOfPos], lambPositions[indexOfPos + 1]);                  
                    break;
                case PrizeTypes.Finish:
                   // WinPanel.WinPage();
                    break;
                default: break;
            }

        }
        GameObjectsEnable(state, isFinish);

    }
    public void GameObjectsEnable(bool enable, bool isFinish)
    {
        if (!isFinish)
        {
            if (enable)
            {
                roomState = RoomState.GameStarted;
                hasHandledGameOver = false;
                dollyCamera.StartGame();
            }

            mobileCanvas.SetActive(enable);
            roomCanvas.SetActive(enable);
            myLamb.SetActive(enable);
            arrowObj.SetActive(enable);
        }

    }
    private void InitializeTime()
    {
        switch(roomState)
        {
            case RoomState.GameStarted:
                if (mainTime > 0)
                {
                    mainTime -= Time.deltaTime;
                    int minutes = Mathf.FloorToInt(mainTime / 60); // Daqiqa
                    int seconds = Mathf.FloorToInt(mainTime % 60); // Qolgan soniyalar

                    timeText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

                    if (mainTime <= 3)
                    {
                        timeText.color = Color.red;
                    }
                }
                else
                {
                    // main time finished
                    timeText.text = "00:00";
                    roomState = RoomState.GameFinished;
                    //open lose panel
                }
                break;
                //loosing state
            case RoomState.GameFinished:
            case RoomState.WaterDropped:
                if (!hasHandledGameOver)
                {                   
                    hasHandledGameOver = true;
                    WinOrLosePage();
                    Debug.Log("Lost game");
                }

                break;
            default:break;
        }

        if (isCatched)
        {
            if (lampCatchTime > 0)
            {
                lampCatchTime -= Time.deltaTime;


                LampTimeText = "00:" + Mathf.Ceil(lampCatchTime).ToString("00");

                if (lampCatchTime <= 3)
                {
                    if (!lessTimeLeft)
                    {
                        TimeNotification.CustomeUpdate("Uloq tushib ketadigan vaqt...", LampTimeText, true);
                        lessTimeLeft = true;
                    }
                    //TimeNotification.UpdateUI()
                    //timerText.color = Color.red;
                }
            }
            else if (lampCatchTime<=0 && !isCatchTimeOver)
            {
                Debug.Log("Dropppp");
                TriggerEvent();
                //Changeiinnngggngggg
                //if(horseDataManager.horseAnimal.activeState.StateName.ToString().Equals("Swim"))
                //{
                //   // LoosePanel.UserLost(roomState);
                //    roomState=RoomState.WaterDropped;
                //    Debug.Log("you dropped to water");
                //    //loose menu
                //}
            }
        }
                
    }

    #region Galaba va Mag'lubiyat
    public void WinOrLosePage()
    {      
        roomState = RoomState.None;       
        if (!string.IsNullOrEmpty(LambOwner))
        {
            if (LambOwner.Equals("dima") && !hasHandledGameOver)//should change
            {
                if (isCatched)
                {
                    isCatched = false;
                }
                switch (prizes)
                {
                    case PrizeTypes.Carpet:
                        ShowWinPage(winningOBjList[(int)prizes], "$5");
                        dollyCamera.BackToDolly(6f);
                        prizes = PrizeTypes.LampWithMoney;
                        break;
                    case PrizeTypes.LampWithMoney:
                        ShowWinPage(winningOBjList[(int)PrizeTypes.LampWithMoney], "$10");
                        dollyCamera.BackToDolly(0);
                        prizes = PrizeTypes.SuperPrize;
                        break;
                    case PrizeTypes.SuperPrize:
                        ShowWinPage(winningOBjList[(int)PrizeTypes.SuperPrize], "$15");
                        dollyCamera.BackToDolly(2);
                        prizes = PrizeTypes.Finish;
                        break;
                    case PrizeTypes.Finish:
                        //WinPanel.WinPage();
                        // Stopped here continue tomorrow
                        break;
                    default:break;
                }
            }
            else
            {
                //Lose page
                if (isCatched)
                {
                    TriggerEvent();
                }
                switch (prizes)
                {
                    case PrizeTypes.Carpet:                       
                        ShowLoosePage(winningOBjList[(int)PrizeTypes.LampWithMoney], "$10", "Hech qisi yo'q polvon, o'tdi ketdi... kuchli bo'lgin");
                        dollyCamera.BackToDolly(6f);
                        prizes = PrizeTypes.LampWithMoney;
                        break;
                    case PrizeTypes.LampWithMoney:
                        dollyCamera.BackToDolly(0);
                        ShowLoosePage(winningOBjList[(int)PrizeTypes.SuperPrize], "$15", "Bizda eng yaxshi imkoniyat superrr tovoooqqqq...");
                        prizes = PrizeTypes.SuperPrize;
                        break;
                    case PrizeTypes.SuperPrize:
                       // WinPanel.WinPage();
                        Debug.Log("Last loosing");
                        break;
                    default: break;
                }               
                Debug.Log("You lost nowww");
            }
        }
        else
        {
            Debug.Log("Lamb Owner doesnt exist");
        }
        GameStartedAction(false);

    }
    private async void ShowWinPage(Sprite sprite, string moneyAmount)
    {
        smallWinPage.gameObject.SetActive(true);
        await Task.Yield(); 
        //smallWinPage.WinSessionEnable(sprite, moneyAmount);
    }
    private async void ShowLoosePage(Sprite sprite, string moneyAmount, string grandySentence)
    {
        LoosePanel.gameObject.SetActive(true);
        await Task.Yield();
       // LoosePanel.UserLost(sprite, moneyAmount, grandySentence);
    }
    #endregion

    #region Uloq olindi va tushirildi

    public void StartPickUpTime()
    {
        isCatched = true;
        lessTimeLeft = false;
        lampCatchTime = 30f;
        //roomState = RoomState.LampCatched;
        TimeNotification.CustomeUpdate("Olg'a", "00:30");
        Debug.Log("Catch Trigger");
        //
    }
    public bool IsCatched() { return isCatched; }
    private void TriggerEvent()
    {
        isCatched = false;
        //there is need look
        playerDataManager.DropObject();
        //pickUP.DropItem();
    }


    //win has game over time or you just dropped to water then also AI should drop lamb
    public void DropLamp(MPickUp pickedObj)
    {
        if(pickedObj != null)
        {
            pickedObj.DropItem();
        }
        else
        {
            Debug.Log("PickedObje is null");
        }
    }
    #endregion

    #region WinningPanelShow

    private void ContinueGameChanger(Transform lambPos, Transform targetPos)
    {
        myLamb.transform.SetPositionAndRotation(lambPos.position, lambPos.rotation);
        lampVFX.transform.position = lambPos.position;
        myTargetPos.transform.SetPositionAndRotation(targetPos.position, targetPos.rotation);
        targetVFX.transform.position = targetPos.position;
    }
    #endregion

    #region Round Result
    public int GetLambCount()
    {
        return lampCatchCount;
    }
    public float GetSpentTime()
    {
        return spentTime;
    }

    #endregion

    public void RiderInfoPopup(string name)
    {
        if(boboyNpcCoroutine != null)
        {
            StopCoroutine(boboyNpcCoroutine);
        }
        boboyNpcCoroutine = StartCoroutine(GetTheRiderName(name));
    }
    IEnumerator GetTheRiderName(string name)
    {
        if (!string.IsNullOrEmpty(riderTextPopup.text)){
            riderTextPopup.text = string.Empty;
        }
        riderTextPopup.text = name;
        LambOwner = name;
        yield return new WaitForSeconds(0.5f);
        riderInfoPopup.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(3f);
        riderInfoPopup.transform.localScale = Vector3.zero;

    }

    public void DisplaySpeedLow()
    {
        RiderInfoPopup("Otaginam otingiz tezligi past hududga kirdi...");
    }
    /// <summary>
    /// for log out section connected throuh scene inspector
    /// </summary>
    public void PopupAppear()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
        //Popup.UpdateUICustom(null,"O'yindan chiqayapsizmi?", "Hali o'yin tugagani yo'qku davom etdirmaysizmi?");
    }
}
