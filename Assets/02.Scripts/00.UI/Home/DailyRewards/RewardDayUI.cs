using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardDayUI : MonoBehaviour
{
    [Header("Reward Data (only one will be not-None)")]
    public CurrencyRewardType currencyReward = CurrencyRewardType.None;
    public PlayerResourse.Resources playerSupplies = PlayerResourse.Resources.None;

    [Header("UI Refs")]
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private GameObject openPanel;

    [Header("Open panel inside details")]
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardAmount;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text dayText2;

    [Header("Amount")]
    [SerializeField] private int amountPrize = 1;

    private bool isClaimed;
    private bool isToday;
    public static event Action<RewardType, int, CurrencyRewardType, PlayerResourse.Resources, int, Sprite, string> OnTodayRewardPrepared;

    /// <summary>
    /// claimed  = oldin olingan kun
    /// isToday  = bugungi kun (hozir claim qilish mumkin bo‘lgan)
    ///
    /// blockPanel:
    ///  - claimed  true  -> blok yo‘q (ochiq)
    ///  - isToday true   -> blok yo‘q (ochiq)
    ///  - ikkalasi ham false -> blok BOR (kelajak kun)
    /// </summary>
    public void Setup(bool claimed, bool isToday, int dayNumber)
    {
        this.isClaimed = claimed;
        this.isToday = isToday;

        // Day text
        if (LanguageManager.Instance != null)
        {
            dayText.text = dayText2.text = $"{LanguageManager.Instance.GetText(391)} {dayNumber}";
        }
        else
        {
            dayText.text = dayText2.text = $"Day {dayNumber}";
        }

        // Amount text must be +X
        if (rewardAmount != null)
            rewardAmount.text = $"+{amountPrize}";

        // Claimed or Today => OPEN, future => BLOCK
        bool shouldBlock = !(claimed || isToday);
        if (blockPanel != null) blockPanel.SetActive(shouldBlock);
        if (openPanel != null) openPanel.SetActive(!shouldBlock);

        // If today -> cache popup + save reward info to prefs
        if (isToday)
        {
            int languageId;
            RewardType type = GetRewardType(out languageId);
            Debug.Log("today");
            Sprite icon = rewardIcon != null ? rewardIcon.sprite : null;
            string amountText = rewardAmount != null ? rewardAmount.text : $"+{amountPrize}";

            // ✅ save today reward (DailyRewardUI claim bosganda shundan o‘qiydi)
            SaveTodayRewardToPrefs(type, languageId, amountPrize);
            Debug.Log($"[RewardDayUI] Invoke today event. Subscribers? {(OnTodayRewardPrepared == null ? "NO" : "YES")}", this);

            OnTodayRewardPrepared?.Invoke(
                    type,
                    amountPrize,
                    currencyReward,
                    playerSupplies,
                    languageId,
                    icon,
                    amountText
                );
        }
    }

    private void SaveTodayRewardToPrefs(RewardType type, int languageId, int amount)
    {
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_TYPE, (int)type);
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_LANG_ID, languageId);
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_AMOUNT, amount);

        // enum value
        int enumValue = 0;
        if (type == RewardType.Currency) enumValue = (int)currencyReward;
        else if (type == RewardType.PlayerSupplies) enumValue = (int)playerSupplies;

        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_TODAY_REWARD_ENUM, enumValue);
        PlayerPrefs.Save();
    }

    // ✅ rewardNameKeyOrText emas, languageId qaytaradi
    private RewardType GetRewardType(out int languageId)
    {
        languageId = 0;

        // 1) Currency bo'lsa
        if (currencyReward != CurrencyRewardType.None)
        {
            switch (currencyReward)
            {
                case CurrencyRewardType.Nyufiy: languageId = 409; break;
                case CurrencyRewardType.Coin: languageId = 390; break;
                default: languageId = 0; break;
            }
            return RewardType.Currency;
        }

        // 2) PlayerSupplies bo'lsa
        if (playerSupplies != PlayerResourse.Resources.None)
        {
            switch (playerSupplies)
            {
                case PlayerResourse.Resources.WalkZone: languageId = 323; break;
                case PlayerResourse.Resources.Defender: languageId = 324; break;
                case PlayerResourse.Resources.WebSnare: languageId = 322; break;
                case PlayerResourse.Resources.Whiplash: languageId = 384; break;
                case PlayerResourse.Resources.HorseDust: languageId = 387; break;
                default: languageId = 0; break;
            }
            return RewardType.PlayerSupplies;
        }

        return RewardType.None;
    }
}

public enum RewardType
{
    None = 0,
    Currency = 1,
    PlayerSupplies = 2
}

public enum CurrencyRewardType
{
    None = 0,
    Nyufiy = 1,
    Coin = 2
}
