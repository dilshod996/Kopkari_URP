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
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private Button foodPanelEnablerBtn;
    [SerializeField] private TMP_Text foodResourcesBtnText;
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
    [SerializeField] private TMP_Text eleksirAmountText;
    [SerializeField] private TMP_Text waterAmountText;

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

    [SerializeField] private float duration = 4f;     // qancha davom etadi
    [SerializeField] private float scaleMin = 1f;     // boshlanish scale
    [SerializeField] private float scaleMax = 1.05f;  // maksimal scale

    public static Action<int> OnGetRiderRank;

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
        FoodNotNeeded();
        UIButtonActions.OnSprintHold += GetOverallBoostTime;
        RacingController.OnOverallBoostTime += GetOverallBoostTime;
       // FoodShowerPopup.OnFoodGivenWithStats += ApplyFoodBuffs;
        //FoodShowerPopup.OnBuyBtnPressed += UpdateNyufiy;
        if (LanguageManager.Instance != null) UITransilations();
        ShowResults();
        foodPanelEnablerBtn.onClick.AddListener(EnableFoodPage);
    }
    private void OnDisable()
    {
        replayButton.onClick.RemoveAllListeners();
        backToHome.onClick.RemoveAllListeners();
        UIButtonActions.OnSprintHold -= GetOverallBoostTime;
        RacingController.OnOverallPenaltyTime -= GetOverallPenaltyTime;
        RacingController.OnOverallBoostTime -= GetOverallBoostTime;
        //FoodShowerPopup.OnFoodGivenWithStats -= ApplyFoodBuffs;
       // FoodShowerPopup.OnBuyBtnPressed -= UpdateNyufiy;
        foodPanelEnablerBtn.onClick.RemoveListener(EnableFoodPage);
        Clear();
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
                int level = PlayerPrefs.GetInt(Constants.HorseCondition.Level, 0);

                levelText.text = LanguageManager.Instance?.GetText(319) + $"{level}" +"/20";
                int taqaPrize = 0;
                int nyufiyPrize = 0;

                switch (e.Ranking)
                {
                    case 1: taqaPrize = 4; nyufiyPrize = 2200; break;
                    case 2: taqaPrize = 2; nyufiyPrize = 1700; break;
                    case 3: taqaPrize = 1; nyufiyPrize = 1300; break;
                    default: taqaPrize = 0; nyufiyPrize = 500; break;
                }

                OnGetRiderRank?.Invoke(e.Ranking);

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
                float savedTime = PlayerPrefs.GetFloat(Constants.Record.Zarafshan);

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
                overAllTime = savedTime;
                //Debug.Log($"Split time {e.LastSplitTime}");
            }
        }

        Debug.Log("player list done");
    }

    #endregion

    #region Horse Details
    private void HorseStats()
    {
        // --- Load ---
        float horsePowerMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float horseCoolingMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float horseStaminaMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        Debug.Log($"Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime}");
        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - overAllPenaltyTime;     // penalty bo‘lmagan vaqt

        float newPower = horsePowerMain - (overAllBoostTime * 0.5f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.05f);

        newPower = Mathf.Max(0, newPower);
        newStamina = Mathf.Max(0, newStamina);//Hard coded now
        newCooling = Mathf.Max(0, newCooling);


        float rPower = Mathf.Round(newPower);          // butun son (masalan: 83)
        float rStamina = Mathf.Round(newStamina);
        float rCooling = Mathf.Round(newCooling);
        // Progress Bar Updatelar
        powerProgress.currentPercent = rPower;
        powerProgress.UpdateUI();
        coolingProgress.currentPercent = rCooling;
        coolingProgress.UpdateUI();
        staminaProgress.currentPercent = rStamina;
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
    private void GetOverallPenaltyTime(float time)
    {
        overAllPenaltyTime+= time; 
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
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        int langId = -1;
        if (currentPower < 20)
            langId = 334;

        if (currentCooling < 20)
            langId = 335;

        if (currentStamina < 30)
            langId = 336;


        if (currentPower < 20 || currentCooling < 20 || currentStamina < 30)
        {
            SHowResourcesNotEnough();
            alarmMessage.text = LanguageManager.Instance.GetText(langId);
            return;  // Racing davom etmaydi
        }
        //Clear();
        PlayAgainText();
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    public void PlayAgainText()
    {
        switch (sceneType)
        {
            case SceneLoadManager.SceneType.SecondRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, "This time is your win!");
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, "Egypt People waiting you race");
                break;
        }
    }
    public void BackLobby()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, "Back To Home");
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
    private void FoodNotNeeded()
    {
        foodPanelEnablerBtn.gameObject.SetActive(false);
        alarmMessage.text = LanguageManager.Instance?.GetText(368);
    }
    private void EnableFoodPage()
    {
        this.gameObject.SetActive(false);
        UIButtonActions.Instance.ShowUI(foodPanel);
    }

}
