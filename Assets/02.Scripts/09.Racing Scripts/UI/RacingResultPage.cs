using Michsky.UI.ModernUIPack;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RacingResultPage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;   // VerticalLayoutGroup
    [SerializeField] private UIRacingPlayerFinal itemPrefab;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button backToHome;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text mainMenuText, raceAgainText;
    [SerializeField] private TMP_Text allNyufiyText, allCoinText;
    [Header("Details")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text horseName;
    [SerializeField] private TMP_Text coolingText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text staminaText;

    // Horse sliders
    [SerializeField] private ProgressBar powerProgress;
    [SerializeField] private ProgressBar staminaProgress;
    [SerializeField] private ProgressBar coolingProgress;

    [Header("Racing Stats")]
    [SerializeField] private TMP_Text racingStatsTitleText;
    [SerializeField] private TMP_Text nyufiyAmountText;
    [SerializeField] private TMP_Text coinAmountText;
    [SerializeField] private TMP_Text xpAmountText;


    [Header("RecordSection")]
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text currentRecordTitle;
    [SerializeField] private TMP_Text currentRecordTime;
    [SerializeField] private TMP_Text adsMoneyText;
    [SerializeField] private Button adWatchBtn;

    [Header("Options")]
    [SerializeField] private bool sortByRankingAsc = true;  // #1, #2, #3...
    [SerializeField] private bool clearOnBuild = true;
    private SceneLoadManager.SceneType sceneType = SceneLoadManager.SceneType.None;

    private readonly List<UIRacingPlayerFinal> _spawned = new();


    private float overAllTime = 0f;
    private float overAllBoostTime=0f;
    private float overAllPenaltyTime=0f;
    private float overAllWalkZoneTime = 0f;

    private float lastPower = 0f;
    private float lastCooling = 0f;
    private float lastStamina = 0f;

    public static Action<int> OnGetRiderRank;

    private bool horseStatsApplied;
    private bool rewardGiven;
    private int cachedTaqaPrize;
    private int cachedNyufiyPrize;
    private int cachedLevelUpPoint;
    private float cachedRaceTime;
    private float cachedRecordTime;
    private string cachedRecordText;
    private bool cachedPlayerFinished;

    private int GetAdsAmountByScene(SceneLoadManager.SceneType sceneType)
    {
        return sceneType switch
        {
            SceneLoadManager.SceneType.SecondRacing => 400, // 18-22
            SceneLoadManager.SceneType.EgyptRacing => 500, // 12-16
            SceneLoadManager.SceneType.Kansas => 600,  // 8-12
            _ => 350    // 4-6
        };
    }
    private void OnEnable()
    {
        ResolveSceneType();
        ResetRaceTimeBuckets();
        Booster.OnWalkZoneDamagedTime += GetWalkZoneOverAllTime;

        if(replayButton != null)
        {
            replayButton.onClick.AddListener(Replay);
        }
        if ((backToHome!=null))
        {
            backToHome.onClick.AddListener(BackLobby);
        }
        if (LanguageManager.Instance != null) UITransilations();
        ShowResults();

        if (adWatchBtn != null)
            adWatchBtn.onClick.AddListener(PlusMoneyReward);
    }
    private void OnDisable()
    {
        if (replayButton != null)
            replayButton.onClick.RemoveListener(Replay);

        if (backToHome != null)
            backToHome.onClick.RemoveListener(BackLobby);


        if (adWatchBtn != null)
            adWatchBtn.onClick.RemoveListener(PlusMoneyReward);
        Clear();
        Booster.OnWalkZoneDamagedTime -= GetWalkZoneOverAllTime;
    }

    private void ResolveSceneType()
    {
        SceneLoadManager manager = SceneLoadManager.Instance;
        if (manager == null)
            return;

        SceneLoadManager.SceneType currentSceneType = manager.CurrentSceneType;
        if (IsSupportedRacingScene(currentSceneType))
            sceneType = currentSceneType;
    }

    private bool IsSupportedRacingScene(SceneLoadManager.SceneType type)
    {
        return type == SceneLoadManager.SceneType.TrainingRacing
            || type == SceneLoadManager.SceneType.SecondRacing
            || type == SceneLoadManager.SceneType.EgyptRacing
            || type == SceneLoadManager.SceneType.Kansas;
    }

    private void ResetRaceTimeBuckets()
    {
        overAllTime = 0f;
        overAllBoostTime = 0f;
        overAllPenaltyTime = 0f;
        overAllWalkZoneTime = Booster.TotalWalkZoneDamagedTime;
    }
    #region Player List && Racing Stats && Records
    public void ShowResults()
    {
        var lb = RacingLeaderboard.Instance;
        var standings = lb?.GetStandings();

        if (standings == null || standings.Count == 0)
        {
            Debug.Log("[ShowResultPanel] standings is null or empty.");
            return;
        }
        BuildList(standings);
        GetBoostTime();
        GetOverallPenaltyTime();
        HorseStats();
    }
    public void BuildList(List<RacingAgent> entries)
    {
        if (clearOnBuild)
            Clear();

        entries = entries != null
            ? entries.Where(e => e != null).ToList()
            : new List<RacingAgent>();

        entries = sortByRankingAsc
            ? entries.OrderBy(e => e.Ranking > 0 ? e.Ranking : int.MaxValue).ToList()
            : entries.OrderBy(e => e.Ranking > 0 ? 0 : 1)
                .ThenByDescending(e => e.Ranking)
                .ToList();

        float startX = 50f;
        float startY = -20f;

        float stepX = -18f;   
        float stepY = -100f;  // har safar y dan 100 pastga: -20, -120, -220, ...
        bool playerHandled = false;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            var item = Instantiate(itemPrefab, contentParent);
            item.Bind(e);
            _spawned.Add(item);

            var rt = item.GetComponent<RectTransform>();

            // Normalize item transform before applying manual list position.
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            rt.localScale = Vector3.one;

            rt.anchoredPosition = Vector2.zero;

            float x = startX + i * stepX;
            float y = startY + i * stepY;

            rt.anchoredPosition = new Vector2(x, y);

            if (e.isPlayer)
            {
                if (!playerHandled)
                {
                    playerHandled = true;
                    BuildPlayerResult(e);
                }

                continue;
            }

        }

    }

    private void BuildPlayerResult(RacingAgent e)
    {
        if (e == null)
            return;

        if (!rewardGiven)
            CacheAndApplyPlayerResult(e);

        ApplyCachedPlayerResultUI();
    }

    private void CacheAndApplyPlayerResult(RacingAgent e)
    {
        rewardGiven = true;
        cachedPlayerFinished = e.HasFinished;
        cachedRaceTime = e.HasFinished ? e.LastSplitTime : 0f;
        cachedRecordText = string.Empty;
        cachedRecordTime = 0f;

        if (!e.HasFinished)
        {
            cachedTaqaPrize = 0;
            cachedNyufiyPrize = 0;
            cachedLevelUpPoint = 0;
            overAllTime = 0f;
            return;
        }

        var prize = GetRacePrizeByMapAndRank(e.Ranking);

        cachedTaqaPrize = prize.taqaPrize;
        cachedNyufiyPrize = prize.nyufiyPrize;
        cachedLevelUpPoint = prize.levelUpPoint;

        DataManager.Instance?.AddLevelPoint(cachedLevelUpPoint, true);
        OnGetRiderRank?.Invoke(e.Ranking);
        CurrencyManager.Instance?.AddNyufiy(cachedNyufiyPrize, false);
        CurrencyManager.Instance?.AddCoin(cachedTaqaPrize, true);

        string recordKey = GetMapKey(sceneType);
        float savedTime = DataManager.Instance != null ? DataManager.Instance.GetBestRecord(recordKey) : 0f;
        if (savedTime == 0 || savedTime > e.LastSplitTime)
        {
            cachedRecordText = LanguageManager.Instance?.GetText(315);
            savedTime = e.LastSplitTime;

            DataManager.Instance?.SaveBestRecord(recordKey, savedTime);
        }
        else
        {
            cachedRecordText = LanguageManager.Instance?.GetText(316);
        }

        cachedRecordTime = savedTime;
        overAllTime = e.LastSplitTime;
        GameAnalyticsEvents.RaceFinished(sceneType.ToString(), "racing", e.Ranking, cachedNyufiyPrize);
        Debug.Log($"Ranking: {e.Ranking}");
        bool isWin = e.Ranking == 1;
        DataManager.Instance?.SaveRaceResult(GetMapKey(sceneType), isWin, (int)e.LastSplitTime);
    }

    private void ApplyCachedPlayerResultUI()
    {
        if (xpAmountText != null) xpAmountText.text = cachedLevelUpPoint.ToString();
        if (nyufiyAmountText != null) nyufiyAmountText.text = $"+{cachedNyufiyPrize:N0}";
        if (coinAmountText != null) coinAmountText.text = $"+{cachedTaqaPrize:N0}";
        if (timeText != null) timeText.text = cachedPlayerFinished ? $"{cachedRaceTime:0.00}s" : "-";
        if (recordText != null) recordText.text = cachedRecordText ?? string.Empty;
        if (currentRecordTime != null) currentRecordTime.text = cachedPlayerFinished ? $"{cachedRecordTime:0.00}s" : "-";

        if (DataManager.Instance != null && levelText != null && LanguageManager.Instance != null)
            levelText.text = $"{LanguageManager.Instance.GetText(319)} {DataManager.Instance.LevelAmount}/20";

        if (CurrencyManager.Instance != null)
        {
            if (allNyufiyText != null) allNyufiyText.text = $"{CurrencyManager.Instance.Nyufiy:N0}";
            if (allCoinText != null) allCoinText.text = $"{CurrencyManager.Instance.Coin:N0}";
        }
    }

    private string GetMapKey(SceneLoadManager.SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneLoadManager.SceneType.TrainingRacing:
                return Constants.MapNames.RacingTraining;

            case SceneLoadManager.SceneType.SecondRacing:
                return Constants.MapNames.Zarafshan;

            case SceneLoadManager.SceneType.EgyptRacing:
                return Constants.MapNames.Egypt;

            case SceneLoadManager.SceneType.Kansas:
                return Constants.MapNames.Kansas;

            case SceneLoadManager.SceneType.PastDargom:
                return Constants.MapNames.PastDargom;

            default:
                return Constants.MapNames.Zarafshan;
        }
    }
    private (int taqaPrize, int nyufiyPrize, int levelUpPoint) GetRacePrizeByMapAndRank(int ranking)
    {
        var rCont = RacingController.Instance;
        RacingController.RacingType mapType = rCont
            ? rCont.mapType
            : RacingController.RacingType.None;

        switch (mapType)
        {
            case RacingController.RacingType.Training:
                {
                    bool hasFinishedTutorial = PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay);

                    if (hasFinishedTutorial)
                        return (0, 0, 0);

                    // tutorial birinchi marta tugaganda fixed 100 XP
                    return (10, 4000, 100);
                }

            case RacingController.RacingType.Zarafshan:
                return ranking switch
                {
                    1 => (3, 2200, GetRandomXpByRank(1)),
                    2 => (1, 1700, GetRandomXpByRank(2)),
                    3 => (1, 1300, GetRandomXpByRank(3)),
                    _ => (0, 500, GetRandomXpByRank(4))
                };

            case RacingController.RacingType.Egypt:
                return ranking switch
                {
                    1 => (5, 2600, GetRandomXpByRank(1)),
                    2 => (3, 2000, GetRandomXpByRank(2)),
                    3 => (2, 1500, GetRandomXpByRank(3)),
                    _ => (0, 600, GetRandomXpByRank(4))
                };

            case RacingController.RacingType.Kansas:
                return ranking switch
                {
                    1 => (7, 3000, GetRandomXpByRank(1)),
                    2 => (4, 2300, GetRandomXpByRank(2)),
                    3 => (2, 1800, GetRandomXpByRank(3)),
                    _ => (0, 700, GetRandomXpByRank(4))
                };

            default:
                return ranking switch
                {
                    1 => (1, 1000, GetRandomXpByRank(1)),
                    2 => (0, 700, GetRandomXpByRank(2)),
                    3 => (0, 500, GetRandomXpByRank(3)),
                    _ => (0, 200, GetRandomXpByRank(4))
                };
        }
    }
    private int GetRandomXpByRank(int ranking)
    {
        return ranking switch
        {
            1 => UnityEngine.Random.Range(18, 23), // 18-22
            2 => UnityEngine.Random.Range(12, 17), // 12-16
            3 => UnityEngine.Random.Range(8, 13),  // 8-12
            _ => UnityEngine.Random.Range(4, 7)    // 4-6
        };
    }
    #endregion

    #region Horse Details
    private void HorseStats()
    {
        if (horseStatsApplied)
        {
            RefreshHorseStatsUI();
            return;
        }

        // --- Load ---
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float horsePowerMain = current.Power;
        float horseCoolingMain = current.Cooling;
        float horseStaminaMain = current.Stamina;
        Debug.Log($"Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime}  over all walkzone time{overAllWalkZoneTime}");
        // --- Calc ---
        float raceTime = Mathf.Max(0f, overAllTime);
        float boostTime = Mathf.Clamp(overAllBoostTime, 0f, raceTime);
        float penaltyTime = Mathf.Clamp(overAllPenaltyTime, 0f, raceTime);
        float walkZoneTime = Mathf.Clamp(overAllWalkZoneTime, 0f, Mathf.Max(0f, raceTime - penaltyTime));
        float basicTime = Mathf.Max(0f, raceTime - boostTime);
        float nonPenaltyTime = Mathf.Max(0f, raceTime - (penaltyTime + walkZoneTime));

        float newPower = horsePowerMain - (boostTime * 0.2f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (raceTime * 0.2f);
        float newCooling = horseCoolingMain - (penaltyTime * 0.5f + walkZoneTime * 0.5f + nonPenaltyTime * 0.05f);

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
        powerProgress.currentPercent = lastPower;
        powerProgress.UpdateUI();
        coolingProgress.currentPercent = lastCooling;
        coolingProgress.UpdateUI();
        staminaProgress.currentPercent = lastStamina;
        staminaProgress.UpdateUI();
        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(rPower, rCooling, rStamina));
        horseStatsApplied = true;

        Debug.Log($"Horse Stats Updated -> Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }

    private void RefreshHorseStatsUI()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());

        lastPower = Mathf.Round(current.Power);
        lastCooling = Mathf.Round(current.Cooling);
        lastStamina = Mathf.Round(current.Stamina);

        powerProgress.currentPercent = lastPower;
        powerProgress.UpdateUI();
        coolingProgress.currentPercent = lastCooling;
        coolingProgress.UpdateUI();
        staminaProgress.currentPercent = lastStamina;
        staminaProgress.UpdateUI();
    }

    private void GetOverallPenaltyTime()
    {
        overAllPenaltyTime = UIButtonActions.Instance?.GetTotalWebSnareTime() ?? 0f; 
    }
    private void GetWalkZoneOverAllTime(float time)
    {
        overAllWalkZoneTime += Mathf.Max(0f, time);
        Debug.Log($"[WalkZone time] {overAllWalkZoneTime}");
    }
    private void GetBoostTime()
    {
        overAllBoostTime = UIButtonActions.Instance?.GetTotalHoldTime() ?? 0f;
    }
    #endregion

    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(318);
        currentRecordTitle.text = LanguageManager.Instance.GetText(317);
        raceAgainText.text = LanguageManager.Instance.GetText(321);
        mainMenuText.text = LanguageManager.Instance.GetText(330);
        powerText.text = LanguageManager.Instance.GetText(326);
        staminaText.text = LanguageManager.Instance.GetText(328);
        coolingText.text = LanguageManager.Instance.GetText(327);
        levelText.text = LanguageManager.Instance.GetText(319);
        racingStatsTitleText.text = LanguageManager.Instance.GetText(331);
        adsMoneyText.text = GetAdsAmountByScene(sceneType).ToString();
    }
    public void Clear()
    {
        StopAllCoroutines();

        foreach (var it in _spawned)
        {
            if (it)
            {
                LeanTween.cancel(it.gameObject);
                Destroy(it.gameObject);
            }
        }
        _spawned.Clear();


    }

    public void Replay()
    {
        if (lastPower < Constants.HorseConditionNum.Power || lastCooling < Constants.HorseConditionNum.Cool || lastStamina < Constants.HorseConditionNum.Stamina)
        {
            OpenFoodPanelPopup();
            return;  // Racing davom etmaydi
        }
        bool success = CurrencyManager.Instance != null && CurrencyManager.Instance.SpendNyufiy(CheckRoomCost(), true);
        if(!success)
        {
            // pul yetmayapti
            UIOverlayRoot.I.Done(487, 488, 498, OnMoneyNotEnoughPlayAgain);
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
    private void PlusMoneyReward()
    {
        int amount = GetAdsAmountByScene(sceneType);
        OnMoneyNotEnough(amount);
    }
    private void OnMoneyNotEnoughPlayAgain()
    {
        OnMoneyNotEnough(CheckRoomCost());
    }
    private void OnMoneyNotEnough(int amount)
    {
        GameAnalyticsEvents.RewardedAdClicked(
          placement: "coin_shop",
          rewardType: "nyufiy",
          rewardAmount: amount
      );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(amount, true);


            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: amount
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: amount
            );

        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }
    public void PlayAgain()
    {
        UIOverlayRoot.I.ShowMovementPanelForScene(sceneType);
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    private void OpenTacticItemsPanel()
    {
        UIButtonActions.Instance?.OpenItemsPanel();
    }
    public void BackLobby()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
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
    #region Resources

    private void OpenFoodPanelPopup()
    {
        UIOverlayRoot.I.Done(431, 432, 433, EnableFoodPage, null);
    }
    private void EnableFoodPage()
    {
        this.gameObject.SetActive(false);
        UIButtonActions.Instance?.OpenFoodPanel();
    }

    #endregion

}
