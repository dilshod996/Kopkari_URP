using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationColorPresetButton : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private Button button;
    [SerializeField] private Image preview;
    [SerializeField] private bool useCurrentCategory = true;
    [SerializeField] private string outfitTypeName;
    [SerializeField, Range(1, 9)] private int colorChannel = 1;
    [SerializeField] private Color color = Color.white;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();

        if (button == null)
            button = GetComponentInChildren<Button>();

        if (preview != null)
            preview.color = color;
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(Apply);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Apply);
    }

    public void Apply()
    {
        if (manager == null)
            return;

        string target = useCurrentCategory ? manager.CurrentCategory : outfitTypeName;
        manager.SetOutfitColor(target, colorChannel, color);
    }
}
