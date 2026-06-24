using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ConfirmationPopupController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;              // popup container
    [SerializeField] private CanvasGroup canvasGroup;      // fade uchun
    [SerializeField] private RectTransform panelRT;        // scale/move uchun
    [SerializeField] private Button blockerButton;         // modal click block (overlay)

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    [Header("Buttons")]
    [SerializeField] private GameObject okRoot;
    [SerializeField] private GameObject cancelRoot;
    [SerializeField] private GameObject doneRoot;

    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button doneButton;

    [SerializeField] private TMP_Text okText;
    [SerializeField] private TMP_Text cancelText;
    [SerializeField] private TMP_Text doneText;

    private Action _onOk;
    private Action _onCancel;
    private Action _onDone;

    private Tween _tween;

    [Serializable]
    public class Options
    {
        public bool closeOnBlocker = true;
        public bool closeOnEsc = true;       // PC
        public bool closeOnAndroidBack = true;
        public bool playHapticOnShow = false;
        public bool playHapticOnClick = true;

        public float showDur = 0.25f;
        public float hideDur = 0.18f;
        public float showScale = 1f;
        public float hiddenScale = 0.92f;
    }

    private static readonly Options DefaultOptions = new Options();

    private void Awake()
    {
        // Button listenerlar bir marta ulanadi
        okButton.onClick.AddListener(HandleOk);
        cancelButton.onClick.AddListener(HandleCancel);
        doneButton.onClick.AddListener(HandleDone);

        if (blockerButton != null)
            blockerButton.onClick.AddListener(HandleBlocker);

        HideImmediate();
    }

    #region Public API

    public void Show(int titleId, int descId, int okTextId, int cancelTextId,
        Action onOk, Action onCancel, Options options = null)
    {
        options ??= DefaultOptions;

        _onOk = onOk;
        _onCancel = onCancel;
        _onDone = null;

        // 🔥 OK/CANCEL rejimi: Done o‘chadi
        okRoot.SetActive(true);
        cancelRoot.SetActive(true);
        doneRoot.SetActive(false);

        ApplyTexts(titleId, descId);
        okText.text = GetText(okTextId);
        cancelText.text = GetText(cancelTextId);

        ShowInternal(options);
    }

    public void Show(string title, string desc, string okLabel, string cancelLabel,
        Action onOk, Action onCancel, Options options = null)
    {
        options ??= DefaultOptions;

        _onOk = onOk;
        _onCancel = onCancel;
        _onDone = null;

        okRoot.SetActive(true);
        cancelRoot.SetActive(true);
        doneRoot.SetActive(false);

        titleText.text = title;
        descText.text = desc;
        okText.text = okLabel;
        cancelText.text = cancelLabel;

        ShowInternal(options);
    }

    public void Show(int titleId, int descId, int doneTextId,
        Action onDone, Options options = null)
    {
        options ??= DefaultOptions;

        _onOk = null;
        _onCancel = null;
        _onDone = onDone;

        // 🔥 DONE rejimi: OK/CANCEL o‘chadi
        okRoot.SetActive(false);
        cancelRoot.SetActive(false);
        doneRoot.SetActive(true);

        ApplyTexts(titleId, descId);
        doneText.text = GetText(doneTextId);

        ShowInternal(options);
    }

    public void Hide(Options options = null)
    {
        options ??= DefaultOptions;
        HideInternal(options);
    }

    #endregion

    #region Internals

    private void ApplyTexts(int titleId, int descId)
    {
        titleText.text = GetText(titleId);
        descText.text = GetText(descId);
    }

    private string GetText(int id)
    {
        // Senda bor: LanguageManager.Instance.GetText(id)
        return LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(id)
            : $"#{id}";
    }

    private void ShowInternal(Options opt)
    {
        root.SetActive(true);
        if (blockerButton != null) blockerButton.gameObject.SetActive(true);

        // optional haptic
        // if (opt.playHapticOnShow) HomeHapticsManager.Instance?.Play(...);

        _tween?.Kill();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panelRT.localScale = Vector3.one * opt.hiddenScale;

        _tween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, opt.showDur))
            .Join(panelRT.DOScale(opt.showScale, opt.showDur).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
    }

    private void HideInternal(Options opt)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        _tween?.Kill();
        _tween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(canvasGroup.DOFade(0f, opt.hideDur))
            .Join(panelRT.DOScale(opt.hiddenScale, opt.hideDur).SetEase(Ease.InQuad))
            .OnComplete(HideImmediate);
    }

    private void HideImmediate()
    {
        _tween?.Kill();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (blockerButton != null) blockerButton.gameObject.SetActive(false);
        root.SetActive(false);

        _onOk = null;
        _onCancel = null;
        _onDone = null;
    }

    #endregion

    #region Button handlers

    private void HandleOk()
    {
        // if (DefaultOptions.playHapticOnClick) HomeHapticsManager.Instance?.Play(...);

        var cb = _onOk;
        HideInternal(DefaultOptions);
        cb?.Invoke();
    }

    private void HandleCancel()
    {
        var cb = _onCancel;
        HideInternal(DefaultOptions);
        cb?.Invoke();
    }

    private void HandleDone()
    {
        var cb = _onDone;
        HideInternal(DefaultOptions);
        cb?.Invoke();
    }

    private void HandleBlocker()
    {
        // Default: blocker bosilsa Cancel kabi yopiladi (agar OK/CANCEL bo‘lsa)
        // Done rejimida esa shunchaki yopishi ham mumkin yoki umuman yopmaslik — options bilan boshqarasan.
        if (!root.activeSelf) return;

        // eng oddiy: Cancel bo‘lsa Cancel’ni chaqir
        if (cancelRoot.activeSelf && _onCancel != null)
            HandleCancel();
        else
            HideInternal(DefaultOptions);
    }

    #endregion
}
