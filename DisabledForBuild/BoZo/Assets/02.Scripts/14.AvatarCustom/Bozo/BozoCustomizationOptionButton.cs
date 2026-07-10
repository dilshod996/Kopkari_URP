using Bozo.ModularCharacters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationOptionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Text legacyLabel;
    [SerializeField] private GameObject selectedIndicator;

    private BozoCustomizationManager manager;
    private Outfit outfit;

    private void Awake()
    {
        if (button == null)
            button = GetComponentInChildren<Button>();
    }

    private void OnEnable()
    {
        if (manager != null)
            manager.OnOutfitChanged.AddListener(HandleOutfitChanged);
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnOutfitChanged.RemoveListener(HandleOutfitChanged);
    }

    public void Init(BozoCustomizationManager customizationManager, Outfit outfitPrefab)
    {
        manager = customizationManager;
        outfit = outfitPrefab;

        string displayName = BozoCustomizationManager.GetDisplayName(outfit);
        if (label != null)
            label.text = displayName;

        if (legacyLabel != null)
            legacyLabel.text = displayName;

        if (icon != null)
        {
            icon.overrideSprite = outfit != null ? outfit.OutfitIcon : null;
            icon.enabled = icon.overrideSprite != null;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        manager.OnOutfitChanged.RemoveListener(HandleOutfitChanged);
        manager.OnOutfitChanged.AddListener(HandleOutfitChanged);
        RefreshSelected();
    }

    public void Select()
    {
        if (manager != null && outfit != null)
            manager.ApplyOutfit(outfit);
    }

    private void HandleOutfitChanged(Outfit _)
    {
        RefreshSelected();
    }

    private void RefreshSelected()
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(manager != null && manager.IsSelected(outfit));
    }
}
