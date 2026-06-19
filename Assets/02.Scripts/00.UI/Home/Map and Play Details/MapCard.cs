using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;
using System.Collections.Generic;


public class MapCard : MonoBehaviour
{
    public enum MapType
    {
        Kopkari,
        Racing,
        Archery
    }
    public MapType mapType;
    public SceneLoadManager.SceneType movingRoom;
    [SerializeField] private Sprite backgroundSprite;
    public Sprite BackgroundSprite => backgroundSprite;
    [Header("UI Elements")]
 
    public Image shadowImage;
    public TMP_Text mapNameText;
    public TMP_Text mapCostText;
    public TMP_Text playCostText;
    [SerializeField] private TMP_Text entryText;
    [SerializeField] private TMP_Text buyText;
    public Button cardScrollBtn;
    [SerializeField] private Button playRoomBtn;
    // public Button blockBtn;
    public GameObject lockObj;
    public Button buySection;
    public Image mapImage;

    [Header("Unlock Settings")]
    public bool isUnlocked = true;

    private Vector3 selectedScale = Vector3.one * 1.04f;
    private Vector3 normalScale = Vector3.one * 0.94f;

    private float currentAlpha = 0.85f;
    private float targetAlpha = 0.85f;
    private Vector3 targetScale;

    [Header("Popup")]
    [SerializeField] private MapShowPopup popupMapInfo;


    [Header("Language")]
    [SerializeField] private int mapLangCode = -1;
    [SerializeField] private string mapLangName = "MapName";

    [SerializeField] private int mapInfoCode = -1;
    [SerializeField] private int costMap;
    [SerializeField] private int playCost;
    private MapCardScaler manager;

    //[Header("Moving Scene Details")]
    //[SerializeField] private SceneLoadManager.SceneType selectedScene;
    //[SerializeField] private Button selectBtn;
    private List<string> preloadRacing = new List<string>();


