using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bozo.ModularCharacters;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class BozoCustomizationCategory
{
    public string displayName;
    public string outfitTypeName;
    public bool allowRemove = true;
}

[Serializable]
public sealed class BozoCategoryEvent : UnityEvent<string> { }

[Serializable]
public sealed class BozoOutfitEvent : UnityEvent<Outfit> { }

public sealed class BozoCustomizationManager : MonoBehaviour
{
    [Header("BoZo")]
    [SerializeField] private OutfitSystem outfitSystem;
    [SerializeField] private DecalController decalController;
    [SerializeField] private CharacterObject defaultCharacter;

    [Header("Save")]
    [SerializeField] private string saveKey = "bozo_player_character";
    [SerializeField] private bool loadSavedOnStart = true;
    [SerializeField] private bool saveOnEveryApply;

    [Header("Addressables")]
    [SerializeField] private bool useAddressableTexturePackages = true;
    [SerializeField] private string texturePackagesLabel = "BozoCustomizationTextures";
    [SerializeField] private bool useResourcesTextureFallback = true;

    [Header("Categories")]
    [SerializeField]
    private BozoCustomizationCategory[] categories =
    {
        new BozoCustomizationCategory { displayName = "Hair Front", outfitTypeName = "HairFront" },
        new BozoCustomizationCategory { displayName = "Hair Back", outfitTypeName = "HairBack" },
        new BozoCustomizationCategory { displayName = "Top", outfitTypeName = "Top" },
        new BozoCustomizationCategory { displayName = "Bottom", outfitTypeName = "Bottom" },
        new BozoCustomizationCategory { displayName = "Feet", outfitTypeName = "Feet" },
        new BozoCustomizationCategory { displayName = "Gloves", outfitTypeName = "Gloves" },
        new BozoCustomizationCategory { displayName = "Hat", outfitTypeName = "Hat" },
        new BozoCustomizationCategory { displayName = "Face", outfitTypeName = "LowerFace" },
        new BozoCustomizationCategory { displayName = "Eyes", outfitTypeName = "Iris", allowRemove = false }
    };

    [Header("Events")]
    public BozoCategoryEvent OnCategoryChanged = new BozoCategoryEvent();
    public BozoOutfitEvent OnOutfitChanged = new BozoOutfitEvent();
    public UnityEvent OnCharacterLoaded = new UnityEvent();
    public UnityEvent OnCharacterSaved = new UnityEvent();

    private readonly Dictionary<string, List<Outfit>> outfitsByType =
        new Dictionary<string, List<Outfit>>(StringComparer.OrdinalIgnoreCase);

    private readonly List<TexturePackage> texturePackages = new List<TexturePackage>();
    private readonly HashSet<string> texturePackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private string currentCategory;
    private bool isReady;
    private bool texturePackagesLoadedFromAddressables;

    public OutfitSystem OutfitSystem => outfitSystem;
    public string CurrentCategory => currentCategory;
    public IReadOnlyList<BozoCustomizationCategory> Categories => categories;
    public bool IsReady => isReady;

    private void Awake()
    {
        Initialize();

        if (string.IsNullOrEmpty(currentCategory))
            currentCategory = GetFirstCategoryName();
    }

    private async void Start()
    {
        Initialize();

        await LoadAddressableTexturePackagesAsync();

        if (loadSavedOnStart)
            await LoadSavedCharacterAsync();

        if (string.IsNullOrEmpty(currentCategory))
            SelectCategory(GetFirstCategoryName());
    }

    public void Initialize()
    {
        if (isReady) return;

        if (outfitSystem == null)
            outfitSystem = GetComponentInChildren<OutfitSystem>(true);

        if (decalController == null)
            decalController = GetComponentInChildren<DecalController>(true);

        if (outfitSystem == null)
        {
            return;
        }

        outfitSystem.Init();
        BuildDatabases();

        if (string.IsNullOrEmpty(currentCategory))
            currentCategory = GetFirstCategoryName();

        isReady = true;
    }

    public async Task BindPlayerAsync(GameObject player, bool loadSavedCharacter = true)
    {
        if (player == null)
            return;

        outfitSystem = player.GetComponentInChildren<OutfitSystem>(true);

        DecalController playerDecalController = player.GetComponentInChildren<DecalController>(true);
        if (playerDecalController != null)
            decalController = playerDecalController;

        isReady = false;
        Initialize();

        if (!isReady)
        {
            Debug.LogError("BozoCustomizationManager could not find an OutfitSystem on the spawned player.", player);
            return;
        }

        await LoadAddressableTexturePackagesAsync();

        if (loadSavedCharacter)
            await LoadSavedCharacterAsync();

        if (string.IsNullOrEmpty(currentCategory))
            SelectCategory(GetFirstCategoryName());
        else
            OnCategoryChanged.Invoke(currentCategory);
    }

