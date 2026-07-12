using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourse : MonoBehaviour
{
    public enum Resources
    {
        None,
        WalkZone,
        Defender,
        WebSnare,
        Whiplash,
        HorseDust
    }
    [SerializeField] private Resources playerResources = Resources.None;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text resourseName;
    [SerializeField] private TMP_Text resourseCost;
    [SerializeField] private Image iconImage;
    [SerializeField] private int resourseTransilationId;
    [SerializeField] private int costOfResource;
    public static event Action OnMoneyNotEnough;
    public static event Action OnNyufiyUpdated;


    private string itemName;
    private int itemAmount;
    public static event Action<string> OnResourseUpdated;
    public static event Action<Resources, int> OnResourseBought;
    private void OnEnable()
    {
        UITransilation();
        buyButton.onClick.AddListener(BuyResource);
        GetData();
    }
    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(BuyResource);
    }
    private void UITransilation()
    {
        if(LanguageManager.Instance != null)
        {
            resourseName.text = LanguageManager.Instance.GetText(resourseTransilationId);
            if(costOfResource == 0)
            {
                resourseCost.text = LanguageManager.Instance.GetText(385);
                buyButton.interactable = false;
            }
            else
            {
                resourseCost.text = costOfResource.ToString();
            }
        }
    }

    private void BuyResource()
    {
        TryBuyResource(playerResources, costOfResource, iconImage != null ? iconImage.sprite : null, out itemAmount);
    }
    public static bool TryBuyResource(Resources resource, int cost, Sprite icon, out int amount, bool notifyNotEnough = true)
    {
        amount = 0;

        if (cost <= 0)
            return false;

        string itemKey = GetItemKey(resource);
        if (string.IsNullOrEmpty(itemKey) || CurrencyManager.Instance == null || DataManager.Instance == null)
        {
            Debug.LogWarning($"Cannot buy resource: {resource}");
            SoundManager.Instance?.PlayUI(UISoundType.Error);
            return false;
        }

        bool success = CurrencyManager.Instance.SpendNyufiy(cost, true);

        if (!success)
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            if (notifyNotEnough)
                OnMoneyNotEnough?.Invoke();
            SoundManager.Instance?.PlayUI(UISoundType.Error);
            return false;
        }

        DataManager.Instance.AddItem(itemKey, 1, true);
        amount = DataManager.Instance.GetItemAmount(itemKey);

        OnResourseBought?.Invoke(resource, amount);
        OnResourseUpdated?.Invoke(itemKey);
        OnNyufiyUpdated?.Invoke();

        HomeMainUI.Instance?.ShowRightPopup($"+1 {GetResourceName(resource)}", icon);
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        SoundManager.Instance?.PlayUI(UISoundType.Success);
        return true;
    }

    private void GetData()
    {
        itemName = GetItemKey(playerResources);
        if (string.IsNullOrEmpty(itemName))
            return; // noma'lum resource bo'lsa hech narsa qilmaymiz
        if (DataManager.Instance == null)
            return;

        itemAmount = DataManager.Instance.GetItemAmount(itemName);
        // resourceAmount.text = $"{itemAmount}X";
    }
    public static string GetItemKey(Resources resource)
    {
        switch (resource)
        {
            case Resources.WalkZone:
                return Constants.PlayerItems.SlowDown;
            case Resources.Defender:
                return Constants.PlayerItems.Defense;
            case Resources.WebSnare:
                return Constants.PlayerItems.WebSnare;
            case Resources.Whiplash:
                return Constants.PlayerItems.Whip;
            case Resources.HorseDust:
                return Constants.PlayerItems.Horsedust;
            default:
                return null;
        }
    }

    public static string GetResourceName(Resources resource)
    {
        int textId = GetResourceLanguageId(resource);
        if (textId == -1)
            return resource.ToString();

        return LanguageManager.Instance != null ? LanguageManager.Instance.GetText(textId) : resource.ToString();
    }

    public static int GetResourceLanguageId(Resources resource)
    {
        switch (resource)
        {
            case Resources.Defender:
                return 324;
            case Resources.WebSnare:
                return 322;
            case Resources.WalkZone:
                return 323;
            case Resources.Whiplash:
                return 384;
            case Resources.HorseDust:
                return 387;
            default:
                return -1;
        }
    }


}
