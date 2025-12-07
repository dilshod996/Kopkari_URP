using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GoatCatchTimer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform panel;
    public TMP_Text timerText;

    [Header("Slide Settings")]
    public float startY = 100f;   // yashirin holatdagi Y
    public float endY = -50f;     // ko‘ringan holatdagi Y
    public float slideDuration = 0.5f;

    private Coroutine slideCoroutine;

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // ⬇️ BaseManager vaqtdan xabar beradi: t (sekund)
        BaseManager.OnGoatPickedTime += OnTimeChanged;
        // ⬇️ Uloq mahalliy playerga berildi / ketdi
        BaseManager.OnGoatPicked += HandleLocalGoatState;
        // ⬇️ Majburan yopish kerak bo‘lganda (finish, round tugashi va hokazo)
        BaseManager.OnHideCatchTime += ForceHide;
    }

    private void OnDisable()
    {
        BaseManager.OnGoatPickedTime -= OnTimeChanged;
        BaseManager.OnGoatPicked -= HandleLocalGoatState;
        BaseManager.OnHideCatchTime -= ForceHide;

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
    }

    /// <summary>
    /// BaseManager’dan har sekund keladigan vaqt (t) ni UI’da yangilash
    /// </summary>
    private void OnTimeChanged(float timeLeft)
    {
        // t manfiy bo‘lib ketsa ham 0 dan pastga tushmasin
        int t = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        if (timerText != null)
            timerText.text = t.ToString("D2");

        // Vaqt tugaganda UI ni sekin yopish (TriggerEvent BaseManager ichida)
        if (timeLeft <= 0f)
        {
            StartSlideOut();
        }
    }

    /// <summary>
    /// Local player uloqni oldi yoki yo‘qotdi
    /// </summary>
    private void HandleLocalGoatState(bool hasGoat)
    {
        if (hasGoat)
        {
            // Uloq bizga o‘tganda panelni chiqaramiz
            StartSlideIn();
        }
        else
        {
            // Uloq bizdan chiqqanda panelni yopamiz
            StartSlideOut();
        }
    }

    /// <summary>
    /// Tashqaridan majburiy yashirish (masalan, raund tugadi)
    /// </summary>
    public void ForceHide()
    {
        StartSlideOut(true);
    }

    // ================= SLIDE FUNKSIYALAR =================

    private void SetY(float y)
    {
        if (panel == null) return;

        Vector2 pos = panel.anchoredPosition;
        pos.y = y;
        panel.anchoredPosition = pos;
    }

    public void StartSlideIn()
    {
        StartSlide(endY, slideDuration);
    }

    public void StartSlideOut(bool instant = false)
    {
        if (instant)
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }

            SetY(startY);
        }
        else
        {
            StartSlide(startY, slideDuration);
        }
    }

    private void StartSlide(float targetY, float duration)
    {
        if (panel == null) return;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideTo(targetY, duration));
    }

    private IEnumerator SlideTo(float targetY, float duration)
    {
        float initialY = panel.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float newY = Mathf.Lerp(initialY, targetY, t);
            SetY(newY);
            yield return null;
        }

        SetY(targetY);
        slideCoroutine = null;
    }
}
