using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static FoodInfo;

public class FoodShowerPopup : MonoBehaviour
{
    [SerializeField] private Image foodIcon;
    [SerializeField] private TMP_Text foodName;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text coolingText;
    [SerializeField] private TMP_Text staminaText;

    [SerializeField] private Button giveBtn;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [Header("Percentages")]
    [SerializeField] private TMP_Text powerPercentage;
    [SerializeField] private TMP_Text coolingPercentage;
    [SerializeField] private TMP_Text staminPercentage;

    [Header("Button Texts")]
    [SerializeField] private TMP_Text nyufiyAmountText;
    [SerializeField] private TMP_Text feedText;
    public static Action<HorseFood, int> OnFoodAmountChanged;
    public static Action OnBuyBtnPressed;
    [SerializeField] private int feedId;
    [Header("Not enought msg")]
    [SerializeField] private GameObject bottomAds;
    [SerializeField] private TMP_Text messageToPlayer;
    private HorseFood currentFoodType;

    private int costOfBooster;

    private float buffPower;
    private float buffCooling;
    private float buffStamina;
    public static Action<float, float, float> OnFoodGivenWithStats;
    public static Action<bool> OnFoodPopupVisibilityChanged;
    public static Action OnWaterDrink;
    public static Action OnFoodEat;

    private void OnEnable()
    {
        UITransilations();
        giveBtn?.onClick.AddListener(GiveButton);
        closeButton?.onClick.AddListener(Close);
        buyButton?.onClick.AddListener(BuyAction);
        bottomAds.SetActive(false);
    }

    private void OnDisable()
    {
        giveBtn?.onClick.RemoveListener(GiveButton);
        closeButton?.onClick.RemoveListener(Close);
        buyButton?.onClick.RemoveListener(BuyAction);   
    }

    private void UITransilations()
    {
        if (LanguageManager.Instance == null)
        {
            Debug.Log("LanguageManager Instance is NULL in FoodShowerPopup");
            return;
        }
        feedText.text = LanguageManager.Instance.GetText(feedId);
        powerText.text = LanguageManager.Instance.GetText(326);
        coolingText.text = LanguageManager.Instance.GetText(327);
        staminaText.text = LanguageManager.Instance.GetText(328);
    }
    public void SHowFoodDetails(int foodId, HorseFood foodType, Sprite icon)
    {
        foodIcon.sprite = icon;
        currentFoodType = foodType;
        foodName.text = LanguageManager.Instance?.GetText(foodId);

        switch (foodType)
        {
            case HorseFood.Water:
                PercentageDetails(0, 6, 3, 500);
                break;
            case HorseFood.Apple:
                PercentageDetails(4, 2, 4, 750);
                break;
            case HorseFood.Wheat:
                PercentageDetails(7, 2, 5, 900);
                break;
            case HorseFood.Barley:
                PercentageDetails(9, 3, 6, 1400);
                break;
            case HorseFood.StaminWater:
                PercentageDetails(0, 5, 15, 1780);
                break;
        }

        FeedBtnState(GetFoodKey(foodType));
        OnFoodPopupVisibilityChanged?.Invoke(false);
    }

    public void BuyAction()
    {
        int nyufiy = GetNyufiyPrefs();

        if (nyufiy < costOfBooster)
        {
            bottomAds.SetActive(true);
            messageToPlayer.text = "Not enough Nyufiy! Watch Adds get more Nyufiy";
            return;
        }

        // 1) Nyufiy coin ayiriladi
        nyufiy -= costOfBooster;
        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiy);

        // 2) Olingan ovqatga +1 qo‘shamiz
        AddPurchasedFood(currentFoodType);
        int newAmount = PlayerPrefs.GetInt(GetFoodKey(currentFoodType), 0);
        OnFoodAmountChanged?.Invoke(currentFoodType, newAmount);
        // 3) GIVE tugmani yangilab qo‘yish (MUHIM)
        FeedBtnState(GetFoodKey(currentFoodType));


