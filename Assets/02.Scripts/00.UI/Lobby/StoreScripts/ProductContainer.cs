using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductContainer : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    public Transform contentParent;
    public GameObject productPrefab;

    public List<ProductData> riderList;
    public List<ProductData> clothesList; 
    public List<ProductData> helmetsList;
    public List<ProductData> horseList;
    public List<ProductData> saddlesList;
    public List<ProductData> armorsList;
    public List<ProductData> foodList;
    public List<ProductData> mysteryBoxList;

    public ModalWindowManager modalWindowManager;

    private void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
    public void OpenStore(CategoryType category)
    {
        ClearContent();

        List<ProductData> selectedList;
        switch(category)
        {
            case CategoryType.Rider:
                selectedList = riderList;
                titleText.text = LanguageManager.Instance.GetText(226);
                break;
            case CategoryType.Clothes:
                selectedList = clothesList;
                titleText.text = LanguageManager.Instance.GetText(230);
                break;
            case CategoryType.Helmets:
                selectedList = helmetsList;
                titleText.text = LanguageManager.Instance.GetText(228);
                break;
            case CategoryType.Horse:
                selectedList = horseList;
                titleText.text = LanguageManager.Instance.GetText(227);
                break;
            case CategoryType.Saddles:
                selectedList = saddlesList;
                titleText.text = LanguageManager.Instance.GetText(229);
                break;
            case CategoryType.Armors:
                selectedList = armorsList;
                titleText.text = LanguageManager.Instance.GetText(231);
                break;
            case CategoryType.Food:
                selectedList = foodList;
                titleText.text = LanguageManager.Instance.GetText(232);
                break;
            case CategoryType.MysteryBox:
                selectedList = mysteryBoxList;
                titleText.text = LanguageManager.Instance.GetText(233);
                break;
            default:
                selectedList = null;
                break;
        }

        if (selectedList == null) return;

        foreach (var data in selectedList)
        {
            GameObject item = Instantiate(productPrefab, contentParent);
            item.GetComponent<ProductItem>().Setup(data, modalWindowManager);
        }
    }

    private void ClearContent()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
}
