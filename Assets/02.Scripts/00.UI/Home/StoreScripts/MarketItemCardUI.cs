using System.Collections;
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
    [SerializeField] private int firstItemAmount;//if Market Items == Coins // Coins
    [SerializeField] private int secondItemAmount; // Nyufiys
    [SerializeField] private float costAmount;

    [SerializeField] private TMP_Text firstItemText;
    [SerializeField] private TMP_Text secondItemText;
    [SerializeField] private TMP_Text topCornerText;

    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyBtn;


    private void OnEnable()
    {
        buyBtn.onClick.AddListener(BuyAction);
        AllTexts(marketItems);
    }
    private void OnDisable()
    {
        buyBtn.onClick.RemoveListener(BuyAction);
    }

    private void AllTexts(MarketItems items)
    {
        switch (items)
        {
            case MarketItems.Coins:
                firstItemText.text = $"+{firstItemAmount:N0}";
                secondItemText.text = $"+{secondItemAmount:N0}";
                if(topCornerText != null)
                {
                    topCornerText.text = LanguageManager.Instance?.GetText(411);
                }
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

    }


}
