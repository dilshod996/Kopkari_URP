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
    [SerializeField] private TMP_Text nameCodeText;
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
        _horseLoader = null;
        _isHorse = false;

        _ = LoadIconAsync(entry.IconKey);

        bool isUnlocked = entry.IsDefault || IsUnlocked(entry);
        bool isSelected = IsSelected(entry);
        if (nameCodeText.text != null)
            nameCodeText.text = LanguageManager.Instance.GetText(entry.NameCode);
        UpdateButtonText(isUnlocked, isSelected);

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
        _loader = null;

        _entry = entry;
        _popup = popup;
        _playerId = horseId;
        _slotId = slotId;

        _ = LoadIconAsync(entry.IconKey);

        bool isUnlocked = entry.IsDefault || IsUnlocked(entry);
        bool isSelected = IsSelected(entry);

        if(nameCodeText.text!=null)
            nameCodeText.text = LanguageManager.Instance.GetText(entry.NameCode);
        UpdateButtonText(isUnlocked, isSelected);

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private async void OnClick()
    {
        if (_entry == null) return;

        bool isUnlocked = _entry.IsDefault || IsUnlocked(_entry);

        // Unlocked selected bo'lsa popup ochmaymiz. Locked preview selected bo'lsa buy popup ochilsin.
        if (IsSelected(_entry) && isUnlocked)
            return;

        if (!IsSelected(_entry))
            await ApplyPreview();

        Sprite popupSprite = icon != null ? icon.sprite : null;

        if (_popup != null)
        {
            _popup.Show(
                _entry,
                _playerId,
                _slotId,
                popupSprite,
                onApply: BuyPreviewedItem);
        }
        else
        {
            if (_isHorse)
                Setup(_entry, _popup, _playerId, _slotId, _horseLoader);
            else
                Setup(_entry, _popup, _playerId, _slotId, _loader);
        }

        RefreshState();
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

    private async Task ApplyPreview()
    {
        if (_entry == null) return;

        if (_isHorse)
            await ApplyToHorse();
        else
            await ApplyToPlayer();

        AvatarCustomizationCart.Register(_entry, _playerId, _slotId);
    }

    private Task<bool> BuyPreviewedItem()
    {
        if (_entry == null)
            return Task.FromResult(false);

        if (_entry.IsDefault || IsUnlocked(_entry))
            return Task.FromResult(true);

        if (_entry.Price > 0)
        {
            CurrencyManager currency = CurrencyManager.Instance;
            if (currency == null || !currency.SpendCoin(_entry.Price, true))
            {
                Debug.Log("Not enough coins");
                HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
                OnNotEnoughCoins?.Invoke();
                return Task.FromResult(false);
            }
        }

        AvatarCustomPrefs.SetUnlocked(_playerId, _slotId, _entry.OptionId);
        PlayerPrefs.Save();
        CustomizationManager.Instance?.SyncUnlock(_playerId, _slotId, _entry.OptionId);
        AvatarCustomizationCart.NotifyChanged();
        OnCoinUpdated?.Invoke();
        RefreshState();
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);

        return Task.FromResult(true);
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
        return AvatarCustomPrefs.IsUnlocked(_playerId, _slotId, entry.OptionId);
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

        return AvatarCustomPrefs.IsSelected(_playerId, _slotId, entry.OptionId);
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

        UpdateButtonText(isUnlocked, isSelected);
    }

    private void UpdateButtonText(bool isUnlocked, bool isSelected)
    {
        if (!btnText) return;

        if (isSelected)
        {
            btnText.text = LanguageManager.Instance.GetText(425);
            return;
        }

        if (!isUnlocked)
        {
            btnText.text = LanguageManager.Instance.GetText(_isHorse ? 424 : 427);
            return;
        }

        btnText.text = LanguageManager.Instance.GetText(426);
    }
    private async Task ApplyToHorse()
    {
        if (_horseLoader != null && _entry != null)
        {
            await _horseLoader.PreviewOne(_slotId, _entry.OptionId);
            if (_entry.SlotId == "Body")
            {
                Debug.Log("Body bought");
            }

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