        // 5) Event (agar boshqa scriptga kerak bo‘lsa)
        OnBuyBtnPressed?.Invoke();
    }


    private void GiveButton()
    {
        // 1) Resursni kamaytirish
        RemoveOneFood(currentFoodType);

        // 2) Give event chaqirish (ovqat berildi)
        int newAmount = PlayerPrefs.GetInt(GetFoodKey(currentFoodType), 0);
        OnFoodAmountChanged?.Invoke(currentFoodType, newAmount);
        // 🔥 Horse statlariga buff berish event
        OnFoodGivenWithStats?.Invoke(buffPower, buffCooling, buffStamina);
        // 3) Tugma holatini qayta tekshirish
        FeedBtnState(GetFoodKey(currentFoodType));
        gameObject.SetActive(false);
        if (currentFoodType == HorseFood.Water || currentFoodType == HorseFood.StaminWater)
        {
            OnWaterDrink?.Invoke();
        }
        else
        {
            OnFoodEat?.Invoke();
        }

        // 5) Popupni yopamiz, LEKIN event chaqirmaymiz
        //    FoodInfo hali ham hidden bo‘lib qoladi → sahnada faqat ot harakati ko‘rinadi

    }

    private void Close()
    {
        this.gameObject.SetActive(false);
        OnFoodPopupVisibilityChanged?.Invoke(true);
    }
    private void PercentageDetails(float power, float cooling, float stamin, int cost)
    {
        powerPercentage.text = $"+{power}%";
        coolingPercentage.text = $"+{cooling}%";
        staminPercentage.text = $"+{stamin}%";

        nyufiyAmountText.text = cost.ToString();
        costOfBooster = cost;
        // 🔥 BUFFLARNI SAQLAB QO‘YAMIZ
        buffPower = power;
        buffCooling = cooling;
        buffStamina = stamin;
    }

    private int GetNyufiyPrefs()
    {
        int nyufiyAmount = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        return nyufiyAmount;
    }
    private void FeedBtnState(string key)
    {
        if (PlayerPrefs.GetInt(key, 0) < 1)
            giveBtn.interactable = false;
        else
            giveBtn.interactable = true;
    }
    private void AddPurchasedFood(HorseFood food)
    {
        string key = GetFoodKey(food);

        int amount = PlayerPrefs.GetInt(key, 0);
        amount++;

        PlayerPrefs.SetInt(key, amount);
        string popupMessage = $"+1 {LanguageManager.Instance.GetText(GetFoodName(food))}";
        HomeMainUI.Instance.ShowRightPopup(popupMessage, foodIcon.sprite);
    }

    private string GetFoodKey(HorseFood food)
    {
        return food switch
        {
            HorseFood.Water => Constants.HorseFoods.Water,
            HorseFood.Apple => Constants.HorseFoods.Apple,
            HorseFood.Wheat => Constants.HorseFoods.Wheat,
            HorseFood.Barley => Constants.HorseFoods.Barley,
            HorseFood.StaminWater => Constants.HorseFoods.StaminWater,
            _ => ""
        };
    }
    private void RemoveOneFood(HorseFood food)
    {
        string key = GetFoodKey(food);

        int current = PlayerPrefs.GetInt(key, 0);

        if (current <= 0)
        {
            Debug.Log("NO FOOD LEFT => Cannot remove!");
            return;
        }

        current--;

        PlayerPrefs.SetInt(key, current);
    }

    private int GetFoodName(HorseFood food)
    {
        switch (food)
        {
            case HorseFood.Water:
                return 111;

            case HorseFood.Apple:
                return 110;

            case HorseFood.Wheat:
                return 108;

            case HorseFood.Barley:
                return 109;

            case HorseFood.StaminWater:
                return 112;

            default:
                return -1; // safety
        }
    }



}
