using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HorseSkinLoader : MonoBehaviour
{
    [Header("Horse Id")]
    [SerializeField] private string horseIdPrefKey = "ActiveHorseId";
    [SerializeField] private string defaultHorseId = "horse_01";

    // ✅ Catalogda qoladigan slotlar
    private const string SlotBody = "Body";
    private const string SlotMane = "Mane";
    private const string SlotSaddle = "Saddle";

    [Header("Skinned Mesh Renderers (Horse)")]
    [SerializeField] private SkinnedMeshRenderer bodySMR;
    [SerializeField] private SkinnedMeshRenderer maneSMR;
    [SerializeField] private SkinnedMeshRenderer tailSMR;
    [SerializeField] private SkinnedMeshRenderer saddleSMR;
    [SerializeField] private SkinnedMeshRenderer reinsSMR;
    [SerializeField] private SkinnedMeshRenderer headReinsSMR;
    [SerializeField] private SkinnedMeshRenderer eyesSMR;


    [Header("Save Flow")]
    // Pending selections (slotId -> optionId)  (faqat Body/Mane/Saddle)
    private readonly Dictionary<string, string> _pending = new(StringComparer.OrdinalIgnoreCase);

    private string _horseId;
    private bool _staticInitializedThisSession;

    // init flag (per horse)
    private string StaticInitPrefKey => $"HorseStaticInited_{_horseId}";

    private void Awake()
    {
        _horseId = PlayerPrefs.GetString(horseIdPrefKey, defaultHorseId);
    }

    private void Start()
    {
        AvatarCustomManager.RaiseHorseSkinLoad(this);
    }

    private void OnEnable()
    {
        AvatarCustomUIManager.OnSavedBtnClicked += CommitPending;
    }

    private void OnDisable()
    {
        AvatarCustomUIManager.OnSavedBtnClicked -= CommitPending;
    }

    public void SetHorseId(string horseId)
    {
        _horseId = string.IsNullOrWhiteSpace(horseId) ? defaultHorseId : horseId;
        _pending.Clear(); // ✅ old pending qolmasin
    }

    // UI "Selected" status uchun (pending + prefs)
    public string GetCurrentOptionId(string slotId)
    {
        slotId = Normalize(slotId);

        if (_pending.TryGetValue(slotId, out var p) && !string.IsNullOrEmpty(p))
            return p;

        return PlayerPrefs.GetString($"Sel_{_horseId}_{slotId}", "");
    }

    /// <summary>
    /// Scene ochilganda chaqirasan.
    /// 1) Static parts (eyes mesh/mat, tail/reins/headreins mesh) bir marta init
    /// 2) Saved/default (Body/Mane/Saddle) apply
    /// </summary>
    public async Task ApplyAllSkins()
    {
        await PlayerCatalogProvider.Instance.WaitInitializedAsync();
        await InitializeStaticPartsOnce();
        await ApplyAllFromPrefs();
    }

    /// <summary>
    /// ✅ Static parts:
    /// - Eyes mesh/material (material doimiy)
    /// - Tail/Reins/HeadReins mesh
    /// ❌ Preload yo'q, faqat LoadAssetAsync
    /// </summary>
    public async Task InitializeStaticPartsOnce()
    {
        if (_staticInitializedThisSession)
            return;
        await AddressablesService.Instance.EnsureInitializedAsync();

        // Eyes mesh
        if (eyesSMR != null && !string.IsNullOrWhiteSpace(Constants.HorseStaticMeshes.Eyes))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(Constants.HorseStaticMeshes.Eyes);
            if (mesh != null) eyesSMR.sharedMesh = mesh;
        }

        // Tail mesh
        if (tailSMR != null && !string.IsNullOrWhiteSpace(Constants.HorseStaticMeshes.Tail))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(Constants.HorseStaticMeshes.Tail);
            if (mesh != null) tailSMR.sharedMesh = mesh;
        }

        // Reins mesh
        if (reinsSMR != null && !string.IsNullOrWhiteSpace(Constants.HorseStaticMeshes.Reins))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(Constants.HorseStaticMeshes.Reins);
            if (mesh != null) reinsSMR.sharedMesh = mesh;
        }

        // HeadReins mesh
        if (headReinsSMR != null && !string.IsNullOrWhiteSpace(Constants.HorseStaticMeshes.HeadReins))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(Constants.HorseStaticMeshes.HeadReins);
            if (mesh != null) headReinsSMR.sharedMesh = mesh;
        }

        // Eyes material (doimiy)
        if (eyesSMR != null && !string.IsNullOrWhiteSpace(Constants.HorseStaticMaterials.Eyes))
        {
            await ApplyMaterial(eyesSMR, Constants.HorseStaticMaterials.Eyes);
        }
        _staticInitializedThisSession = true;
    }

    /// <summary>
    /// ✅ Preview apply (NO save)
    /// Catalog slotlar: Body/Mane/Saddle
    /// - Mane material -> Tail ham shu materialni oladi
    /// - Saddle material -> Reins + HeadReins ham shu materialni oladi
    /// </summary>
    public async Task PreviewOne(string slotId, string optionId)
    {
        slotId = Normalize(slotId);
        if (string.IsNullOrEmpty(optionId)) return;

        var entry = await PlayerCatalogProvider.Instance.FindAsync(_horseId, slotId, optionId);
        if (entry == null) return;

        // BODY
        if (slotId.Equals(SlotBody, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(entry.MeshKey) && bodySMR != null)
            {
                var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(entry.MeshKey);
                if (mesh != null) bodySMR.sharedMesh = mesh;
            }

            if (!string.IsNullOrEmpty(entry.MaterialKey) && bodySMR != null)
                await ApplyMaterial(bodySMR, entry.MaterialKey);
        }
        // MANE (-> TAIL follow)
        else if (slotId.Equals(SlotMane, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(entry.MeshKey) && maneSMR != null)
            {
                var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(entry.MeshKey);
                if (mesh != null) maneSMR.sharedMesh = mesh;
            }

            if (!string.IsNullOrEmpty(entry.MaterialKey))
            {
                if (maneSMR != null) await ApplyMaterial(maneSMR, entry.MaterialKey);
                if (tailSMR != null) await ApplyMaterial(tailSMR, entry.MaterialKey);
            }
        }
        // SADDLE (-> REINS + HEADREINS follow)
        else if (slotId.Equals(SlotSaddle, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(entry.MeshKey) && saddleSMR != null)
            {
                var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(entry.MeshKey);
                if (mesh != null) saddleSMR.sharedMesh = mesh;
            }

            if (!string.IsNullOrEmpty(entry.MaterialKey))
            {
                if (saddleSMR != null) await ApplyMaterial(saddleSMR, entry.MaterialKey);
                if (reinsSMR != null) await ApplyMaterial(reinsSMR, entry.MaterialKey);
                if (headReinsSMR != null) await ApplyMaterial(headReinsSMR, entry.MaterialKey);
            }
        }

        _pending[slotId] = optionId;
    }

    /// <summary>
    /// ✅ Scene ochilganda saved holatni qo'llash (Body/Mane/Saddle)
    /// </summary>
    public async Task ApplyAllFromPrefs()
    {
        await AddressablesService.Instance.EnsureInitializedAsync();

        await ApplyFromSavedOrDefault(SlotBody);
        await ApplyFromSavedOrDefault(SlotMane);
        await ApplyFromSavedOrDefault(SlotSaddle);

        _pending.Clear();
    }

    private async Task ApplyFromSavedOrDefault(string slotId)
    {
        string optionId = PlayerPrefs.GetString($"Sel_{_horseId}_{slotId}", "");
        if (string.IsNullOrEmpty(optionId))
            optionId = PlayerCatalogProvider.Instance.GetDefaultOptionId(_horseId, slotId);

        if (string.IsNullOrEmpty(optionId)) return;

        await PreviewOne(slotId, optionId);
    }

    // ✅ Save bosilganda commit (faqat o'zgargan slotlar)
    private void CommitPending()
    {
        if (_pending.Count == 0) return;

        foreach (var kv in _pending)
        {
            string slotId = kv.Key;      // Body/Mane/Saddle
            string optionId = kv.Value;

            PlayerPrefs.SetString($"Sel_{_horseId}_{slotId}", optionId);
        }

        PlayerPrefs.Save();
        _pending.Clear();
    }

    // ----------------- helpers -----------------

    private static string Normalize(string s) => (s ?? "").Trim();

    private async Task ApplyMaterial(Renderer r, string materialKey)
    {
        if (r == null || string.IsNullOrWhiteSpace(materialKey)) return;

        if (MaterialCacheManager.TryGet(materialKey, out var cached) && cached != null)
        {
            r.sharedMaterial = cached;
            return;
        }

        var mat = await AddressablesService.Instance.LoadAssetAsync<Material>(materialKey);
        if (mat != null)
        {
            MaterialCacheManager.Add(materialKey, mat);
            r.sharedMaterial = mat;
        }
        else
        {
            Debug.LogError($"❌ HorseSkinLoader failed to load material: {materialKey}");
        }
    }
}
