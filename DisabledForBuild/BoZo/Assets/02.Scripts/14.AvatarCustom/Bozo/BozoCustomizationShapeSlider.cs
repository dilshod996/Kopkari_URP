using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BozoCustomizationShapeSlider : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private string shapeKey;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Text legacyLabel;
    [SerializeField] private bool applyOnValueChanged = true;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();

        if (slider == null)
            slider = GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        if (label != null)
            label.text = shapeKey;

        if (legacyLabel != null)
            legacyLabel.text = shapeKey;

        if (slider != null && manager != null && manager.OutfitSystem != null && !string.IsNullOrEmpty(shapeKey))
            slider.SetValueWithoutNotify(manager.OutfitSystem.GetShapeValue(shapeKey));
    }

    private void OnEnable()
    {
        if (applyOnValueChanged && slider != null)
            slider.onValueChanged.AddListener(Apply);
    }

    private void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(Apply);
    }

    public void Apply(float value)
    {
        if (manager != null && !string.IsNullOrEmpty(shapeKey))
            manager.SetShape(shapeKey, value);
    }
}
