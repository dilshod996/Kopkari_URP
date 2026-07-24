using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ComboPrize : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text prizeAmountText;

    private Coroutine countdownRoutine;
    private float remainingTime;
    private bool comboActive;
    private bool showInProgress;

    public bool IsActive => comboActive && remainingTime > 0f;
    public int PrizeAmount { get; private set; }

    private void Awake()
    {
        // An inactive scene object receives Awake only when Show first activates
        // it. Do not let that first activation immediately hide itself.
        if (!showInProgress)
            Hide();
    }

    private void OnDisable()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        comboActive = false;
        remainingTime = 0f;
    }

    public void Show(float duration, int prizeAmount)
    {
        if (duration <= 0f || prizeAmount <= 0)
        {
            Hide();
            return;
        }

        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        showInProgress = true;
        remainingTime = duration;
        PrizeAmount = Mathf.Max(0, prizeAmount);
        comboActive = true;
        gameObject.SetActive(true);
        showInProgress = false;
        UpdateTexts();
        countdownRoutine = StartCoroutine(Countdown());
    }

    public bool TryComplete()
    {
        bool completedInTime = IsActive;
        Hide();
        return completedInTime;
    }

    public void Hide()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        comboActive = false;
        remainingTime = 0f;
        gameObject.SetActive(false);
    }

    private IEnumerator Countdown()
    {
        while (remainingTime > 0f)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            UpdateTexts();
            yield return null;
        }

        countdownRoutine = null;
        Hide();
    }

    private void UpdateTexts()
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timeText != null)
            timeText.text = $"{minutes:00}:{seconds:00}";
        if (prizeAmountText != null)
            prizeAmountText.text = $"+{PrizeAmount:N0}";
    }
}
