using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RacingResultPage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;   // VerticalLayoutGroup
    [SerializeField] private UIRacingPlayerFinal itemPrefab;
    [SerializeField] private Button replayButton;            // Yangi qo‘shilgan Start Button
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
    [SerializeField] private TMP_Text alarmMessage;

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
    public SceneLoadManager.SceneType sceneType;

    private readonly List<UIRacingPlayerFinal> _spawned = new();


    private float overAllTime = 0f;
    private float overAllBoostTime=0f;
    private float overAllPenaltyTime=0f;
    private float overAllWalkZoneTime = 0f;

    private float lastPower = 0f;
    private float lastCooling = 0f;
    private float lastStamina = 0f;

    [SerializeField] private float duration = 4f;     // qancha davom etadi
    [SerializeField] private float scaleMin = 1f;     // boshlanish scale
    [SerializeField] private float scaleMax = 1.05f;  // maksimal scale

    [SerializeField] private ConditionCheck conditionCheck;
    public static Action<int> OnGetRiderRank;

    [Header("Not Enough Resource")]
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private Button foodPanelEnablerBtn;
    [SerializeField] private TMP_Text foodResourcesBtnText;
    private bool rewardGiven;

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
        foodPanelEnablerBtn.onClick.AddListener(OpenFoodPanelPopup);
        Booster.OnWalkZoneDamagedTime += GetWalkZoneOverAllTime;
        adWatchBtn.onClick.AddListener(PlusMoneyReward);
    }
    private void OnDisable()
    {
        replayButton.onClick.RemoveAllListeners();
        backToHome.onClick.RemoveAllListeners();
        adWatchBtn.onClick.RemoveAllListeners();
        Clear();
        Booster.OnWalkZoneDamagedTime -= GetWalkZoneOverAllTime;
    }
    #region Player List && Racing Stats && Records
    public void ShowResults()
    {
        var lb = RacingLeaderboard.Instance;
        var standings = lb?.GetStandings();

        if (standings == null || standings.Count == 0)
        {
            Debug.Log("[ShowResultPanel] standings bo'sh yoki null – panelni hozircha ko'rsatmiman");
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

        entries = sortByRankingAsc
            ? entries.OrderBy(e => e.Ranking).ToList()
            : entries.OrderByDescending(e => e.Ranking).ToList();

        float startX = 50f;
        float startY = -20f;

        float stepX = -18f;   
        float stepY = -100f;  // har safar y dan 100 pastga: -20, -120, -220, ...

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];

            var item = Instantiate(itemPrefab, contentParent);
            item.Bind(e);
            _spawned.Add(item);

            var rt = item.GetComponent<RectTransform>();

            // ⚠ anchori/pivoti 1 marta shu yerda to‘g‘rilab qo‘yamiz
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            rt.localScale = Vector3.one;

            // PREFABdagi eski offsetni yo‘qotish uchun avval nolga qo‘yamiz
            rt.anchoredPosition = Vector2.zero;

            // Endi o‘zimiz kerakli koordinatani beramiz
            float x = startX + i * stepX;
            float y = startY + i * stepY;

            rt.anchoredPosition = new Vector2(x, y);

            if (e.isPlayer && !rewardGiven)
            {
                rewardGiven = true;
                var prize = GetRacePrizeByMapAndRank(e.Ranking);

                int taqaPrize = prize.taqaPrize;
                int nyufiyPrize = prize.nyufiyPrize;
                int levelUpPoint = prize.levelUpPoint;
                if(xpAmountText!=null)
                {
                    xpAmountText.text = levelUpPoint.ToString();
                }
                DataManager.Instance.AddLevelPoint(levelUpPoint, true);
                OnGetRiderRank?.Invoke(e.Ranking);
                levelText.text = $"{LanguageManager.Instance.GetText(319)} {DataManager.Instance.LevelAmount}/20";

                nyufiyAmountText.text = $"+{nyufiyPrize:N0}";
                coinAmountText.text = $"+{taqaPrize:N0}";

                CurrencyManager.Instance.AddNyufiy(nyufiyPrize, false);
                CurrencyManager.Instance.AddCoin(taqaPrize, true);

                allNyufiyText.text = $"{CurrencyManager.Instance.Nyufiy:N0}";
                allCoinText.text = $"{CurrencyManager.Instance.Coin:N0}";

                // Record tekshirish va yangilash bu faqat hozir Zarafshan uchun ishlaydi, boshqa xaritalar uchun kerak bo‘lsa shartni kengaytirish kerak

                string recordKey = Constants.MapNames.Zarafshan;

                if (sceneType == SceneLoadManager.SceneType.EgyptRacing)
                    recordKey = Constants.MapNames.Egypt;
                else if (sceneType == SceneLoadManager.SceneType.Kansas)
                    recordKey = Constants.MapNames.Kansas;

                float savedTime = DataManager.Instance != null ? DataManager.Instance.GetBestRecord(recordKey) : 0f;
                if (savedTime == 0 || savedTime > e.LastSplitTime)
                {
                    recordText.text = LanguageManager.Instance?.GetText(315);
                    savedTime = e.LastSplitTime;

                    DataManager.Instance?.SaveBestRecord(recordKey, savedTime);
                }
                else
                {
                    recordText.text = LanguageManager.Instance?.GetText(316);
                }

                timeText.text = $"{e.LastSplitTime:0.00}s";
                currentRecordTime.text = $"{savedTime:0.00}s";
                overAllTime = e.LastSplitTime;
                GameAnalyticsEvents.RaceFinished(sceneType.ToString(), "racing", e.Ranking, nyufiyPrize);
                Debug.Log($"Ranking: {e.Ranking}");
                bool isWin = e.Ranking == 1;
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.SaveRaceResult(GetMapKey(sceneType), isWin, (int)e.LastSplitTime);
                }
                //Debug.Log($"Split time {e.LastSplitTime}");
            }
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
                    1 => (4, 2600, GetRandomXpByRank(1)),
                    2 => (3, 2000, GetRandomXpByRank(2)),
                    3 => (2, 1500, GetRandomXpByRank(3)),
                    _ => (0, 600, GetRandomXpByRank(4))
                };

            case RacingController.RacingType.Kansas:
                return ranking switch
                {
                    1 => (6, 3000, GetRandomXpByRank(1)),
                    2 => (3, 2300, GetRandomXpByRank(2)),
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
        // --- Load ---
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float horsePowerMain = current.Power;
        float horseCoolingMain = current.Cooling;
        float horseStaminaMain = current.Stamina;
        Debug.Log($"Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime}  over all walkzone time{overAllWalkZoneTime}");
        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - (overAllPenaltyTime + overAllWalkZoneTime);     // penalty bo‘lmagan vaqt

        float newPower = horsePowerMain - (overAllBoostTime * 0.2f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.2f);
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
        powerProgress.currentPercent = lastPower;
        powerProgress.UpdateUI();
        coolingProgress.currentPercent = lastCooling;
        coolingProgress.UpdateUI();
        staminaProgress.currentPercent = lastStamina;
        staminaProgress.UpdateUI();
        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(rPower, rCooling, rStamina));

        Debug.Log($"Horse Stats Updated → Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }
    private void GetOverallBoostTime(float time)
    {
        overAllBoostTime+= time;
    }
    private void GetOverallPenaltyTime()
    {
        overAllPenaltyTime = UIButtonActions.Instance?.GetTotalWebSnareTime() ?? 0f; 
    }
    private void GetWalkZoneOverAllTime(float time)
    {
        overAllWalkZoneTime= time;
        Debug.Log($"[WalkZone time] {overAllWalkZoneTime}");
    }
    private void GetBoostTime()
    {
        overAllBoostTime = UIButtonActions.Instance?.GetTotalHoldTime() ?? 0f;
    }
    private void ApplyFoodBuffs(float powerPercent, float coolingPercent, float staminaPercent)
    {
        HorseConditionStats current = HorseConditionStatsService.AddFood(
            powerPercent,
            coolingPercent,
            staminaPercent);

        // 4) UI barlarni yangilaymiz
        powerProgress.currentPercent = current.Power;
        coolingProgress.currentPercent = current.Cooling;
        staminaProgress.currentPercent = current.Stamina;

        powerProgress.UpdateUI();
        coolingProgress.UpdateUI();
        staminaProgress.UpdateUI();
    }
    #endregion

    #region Resources

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
            SHowResourcesNotEnough();
            alarmMessage.text = LanguageManager.Instance.GetText(334);
         
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
        if (sceneType == SceneLoadManager.SceneType.TrainingRacing)
        {
            UIOverlayRoot.I.ShowPanel(UIPanelType.RacingTutorial, LanguageManager.Instance.GetText(497), instant: false);
        }
        else if (sceneType == SceneLoadManager.SceneType.SecondRacing)
        {
            UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, LanguageManager.Instance.GetText(209), instant: false);
        }
        else if (sceneType == SceneLoadManager.SceneType.EgyptRacing)
        {
            UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, LanguageManager.Instance.GetText(210), instant: false);
        }
        else if(sceneType == SceneLoadManager.SceneType.Kansas)
        {
            UIOverlayRoot.I.ShowPanel(UIPanelType.Kansas, LanguageManager.Instance.GetText(519), instant: false);
        }
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    private void OpenTacticItemsPanel()
    {
        UIButtonActions.Instance.OpenItemsPanel();
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
            //case SceneLoadManager.SceneType.TexasRacing:
            //    return 2;

            default:
                return 0;
        }
    }
    #region Resources
    private void SHowResourcesNotEnough()
    {
        StartCoroutine(PulseRoutine());
        foodPanelEnablerBtn?.gameObject.SetActive(true);
        foodResourcesBtnText.text = LanguageManager.Instance?.GetText(369);
    }
    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        RectTransform rt = foodPanelEnablerBtn.GetComponent<RectTransform>();

        while (t < duration)
        {
            t += Time.deltaTime;

            // 0 → 1 → 0 yurak urishi effekti
            float pingPong = Mathf.PingPong(Time.time * 2, 1f);

            float scale = Mathf.Lerp(scaleMin, scaleMax, pingPong);

            rt.localScale = new Vector3(scale, scale, 1);

            yield return null;
        }

        rt.localScale = Vector3.one;
    }

    private void OpenFoodPanelPopup()
    {
        UIOverlayRoot.I.Done(431, 432, 433, EnableFoodPage, null);
    }
    private void EnableFoodPage()
    {
        this.gameObject.SetActive(false);
        UIButtonActions.Instance.ShowUI(foodPanel);
    }

    #endregion

}
