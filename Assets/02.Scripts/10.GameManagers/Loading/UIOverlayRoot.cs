using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using Michsky.UI.ModernUIPack;

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
    public Slider timeSlider;
    public ProgressBar timeProgressBar;
    public TMP_Text timeText;
}

public class UIOverlayRoot : MonoBehaviour
{
    public static UIOverlayRoot I { get; private set; }

    [Header("All Panels (including Loading)")]
    [SerializeField] private List<PanelEntry> panels = new();
    [Header("Movement Panel")]
    [SerializeField] private RectTransform movementPanelRoot;
    [SerializeField] private CanvasGroup movementPanelGroup;
    [SerializeField] private Image movementBackgroundImage;
    [SerializeField] private Image movementMapImage;
    [SerializeField] private TMP_Text movementMapNameText;
    [SerializeField] private TMP_Text distaneTitle;
    [SerializeField] private TMP_Text movementDistanceText;
    [SerializeField] private TMP_Text movementRidersText;
    [SerializeField] private TMP_Text ridersTitle;
    [SerializeField] private TMP_Text movementWeatherText;
    [SerializeField] private TMP_Text weatherTitle;
    [SerializeField] private TMP_Text movementEntryCostText;
    [SerializeField] private TMP_Text entryCostTitle;
    [SerializeField] private Slider movementTimeSlider;
    [SerializeField] private ProgressBar movementTimeProgressBar;
    [SerializeField] private TMP_Text movementTimeText;
    [SerializeField] private RectTransform movementContentRoot;
    [SerializeField] private RectTransform movementMapImageRoot;
    [SerializeField] private RectTransform[] movementStaggerItems;

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
    private Coroutine _timeRoutine;
    private Sequence _movementSeq;
    private Tween _movementPulseTween;
    private bool _sceneProgressLoaded;
    private Action _sceneProgressLoadedHandler;
    private UIPanelType _current = UIPanelType.None;
    private MapCard.MapDetailsData _lastMovementData;
    private bool _hasLastMovementData;

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
        StopTimeProgress();
        ApplyMovementPanelHiddenInstant();
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

    public void ShowMovementPanel(MapCard.MapDetailsData data, bool instant = false)
    {
        _lastMovementData = data;
        _hasLastMovementData = true;

        if (movementPanelRoot == null)
        {
            ShowFallbackMovementPanel(data, instant, true);
            return;
        }

        CacheMovementPanelRefs();
        ShowMovementPanelRoot(instant);
        ApplyMovementPanelTranslations();
        SetMovementPanelData(data);
        StartMovementSceneProgress();
        PlayMovementPanelAnimation();
    }

    public void ShowMovementPanelForScene(SceneLoadManager.SceneType sceneType, bool instant = false)
    {
        if (_hasLastMovementData && _lastMovementData.MovingRoom == sceneType)
        {
            ShowMovementPanel(_lastMovementData, instant);
            return;
        }

        ShowFallbackMovementPanel(new MapCard.MapDetailsData { MovingRoom = sceneType }, instant, true);
    }

    public void HideMovementPanel(bool instant = false)
    {
        if (movementPanelRoot == null || !movementPanelRoot.gameObject.activeSelf)
            return;

        StopMovementPanelAnimation();

        if (instant)
        {
            ApplyMovementPanelHiddenInstant();
            return;
        }

        movementPanelGroup.interactable = false;
        movementPanelGroup.blocksRaycasts = false;

        _movementSeq = DOTween.Sequence().SetTarget(this);
        _movementSeq.Join(movementPanelGroup.DOFade(0f, hideDuration).SetEase(hideEase));
        _movementSeq.Join(movementPanelRoot.DOScale(0.98f, hideDuration).SetEase(hideEase));
        _movementSeq.OnComplete(ApplyMovementPanelHiddenInstant);
        _movementSeq.OnKill(() => _movementSeq = null);
    }

    private void CacheMovementPanelRefs()
    {
        if (movementPanelRoot == null)
            return;

        movementPanelGroup ??= movementPanelRoot.GetComponent<CanvasGroup>() ?? movementPanelRoot.gameObject.AddComponent<CanvasGroup>();
        movementBackgroundImage ??= movementPanelRoot.GetComponent<Image>();
        movementTimeSlider ??= movementPanelRoot.GetComponentInChildren<Slider>(true);
        movementTimeProgressBar ??= movementPanelRoot.GetComponentInChildren<ProgressBar>(true);
    }

