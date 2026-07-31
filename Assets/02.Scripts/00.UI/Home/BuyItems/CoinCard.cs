using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinCard : MonoBehaviour
{
    public enum CoinType
    {
        Coin,
        Nyufiy
    }

    public enum NyufiyProduct
    {
        nyufiy_4350,
        nyufiy_7750,
        nyufiy_10350,
        nyufiy_15900,
        nyufiy_23600,
        nyufiy_38500,
        nyufiy_68000
    }

    [Header("UI Section")]
    [SerializeField] TMP_Text mainAmount;
    [SerializeField] Image mainImage;
    [SerializeField] TMP_Text bonusAmount;
    [SerializeField] TMP_Text bonusText;
    [SerializeField] TMP_Text cost;
    [SerializeField] private Button buyButton;
    [Header("Cost Nums")]
    [SerializeField] private int mainAmountNum;
    [SerializeField] private int bonusAmountNum;
    [SerializeField] private float costNum;
    [SerializeField] private NyufiyProduct nyufiyProduct = NyufiyProduct.nyufiy_4350;

    public CoinType coinType;
    public static event Action OnMoneyNotEnough;
    private bool purchaseInProgress;

    public void OnEnable()
    {
        if(LanguageManager.Instance != null && bonusText != null)
        {
            bonusText.text = LanguageManager.Instance.GetText(403);
        }
        AmountDatas();
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyCoins);
        if (IapPurchaseManager.Instance != null)
        {
            IapPurchaseManager.Instance.OnProductsUpdated += UpdateNyufiyPrice;
            IapPurchaseManager.Instance.OnNyufiyPurchaseSucceeded += HandleNyufiyPurchaseSucceeded;
            IapPurchaseManager.Instance.OnPurchaseFailed += HandleIapPurchaseFailed;
        }
        //        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";

    }
    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyCoins);
        if (IapPurchaseManager.Instance != null)
        {
            IapPurchaseManager.Instance.OnProductsUpdated -= UpdateNyufiyPrice;
            IapPurchaseManager.Instance.OnNyufiyPurchaseSucceeded -= HandleNyufiyPurchaseSucceeded;
            IapPurchaseManager.Instance.OnPurchaseFailed -= HandleIapPurchaseFailed;
        }
    }
    private void AmountDatas()
    {
        if(coinType == CoinType.Coin)
        {
            int nyufiyCost = Mathf.RoundToInt(costNum);
            if (mainAmount != null) mainAmount.text = mainAmountNum > 0 ? $"+{mainAmountNum:N0}" : "0";
            if (bonusAmount != null) bonusAmount.text = bonusAmountNum > 0 ? $"+{bonusAmountNum:N0}" : "0";
            if (cost != null) cost.text = nyufiyCost > 0 ? $"{nyufiyCost:N0}" : "0";
        }
        else
        {
            if (mainAmount != null) mainAmount.text = mainAmountNum > 0 ? $"+{mainAmountNum:N0}" : "0";
            if (bonusAmount != null) bonusAmount.text = bonusAmountNum > 0 ? $"+{bonusAmountNum:N0}" : "0";
            UpdateNyufiyPrice();
        }
    }

    private void UpdateNyufiyPrice()
    {
        if (coinType != CoinType.Nyufiy)
            return;

        string localizedPrice = string.Empty;
        bool priceAvailable =
            IapPurchaseManager.Instance != null &&
            IapPurchaseManager.Instance.TryGetLocalizedPrice(nyufiyProduct, out localizedPrice);

        if (cost != null)
            cost.text = priceAvailable ? localizedPrice : "...";

        if (buyButton != null)
            buyButton.interactable = priceAvailable && !purchaseInProgress;
    }

    private void BuyCoins()
    {
        if (coinType != CoinType.Coin)
        {
            if (IapPurchaseManager.Instance == null)
            {
                Debug.LogWarning("IAP manager is missing from the scene.");
                return;
            }

            purchaseInProgress = true;
            UpdateNyufiyPrice();
            IapPurchaseManager.Instance.BuyNyufiy(nyufiyProduct);
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("Currency manager is missing from the scene.");
            return;
        }

        int costAmount = Mathf.RoundToInt(costNum);
        bool success = CurrencyManager.Instance.SpendNyufiy(costAmount, true);

        if (!success)
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            OnMoneyNotEnough?.Invoke();
            return;
        }

        int totalAmount = mainAmountNum + bonusAmountNum;

        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        CurrencyManager.Instance.AddCoin(totalAmount, true);

        if (HomeMainUI.Instance != null)
        {
            HomeMainUI.Instance.DisplayAutoReward(
                mainImage != null ? mainImage.sprite : null,
                GetLocalizedText(409, "Reward"),
                totalAmount.ToString(),
                GetLocalizedText(390, "Coin")
            );

            HomeMainUI.Instance.CloseCoinsPage();
        }
    }

    private int GetNyufiyAmount()
    {
        if (IapPurchaseManager.Instance != null)
            return IapPurchaseManager.Instance.GetNyufiyAmount(nyufiyProduct);

        switch (nyufiyProduct)
        {
            case NyufiyProduct.nyufiy_4350:
                return 4350;
            case NyufiyProduct.nyufiy_7750:
                return 7750;
            case NyufiyProduct.nyufiy_10350:
                return 10350;
            case NyufiyProduct.nyufiy_15900:
                return 15900;
            case NyufiyProduct.nyufiy_23600:
                return 23600;
            case NyufiyProduct.nyufiy_38500:
                return 38500;
            case NyufiyProduct.nyufiy_68000:
                return 68000;
            default:
                return 0;
        }
    }

    private void HandleNyufiyPurchaseSucceeded(string productId, int amount)
    {
        if (coinType != CoinType.Nyufiy || productId != nyufiyProduct.ToString())
            return;

        purchaseInProgress = false;
        UpdateNyufiyPrice();
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);

        if (HomeMainUI.Instance != null)
        {
            HomeMainUI.Instance.DisplayAutoReward(
                mainImage != null ? mainImage.sprite : null,
                GetLocalizedText(409, "Reward"),
                amount.ToString(),
                GetLocalizedText(389, "Nyufiy")
            );

            HomeMainUI.Instance.CloseCoinsPage();
        }
    }

    private void HandleIapPurchaseFailed(string productId, string reason)
    {
        if (coinType != CoinType.Nyufiy)
            return;

        if (!string.IsNullOrEmpty(productId) && productId != nyufiyProduct.ToString())
            return;

        purchaseInProgress = false;
        UpdateNyufiyPrice();
        HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
        Debug.LogWarning($"Nyufiy purchase failed: {reason}");
    }

    private string GetLocalizedText(int id, string fallback)
    {
        return LanguageManager.Instance != null ? LanguageManager.Instance.GetText(id) : fallback;
    }
}
