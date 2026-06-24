using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public enum UIPanelType
{
    None = 0,
    Loading = 1,
    Home = 2,
    Zarafshan = 3,
    Egypt = 4,
    Sibiria = 5,
    KopkariRegistan = 6,
    Custom = 7,
    RacingTutorial = 8,
    Kansas = 9,
}

[Serializable]
public class PanelEntry
{
    public UIPanelType type;
    public RectTransform root;     // panelning anim qilinadigan root'i
    public CanvasGroup group;      // fade + raycast uchun
    public TMP_Text panelText;
}

public class UIOverlayRoot : MonoBehaviour
{
    public static UIOverlayRoot I { get; private set; }

    [Header("All Panels (including Loading)")]
    [SerializeField] private List<PanelEntry> panels = new();

    [Header("Default Panel On Start (optional)")]
    [SerializeField] private UIPanelType startPanel = UIPanelType.None;

    [Header("Tween Settings")]
    [SerializeField] private float showDuration = 0.22f;
    [SerializeField] private float hideDuration = 0.18f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;
    [SerializeField] private float slideOffsetX = 900f;  // esport uslub: o'ngdan kiradi

    private readonly Dictionary<UIPanelType, PanelEntry> _map = new();
    private readonly Dictionary<UIPanelType, Vector2> _shownPos = new();
    private readonly Dictionary<UIPanelType, Vector2> _hiddenPos = new();

    private Sequence _seq;
    private UIPanelType _current = UIPanelType.None;

    [SerializeField] private ConfirmationPopupController confirmPopup;
    private readonly Queue<IRequest> _queue = new Queue<IRequest>();
    private bool _isShowing;

    private void Awake()
    {
        // Singleton + DontDestroy
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        BuildCache();
        HideAllInstant();

        if (startPanel != UIPanelType.None)
            ShowPanel(startPanel,"", instant: true);
    }

    private void BuildCache()
    {
        _map.Clear();
        _shownPos.Clear();
        _hiddenPos.Clear();

        foreach (var p in panels)
        {
            if (p == null || p.root == null) continue;

            if (p.group == null)
                p.group = p.root.GetComponent<CanvasGroup>() ?? p.root.gameObject.AddComponent<CanvasGroup>();

            if (_map.ContainsKey(p.type)) continue;

            _map.Add(p.type, p);

            // shown pos = current anchoredPosition
            var shown = p.root.anchoredPosition;
            _shownPos[p.type] = shown;

            // hidden pos = o¡®ng tomonga surib qo'yamiz (e-sport slide)
            var hidden = shown + new Vector2(slideOffsetX, 0f);
            _hiddenPos[p.type] = hidden;
        }
    }

    // --- PUBLIC API ---

    public void HideAllInstant()
    {
        KillTweens();

        foreach (var kv in _map)
            ApplyHiddenInstant(kv.Key);

        _current = UIPanelType.None;
    }

    public void ShowLoading(bool instant = false)
    {
        ShowPanel(UIPanelType.Loading, "",instant);
    }

    public void HideLoading(bool instant = false)
    {
        HidePanel(UIPanelType.Loading, instant);
    }

    /// <summary>
    /// Bitta panelni ko'rsatadi. Agar exclusive=true bo'lsa, boshqalar yopiladi.
    /// </summary>
    public void ShowPanel(UIPanelType type, string message, bool instant = false, bool exclusive = true)
    {
        SoundManager.Instance?.StopRoomSmooth();
        if (!_map.TryGetValue(type, out var target) || target.root == null) return;
        if (!string.IsNullOrEmpty(message) && target.panelText != null)
            target.panelText.text = message;

        KillTweens();

        if (exclusive)
        {
            foreach (var kv in _map)
            {
                if (kv.Key == type) continue;
                ApplyHiddenInstant(kv.Key);
            }
        }

        target.root.gameObject.SetActive(true);

        if (instant)
        {
            ApplyShownInstant(type);
            _current = type;
            return;
        }

        // prepare hidden
        target.root.anchoredPosition = _hiddenPos[type];
        target.root.localScale = Vector3.one * 0.98f;
        target.group.alpha = 0f;
        target.group.blocksRaycasts = true; // show paytida input blok bo'lsin
        target.group.interactable = false;

        _seq = DOTween.Sequence();
        _seq.Join(target.root.DOAnchorPos(_shownPos[type], showDuration).SetEase(showEase));
        _seq.Join(target.group.DOFade(1f, showDuration).SetEase(showEase));
        _seq.Join(target.root.DOScale(1f, showDuration).SetEase(Ease.OutBack));

        _seq.OnComplete(() =>
        {
            target.group.interactable = true;
            target.group.blocksRaycasts = true;
            _current = type;
        });
    }

    public void HidePanel(UIPanelType type, bool instant = false)
    {
        if (!_map.TryGetValue(type, out var target) || target.root == null) return;
        if (!target.root.gameObject.activeSelf) return;

        KillTweens();

        if (instant)
        {
            ApplyHiddenInstant(type);
            if (_current == type) _current = UIPanelType.None;
            return;
        }

        target.group.interactable = false;
        target.group.blocksRaycasts = false;

        _seq = DOTween.Sequence();
        _seq.Join(target.root.DOAnchorPos(_hiddenPos[type], hideDuration).SetEase(hideEase));
        _seq.Join(target.group.DOFade(0f, hideDuration).SetEase(hideEase));
        _seq.Join(target.root.DOScale(0.98f, hideDuration).SetEase(hideEase));

        _seq.OnComplete(() =>
        {
            target.root.gameObject.SetActive(false);
            if (_current == type) _current = UIPanelType.None;
        });
    }
    public void HideCurrentPanel(bool instant = false)
    {
        if (_current == UIPanelType.None) return;
        HidePanel(_current, instant);
    }

