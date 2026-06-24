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
    public void OnEnable()
    {
        if(LanguageManager.Instance != null)
        {
            bonusText.text = LanguageManager.Instance.GetText(403);
        }
        AmountDatas();
        buyButton.onClick.AddListener(BuyCoins);
        if (IapPurchaseManager.Instance != null)
        {
            IapPurchaseManager.Instance.OnNyufiyPurchaseSucceeded += HandleNyufiyPurchaseSucceeded;
            IapPurchaseManager.Instance.OnPurchaseFailed += HandleIapPurchaseFailed;
        }
        //        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";

    }
    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(BuyCoins);
        if (IapPurchaseManager.Instance != null)
        {
            IapPurchaseManager.Instance.OnNyufiyPurchaseSucceeded -= HandleNyufiyPurchaseSucceeded;
            IapPurchaseManager.Instance.OnPurchaseFailed -= HandleIapPurchaseFailed;
        }
    }
    private void AmountDatas()
    {
        if(coinType == CoinType.Coin)
        {
            int nyufiyCost = Mathf.RoundToInt(costNum);
            mainAmount.text = mainAmountNum > 0 ? $"+{mainAmountNum:N0}" : "0";
            bonusAmount.text = bonusAmountNum > 0 ? $"+{bonusAmountNum:N0}" : "0";
            cost.text = nyufiyCost > 0 ? $"{nyufiyCost:N0}" : "0";
        }
        else
        {
            int amount = GetNyufiyAmount();
            mainAmount.text = amount > 0 ? $"+{amount:N0}" : "0";
            bonusAmount.text = bonusAmountNum > 0 ? $"+{bonusAmountNum:N0}" : "0";
            cost.text = costNum % 1f == 0f ? $"${costNum:0}" : $"${costNum:0.##}";
        }
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

            buyButton.interactable = false;
            IapPurchaseManager.Instance.BuyNyufiy(nyufiyProduct);
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

        HomeMainUI.Instance.DisplayAutoReward(
            mainImage.sprite,
            LanguageManager.Instance.GetText(409),
            totalAmount.ToString(),
            LanguageManager.Instance.GetText(390)
        );

        HomeMainUI.Instance.CloseCoinsPage();
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

        buyButton.interactable = true;
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);

        HomeMainUI.Instance.DisplayAutoReward(
            mainImage.sprite,
            LanguageManager.Instance.GetText(409),
            amount.ToString(),
            LanguageManager.Instance.GetText(389)
        );

        HomeMainUI.Instance.CloseCoinsPage();
    }

    private void HandleIapPurchaseFailed(string productId, string reason)
    {
        if (coinType != CoinType.Nyufiy)
            return;

        if (!string.IsNullOrEmpty(productId) && productId != nyufiyProduct.ToString())
            return;

        buyButton.interactable = true;
        HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
        Debug.LogWarning($"Nyufiy purchase failed: {reason}");
    }
}
