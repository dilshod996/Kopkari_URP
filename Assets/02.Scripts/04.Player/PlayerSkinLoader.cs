using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerSkinLoader : MonoBehaviour
{
    [Header("Slot Bindings (Inspector)")]
    [SerializeField] private SkinSlot[] slotBindings;

    private readonly Dictionary<string, SkinnedMeshRenderer> _slots =
        new Dictionary<string, SkinnedMeshRenderer>(StringComparer.OrdinalIgnoreCase);

    private string _playerId;

    private readonly Dictionary<string, string> _pending = new Dictionary<string, string>();
    private void Awake()
    {
        BuildSlotMap();
    }

    private void Start()
    {
        _playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");
        AvatarCustomManager.RaisePlayerSkinLoad(this);
    }
    private void OnEnable()
    {
        AvatarCustomUIManager.OnSavedBtnClicked += CommitPending;  // static event
        AvatarCustomUIManager.OnRevertPreviewRequested += RevertPending;
    }

    private void OnDisable()
    {
        AvatarCustomUIManager.OnSavedBtnClicked -= CommitPending;
        AvatarCustomUIManager.OnRevertPreviewRequested -= RevertPending;
    }
    private void BuildSlotMap()
    {
        _slots.Clear();

        if (slotBindings == null) return;

        foreach (var b in slotBindings)
        {
            if (b == null || b.target == null) continue;

            string key = b.slotId.ToString();
            if (_slots.ContainsKey(key))
            {
                Debug.LogWarning($"⚠ Duplicate slot binding: {key}");
                continue;
            }

            _slots.Add(key, b.target);
        }
    }
    public async Task ApplyAllSkins()
    {
        _playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");
        await PlayerCatalogProvider.Instance.WaitInitializedAsync(); // yoki EnsureCatalogAsync()
        await ApplyAllFromPrefs();

    }
    public async Task ApplyAllFromPrefs()
    {
        await AddressablesService.Instance.EnsureInitializedAsync();

        foreach (var kv in _slots)
        {
            string slotId = kv.Key;
            string optionId = AvatarCustomPrefs.GetSelection(_playerId, slotId);
            if (string.IsNullOrEmpty(optionId))
                optionId = PlayerCatalogProvider.Instance.GetDefaultOptionId(_playerId, slotId);

            if (!string.IsNullOrEmpty(optionId))
                await ApplyVisual(slotId, optionId);
        }
    }

    public async Task ApplyOne(string slotId, string optionId)
    {
        await ApplyVisual(slotId, optionId);

        AvatarCustomPrefs.SetSelection(_playerId, slotId, optionId);
        PlayerPrefs.Save();
    }
    public async Task PreviewOne(string slotId, string optionId)
    {
        await ApplyVisual(slotId, optionId);

        // ✅ faqat pendingga yozamiz (prefsga emas)
        _pending[slotId] = optionId;
    }
    private void CommitPending()
    {
        if (_pending.Count == 0) return;

        foreach (var kv in _pending)
        {
            string slotId = kv.Key;
            string optionId = kv.Value;

            AvatarCustomPrefs.SetSelection(_playerId, slotId, optionId);
            CustomizationManager.Instance?.SyncSelection(_playerId, slotId, optionId);
        }

        PlayerPrefs.Save();
        _pending.Clear();
    }

    private async void RevertPending()
    {
        if (_pending.Count == 0) return;

        List<string> slots = new List<string>(_pending.Keys);
        _pending.Clear();

        foreach (string slotId in slots)
        {
            string optionId = AvatarCustomPrefs.GetSelection(_playerId, slotId);
            if (string.IsNullOrEmpty(optionId))
                optionId = PlayerCatalogProvider.Instance.GetDefaultOptionId(_playerId, slotId);

            if (!string.IsNullOrEmpty(optionId))
                await ApplyVisual(slotId, optionId);

            OptionItemUI.OnSelectionChanged?.Invoke(_playerId, slotId);
        }
    }

    private async Task ApplyVisual(string slotId, string optionId)
    {
        if (!_slots.TryGetValue(slotId, out var smr))
            return;

        var entry = await PlayerCatalogProvider.Instance.FindAsync(_playerId, slotId, optionId);
        if (entry == null) return;

        await ApplyEntry(smr, entry);
    }

    private async Task ApplyEntry(SkinnedMeshRenderer smr, CatalogEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.MeshKey))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(entry.MeshKey);
            if (mesh != null)
                smr.sharedMesh = mesh;
        }

        if (!string.IsNullOrEmpty(entry.MaterialKey))
        {
            if (MaterialCacheManager.TryGet(entry.MaterialKey, out var cached) && cached != null)
            {
                smr.sharedMaterial = cached;
            }
            else
            {
                var mat = await AddressablesService.Instance.LoadAssetAsync<Material>(entry.MaterialKey);
                if (mat != null)
                {
                    MaterialCacheManager.Add(entry.MaterialKey, mat);
                    smr.sharedMaterial = mat;
                }
            }
        }
    }
    public string GetCurrentOptionId(string slotId)
    {
        if (string.IsNullOrEmpty(slotId)) return "";

        // 1) pending bor bo'lsa shuni qaytaramiz
        if (_pending != null && _pending.TryGetValue(slotId, out var pendingId) && !string.IsNullOrEmpty(pendingId))
            return pendingId;

        // 2) bo'lmasa prefs
        return AvatarCustomPrefs.GetSelection(_playerId, slotId);
    }

}
