using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodInfo : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text nameFood;
    [SerializeField] private Image imageofFood;

    [SerializeField] private int nameFoodId=-1;
    [Header("UI Settings")]
    [SerializeField] private Button buyBtn;
    [SerializeField] private TMP_Text foodCostText;

    [SerializeField] private int foodCost;

    public static event Action<float, float, float> OnFoodAddToHorse;
    public static event Action<int> OnNyufiyUpdate;
    public static event Action OnMoneyNotEnough;
    public enum HorseFood
    {
        None,
        Wheat,
        Barley,
        Apple,
        Water,
        StaminWater
    }
    [SerializeField] private HorseFood food;

    private void OnEnable()
    {
        TextTransilations();
        buyBtn.onClick.AddListener(BuyFood);
    }
    private void OnDisable()
    {
        buyBtn.onClick.RemoveAllListeners();
    }


    private void TextTransilations()
    {
        if (nameFoodId != -1)
            nameFood.text = LanguageManager.Instance.GetText(nameFoodId);
        if(foodCostText != null)
            foodCostText.text = foodCost > 0 ? $"{foodCost:N0}" : "0";
    }

    private void BuyFood()
    {
        int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        if (nyufiyAmount > foodCost)
        {
            nyufiyAmount -= foodCost;
            
            BuyFeedHorse(food);
            SoundManager.Instance.PlayUI(UISoundType.Success);
            PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
            OnNyufiyUpdate?.Invoke(nyufiyAmount);
        }
        else
        {
            
            OnMoneyNotEnough?.Invoke();
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            SoundManager.Instance.PlayUI(UISoundType.Error);
        }
    }
    private void BuyFeedHorse(HorseFood foodType)
    {
        int langId = -1;
        switch (foodType)
        {
            case HorseFood.Water:
                AddSupplies(0, 7, 0);// 300
                langId = 204;
                break;
            case HorseFood.Apple:
                AddSupplies(4, 0, 4);//500
                langId = 205;
                break;
            case HorseFood.Wheat:
                AddSupplies(6, 0, 8);//720
                langId = 206;
                break;
            case HorseFood.Barley:
                AddSupplies(7, 0, 10); //910
                langId = 207;
                break;
            case HorseFood.StaminWater:
                AddSupplies(0, 6, 13); // 1220
                langId = 208;
                break;
        }
        HomeMainUI.Instance?.ShowRightPopup(LanguageManager.Instance.GetText(langId), imageofFood.sprite);
    }
    private void AddSupplies(float powerAddAmount, float coolingAddAmount, float staminaAddAmount)
    {
        OnFoodAddToHorse?.Invoke(powerAddAmount, coolingAddAmount, staminaAddAmount);
    }
}
