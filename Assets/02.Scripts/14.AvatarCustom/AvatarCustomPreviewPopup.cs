using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class AvatarCustomPreviewPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private Image bigIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text applyBtnText;
    [SerializeField] private Button applyBtn;
    [SerializeField] private GameObject lockedRoot;

    [Header("DOTween anim target")]
    [SerializeField] private RectTransform panelRect;
    private Tween _shakeTween;
    private Tween _moveTween;
    private Coroutine _autoHideCo;

    private Func<Task<bool>> _onApply;

    private const float HiddenX = -300f;
    private const float ShownX = 228f;
    private const float ShowDur = 0.45f;
    private const float HideDur = 0.25f;

    private void Awake()
    {
        // ❗ gameObject.SetActive(false) QILMAYMIZ
        // Popup doim active turadi, faqat ko‘rinmaydi va raycast olmaydi
        InitHiddenImmediate();
    }

    private void OnEnable()
    {
        // double-subscribe bo‘lmasin
        OptionItemUI.OnNotEnoughCoins -= PlayNotEnoughCoinsFeedback;
        OptionItemUI.OnNotEnoughCoins += PlayNotEnoughCoinsFeedback;
    }

    private void OnDisable()
    {
        OptionItemUI.OnNotEnoughCoins -= PlayNotEnoughCoinsFeedback;
    }

    private void InitHiddenImmediate()
    {
        if (panelRect != null)
        {
            panelRect.DOKill(false);
            panelRect.anchoredPosition = new Vector2(HiddenX, panelRect.anchoredPosition.y);
        }

        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public void Show(CatalogEntry entry, string playerId, string slotId, Sprite imageSprite, Func<Task<bool>> onApply)
    {
        _onApply = onApply;

        bool unlocked = entry.IsDefault ||
                        PlayerPrefs.GetInt($"Unlock_{playerId}_{slotId}_{entry.OptionId}", 0) == 1;

        if (applyBtnText) applyBtnText.text = unlocked ? "Change" : $"Buy: {entry.Price}";
        if (lockedRoot) lockedRoot.SetActive(!unlocked);

        if (titleText) titleText.text = entry.SlotId;
        if (bigIcon) bigIcon.sprite = imageSprite;

        if (applyBtn)
        {
            applyBtn.onClick.RemoveAllListeners();
            applyBtn.onClick.AddListener(() => _ = OnClickApply());
        }

        SetVisible(true);
        RestartAutoHide();
        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
    }

    private async Task OnClickApply()
    {
        bool ok = true;
        if (_onApply != null)
            ok = await _onApply.Invoke();

        if (ok)
            Hide();
        else
            PlayNotEnoughCoinsFeedback(); // yopilmasin

        RestartAutoHide();
    }

    public void Hide()
    {
        SetVisible(false);
        SoundManager.Instance?.PlayUI(UISoundType.PopupClose);
    }

    private void SetVisible(bool v)
    {
        _moveTween?.Kill(false);

        if (cg != null)
        {
            cg.alpha = v ? 1f : 0f;
            cg.interactable = v;
            cg.blocksRaycasts = v;
        }

        if (panelRect == null) return;

        panelRect.DOKill(false);

        if (v)
        {
            panelRect.anchoredPosition = new Vector2(HiddenX, panelRect.anchoredPosition.y);
            _moveTween = panelRect.DOAnchorPosX(ShownX, ShowDur).SetEase(Ease.OutBack);
        }
        else
        {
            _moveTween = panelRect.DOAnchorPosX(HiddenX, HideDur).SetEase(Ease.InBack);
        }
    }

    private void RestartAutoHide()
    {
        if (_autoHideCo != null) StopCoroutine(_autoHideCo);
        _autoHideCo = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(5f);
        Hide();
    }

    private void PlayNotEnoughCoinsFeedback()
    {
        // endi gameObject inactive bo‘lmaydi, shuning uchun bu stabil ishlaydi
        if (!isActiveAndEnabled) return;
        if (panelRect == null) return;

        _shakeTween?.Kill(false);
        panelRect.DOKill(false);

        _shakeTween = panelRect.DOShakeAnchorPos(
            duration: 0.25f,
            strength: new Vector2(20f, 0f),
            vibrato: 18,
            randomness: 0f,
            snapping: false,
            fadeOut: true
        );

        SoundManager.Instance?.PlayUI(UISoundType.Error);
        RestartAutoHide();
    }
}
