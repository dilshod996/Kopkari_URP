using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationCategoryButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Text legacyLabel;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private string outfitTypeName;

    private BozoCustomizationManager manager;

    private void Awake()
    {
        if (button == null)
            button = GetComponentInChildren<Button>();
    }

    private void OnEnable()
    {
        if (manager != null)
            manager.OnCategoryChanged.AddListener(Refresh);
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnCategoryChanged.RemoveListener(Refresh);
    }

    public void Init(BozoCustomizationManager customizationManager, string category, string displayName)
    {
        manager = customizationManager;
        outfitTypeName = category;

        if (label != null)
            label.text = displayName;

        if (legacyLabel != null)
            legacyLabel.text = displayName;

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        manager.OnCategoryChanged.RemoveListener(Refresh);
        manager.OnCategoryChanged.AddListener(Refresh);
        Refresh(manager.CurrentCategory);
    }

    public void Select()
    {
        manager?.SelectCategory(outfitTypeName);
    }

    private void Refresh(string activeCategory)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(activeCategory == outfitTypeName);
    }
}
