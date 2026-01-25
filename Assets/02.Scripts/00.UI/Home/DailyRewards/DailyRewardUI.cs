using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    [Header("1–7 kunlik itemlar")]
    [SerializeField] private RewardDayUI[] first7Days;   // size = 7

    [Header("8-kun BIG prize item")]
    [SerializeField] private RewardDayUI day8;

    [Header("Monthly Slider")]
    [SerializeField] private Slider monthlySlider;
    [SerializeField] private GameObject monthlyReadyFx;

    private HomeMainUI Core => HomeMainUI.Instance;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text claimText;
    [SerializeField] private Button claimButton;

    private void OnEnable()
    {
        if(LanguageManager.Instance!=null)
        {
            titleText.text = LanguageManager.Instance.GetText(394); 
            claimText.text = LanguageManager.Instance.GetText(392);
        }
        if (Core != null)
        {
            Core.OnClaimCompleted += OnClaimCompleted;
            Core.OnMonthlyRewardReady += OnMonthlyRewardReady;
        }

        RefreshDaysUI();
        UpdateMonthlySliderVisual();
        claimButton.onClick.AddListener(OnClickClaim);

    }

    private void OnDisable()
    {
        if (Core != null)
        {
            Core.OnClaimCompleted -= OnClaimCompleted;
            Core.OnMonthlyRewardReady -= OnMonthlyRewardReady;
        }
        claimButton.onClick.RemoveListener(OnClickClaim);
    }

    private void RefreshDaysUI()
    {
        if (Core == null) return;

        int lastClaimed = Core.LastClaimedDay;   // 0 yoki 1..8
        int todayIndex = Core.TodayDayIndex;    // 1..8

        // 1–7 kun
        for (int i = 0; i < first7Days.Length; i++)
        {
            var ui = first7Days[i];
            if (ui == null) continue;

            int dayIndex = i + 1; // 1..7
            bool claimed = (lastClaimed > 0 && dayIndex <= lastClaimed);
            bool isToday = (dayIndex == todayIndex && Core.CanClaimToday);

            ui.Setup(claimed, isToday, dayIndex);
        }

        // 8-kun
        if (day8 != null)
        {
            int dayIndex = 8;
            bool claimed = (lastClaimed > 0 && dayIndex <= lastClaimed);
            bool isToday = (dayIndex == todayIndex && Core.CanClaimToday);

            day8.Setup(claimed, isToday, dayIndex);
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

    public void OnClickClaim()
    {
        if (Core == null) return;
        Core.ClaimToday();
    }

    private void OnClaimCompleted(int dayIndex)
    {
        RefreshDaysUI();
        UpdateMonthlySliderVisual();
        gameObject.SetActive(false);
    }

    private void OnMonthlyRewardReady()
    {
        Debug.Log("Monthly to‘ldi – katta sovga UIda ko‘rsat!");
    }
}
