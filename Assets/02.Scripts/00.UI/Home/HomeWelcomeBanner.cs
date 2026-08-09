using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeWelcomeBanner : MonoBehaviour
{
    [Serializable]
    private sealed class LocalizedMessageIds
    {
        [SerializeField] private int titleTextId = -1;
        [SerializeField] private int detailsTextId = -1;

        public int TitleTextId => titleTextId;
        public int DetailsTextId => detailsTextId;
    }

    public readonly struct LocalizedMessage
    {
        public LocalizedMessage(string title, string details)
        {
            Title = title;
            Details = details;
        }

        public string Title { get; }
        public string Details { get; }
    }

    public enum MessageMode
    {
        Welcome,
        Toast
    }

    public enum MessagePriority
    {
        Low = 0,
        Normal = 10,
        High = 20
    }

    public static HomeWelcomeBanner Instance { get; private set; }

    [Header("Banner References")]
    [Tooltip("Keep this controller active and assign a child object as the visual root.")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image accentImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("Mode Appearance")]
    [SerializeField] private Color welcomeAccentColor = new Color32(238, 211, 102, 255);
    [SerializeField] private Color toastAccentColor = new Color32(41, 203, 255, 255);

    [Header("Localization IDs")]
    [SerializeField] private LocalizedMessageIds introWelcomeIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds fullyRecoveredIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds needsFoodIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds needsWaterIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds needsRecoveryIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds readyToRaceIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds avatarReturnIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds kopkariReturnIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds zarafshanReturnIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds egyptReturnIds = new LocalizedMessageIds();
    [SerializeField] private LocalizedMessageIds kansasReturnIds = new LocalizedMessageIds();
    [SerializeField] private int playButtonTextId = -1;
    [SerializeField] private int conditionButtonTextId = -1;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float welcomeDuration = 6f;
    [SerializeField, Min(0f)] private float toastDuration = 2.4f;
    [SerializeField, Min(0f)] private float showDuration = 0.35f;
    [SerializeField, Min(0f)] private float hideDuration = 0.25f;

    [Header("Animation")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, 140f);
    [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.96f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    public bool IsShowing => currentRequest != null;
    public int PendingCount => pendingRequests.Count;
    public string PlayButtonText => GetLocalizedText(playButtonTextId, "Play");
    public string ConditionButtonText => GetLocalizedText(conditionButtonTextId, "Condition");

    public LocalizedMessage GetIntroWelcomeMessage(string playerName)
    {
        return GetLocalizedMessage(introWelcomeIds, "Welcome Back,", playerName, playerName);
    }

    public LocalizedMessage GetFullyRecoveredMessage(string horseName)
    {
        return GetLocalizedMessage(fullyRecoveredIds, GetHorseTitleFallback(horseName), "Your horse is fully recovered.", horseName);
    }

    public LocalizedMessage GetNeedsFoodMessage(string horseName)
    {
        return GetLocalizedMessage(needsFoodIds, GetHorseTitleFallback(horseName), "Your horse needs food before racing.", horseName);
    }

    public LocalizedMessage GetNeedsWaterMessage(string horseName)
    {
        return GetLocalizedMessage(needsWaterIds, GetHorseTitleFallback(horseName), "Your horse needs water.", horseName);
    }

    public LocalizedMessage GetNeedsRecoveryMessage(string horseName)
    {
        return GetLocalizedMessage(needsRecoveryIds, GetHorseTitleFallback(horseName), "Your horse needs more recovery.", horseName);
    }

    public LocalizedMessage GetReadyToRaceMessage(string horseName)
    {
        return GetLocalizedMessage(readyToRaceIds, GetHorseTitleFallback(horseName), "Your horse is ready to race.", horseName);
    }

    public LocalizedMessage GetAvatarReturnMessage()
    {
        return GetLocalizedMessage(avatarReturnIds, "Appearance Updated", "Your changes were saved.");
    }

    public LocalizedMessage GetKopkariReturnMessage()
    {
        return GetLocalizedMessage(kopkariReturnIds, "Back from Kopkari", "Check your horse before playing again.");
    }

    public LocalizedMessage GetZarafshanReturnMessage()
    {
        return GetLocalizedMessage(zarafshanReturnIds, "Back from Zarafshan", "Check your horse before the next race.");
    }

    public LocalizedMessage GetEgyptReturnMessage()
    {
        return GetLocalizedMessage(egyptReturnIds, "Back from Egypt", "Check your horse before the next race.");
    }

    public LocalizedMessage GetKansasReturnMessage()
    {
        return GetLocalizedMessage(kansasReturnIds, "Back from Kansas", "Check your horse before the next race.");
    }

    public event Action<MessageMode> MessageShown;
    public event Action<MessageMode> MessageHidden;

    private sealed class BannerRequest
    {
        public MessageMode Mode;
        public MessagePriority Priority;
        public string Title;
        public string Message;
        public Sprite Icon;
        public float Duration;
        public long Order;
        public string ActionLabel;
        public Action OnAction;
    }

    private readonly List<BannerRequest> pendingRequests = new List<BannerRequest>();
    private BannerRequest currentRequest;
    private Sequence activeSequence;
    private Vector2 shownPosition;
    private long nextOrder;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{nameof(HomeWelcomeBanner)}] Duplicate instance found.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ResolveOptionalReferences();
        InitializeVisuals();
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(DismissCurrent);
        if (actionButton != null)
            actionButton.onClick.AddListener(HandleActionButtonClicked);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(DismissCurrent);
        if (actionButton != null)
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);

        KillActiveSequence();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowWelcome(
        string title,
        string message,
        Sprite icon = null,
        float duration = -1f,
        MessagePriority priority = MessagePriority.Normal,
        string actionLabel = null,
        Action onAction = null)
    {
        Enqueue(new BannerRequest
        {
            Mode = MessageMode.Welcome,
            Priority = priority,
            Title = title,
            Message = message,
            Icon = icon,
            Duration = duration >= 0f ? duration : welcomeDuration,
            Order = nextOrder++,
            ActionLabel = actionLabel,
            OnAction = onAction
        });
    }

    public void ShowToast(
        string message,
        Sprite icon = null,
        MessagePriority priority = MessagePriority.Normal,
        float duration = -1f)
    {
        Enqueue(new BannerRequest
        {
            Mode = MessageMode.Toast,
            Priority = priority,
            Title = string.Empty,
            Message = message,
            Icon = icon,
            Duration = duration >= 0f ? duration : toastDuration,
            Order = nextOrder++
        });
    }

    public void DismissCurrent()
    {
        if (currentRequest == null)
            return;

        PlayHide();
    }

    public void ClearPending()
    {
        pendingRequests.Clear();
    }

    public void HideAndClear()
    {
        pendingRequests.Clear();

        if (currentRequest != null)
            PlayHide();
        else
            SetHiddenImmediate();
    }

    private void Enqueue(BannerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return;

        pendingRequests.Add(request);
        pendingRequests.Sort(CompareRequests);

        if (currentRequest == null)
            PlayNext();
    }

    private static int CompareRequests(BannerRequest left, BannerRequest right)
    {
        int priorityComparison = right.Priority.CompareTo(left.Priority);
        return priorityComparison != 0
            ? priorityComparison
            : left.Order.CompareTo(right.Order);
    }

    private void PlayNext()
    {
        if (pendingRequests.Count == 0)
        {
            currentRequest = null;
            SetHiddenImmediate();
            return;
        }

        currentRequest = pendingRequests[0];
        pendingRequests.RemoveAt(0);

        ApplyContent(currentRequest);
        PlayShow(currentRequest);
    }

    private void ApplyContent(BannerRequest request)
    {
        bool isWelcome = request.Mode == MessageMode.Welcome;

        if (titleText != null)
        {
            titleText.gameObject.SetActive(isWelcome && !string.IsNullOrWhiteSpace(request.Title));
            titleText.text = request.Title ?? string.Empty;
        }

        if (messageText != null)
            messageText.text = request.Message;

        if (iconImage != null)
        {
            iconImage.sprite = request.Icon;
            iconImage.gameObject.SetActive(request.Icon != null);
        }

        if (accentImage != null)
            accentImage.color = isWelcome ? welcomeAccentColor : toastAccentColor;

        bool showAction = actionButton != null &&
                          request.OnAction != null &&
                          !string.IsNullOrWhiteSpace(request.ActionLabel);

        if (actionButton != null)
            actionButton.gameObject.SetActive(showAction);

        if (actionButtonText != null)
            actionButtonText.text = showAction ? request.ActionLabel : string.Empty;
    }

    private void PlayShow(BannerRequest request)
    {
        InitializeVisuals();
        KillActiveSequence();

        if (visualRoot != null)
            visualRoot.SetActive(true);

        bannerRect.anchoredPosition = shownPosition + hiddenOffset;
        bannerRect.localScale = Vector3.one * hiddenScale;
        canvasGroup.alpha = 0f;
        SetInteraction(false);

        activeSequence = DOTween.Sequence().SetUpdate(true);
        activeSequence.Join(bannerRect.DOAnchorPos(shownPosition, showDuration).SetEase(showEase));
        activeSequence.Join(bannerRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));
        activeSequence.Join(canvasGroup.DOFade(1f, Mathf.Min(showDuration, 0.2f)));
        activeSequence.AppendCallback(() =>
        {
            SetInteraction(true);
            MessageShown?.Invoke(request.Mode);
        });

        if (request.Duration > 0f)
        {
            activeSequence.AppendInterval(request.Duration);
            activeSequence.AppendCallback(PlayHide);
        }
    }

    private void PlayHide()
    {
        if (currentRequest == null)
            return;

        MessageMode hiddenMode = currentRequest.Mode;
        KillActiveSequence();
        SetInteraction(false);

        activeSequence = DOTween.Sequence().SetUpdate(true);
        activeSequence.Join(bannerRect.DOAnchorPos(shownPosition + hiddenOffset, hideDuration).SetEase(hideEase));
        activeSequence.Join(bannerRect.DOScale(hiddenScale, hideDuration).SetEase(Ease.InQuad));
        activeSequence.Join(canvasGroup.DOFade(0f, Mathf.Min(hideDuration, 0.18f)));
        activeSequence.OnComplete(() =>
        {
            currentRequest = null;
            activeSequence = null;

            if (visualRoot != null && visualRoot != gameObject)
                visualRoot.SetActive(false);

            MessageHidden?.Invoke(hiddenMode);
            PlayNext();
        });
    }

    private void InitializeVisuals()
    {
        if (initialized)
            return;

        if (bannerRect == null)
            bannerRect = visualRoot != null
                ? visualRoot.GetComponent<RectTransform>()
                : GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = visualRoot != null
                ? visualRoot.GetComponent<CanvasGroup>()
                : GetComponent<CanvasGroup>();

        if (bannerRect == null || canvasGroup == null)
        {
            Debug.LogError($"[{nameof(HomeWelcomeBanner)}] Banner RectTransform and CanvasGroup are required.", this);
            enabled = false;
            return;
        }

        shownPosition = bannerRect.anchoredPosition;
        initialized = true;
        SetHiddenImmediate();
    }

    private void ResolveOptionalReferences()
    {
        if (actionButton == null && visualRoot != null)
        {
            Button[] buttons = visualRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i] != closeButton)
                {
                    actionButton = buttons[i];
                    break;
                }
            }
        }

        if (actionButtonText == null && actionButton != null)
            actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void SetHiddenImmediate()
    {
        if (!initialized || bannerRect == null || canvasGroup == null)
            return;

        bannerRect.anchoredPosition = shownPosition + hiddenOffset;
        bannerRect.localScale = Vector3.one * hiddenScale;
        canvasGroup.alpha = 0f;
        SetInteraction(false);

        if (visualRoot != null && visualRoot != gameObject)
            visualRoot.SetActive(false);
    }

    private void SetInteraction(bool value)
    {
        if (canvasGroup == null)
            return;

        bool hasVisibleAction = actionButton != null && actionButton.gameObject.activeSelf;
        bool hasInteraction = value && (closeButton != null || hasVisibleAction);
        canvasGroup.interactable = hasInteraction;
        canvasGroup.blocksRaycasts = hasInteraction;
    }

    private void HandleActionButtonClicked()
    {
        Action action = currentRequest?.OnAction;
        DismissCurrent();
        action?.Invoke();
    }

    private static LocalizedMessage GetLocalizedMessage(
        LocalizedMessageIds ids,
        string fallbackTitle,
        string fallbackDetails,
        params object[] formatArgs)
    {
        if (ids == null)
            return new LocalizedMessage(fallbackTitle, fallbackDetails);

        return new LocalizedMessage(
            GetLocalizedText(ids.TitleTextId, fallbackTitle, formatArgs),
            GetLocalizedText(ids.DetailsTextId, fallbackDetails, formatArgs));
    }

    private static string GetHorseTitleFallback(string horseName)
    {
        return string.IsNullOrWhiteSpace(horseName) ? "Horse Condition" : horseName;
    }

    private static string GetLocalizedText(int textId, string fallback)
    {
        return GetLocalizedText(textId, fallback, Array.Empty<object>());
    }

    private static string GetLocalizedText(int textId, string fallback, params object[] formatArgs)
    {
        if (textId < 0 || LanguageManager.Instance == null)
            return fallback;

        string localized = formatArgs != null && formatArgs.Length > 0
            ? LanguageManager.Instance.GetText(textId, formatArgs)
            : LanguageManager.Instance.GetText(textId);
        return string.IsNullOrWhiteSpace(localized) ? fallback : localized;
    }

    private void KillActiveSequence()
    {
        activeSequence?.Kill(false);
        activeSequence = null;

        if (bannerRect != null)
            bannerRect.DOKill(false);
        if (canvasGroup != null)
            canvasGroup.DOKill(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (visualRoot == gameObject)
        {
            Debug.LogWarning(
                $"[{nameof(HomeWelcomeBanner)}] Use a child as Visual Root so the queue controller stays active while hidden.",
                this);
        }
    }
#endif
}
