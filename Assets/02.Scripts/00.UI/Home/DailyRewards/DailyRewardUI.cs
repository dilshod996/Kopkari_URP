using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    [Header("1–6 kunlik itemlar")]
    [SerializeField] private RewardDayUI[] first6Days;   // size = 6

    [Header("7-kun item")]
    [SerializeField] private RewardDayUI day7;

    [Header("Monthly Slider")]
    [SerializeField] private Slider monthlySlider;
    [SerializeField] private GameObject monthlyReadyFx; // optional

    private HomeMainUI Core => HomeMainUI.Instance;

    private void OnEnable()
    {
        if (Core != null)
        {
            Core.OnClaimCompleted += OnClaimCompleted;
            Core.OnMonthlyRewardReady += OnMonthlyRewardReady;
        }

        RefreshDaysUI();
        UpdateMonthlySliderVisual();
    }

    private void OnDisable()
    {
        if (Core != null)
        {
            Core.OnClaimCompleted -= OnClaimCompleted;
            Core.OnMonthlyRewardReady -= OnMonthlyRewardReady;
        }
    }

    /// <summary>
    /// 1–7 kun UI larini yangilash (faqat claimed/unclaimed bo‘yicha)
    /// </summary>
    private void RefreshDaysUI()
    {
        if (Core == null) return;

        int lastClaimed = Core.LastClaimedDay;   // masalan: 0 yoki 1..7
        int todayIndex = Core.TodayDayIndex;    // bugungi kun indeksi 1..7

        // 1–6 kun
        for (int i = 0; i < first6Days.Length; i++)
        {
            var ui = first6Days[i];
            if (ui == null) continue;

            int dayIndex = i + 1; // 1..6

            bool claimed = (lastClaimed > 0 && dayIndex <= lastClaimed);
            bool isToday = (dayIndex == todayIndex && Core.CanClaimToday);

            ui.Setup(claimed, isToday);
        }

        // 7-kun
        if (day7 != null)
        {
            int dayIndex = 7;
            bool claimed = (lastClaimed > 0 && dayIndex <= lastClaimed);
            bool isToday = (dayIndex == todayIndex && Core.CanClaimToday);

            day7.Setup(claimed, isToday);
        }
    }


    private void UpdateMonthlySliderVisual()
    {
        if (Core == null || monthlySlider == null) return;

        monthlySlider.maxValue = Core.MonthCycleLength;
        monthlySlider.value = Core.CurrentMonthProgress;

        if (monthlyReadyFx != null)
            monthlyReadyFx.SetActive(Core.CurrentMonthProgress >= Core.MonthCycleLength);
    }

    // Claim tugmasi UI dan shu methodga ulangan bo‘ladi
    public void OnClickClaim()
    {
        if (Core == null) return;
        Core.ClaimToday();
    }

    private void OnClaimCompleted(int dayIndex)
    {
        RefreshDaysUI();
        UpdateMonthlySliderVisual();

        // Claim tugagach, bu UI’ni yopamiz
        gameObject.SetActive(false);
    }

    private void OnMonthlyRewardReady()
    {
        // Xohlasang alohida popup yoki efekt
        Debug.Log("Monthly to‘ldi – katta sovga UIda ko‘rsat!");
    }
}
