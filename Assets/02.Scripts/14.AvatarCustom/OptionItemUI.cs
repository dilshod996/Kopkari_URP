using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
using static UnityEngine.EventSystems.EventTrigger;

public class OptionItemUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image buyBtnBg;
    private static readonly Color horseCardBgColor = new Color32(245, 154, 0, 255);
    private static readonly Color playerCardBgColor = new Color32(22, 180, 232, 255);
    private static readonly Color horseBuyBtnBgColor = new Color32(255, 206, 32, 255);
    private static readonly Color playerBuyBtnBgColor = new Color32(88, 234, 255, 255);
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text btnText;
    //[SerializeField] private GameObject selectedMark;

    //[SerializeField] private GameObject notOpendObj;
    //[SerializeField] private TMP_Text priceText;
    private CatalogEntry _entry;
    private AvatarCustomPreviewPopup _popup;
    private PlayerSkinLoader _loader;
    private string _playerId;
    private string _slotId;

    public static Action<string, string> OnSelectionChanged; // playerId, slotId
    public static Action OnNotEnoughCoins;
    public static Action OnCoinUpdated;
    private HorseSkinLoader _horseLoader;
    private bool _isHorse;

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
        ApplyTheme(true);
        _entry = entry;
        _popup = popup;
        _playerId = playerId;
        _slotId = slotId;
        _loader = loader;

        _ = LoadIconAsync(entry.IconKey);

        bool isUnlocked = entry.IsDefault || IsUnlocked(entry);
        bool isSelected = IsSelected(entry);

        // ---- LOCK / OPEN STATE ----

        //if (notOpendObj)
        //    notOpendObj.SetActive(!isUnlocked);   // ✅ ASOSIY FIX

        //if (selectedMark)
        //    selectedMark.SetActive(isSelected);

        // ---- BUTTON TEXT ----
        if (btnText)
        {
            if (!isUnlocked)
            {
                btnText.text = "Unlock";
                //if (priceText)
                //    priceText.text = entry.Price.ToString();
            }
            else
            {
                btnText.text = isSelected ? "Selected" : "Change";
                //if (priceText)
                //    priceText.text = "";
            }
        }

        // ---- CLICK ----
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }
    public void Setup(
      CatalogEntry entry,
      AvatarCustomPreviewPopup popup,
      string horseId,
      string slotId,
      HorseSkinLoader loader)
    {
        ApplyTheme(false);
        _isHorse = true;
        _horseLoader = loader;

        _entry = entry;
        _popup = popup;
        _playerId = horseId;
        _slotId = slotId;

        _ = LoadIconAsync(entry.IconKey);

        bool isUnlocked = entry.IsDefault || IsUnlocked(entry);
        bool isSelected = IsSelected(entry);

        //if (notOpendObj)
        //    notOpendObj.SetActive(!isUnlocked);

        //if (selectedMark)
        //    selectedMark.SetActive(isSelected);

        if (btnText)
        {
            if (!isUnlocked)
            {
                btnText.text = "Buy";
                //if (priceText) priceText.text = entry.Price.ToString();
            }
            else
            {
                btnText.text = isSelected ? "Selected" : "Change";
                //if (priceText) priceText.text = "";
            }
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_entry == null) return;

        // Selected bo'lsa popup ochmaymiz
        if (IsSelected(_entry))
            return;

        bool isUnlocked = _entry.IsDefault || IsUnlocked(_entry);

        if (_popup != null)
        {
            _popup.Show(
                _entry,
                _playerId,
                _slotId,
                icon.sprite,
                onApply: async () =>
                {
                    bool isUnlockedNow = isUnlocked;

                    if (isUnlockedNow)
                    {
                        if (_isHorse)
                        {
                            await ApplyToHorse();
                            Setup(_entry, _popup, _playerId, _slotId, _horseLoader); // ✅ horse refresh
                        }
                        else
                        {
                            await ApplyToPlayer();
                            Setup(_entry, _popup, _playerId, _slotId, _loader);      // ✅ player refresh (old)
                        }
                        return true;
                    }
                    else
                    {
                        bool bought = TryBuy(_entry);

                        if (!bought)
                        {
                            if (_isHorse)
                                Setup(_entry, _popup, _playerId, _slotId, _horseLoader);
                            else
                                Setup(_entry, _popup, _playerId, _slotId, _loader);
                            return false;
                        }

                        if (_isHorse)
                        {
                            await ApplyToHorse();
                            Setup(_entry, _popup, _playerId, _slotId, _horseLoader);
                        }
                        else
                        {
                            await ApplyToPlayer();
                            Setup(_entry, _popup, _playerId, _slotId, _loader);
                        }

                        return true;
                    }
                });
        }
        else
        {
            _ = HandleNoPopupAsync(isUnlocked);
        }
    }


    private async Task HandleNoPopupAsync(bool isUnlocked)
    {
        if (isUnlocked)
        {
            if (_isHorse) await ApplyToHorse();
            else await ApplyToPlayer();
        }
        else
        {
            if (TryBuy(_entry))
            {
                if (_isHorse) await ApplyToHorse();
                else await ApplyToPlayer();
            }
        }

        if (_isHorse)
            Setup(_entry, _popup, _playerId, _slotId, _horseLoader);
        else
            Setup(_entry, _popup, _playerId, _slotId, _loader);
    }
    private void ApplyTheme(bool player)
    {
        background.color = player ? playerCardBgColor : horseCardBgColor;
        buyBtnBg.color = player ? playerBuyBtnBgColor : horseBuyBtnBgColor;
    }


    private async Task ApplyToPlayer()
    {
        if (_loader != null && _entry != null)
        {
            await _loader.PreviewOne(_slotId, _entry.OptionId);
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
        if (entry == null) return false;

        // ✅ HORSE: pending+prefs
        if (_isHorse && _horseLoader != null)
        {
            string cur = _horseLoader.GetCurrentOptionId(_slotId);
            return cur == entry.OptionId;
        }

        // ✅ PLAYER: eski holat (o'zgarmasin)
        if (_loader != null)
        {
            string cur = _loader.GetCurrentOptionId(_slotId);
            return cur == entry.OptionId;
        }

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

        //if (notOpendObj) notOpendObj.SetActive(!isUnlocked);
        //if (selectedMark) selectedMark.SetActive(isSelected);

        if (btnText)
        {
            if (!isUnlocked)
            {
                btnText.text = /*entry.Price.ToString()*/"Unlock";   // listda narx ko'rinsin
                //priceText.text = _entry.Price.ToString();
            }
            else
                btnText.text = isSelected ? "Selected" : "Change";
        }
    }
    private async Task ApplyToHorse()
    {
        if (_horseLoader != null && _entry != null)
        {
            await _horseLoader.PreviewOne(_slotId, _entry.OptionId);

            // ✅ UI refresh: o'sha slot
            OnSelectionChanged?.Invoke(_playerId, _slotId);

            // ✅ Mane tanlansa Tail ham (va aksincha) refresh bo'lsin
            if (IsManeTailSlot(_slotId))
            {
                OnSelectionChanged?.Invoke(_playerId, "Mane");
                OnSelectionChanged?.Invoke(_playerId, "Tail");
            }
        }
    }

    private bool IsManeTailSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId)) return false;
        slotId = slotId.Trim();
        return slotId.Equals("Mane", StringComparison.OrdinalIgnoreCase)
            || slotId.Equals("Tail", StringComparison.OrdinalIgnoreCase);
    }


}
