using System.Threading.Tasks;
using Bozo.ModularCharacters;
using UnityEngine;
using UnityEngine.Events;

public sealed class BozoSavedCharacterLoader : MonoBehaviour
{
    [SerializeField] private OutfitSystem outfitSystem;
    [SerializeField] private CharacterObject defaultCharacter;
    [SerializeField] private string saveKey = "bozo_man_character";
    [SerializeField] private string bozoSaveId;
    [SerializeField] private bool loadOnStart;

    public UnityEvent OnCharacterLoaded = new UnityEvent();

    private bool hasLoaded;
    private Task loadTask;

    private async void Start()
    {
        if (loadOnStart)
            await LoadSavedCharacterAsync(false);
    }

    public async void LoadSavedCharacter()
    {
        await LoadSavedCharacterAsync();
    }

    public void SaveCharacter(GameObject targetPlayer = null)
    {
        if (targetPlayer != null)
            outfitSystem = targetPlayer.GetComponentInChildren<OutfitSystem>(true);

        if (outfitSystem == null)
            outfitSystem = GetComponentInChildren<OutfitSystem>(true);

        if (outfitSystem == null)
        {
            Debug.LogError("BozoSavedCharacterLoader needs an OutfitSystem reference to save.", this);
            return;
        }

        outfitSystem.Init();

        CharacterData data = BMAC_SaveSystem.GetCharacterData(outfitSystem);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
    }

    public async Task LoadSavedCharacterAsync(bool forceReload = true)
    {
        if (!forceReload && hasLoaded)
            return;

        if (loadTask != null && !loadTask.IsCompleted)
        {
            await loadTask;
            hasLoaded = true;
            return;
        }

        loadTask = LoadSavedCharacterInternalAsync();
        await loadTask;
        hasLoaded = true;
    }

    public async Task LoadSavedCharacterAsync(GameObject targetPlayer, bool forceReload = true)
    {
        if (targetPlayer != null)
            outfitSystem = targetPlayer.GetComponentInChildren<OutfitSystem>(true);

        await LoadSavedCharacterAsync(forceReload);
    }

    private async Task LoadSavedCharacterInternalAsync()
    {
        if (outfitSystem == null)
            outfitSystem = GetComponentInChildren<OutfitSystem>(true);

        if (outfitSystem == null)
        {
            Debug.LogError("BozoSavedCharacterLoader needs an OutfitSystem reference.", this);
            return;
        }

        outfitSystem.Init();

        CharacterData data = null;
        string json = PlayerPrefs.GetString(saveKey, "");

        if (!string.IsNullOrEmpty(json))
            data = JsonUtility.FromJson<CharacterData>(json);
        else
            data = LoadBozoSaveId();

        if (data == null && defaultCharacter != null)
            data = defaultCharacter.GetCharacterData();

        if (data == null)
            return;

        await BMAC_SaveSystem.LoadCharacter(outfitSystem, data, false, outfitSystem.async);
        OnCharacterLoaded.Invoke();
    }

    private CharacterData LoadBozoSaveId()
    {
        string saveId = !string.IsNullOrWhiteSpace(bozoSaveId)
            ? bozoSaveId.Trim()
            : saveKey.Trim();

        if (string.IsNullOrEmpty(saveId))
            return null;

        return BMAC_SaveSystem.GetDataFromID(saveId);
    }
}
