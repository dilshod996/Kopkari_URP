using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceInfoDetailsPopup : MonoBehaviour
{
    public static ResourceInfoDetailsPopup Instance { get; private set; }
    public event Action<DetailsMode, bool> DetailsShown;
    public event Action<DetailsMode> DetailsClosed;

    private const int BuyButtonTextId = 424;
    private const int CloseButtonTextId = 137;
    private const int CostTextId = 548;
    private const int NotEnoughNyufiyTextId = 363;
    private const int RewardAdNyufiyAmount = 300;
    private const int FoodPopupTitleTextId = 559;
    private const int PlayerResourcePopupTitleTextId = 549;
    private const int PlayerTypeTextId = 558;
    private const int PlayerTypeValueTextId = 549;
    private const int PlayerEffectTextId = 550;
    private const int PlayerOwnedTextId = 551;
    private const int HorseStaminaTextId = 328;
    private const int HorseCoolingTextId = 327;
    private const int HorsePowerTextId = 326;

    public enum DetailsMode
    {
        PlayerResource,
        HorseResource
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image backgroundImage;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;

    [Header("Resource")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text costTitle;
    [SerializeField] private TMP_Text costText2;

    [Header("Player Details")]
    [SerializeField] private GameObject playerDetailsRoot;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text typeNameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text effectNameText;
    [SerializeField] private TMP_Text ownedText;
    [SerializeField] private TMP_Text ownedNameText;

    [Header("Horse Details")]
    [SerializeField] private GameObject horseDetailsRoot;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text staminaValueText;
    [SerializeField] private TMP_Text coolingText;
    [SerializeField] private TMP_Text coolingValueText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text powerValueText;

    [Header("Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text closeButtonText;

    [Header("Not Enough Nyufiy")]
    [SerializeField] private GameObject notEnoughNyufiyPanel;
    [SerializeField] private TMP_Text notEnoughNyufiyText;
    [SerializeField] private Button rewardAdsButton;
    [SerializeField] private TMP_Text rewardAdsButtonText;
    [SerializeField] private int rewardAdsNyufiyAmount = RewardAdNyufiyAmount;

    private Action onBuy;
    private Action onClose;
    private DetailsMode currentMode;
    private bool hasOpenDetails;
    private Tween rewardAdsButtonTween;
    private Vector3 rewardAdsButtonBaseScale = Vector3.one;
    private bool hasRewardAdsButtonBaseScale;

    public readonly struct ResourceDetails
    {
        public readonly DetailsMode Mode;
        public readonly string Name;
        public readonly Sprite Icon;
        public readonly int Cost;
        public readonly Color BackgroundColor;

        public readonly string EffectValue;
        public readonly string OwnedValue;

        public readonly string StaminaValue;
        public readonly string CoolingValue;
        public readonly string PowerValue;

        private ResourceDetails(
            DetailsMode mode,
            string name,
            Sprite icon,
            int cost,
            Color backgroundColor,
            string effectValue,
            string ownedValue,
            string staminaValue,
            string coolingValue,
            string powerValue)
        {
            Mode = mode;
            Name = name;
            Icon = icon;
            Cost = cost;
            BackgroundColor = backgroundColor;
            EffectValue = effectValue;
            OwnedValue = ownedValue;
            StaminaValue = staminaValue;
            CoolingValue = coolingValue;
            PowerValue = powerValue;
        }

        public static ResourceDetails Player(
            string name,
            Sprite icon,
            int cost,
            Color backgroundColor,
            string effectValue,
            string ownedValue)
        {
            return new ResourceDetails(
                DetailsMode.PlayerResource,
                name,
                icon,
                cost,
                backgroundColor,
                effectValue,
                ownedValue,
                "",
                "",
                "");
        }

        public static ResourceDetails Horse(
            string name,
            Sprite icon,
            int cost,
            Color backgroundColor,
            string staminaValue,
            string coolingValue,
            string powerValue)
        {
            return new ResourceDetails(
                DetailsMode.HorseResource,
                name,
                icon,
                cost,
                backgroundColor,
                "",
                "",
                staminaValue,
                coolingValue,
                powerValue);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[ResourceInfoDetailsPopup] Duplicate popup instance found.");

        Instance = this;

        if (root == null)
            root = gameObject;

        buyButton?.onClick.AddListener(HandleBuy);
        closeButton?.onClick.AddListener(HandleClose);
        rewardAdsButton?.onClick.AddListener(HandleRewardAds);
        CloseImmediate();
    }

    private void OnDestroy()
    {
        buyButton?.onClick.RemoveListener(HandleBuy);
        closeButton?.onClick.RemoveListener(HandleClose);
        rewardAdsButton?.onClick.RemoveListener(HandleRewardAds);
        StopRewardAdsButtonAnimation(true);

        if (Instance == this)
            Instance = null;
    }

    public void Show(ResourceDetails details, bool canBuy, Action buyAction, Action closeAction = null)
    {
        onBuy = buyAction;
        onClose = closeAction;
        currentMode = details.Mode;
        hasOpenDetails = true;

        if (iconImage != null)
            iconImage.sprite = details.Icon;

        if (backgroundImage != null)
            backgroundImage.color = details.BackgroundColor;

        SetText(nameText, details.Name);
        SetText(titleText, GetPopupTitle(details.Mode));
        SetText(costText, details.Cost > 0 ? $"{details.Cost:N0}" : "0");
        SetText(costText2, details.Cost > 0 ? $"{details.Cost:N0}" : "0");
        SetText(costTitle, GetLocalizedText(CostTextId, "Cost"));
        SetText(buyButtonText, GetLocalizedText(BuyButtonTextId, "Buy"));
        SetText(closeButtonText, GetLocalizedText(CloseButtonTextId, "Close"));
        SetText(rewardAdsButtonText, $"+{rewardAdsNyufiyAmount:N0}");
        HideNotEnoughNyufiy();
        ApplyMode(details);

        if (buyButton != null)
            buyButton.interactable = canBuy;

        root.SetActive(true);
        DetailsShown?.Invoke(details.Mode, canBuy);
    }

    public void ShowNotEnoughNyufiy()
    {
        string message = GetLocalizedText(NotEnoughNyufiyTextId, "Not enough Nyufiy.");

        if (notEnoughNyufiyPanel == null)
            SetText(nameText, message);

        SetText(notEnoughNyufiyText, message);
        SetText(rewardAdsButtonText, $"+{rewardAdsNyufiyAmount:N0}");
        SetActive(notEnoughNyufiyPanel, true);
        StartRewardAdsButtonAnimation();
    }

    public void Close()
    {
        bool notifyClosed = hasOpenDetails;
        DetailsMode closedMode = currentMode;
        onClose?.Invoke();
        CloseImmediate();

        if (notifyClosed)
            DetailsClosed?.Invoke(closedMode);
    }

    private void HandleBuy()
    {
        HideNotEnoughNyufiy();
        onBuy?.Invoke();
    }

    private void HandleRewardAds()
    {
        GameAnalyticsEvents.RewardedAdClicked(
            placement: "resource_details_popup",
            rewardType: "nyufiy",
            rewardAmount: rewardAdsNyufiyAmount
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("resource_details_popup");
            return;
        }

        if (rewardAdsButton != null)
            rewardAdsButton.interactable = false;

        StopRewardAdsButtonAnimation(true);

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance?.AddNyufiy(rewardAdsNyufiyAmount, true);

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "resource_details_popup",
                rewardType: "nyufiy",
                rewardAmount: rewardAdsNyufiyAmount
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_resource_details_popup",
                amount: rewardAdsNyufiyAmount
            );

            HideNotEnoughNyufiy();
        },
        () =>
        {
            GameAnalyticsEvents.RewardedAdFailed("resource_details_popup");

            if (rewardAdsButton != null)
                rewardAdsButton.interactable = true;

            StartRewardAdsButtonAnimation();
        });
    }

    private void HandleClose()
    {
        Close();
    }

    private void CloseImmediate()
    {
        HideNotEnoughNyufiy();
        hasOpenDetails = false;

        if (root != null)
            root.SetActive(false);
    }

    private void HideNotEnoughNyufiy()
    {
        StopRewardAdsButtonAnimation(true);
        SetActive(notEnoughNyufiyPanel, false);

        if (rewardAdsButton != null)
            rewardAdsButton.interactable = true;
    }

    private void StartRewardAdsButtonAnimation()
    {
        if (rewardAdsButton == null)
            return;

        Transform target = rewardAdsButton.transform;
        if (!hasRewardAdsButtonBaseScale)
        {
            rewardAdsButtonBaseScale = target.localScale;
            hasRewardAdsButtonBaseScale = true;
        }

        rewardAdsButtonTween?.Kill(false);
        target.localScale = rewardAdsButtonBaseScale;
        rewardAdsButtonTween = target
            .DOScale(rewardAdsButtonBaseScale * 1.08f, 0.45f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopRewardAdsButtonAnimation(bool resetScale)
    {
        rewardAdsButtonTween?.Kill(false);
        rewardAdsButtonTween = null;

        if (resetScale && hasRewardAdsButtonBaseScale && rewardAdsButton != null)
            rewardAdsButton.transform.localScale = rewardAdsButtonBaseScale;
    }

    private void ApplyMode(ResourceDetails details)
    {
        bool isPlayer = details.Mode == DetailsMode.PlayerResource;
        SetActive(playerDetailsRoot, isPlayer);
        SetActive(horseDetailsRoot, !isPlayer);

        if (isPlayer)
        {
            SetText(typeText, GetLocalizedTextCustom(PlayerTypeTextId, "Type:"));
            SetText(typeNameText, GetLocalizedText(PlayerTypeValueTextId, ""));
            SetText(effectText, GetLocalizedTextCustom(PlayerEffectTextId, "Effect"));
            SetText(effectNameText, details.EffectValue);
            SetText(ownedText, GetLocalizedTextCustom(PlayerOwnedTextId, "Owned"));
            SetText(ownedNameText, details.OwnedValue);
        }
        else
        {
            SetText(staminaText, GetLocalizedText(HorseStaminaTextId, "Stamina"));
            SetText(staminaValueText, details.StaminaValue);
            SetText(coolingText, GetLocalizedText(HorseCoolingTextId, "Cooling"));
            SetText(coolingValueText, details.CoolingValue);
            SetText(powerText, GetLocalizedText(HorsePowerTextId, "Power"));
            SetText(powerValueText, details.PowerValue);
        }
    }

    private string GetPopupTitle(DetailsMode mode)
    {
        return mode == DetailsMode.HorseResource
            ? GetLocalizedText(FoodPopupTitleTextId, "Food")
            : GetLocalizedText(PlayerResourcePopupTitleTextId, "Resource");
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private string GetLocalizedText(int id, string fallback)
    {
        if (id == -1 || LanguageManager.Instance == null)
            return fallback;

        string localized = LanguageManager.Instance.GetText(id);
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
    private string GetLocalizedTextCustom(int id, string fallback)
    {
        if (id == -1 || LanguageManager.Instance == null)
            return fallback;

        string localized = LanguageManager.Instance.GetText(id) + ":";
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
}