    private void OnEnable()
    {
        LockeMap();
        UIUpdates();
        buySection.onClick.AddListener(MapDetails);
        playRoomBtn.onClick.AddListener(EnterPlayRoom);
    }
    private void OnDisable()
    {
        buySection?.onClick.RemoveAllListeners();
        playRoomBtn?.onClick.RemoveAllListeners();
    }
    private void UIUpdates()
    {
        mapNameText.text = LanguageManager.Instance.GetText(mapLangCode);
        if(mapCostText != null)
        {
            mapCostText.text = costMap>0 ? $"{costMap:N0}" : "0"; //nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        }
        if (playCostText != null)
        {
            playCostText.text = playCost > 0 ? $"-{playCost:N0}" : "0";
        }
        buyText.text = LanguageManager.Instance.GetText(424);
        entryText.text = LanguageManager.Instance.GetText(485);
    }
    private void EnterPlayRoom()
    {
        //if(movingRoom!= SceneLoadManager.SceneType.TrainingRacing)
        //{

        //}
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);

        if (currentPower < Constants.HorseConditionNum.Power || currentCooling < Constants.HorseConditionNum.Cool || currentStamina < Constants.HorseConditionNum.Stamina)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            HomeMainUI.Instance?.SHowFoodPanel();
            return;  // Racing boshlanmaydi
        }
        if (playCost > 0)
        {
            int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
            if(nyufiyAmount < playCost)
            {
                Debug.Log("Money is not enough to play popup");
                UIOverlayRoot.I.Confirm(487, 488, 489, 490, MoveToShop, WatchAdds);
                return;
            }
            else
            {
                nyufiyAmount -= playCost;  
                PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
            }
        }
        if (mapType == MapType.Racing)
        {
            preloadRacing.Add(Constants.RoomSound.RacingSound);
        }
        int defenseCheck = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
        if(defenseCheck <1)
        {
            UIOverlayRoot.I.Confirm(493, 494, 496, 253, OpenTacticItemsPanel, MovingRacingRoom);
        }
        else
        {
            MovingRacingRoom();
        }
    }
    private void MovingRacingRoom()
    {
        switch (movingRoom)
        {
            case SceneLoadManager.SceneType.TrainingRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.RacingTutorial, LanguageManager.Instance.GetText(486), instant: false);
                break;
            case SceneLoadManager.SceneType.SecondRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, LanguageManager.Instance.GetText(209), instant: false);
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, LanguageManager.Instance.GetText(210), instant: false);
                break;
            //case SceneLoadManager.SceneType.SibiriaRacing:
            //    UIOverlayRoot.I.ShowPanel(UIPanelType.Sibiria, LanguageManager.Instance.GetText(211), instant: false);
            //    break;
            case SceneLoadManager.SceneType.Kansas:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Kansas, LanguageManager.Instance.GetText(519), instant: false);
                break;
        }
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        SceneLoadManager.Instance.LoadSceneNew(movingRoom, preloadRacing);
    }
    private void OpenTacticItemsPanel()
    {
        HomeMainUI.Instance.ShowSuppliesPanel();
    }
    private void MoveToShop()
    {
        HomeMainUI.Instance.NyufiyClicked();
        HomeMainUI.Instance.CloseRacingField();
        //this.gameObject.SetActive(false);
    }
    private void WatchAdds()
    {
        int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        nyufiyAmount += playCost;
        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
    }
    private void MapDetails()
    {
        //string cost  = costMap.ToString() /*+ LanguageManager.Instance.GetText(58)*/;
        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
        HomeMainUI.Instance.ShowUI(popupMapInfo);
        popupMapInfo.SetMapData(LanguageManager.Instance.GetText(mapLangCode), mapImage.sprite, costMap, LanguageManager.Instance.GetText(mapInfoCode), mapLangName, mapType);
    }
    public void Initialize(MapCardScaler cardManager)
    {
        manager = cardManager;

        if (cardScrollBtn != null)
            cardScrollBtn.onClick.AddListener(() => manager.ScrollToCard(this));

        //if (chooseButton != null)
        //    chooseButton.onClick.AddListener(OnChooseClicked);
    }

    public void SetAsMain(bool isMain)
    {
        targetScale = isMain ? selectedScale : normalScale;

        if (isUnlocked)
        {
            targetAlpha = isMain ? 0f : 0.85f;

            if (shadowImage != null)
                shadowImage.gameObject.SetActive(!isMain);
           // Debug.Log("Card is main?" + gameObject.name);
        }
        else
        {
            targetAlpha = 0.85f;;
            //Debug.Log("Card is locked, setting alpha to 0.85");
        }
    }
    private void LockeMap()
    {
        int mapOpen = PlayerPrefs.GetInt(mapLangName, 0);
        if (mapOpen != 0)
        {
            buySection.gameObject.SetActive(false);
           if(lockObj != null) lockObj.SetActive(false);
            isUnlocked = true;
        }
        else
        {
            buySection.gameObject.SetActive(true);
            if (lockObj != null) lockObj.SetActive(true);
            isUnlocked= false;
        }
        //if (!isUnlocked)
        //{
        //    blockBtn.gameObject.SetActive(true);
        //}
        //else
        //{
        //    blockBtn.gameObject.SetActive(false);
        //}
    }

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 2.5f);
    }

    public void SetMapInfo(string name, string info, string unlockInfo, bool unlocked)
    {
        if (mapNameText != null) mapNameText.text = name;
        //if (mapInfoText != null) mapInfoText.text = info;

        isUnlocked = unlocked;

       // UpdateChooseButtonText();
    }

    private void OnChooseClicked()
    {
        if (isUnlocked)
        {
            Debug.Log($"Map chosen: {mapNameText.text}");
            // Bu yerda tanlangan kartani ishlatish mumkin
        }
        else
        {
            // Agar locked bo‘lsa, popup orqali ko‘rsat
            //InfoPopupManager.Instance.ShowPopup(mapNameText.text, unlockCondition, false, this);
        }
    }

    private void OnInfoClicked()
    {
        //InfoPopupManager.Instance.ShowPopup(mapNameText.text, unlockCondition, isUnlocked, this);
    }

    public void UnlockCard()
    {
        isUnlocked = true;
        //UpdateChooseButtonText();
    }

   
}

