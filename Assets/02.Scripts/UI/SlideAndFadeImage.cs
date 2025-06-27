using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SlideAndFadeImage : MonoBehaviour
{
    public float destinationX = 500f;
    public float slideDuration = 1f;
    public float holdDuration = 1f;

    public TMP_Text countdownText; // TextMeshPro Text

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private float startX;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        startX = rectTransform.anchoredPosition.x;
        canvasGroup.alpha = 0f;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
       // StartSlide(holdDuration);
    }
    public void StartSlide(float holdDuration)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateSlide(holdDuration));
    }

    public IEnumerator AnimateSlide(float holdDuration)
    {
        // 1. Kirish animatsiyasi (fade + slide)
        yield return StartCoroutine(SlideAndFade(startX, destinationX, 0f, 1f, slideDuration));

        // 2. Agar holdDuration > 1 bo¡®lsa text ni ko¡®rsatib update qilamiz
        if (holdDuration > 2f && countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            yield return StartCoroutine(UpdateCountdown(holdDuration));
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(holdDuration);
        }

        // 3. Chiqish animatsiyasi (fade + slide)
        yield return StartCoroutine(SlideAndFade(destinationX, startX, 1f, 0f, slideDuration));

        gameObject.SetActive(false);
    }

    IEnumerator SlideAndFade(float fromX, float toX, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float newX = Mathf.Lerp(fromX, toX, t);
            float newAlpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
            canvasGroup.alpha = newAlpha;

            yield return null;
        }

        rectTransform.anchoredPosition = new Vector2(toX, rectTransform.anchoredPosition.y);
        canvasGroup.alpha = toAlpha;
    }

    IEnumerator UpdateCountdown(float duration)
    {
        float remainingTime = duration;

        while (remainingTime > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remainingTime).ToString();

            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        if (countdownText != null)
            countdownText.text = "0";
    }
}
