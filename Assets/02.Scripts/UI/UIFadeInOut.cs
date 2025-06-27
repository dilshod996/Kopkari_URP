using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeInOut : MonoBehaviour
{
    public Image boostImage;
    public float fadeDuration = 0.25f;
    public float showDuration = 0.2f;
    void Start()
    {

    }

    public void BoosterUI(Color32 color32)
    {
        boostImage.gameObject.SetActive(true);
        Color baseColor = color32;
        baseColor.a = 0f;

        boostImage.color = baseColor;

        StartCoroutine(FadeBoosterUI(color32));
    }

    private IEnumerator FadeBoosterUI(Color32 color32)
    {
        float timer = 0f;

        Color startColor = color32;
        startColor.a = 0f;

        Color targetColor = color32;
        targetColor.a = 1f;

        // Fade in
        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            boostImage.color = Color.Lerp(startColor, targetColor, t);
            timer += Time.deltaTime;
            yield return null;
        }

        boostImage.color = targetColor;

        // Pause
        yield return new WaitForSeconds(showDuration);

        // Fade out
        timer = 0f;
        startColor = targetColor;
        Color endColor = targetColor;
        endColor.a = 0f;

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            boostImage.color = Color.Lerp(startColor, endColor, t);
            timer += Time.deltaTime;
            yield return null;
        }

        boostImage.color = endColor;
        boostImage.gameObject.SetActive(false);
    }
}
