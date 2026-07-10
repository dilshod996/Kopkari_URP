using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductItem : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text costText;
    public Image image;
    public Button buyButton;
    private ModalWindowManager modalWindowManager;

    private void OnEnable()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyButtonEvent);
    }

    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyButtonEvent);
    }

    public void Setup(ProductData data, ModalWindowManager modalWindow)
    {
        if (data == null) return;

        modalWindowManager = modalWindow;
        var language = LanguageManager.Instance;

        if (nameText != null && language != null)
            nameText.text = language.GetText(data.nameId);
        if (costText != null)
            costText.text = data.cost.ToString();
        if (image != null)
            image.sprite = data.productImage;
    }
    private void BuyButtonEvent()
    {
        if (modalWindowManager != null) 
        {
            var language = LanguageManager.Instance;
            if (language == null) return;

            modalWindowManager.UpdateUICustomWithButtons(language.GetText(40), language.GetText(41),
                language.GetText(1), language.GetText(2));
        }
        else
        {
            Debug.Log("ModalWindowManager is not assigned in ProductItem.");
        }

    }
}
