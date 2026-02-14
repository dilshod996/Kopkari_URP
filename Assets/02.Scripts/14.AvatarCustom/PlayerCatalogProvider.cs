using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// GLOBAL Catalog Provider (JSON):
/// - S3 dan 1 ta umumiy catalog.json + version.txt ni yuklab oladi (version check bilan)
/// - Local cache qiladi
/// - Runtime paytida avatarId (playerId) + slotId bo‘yicha optionlarni beradi
///
/// JSON format:
/// { "entries": [ { avatarId, slotId, optionId, meshKey, materialKey, iconKey, isDefault, price }, ... ] }
/// </summary>
public sealed class PlayerCatalogProvider : MonoBehaviour
{
    public static PlayerCatalogProvider Instance { get; private set; }

    public event Action OnCatalogReady; // global ready

    [Header("Remote URLs")]
    [SerializeField] private string baseUrl =
        "https://s3.ap-northeast-2.amazonaws.com/kaja-games.com/AvatarCatalogs";

    [SerializeField] private string catalogFileName = "catalog.json";
    [SerializeField] private string versionFileName = "version.txt";

    [Header("Auto Init On Start")]
    [SerializeField] private bool initOnStart = true;
    [SerializeField] private bool preloadSelectedAssets = true;
    [Header("Preload Active Horse Too (optional)")]
    [SerializeField] private bool preloadActiveHorseSelectedAssets = true;

    private const string VERSION_PREF_KEY = "avatar_catalog_version_global";

    private CatalogData _globalCatalog;
    private bool _loadingGlobal;
    private Task _initTask;

    private string LocalDir() =>
        Path.Combine(Application.persistentDataPath, "avatar_catalogs");

    private string LocalCatalogPath() =>
        Path.Combine(LocalDir(), catalogFileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        if (!initOnStart) return;
        _initTask = InitializeAsync(preloadSelectedAssets);
        await _initTask;
    }
    #region Horse Custom Methods
    // PlayerCatalogProvider ichiga qo'sh (class ichida)
    public Task<List<CatalogEntry>> GetHorseOptionsAsync(string horseId, AvatarCustomTypes.HorseSkins slot)
    {
        // slotId catalogdagi string bo'lishi kerak: "Body","Mane","Tail","Saddle"
        return GetOptionsAsync(horseId, slot.ToString());
    }

    public Task<CatalogEntry> FindHorseAsync(string horseId, AvatarCustomTypes.HorseSkins slot, string optionId)
    {
        return FindAsync(horseId, slot.ToString(), optionId);
    }

    public string GetDefaultHorseOptionId(string horseId, AvatarCustomTypes.HorseSkins slot)
    {
        return GetDefaultOptionId(horseId, slot.ToString());
    }

    #endregion
    public Task WaitInitializedAsync() => _initTask ?? Task.CompletedTask;

    public async Task InitializeAsync(bool preloadActivePlayerSelectedAssets)
    {
        await AddressablesService.Instance.EnsureInitializedAsync();
        await EnsureCatalogAsync();

        if (preloadActivePlayerSelectedAssets)
        {
            string playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");
            await PreloadSelectedAssetsAsync(playerId);
        }
        // ✅ horse ham xuddi shunday (faqat providerda)
        if (preloadActiveHorseSelectedAssets)
        {
            string horseId = PlayerPrefs.GetString("ActiveHorseId", "horse_01");
            await PreloadSelectedAssetsAsync(horseId);
        }
    }

