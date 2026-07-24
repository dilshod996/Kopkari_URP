using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class KopkariRoundChangePopup : MonoBehaviour
{
    public enum WarmupPhase
    {
        ReachWarmupPoint,
        RoundStart
    }

    public enum DisplayReason
    {
        RoundFinished,
        WarmupNotReached
    }

    private enum RoundOutcome
    {
        None,
        Winner,
        Loser,
        TimeExpired,
        WarmupNotReached
    }

    [Header("Round Change")]
    [SerializeField] private GameObject roundChangePanel;
    [SerializeField] private TMP_Text roundChangeTitle;
    [SerializeField] private TMP_Text roundDetailsText;

    [Header("Outcome Language IDs")]
    [SerializeField] private int playerWonTitleLanguageId = -1;
    [SerializeField] private int playerLostTitleLanguageId = -1;
    [SerializeField] private int timeExpiredTitleLanguageId = -1;
    [SerializeField] private int playerWonDetailsLanguageId = -1;
    [SerializeField] private int playerLostDetailsLanguageId = -1;
    [SerializeField] private int timeExpiredDetailsLanguageId = -1;
    private const int WarmupNotReachedTitleLanguageId = 504;
    private const int WarmupNotReachedDetailsLanguageId = 621;

    [Header("Rewards")]
    [SerializeField] private GameObject rewardsBackground;
    [SerializeField] private TMP_Text rewardInfoText;
    [SerializeField] private TMP_Text nyufiyAmountText;
    [SerializeField] private TMP_Text coinAmountText;
    [SerializeField] private TMP_Text xpAmountText;
    [SerializeField] private int wonRewardInfoLanguageId = -1;
    [SerializeField] private int lostRewardInfoLanguageId = -1;

    [Header("Buttons")]
    [SerializeField] private Button nextRoundButton;
    [SerializeField] private TMP_Text nextRoundButtonText;
    [SerializeField] private int nextRoundButtonLanguageId = -1;
    [SerializeField] private Button finishHereButton;
    [SerializeField] private TMP_Text finishHereButtonText;
    [SerializeField] private int finishHereButtonLanguageId = -1;
    [SerializeField] private Button viewResultsButton;
    [SerializeField] private TMP_Text viewResultsButtonText;
    [SerializeField] private int viewResultsButtonLanguageId = -1;
    [SerializeField] private Button horseConditionButton;
    [SerializeField] private TMP_Text horseConditionButtonText;
    [SerializeField] private int horseConditionButtonLanguageId = -1;
    [SerializeField, Range(0f, 100f)] private float criticalConditionPercent = 15f;
    [SerializeField, Min(1f)] private float horseConditionPulseScale = 1.08f;
    [SerializeField, Min(0.1f)] private float horseConditionPulseDuration = 0.65f;

    public float CriticalConditionPercent => criticalConditionPercent;

    [Header("Warmup Countdown")]
    [SerializeField] private GameObject warmupBackground;
    [FormerlySerializedAs("warmupText")]
    [SerializeField] private TMP_Text warmupTimeText;
    [SerializeField] private TMP_Text warmupTitleText;
    [SerializeField] private TMP_Text warmupDetailsText;
    [SerializeField] private int reachWarmupTitleLanguageId = -1;
    [SerializeField] private int reachWarmupDetailsLanguageId = -1;
    [SerializeField] private int roundStartTitleLanguageId = -1;
    [SerializeField] private int roundStartDetailsLanguageId = -1;

    public RectTransform WarmupTutorialTarget =>
        warmupBackground != null ? warmupBackground.transform as RectTransform : null;

    private void Awake()
    {
        HideAll();
    }

    private void OnEnable()
    {
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(HandleNextRoundClicked);
        if (finishHereButton != null)
            finishHereButton.onClick.AddListener(HandleFinishHereClicked);
        if (viewResultsButton != null)
            viewResultsButton.onClick.AddListener(HandleViewResultsClicked);
        if (horseConditionButton != null)
            horseConditionButton.onClick.AddListener(HandleHorseConditionClicked);
    }

    private void OnDisable()
    {
        if (nextRoundButton != null)
            nextRoundButton.onClick.RemoveListener(HandleNextRoundClicked);
        if (finishHereButton != null)
            finishHereButton.onClick.RemoveListener(HandleFinishHereClicked);
        if (viewResultsButton != null)
            viewResultsButton.onClick.RemoveListener(HandleViewResultsClicked);
        if (horseConditionButton != null)
            horseConditionButton.onClick.RemoveListener(HandleHorseConditionClicked);
        StopHorseConditionPulse();
    }

    public void ShowRoundChange(bool canStartNextRound)
    {
        ShowRoundChange(canStartNextRound, DisplayReason.RoundFinished);
    }

    public void ShowRoundChange(bool canStartNextRound, string unusedDetails)
    {
        ShowRoundChange(canStartNextRound, DisplayReason.RoundFinished);
    }

    public void ShowRoundChange(bool canStartNextRound, DisplayReason reason)
    {
        HideWarmupCountdown();
        SetRoundChangeVisible(true);

        RoundOutcome outcome = ResolveRoundOutcome(reason);
        ApplyOutcomeText(outcome);
        ApplyButtonLabels();

        SetButtonState(nextRoundButton, canStartNextRound);
        SetButtonState(finishHereButton, canStartNextRound);
        SetButtonState(viewResultsButton, !canStartNextRound);
        SetButtonState(horseConditionButton, false);
        ShowRewardInformation(outcome, canStartNextRound);
    }

    public void HideRoundChange()
    {
        SetRoundChangeVisible(false);
        ClearText(roundChangeTitle);
        ClearText(roundDetailsText);
        HideRewardInformation();
        SetButtonState(nextRoundButton, false);
        SetButtonState(finishHereButton, false);
        SetButtonState(viewResultsButton, false);
        SetButtonState(horseConditionButton, false);
        StopHorseConditionPulse();
    }

    public void ShowWarmupCountdown(int seconds)
    {
        ShowWarmupCountdown(seconds, WarmupPhase.ReachWarmupPoint);
    }

    public void ShowWarmupCountdown(int seconds, WarmupPhase phase)
    {
        HideRoundChange();

        if (warmupBackground != null)
            warmupBackground.SetActive(true);
        if (warmupTimeText != null)
            warmupTimeText.text = Mathf.Max(0, seconds).ToString();

        bool isRoundStart = phase == WarmupPhase.RoundStart;
        SetLocalizedText(
            warmupTitleText,
            isRoundStart ? roundStartTitleLanguageId : reachWarmupTitleLanguageId);
        SetLocalizedText(
            warmupDetailsText,
            isRoundStart ? roundStartDetailsLanguageId : reachWarmupDetailsLanguageId);
    }

    public void HideWarmupCountdown()
    {
        if (warmupBackground != null)
            warmupBackground.SetActive(false);
        ClearText(warmupTimeText);
        ClearText(warmupTitleText);
        ClearText(warmupDetailsText);
    }

    public void HideAll()
    {
        HideRoundChange();
        HideWarmupCountdown();
    }

    private RoundOutcome ResolveRoundOutcome(DisplayReason reason)
    {
        if (reason == DisplayReason.WarmupNotReached)
            return RoundOutcome.WarmupNotReached;

        KopkariResultsManager results = KopkariResultsManager.Instance;
        if (results == null)
            return RoundOutcome.None;
        if (results.WinnerId == KopkariResultsManager.NoWinnerId)
            return RoundOutcome.TimeExpired;

        RiderRaceStats winner = results.Get(results.WinnerId);
        return winner != null && winner.isPlayer ? RoundOutcome.Winner : RoundOutcome.Loser;
    }

    private void ApplyOutcomeText(RoundOutcome outcome)
    {
        int titleId = -1;
        int detailsId = -1;

        switch (outcome)
        {
            case RoundOutcome.Winner:
                titleId = playerWonTitleLanguageId;
                detailsId = playerWonDetailsLanguageId;
                break;
            case RoundOutcome.Loser:
                titleId = playerLostTitleLanguageId;
                detailsId = playerLostDetailsLanguageId;
                break;
            case RoundOutcome.TimeExpired:
                titleId = timeExpiredTitleLanguageId;
                detailsId = timeExpiredDetailsLanguageId;
                break;
            case RoundOutcome.WarmupNotReached:
                titleId = WarmupNotReachedTitleLanguageId;
                detailsId = WarmupNotReachedDetailsLanguageId;
                break;
        }

        SetLocalizedText(roundChangeTitle, titleId);
        SetLocalizedText(roundDetailsText, detailsId);
    }

    private void ShowRewardInformation(RoundOutcome outcome, bool hasNextRound)
    {
        if (!hasNextRound)
        {
            HideRewardInformation();
            return;
        }

        int coinAmount = 0;
        int nyufiyAmount = 0;
        int xpAmount = 0;
        int rewardInfoLanguageId = lostRewardInfoLanguageId;

        if (outcome == RoundOutcome.Winner)
        {
            RiderRoundStats latestRound = KopkariResultsManager.Instance?.GetLatestPlayerRound();
            if (latestRound != null)
            {
                coinAmount = latestRound.coinPrize;
                nyufiyAmount = latestRound.nyufiyPrize;
                xpAmount = latestRound.xpPrize;
            }
            rewardInfoLanguageId = wonRewardInfoLanguageId;
        }
        else
        {
            KopkariManager manager = KopkariManager.Instance;
            if (manager != null)
            {
                coinAmount = manager.CurrentRoundCoinAmount;
                nyufiyAmount = manager.CurrentRoundNyufiyAmount;
                xpAmount = manager.CurrentRoundXpAmount;
            }
        }

        if (rewardsBackground != null)
            rewardsBackground.SetActive(true);
        SetLocalizedText(rewardInfoText, rewardInfoLanguageId);
        SetAmountText(coinAmountText, coinAmount);
        SetAmountText(nyufiyAmountText, nyufiyAmount);
        SetAmountText(xpAmountText, xpAmount);
    }

    private void HideRewardInformation()
    {
        if (rewardsBackground != null)
            rewardsBackground.SetActive(false);
        ClearText(rewardInfoText);
        ClearText(coinAmountText);
        ClearText(nyufiyAmountText);
        ClearText(xpAmountText);
    }

    private void ApplyButtonLabels()
    {
        SetLocalizedText(nextRoundButtonText, nextRoundButtonLanguageId);
        SetLocalizedText(finishHereButtonText, finishHereButtonLanguageId);
        SetLocalizedText(viewResultsButtonText, viewResultsButtonLanguageId);
        SetLocalizedText(horseConditionButtonText, horseConditionButtonLanguageId);
    }

    private void HandleNextRoundClicked()
    {
        if (horseConditionButton != null && IsHorseConditionCritical())
        {
            SetButtonState(horseConditionButton, true);
            StartHorseConditionPulse();
            return;
        }

        HideRoundChange();
        KopkariManager.Instance?.BeginNextRoundWarmup();
    }

    private void HandleHorseConditionClicked()
    {
        StopHorseConditionPulse();
        KopkariMainUI.Instance?.ShowRoundFoodPanel();
    }

    public void RefreshHorseConditionAttention()
    {
        if (horseConditionButton == null)
            return;

        bool isCritical = IsHorseConditionCritical();
        SetButtonState(horseConditionButton, isCritical);
        if (isCritical)
            StartHorseConditionPulse();
        else
            StopHorseConditionPulse();
    }

    private void HandleFinishHereClicked()
    {
        HideAll();
        KopkariMainUI.Instance?.ShowResult();
    }

    private void HandleViewResultsClicked()
    {
        HideAll();
        KopkariMainUI.Instance?.ShowResult();
    }

    private bool IsHorseConditionCritical()
    {
        HorseConditionStats max = HorseConditionStatsService.GetCachedMaxOrDefault();
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(max);
        return GetPercent(current.Power, max.Power) < criticalConditionPercent ||
               GetPercent(current.Cooling, max.Cooling) < criticalConditionPercent ||
               GetPercent(current.Stamina, max.Stamina) < criticalConditionPercent;
    }

    private static float GetPercent(float current, float maximum)
    {
        return maximum > 0f ? Mathf.Clamp01(current / maximum) * 100f : 0f;
    }

    private void StartHorseConditionPulse()
    {
        if (horseConditionButton == null)
            return;

        GameObject target = horseConditionButton.gameObject;
        LeanTween.cancel(target);
        target.transform.localScale = Vector3.one;
        LeanTween.scale(target, Vector3.one * horseConditionPulseScale, horseConditionPulseDuration)
            .setIgnoreTimeScale(true)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
    }

    private void StopHorseConditionPulse()
    {
        if (horseConditionButton == null)
            return;

        LeanTween.cancel(horseConditionButton.gameObject);
        horseConditionButton.transform.localScale = Vector3.one;
    }

    private void SetRoundChangeVisible(bool visible)
    {
        if (roundChangePanel != null)
            roundChangePanel.SetActive(visible);
    }

    private static void SetLocalizedText(TMP_Text text, int languageId)
    {
        if (text == null)
            return;

        text.text = languageId >= 0 && LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(languageId)
            : string.Empty;
    }

    private static void SetAmountText(TMP_Text text, int amount)
    {
        if (text != null)
            text.text = Mathf.Max(0, amount).ToString();
    }

    private static void ClearText(TMP_Text text)
    {
        if (text != null)
            text.text = string.Empty;
    }

    private static void SetButtonState(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = visible;
    }
}