    private void BuildDatabases()
    {
        outfitsByType.Clear();

        Outfit[] outfits = Resources.LoadAll<Outfit>("");
        foreach (Outfit outfit in outfits)
        {
            if (outfit == null || outfit.Type == null || !outfit.showCharacterCreator)
                continue;

            string typeName = outfit.Type.name;
            if (!outfitsByType.TryGetValue(typeName, out List<Outfit> list))
            {
                list = new List<Outfit>();
                outfitsByType[typeName] = list;
            }

            if (!HasOutfit(list, outfit.name))
                list.Add(outfit);
        }

        foreach (List<Outfit> list in outfitsByType.Values)
        {
            list.Sort((a, b) => string.Compare(GetDisplayName(a), GetDisplayName(b), StringComparison.OrdinalIgnoreCase));
        }

        texturePackages.Clear();
        texturePackageNames.Clear();

        if (useResourcesTextureFallback)
            AddTexturePackages(Resources.LoadAll<TexturePackage>(""));
    }

    public async Task LoadAddressableTexturePackagesAsync()
    {
        if (!useAddressableTexturePackages || texturePackagesLoadedFromAddressables || string.IsNullOrWhiteSpace(texturePackagesLabel))
            return;

        if (AddressablesService.Instance == null)
        {
            Debug.LogWarning("AddressablesService is missing. BoZo texture packages will use Resources fallback.", this);
            return;
        }

        IList<GameObject> packageObjects = await AddressablesService.Instance.LoadAssetsAsync<GameObject>(texturePackagesLabel);
        if (packageObjects == null || packageObjects.Count == 0)
            return;

        if (!useResourcesTextureFallback)
        {
            texturePackages.Clear();
            texturePackageNames.Clear();
        }

        int addedCount = 0;
        for (int i = 0; i < packageObjects.Count; i++)
        {
            GameObject packageObject = packageObjects[i];
            if (packageObject == null)
                continue;

            TexturePackage package = packageObject.GetComponent<TexturePackage>();
            if (AddTexturePackage(package))
                addedCount++;
        }

        if (addedCount == 0)
            return;

        texturePackagesLoadedFromAddressables = true;
        if (!string.IsNullOrEmpty(currentCategory))
            OnCategoryChanged.Invoke(currentCategory);
    }

    private void AddTexturePackages(IEnumerable<TexturePackage> packages)
    {
        foreach (TexturePackage package in packages)
            AddTexturePackage(package);
    }

    private bool AddTexturePackage(TexturePackage package)
    {
        if (package == null || package.texture == null)
            return false;

        string packageName = package.name;
        if (string.IsNullOrEmpty(packageName))
            return false;

        if (!texturePackageNames.Add(packageName))
            return false;

        texturePackages.Add(package);
        return true;
    }

