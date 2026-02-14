using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class RightPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;          // popup container (setActive qilinadi)
    [SerializeField] private RectTransform popupRT;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image icon;

    [Header("Positions (Anchored X)")]
    [SerializeField] private float showX = 301f;
    [SerializeField] private float hideX = -315f;

    [Header("Timings")]
    [SerializeField] private float showDuration = 0.4f;
    [SerializeField] private float stayTime = 2.4f;
    [SerializeField] private float hideDuration = 0.35f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.96f;
    [SerializeField] private float showScaleDuration = 0.25f;

    private Tween sequenceTween;

    private struct PopupRequest
    {
        public string message;
        public Sprite icon;
        public float stay;
    }

    private readonly Queue<PopupRequest> _queue = new Queue<PopupRequest>(8);
    private bool _isPlayingQueue = false;

    private void Reset()
    {
        // root bo'sh bo'lsa o'zini root deb olsin
        if (root == null) root = gameObject;
        if (popupRT == null) popupRT = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowRightPopup(string message, Sprite iconSprite)
    {
        if (popupRT == null || canvasGroup == null || messageText == null) return;

        // Set content
        messageText.text = message;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = (iconSprite != null);
        }

        // Kill old tweens
        popupRT.DOKill(false);
        canvasGroup.DOKill(false);
        sequenceTween?.Kill(false);

        // Initial state
        popupRT.anchoredPosition = new Vector2(hideX, popupRT.anchoredPosition.y);
        popupRT.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        // Sequence (unscaled time: pause bo'lsa ham chiqaversin)
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // SHOW
        seq.Append(popupRT.DOAnchorPosX(showX, showDuration).SetEase(Ease.OutCubic));
        seq.Join(popupRT.DOScale(1f, showScaleDuration).SetEase(Ease.OutBack));
        seq.Join(canvasGroup.DOFade(1f, 0.15f));

        // STAY
        if (stayTime > 0f)
            seq.AppendInterval(stayTime);

        // HIDE
        seq.Append(popupRT.DOAnchorPosX(hideX, hideDuration).SetEase(Ease.InCubic));
        seq.Join(canvasGroup.DOFade(0f, 0.15f));

        seq.OnComplete(() =>
        {
            if (root != null) root.SetActive(false);
            else gameObject.SetActive(false);
        });

        sequenceTween = seq;
    }

    public void HideNow()
    {
        popupRT?.DOKill(false);
        canvasGroup?.DOKill(false);
        sequenceTween?.Kill(false);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (popupRT != null) popupRT.anchoredPosition = new Vector2(hideX, popupRT.anchoredPosition.y);

        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }


    #region AI Rider larn chiqib ketganlik haqida message
    public void EnqueueEliminatedRider(string riderName, Sprite iconSprite = null, bool disqualified = true)
    {
        string status = disqualified ? "Disqualified" : "Eliminated";
        string msg = string.IsNullOrWhiteSpace(riderName) ? status : $"{riderName} — {status}";

        _queue.Enqueue(new PopupRequest
        {
            message = msg,
            icon = iconSprite,
            stay = 2f // sen xohlagan: 2 sekund
        });

        TryPlayNextFromQueue();
    }
    private void TryPlayNextFromQueue()
    {
        if (_isPlayingQueue) return;
        if (_queue.Count == 0) return;

        _isPlayingQueue = true;

        var req = _queue.Dequeue();
        PlayPopupInternal(req.message, req.icon, req.stay, () =>
        {
            _isPlayingQueue = false;
            // navbatda yana bo'lsa, keyingisini chiqaramiz
            TryPlayNextFromQueue();
        });
    }

    private void PlayPopupInternal(string message, Sprite iconSprite, float stay, System.Action onDone)
    {
        if (popupRT == null || canvasGroup == null || messageText == null)
        {
            onDone?.Invoke();
            return;
        }

        // Set content
        messageText.text = message;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = (iconSprite != null);
        }

        // Kill old tweens (queue popuplar o'zaro to'qnashmasin)
        popupRT.DOKill(false);
        canvasGroup.DOKill(false);
        sequenceTween?.Kill(false);

        // Initial state
        popupRT.anchoredPosition = new Vector2(hideX, popupRT.anchoredPosition.y);
        popupRT.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        // Sequence (unscaled time)
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // SHOW
        seq.Append(popupRT.DOAnchorPosX(showX, showDuration).SetEase(Ease.OutCubic));
        seq.Join(popupRT.DOScale(1f, showScaleDuration).SetEase(Ease.OutBack));
        seq.Join(canvasGroup.DOFade(1f, 0.15f));

        // STAY
        if (stay > 0f)
            seq.AppendInterval(stay);

        // HIDE
        seq.Append(popupRT.DOAnchorPosX(hideX, hideDuration).SetEase(Ease.InCubic));
        seq.Join(canvasGroup.DOFade(0f, 0.15f));

        seq.OnComplete(() =>
        {
            if (root != null) root.SetActive(false);
            else gameObject.SetActive(false);

            onDone?.Invoke();
        });

        sequenceTween = seq;
    }
    public void ClearQueue()
    {
        _queue.Clear();
    }

    #endregion
}
