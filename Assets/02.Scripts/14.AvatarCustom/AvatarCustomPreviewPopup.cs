using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MalbersAnimations;
using static Constants;
using System.Collections;

public class AvatarCustomPreviewPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private Image bigIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text applyBtnText;
    [SerializeField] private Button applyBtn;

    [SerializeField] private GameObject lockedRoot;
    // DOTween anim target
    [SerializeField] private RectTransform panelRect;
    private Tween _shakeTween;

    private const float HiddenX = -300f;
    private const float ShownX = 228f;
    private const float ShowDur = 0.45f;
    private const float HideDur = 0.25f;

    private Tween _moveTween;
    private Coroutine _autoHideCo;

    private Func<Task> _onApply;

    private void Awake()
    {


        // default joyga qo'yib qo'yamiz
        if (panelRect != null)
        {
            var p = panelRect.anchoredPosition;
            p.x = HiddenX;
            panelRect.anchoredPosition = p;
        }

        Hide();
        OptionItemUI.OnNotEnoughCoins += PlayNotEnoughCoinsFeedback;

    }

    public void Show(CatalogEntry entry, string playerId, string slotId, Sprite imageSprite, Func<Task> onApply)
    {
        _onApply = onApply;
        bool unlocked = entry.IsDefault || PlayerPrefs.GetInt($"Unlock_{playerId}_{slotId}_{entry.OptionId}", 0) == 1;

        if (applyBtnText) applyBtnText.text = unlocked ? "Change" : $"{entry.Price}";
        if (lockedRoot) lockedRoot.SetActive(!unlocked);
       // if (priceText) priceText.text = unlocked ? "" : entry.Price.ToString();

        if (titleText) titleText.text = entry.SlotId;
        bigIcon.sprite = imageSprite;
        if (applyBtn)
        {
            applyBtn.onClick.RemoveAllListeners();
            applyBtn.onClick.AddListener(async () =>
            {
                if (_onApply != null) await _onApply.Invoke();
                Hide();
            });
        }

        SetVisible(true);
        RestartAutoHide();


    }

    public void Hide() => SetVisible(false);

    private void SetVisible(bool v)
    {
        if (cg == null)
        {
            gameObject.SetActive(v);
            return;
        }

        // tween conflict bo'lmasin
        _moveTween?.Kill(false);

        if (v)
        {
            // avval yoqamiz (tween ko'rinishi uchun)
            gameObject.SetActive(true);

            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // kirib kelish anim
            if (panelRect != null)
            {
                panelRect.DOKill(false);
                panelRect.anchoredPosition = new Vector2(HiddenX, panelRect.anchoredPosition.y);

                _moveTween = panelRect.DOAnchorPosX(ShownX, ShowDur)
                    .SetEase(Ease.OutBack);
            }
        }
        else
        {
            // chiqib ketish anim (tugagach o'chiramiz)
            cg.interactable = false;
            cg.blocksRaycasts = false;

            if (panelRect != null)
            {
                panelRect.DOKill(false);

                _moveTween = panelRect.DOAnchorPosX(HiddenX, HideDur)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        cg.alpha = 0;
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                cg.alpha = 0;
                gameObject.SetActive(false);
            }
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

        if (gameObject.activeSelf)
            Hide();
    }
    private void PlayNotEnoughCoinsFeedback()
    {
        if (!gameObject.activeInHierarchy) return;
        if (panelRect == null) return;

        _shakeTween?.Kill(false);
        panelRect.DOKill(false);

        // “jizz” — qisqa va jonli qaltirash
        _shakeTween = panelRect.DOShakeAnchorPos(
            duration: 0.25f,
            strength: new Vector2(20f, 0f),
            vibrato: 18,
            randomness: 0f,
            snapping: false,
            fadeOut: true
        );
        RestartAutoHide();

    }
    private void OnDestroy()
    {
        OptionItemUI.OnNotEnoughCoins -= PlayNotEnoughCoinsFeedback;
    }



}
