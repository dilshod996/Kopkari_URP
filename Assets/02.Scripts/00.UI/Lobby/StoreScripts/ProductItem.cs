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

    private void Start()
    {
        buyButton.onClick.AddListener(ButButtonEvent);
    }
    public void Setup(ProductData data, ModalWindowManager modalWindow)
    {
        modalWindowManager = modalWindow;
        nameText.text = LanguageManager.Instance.GetText(data.nameId);
        costText.text = data.cost.ToString();
        image.sprite = data.productImage;
    }
    private void ButButtonEvent()
    {
        if (modalWindowManager != null) 
        {
            modalWindowManager.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(40), LanguageManager.Instance.GetText(41),
                LanguageManager.Instance.GetText(1), LanguageManager.Instance.GetText(2));
        }
        else
        {
            Debug.Log("ModalWindowManager is not assigned in ProductItem.");
        }

    }
}