using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UISpeechBuble : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text speechText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Anim Settings")]
    [SerializeField] private float startX = -360f;
    [SerializeField] private float midX = -312f;
    [SerializeField] private float endX = -286f;
    [SerializeField] private float hideX = 300f; // ❗ chiqishda shu nuqtaga boradi
    [SerializeField] private float move1Duration = 0.22f;
    [SerializeField] private float settleDuration = 0.12f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (!canvasGroup)
        {
            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        CleanSpeech();
        var pos = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(startX, pos.y);
        canvasGroup.alpha = 0f;
    }

    public void Show(string text)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SpeechBuble(text);

        LeanTween.cancel(gameObject);
        LeanTween.cancel(rect);

        // Boshlang‘ich holat — o‘ng tomonda yashiringan holatda
        var pos = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(hideX, pos.y); // 300 dan boshlanadi
        canvasGroup.alpha = 0f;

        // 1️⃣ O‘ngdan chapga kirib kelish (-360 gacha)
        LeanTween.moveX(rect, startX, move1Duration)
            .setEase(LeanTweenType.easeOutCubic)
            .setIgnoreTimeScale(true)
            .setOnStart(() =>
            {
                // Kirib kelish boshlangan frame’da fade-in boshlanadi
                LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration)
                    .setIgnoreTimeScale(true);
            })
            .setOnComplete(() =>
            {
                // 2️⃣ Keyin yumshoq to‘xtash (-312 gacha)
                LeanTween.moveX(rect, -312f, settleDuration)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setIgnoreTimeScale(true);
            });
    }



    public void Hide()
    {
        LeanTween.cancel(gameObject);
        LeanTween.cancel(rect);

        // Hozirgi Y ni saqlab turamiz
        float y = rect.anchoredPosition.y;

        // --- Parallel fade va move-out ---
        LeanTween.moveX(rect, hideX, fadeOutDuration)
            .setEase(LeanTweenType.easeInCubic)
            .setIgnoreTimeScale(true);

        LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                // Yashirib, pozitsiyani qayta tiklaymiz
                gameObject.SetActive(false);
                rect.anchoredPosition = new Vector2(startX, y);
            });
    }

    public void SpeechBuble(string text)
    {
        if (speechText) speechText.text = text;
    }

    private void CleanSpeech()
    {
        if (speechText) speechText.text = null;
    }
}
