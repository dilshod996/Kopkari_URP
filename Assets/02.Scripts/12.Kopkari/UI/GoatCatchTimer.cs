using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoatCatchTimer : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Timer paneli (butun obyekt). Bo'sh qoldirsang = shu GameObject")]
    public GameObject timerRoot;

    public Slider timeSlider;
    public TMP_Text timerText;

    [Header("Timer Settings")]
    public float maxTime = 30f;      // 30 sekund
    private float currentTime;

    private Coroutine countdownCoroutine;

    private void Awake()
    {
        if (timerRoot == null)
            timerRoot = this.gameObject;

        // Boshlanishida yashirin tursin
        if (timerRoot != null)
            timerRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // Local player uloq oldi/yo‘qotdi (bool hasGoat)
        BaseManager.OnGoatPicked += HandleLocalGoatState;
        // Masalan raund qayta start bo‘lganda majburan yopish
        BaseManager.OnGameStarted += ForceHide;
    }

    private void OnDisable()
    {
        BaseManager.OnGoatPicked -= HandleLocalGoatState;
        BaseManager.OnGameStarted -= ForceHide;

        StopCountdown();
    }

    // ==================== MAIN LOGIC ====================

    private void HandleLocalGoatState(bool hasGoat)
    {
        if (hasGoat)
        {
            StartTimer();
        }
        else
        {
            // Uloq ketdi – timerni to‘xtatib, panelni yopamiz
            ForceHide();
        }
    }

    /// <summary>
    /// Uloq olinganda timer boshlangan holat
    /// </summary>
    private void StartTimer()
    {
        if (timerRoot != null)
            timerRoot.SetActive(true);

        StopCountdown();

        currentTime = maxTime;

        if (timeSlider != null)
        {
            timeSlider.maxValue = maxTime;
            timeSlider.value = maxTime;
        }

        UpdateTimerText();

        countdownCoroutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (currentTime > 0f)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0f)
                currentTime = 0f;

            if (timeSlider != null)
                timeSlider.value = currentTime;

            UpdateTimerText();

            yield return null;
        }

        // Vaqt tugadi → panelni o‘chiramiz
        if (timerRoot != null)
            timerRoot.SetActive(false);

        countdownCoroutine = null;
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int t = Mathf.CeilToInt(currentTime);
        timerText.text = t.ToString(); // 30, 29, ... 01, 00
    }

    /// <summary>
    /// Tashqaridan majburiy yopish (round tugadi, finish va hokazo)
    /// </summary>
    public void ForceHide()
    {
        StopCountdown();

        if (timerRoot != null)
            timerRoot.SetActive(false);
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }
}
