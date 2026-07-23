using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketItemCardUI : MonoBehaviour
{
    public enum MarketItems
    {   
        None,
        Coins,
        Foods,
        Supplies,
        Skins
    }

    public MarketItems marketItems = MarketItems.None;
    [SerializeField] private IapPurchaseManager.CurrencyBundleProduct currencyBundleProduct =
        IapPurchaseManager.CurrencyBundleProduct.None;
    [SerializeField] private int firstItemAmount;//if Market Items == Coins // Coins
    [SerializeField] private int secondItemAmount; // Nyufiys
    [SerializeField] private float costAmount;

    [SerializeField] private TMP_Text firstItemText;
    [SerializeField] private TMP_Text secondItemText;
    [SerializeField] private TMP_Text topCornerText;

    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyBtn;

    private bool purchasePending;

    private void OnEnable()
    {
        purchasePending = false;
        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveListener(BuyAction);
            buyBtn.onClick.AddListener(BuyAction);
        }

        SubscribeToIapEvents();

        AllTexts(marketItems);
        RefreshBuyButton();
    }
    private void OnDisable()
    {
        purchasePending = false;

        if (buyBtn != null)
            buyBtn.onClick.RemoveListener(BuyAction);

        UnsubscribeFromIapEvents();
    }

    private void AllTexts(MarketItems items)
    {
        switch (items)
        {
            case MarketItems.Coins:
                GetCurrencyBundleAmounts(out int coinAmount, out int nyufiyAmount);
                SetText(firstItemText, $"+{coinAmount:N0}");
                SetText(secondItemText, $"+{nyufiyAmount:N0}");
                SetText(costText, "$" + costAmount.ToString("0.##", CultureInfo.InvariantCulture));
                SetText(topCornerText, LanguageManager.Instance?.GetText(411));
                break;
            case MarketItems.Foods:
                break;
            case MarketItems.Supplies:
                break;
            case MarketItems.Skins: 
                break;
        }

    }

    private void BuyAction()
    {
        switch (marketItems)
        {
            case MarketItems.Coins:
                BuyCurrencyBundle();
                break;
        }
    }

    private void BuyCurrencyBundle()
    {
        if (currencyBundleProduct == IapPurchaseManager.CurrencyBundleProduct.None)
        {
            Debug.LogWarning("Currency bundle product is not assigned on this market card.", this);
            return;
        }

        if (IapPurchaseManager.Instance == null)
        {
            Debug.LogWarning("IAP manager is missing from the scene.", this);
            return;
        }

        if (buyBtn != null)
            buyBtn.interactable = false;

        purchasePending = true;
        IapPurchaseManager.Instance.BuyCurrencyBundle(currencyBundleProduct);
    }

    private void SubscribeToIapEvents()
    {
        if (IapPurchaseManager.Instance == null)
            return;

        IapPurchaseManager.Instance.OnCurrencyBundlePurchaseSucceeded -= HandleBundlePurchaseSucceeded;
        IapPurchaseManager.Instance.OnCurrencyBundlePurchaseSucceeded += HandleBundlePurchaseSucceeded;
        IapPurchaseManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
        IapPurchaseManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
    }

    private void UnsubscribeFromIapEvents()
    {
        if (IapPurchaseManager.Instance == null)
            return;

        IapPurchaseManager.Instance.OnCurrencyBundlePurchaseSucceeded -= HandleBundlePurchaseSucceeded;
        IapPurchaseManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
    }

    private void HandleBundlePurchaseSucceeded(string productId, int coinAmount, int nyufiyAmount)
    {
        if (!IsThisProduct(productId))
            return;

        if (buyBtn != null)
            buyBtn.interactable = true;

        purchasePending = false;
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        Debug.Log($"Currency bundle granted: +{coinAmount} Coin, +{nyufiyAmount} Nyufiy.", this);
    }

    private void HandlePurchaseFailed(string productId, string reason)
    {
        if (!purchasePending)
            return;

        if (!string.IsNullOrEmpty(productId) && !IsThisProduct(productId))
            return;

        if (buyBtn != null)
            buyBtn.interactable = true;

        purchasePending = false;
        HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
        Debug.LogWarning($"Currency bundle purchase failed: {reason}", this);
    }

    private bool IsThisProduct(string productId)
    {
        return IapPurchaseManager.Instance != null
            && productId == IapPurchaseManager.Instance.GetCurrencyBundleProductId(currencyBundleProduct);
    }

    private void GetCurrencyBundleAmounts(out int coinAmount, out int nyufiyAmount)
    {
        if (IapPurchaseManager.Instance != null
            && IapPurchaseManager.Instance.TryGetCurrencyBundleReward(currencyBundleProduct, out var reward))
        {
            coinAmount = reward.CoinAmount;
            nyufiyAmount = reward.NyufiyAmount;
            return;
        }

        // Inspector values remain a safe preview fallback when the IAP manager is unavailable.
        coinAmount = Mathf.Max(0, firstItemAmount);
        nyufiyAmount = Mathf.Max(0, secondItemAmount);
    }

    private void RefreshBuyButton()
    {
        if (buyBtn != null)
        {
            bool currencyBundleReady = marketItems == MarketItems.Coins
                && currencyBundleProduct != IapPurchaseManager.CurrencyBundleProduct.None;
            buyBtn.interactable = !purchasePending && currencyBundleReady;
        }

    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

}
