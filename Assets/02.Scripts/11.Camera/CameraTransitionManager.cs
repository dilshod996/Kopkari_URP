using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Cinemachine;

public class CameraTransitionManager : MonoBehaviour
{
    public CinemachineVirtualCamera startCam;
    public CinemachineVirtualCamera mainCam;
    public CinemachineImpulseSource impulseSource;
    public GameObject fadePanel;
    public Image fadeImage;
    public float fadeDuration = 3f;
    private void Start()
    {
        PlayImpulse(); // Dastlabki impulsni jo‘natamiz
    }
    public void OnStartButtonClicked()
    {
        StartCoroutine(FadeToMainCamera());
    }

    private IEnumerator FadeToMainCamera()
    {
        fadePanel.SetActive(true);

        // 1. Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. Camera switch (hali ekran qora)
        startCam.Priority = 0;
        mainCam.Priority = 15;

        // 3. BIR frame kutamiz, Cinemachine o‘zgarishini yakunlasin
        yield return new WaitForSeconds(1f);

        // 4. Endi fade-in boshlanadi
        yield return StartCoroutine(Fade(1f, 0f));

        fadePanel.SetActive(false);

        if (KopkariManager.Instance != null)
        {
            KopkariManager.Instance.StartGame();
            Debug.Log("Game started from cameratransition");
        }
        else
        {
            Debug.LogError("RoomStartTutorial: BaseManager.Instance not found");
        }
    }

    // Fade with optional callback at the end
    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Yakuniy alpha
        fadeImage.color = new Color(color.r, color.g, color.b, to);

        // Fade tugagach callback bajariladi
        //onFadeComplete?.Invoke();
    }
    public void PlayImpulse()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
            Debug.Log("Impulse signal jo‘natildi!");
        }
        else
        {
            Debug.LogWarning("ImpulseSource biriktirilmagan!");
        }
    }
}
