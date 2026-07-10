using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationColorControl : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private bool useCurrentCategory = true;
    [SerializeField] private string outfitTypeName;
    [SerializeField, Range(1, 9)] private int colorChannel = 1;
    [SerializeField] private Slider red;
    [SerializeField] private Slider green;
    [SerializeField] private Slider blue;
    [SerializeField] private Slider alpha;
    [SerializeField] private Image preview;
    [SerializeField] private bool applyOnSliderChange = true;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();
    }

    private void OnEnable()
    {
        if (!applyOnSliderChange)
            return;

        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void ApplyColor()
    {
        if (manager == null)
            return;

        Color color = ReadColor();
        string target = useCurrentCategory ? manager.CurrentCategory : outfitTypeName;
        manager.SetOutfitColor(target, colorChannel, color);

        if (preview != null)
            preview.color = color;
    }

    public void SetPresetColor(Color color)
    {
        SetSlider(red, color.r);
        SetSlider(green, color.g);
        SetSlider(blue, color.b);
        SetSlider(alpha, color.a);
        ApplyColor();
    }

    private Color ReadColor()
    {
        return new Color(
            red != null ? red.value : 1f,
            green != null ? green.value : 1f,
            blue != null ? blue.value : 1f,
            alpha != null ? alpha.value : 1f);
    }

    private void AddListeners()
    {
        if (red != null) red.onValueChanged.AddListener(HandleSliderChanged);
        if (green != null) green.onValueChanged.AddListener(HandleSliderChanged);
        if (blue != null) blue.onValueChanged.AddListener(HandleSliderChanged);
        if (alpha != null) alpha.onValueChanged.AddListener(HandleSliderChanged);
    }

    private void RemoveListeners()
    {
        if (red != null) red.onValueChanged.RemoveListener(HandleSliderChanged);
        if (green != null) green.onValueChanged.RemoveListener(HandleSliderChanged);
        if (blue != null) blue.onValueChanged.RemoveListener(HandleSliderChanged);
        if (alpha != null) alpha.onValueChanged.RemoveListener(HandleSliderChanged);
    }

    private void HandleSliderChanged(float _)
    {
        ApplyColor();
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }
}
