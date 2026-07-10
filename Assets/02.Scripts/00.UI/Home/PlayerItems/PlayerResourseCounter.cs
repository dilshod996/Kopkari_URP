using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourseCounter : MonoBehaviour
{
    public PlayerResourse.Resources resources;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button infoButton;
    [SerializeField] private ResourceInfoDetailsPopup detailsPopup;
    [SerializeField] private Color itemColor = Color.white;
    [SerializeField] private int costOfResource;

    private string itemName;
    private int itemAmount;

    private void OnEnable()
    {
        GetData();
        PlayerResourse.OnResourseBought += GetSetResource;
        infoButton?.onClick.AddListener(OpenResourceDetails);

        if (nameText != null)
            nameText.text = GetResourceName();
    }

    private void OnDisable()
    {
        PlayerResourse.OnResourseBought -= GetSetResource;
        infoButton?.onClick.RemoveListener(OpenResourceDetails);
    }

    private void GetSetResource(PlayerResourse.Resources comeResource, int amount)
    {
        if (resources != comeResource)
            return;

        itemAmount = amount;
        SetCountText();
    }

    private void GetData()
    {
        itemName = GetItemKey(resources);
        if (string.IsNullOrEmpty(itemName))
            return;

        if (DataManager.Instance == null)
            return;

        itemAmount = DataManager.Instance.GetItemAmount(itemName);
        SetCountText();
    }

    private void OpenResourceDetails()
    {
        ResourceInfoDetailsPopup popup = detailsPopup != null ? detailsPopup : ResourceInfoDetailsPopup.Instance;
        if (popup == null)
            return;

        ResourceInfoDetailsPopup.ResourceDetails details = ResourceInfoDetailsPopup.ResourceDetails.Player(
            GetResourceName(),
            icon != null ? icon.sprite : null,
            costOfResource,
            itemColor,
            GetResourceEffectName(),
            $"X{itemAmount}");

        popup.Show(details, costOfResource > 0, () => BuyResourceFromPopup(popup));
        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
    }

    private void BuyResourceFromPopup(ResourceInfoDetailsPopup popup)
    {
        bool success = PlayerResourse.TryBuyResource(
            resources,
            costOfResource,
            icon != null ? icon.sprite : null,
            out int amount);

        if (!success)
        {
            popup?.ShowNotEnoughNyufiy();
            return;
        }

        itemAmount = amount;
        SetCountText();
        popup?.Close();
    }

    private void SetCountText()
    {
        if (countText != null)
            countText.text = $"X{itemAmount}";
    }

    private string GetResourceName()
    {
        return PlayerResourse.GetResourceName(resources);
    }

    private string GetResourceEffectName()
    {
        return GetLocalizedText(GetResourceEffectNameLanguageId(resources));
    }

    private int GetResourceEffectNameLanguageId(PlayerResourse.Resources resource)
    {
        switch (resource)
        {
            case PlayerResourse.Resources.WalkZone:
                return 556;
            case PlayerResourse.Resources.Defender:
                return 555;
            case PlayerResourse.Resources.WebSnare:
                return 554;
            case PlayerResourse.Resources.Whiplash:
                return 456;
            case PlayerResourse.Resources.HorseDust:
                return 557;
            default:
                return -1;
        }
    }

    private string GetLocalizedText(int id)
    {
        return id != -1 && LanguageManager.Instance != null ? LanguageManager.Instance.GetText(id) : "";
    }

    private string GetItemKey(PlayerResourse.Resources resource)
    {
        return PlayerResourse.GetItemKey(resource);
    }
}
