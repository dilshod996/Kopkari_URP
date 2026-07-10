using Bozo.ModularCharacters;
using UnityEngine;

public sealed class BozoSaveImporter : MonoBehaviour
{
    [SerializeField] private string sourceBozoSaveId;
    [SerializeField] private string targetPlayerPrefsKey = "bozo_player_character";
    [SerializeField] private bool importOnStart;

    private void Start()
    {
        if (importOnStart)
            Import();
    }

    [ContextMenu("Import BoZo Save To PlayerPrefs")]
    public void Import()
    {
        if (string.IsNullOrWhiteSpace(sourceBozoSaveId))
        {
            Debug.LogWarning("BozoSaveImporter needs a Source BoZo Save Id.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetPlayerPrefsKey))
        {
            Debug.LogWarning("BozoSaveImporter needs a Target PlayerPrefs Key.", this);
            return;
        }

        CharacterData data = BMAC_SaveSystem.GetDataFromID(sourceBozoSaveId.Trim());
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(targetPlayerPrefsKey.Trim(), json);
        PlayerPrefs.Save();

        Debug.Log($"Imported BoZo save '{sourceBozoSaveId}' into PlayerPrefs key '{targetPlayerPrefsKey}'.", this);
    }
}
