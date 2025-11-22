using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardDayUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject blockPanel;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardAmount;
    [SerializeField] private TMP_Text dayText;

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
    public void Setup(bool claimed, bool isToday)
    {
        this.isClaimed = claimed;
        this.isToday = isToday;

        bool shouldBlock = !(claimed || isToday);   // faqat future kunlar bloklanadi
        if (blockPanel != null)
            blockPanel.SetActive(shouldBlock);
        if (isToday)
        {
            Sprite icon = rewardIcon != null ? rewardIcon.sprite : null;
            string amount = rewardAmount != null ? rewardAmount.text : "";
            Debug.Log(amount);
            HomeMainUI.Instance.CacheTodayReward(icon, amount);
        }
    }
}
