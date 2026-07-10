using Bozo.ModularCharacters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationTextureButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Text legacyLabel;

    private BozoCustomizationManager manager;
    private TexturePackage package;

    private void Awake()
    {
        if (button == null)
            button = GetComponentInChildren<Button>();
    }

    public void Init(BozoCustomizationManager customizationManager, TexturePackage texturePackage)
    {
        manager = customizationManager;
        package = texturePackage;

        string displayName = package != null ? package.name.Replace("_", " ") : "";
        if (label != null)
            label.text = displayName;

        if (legacyLabel != null)
            legacyLabel.text = displayName;

        if (icon != null)
        {
            icon.overrideSprite = package != null ? package.icon : null;
            icon.enabled = icon.overrideSprite != null;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    public void Select()
    {
        if (manager != null && package != null)
            manager.ApplyTexture(package);
    }
}
