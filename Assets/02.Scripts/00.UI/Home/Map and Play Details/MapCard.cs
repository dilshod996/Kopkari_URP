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
    public int amountWatch = 500;
    //[Header("Moving Scene Details")]
    //[SerializeField] private SceneLoadManager.SceneType selectedScene;
    //[SerializeField] private Button selectBtn;
    private List<string> preloadRacing = new List<string>();


    private void OnEnable()
    {
        DataManager.OnMapUnlocked += HandleMapUnlocked;
        LockeMap();
        UIUpdates();
        buySection.onClick.AddListener(MapDetails);
        playRoomBtn.onClick.AddListener(EnterPlayRoom);
    }
    private void OnDisable()
    {
        DataManager.OnMapUnlocked -= HandleMapUnlocked;
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
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());

        if (current.Power < Constants.HorseConditionNum.Power || current.Cooling < Constants.HorseConditionNum.Cool || current.Stamina < Constants.HorseConditionNum.Stamina)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            HomeMainUI.Instance?.SHowFoodPanel();
            HomeMainUI.Instance.CloseRacingField();
            return;  // Racing boshlanmaydi
        }

        if (playCost > 0)
        {
            bool success = CurrencyManager.Instance.SpendNyufiy(playCost);
            if (!success)
            {
                Debug.Log("Money is not enough to play popup");
                UIOverlayRoot.I.Confirm(487, 488, 489, 490, MoveToShop, WatchAdds);
                return;
            }
        }
        if (mapType == MapType.Racing)
        {
            preloadRacing.Add(Constants.RoomSound.RacingSound);
        }
        int defenseCheck = DataManager.Instance.GetItemAmount(Constants.PlayerItems.Defense);
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
                UIOverlayRoot.I.ShowPanel(UIPanelType.Kansas, LanguageManager.Instance.GetText(518), instant: false);
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
        GameAnalyticsEvents.RewardedAdClicked(
            placement: "coin_shop",
            rewardType: "nyufiy",
            rewardAmount: amountWatch
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(amountWatch, true);

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: amountWatch
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: amountWatch
            );

        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
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
        bool mapOpen = IsMapOpen();

        if (mapOpen)
        {
            if (buySection != null) buySection.gameObject.SetActive(false);
            if(lockObj != null) lockObj.SetActive(false);
            isUnlocked = true;
        }
        else
        {
            if (buySection != null) buySection.gameObject.SetActive(true);
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

    private bool IsMapOpen()
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.IsMapUnlocked(mapLangName);

        int defaultValue = mapLangName == Constants.MapNames.RacingTraining || mapLangName == Constants.MapNames.Zarafshan ? 1 : 0;
        return PlayerPrefs.GetInt(mapLangName, defaultValue) == 1;
    }

    private void HandleMapUnlocked(string mapKey)
    {
        if (mapKey == mapLangName)
            LockeMap();
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
        LockeMap();
        //UpdateChooseButtonText();
    }

   
}

