using Michsky.UI.ModernUIPack;
using System.Collections;
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

   private void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void OpenStore(PremiumCategoryType premium)
    {
        ClearContent();

        List<PremiumData> selectedList;
        switch (premium)
        {
            case PremiumCategoryType.Bronze:
                selectedList = bronzeData;
                titleText.color = new Color32(206, 137, 70, 255);
                costText.color = new Color32(206, 137, 70, 255);
                titleText.text = LanguageManager.Instance.GetText(185);
                costText.text ="65,000 " +  LanguageManager.Instance.GetText(58); 
                break;
            case PremiumCategoryType.Silver:
                selectedList = silverData;
                titleText.color = new Color32(192, 192, 192, 255);
                costText.color = new Color32(192, 192, 192, 255);
                titleText.text = LanguageManager.Instance.GetText(186);
                costText.text = "85,000 " + LanguageManager.Instance.GetText(58);
                break;
            case PremiumCategoryType.Gold:
                selectedList = goldData;
                titleText.color = new Color32(225, 164, 56, 255);
                costText.color = new Color32(225, 164, 56, 255);
                titleText.text = LanguageManager.Instance.GetText(187);
                costText.text = "99,000 " + LanguageManager.Instance.GetText(58);
                break;
            case PremiumCategoryType.Diamond:
                selectedList = diamondData;
                titleText.color = new Color32(185, 242, 192, 255);
                costText.color = new Color32(185, 242, 192, 255);
                titleText.text = LanguageManager.Instance.GetText(188);
                costText.text = "129,000 " + LanguageManager.Instance.GetText(58);
                break;
            case PremiumCategoryType.Premium:
                selectedList = premiumData;
                titleText.color = new Color32(67, 238, 0, 255);
                costText.color = new Color32(67, 238, 0, 255);
                titleText.text = LanguageManager.Instance.GetText(189);
                costText.text = "199,000 " + LanguageManager.Instance.GetText(58);
                break;
            default:
                selectedList = null;
                titleText.text = "";
                break;
        }


        if (selectedList == null) return;

        foreach (var data in selectedList)
        {
            GameObject item = Instantiate(premiumPrefab, contentParent);
            item.GetComponent<PremiumItem>().Setup(data);
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
