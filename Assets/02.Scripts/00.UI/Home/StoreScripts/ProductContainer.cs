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
    private readonly List<ProductItem> productItems = new List<ProductItem>();

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void OpenStore(CategoryType category)
    {
        HideItems();

        var language = LanguageManager.Instance;
        List<ProductData> selectedList;
        switch(category)
        {
            case CategoryType.Rider:
                selectedList = riderList;
                SetTitle(language, 226);
                break;
            case CategoryType.Clothes:
                selectedList = clothesList;
                SetTitle(language, 230);
                break;
            case CategoryType.Helmets:
                selectedList = helmetsList;
                SetTitle(language, 228);
                break;
            case CategoryType.Horse:
                selectedList = horseList;
                SetTitle(language, 227);
                break;
            case CategoryType.Saddles:
                selectedList = saddlesList;
                SetTitle(language, 229);
                break;
            case CategoryType.Armors:
                selectedList = armorsList;
                SetTitle(language, 231);
                break;
            case CategoryType.Food:
                selectedList = foodList;
                SetTitle(language, 232);
                break;
            case CategoryType.MysteryBox:
                selectedList = mysteryBoxList;
                SetTitle(language, 233);
                break;
            default:
                selectedList = null;
                break;
        }

        if (selectedList == null) return;

        for (int i = 0; i < selectedList.Count; i++)
        {
            var data = selectedList[i];
            if (data == null) continue;

            ProductItem item = GetOrCreateItem(i);
            if (item == null) continue;

            item.gameObject.SetActive(true);
            item.Setup(data, modalWindowManager);
        }
    }

    private ProductItem GetOrCreateItem(int index)
    {
        if (index < productItems.Count)
            return productItems[index];

        if (productPrefab == null || contentParent == null)
            return null;

        GameObject itemObject = Instantiate(productPrefab, contentParent);
        ProductItem item = itemObject.GetComponent<ProductItem>();
        if (item == null)
        {
            Debug.LogWarning("Product prefab is missing ProductItem component.", productPrefab);
            itemObject.SetActive(false);
            return null;
        }

        productItems.Add(item);
        return item;
    }

    private void HideItems()
    {
        foreach (var item in productItems)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }
    }

    private void SetTitle(LanguageManager language, int id)
    {
        if (titleText != null && language != null)
            titleText.text = language.GetText(id);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