    public async Task<CatalogData> EnsureCatalogAsync()
    {
        if (_globalCatalog != null) return _globalCatalog;

        if (_loadingGlobal)
        {
            while (_loadingGlobal) await Task.Yield();
            return _globalCatalog;
        }

        _loadingGlobal = true;

        try
        {
            Directory.CreateDirectory(LocalDir());

            string remoteVersion = await GetRemoteVersion();
            string localVersion = PlayerPrefs.GetString(VERSION_PREF_KEY, "");

            bool needDownload = string.IsNullOrEmpty(remoteVersion)
                ? !File.Exists(LocalCatalogPath())
                : (remoteVersion != localVersion || !File.Exists(LocalCatalogPath()));

            if (needDownload)
            {
                Debug.Log($"📥 Global Catalog download (remoteVersion={remoteVersion}, localVersion={localVersion})");
                bool ok = await DownloadCatalog();

                if (ok && !string.IsNullOrEmpty(remoteVersion))
                {
                    PlayerPrefs.SetString(VERSION_PREF_KEY, remoteVersion);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                Debug.Log("✅ Using cached Global Catalog (JSON)");
            }

            if (!File.Exists(LocalCatalogPath()))
            {
                Debug.LogError($"❌ Global catalog file missing: {LocalCatalogPath()}");
                return null;
            }

            string jsonText = File.ReadAllText(LocalCatalogPath());
            _globalCatalog = CatalogData.ParseJson(jsonText);
            _globalCatalog.BuildIndexes();

            OnCatalogReady?.Invoke();
            return _globalCatalog;
        }
        finally
        {
            _loadingGlobal = false;
        }
    }

    // -------- PUBLIC API --------

    public async Task<List<CatalogEntry>> GetOptionsAsync(string playerId, string slotId)
    {
        var catalog = await EnsureCatalogAsync();
        if (catalog == null) return new List<CatalogEntry>();
        return catalog.GetOptions(playerId, slotId);
    }

    public async Task<CatalogEntry> FindAsync(string playerId, string slotId, string optionId)
    {
        var catalog = await EnsureCatalogAsync();
        if (catalog == null) return null;
        return catalog.Find(playerId, slotId, optionId);
    }

    public string GetDefaultOptionId(string playerId, string slotId)
    {
        if (_globalCatalog == null) return "";
        return _globalCatalog.GetDefaultOptionId(playerId, slotId);
    }

    // -------- Preload (download deps) --------
    public async Task PreloadSelectedAssetsAsync(string playerId)
    {
        var catalog = await EnsureCatalogAsync();
        if (catalog == null) return;

        await AddressablesService.Instance.EnsureInitializedAsync();

        var slotIds = catalog.GetSlotIds(playerId);
        if (slotIds == null || slotIds.Count == 0) return;

        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var slotId in slotIds)
        {
            if (string.IsNullOrWhiteSpace(slotId)) continue;

            string prefKey = $"Sel_{playerId}_{slotId}";
            string optionId = PlayerPrefs.GetString(prefKey, "");

            if (string.IsNullOrEmpty(optionId))
                optionId = catalog.GetDefaultOptionId(playerId, slotId);

            if (string.IsNullOrEmpty(optionId))
                continue;

            var entry = catalog.Find(playerId, slotId, optionId);
            if (entry == null)
                continue;

            if (!string.IsNullOrEmpty(entry.MeshKey)) keySet.Add(entry.MeshKey);
            if (!string.IsNullOrEmpty(entry.MaterialKey)) keySet.Add(entry.MaterialKey);

            // UI iconlarni ham oldindan download qilmoqchi bo'lsang:
            // if (!string.IsNullOrEmpty(entry.IconKey)) keySet.Add(entry.IconKey);
        }
        // ---------- 🐎 HORSE STATIC PARTS ----------
        if (playerId.StartsWith("horse_", StringComparison.OrdinalIgnoreCase))
        {
            // meshes
            keySet.Add(Constants.HorseStaticMeshes.Eyes);
            keySet.Add(Constants.HorseStaticMeshes.Tail);
            keySet.Add(Constants.HorseStaticMeshes.Reins);
            keySet.Add(Constants.HorseStaticMeshes.HeadReins);

            // materials
            keySet.Add(Constants.HorseStaticMaterials.Eyes);
        }
        if (keySet.Count == 0) return;

        var keys = new List<string>(keySet);

        bool ok = await AddressablesService.Instance.PreloadDependenciesAsync(
            keys,
            onProgress: null,
            fakeDurationIfCached: 0.8f
        );

        if (!ok)
            Debug.LogWarning($"⚠️ PreloadSelectedAssetsAsync failed for {playerId} (keys={keys.Count})");
    }
    public async Task<bool> PreloadAllForPlayerAsync(
        string playerId,
        Action<float> onProgress = null,
        bool includeIcons = true)
    {
        var catalog = await EnsureCatalogAsync();
        if (catalog == null) return false;

        var keys = catalog.CollectAllKeysForPlayer(playerId, includeIcons);
        if (keys == null || keys.Count == 0)
        {
            onProgress?.Invoke(1f);
            return true;
        }

        return await AddressablesService.Instance.PreloadDependenciesAsync(
            keys,
            onProgress,
            fakeDurationIfCached: 0.3f
        );
    }



    // -------- Remote helpers (GLOBAL) --------

    private string RemoteVersionUrl() => $"{baseUrl}/{versionFileName}";
    private string RemoteCatalogUrl() => $"{baseUrl}/{catalogFileName}";

    private async Task<string> GetRemoteVersion()
    {
        using var request = UnityWebRequest.Get(RemoteVersionUrl());
        var op = request.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
            return request.downloadHandler.text.Trim();

        Debug.LogWarning($"⚠️ Global version.txt load failed: {request.error}");
        return "";
    }

