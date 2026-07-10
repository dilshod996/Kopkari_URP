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
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyAction);
    }
    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyAction);
    }
    private void CardDetails(MainCardType cardType)
    {

        var language = LanguageManager.Instance;
        if (language == null) return;
        SetText(buyText, language.GetText(424));
        switch(cardType)
        {
            case MainCardType.NomadicStarter:
                SetText(title, language.GetText(416));
                SetText(bestValueText, language.GetText(419)); //Best Value
                SetText(firstItemText, $"+{firstItemAmount} {language.GetText(390)}"); //coins
                SetText(secondItemText, $"+{language.GetText(323)} {secondItemAmount}X"); //walkTrap
                SetText(thirdItemText, $"+{language.GetText(108)} {thirdItemAmount}X"); // bugdoy
                SetText(discountAmountText, $"-{discountAmount}%");
                SetText(costText, $"${buyAmount}");
                break;
            case MainCardType.NomadicExplorer:
                SetText(title, language.GetText(417));
                SetText(bestValueText, language.GetText(421)); //Limited
                SetText(firstItemText, $"+{firstItemAmount} {language.GetText(390)}"); //coins
                SetText(secondItemText, $"+{language.GetText(322)} {secondItemAmount}X"); //Web snare
                SetText(thirdItemText, $"+{language.GetText(110)} {thirdItemAmount}X"); // olma
                SetText(discountAmountText, $"-{discountAmount}%");
                SetText(costText, $"${buyAmount}");
                break;
            case MainCardType.Warrior:
                SetText(title, language.GetText(418));
                SetText(bestValueText, language.GetText(420)); // most popular
                SetText(firstItemText, $"+{firstItemAmount} {language.GetText(390)}"); //coins
                SetText(secondItemText, $"+{language.GetText(324)} {secondItemAmount}X"); //Defense
                SetText(thirdItemText, $"+{language.GetText(109)} {thirdItemAmount}X"); // arpa
                SetText(discountAmountText, $"-{discountAmount}%");
                SetText(costText, $"${buyAmount}");
                break;
            case MainCardType.Legend:
                SetText(title, language.GetText(423));
                SetText(bestValueText, language.GetText(422)); // Hot Deal
                SetText(firstItemText, $"+{firstItemAmount} {language.GetText(390)}"); //coins
                SetText(secondItemText, $"+{language.GetText(324)} {secondItemAmount}X"); //Defense
                SetText(thirdItemText, $"+{language.GetText(112)} {thirdItemAmount}X"); // stamin Water
                SetText(discountAmountText, $"-{discountAmount}%");
                SetText(costText, $"${buyAmount}");
                break;
        }
    }
    private void BuyAction()
    {
        BuyAction(cardType);
    }

    private void BuyAction(MainCardType type)
    {
        switch(type)
        {
            case MainCardType.NomadicStarter:
                break;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
    
}
