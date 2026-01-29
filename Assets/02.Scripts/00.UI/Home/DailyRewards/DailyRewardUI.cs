using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    [Header("1–7 days")]
    [SerializeField] private RewardDayUI[] first7Days; // size = 7

    [Header("Day 8 (BIG)")]
    [SerializeField] private RewardDayUI day8;

    [Header("Monthly Slider")]
    [SerializeField] private Slider monthlySlider;
    [SerializeField] private GameObject monthlyReadyFx;

    [Header("Texts / Button")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text claimText;
    [SerializeField] private Button claimButton;

    [Header("Config")]
    [SerializeField] private int monthCycleLength = 31;

    // ===== Runtime state =====
    private int streakDay;                 // 1..31
    private int currentMonthProgress;      // 0..31 (slider)
    private int todayDayIndex;             // 1..8 (UI cycle)
    private int lastClaimedCycleDay;       // 0..8
    private bool canClaimToday;

    public int MonthCycleLength => monthCycleLength;
    public int CurrentMonthProgress => currentMonthProgress;
    public int TodayDayIndex => todayDayIndex;
    public int LastClaimedDay => lastClaimedCycleDay;
    public bool CanClaimToday => canClaimToday;

    public event Action<int> OnClaimCompleted;
    public event Action OnMonthlyRewardReady;
    private RewardType todayType = RewardType.None;
    private int todayAmount = 0;
    private FoodInfo.HorseFood todayFood = FoodInfo.HorseFood.None;
    private PlayerResourse.Resources todayResource = PlayerResourse.Resources.None;
    private int todayLanguageId = 0;
    private Sprite todayIcon = null;
    private string todayAmountText = "";

    private void OnEnable()
    {
        RewardDayUI.OnTodayRewardPrepared += OnTodayRewardPrepared;
        // Localize UI text
        if (LanguageManager.Instance != null)
        {
            if (titleText != null) titleText.text = LanguageManager.Instance.GetText(394);
            if (claimText != null) claimText.text = LanguageManager.Instance.GetText(392);
        }

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClickClaim);
        }

        LoadState();
        CheckNewDayAndNotify();

        RefreshDaysUI();
        UpdateMonthlySliderVisual();

    }

    private void OnDisable()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClickClaim);
        RewardDayUI.OnTodayRewardPrepared -= OnTodayRewardPrepared;
    }
    public bool PeekCanClaimToday()
    {
        LoadState();             // prefsdan streak/lastdate o‘qiydi
        CheckNewDayAndNotify();  // canClaimToday ni hisoblaydi
        return CanClaimToday;
    }

    // GameManager.Start() dan chaqirsang ham bo‘ladi
    public void CheckNewDayAndNotify()
    {
        canClaimToday = false;

        string lastClaimStr = PlayerPrefs.GetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE, string.Empty);
        DateTime today = DateTime.UtcNow.Date;

        // first time
        if (string.IsNullOrEmpty(lastClaimStr))
        {
            streakDay = Mathf.Clamp(streakDay, 1, monthCycleLength);
            todayDayIndex = ((streakDay - 1) % 8) + 1;
            canClaimToday = true;

            // ✅ ADD
            lastClaimedCycleDay = (todayDayIndex == 1) ? 0 : (todayDayIndex - 1);

            return;
        }

        // parse
        if (!DateTime.TryParseExact(lastClaimStr, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime lastClaimDate))
        {
            // parse fail -> reset
            streakDay = 1;
            todayDayIndex = 1;
            canClaimToday = true;

            // ✅ ADD
            lastClaimedCycleDay = 0;

            SaveState();
            return;
        }

        int diffDays = (today - lastClaimDate.Date).Days;

        // already claimed today
        if (diffDays <= 0)
        {
            canClaimToday = false;
            todayDayIndex = ((streakDay - 1) % 8) + 1; // UI uchun

            // ✅ ADD (UI correct bo‘lishi uchun)
            lastClaimedCycleDay = (todayDayIndex == 1) ? 0 : (todayDayIndex - 1);

            return;
        }

        // next day
        if (diffDays == 1)
        {
            if (streakDay > monthCycleLength) streakDay = 1;

            todayDayIndex = ((streakDay - 1) % 8) + 1;
            canClaimToday = true;

            // ✅ ADD
            lastClaimedCycleDay = (todayDayIndex == 1) ? 0 : (todayDayIndex - 1);

            SaveState();
        }
        else
        {
            // missed day -> reset streak
            streakDay = 1;
            todayDayIndex = 1;
            canClaimToday = true;

            // ✅ ADD
            lastClaimedCycleDay = 0;

            SaveState();
        }
    }


    public void OnClickClaim()
    {
        ClaimToday();
    }

    public void ClaimToday()
    {
        if (!canClaimToday) return;

        // Monthly claim (31-kun)
        if (streakDay >= monthCycleLength)
        {
            GiveMonthlyReward();

            PlayerPrefs.SetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE,
                DateTime.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

            // reset
            streakDay = 1;
            todayDayIndex = 1;
            lastClaimedCycleDay = 0;
            currentMonthProgress = 0;

            SaveState();

            canClaimToday = false;
            UpdateMonthlySliderVisual();
            OnMonthlyRewardReady?.Invoke();
            gameObject.SetActive(false);
            return;
        }
        if (HomeMainUI.Instance != null && LanguageManager.Instance != null)
        {
            HomeMainUI.Instance.DisplayRewardPopup(
                todayIcon,
                todayAmountText,
                LanguageManager.Instance.GetText(todayLanguageId)
            );
        }
        // Daily claim -> Prefsdan o‘qiymiz (RewardDayUI bugun save qilib qo‘ygan)
        //GrantTodayRewardFromPrefs();
        SaveRewardToPlayerPrefs();
        // mark claimed
        PlayerPrefs.SetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE,
            DateTime.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        // cycle day (1..8)
        lastClaimedCycleDay = todayDayIndex;

        // month progress
        currentMonthProgress = Mathf.Clamp(currentMonthProgress + 1, 0, monthCycleLength);

        // prepare next streak day
        streakDay++;
        if (streakDay > monthCycleLength) streakDay = 1;

        // next UI today index (for next time open)
        todayDayIndex = ((streakDay - 1) % 8) + 1;

        SaveState();

        canClaimToday = false;

        RefreshDaysUI();
        UpdateMonthlySliderVisual();

        OnClaimCompleted?.Invoke(lastClaimedCycleDay);

        gameObject.SetActive(false);
    }

    private void GrantTodayRewardFromPrefs()
    {
        RewardType type = (RewardType)PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_TYPE, 0);
        int amount = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_AMOUNT, 0);
        int enumValue = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_ENUM, 0);

        if (type == RewardType.None || amount <= 0)
        {
            Debug.LogWarning("[DailyReward] No valid today reward in prefs.");
            return;
        }

        switch (type)
        {
            case RewardType.HorseFood:
                {
                    var food = (FoodInfo.HorseFood)enumValue;
                    Debug.Log($"[DailyReward] GIVE FOOD => {food} x{amount}");
                    // TODO: real add
                    // FoodInventory.Add(food, amount);
                    break;
                }
            case RewardType.PlayerSupplies:
                {
                    var res = (PlayerResourse.Resources)enumValue;
                    Debug.Log($"[DailyReward] GIVE RESOURCE => {res} x{amount}");
                    // TODO: real add
                    // PlayerResourse.Add(res, amount);
                    break;
                }
        }
    }

    private void GiveMonthlyReward()
    {
        Debug.Log("🌟 MONTHLY BIG REWARD");
        // TODO: monthly reward logic
    }

    private void RefreshDaysUI()
    {
        // 1–7
        for (int i = 0; i < first7Days.Length; i++)
        {
            var ui = first7Days[i];
            if (ui == null) continue;

            int dayIndex = i + 1; // 1..7
            bool claimed = (lastClaimedCycleDay > 0 && dayIndex <= lastClaimedCycleDay);
            bool isToday = (dayIndex == todayDayIndex && canClaimToday);

            ui.Setup(claimed, isToday, dayIndex);
        }

        // 8
        if (day8 != null)
        {
            int dayIndex = 8;
            bool claimed = (lastClaimedCycleDay > 0 && dayIndex <= lastClaimedCycleDay);
            bool isToday = (dayIndex == todayDayIndex && canClaimToday);

            day8.Setup(claimed, isToday, dayIndex);
        }
    }

    private void UpdateMonthlySliderVisual()
    {
        if (monthlySlider == null) return;

        monthlySlider.maxValue = monthCycleLength;
        monthlySlider.value = currentMonthProgress;

        if (monthlyReadyFx != null)
            monthlyReadyFx.SetActive(currentMonthProgress >= monthCycleLength);
    }


    private void LoadState()
    {
        streakDay = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_STREAK_DAY, 1);
        currentMonthProgress = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_MONTH_PROGRESS, 0);
        lastClaimedCycleDay = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_LAST_CLAIMED_CYCLE_DAY, 0);

        streakDay = Mathf.Clamp(streakDay, 1, monthCycleLength);
        todayDayIndex = ((streakDay - 1) % 8) + 1;
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_STREAK_DAY, streakDay);
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_MONTH_PROGRESS, currentMonthProgress);
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_LAST_CLAIMED_CYCLE_DAY, lastClaimedCycleDay);
        PlayerPrefs.Save();
    }
    private void OnTodayRewardPrepared(
     RewardType type,
     int amount,
     FoodInfo.HorseFood food,
     PlayerResourse.Resources resource,
     int languageId,
     Sprite icon,
     string amountText)
    {
        todayType = type;
        todayAmount = amount;
        todayFood = food;
        todayResource = resource;
        todayLanguageId = languageId;
        todayIcon = icon;
        todayAmountText = amountText;
    }