    private async Task<bool> DownloadCatalog()
    {
        using var request = UnityWebRequest.Get(RemoteCatalogUrl());
        var op = request.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Global catalog download failed: {request.error}");
            return false;
        }

        File.WriteAllText(LocalCatalogPath(), request.downloadHandler.text);
        Debug.Log($"✅ Global catalog saved: {LocalCatalogPath()}");
        return true;
    }
    #region Horse Preload Wrappers (optional, qulaylik uchun)
    //public Task PreloadSelectedAssetsForHorseAsync(string horseId)
    //{
    //    // PreloadSelectedAssetsAsync allaqachon avatarId bo‘yicha ishlaydi
    //    return PreloadSelectedAssetsAsync(horseId);
    //}

    public Task<bool> PreloadAllForHorseAsync(string horseId, Action<float> onProgress = null, bool includeIcons = true)
    {
        // PreloadAllForPlayerAsync ham avatarId bo‘yicha ishlaydi (nomi player bo‘lsa ham)
        return PreloadAllForPlayerAsync(horseId, onProgress, includeIcons);
    }
    #endregion

    #region AI Rider lar uchun random custom
    public async Task<List<string>> GetMaterialKeysAsync(string avatarId, string slotId)
    {
        var catalog = await EnsureCatalogAsync(); // sen endi bitta umumiy catalog qilganding
        if (catalog == null) return new List<string>();

        return catalog.CollectMaterialKeys(avatarId, slotId);
    }
    public async Task<bool> PreloadMaterialPoolAsync(
        string avatarId,
        List<string> slotIds,
        Action<float> onProgress = null)
    {
        var catalog = await EnsureCatalogAsync();
        if (catalog == null) return false;

        if (string.IsNullOrWhiteSpace(avatarId) || slotIds == null || slotIds.Count == 0)
        {
            onProgress?.Invoke(1f);
            return true;
        }

        // Slotlar bo'yicha material keylarni yig'amiz (unique)
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var slotId in slotIds)
        {
            if (string.IsNullOrWhiteSpace(slotId)) continue;

            // CatalogData ichida shu method bo'lishi kerak:
            // CollectMaterialKeys(avatarId, slotId)
            var keys = catalog.CollectMaterialKeys(avatarId, slotId);
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                if (!string.IsNullOrWhiteSpace(k))
                    set.Add(k);
            }
        }

        if (set.Count == 0)
        {
            onProgress?.Invoke(1f);
            return true;
        }

        // Addressables download progress (0..1)
        var list = new List<string>(set);

        return await AddressablesService.Instance.PreloadDependenciesAsync(
            list,
            onProgress,
            fakeDurationIfCached: 0.3f
        );
    }



    #endregion

}

// ---------------- DATA ----------------

public sealed class CatalogData
{
    public readonly List<CatalogEntry> Entries = new();

    private readonly Dictionary<string, List<CatalogEntry>> _byAvatarSlot = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CatalogEntry> _byAvatarSlotOption = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaultByAvatarSlot = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _slotIdsByAvatar = new(StringComparer.OrdinalIgnoreCase);

    private static string KeyAS(string avatarId, string slotId) => $"{avatarId}||{slotId}";
    private static string KeyASO(string avatarId, string slotId, string optionId) => $"{avatarId}||{slotId}||{optionId}";

    public void BuildIndexes()
    {
        _byAvatarSlot.Clear();
        _byAvatarSlotOption.Clear();
        _defaultByAvatarSlot.Clear();
        _slotIdsByAvatar.Clear();

        foreach (var e in Entries)
        {
            if (e == null) continue;

            if (string.IsNullOrWhiteSpace(e.AvatarId) ||
                string.IsNullOrWhiteSpace(e.SlotId) ||
                string.IsNullOrWhiteSpace(e.OptionId))
                continue;

            if (!_slotIdsByAvatar.TryGetValue(e.AvatarId, out var slotSet))
            {
                slotSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _slotIdsByAvatar[e.AvatarId] = slotSet;
            }
            slotSet.Add(e.SlotId);

            string kAS = KeyAS(e.AvatarId, e.SlotId);

            if (!_byAvatarSlot.TryGetValue(kAS, out var list))
            {
                list = new List<CatalogEntry>();
                _byAvatarSlot[kAS] = list;
            }
            list.Add(e);

            _byAvatarSlotOption[KeyASO(e.AvatarId, e.SlotId, e.OptionId)] = e;

            if (e.IsDefault)
            {
                if (!_defaultByAvatarSlot.ContainsKey(kAS))
                    _defaultByAvatarSlot[kAS] = e.OptionId;
                else
                    Debug.LogWarning($"⚠️ Duplicate isDefault for {kAS}. Keeping first: {_defaultByAvatarSlot[kAS]} (ignored {e.OptionId})");
            }
        }
    }

