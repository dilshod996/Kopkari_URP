using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItem : MonoBehaviour
{
    public CategoryType categoryType;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text btnText;
    [SerializeField] private int titleId;
    [SerializeField] private Button viewBtn;

    public ProductContainer productContainer;
    private void OnEnable()
    {
        if (viewBtn != null)
            viewBtn.onClick.AddListener(OpenProductPanel);

        UITransilitions();
    }

    private void OnDisable()
    {
        if (viewBtn != null)
            viewBtn.onClick.RemoveListener(OpenProductPanel);
    }

    private void OpenProductPanel()
    {
        if (productContainer == null) return;

        productContainer.gameObject.SetActive(true);
        productContainer.OpenStore(categoryType);
    }
    
    private void UITransilitions()
    {
        var language = LanguageManager.Instance;
        if (language == null) return;

        if (titleText != null)
            titleText.text = language.GetText(titleId);
        if (btnText != null)
            btnText.text = language.GetText(190);
    }
}
