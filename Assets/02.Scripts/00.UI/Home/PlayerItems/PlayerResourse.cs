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
        buyButton.onClick.RemoveAllListeners();
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
        if(costOfResource == 0)
        {
            return;
        }

        itemName = GetItemKey(playerResources);
        if (string.IsNullOrEmpty(itemName) || CurrencyManager.Instance == null || DataManager.Instance == null)
        {
            Debug.LogWarning($"Cannot buy resource: {playerResources}");
            SoundManager.Instance?.PlayUI(UISoundType.Error);
            return;
        }

        bool success = CurrencyManager.Instance.SpendNyufiy(costOfResource, true);

        if (!success)
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            // money not enough text
            OnMoneyNotEnough?.Invoke();
            SoundManager.Instance?.PlayUI(UISoundType.Error);
            return;
        }

        BuyResourceSave();
        ResourceName();
    }
    private void ResourceName(int amount = 1)
    {
        int textId = -1;

        switch (playerResources)
        {
            case Resources.Defender:
                textId = 324;
                break;

            case Resources.WebSnare:
                textId = 322;
                break;

            case Resources.WalkZone:
                textId = 323;
                break;

            case Resources.Whiplash:
                textId = 384;
                break;

            case Resources.HorseDust:
                textId = 387;
                break;

            default:
                Debug.LogWarning($"Unknown resource: {playerResources}");
                return;
        }

        string resourceName =
            $"+{amount} {LanguageManager.Instance.GetText(textId)}";

        HomeMainUI.Instance.ShowRightPopup(resourceName, iconImage.sprite);
    }
    private void BuyResourceSave()
    {
        DataManager.Instance.AddItem(itemName, 1, true);
        itemAmount = DataManager.Instance.GetItemAmount(itemName);

        OnResourseBought?.Invoke(playerResources, itemAmount);
        OnResourseUpdated?.Invoke(itemName);
        OnNyufiyUpdated?.Invoke();

        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        SoundManager.Instance?.PlayUI(UISoundType.Success);
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
    private string GetItemKey(Resources resource)
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


}