    public List<string> GetSlotIds(string avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId)) return new List<string>();
        return _slotIdsByAvatar.TryGetValue(avatarId, out var set) ? new List<string>(set) : new List<string>();
    }

    public List<CatalogEntry> GetOptions(string avatarId, string slotId)
    {
        if (string.IsNullOrWhiteSpace(avatarId) || string.IsNullOrWhiteSpace(slotId))
            return new List<CatalogEntry>();

        return _byAvatarSlot.TryGetValue(KeyAS(avatarId, slotId), out var list) ? list : new List<CatalogEntry>();
    }

    public CatalogEntry Find(string avatarId, string slotId, string optionId)
    {
        if (string.IsNullOrWhiteSpace(avatarId) ||
            string.IsNullOrWhiteSpace(slotId) ||
            string.IsNullOrWhiteSpace(optionId))
            return null;

        _byAvatarSlotOption.TryGetValue(KeyASO(avatarId, slotId, optionId), out var e);
        return e;
    }

    public string GetDefaultOptionId(string avatarId, string slotId)
    {
        if (string.IsNullOrWhiteSpace(avatarId) || string.IsNullOrWhiteSpace(slotId))
            return "";

        return _defaultByAvatarSlot.TryGetValue(KeyAS(avatarId, slotId), out var v) ? v : "";
    }
    public List<string> CollectAllKeysForPlayer(string playerId, bool includeIcons)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in Entries)
        {
            if (e == null) continue;
            if (!string.Equals(e.AvatarId, playerId, StringComparison.OrdinalIgnoreCase)) continue;

            if (!string.IsNullOrEmpty(e.MeshKey)) set.Add(e.MeshKey);
            if (!string.IsNullOrEmpty(e.MaterialKey)) set.Add(e.MaterialKey);
            if (includeIcons && !string.IsNullOrEmpty(e.IconKey)) set.Add(e.IconKey);
        }

        return new List<string>(set);
    }

    // -------- JSON PARSE --------
    public static CatalogData ParseJson(string json)
    {
        var data = new CatalogData();
        if (string.IsNullOrWhiteSpace(json)) return data;

        CatalogJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<CatalogJsonRoot>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Catalog JSON parse error: {ex.Message}");
            return data;
        }

        if (root == null || root.entries == null) return data;

        for (int i = 0; i < root.entries.Length; i++)
        {
            var r = root.entries[i];
            if (r == null) continue;

            var e = new CatalogEntry(
                avatarId: r.avatarId,
                slotId: r.slotId,
                optionId: r.optionId,
                meshKey: r.meshKey,
                materialKey: r.materialKey,
                iconKey: r.iconKey,
                isDefault: r.isDefault,
                price: r.price
            );

            data.Entries.Add(e);
        }

        return data;
    }
    public List<string> CollectMaterialKeys(string avatarId, string slotId)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in Entries)
        {
            if (e == null) continue;

            if (!string.Equals(e.AvatarId, avatarId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(e.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(e.MaterialKey))
                set.Add(e.MaterialKey);
        }

        return new List<string>(set);
    }

    [Serializable]
    private class CatalogJsonRoot
    {
        public CatalogJsonEntry[] entries;
    }

    [Serializable]
    private class CatalogJsonEntry
    {
        public string avatarId;
        public string slotId;
        public string optionId;
        public string meshKey;
        public string materialKey;
        public string iconKey;
        public bool isDefault;
        public int price;
    }
}

public sealed class CatalogEntry
{
    public string AvatarId { get; }
    public string SlotId { get; }
    public string OptionId { get; }
    public string MeshKey { get; }
    public string MaterialKey { get; }
    public string IconKey { get; }
    public bool IsDefault { get; }
    public int Price { get; }   // 0 → bepul

    public CatalogEntry(
        string avatarId, string slotId, string optionId,
        string meshKey, string materialKey, string iconKey,
        bool isDefault, int price)
    {
        AvatarId = (avatarId ?? "").Trim();
        SlotId = (slotId ?? "").Trim();
        OptionId = (optionId ?? "").Trim();
        MeshKey = (meshKey ?? "").Trim();
        MaterialKey = (materialKey ?? "").Trim();
        IconKey = (iconKey ?? "").Trim();
        IsDefault = isDefault;
        Price = Mathf.Max(0, price);
    }
}
