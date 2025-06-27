using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownAnimator : MonoBehaviour
{
    public TMP_Text countdownText;
    public float animationDuration = 1f;
    public Vector3 maxScale = Vector3.one * 2f;
    public Vector3 minScale = Vector3.zero;

    public delegate void CountdownComplete();
    public event CountdownComplete onCountdownComplete;
    public CameraTransitionManager cameraMove;
    private void OnEnable()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int count = 3;

        while (count > 0)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = count.ToString();
            countdownText.rectTransform.localScale = maxScale;

            float t = 0f;
            while (t < animationDuration)
            {
                t += Time.deltaTime;
                float normalized = t / animationDuration;
                countdownText.rectTransform.localScale = Vector3.Lerp(maxScale, minScale, normalized);
                yield return null;
            }

            count--;
        }

        // GO ni chiqarish – SCALE QO‘ZG‘ALMAYDI
        countdownText.text = "Go!";
        countdownText.rectTransform.localScale = Vector3.one; // yoki maxScale yoki boshqa o‘lcham

        // "Go!" ni biroz ko‘rsatib turamiz (lekin animatsiyasiz)
        yield return new WaitForSeconds(animationDuration); // 1 soniya kutadi


        cameraMove.OnStartButtonClicked();
        // Countdown tugadi – textni o‘chiramiz
        gameObject.SetActive(false);

        // Event chaqiriladi (o'yinni boshlash uchun)
        onCountdownComplete?.Invoke();
    }
}