    private void ApplyMovementPanelHiddenInstant()
    {
        if (movementPanelRoot == null)
            return;

        movementPanelRoot.gameObject.SetActive(false);
        movementPanelRoot.localScale = Vector3.one;

        if (movementPanelGroup != null)
        {
            movementPanelGroup.alpha = 0f;
            movementPanelGroup.blocksRaycasts = false;
            movementPanelGroup.interactable = false;
        }

        ApplyMovementTimeProgress(0f);
    }

    private void ShowMovementPanelRoot(bool instant)
    {
        movementPanelRoot.gameObject.SetActive(true);
        movementPanelRoot.localScale = Vector3.one;

        movementPanelGroup.alpha = 1f;
        movementPanelGroup.blocksRaycasts = true;
        movementPanelGroup.interactable = true;
    }

    private void SetMovementPanelData(MapCard.MapDetailsData data)
    {
        if (movementBackgroundImage != null)
            movementBackgroundImage.color = data.BackgroundColor;

        if (movementMapImage != null)
        {
            movementMapImage.sprite = data.MapSprite;
            movementMapImage.enabled = data.MapSprite != null;
        }

        if (movementMapNameText != null)
            movementMapNameText.text = GetMapName(data);

        if (movementDistanceText != null)
            movementDistanceText.text = data.Distance > 0 ? $"{data.Distance:N0}" : "0";

        if (movementRidersText != null)
            movementRidersText.text = data.RidersAmount > 0 ? $"{data.RidersAmount:N0}" : "0";

        if (movementWeatherText != null)
            movementWeatherText.text = FormatWeather(data.Weather);

        if (movementEntryCostText != null)
            movementEntryCostText.text = data.PlayCost > 0 ? $"{data.PlayCost:N0}" : "0";
    }

    private void ApplyMovementPanelTranslations()
    {
        if (LanguageManager.Instance == null || !LanguageManager.Instance.IsReady)
            return;

        SetLocalizedText(distaneTitle, 529);
        SetLocalizedText(ridersTitle, 226);
        SetLocalizedText(weatherTitle, 562);
        SetLocalizedText(entryCostTitle, 485);
    }

    private void SetLocalizedText(TMP_Text target, int textId)
    {
        if (target == null)
            return;

        string localized = LanguageManager.Instance.GetText(textId);
        if (!string.IsNullOrEmpty(localized))
            target.text = localized;
    }

    private void PlayMovementPanelAnimation()
    {
        StopMovementPanelAnimation();

        Transform content = movementContentRoot != null ? movementContentRoot : movementPanelRoot;
        if (content != null)
        {
            content.localScale = Vector3.one;
            CanvasGroup contentGroup = content.GetComponent<CanvasGroup>();
            if (contentGroup != null)
                contentGroup.alpha = 1f;
        }

        if (movementStaggerItems != null)
        {
            foreach (RectTransform item in movementStaggerItems)
            {
                if (item == null) continue;

                CanvasGroup group = item.GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 1f;
            }
        }

        if (movementBackgroundImage != null)
        {
            Color imageColor = movementBackgroundImage.color;
            if (imageColor.a <= 0f)
            {
                imageColor.a = 1f;
                movementBackgroundImage.color = imageColor;
            }
        }

        StartMovementPulse();
    }

    private void StartMovementPulse()
    {
        Transform mapTarget = movementMapImageRoot != null ? movementMapImageRoot : movementMapImage != null ? movementMapImage.transform : null;
        if (mapTarget == null)
            return;

        _movementPulseTween = mapTarget
            .DOScale(1.025f, 1.05f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this);
    }

    private void StopMovementPanelAnimation(bool resetScale = true)
    {
        _movementSeq?.Kill();
        _movementSeq = null;

        _movementPulseTween?.Kill();
        _movementPulseTween = null;

        if (!resetScale)
            return;

        Transform mapTarget = movementMapImageRoot != null ? movementMapImageRoot : movementMapImage != null ? movementMapImage.transform : null;
        if (mapTarget != null)
        {
            mapTarget.DOKill();
            mapTarget.localScale = Vector3.one;
        }
    }

