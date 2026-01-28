using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;

public class EnvironmentChangeUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform maskRT;   // RectMask2D bor object
    [SerializeField] private CanvasGroup canvasGroup; // panel yoki parentda

    [Header("Timing")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Behavior")]
    [SerializeField] private bool disableAfterHide = true;
    private bool _isOpen = false;

    private float _fullWidth;
    private Sequence _seq;



    private void Awake()
    {
        if (maskRT != null)
            _fullWidth = maskRT.sizeDelta.x;
    }

    private void OnEnable()
    {

    }
    public void Toggle()
    {
        if (_isOpen) Hide();
        else Show();
    }
    public void Show()
    {
        if (maskRT == null) return;

        KillTweens();
        _isOpen = true;
        // Reset start
        maskRT.sizeDelta = new Vector2(0f, maskRT.sizeDelta.y);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        _seq = DOTween.Sequence();

        // Fade + Reveal parallel
        if (canvasGroup != null)
            _seq.Join(canvasGroup.DOFade(1f, duration).SetEase(ease));

        _seq.Join(maskRT.DOSizeDelta(new Vector2(_fullWidth, maskRT.sizeDelta.y), duration)
            .SetEase(ease));
    }

    public void Hide()
    {
        if (maskRT == null) return;

        KillTweens();
        _isOpen = false;
        _seq = DOTween.Sequence();

        // Reverse: FadeOut + Unreveal parallel
        if (canvasGroup != null)
            _seq.Join(canvasGroup.DOFade(0f, duration * 0.6f).SetEase(Ease.InCubic));

        _seq.Join(maskRT.DOSizeDelta(new Vector2(0f, maskRT.sizeDelta.y), duration)
            .SetEase(Ease.InCubic));

        if (disableAfterHide)
        {
            _seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }


    private void KillTweens()
    {
        _seq?.Kill();
        _seq = null;

        maskRT?.DOKill();
        canvasGroup?.DOKill();
    }
   

    private void OnDisable()
    {
        KillTweens();
    }
}
