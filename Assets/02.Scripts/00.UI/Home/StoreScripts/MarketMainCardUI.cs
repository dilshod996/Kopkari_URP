using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketMainCardUI : MonoBehaviour
{
    public enum MainCardType
    {
        None,
        NomadicStarter,
        NomadicExplorer,
        Warrior,
        Legend
    }
    public MainCardType cardType = MainCardType.None;

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text bestValueText;
    [SerializeField] private TMP_Text discountAmountText;
    [SerializeField] private TMP_Text firstItemText;
    [SerializeField] private TMP_Text secondItemText;
    [SerializeField] private TMP_Text thirdItemText;

    [SerializeField] private TMP_Text buyText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;

    [SerializeField] private int firstItemAmount;
    [SerializeField] private int secondItemAmount;
    [SerializeField] private int thirdItemAmount;
    [SerializeField] private float buyAmount;
    [SerializeField] private int discountAmount;

    private void OnEnable()
    {
        CardDetails(cardType);
        buyButton.onClick.AddListener(()=>BuyAction(cardType));
    }
    private void OnDisable()
    {
        buyButton.onClick.RemoveAllListeners();
    }
    private void CardDetails(MainCardType cardType)
    {

        var language = LanguageManager.Instance;
        if (language == null) return;
        buyText.text = language.GetText(424);
        switch(cardType)
        {
            case MainCardType.NomadicStarter:
                title.text = language.GetText(416);
                bestValueText.text = language.GetText(419); //Best Value
                firstItemText.text = $"+{firstItemAmount} {language.GetText(390)}"; //coins
                secondItemText.text = $"+{language.GetText(323)} {secondItemAmount}X"; //walkTrap
                thirdItemText.text = $"+{language.GetText(108)} {thirdItemAmount}X"; // bugdoy
                discountAmountText.text = $"-{discountAmount}%";
                costText.text = $"${buyAmount}";
                break;
            case MainCardType.NomadicExplorer:
                title.text = language.GetText(417);
                bestValueText.text = language.GetText(421); //Limited
                firstItemText.text = $"+{firstItemAmount} {language.GetText(390)}"; //coins
                secondItemText.text = $"+{language.GetText(322)} {secondItemAmount}X"; //Web snare
                thirdItemText.text = $"+{language.GetText(110)} {thirdItemAmount}X"; // olma
                discountAmountText.text = $"-{discountAmount}%";
                costText.text = $"${buyAmount}";
                break;
            case MainCardType.Warrior:
                title.text = language.GetText(418);
                bestValueText.text = language.GetText(420); // most popular
                firstItemText.text = $"+{firstItemAmount} {language.GetText(390)}"; //coins
                secondItemText.text = $"+{language.GetText(324)} {secondItemAmount}X"; //Defense
                thirdItemText.text = $"+{language.GetText(109)} {thirdItemAmount}X"; // arpa
                discountAmountText.text = $"-{discountAmount}%";
                costText.text = $"${buyAmount}";
                break;
            case MainCardType.Legend:
                title.text = language.GetText(423);
                bestValueText.text = language.GetText(422); // Hot Deal
                firstItemText.text = $"+{firstItemAmount} {language.GetText(390)}"; //coins
                secondItemText.text = $"+{language.GetText(324)} {secondItemAmount}X"; //Defense
                thirdItemText.text = $"+{language.GetText(112)} {thirdItemAmount}X"; // stamin Water
                discountAmountText.text = $"-{discountAmount}%";
                costText.text = $"${buyAmount}";
                break;
        }
    }
    private void BuyAction(MainCardType type)
    {
        switch(type)
        {
            case MainCardType.NomadicStarter:
                break;
        }
    }

    
}
