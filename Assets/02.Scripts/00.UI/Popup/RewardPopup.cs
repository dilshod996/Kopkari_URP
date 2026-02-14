using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RewardPopup : MonoBehaviour
{
    [Header("Reward Data")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text rewardName;
    [SerializeField] private GameObject amountBg;

    [Header("Popup Refs")]
    [SerializeField] private CanvasGroup dimGroup;     // dim background canvas group
    [SerializeField] private CanvasGroup panelGroup;   // main panel canvas group
    [SerializeField] private RectTransform panel;      // main panel rect

    [Header("Success UI")]
    [SerializeField] private Image successGlow;        // optional glow image on panel (alpha 0 default)
    [SerializeField] private Image successCheck;       // optional check icon (alpha 0 default)
    [SerializeField] private TMP_Text purchasedText;   // "PURCHASED" or "Sotib olindi!"
    [SerializeField] private RectTransform purchasedTextRT;
    [SerializeField] private ParticleSystem sparkleBurst; // optional particle burst (PlayOnAwake OFF)

    [Header("Timings")]
    [SerializeField] private float openDimFade = 0.15f;
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float successFlashDelay = 0.02f;
    [SerializeField] private float autoCloseDelay = 3.0f;
    [SerializeField] private float closeDuration = 0.18f;

    [Header("Tuning")]
    [SerializeField] private float startScale = 0.86f;
    [SerializeField] private float overshootScale = 1.03f;

    [SerializeField] private Button closeButton;

    private Sequence _seq;

    public void SetData(Sprite sprite, string titleTop, string amount, string name)
    {
        if(title != null) { title.text = titleTop;}
        if (icon != null) icon.sprite = sprite;
        if (amountText != null) amountText.text = amount;
        if(rewardName!=null) rewardName.text = name;
    }

    /// <summary>
    /// Call this when reward is granted / buy success happens.
    /// It will open popup, play success FX automatically, then close after 3s.
    /// </summary>
    public void PlaySuccess(Sprite sprite, string title, string amount, string name)
    {
        SetData(sprite, title, amount, name);
        PlayAutoSuccess();
    }

    private void OnEnable()
    {
        // Agar popup show bo'lganda avtomatik ishlasin desang:
        // PlayAutoSuccess();
        closeButton.onClick.AddListener(CloseTween);
        SoundManager.Instance?.PlayUI(UISoundType.Success);
    }

    private void OnDisable()
    {
        _seq?.Kill();
        closeButton.onClick.RemoveListener(CloseTween);
    }

    public void OnClose()
    {
        // manual close (optional). auto close bo'lsa ham ishlaydi.
        CloseTween();
    }

    private void PlayAutoSuccess()
    {
        ResetVisuals();

        _seq?.Kill();
        _seq = DOTween.Sequence();

        // OPEN
        if (dimGroup != null)
            _seq.Join(dimGroup.DOFade(1f, openDimFade).SetEase(Ease.OutQuad));

        if (panelGroup != null)
            _seq.Join(panelGroup.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));

        if (panel != null)
        {
            _seq.Join(panel.DOScale(overshootScale, openDuration).SetEase(Ease.OutBack));
            _seq.Append(panel.DOScale(1f, 0.10f).SetEase(Ease.OutQuad));
        }

        // SUCCESS FLASH (open bo'lishi bilan)
        _seq.Insert(successFlashDelay, DOTween.Sequence().AppendCallback(SuccessFlash));

    
    }

    private void SuccessFlash()
    {
        // Panel punch
        if (panel != null)
        {
            panel.DOKill();
            panel.DOPunchScale(Vector3.one * 0.08f, 0.16f, 10, 0.9f);
        }

        // Glow flash (optional)
        if (successGlow != null)
        {
            successGlow.DOKill();
            var c = successGlow.color;
            successGlow.color = new Color(c.r, c.g, c.b, 0f);
            successGlow.DOFade(1f, 0.08f).OnComplete(() => successGlow.DOFade(0f, 0.22f));
        }

        // Check pop (optional)
        if (successCheck != null)
        {
            successCheck.DOKill();
            var c = successCheck.color;
            successCheck.color = new Color(c.r, c.g, c.b, 0f);
            successCheck.transform.localScale = Vector3.one * 0.75f;

            successCheck.DOFade(1f, 0.08f);
            successCheck.transform.DOScale(1f, 0.14f).SetEase(Ease.OutBack);
        }

        // Purchased text (no auto-hide)
        if (purchasedText != null && purchasedTextRT != null)
        {
            purchasedText.DOKill();
            purchasedTextRT.DOKill();

            // kichkina pop kirish
            purchasedTextRT.anchoredPosition = Vector2.zero;     // uchib berishini xohlamasang 0 dan boshlaymiz
            purchasedTextRT.localScale = Vector3.one * 0.9f;
            purchasedText.alpha = 0f;

            // fade in + scale up
            purchasedText.DOFade(1f, 0.10f).SetEase(Ease.OutQuad);
            purchasedTextRT.DOScale(1f, 0.18f).SetEase(Ease.OutBack);

            // ❌ O‘CHIRILDI: auto fade out
            // purchasedText.DOFade(0f, 0.35f).SetDelay(1.0f);
        }

        // Sparkle burst (optional)
        if (sparkleBurst != null)
        {
            sparkleBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkleBurst.Play(true);
        }
    }

    private void ResetVisuals()
    {
        // kill old tweens
        panel?.DOKill();
        panelGroup?.DOKill();
        dimGroup?.DOKill();
        successGlow?.DOKill();
        if (successCheck != null) successCheck.DOKill();
        if (purchasedText != null) purchasedText.DOKill();
        if (purchasedTextRT != null) purchasedTextRT.DOKill();

        // base states
        if (dimGroup != null) dimGroup.alpha = 0f;
        if (panelGroup != null) panelGroup.alpha = 0f;
        if (panel != null) panel.localScale = Vector3.one * startScale;

        if (successGlow != null)
        {
            var c = successGlow.color;
            successGlow.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (successCheck != null)
        {
            var c = successCheck.color;
            successCheck.color = new Color(c.r, c.g, c.b, 0f);
            successCheck.transform.localScale = Vector3.one;
        }

        if (purchasedText != null)
        {
            purchasedText.alpha = 0f;
        }

        if (purchasedTextRT != null)
        {
            purchasedTextRT.anchoredPosition = Vector2.zero;
            purchasedTextRT.localScale = Vector3.one;
        }
    }

    public void CloseTween()
    {
        _seq?.Kill();

        var s = DOTween.Sequence();

        if (panel != null)
            s.Join(panel.DOScale(0.92f, closeDuration).SetEase(Ease.InQuad));

        if (panelGroup != null)
            s.Join(panelGroup.DOFade(0f, closeDuration));

        if (dimGroup != null)
            s.Join(dimGroup.DOFade(0f, closeDuration));

        s.OnComplete(() =>
        {
            HomeMainUI.Instance.HideUI(this);
        });

        _seq = s;
    }

}
