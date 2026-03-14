using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaqaScript : MonoBehaviour
{
    public Image targetImage; // ✅ UI Image uchun
    public void CallImage()
    {
        StartCoroutine(FadeInImage()); // ✅ FadeInImage() ni chaqirish
    }
    IEnumerator FadeInImage()
    {
        if (targetImage == null) yield break;

        float fadeDuration = 0.5f;
        float currentTime = 0f;

        Color startColor = targetImage.color;
        Color endColor = startColor;
        endColor.a = 129f / 255f;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float t = currentTime / fadeDuration;
            targetImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        targetImage.color = endColor; // oxirgi nuqtada to‘liq alpha bilan tugaydi
        Debug.Log("Image fade-in 129 alpha bilan tugadi");
    }
}
