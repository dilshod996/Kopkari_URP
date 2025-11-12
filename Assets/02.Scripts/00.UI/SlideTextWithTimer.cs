using System.Collections;
using TMPro;
using UnityEngine;

public class SlideTextWithTimer : MonoBehaviour
{
    public RectTransform panel;
    public TMP_Text timerText;

    public float startY = 100f;
    public float endY = -50f;
    public float slideDuration = 0.5f;

    private Coroutine slideCoroutine;

    // holdTime parametr qabul qiladi
    public void StartSlide(float holdTime)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideAndHold(holdTime));
    }

    private IEnumerator SlideAndHold(float holdTime)
    {
        // Y pozitsiyani boshlang¡®ichga qo¡®yish
        SetY(startY);

        // Pastga tushirish
        yield return SlideTo(endY, slideDuration);

        // Timer -1 sekunddan hisoblaydi
        int timeLeft = Mathf.CeilToInt(holdTime);
        while (timeLeft > 0)
        {
            timerText.text = timeLeft.ToString("D2");
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
        timerText.text = "00";
        if (BaseManager.Instance != null) BaseManager.Instance.TriggerEvent();

        // Yana yuqoriga ko¡®tarish
        yield return SlideTo(startY, slideDuration);
    }
    public void ForceHide()
    {
        StartCoroutine(SlideTo(startY, 0));
    }
    private void SetY(float y)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.y = y;
        panel.anchoredPosition = pos;
    }

    private IEnumerator SlideTo(float targetY, float duration)
    {
        float startY = panel.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float newY = Mathf.Lerp(startY, targetY, t);
            SetY(newY);
            yield return null;
        }

        SetY(targetY);
    }
}
