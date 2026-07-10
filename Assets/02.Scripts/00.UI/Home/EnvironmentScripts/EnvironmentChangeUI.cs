using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentChangeUI : MonoBehaviour
{
    private const float HiddenY = 390f;
    private const float VisibleY = -264f;
    private const float ShowDuration = 0.35f;
    private const float HideDuration = 0.25f;

    private RectTransform rectTransform;
    private Tween moveTween;
    private bool isOpen;

    [SerializeField] private Button closeButton;

    private void Awake()
    {
        CacheRefs();
        SetY(HiddenY);
    }

    private void OnEnable()
    {
        CacheRefs();

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        KillTween();
        isOpen = false;
        SetY(HiddenY);
    }

    public void Toggle()
    {
        if (isOpen)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        CacheRefs();

        if (rectTransform == null)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        KillTween();
        isOpen = true;

        SetY(HiddenY);
        moveTween = rectTransform
            .DOAnchorPosY(VisibleY, ShowDuration)
            .SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        CacheRefs();

        if (rectTransform == null)
            return;

        KillTween();
        isOpen = false;

        moveTween = rectTransform
            .DOAnchorPosY(HiddenY, HideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void CacheRefs()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
    }

    private void SetY(float y)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }

    private void KillTween()
    {
        moveTween?.Kill();
        moveTween = null;

        if (rectTransform != null)
            rectTransform.DOKill();
    }

}
