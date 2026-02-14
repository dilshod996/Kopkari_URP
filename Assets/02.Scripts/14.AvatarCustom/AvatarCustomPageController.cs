using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AvatarCustomPageController : MonoBehaviour
{
    [Header("Page Type")]
    public bool isHorsePage = false;

    [Header("Player Skin Type")]
    public AvatarCustomTypes.PlayerSkins skinType;

    [Header("Horse Skin Type")]
    public AvatarCustomTypes.HorseSkins horseSkinType;

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private OptionItemUI itemPrefab;
    [SerializeField] private AvatarCustomPreviewPopup popup;

    // ✅ loaders (separate)
    private PlayerSkinLoader _playerLoader;
    private HorseSkinLoader _horseLoader;

    // Pool
    private readonly List<OptionItemUI> _active = new();
    private readonly Stack<OptionItemUI> _pool = new();

    private string _builtForAvatarId;
    private string _avatarId;

    private void OnEnable()
    {
        // avatar id
        _avatarId = isHorsePage
            ? PlayerPrefs.GetString("ActiveHorseId", "horse_01")
            : PlayerPrefs.GetString("ActivePlayerId", "player_01");

        // ✅ subscribe correct event
        if (isHorsePage)
            AvatarCustomManager.OnHorseSkinLoad += HandleHorseLoaderReady;
        else
            AvatarCustomManager.OnPlayerSkinLoad += HandlePlayerLoaderReady;

        // try build
        _ = BuildIfNeededAsync();
    }

    private void OnDisable()
    {
        if (isHorsePage)
            AvatarCustomManager.OnHorseSkinLoad -= HandleHorseLoaderReady;
        else
            AvatarCustomManager.OnPlayerSkinLoad -= HandlePlayerLoaderReady;
    }

    private void HandlePlayerLoaderReady(PlayerSkinLoader loader)
    {
        _playerLoader = loader;
        if (isActiveAndEnabled && !isHorsePage)
            _ = BuildIfNeededAsync();
    }

    private void HandleHorseLoaderReady(HorseSkinLoader loader)
    {
        _horseLoader = loader;
        if (isActiveAndEnabled && isHorsePage)
            _ = BuildIfNeededAsync();
    }

    public async Task BuildIfNeededAsync()
    {
        // ✅ require correct loader
        if (isHorsePage)
        {
            if (_horseLoader == null) return;
        }
        else
        {
            if (_playerLoader == null) return;
        }

        if (!string.IsNullOrEmpty(_builtForAvatarId) &&
            _builtForAvatarId == _avatarId &&
            _active.Count > 0)
            return;

        await RebuildAsync(_avatarId);
    }

    private async Task RebuildAsync(string avatarId)
    {
        _builtForAvatarId = avatarId;

        DespawnAll();

        string slotId = isHorsePage
            ? horseSkinType.ToString()   // "Body" / "Mane" / "Tail" / "Saddle"
            : GetPlayerSlotId();         // "Hair" / "Facehair" / "Upper" / "Lower"

        var options = await PlayerCatalogProvider.Instance.GetOptionsAsync(avatarId, slotId);

        Debug.Log($"[{name}] Build {avatarId} / {slotId} -> {options.Count}");

        foreach (var e in options)
        {
            var item = Spawn();

            if (isHorsePage)
            {
                item.Setup(
                    entry: e,
                    popup: popup,
                    horseId: avatarId,
                    slotId: slotId,
                    loader: _horseLoader
                );
            }
            else
            {
                item.Setup(
                    entry: e,
                    popup: popup,
                    playerId: avatarId,
                    slotId: slotId,
                    loader: _playerLoader
                );
            }
        }
    }

    private OptionItemUI Spawn()
    {
        OptionItemUI item = (_pool.Count > 0) ? _pool.Pop() : Instantiate(itemPrefab, contentRoot);
        item.transform.SetParent(contentRoot, false);
        item.gameObject.SetActive(true);
        _active.Add(item);
        return item;
    }

    private void DespawnAll()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            var it = _active[i];
            if (!it) continue;
            it.gameObject.SetActive(false);
            _pool.Push(it);
        }
        _active.Clear();
    }

    private string GetPlayerSlotId()
    {
        return skinType switch
        {
            AvatarCustomTypes.PlayerSkins.Hair => "Hair",
            AvatarCustomTypes.PlayerSkins.FaceHair => "Facehair",
            AvatarCustomTypes.PlayerSkins.Upper => "Upper",
            AvatarCustomTypes.PlayerSkins.Lower => "Lower",
            _ => "Hair"
        };
    }
}