    private static bool HasOutfit(List<Outfit> outfits, string outfitName)
    {
        for (int i = 0; i < outfits.Count; i++)
        {
            Outfit outfit = outfits[i];
            if (outfit != null && string.Equals(outfit.name, outfitName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public IReadOnlyList<Outfit> GetOutfits(string outfitTypeName)
    {
        Initialize();

        if (string.IsNullOrEmpty(outfitTypeName))
            outfitTypeName = currentCategory;

        return !string.IsNullOrEmpty(outfitTypeName) && outfitsByType.TryGetValue(outfitTypeName, out List<Outfit> list)
            ? list
            : Array.Empty<Outfit>();
    }

    public IReadOnlyList<TexturePackage> GetTextures(TextureType textureType, string category = null)
    {
        Initialize();

        string targetCategory = string.IsNullOrEmpty(category) ? currentCategory : category;
        List<TexturePackage> result = new List<TexturePackage>();

        for (int i = 0; i < texturePackages.Count; i++)
        {
            TexturePackage package = texturePackages[i];
            if (package == null || package.type != textureType)
                continue;

            if (!string.IsNullOrEmpty(targetCategory) && !string.Equals(package.catagory, targetCategory, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(package);
        }

        result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public void SelectCategory(string outfitTypeName)
    {
        Initialize();

        if (string.IsNullOrEmpty(outfitTypeName))
            return;

        currentCategory = outfitTypeName;
        OnCategoryChanged.Invoke(currentCategory);
    }

    public async void ApplyOutfit(Outfit outfitPrefab)
    {
        await ApplyOutfitAsync(outfitPrefab);
    }

    public async Task ApplyOutfitAsync(Outfit outfitPrefab)
    {
        Initialize();

        if (outfitSystem == null || outfitPrefab == null)
            return;

        Outfit instance = Instantiate(outfitPrefab, outfitSystem.transform);
        await Task.Yield();

        instance.Attach(outfitSystem);
        currentCategory = instance.Type != null ? instance.Type.name : currentCategory;

        OnOutfitChanged.Invoke(instance);
        OnCategoryChanged.Invoke(currentCategory);

        if (saveOnEveryApply)
            SaveCharacter();
    }

    public void RemoveCurrentOutfit()
    {
        RemoveOutfit(currentCategory);
    }

    public void RemoveOutfit(string outfitTypeName)
    {
        Initialize();

        if (outfitSystem == null || string.IsNullOrEmpty(outfitTypeName))
            return;

        BozoCustomizationCategory category = GetCategory(outfitTypeName);
        if (category != null && !category.allowRemove)
            return;

        outfitSystem.RemoveOutfit(outfitTypeName);
        OnOutfitChanged.Invoke(null);

        if (saveOnEveryApply)
            SaveCharacter();
    }

    public Outfit GetCurrentOutfit(string outfitTypeName = null)
    {
        Initialize();

        string targetType = string.IsNullOrEmpty(outfitTypeName) ? currentCategory : outfitTypeName;
        return outfitSystem != null && !string.IsNullOrEmpty(targetType)
            ? outfitSystem.GetOutfit(targetType)
            : null;
    }

    public void SetCurrentOutfitColor(int channel, Color color)
    {
        SetOutfitColor(currentCategory, channel, color);
    }

    public void SetOutfitColor(string outfitTypeName, int channel, Color color)
    {
        Initialize();

        Outfit outfit = GetCurrentOutfit(outfitTypeName);
        if (outfit == null)
            return;

        outfit.SetColor(color, Mathf.Clamp(channel, 1, 9));
        OnOutfitChanged.Invoke(outfit);

        if (saveOnEveryApply)
            SaveCharacter();
    }

    public void ApplyTexture(TexturePackage package)
    {
        Initialize();

        if (package == null || package.texture == null)
            return;

        if (package.type == TextureType.Pattern)
        {
            Outfit outfit = GetCurrentOutfit(currentCategory);
            if (outfit == null)
                return;

            outfit.SetPattern(package.texture, package.colors);
            OnOutfitChanged.Invoke(outfit);
        }
        else if (package.type == TextureType.Decal)
        {
            if (decalController == null)
            {
                Debug.LogWarning("DecalController is not assigned, decal texture was ignored.", this);
                return;
            }

            decalController.SetDecal(package.texture);
        }

        if (saveOnEveryApply)
            SaveCharacter();
    }

    public void SetShape(string shapeKey, float value)
    {
        Initialize();
        outfitSystem?.SetShape(shapeKey, value);
    }

    public void SaveCharacter()
    {
        Initialize();

        if (outfitSystem == null)
            return;

        CharacterData data = BMAC_SaveSystem.GetCharacterData(outfitSystem);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        OnCharacterSaved.Invoke();
    }

    public async void LoadSavedCharacter()
    {
        await LoadSavedCharacterAsync();
    }

    public async Task LoadSavedCharacterAsync()
    {
        Initialize();

        if (outfitSystem == null)
            return;

        CharacterData data = null;
        string json = PlayerPrefs.GetString(saveKey, "");

        if (!string.IsNullOrEmpty(json))
            data = JsonUtility.FromJson<CharacterData>(json);
        else if (defaultCharacter != null)
            data = defaultCharacter.GetCharacterData();

        if (data == null)
            return;

        await BMAC_SaveSystem.LoadCharacter(outfitSystem, data, false, outfitSystem.async);
        OnCharacterLoaded.Invoke();
        OnOutfitChanged.Invoke(GetCurrentOutfit(currentCategory));
    }

    public async void ResetToDefault()
    {
        await ResetToDefaultAsync();
    }

    public async Task ResetToDefaultAsync()
    {
        Initialize();

        if (outfitSystem == null || defaultCharacter == null)
            return;

        await BMAC_SaveSystem.LoadCharacter(outfitSystem, defaultCharacter.GetCharacterData(), false, outfitSystem.async);
        SaveCharacter();
        OnCharacterLoaded.Invoke();
    }

    public bool IsSelected(Outfit outfit)
    {
        if (outfit == null || outfit.Type == null)
            return false;

        Outfit current = GetCurrentOutfit(outfit.Type.name);
        return current != null && CleanCloneName(current.name) == CleanCloneName(outfit.name);
    }

    public static string GetDisplayName(Outfit outfit)
    {
        if (outfit == null)
            return "";

        if (!string.IsNullOrWhiteSpace(outfit.OutfitName))
            return outfit.OutfitName;

        return CleanCloneName(outfit.name).Replace("_", " ");
    }

    public static string CleanCloneName(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.Replace("(Clone)", "").Trim();
    }

    private string GetFirstCategoryName()
    {
        if (categories != null)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                BozoCustomizationCategory category = categories[i];
                if (category != null && !string.IsNullOrEmpty(category.outfitTypeName))
                    return category.outfitTypeName;
            }
        }

        foreach (string outfitTypeName in outfitsByType.Keys)
            return outfitTypeName;

        return null;
    }

    private BozoCustomizationCategory GetCategory(string outfitTypeName)
    {
        if (categories == null)
            return null;

        for (int i = 0; i < categories.Length; i++)
        {
            BozoCustomizationCategory category = categories[i];
            if (category != null && string.Equals(category.outfitTypeName, outfitTypeName, StringComparison.OrdinalIgnoreCase))
                return category;
        }

        return null;
    }
}