    public bool IsVisible(UIPanelType type)
    {
        return _map.TryGetValue(type, out var p) && p.root != null && p.root.gameObject.activeSelf;
    }

    // --- Helpers ---

    private void ApplyShownInstant(UIPanelType type)
    {
        if (!_map.TryGetValue(type, out var p) || p.root == null) return;

        p.root.gameObject.SetActive(true);
        p.root.anchoredPosition = _shownPos[type];
        p.root.localScale = Vector3.one;
        p.group.alpha = 1f;
        p.group.blocksRaycasts = true;
        p.group.interactable = true;
    }

    private void ApplyHiddenInstant(UIPanelType type)
    {
        if (!_map.TryGetValue(type, out var p) || p.root == null) return;

        p.root.gameObject.SetActive(false);
        p.root.anchoredPosition = _hiddenPos[type];
        p.root.localScale = Vector3.one;
        p.group.alpha = 0f;
        p.group.blocksRaycasts = false;
        p.group.interactable = false;
    }

    private void KillTweens()
    {
        _seq?.Kill();
        foreach (var kv in _map)
        {
            var p = kv.Value;
            if (p?.root != null) p.root.DOKill();
            if (p?.group != null) p.group.DOKill();
        }
    }
    #region Popup
    // =========================
    // Public Facade API
    // =========================

    public void Confirm(int titleId, int descId, int okTextId, int cancelTextId,
        Action onOk, Action onCancel,
        ConfirmationPopupController.Options options = null)
    {
        Enqueue(new ConfirmRequest(titleId, descId, okTextId, cancelTextId, onOk, onCancel, options));
    }

    public void Confirm(string title, string desc, string okText, string cancelText,
        Action onOk, Action onCancel,
        ConfirmationPopupController.Options options = null)
    {
        Enqueue(new ConfirmTextRequest(title, desc, okText, cancelText, onOk, onCancel, options));
    }

    public void Done(int titleId, int descId, int doneTextId,
        Action onDone,
        ConfirmationPopupController.Options options = null)
    {
        Enqueue(new DoneRequest(titleId, descId, doneTextId, onDone, options));
    }

    // (ixtiyoriy) navbatni tozalash
    public void ClearAllPopups()
    {
        _queue.Clear();
        _isShowing = false;
        confirmPopup.Hide();
    }

    // =========================
    // Queue Core
    // =========================

    private void Enqueue(IRequest req)
    {
        _queue.Enqueue(req);
        TryShowNext();
    }

    private void TryShowNext()
    {
        if (_isShowing) return;
        if (_queue.Count == 0) return;

        _isShowing = true;
        var req = _queue.Dequeue();

        // Request popupni ko¡®rsatadi, biz esa "yopilganda" callback olamiz
        req.Show(confirmPopup, onClosed: () =>
        {
            _isShowing = false;
            TryShowNext();
        });
    }

    // =========================
    // Internal Request Types
    // =========================

    private interface IRequest
    {
        void Show(ConfirmationPopupController popup, Action onClosed);
    }

    private class ConfirmRequest : IRequest
    {
        private readonly int _titleId, _descId, _okId, _cancelId;
        private readonly Action _onOk, _onCancel;
        private readonly ConfirmationPopupController.Options _options;

        public ConfirmRequest(int titleId, int descId, int okId, int cancelId,
            Action onOk, Action onCancel,
            ConfirmationPopupController.Options options)
        {
            _titleId = titleId;
            _descId = descId;
            _okId = okId;
            _cancelId = cancelId;
            _onOk = onOk;
            _onCancel = onCancel;
            _options = options;
        }

        public void Show(ConfirmationPopupController popup, Action onClosed)
        {
            popup.Show(
                _titleId, _descId, _okId, _cancelId,
                onOk: () => { _onOk?.Invoke(); onClosed?.Invoke(); },
                onCancel: () => { _onCancel?.Invoke(); onClosed?.Invoke(); },
                options: _options
            );
        }
    }

    private class ConfirmTextRequest : IRequest
    {
        private readonly string _title, _desc, _ok, _cancel;
        private readonly Action _onOk, _onCancel;
        private readonly ConfirmationPopupController.Options _options;

        public ConfirmTextRequest(string title, string desc, string ok, string cancel,
            Action onOk, Action onCancel,
            ConfirmationPopupController.Options options)
        {
            _title = title;
            _desc = desc;
            _ok = ok;
            _cancel = cancel;
            _onOk = onOk;
            _onCancel = onCancel;
            _options = options;
        }

        public void Show(ConfirmationPopupController popup, Action onClosed)
        {
            popup.Show(
                _title, _desc, _ok, _cancel,
                onOk: () => { _onOk?.Invoke(); onClosed?.Invoke(); },
                onCancel: () => { _onCancel?.Invoke(); onClosed?.Invoke(); },
                options: _options
            );
        }
    }
    private class DoneRequest : IRequest
    {
        private readonly int _titleId, _descId, _doneId;
        private readonly Action _onDone;
        private readonly ConfirmationPopupController.Options _options;

        public DoneRequest(int titleId, int descId, int doneId,
            Action onDone,
            ConfirmationPopupController.Options options)
        {
            _titleId = titleId;
            _descId = descId;
            _doneId = doneId;
            _onDone = onDone;
            _options = options;
        }

        public void Show(ConfirmationPopupController popup, Action onClosed)
        {
            popup.Show(
                _titleId, _descId, _doneId,
                onDone: () => { _onDone?.Invoke(); onClosed?.Invoke(); },
                options: _options
            );
        }
    }
    #endregion
}
