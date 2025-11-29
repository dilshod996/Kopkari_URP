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
        UIButtonActions.OnSprintHold += GetOverallBoostTime;
        RacingController.OnOverallBoostTime += GetOverallBoostTime;
        RacingController.OnOverallBoostTime -= GetOverallBoostTime;
        if (LanguageManager.Instance != null) UITransilations();
        ShowResults();
    }
    private void OnDisable()
    {
        replayButton.onClick.RemoveAllListeners();
        backToHome.onClick.RemoveAllListeners();
        UIButtonActions.OnSprintHold -= GetOverallBoostTime;
        RacingController.OnOverallPenaltyTime -= GetOverallPenaltyTime;
        RacingController.OnOverallBoostTime -= GetOverallBoostTime;
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
                    case 1: taqaPrize = 2; nyufiyPrize = 1500; break;
                    case 2: taqaPrize = 1; nyufiyPrize = 1100; break;
                    case 3: taqaPrize = 0; nyufiyPrize = 700; break;
                    default: taqaPrize = 0; nyufiyPrize = 300; break;
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
                float savedTime = PlayerPrefs.GetFloat(Constants.Record.BaxmalRacing);

                if (savedTime == 0 || savedTime > e.LastSplitTime)
                {
                    recordText.text = LanguageManager.Instance?.GetText(315);
                    savedTime = e.LastSplitTime;
                    PlayerPrefs.SetFloat(Constants.Record.BaxmalRacing, savedTime);
                }
                else
                {
                    recordText.text = LanguageManager.Instance?.GetText(316);
                }

                timeText.text = $"{e.LastSplitTime:0.00}s";
                currentRecordTime.text = $"{savedTime:0.00}s";
                overAllTime = savedTime;
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

        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - overAllPenaltyTime;     // penalty bo‘lmagan vaqt

        float newPower = horsePowerMain - (overAllBoostTime * 0.5f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.05f);

        newPower = Mathf.Max(0, newPower);
        newStamina = Mathf.Max(0, newStamina);
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
        Clear();
        SceneLoadManager.Instance.LoadScene(sceneType);
    }
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Home);
    }

}
