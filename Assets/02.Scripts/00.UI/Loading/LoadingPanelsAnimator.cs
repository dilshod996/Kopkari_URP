using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingPanelsAnimator : MonoBehaviour
{
    [Header("Slide-In Settings")]
    public float delayBetween = 0.25f;
    public float slideInTime = 0.5f;
    public float startOffsetX = -2000f;

    [Header("Wave Bounce Settings")]
    public float bounceAmount = 80f;
    public float bounceTime = 0.3f;
    public float waveDelay = 0.07f;
    public float scaleAmount = 1.05f;

    void Start()
    {
        StartCoroutine(AnimatePanels());
    }

    IEnumerator AnimatePanels()
    {
        int index = 0;

        foreach (Transform child in transform)
        {
            RectTransform panel = child.GetComponent<RectTransform>();
            if (panel == null) continue;

            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = child.gameObject.AddComponent<CanvasGroup>();

            // Set start position and invisible
            Vector2 finalPos = panel.anchoredPosition;
            panel.anchoredPosition = new Vector2(startOffsetX, finalPos.y);
            cg.alpha = 0;

            // Slide-in and fade-in
            LeanTween.moveX(panel, finalPos.x, slideInTime)
                     .setDelay(index * delayBetween)
                     .setEaseOutExpo();

            LeanTween.alphaCanvas(cg, 1f, slideInTime)
                     .setDelay(index * delayBetween);

            index++;
        }

        float totalAnimTime = slideInTime + ((index - 1) * delayBetween);
        yield return new WaitForSeconds(totalAnimTime + 0.1f);

        // After slide-in, start wave bounce effect
        StartCoroutine(WaveBackBounce());
    }

    IEnumerator WaveBackBounce()
    {
        int index = 0;

        foreach (Transform child in transform)
        {
            RectTransform panel = child.GetComponent<RectTransform>();
            if (panel == null) continue;

            Vector2 originalPos = panel.anchoredPosition;
            Vector2 backPos = new Vector2(originalPos.x - bounceAmount, originalPos.y);
            Vector3 originalScale = panel.localScale;
            Vector3 punchScale = originalScale * scaleAmount;

            float thisDelay = index * waveDelay;

            StartCoroutine(DoBounce(panel, originalPos, backPos, originalScale, punchScale, thisDelay));

            index++;
        }

        // Bitta wave harakati tugagandan keyin biroz kutamiz (delay + bounceTime * 2)
        float totalWait = ((index - 1) * waveDelay) + bounceTime * 2 + 1f;
        yield return new WaitForSeconds(totalWait);

        // O'zini qayta chaqiradi
        StartCoroutine(WaveBackBounce());
    }

    IEnumerator DoBounce(RectTransform panel, Vector2 originalPos, Vector2 backPos, Vector3 originalScale, Vector3 punchScale, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Position bounce
        LeanTween.value(gameObject, originalPos.x, backPos.x, bounceTime)
            .setOnUpdate((float val) =>
            {
                panel.anchoredPosition = new Vector2(val, originalPos.y);
            })
            .setEaseInSine()
            .setOnComplete(() =>
            {
                LeanTween.value(gameObject, backPos.x, originalPos.x, bounceTime)
                    .setOnUpdate((float val) =>
                    {
                        panel.anchoredPosition = new Vector2(val, originalPos.y);
                    })
                    .setEaseOutSine();
            });

        // Scale bounce
        LeanTween.scale(panel, punchScale, bounceTime)
            .setEaseOutSine()
            .setOnComplete(() =>
            {
                LeanTween.scale(panel, originalScale, bounceTime)
                    .setEaseInSine();
            });
    }
}
