using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KopkariRoundChangePopup : MonoBehaviour
{
    private enum RoundOutcome
    {
        None,
        Winner,
        Loser,
        TimeExpired
    }

    [Header("Round Outcome")]
    [SerializeField] private GameObject winnerBackground;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private GameObject loserBackground;
    [SerializeField] private TMP_Text loserText;
    [SerializeField, Min(0f)] private float outcomeDisplayDuration = 1.5f;
    [SerializeField] private string winnerMessage = "Winner";
    [SerializeField] private string loserMessage = "Lost";
    [SerializeField] private string timeExpiredMessage = "Lost\nTime expired";

    [Header("Round Change")]
    [SerializeField] private GameObject roundChangePanel;
    [SerializeField] private TMP_Text roundDetailsText;
    [SerializeField] private Button nextRoundButton;
    [SerializeField] private Button finishHereButton;

    [Header("Warmup Countdown")]
    [SerializeField] private GameObject warmupBackground;
    [SerializeField] private TMP_Text warmupText;

    private Coroutine outcomeRoutine;
    private bool outcomeTransitionPending;
    private float outcomeTransitionEndTime;
    private bool pendingCanStartNextRound;
    private string pendingRoundDetails;

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

        ResumeOutcomeRoutineIfNeeded();
    }

    private void OnDisable()
    {
        StopOutcomeRoutine(false);

        if (nextRoundButton != null)
            nextRoundButton.onClick.RemoveListener(HandleNextRoundClicked);
        if (finishHereButton != null)
            finishHereButton.onClick.RemoveListener(HandleFinishHereClicked);
    }

    public void ShowRoundChange(bool canStartNextRound)
    {
        ShowRoundChange(canStartNextRound, null);
    }

    public void ShowRoundChange(bool canStartNextRound, string details)
    {
        StopOutcomeRoutine();
        HideWarmupCountdown();
        SetRoundChangeVisible(false);
        HideRoundOutcome();

        RoundOutcome outcome = ResolveRoundOutcome(details);
        if (!ShowRoundOutcome(outcome))
        {
            ShowRoundChangePanel(canStartNextRound, details);
            return;
        }

        outcomeTransitionPending = true;
        outcomeTransitionEndTime = Time.realtimeSinceStartup + outcomeDisplayDuration;
        pendingCanStartNextRound = canStartNextRound;
        pendingRoundDetails = details;
        ResumeOutcomeRoutineIfNeeded();
    }

    private IEnumerator ShowRoundChangeAfterOutcome()
    {
        while (Time.realtimeSinceStartup < outcomeTransitionEndTime)
            yield return null;

        outcomeRoutine = null;
        outcomeTransitionPending = false;
        HideRoundOutcome();
        ShowRoundChangePanel(pendingCanStartNextRound, pendingRoundDetails);
        pendingRoundDetails = null;
    }

    private void ShowRoundChangePanel(bool canStartNextRound, string details)
    {
        SetRoundChangeVisible(true);

        if (roundDetailsText != null)
            roundDetailsText.text = details ?? string.Empty;

        SetButtonState(nextRoundButton, canStartNextRound);
        SetButtonState(finishHereButton, true);
    }

    public void HideRoundChange()
    {
        StopOutcomeRoutine();
        HideRoundOutcome();
        SetRoundChangeVisible(false);
        if (roundDetailsText != null)
            roundDetailsText.text = string.Empty;

        SetButtonState(nextRoundButton, false);
        SetButtonState(finishHereButton, false);
    }

    public void ShowWarmupCountdown(int seconds)
    {
        HideRoundChange();

        if (warmupBackground != null)
            warmupBackground.SetActive(true);
        if (warmupText != null)
            warmupText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideWarmupCountdown()
    {
        if (warmupBackground != null)
            warmupBackground.SetActive(false);
        if (warmupText != null)
            warmupText.text = string.Empty;
    }

    public void HideAll()
    {
        StopOutcomeRoutine();
        HideRoundOutcome();
        HideRoundChange();
        HideWarmupCountdown();
    }

    private RoundOutcome ResolveRoundOutcome(string details)
    {
        if (!string.IsNullOrEmpty(details) &&
            details.IndexOf("time finished", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return RoundOutcome.TimeExpired;
        }

        KopkariResultsManager results = KopkariResultsManager.Instance;
        if (results == null)
            return RoundOutcome.None;
        if (results.WinnerId == KopkariResultsManager.NoWinnerId)
            return RoundOutcome.TimeExpired;

        RiderRaceStats winner = results.Get(results.WinnerId);
        if (winner == null)
            return RoundOutcome.Loser;

        return winner.isPlayer ? RoundOutcome.Winner : RoundOutcome.Loser;
    }

    private bool ShowRoundOutcome(RoundOutcome outcome)
    {
        switch (outcome)
        {
            case RoundOutcome.Winner:
                if (winnerText != null)
                    winnerText.text = winnerMessage;
                if (winnerBackground != null)
                    winnerBackground.SetActive(true);
                return winnerBackground != null;

            case RoundOutcome.Loser:
                if (loserText != null)
                    loserText.text = loserMessage;
                if (loserBackground != null)
                    loserBackground.SetActive(true);
                return loserBackground != null;

            case RoundOutcome.TimeExpired:
                if (loserText != null)
                    loserText.text = timeExpiredMessage;
                if (loserBackground != null)
                    loserBackground.SetActive(true);
                return loserBackground != null;

            default:
                return false;
        }
    }

    private void HideRoundOutcome()
    {
        if (winnerBackground != null)
            winnerBackground.SetActive(false);
        if (loserBackground != null)
            loserBackground.SetActive(false);
    }

    private void SetRoundChangeVisible(bool visible)
    {
        if (roundChangePanel != null)
            roundChangePanel.SetActive(visible);
    }

    private void ResumeOutcomeRoutineIfNeeded()
    {
        if (!outcomeTransitionPending || outcomeRoutine != null || !isActiveAndEnabled)
            return;

        outcomeRoutine = StartCoroutine(ShowRoundChangeAfterOutcome());
    }

    private void StopOutcomeRoutine(bool clearPending = true)
    {
        if (outcomeRoutine != null)
        {
            StopCoroutine(outcomeRoutine);
            outcomeRoutine = null;
        }

        if (!clearPending)
            return;

        outcomeTransitionPending = false;
        pendingRoundDetails = null;
    }

    private void HandleNextRoundClicked()
    {
        HideRoundChange();
        KopkariManager.Instance?.BeginNextRoundWarmup();
    }

    private void HandleFinishHereClicked()
    {
        HideAll();
        KopkariMainUI.Instance?.ShowResult();
    }

    private static void SetButtonState(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = visible;
    }
}
