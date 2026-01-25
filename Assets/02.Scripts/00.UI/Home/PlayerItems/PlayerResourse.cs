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
    public static event Action<string> OnResourseUpdated;
    private void OnEnable()
    {
        UITransilation();
        buyButton.onClick.AddListener(BuyResource);
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
        if(costOfResource == 0)
        {
            return;
        }
        int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy, 0);
        if (nyufiyAmount < costOfResource)
        {
           
            // money not enough text
            OnMoneyNotEnough?.Invoke();
            return;
        }
        ResourceName();
        BuyResourceSave(nyufiyAmount);
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

            default:
                Debug.LogWarning($"Unknown resource: {playerResources}");
                return;
        }

        string resourceName =
            $"+{amount} {LanguageManager.Instance.GetText(textId)}";

        HomeMainUI.Instance.ShowRightPopup(resourceName, iconImage.sprite);
    }
    private void BuyResourceSave(int nyufiyAmount)
    {
        string itemKey = GetItemKey(playerResources);
        if (string.IsNullOrEmpty(itemKey))
            return; // noma'lum resource bo'lsa hech narsa qilmaymiz

        int newNyufiyAmount = nyufiyAmount - costOfResource;

        int count = PlayerPrefs.GetInt(itemKey, 0);
        PlayerPrefs.SetInt(itemKey, count + 1);

        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, newNyufiyAmount);
        PlayerPrefs.Save();
       // OnNyufiyUpdated?.Invoke();
        OnResourseUpdated?.Invoke(itemKey);
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