    private void StartMovementSceneProgress()
    {
        StopTimeProgress();
        ApplyMovementTimeProgress(0f);
        _sceneProgressLoaded = false;

        if (SceneLoadManager.Instance != null)
        {
            _sceneProgressLoadedHandler = () => _sceneProgressLoaded = true;
            SceneLoadManager.Instance.OnSceneLoaded += _sceneProgressLoadedHandler;
        }

        _timeRoutine = StartCoroutine(MovementSceneProgressRoutine());
    }

    private IEnumerator MovementSceneProgressRoutine()
    {
        while (!_sceneProgressLoaded)
        {
            float normalized = SceneLoadManager.Instance != null
                ? Mathf.Clamp01(SceneLoadManager.Instance.loadingTime / 100f)
                : 0f;

            ApplyMovementTimeProgress(normalized);
            yield return null;
        }

        ApplyMovementTimeProgress(1f);
        ClearSceneProgressSubscription();
        _timeRoutine = null;
        HideMovementPanel(false);
    }

    private void ApplyMovementTimeProgress(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        int percent = Mathf.Clamp(Mathf.CeilToInt(normalized * 100f), 1, 100);

        if (movementTimeSlider != null)
        {
            movementTimeSlider.minValue = 1f;
            movementTimeSlider.maxValue = 100f;
            movementTimeSlider.value = percent;
        }

        if (movementTimeProgressBar != null)
        {
            movementTimeProgressBar.currentPercent = percent;
            movementTimeProgressBar.UpdateUI();
        }

        if (movementTimeText != null)
            movementTimeText.text = $"{percent}%";
    }
    private string GetMapName(MapCard.MapDetailsData data)
    {
        if (LanguageManager.Instance != null && LanguageManager.Instance.IsReady && data.MapLangCode >= 0)
        {
            string localized = LanguageManager.Instance.GetText(data.MapLangCode);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return string.IsNullOrWhiteSpace(data.MapKey) ? data.MovingRoom.ToString() : data.MapKey;
    }

    private string FormatWeather(MapCard.MapWeather weather)
    {
        return weather.ToString();
    }


    private void ShowFallbackMovementPanel(MapCard.MapDetailsData data, bool instant, bool exclusive)
    {
        switch (data.MovingRoom)
        {
            case SceneLoadManager.SceneType.TrainingRacing:
                ShowPanel(UIPanelType.RacingTutorial, data.MovingRoom.ToString(), instant, exclusive, trackSceneProgress: true);
                break;
            case SceneLoadManager.SceneType.SecondRacing:
                ShowPanel(UIPanelType.Zarafshan, data.MovingRoom.ToString(), instant, exclusive, trackSceneProgress: true);
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                ShowPanel(UIPanelType.Egypt, data.MovingRoom.ToString(), instant, exclusive, trackSceneProgress: true);
                break;
            case SceneLoadManager.SceneType.Kansas:
                ShowPanel(UIPanelType.Kansas, data.MovingRoom.ToString(), instant, exclusive, trackSceneProgress: true);
                break;
            default:
                ShowLoading(instant);
                break;
        }
    }


    /// <summary>
    /// Bitta panelni ko'rsatadi. Agar exclusive=true bo'lsa, boshqalar yopiladi.
    /// </summary>
    public void ShowPanel(UIPanelType type, string message, bool instant = false, bool exclusive = true, float time = 0f, bool trackSceneProgress = false)
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
        StartTimeProgress(target, time, trackSceneProgress);

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

        StopTimeProgress();
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
        ApplyTimeProgress(p, 0f, 0f, false);
    }

    private void KillTweens()
    {
        StopMovementPanelAnimation();
        _seq?.Kill();
        foreach (var kv in _map)
        {
            var p = kv.Value;
            if (p?.root != null) p.root.DOKill();
            if (p?.group != null) p.group.DOKill();
        }
    }

