using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AvatarCustomPageController : MonoBehaviour
{
    public AvatarCustomTypes.PlayerSkins skinType;

    [SerializeField] private Transform contentRoot;
    [SerializeField] private OptionItemUI itemPrefab;
    [SerializeField] private AvatarCustomPreviewPopup popup;

    [SerializeField] private PlayerSkinLoader _loader;

    // Pool
    private readonly List<OptionItemUI> _active = new();
    private readonly Stack<OptionItemUI> _pool = new();

    private string _builtForPlayerId;
    private string playerId;

    private void OnEnable()
    {
        playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");
        // event subscribe
        AvatarCustomManager.OnPlayerSkinLoad += HandleLoaderReady;

        // agar loader oldinroq kelgan bo'lsa (static cache bo'lsa), build qilib yuboramiz
        _ = BuildIfNeededAsync();
    }

    private void OnDisable()
    {
        AvatarCustomManager.OnPlayerSkinLoad -= HandleLoaderReady;
    }

    private void HandleLoaderReady(PlayerSkinLoader loader)
    {
        _loader = loader;

        // page ochiq bo'lsa va loader endi kelsa - darrov build
        if (isActiveAndEnabled)
            _ = BuildIfNeededAsync();
    }

    public async Task BuildIfNeededAsync()
    {
        // loader bo'lmasa build qilmaymiz
        if (_loader == null) return;

        Debug.Log("Coming " + _active.Count);

        if (!string.IsNullOrEmpty(_builtForPlayerId) &&
            _builtForPlayerId == playerId &&
            _active.Count > 0)
            return;

        await RebuildAsync(playerId);
    }

    private async Task RebuildAsync(string playerId)
    {
        _builtForPlayerId = playerId;

        DespawnAll();

        string slotId = GetSlotId(); // "Hair" / "FaceHair" / "Upper" / "Lower"

        var options = await PlayerCatalogProvider.Instance.GetOptionsAsync(playerId, slotId);
        Debug.Log("Get options count: " + options.Count);
        foreach (var e in options)
        {
            var item = Spawn();
            item.Setup(
                entry: e,
                popup: popup,
                playerId: playerId,
                slotId: slotId,
                loader: _loader
            );
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
    private string GetSlotId()
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
