using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIPopupCardTween : MonoBehaviour
{
    [Header("Refs")]
    public GameObject popupRoot;       // whole popup parent (inactive by default)
    public CanvasGroup dimGroup;       // background dim
    public CanvasGroup panelGroup;     // panel alpha
    public RectTransform panel;        // panel transform
    public Image successGlow;          // optional: glow/outline image
    public Image successCheck;         // optional: check icon image

    [Header("Timings")]
    public float openDimFade = 0.15f;
    public float openDuration = 0.22f;
    public float successFlashDelay = 0.02f; // after open starts
    public float autoCloseDelay = 3.0f;
    public float closeDuration = 0.18f;

    [Header("Tuning")]
    public float startScale = 0.86f;
    public float overshootScale = 1.03f;

    private Sequence _seq;

    private void Awake()
    {
        ResetVisuals();
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    // Call this from BUY button
    public void PlayAutoSuccess()
    {
        if (popupRoot == null) return;

        popupRoot.SetActive(true);
        ResetVisuals();

        _seq?.Kill();

        _seq = DOTween.Sequence();

        // OPEN
        _seq.Join(dimGroup.DOFade(1f, openDimFade).SetEase(Ease.OutQuad));
        _seq.Join(panelGroup.DOFade(1f, 0.12f).SetEase(Ease.OutQuad));
        _seq.Join(panel.DOScale(overshootScale, openDuration).SetEase(Ease.OutBack));
        _seq.Append(panel.DOScale(1f, 0.10f).SetEase(Ease.OutQuad));

        // SUCCESS FLASH (right after open)
        _seq.Insert(successFlashDelay, DOTween.Sequence()
            .AppendCallback(SuccessFlash)
        );

        // HOLD 3s
        _seq.AppendInterval(autoCloseDelay);

        // CLOSE
        _seq.AppendCallback(() =>
        {
            // kill any running tweens on visuals to avoid overlap
            panel.DOKill();
            panelGroup.DOKill();
            dimGroup.DOKill();
        });

        _seq.Join(panel.DOScale(0.92f, closeDuration).SetEase(Ease.InQuad));
        _seq.Join(panelGroup.DOFade(0f, closeDuration));
        _seq.Join(dimGroup.DOFade(0f, closeDuration));

        _seq.OnComplete(() =>
        {
            popupRoot.SetActive(false);
        });
    }

    private void SuccessFlash()
    {
        // esports style: quick punch + glow + check pop
        panel.DOKill();
        panel.DOPunchScale(Vector3.one * 0.08f, 0.16f, 10, 0.9f);

        if (successGlow != null)
        {
            successGlow.DOKill();
            var c = successGlow.color;
            successGlow.color = new Color(c.r, c.g, c.b, 0f);
            successGlow.DOFade(1f, 0.08f).OnComplete(() => successGlow.DOFade(0f, 0.22f));
        }

        if (successCheck != null)
        {
            successCheck.DOKill();
            var c = successCheck.color;
            successCheck.color = new Color(c.r, c.g, c.b, 0f);
            successCheck.transform.localScale = Vector3.one * 0.75f;

            successCheck.DOFade(1f, 0.08f);
            successCheck.transform.DOScale(1f, 0.14f).SetEase(Ease.OutBack);
        }
    }

    private void ResetVisuals()
    {
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
    }
}
