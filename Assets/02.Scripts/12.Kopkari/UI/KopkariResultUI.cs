using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class KopkariResultUI : MonoBehaviour
{
    [Header("UI Title Texts")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text nameTeamNameInfoText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text racingStatsText;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private TMP_Text currentRecordText;
    [SerializeField] private TMP_Text overallTimeText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text coolingText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text bonusText;
    [SerializeField] private TMP_Text bonusDetailsText;

    [Header("Buttons")]
    [SerializeField] private Button playMore;
    [SerializeField] private Button moveToHome;
    [SerializeField] private Button adWatchBtn;
    [SerializeField] private TMP_Text playMoreText;
    [SerializeField] private TMP_Text moveToHomeText;

    [SerializeField] private RectTransform playersParent;
    [SerializeField] private KopkariPlayersResult resultInfoPrefab;

    [Header("Earnings")]
    [SerializeField] private TMP_Text nyufiyAmountText;
    [SerializeField] private TMP_Text coinAmountText;

    [Header("Game Texts")]
    [SerializeField] private TMP_Text mainTimeAmountText;
    [SerializeField] private TMP_Text bonusAmountText;
    [SerializeField] private TMP_Text horseNameText;
    [SerializeField] private TMP_Text nyufiyEarningAmount;
    [SerializeField] private TMP_Text coinEarningAmount;
    [SerializeField] private TMP_Text xpAddAmountText;
    [SerializeField] private TMP_Text totalCatchTime;
    [SerializeField] private TMP_Text currentRecordCatchTime;

    [SerializeField] private TMP_Text adsWatchAmountNyufiy;

    [Header("Player Round Details")]
    [SerializeField] private TMP_Text playerPickupCountText;
    [SerializeField] private TMP_Text playerCarrierTakeoverCountText;
    [SerializeField] private TMP_Text playerRoundWinsText;
    [SerializeField] private TMP_Text playerWinningTimeText;

    [Header("Horse Statistics")]
    [SerializeField] private ProgressBar powerProgress;
    [SerializeField] private ProgressBar staminaProgress;
    [SerializeField] private ProgressBar coolingProgress;

    private readonly List<KopkariPlayersResult> _spawnedRows = new();

    private float overAllTime = 0f;
    private float overAllBoostTime = 0f;
    private float overAllPenaltyTime = 0f;
    private float totalUloqCatchTime = 0f;
    [Header("Foods")]
    [SerializeField] private GameFood gameFoodPage;
    [SerializeField] private Button gameFoodBtn;
    [SerializeField] private TMP_Text gameFoodBtnText;
    [SerializeField] private TMP_Text notenoughResource;
    private bool rewardGiven;

    private void OnEnable()
    {
        moveToHome.onClick.AddListener(BackToHome);
        gameFoodBtn.onClick.AddListener(EnableFoodPage);
        playMore.onClick.AddListener(PlayMore);
        RefreshUI();
        UITransilation();
        FoodNeeded();
    }
    private void OnDisable()
    {
        ClearPlayersList();
        moveToHome.onClick.RemoveListener(BackToHome);
        gameFoodBtn.onClick.RemoveListener(EnableFoodPage);
        playMore.onClick.RemoveListener(PlayMore);
    }
    #region Transilations
    private void UITransilation()
    {
        string horsename = GetStringPrefs(Constants.Horse.HorseNameKey);
        horseNameText.text = horsename;
        if(LanguageManager.Instance != null)
        {
            resultText.text = LanguageManager.Instance.GetText(318);
            powerText.text = LanguageManager.Instance.GetText(326);
            staminaText.text = LanguageManager.Instance.GetText(328);
            coolingText.text = LanguageManager.Instance.GetText(327);
            //levelText.text = LanguageManager.Instance.GetText(319);
            nameTeamNameInfoText.text = LanguageManager.Instance.GetText(340);
            racingStatsText.text = LanguageManager.Instance.GetText(341);
            recordText.text = LanguageManager.Instance.GetText(315);
            currentRecordText.text = LanguageManager.Instance.GetText(317);
            overallTimeText.text = LanguageManager.Instance.GetText(342);
            bonusText.text = LanguageManager.Instance.GetText(343);
            moveToHomeText.text = LanguageManager.Instance.GetText(330);
        }
    }
    #endregion

    #region Players List
    public void RefreshFromResults()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        FillOverallTime();
        BuildPlayersList();
        FillPlayerRoundDetails();
        //FillPlayerStats();
        ApplyPlayerRankRewards(); // ✅ shu
        HorseStats();
    }

    private void FillOverallTime()
    {
        // RaceDuration ni managerdan olamiz
        float raceDuration = 0f;
        if (KopkariResultsManager.Instance != null)
            raceDuration = KopkariResultsManager.Instance.RaceDuration; // shu yerda tuxtadim

        string timeStr = FormatTime(raceDuration);
        if (mainTimeAmountText) mainTimeAmountText.text = timeStr; // agar shu joyga ham yozmoqchi bo‘lsang
        overAllTime = raceDuration;
    }

    private void BuildPlayersList()
    {
        ClearPlayersList();

        if (playersParent == null || resultInfoPrefab == null)
        {
            Debug.Log("[KopkariResultUI] playersParent or prefab is missing.");
            return;
        }

        var mgr = KopkariResultsManager.Instance;
        if (mgr == null)
        {
            Debug.Log("[KopkariResultUI] KopkariResultsManager.Instance is null.");
            return;
        }

        var leaderboard = mgr.BuildLeaderboard();
        for (int i = 0; i < leaderboard.Count; i++)
        {
            var row = Instantiate(resultInfoPrefab, playersParent);
            row.BindData(leaderboard[i], i);
            _spawnedRows.Add(row);
        }
    }

    private void ClearPlayersList()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }
        _spawnedRows.Clear();

        // agar oldin boshqa childlar ham qolib ketayotgan bo‘lsa:
        // for (int i = playersParent.childCount - 1; i >= 0; i--)
        //     Destroy(playersParent.GetChild(i).gameObject);
    }

    private void FillPlayerRoundDetails()
    {
        KopkariResultsManager manager = KopkariResultsManager.Instance;
        if (manager == null)
            return;

        RiderRaceStats player = manager.BuildLeaderboard().Find(stats => stats != null && stats.isPlayer);
        if (player == null)
            return;

        if (playerPickupCountText) playerPickupCountText.text = player.pickupTimes.ToString();
        if (playerCarrierTakeoverCountText) playerCarrierTakeoverCountText.text = player.carrierTakeovers.ToString();
        if (playerRoundWinsText) playerRoundWinsText.text = player.roundWins.ToString();
        if (playerWinningTimeText)
            playerWinningTimeText.text = player.roundWins > 0
                ? FormatTime(player.lastRoundFinishTime)
                : "--:--";
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
    #endregion

    #region Player Ranking
    private int GetNyufiyByRank(int rank1Based)
    {
        switch (rank1Based)
        {
            case 1: return 3400;
            case 2: return 2600;
            case 3: return 1800;
            case 4: return 1000;
            case 5: return 800;
            default: return 200;
        }
    }
    private int GetCoinByRank(int rank1Based)
    {
        switch (rank1Based)
        {
            case 1: return 7;
            case 2: return 5;
            case 3: return 3;
            default: return 0;
        }
    }
    private int GetRandomXpByRank(int ranking)
    {
        return ranking switch
        {
            1 => Random.Range(22, 26), // 18-22
            2 => Random.Range(15, 21), // 12-16
            3 => Random.Range(10, 15),  // 8-12
            _ => Random.Range(7, 11)    // 4-6
        };
    }
    private void ApplyPlayerRankRewards()
    {
        if (rewardGiven)
            return;
        rewardGiven = true;
        var mgr = KopkariResultsManager.Instance;
        if (mgr == null) return;

        var leaderboard = mgr.BuildLeaderboard();
        if (leaderboard == null || leaderboard.Count == 0) return;

        // ✅ Player statsni topamiz (2 usuldan bittasini tanla)
        int playerIndex = leaderboard.FindIndex(s => s != null && s.isPlayer);   // usul A
                                                                                 // int playerIndex = leaderboard.FindIndex(s => s != null && s.riderId == 0); // usul B
  

        if (playerIndex < 0)
        {
            Debug.Log("[Rewards] Player not found in leaderboard!");
            return;
        }
        var playerStats = leaderboard[playerIndex];
        Debug.Log($"Player Stats name: {playerStats.playerName}");
        //Record Timing
        if (playerStats == null) return;
        float recordAmount = DataManager.Instance != null ? DataManager.Instance.GetBestRecord(Constants.MapNames.Registan) : 0f;
        if (recordAmount == 0 || recordAmount > playerStats.totalCatchTime)
        {
            //record
            recordText.text = LanguageManager.Instance?.GetText(315);
            recordAmount = playerStats.totalCatchTime;
            DataManager.Instance.SaveBestRecord(Constants.MapNames.Registan, recordAmount);
        }
        else
        {
            recordText.text = LanguageManager.Instance?.GetText(316);
        }
        // catch time (formatlab)
        totalUloqCatchTime = playerStats.totalCatchTime;
        currentRecordCatchTime.text = FormatTime(recordAmount);
        totalCatchTime.text = FormatTime(playerStats.totalCatchTime);
        //Bonus
        int bonusAmount = BonusAmount(playerStats.pickupTimes);

        int rank = playerIndex + 1;               // 1-based
        int nyufiyReward = GetNyufiyByRank(rank) +
                           playerStats.nyufiyPrize +
                           playerStats.comboPrize +
                           bonusAmount;
        int coinReward = GetCoinByRank(rank) + playerStats.coinPrize;
        int xpAmount = GetRandomXpByRank(rank);
        DataManager.Instance.AddLevelPoint(xpAmount, true);
        if (xpAddAmountText != null)
        {
            xpAddAmountText.text = xpAmount.ToString();
        }

        // UI ga chiqarish
        if (nyufiyEarningAmount) nyufiyEarningAmount.text = nyufiyReward.ToString();
        if(coinEarningAmount) coinEarningAmount.text = coinReward.ToString();
        levelText.text = $"{LanguageManager.Instance.GetText(319)} {DataManager.Instance.LevelAmount}/20";
        bonusAmountText.text = $"+{bonusAmount.ToString()}";

        UpdatCoins(nyufiyReward, coinReward);
        Debug.Log($"[Rewards] Player rank={rank}, NyufiyReward={nyufiyReward}, Coin={coinReward}");
        bool isWinner = rank == 1;
        DataManager.Instance.SaveRaceResult(Constants.MapNames.Registan, isWinner, (int)overAllTime);
    }
    
    private void FillPlayerStats()
    {
        var mgr = KopkariResultsManager.Instance;
        if (mgr == null) return;

        var leaderboard = mgr.BuildLeaderboard();
        var playerStats = leaderboard.Find(s => s != null && s.isPlayer);

        if (playerStats == null) return;
        float recordAmount = DataManager.Instance != null ? DataManager.Instance.GetBestRecord(Constants.MapNames.Registan) : 0f;
        if(recordAmount == 0 || recordAmount> playerStats.totalCatchTime)
        {
            //record
            recordText.text = LanguageManager.Instance?.GetText(315);
            recordAmount = playerStats.totalCatchTime;
            DataManager.Instance.SaveBestRecord(Constants.MapNames.Registan, recordAmount);
        }
        else
        {
            recordText.text = LanguageManager.Instance?.GetText(316);
        }
        // catch time (formatlab)
        currentRecordCatchTime.text  = FormatTime(recordAmount);
        totalCatchTime.text = FormatTime(playerStats.totalCatchTime);

        //main time


    }

    #endregion

    #region Button Actions
    private void BackToHome()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Home);
    }
    #endregion

    #region Coin Updates

    private void UpdatCoins(int nyufiyAmount, int coinAmount)
    {
        CurrencyManager.Instance.AddNyufiy(nyufiyAmount,true);
        CurrencyManager.Instance.AddCoin(coinAmount, true);

        nyufiyAmountText.text = $"{CurrencyManager.Instance.Nyufiy:N0}";
        coinAmountText.text = $"{CurrencyManager.Instance.Coin:N0}";

       // $"{allCoin:N0}";
    }

    #endregion

    #region Bonus
    private int BonusAmount(int pickUpTimes)
    {
        string bonusComing;
        string bonusDetails;

        switch (pickUpTimes)
        {
            case 0:
                bonusComing = LanguageManager.Instance?.GetText(339);
                bonusDetails = string.Format(bonusComing, pickUpTimes);
                bonusDetailsText.text = bonusDetails;
                return 250;
            case 1:
                bonusComing = LanguageManager.Instance?.GetText(338);
                bonusDetails = string.Format(bonusComing, pickUpTimes);
                bonusDetailsText.text = bonusDetails;
                return 50;

            case 2:
                bonusComing = LanguageManager.Instance?.GetText(337);
                bonusDetails = string.Format(bonusComing, pickUpTimes);
                bonusDetailsText.text = bonusDetails;
                return 150;

            default:
                bonusComing = LanguageManager.Instance?.GetText(337);
                bonusDetails = string.Format(bonusComing, pickUpTimes);
                bonusDetailsText.text = bonusDetails;
                return 250;
        }
    }

    #endregion

    #region Get Prefs Data
    private int GetIntPrefs(string key)
    {
        return PlayerPrefs.GetInt(key, 0);
    }
    private float GetFloatPrefs(string key) { 
        return PlayerPrefs.GetFloat(key,0);
    }
    private string GetStringPrefs(string key)
    {
        return PlayerPrefs.GetString(key);
    }
    #endregion

    #region Replay or NextRound
    private void PlayMore()
    {
        CheckResources();
        //KopkariMainUI.Instance.HideUI(this);
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

        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - overAllPenaltyTime;     // penalty bo‘lmagan vaqt

        float newPower = horsePowerMain - (overAllBoostTime * 0.5f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        //Uloq Catch time coolingdan ketayapti
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.05f + totalUloqCatchTime * 0.4f);

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
        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(rPower, rCooling, rStamina));

        Debug.Log($"Horse Stats Updated → Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
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

    #region Not Enough Resource
    private void EnableFoodPage()
    {
        this.gameObject.SetActive(false);
        KopkariMainUI.Instance.ShowUI(gameFoodPage);
    }
    private void EnableResourceText()
    {
        notenoughResource.text = LanguageManager.Instance.GetText(364);
    }
    private void CheckResources()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float horsePowerMain = current.Power;
        float horseCoolingMain = current.Cooling;
        float horseStaminaMain = current.Stamina;

        if (horsePowerMain < 30f || horseCoolingMain < 30f || horseStaminaMain < 30f)
        {
            gameFoodBtn.gameObject.SetActive(true);
            gameFoodBtnText.text =  LanguageManager.Instance?.GetText(369);
            EnableResourceText();
        }
        else
        {
            KopkariMainUI.Instance.HideUI(this);
            //warningGO.SetActive(false);
        }

    }
    private void FoodNeeded()
    {
        gameFoodBtn.gameObject.SetActive(false);
        notenoughResource.text = LanguageManager.Instance?.GetText(368);
    }

    #endregion
}