    private void StartTimeProgress(PanelEntry panel, float time, bool trackSceneProgress)
    {
        StopTimeProgress();
        ApplyTimeProgress(panel, 0f, time, false);

        if (panel == null)
            return;

        if (trackSceneProgress)
        {
            _sceneProgressLoaded = false;
            if (SceneLoadManager.Instance != null)
            {
                _sceneProgressLoadedHandler = () => _sceneProgressLoaded = true;
                SceneLoadManager.Instance.OnSceneLoaded += _sceneProgressLoadedHandler;
            }

            _timeRoutine = StartCoroutine(SceneProgressRoutine(panel));
            return;
        }

        if (time <= 0f)
            return;

        _timeRoutine = StartCoroutine(TimeProgressRoutine(panel, time));
    }

    private void StopTimeProgress()
    {
        ClearSceneProgressSubscription();

        if (_timeRoutine == null) return;

        StopCoroutine(_timeRoutine);
        _timeRoutine = null;
    }

    private void ClearSceneProgressSubscription()
    {
        if (_sceneProgressLoadedHandler == null || SceneLoadManager.Instance == null) return;

        SceneLoadManager.Instance.OnSceneLoaded -= _sceneProgressLoadedHandler;
        _sceneProgressLoadedHandler = null;
    }

    private IEnumerator TimeProgressRoutine(PanelEntry panel, float time)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / time);
            ApplyTimeProgress(panel, normalized, Mathf.Max(0f, time - elapsed), false);
            yield return null;
        }

        ApplyTimeProgress(panel, 1f, 0f, false);
        _timeRoutine = null;
    }

    private IEnumerator SceneProgressRoutine(PanelEntry panel)
    {
        while (!_sceneProgressLoaded)
        {
            float normalized = SceneLoadManager.Instance != null
                ? Mathf.Clamp01(SceneLoadManager.Instance.loadingTime / 100f)
                : 0f;

            float elapsed = SceneLoadManager.Instance != null
                ? SceneLoadManager.Instance.CurrentSceneMoveTime
                : 0f;

            ApplyTimeProgress(panel, normalized, elapsed, true);
            yield return null;
        }

        float finalTime = SceneLoadManager.Instance != null
            ? SceneLoadManager.Instance.LastSceneMoveTime
            : 0f;

        ApplyTimeProgress(panel, 1f, finalTime, true);
        ClearSceneProgressSubscription();
        _timeRoutine = null;
    }

    private void ApplyTimeProgress(PanelEntry panel, float normalized, float displayTime, bool showElapsedTime)
    {
        if (panel == null) return;

        normalized = Mathf.Clamp01(normalized);

        if (panel.timeSlider != null)
        {
            panel.timeSlider.minValue = 0f;
            panel.timeSlider.maxValue = 1f;
            panel.timeSlider.value = normalized;
        }

        if (panel.timeProgressBar != null)
        {
            panel.timeProgressBar.currentPercent = normalized * 100f;
            panel.timeProgressBar.UpdateUI();
        }

        if (panel.timeText != null)
        {
            if (showElapsedTime)
            {
                int percent = Mathf.Clamp(Mathf.CeilToInt(normalized * 100f), 1, 100);
                panel.timeText.text = $"{percent}%";
            }
            else if (displayTime <= 0f)
            {
                panel.timeText.text = string.Empty;
            }
            else
            {
                panel.timeText.text = $"{displayTime:0.0}s";
            }
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

    public void Done(string title, string desc, string doneText,
        Action onDone,
        ConfirmationPopupController.Options options = null)
    {
        Enqueue(new DoneTextRequest(title, desc, doneText, onDone, options));
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

    private class DoneTextRequest : IRequest
    {
        private readonly string _title, _desc, _doneText;
        private readonly Action _onDone;
        private readonly ConfirmationPopupController.Options _options;

        public DoneTextRequest(string title, string desc, string doneText,
            Action onDone,
            ConfirmationPopupController.Options options)
        {
            _title = title;
            _desc = desc;
            _doneText = doneText;
            _onDone = onDone;
            _options = options;
        }

        public void Show(ConfirmationPopupController popup, Action onClosed)
        {
            popup.Show(
                _title, _desc, _doneText,
                onDone: () => { _onDone?.Invoke(); onClosed?.Invoke(); },
                options: _options
            );
        }
    }
    #endregion
}