#if UNITY_EDITOR
    public void Debug_ForceNextDay()
    {
        // Oxirgi claim sanasini 1 kun oldinga surib qo‘yamiz
        DateTime fakeYesterday = DateTime.UtcNow.Date.AddDays(-1);

        PlayerPrefs.SetString(
            Constants.DailyPrizes.PREF_LAST_CLAIM_DATE,
            fakeYesterday.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        );

        PlayerPrefs.Save();

        // Qayta hisoblaymiz
        CheckNewDayAndNotify();
    }
#endif

    #region Save to prefs
    private void SaveRewardToPlayerPrefs()
    {
        switch (todayType)
        {
            case RewardType.HorseFood:
                {
                    if (todayFood == FoodInfo.HorseFood.None)
                        return;

                    string prefKey = GetHorseFoodPrefKey(todayFood);

                    int oldAmount = PlayerPrefs.GetInt(prefKey, 0);
                    int newAmount = oldAmount + todayAmount;
                    
                    PlayerPrefs.SetInt(prefKey, newAmount);
                    PlayerPrefs.Save();
                    HomeMainUI.Instance.UpdatePlayerResources(prefKey, newAmount);
                    //Debug.Log($"[DailyReward] Saved HorseFood {todayFood}: {oldAmount} + {todayAmount} = {newAmount}");
                    break;
                }

            case RewardType.PlayerSupplies:
                {
                    if (todayResource == PlayerResourse.Resources.None)
                        return;

                    string prefKey = GetPlayerResourcePrefKey(todayResource);

                    int oldAmount = PlayerPrefs.GetInt(prefKey, 0);
                    int newAmount = oldAmount + todayAmount;

                    PlayerPrefs.SetInt(prefKey, newAmount);
                    PlayerPrefs.Save();

                   // Debug.Log($"[DailyReward] Saved Resource {todayResource}: {oldAmount} + {todayAmount} = {newAmount}");
                    break;
                }

            default:
                Debug.LogWarning("[DailyReward] No reward type to save.");
                break;
        }
    }

    private string GetHorseFoodPrefKey(FoodInfo.HorseFood food)
    {
        switch (food)
        {
            case FoodInfo.HorseFood.Wheat:
                return Constants.HorseFoods.Wheat;
            case FoodInfo.HorseFood.Barley:
                return Constants.HorseFoods.Barley;
            case FoodInfo.HorseFood.Apple:
                return Constants.HorseFoods.Apple;
            case FoodInfo.HorseFood.Water:
                return Constants.HorseFoods.Water;
            case FoodInfo.HorseFood.StaminWater:
                return Constants.HorseFoods.StaminWater;
            default:
                return string.Empty;
        }
    }
    private string GetPlayerResourcePrefKey(PlayerResourse.Resources res)
    {
        switch (res)
        {
            case PlayerResourse.Resources.WalkZone:
                return Constants.PlayerItems.SlowDown;
            case PlayerResourse.Resources.Defender:
                return Constants.PlayerItems.Defense;
            case PlayerResourse.Resources.WebSnare:
                return Constants.PlayerItems.WebSnare;
            case PlayerResourse.Resources.Whiplash:
                return Constants.PlayerItems.Whip;
            case PlayerResourse.Resources.HorseDust:
                return Constants.PlayerItems.Horsedust;
            default:
                return string.Empty;
        }
    }


    #endregion

}
