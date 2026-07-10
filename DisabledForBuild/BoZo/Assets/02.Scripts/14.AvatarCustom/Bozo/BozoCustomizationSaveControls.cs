using TMPro;
using UnityEngine;

public sealed class BozoCustomizationSaveControls : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private TMP_Text statusLabel;

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();
    }

    public void Save()
    {
        if (manager == null)
            return;

        manager.SaveCharacter();
        SetStatus("Saved");
    }

    public void Load()
    {
        if (manager == null)
            return;

        manager.LoadSavedCharacter();
        SetStatus("Loaded");
    }

    public void ResetToDefault()
    {
        if (manager == null)
            return;

        manager.ResetToDefault();
        SetStatus("Reset");
    }

    private void SetStatus(string value)
    {
        if (statusLabel != null)
            statusLabel.text = value;
    }
}
