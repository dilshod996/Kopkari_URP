using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardDayUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private GameObject openPanel;

    //Open panel inside details
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardAmount;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text dayText2;

    private bool isClaimed;
    private bool isToday;

    /// <summary>
    /// claimed  = oldin olingan kun
    /// isToday  = bugungi kun (hozir claim qilish mumkin bo¡®lgan)
    /// 
    /// blockPanel:
    ///  - claimed  true  -> blok yo¡®q     (ochiq)
    ///  - isToday true   -> blok yo¡®q     (ochiq)
    ///  - ikkalasi ham false -> blok BOR (kelajak kun)
    /// </summary>
    public void Setup(bool claimed, bool isToday, int dayNumber)
    {
        this.isClaimed = claimed;
        this.isToday = isToday;

        if (dayNumber <= 7)
            dayText.text = dayText2.text = $"{LanguageManager.Instance?.GetText(391)} {dayNumber}";
        else
            dayText.text = dayText2.text = LanguageManager.Instance.GetText(392); // xohlasang LanguageManager key qo¡®shamiz

        bool shouldBlock = !(claimed || isToday);

        if (blockPanel != null) blockPanel.SetActive(shouldBlock);
        if (openPanel != null) openPanel.SetActive(!shouldBlock);

        if (isToday)
        {
            Sprite icon = rewardIcon != null ? rewardIcon.sprite : null;
            string amount = rewardAmount != null ? rewardAmount.text : "";
            HomeMainUI.Instance.CacheReward(icon,LanguageManager.Instance.GetText(408), amount, null);
        }
    }

}
