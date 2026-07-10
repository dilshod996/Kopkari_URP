using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreAIItem : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private int itemID;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text excost;
    [SerializeField] private TMP_Text costText;

    [SerializeField] private int titleID;
    [SerializeField] private int excostValue;
    [SerializeField] private int costValue;

    [Header("UI Settings")]
    [SerializeField] private Button buyButton;

    private void OnEnable()
    {
        UITranslitions();
    }
    private void UITranslitions()
    {
        var language = LanguageManager.Instance;
        if (language == null) return;

        if (titleText != null)
            titleText.text = language.GetText(titleID);
        if (excost != null)
            excost.text = excostValue + " " + language.GetText(58);
        if (costText != null)
            costText.text = costValue + " " + language.GetText(58);
    }
}
