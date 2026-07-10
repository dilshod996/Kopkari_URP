using Michsky.UI.ModernUIPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PremiumShower : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    public Transform contentParent;
    public GameObject premiumPrefab;

    [Header("Premium Cards")]
    [SerializeField] private List<PremiumData> bronzeData;
    [SerializeField] private List<PremiumData> silverData;
    [SerializeField] private List<PremiumData> goldData;
    [SerializeField] private List<PremiumData> diamondData;
    [SerializeField] private List<PremiumData> premiumData;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    private readonly List<PremiumItem> premiumItems = new List<PremiumItem>();

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

    public void OpenStore(PremiumCategoryType premium)
    {
        HideItems();

        var language = LanguageManager.Instance;
        List<PremiumData> selectedList;
        switch (premium)
        {
            case PremiumCategoryType.Bronze:
                selectedList = bronzeData;
                SetTextColor(new Color32(206, 137, 70, 255));
                SetPremiumText(language, 185, "65,000");
                break;
            case PremiumCategoryType.Silver:
                selectedList = silverData;
                SetTextColor(new Color32(192, 192, 192, 255));
                SetPremiumText(language, 186, "85,000");
                break;
            case PremiumCategoryType.Gold:
                selectedList = goldData;
                SetTextColor(new Color32(225, 164, 56, 255));
                SetPremiumText(language, 187, "99,000");
                break;
            case PremiumCategoryType.Diamond:
                selectedList = diamondData;
                SetTextColor(new Color32(185, 242, 192, 255));
                SetPremiumText(language, 188, "129,000");
                break;
            case PremiumCategoryType.Premium:
                selectedList = premiumData;
                SetTextColor(new Color32(67, 238, 0, 255));
                SetPremiumText(language, 189, "199,000");
                break;
            default:
                selectedList = null;
                if (titleText != null)
                    titleText.text = "";
                break;
        }


        if (selectedList == null) return;

        for (int i = 0; i < selectedList.Count; i++)
        {
            var data = selectedList[i];
            if (data == null) continue;

            PremiumItem item = GetOrCreateItem(i);
            if (item == null) continue;

            item.gameObject.SetActive(true);
            item.Setup(data);
        }
    }

    private PremiumItem GetOrCreateItem(int index)
    {
        if (index < premiumItems.Count)
            return premiumItems[index];

        if (premiumPrefab == null || contentParent == null)
            return null;

        GameObject itemObject = Instantiate(premiumPrefab, contentParent);
        PremiumItem item = itemObject.GetComponent<PremiumItem>();
        if (item == null)
        {
            Debug.LogWarning("Premium prefab is missing PremiumItem component.", premiumPrefab);
            itemObject.SetActive(false);
            return null;
        }

        premiumItems.Add(item);
        return item;
    }

    private void HideItems()
    {
        foreach (var item in premiumItems)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }
    }

    private void SetPremiumText(LanguageManager language, int titleId, string cost)
    {
        if (language == null) return;

        if (titleText != null)
            titleText.text = language.GetText(titleId);
        if (costText != null)
            costText.text = cost + " " + language.GetText(58);
    }

    private void SetTextColor(Color color)
    {
        if (titleText != null)
            titleText.color = color;
        if (costText != null)
            costText.color = color;
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
