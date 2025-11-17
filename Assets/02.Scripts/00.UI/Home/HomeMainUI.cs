using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeMainUI : MonoBehaviour
{
    public static HomeMainUI Instance { get; private set; }

    [Header("Auto Play")]
    [SerializeField] private bool playOnStart = true;

    [Header("Left Panel Settings")]
    [SerializeField] private RectTransform leftRect;
    [Header("Right Panel Settings")]
    [SerializeField] private RectTransform rightRect;
    [SerializeField] private float startXRight = 200f;   // o¡®ngdan kiradi
    [SerializeField] private float targetXRight = -143f; // final pozitsiya

    [Header("Movement Common Settings")]
    [SerializeField] private float moveTime = 0.35f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeOutCubic;

    [Header("Scale Animation")]
    [SerializeField] private float punchScaleM = 1.2f;
    [SerializeField] private float scaleTime = 0.2f;

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;

    [Header("Fade Settings For UI Pages")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeTime = 0.2f;

    [Header("UI Buttons")]
    [SerializeField] private Button playBtn;
    [Header("UI Pages")]
    [SerializeField] private GameplayMode playMode;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayLeft();
            PlayRight();
        }
        playBtn.onClick.AddListener(() =>
        {
            ShowUI(playMode);
        });
    }

    #region Beginning Right & Left Animations

    public void PlayLeft()
    {
        if (leftRect == null) return;
        Play(leftRect, -startXRight, -targetXRight);
    }

    public void PlayRight()
    {
        if (rightRect == null) return;
        Play(rightRect, startXRight, targetXRight);
    }

    /// <summary>
    /// Universal slide + punch scale
    /// </summary>
    private void Play(RectTransform rect, float startX, float targetX)
    {
        // Start pozitsiyani beramiz
        var pos = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(startX, pos.y);
        rect.localScale = Vector3.one;

        // Slide anima
        LeanTween.moveX(rect, targetX, moveTime)
            .setEase(ease)
            .setOnComplete(() => PunchScale(rect));
    }

    private void PunchScale(RectTransform rect)
    {
        // Scale 1 ¡æ 1.2 ¡æ 1
        LeanTween.scale(rect, Vector3.one * punchScaleM, scaleTime)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                LeanTween.scale(rect, Vector3.one, scaleTime * 0.8f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });
    }

    #endregion

    #region SHow and Hide UI Pages
    public void ShowUI(MonoBehaviour ui) => ShowUI(ui.gameObject);
    public void HideUI(MonoBehaviour ui) => HideUI(ui.gameObject);

    public void ShowUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>(); // oldindan bor deb faraz qilamiz

        page.SetActive(true);
        rt.localScale = Vector3.one * startScale;
        cg.alpha = 0f;

        // fade
        LeanTween.alphaCanvas(cg, 1f, fadeTime);

        // scale
        LeanTween.scale(rt, Vector3.one * punchScale, animTime)
            .setEase(easeIn)
            .setOnComplete(() =>
            {
                LeanTween.scale(rt, Vector3.one, animTime * 0.7f)
                    .setEase(easeOut);
            });
    }

    public void HideUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        // fade
        LeanTween.alphaCanvas(cg, 0f, fadeTime);

        // scale + deactivate
        LeanTween.scale(rt, Vector3.one * startScale, animTime)
            .setEase(easeOut)
            .setOnComplete(() =>
            {
                page.SetActive(false);
            });
    }
    #endregion
}
