using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{

    public enum GameType
    {
        Racing,
        Kopkari
    }
    public GameType gameType = GameType.Racing;
    [Header("UI Refs")]
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button playAgain;
    [SerializeField] private Button backLobby;
    [SerializeField] private Button support;
    [SerializeField] private TMP_Text replayText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text supportText;

    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameFood foodPage;

    public SceneLoadManager.SceneType sceneType;

    private float overAllTime = 0f;
    private float overAllBoostTime = 0f;
    private float overAllPenaltyTime = 0f;
    private float overAllWalkZoneTime =0f;

    // Horse Last stats

    private float lastPower = 0f;
    private float lastCooling = 0f;
    private float lastStamina = 0f;
    private void OnEnable()
    {
        GetGameFinishedTime();
        if (gameType == GameType.Racing)
        {
            var rc = RacingController.Instance;
            if (rc == null) return;
            GetGameOverType(rc.gameOverType);
        }
        else
        {
            var km = KopkariManager.Instance;
            if (km == null) return;
            GameOverByType(km.gameOverTypes);
            //TextDetails();
        }
        GetOverallPenaltyTimeAndBoost();
        HorseStats();
        if (backLobby != null)
            backLobby.onClick.AddListener(BackHome);

        if (playAgain != null)
            playAgain.onClick.AddListener(PlayAgainAction);

        if (support != null)
            support.onClick.AddListener(OpenSuppliesPage);
        UITransilations();

        Booster.OnWalkZoneDamagedTime += GetWalkZoneOverAllTime;
    }

    private void OnDisable()
    {
        if (playAgain != null)
            playAgain.onClick.RemoveListener(PlayAgainAction);

        if (backLobby != null)
            backLobby.onClick.RemoveListener(BackHome);

        if (support != null)
            support.onClick.RemoveListener(OpenSuppliesPage);
        Booster.OnWalkZoneDamagedTime -= GetWalkZoneOverAllTime;
    }

    #region Racing Over
    public void GetGameOverType(GameOverTypes type)
    {
        if(LanguageManager.Instance != null)
        {
            if (type == GameOverTypes.ObstacleHit)
            {
                gameOverTitle.text = LanguageManager.Instance.GetText(215);
                infoText.text = LanguageManager.Instance.GetText(217);
            }
            else if (type == GameOverTypes.Offside)
            {
                gameOverTitle.text = LanguageManager.Instance.GetText(219);
                infoText.text = LanguageManager.Instance.GetText(218);
            }
            else if (type == GameOverTypes.ByTime)
            {
                gameOverTitle.text = LanguageManager.Instance.GetText(196);
                infoText.text = LanguageManager.Instance.GetText(216);
            }
            else
            {
                // None yoki default
                gameOverTitle.text = LanguageManager.Instance.GetText(196);
                infoText.text = "";
            }
        }

    }
    #endregion

    private void UITransilations()
    {
        if(LanguageManager.Instance != null)
        {
            backText.text = LanguageManager.Instance.GetText(302);
            replayText.text = LanguageManager.Instance.GetText(197);
            supportText.text = LanguageManager.Instance.GetText(198);
        }
    }

    #region Tactic Items and Supplies
    private void OpenTacticItemsPanel()
    {
        UIButtonActions.Instance?.OpenItemsPanel();
    }
    private void OpenSuppliesPage()
    {
        this.gameObject.SetActive(false);
        UIButtonActions.Instance?.ShowUI(foodPage);
    }
    #endregion

    #region Room Info
    private int CheckRoomCost()
    {
        switch (sceneType)
        {
            case SceneLoadManager.SceneType.SecondRacing:
                return Constants.RoomEnterCosts.ZarafshanCost;

            case SceneLoadManager.SceneType.EgyptRacing:
                return Constants.RoomEnterCosts.EgyptCost;
            case SceneLoadManager.SceneType.Kansas:
                return Constants.RoomEnterCosts.Kansas;

            default:
                return 0;
        }
    }
    private void OnMoneyNotEnough()
    {
        GameAnalyticsEvents.RewardedAdClicked(
           placement: "coin_shop",
           rewardType: "nyufiy",
           rewardAmount: CheckRoomCost()
       );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(CheckRoomCost(), true);


            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: CheckRoomCost()
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: CheckRoomCost()
            );

        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }
    private void BackHome()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
    private void ResourceNotEnoughPopup()
    {
        UIButtonActions.Instance?.ShowUI(foodPage);
        this.gameObject.SetActive(false);
    }
    public void PlayAgain()
    {
        UIOverlayRoot.I.ShowMovementPanelForScene(sceneType);
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    public void PlayAgainAction()
    {
        if (lastPower < Constants.HorseConditionNum.Power || lastCooling < Constants.HorseConditionNum.Cool || lastStamina < Constants.HorseConditionNum.Stamina)
        {
            UIOverlayRoot.I.Done(431, 432, 433, ResourceNotEnoughPopup, null);
            return;  // Racing davom etmaydi
        }
        bool success = CurrencyManager.Instance != null && CurrencyManager.Instance.SpendNyufiy(CheckRoomCost(), true);
        if (!success)
        {
            // pul yetmayapti
            UIOverlayRoot.I.Done(487, 488, 498, OnMoneyNotEnough);
            return;
        }
        int defenseCheck = DataManager.Instance != null ? DataManager.Instance.GetItemAmount(Constants.PlayerItems.Defense) : 0;
        if (defenseCheck < 1)
        {
            UIOverlayRoot.I.Confirm(493, 494, 496, 253, OpenTacticItemsPanel, PlayAgain);
        }
        else
        {
            PlayAgain();
        }

    }
    #endregion

    #region Horse Statistics
    private void GetOverallPenaltyTimeAndBoost()
    {
        if(UIButtonActions.Instance != null)
        {
            overAllPenaltyTime = UIButtonActions.Instance.GetTotalWebSnareTime();
            overAllBoostTime = UIButtonActions.Instance.GetTotalHoldTime();
        }
        if(KopkariMainUI.Instance != null)
        {
            overAllPenaltyTime = KopkariMainUI.Instance.GetTotalWebSnareTime();
            overAllBoostTime = KopkariMainUI.Instance.GetTotalHoldTime();
        }
    }
    private void GetGameFinishedTime()
    {
        if (RacingController.Instance != null)
            overAllTime = RacingController.Instance.ElapsedTime;
        if (KopkariManager.Instance != null)
        {
            overAllTime = KopkariManager.Instance.GetUsedMainTime();
        }
    }
    private void GetWalkZoneOverAllTime(float time)
    {
        overAllWalkZoneTime = time;
        Debug.Log($"[WalkZone time] {overAllWalkZoneTime}");
    }

    private void HorseStats()
    {
        // --- Load ---
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float horsePowerMain = current.Power;
        float horseCoolingMain = current.Cooling;
        float horseStaminaMain = current.Stamina;
        Debug.Log($"[Game Over] Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime} over all walkzone time{overAllWalkZoneTime}");
        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - (overAllPenaltyTime+overAllWalkZoneTime);     // Non-penalty time.

        float newPower = horsePowerMain - (overAllBoostTime * 0.4f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.05f);

        newPower = Mathf.Max(0, newPower);
        newStamina = Mathf.Max(0, newStamina);//Hard coded now
        newCooling = Mathf.Max(0, newCooling);


        float rPower = Mathf.Round(newPower);          // butun son (masalan: 83)
        float rStamina = Mathf.Round(newStamina);
        float rCooling = Mathf.Round(newCooling);
        lastPower = rPower;
        lastCooling = rCooling;
        lastStamina = rStamina;
        // Progress Bar Updatelar
     
        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(rPower, rCooling, rStamina));

        Debug.Log($"Horse Stats Updated -> Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }
    #endregion

    #region Kopkari Details
    private void GameOverByType(GameOverTypes type)
    {
        string gameOverTitleS = string.Empty;
        string gameOverDescription = string.Empty;
        var lang = LanguageManager.Instance;
        if(lang == null )
        {
            Debug.Log("[Game Over] Language Manager is not working");
        }

        switch (type)
        {
            case GameOverTypes.None:
                break;
            case GameOverTypes.ByTime:
                gameOverTitleS = lang.GetText(501);
                gameOverDescription = lang.GetText(502);
                break;
            case GameOverTypes.Offside:
                gameOverTitleS = lang.GetText(219);
                gameOverDescription = lang.GetText(503);
                break;
            case GameOverTypes.KopkariStartFailed:
                gameOverTitleS = lang.GetText(504);
                gameOverDescription = lang.GetText(505);
                break;
            default:
                Debug.Log("[Game Over ] default is running");
                break;
        }
        gameOverTitle.text = gameOverTitleS;
        infoText.text = gameOverDescription;
    }
    private void TextDetails()
    {
        if(LanguageManager.Instance != null)
        {
            gameOverTitle.text = LanguageManager.Instance.GetText(501);
            infoText.text = LanguageManager.Instance.GetText(502);
        }
    }
    #endregion
}
