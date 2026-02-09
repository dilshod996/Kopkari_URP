using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
using static UnityEngine.EventSystems.EventTrigger;

public class OptionItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text btnText;
    [SerializeField] private GameObject selectedMark;

    [SerializeField] private GameObject notOpendObj;
    [SerializeField] private TMP_Text priceText;
    private CatalogEntry _entry;
    private AvatarCustomPreviewPopup _popup;
    private PlayerSkinLoader _loader;
    private string _playerId;
    private string _slotId;

    public static Action<string, string> OnSelectionChanged; // playerId, slotId
    public static Action OnNotEnoughCoins;
    public static Action OnCoinUpdated;


    private void OnEnable()
    {
        OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        OnSelectionChanged -= HandleSelectionChanged;
    }
    public void Setup(
     CatalogEntry entry,
     AvatarCustomPreviewPopup popup,
     string playerId,
     string slotId,
     PlayerSkinLoader loader)
    {
        _entry = entry;
        _popup = popup;
        _playerId = playerId;
        _slotId = slotId;
        _loader = loader;

        _ = LoadIconAsync(entry.IconKey);

        bool isUnlocked = entry.IsDefault || IsUnlocked(entry);
        bool isSelected = IsSelected(entry);

        // ---- LOCK / OPEN STATE ----

        if (notOpendObj)
            notOpendObj.SetActive(!isUnlocked);   // ✅ ASOSIY FIX

        if (selectedMark)
            selectedMark.SetActive(isSelected);

        // ---- BUTTON TEXT ----
        if (btnText)
        {
            if (!isUnlocked)
            {
                btnText.text = "Buy";
                if (priceText)
                    priceText.text = entry.Price.ToString();
            }
            else
            {
                btnText.text = isSelected ? "Selected" : "Change";
                if (priceText)
                    priceText.text = "";
            }
        }

        // ---- CLICK ----
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_entry == null) return;

        // Selected bo'lsa popup ochmaymiz (xohlasang ochsa ham bo'ladi)
        if (IsSelected(_entry))
            return;

        bool isUnlocked = _entry.IsDefault || IsUnlocked(_entry);

        if (_popup != null)
        {
            // popupda apply/buy action bitta bo'ladi
            _popup.Show(
                _entry,
                _playerId, 
                _slotId,
                icon.sprite,
                onApply: async () =>
                {
                    if (isUnlocked)
                    {
                        await ApplyToPlayer();
                    }
                    else
                    {
                        bool bought = TryBuy(_entry);
                        if (bought)
                            await ApplyToPlayer();
                    }

                    // list item UI refresh (lock/selected)
                    Setup(_entry, _popup, _playerId, _slotId, _loader);
                });
        }
        else
        {
            // popup yo'q bo'lsa: unlocked bo'lsa apply, locked bo'lsa buy+apply
            _ = HandleNoPopupAsync(isUnlocked);
        }
    }

    private async Task HandleNoPopupAsync(bool isUnlocked)
    {
        if (isUnlocked)
        {
            await ApplyToPlayer();
        }
        else
        {
            if (TryBuy(_entry))
                await ApplyToPlayer();
        }

        Setup(_entry, _popup, _playerId, _slotId, _loader);
    }

    private async Task ApplyToPlayer()
    {
        if (_loader != null && _entry != null)
        {
            await _loader.ApplyOne(_slotId, _entry.OptionId);
            OnSelectionChanged?.Invoke(_playerId, _slotId);
        }
            

    }

    private bool TryBuy(CatalogEntry entry)
    {
        if (entry == null) return false;

        int coins = PlayerPrefs.GetInt(Constants.Coins.Coin, 0);
        if (coins < entry.Price)
        {
            Debug.Log("❌ Not enough coins");
            OnNotEnoughCoins?.Invoke();  
            return false;
        }

        PlayerPrefs.SetInt(Constants.Coins.Coin, coins - entry.Price);
        OnCoinUpdated?.Invoke();
        string unlockKey = $"Unlock_{_playerId}_{_slotId}_{entry.OptionId}";
        PlayerPrefs.SetInt(unlockKey, 1);
        PlayerPrefs.Save();

        return true;
    }

    private async Task LoadIconAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || icon == null) return;

        await AddressablesService.Instance.EnsureInitializedAsync();
        var sprite = await AddressablesService.Instance.LoadAssetAsync<Sprite>(key);
        if (sprite != null) icon.sprite = sprite;
    }

    private bool IsUnlocked(CatalogEntry entry)
    {
        string key = $"Unlock_{_playerId}_{_slotId}_{entry.OptionId}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private bool IsSelected(CatalogEntry entry)
    {
        string key = $"Sel_{_playerId}_{_slotId}";
        return PlayerPrefs.GetString(key, "") == entry.OptionId;
    }
    private void HandleSelectionChanged(string playerId, string slotId)
    {
        if (_playerId == playerId && _slotId == slotId)
            RefreshState();
    }
    private void RefreshState()
    {
        if (_entry == null) return;

        bool isUnlocked = _entry.IsDefault || IsUnlocked(_entry);
        bool isSelected = IsSelected(_entry);

        if (notOpendObj) notOpendObj.SetActive(!isUnlocked);
        if (selectedMark) selectedMark.SetActive(isSelected);

        if (btnText)
        {
            if (!isUnlocked)
            {
                btnText.text = /*entry.Price.ToString()*/"Buy";   // listda narx ko'rinsin
                priceText.text = _entry.Price.ToString();
            }
            else
                btnText.text = isSelected ? "Selected" : "Change";
        }
    }

}
