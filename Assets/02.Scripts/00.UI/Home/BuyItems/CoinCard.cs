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
        Korak,
        Nyufiy
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
        //        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";

    }
    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(BuyCoins);
    }
    private void AmountDatas()
    {
        mainAmount.text = mainAmountNum > 0 ? $"+{mainAmountNum:N0}" : "0";
        bonusAmount.text = bonusAmountNum > 0 ? $"+{bonusAmountNum:N0}" : "0";
        if(coinType == CoinType.Korak)
            cost.text = costNum > 0 ? $"{costNum:N0}" : "0";
        else
        {
            cost.text = $"${costNum}";
        }
    }

    private void BuyCoins()
    {
        if(coinType != CoinType.Korak)
        {
            // buy for actual money
            Debug.Log("You are clicking nyufiy");
        }
        else
        {
            int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy,0);
            if(costNum>nyufiyAmount)
            {
                // Demak nyufiy yetarli emas
               // Debug.Log("Nyufiy yetarli emasss");
                OnMoneyNotEnough?.Invoke();
            }
            else
            {
                int qorakAmount = PlayerPrefs.GetInt(Constants.Coins.Coin,0);
                nyufiyAmount -= (int)costNum;
                qorakAmount = qorakAmount + mainAmountNum + bonusAmountNum;
                PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
                PlayerPrefs.SetInt(Constants.Coins.Coin , qorakAmount);
                //HomeMainUI.Instance.UpdateNyufiy();
                string amount = $"{mainAmountNum + bonusAmountNum}";
                HomeMainUI.Instance.DisplayAutoReward(mainImage.sprite, LanguageManager.Instance.GetText(409), amount, LanguageManager.Instance.GetText(390));
                HomeMainUI.Instance.CloseCoinsPage();
            }
        }
    }
}
