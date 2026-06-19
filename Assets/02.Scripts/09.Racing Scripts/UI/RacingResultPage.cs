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
    }
    private void OnDisable()
    {
        replayButton.onClick.RemoveAllListeners();
        backToHome.onClick.RemoveAllListeners();
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

            if (e.isPlayer)
            {
                var prize = GetRacePrizeByMapAndRank(e.Ranking);

                int taqaPrize = prize.taqaPrize;
                int nyufiyPrize = prize.nyufiyPrize;
                int levelUpPoint = prize.levelUpPoint;
                if(xpAmountText!=null)
                {
                    xpAmountText.text = levelUpPoint.ToString();
                }
                AddLevelPoint(levelUpPoint);
                OnGetRiderRank?.Invoke(e.Ranking);
                int getLevel = PlayerPrefs.GetInt(Constants.Level.LevelAmount, 1);
                levelText.text = $"{LanguageManager.Instance.GetText(319)} {getLevel}/20";
                nyufiyAmountText.text = $"+{nyufiyPrize:N0}";
                coinAmountText.text = $"+{taqaPrize:N0}";

                int allNyufiy = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
                int allCoin = PlayerPrefs.GetInt(Constants.Coins.Coin);
                allNyufiy += nyufiyPrize;
                allCoin += taqaPrize;
                allNyufiyText.text = $"{allNyufiy:N0}";
                allCoinText.text = $"{allCoin:N0}";
                PlayerPrefs.SetInt(Constants.Coins.Nyufiy, allNyufiy);
                PlayerPrefs.SetInt(Constants.Coins.Coin, allCoin);

                // Record tekshirish va yangilash bu faqat hozir Zarafshan uchun ishlaydi, boshqa xaritalar uchun kerak bo‘lsa shartni kengaytirish kerak
                float savedTime = 0;
                if (sceneType == SceneLoadManager.SceneType.SecondRacing)
                {
                    savedTime = PlayerPrefs.GetFloat(Constants.Record.Zarafshan);
                }
                else if (sceneType == SceneLoadManager.SceneType.EgyptRacing)
                {
                    savedTime = PlayerPrefs.GetFloat(Constants.Record.Egypt);
                }
                else if (sceneType == SceneLoadManager.SceneType.Kansas)
                {
                    savedTime = PlayerPrefs.GetFloat(Constants.Record.Kansas);
                }

                if (savedTime == 0 || savedTime > e.LastSplitTime)
                {
                    recordText.text = LanguageManager.Instance?.GetText(315);
                    savedTime = e.LastSplitTime;
                    PlayerPrefs.SetFloat(Constants.Record.Zarafshan, savedTime);
                }
                else
                {
                    recordText.text = LanguageManager.Instance?.GetText(316);
                }

                timeText.text = $"{e.LastSplitTime:0.00}s";
                currentRecordTime.text = $"{savedTime:0.00}s";
                overAllTime = e.LastSplitTime;
                //Debug.Log($"Split time {e.LastSplitTime}");
            }
        }

        Debug.Log("player list done");
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
    private void AddLevelPoint(int earnedXp)
    {
        if (earnedXp <= 0)
            return;

        int currentLevel = PlayerPrefs.GetInt(Constants.Level.LevelAmount, 1);
        int currentXp = PlayerPrefs.GetInt(Constants.Level.XP, 0); // sliderdagi xp
        int pendingCount = PlayerPrefs.GetInt(Constants.Level.LevelUpPending, 0);

        currentXp += earnedXp;

        while (currentXp >= 100)
        {
            currentXp -= 100;
            currentLevel++;
            pendingCount++;
        }

        PlayerPrefs.SetInt(Constants.Level.LevelAmount, currentLevel);
        PlayerPrefs.SetInt(Constants.Level.XP, currentXp);
        PlayerPrefs.SetInt(Constants.Level.LevelUpPending, pendingCount);
        PlayerPrefs.Save();
    }
    #endregion

    #region Horse Details
    private void HorseStats()
    {
        // --- Load ---
        float horsePowerMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float horseCoolingMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float horseStaminaMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
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
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, rPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, rStamina);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, rCooling);

        PlayerPrefs.Save();

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
        // 1) PlayerPrefs dagi qiymatlarni olamiz
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);

        // 2) Bufflarni qo‘shamiz
        currentPower = Mathf.Clamp(currentPower + powerPercent, 0f, 100f);
        currentCooling = Mathf.Clamp(currentCooling + coolingPercent, 0f, 100f);
        currentStamina = Mathf.Clamp(currentStamina + staminaPercent, 0f, 100f);

        // 3) Yangi qiymatlarni saqlaymiz
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, currentPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, currentCooling);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, currentStamina);
        PlayerPrefs.Save();

        // 4) UI barlarni yangilaymiz
        powerProgress.currentPercent = currentPower;
        coolingProgress.currentPercent = currentCooling;
        staminaProgress.currentPercent = currentStamina;

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
    }

    private void UpdateNyufiy()
    {
        int allNyufiy = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        allNyufiyText.text = allNyufiy > 0 ? $"{allNyufiy:N0}" : "0";
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
        int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        if(nyufiyAmount < CheckRoomCost())
        {
            // pul yetmayapti
            UIOverlayRoot.I.Done(487, 488, 498, OnMoneyNotEnough);
            return;
        }
        nyufiyAmount -= CheckRoomCost();
        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
        if (lastPower < Constants.HorseConditionNum.Power || lastCooling < Constants.HorseConditionNum.Cool || lastStamina < Constants.HorseConditionNum.Stamina)
        {
            SHowResourcesNotEnough();
            alarmMessage.text = LanguageManager.Instance.GetText(334);
         
            return;  // Racing davom etmaydi
        }
        int defenseCheck = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
        if (defenseCheck < 1)
        {
            UIOverlayRoot.I.Confirm(493, 494, 496, 253, OpenTacticItemsPanel, PlayAgain);
        }
        else
        {
            PlayAgain();
        }

    }
    private void OnMoneyNotEnough()
    {
        //Watch ads
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
