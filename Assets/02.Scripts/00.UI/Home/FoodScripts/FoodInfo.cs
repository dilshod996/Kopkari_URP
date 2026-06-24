using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    [Header("Useful Food Animation")]
    [SerializeField] private float usefulScale = 1.04f;
    [SerializeField] private float usefulScaleDuration = 0.55f;

    public static event Action<float, float, float> OnFoodAddToHorse;
    public static event Action OnMoneyNotEnough;
    private static event Action OnFoodButtonsRefreshRequested;
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
    private Tween usefulTween;
    private Vector3 originalScale;

    private void OnEnable()
    {
        originalScale = transform.localScale;
        TextTransilations();
        RefreshButtonState();
        OnFoodButtonsRefreshRequested += RefreshButtonState;
        buyBtn.onClick.AddListener(BuyFood);
    }
    private void OnDisable()
    {
        OnFoodButtonsRefreshRequested -= RefreshButtonState;
        StopUsefulAnimation();
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
        if (!CanImproveHorse())
        {
            RefreshButtonState();
            return;
        }

        StopUsefulAnimation();
        bool success = CurrencyManager.Instance.SpendNyufiy(foodCost, true);

        if (!success)
        {
            OnMoneyNotEnough?.Invoke();
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            SoundManager.Instance.PlayUI(UISoundType.Error);
            return;
        }

        BuyFeedHorse(food);
        OnFoodButtonsRefreshRequested?.Invoke();
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        SoundManager.Instance.PlayUI(UISoundType.Success);
    }
    private void BuyFeedHorse(HorseFood foodType)
    {
        int langId = -1;
        GetFoodBuffs(foodType, out float power, out float cooling, out float stamina);

        switch (foodType)
        {
            case HorseFood.Water:
                langId = 204;
                break;
            case HorseFood.Apple:
                langId = 205;
                break;
            case HorseFood.Wheat:
                langId = 206;
                break;
            case HorseFood.Barley:
                langId = 207;
                break;
            case HorseFood.StaminWater:
                langId = 208;
                break;
        }
        AddSupplies(power, cooling, stamina);
        HomeMainUI.Instance?.ShowRightPopup(LanguageManager.Instance.GetText(langId), imageofFood.sprite);
    }
    private void AddSupplies(float powerAddAmount, float coolingAddAmount, float staminaAddAmount)
    {
        OnFoodAddToHorse?.Invoke(powerAddAmount, coolingAddAmount, staminaAddAmount);
    }

    private void RefreshButtonState()
    {
        bool canImprove = CanImproveHorse();

        if (buyBtn != null)
            buyBtn.interactable = canImprove;

        if (canImprove)
            StartUsefulAnimation();
        else
            StopUsefulAnimation();
    }

    private bool CanImproveHorse()
    {
        GetFoodBuffs(food, out float power, out float cooling, out float stamina);

        HorseConditionStats max = HorseConditionStatsService.GetCachedMaxOrDefault();
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(max);

        return CanIncrease(power, current.Power, max.Power) ||
               CanIncrease(cooling, current.Cooling, max.Cooling) ||
               CanIncrease(stamina, current.Stamina, max.Stamina);
    }

    private static bool CanIncrease(float amount, float current, float max)
    {
        return amount > 0f && current < max;
    }

    private static void GetFoodBuffs(HorseFood foodType, out float power, out float cooling, out float stamina)
    {
        power = 0f;
        cooling = 0f;
        stamina = 0f;

        switch (foodType)
        {
            case HorseFood.Water:
                cooling = 7f;
                break;
            case HorseFood.Apple:
                power = 4f;
                stamina = 4f;
                break;
            case HorseFood.Wheat:
                power = 6f;
                stamina = 8f;
                break;
            case HorseFood.Barley:
                power = 7f;
                stamina = 10f;
                break;
            case HorseFood.StaminWater:
                cooling = 6f;
                stamina = 13f;
                break;
        }
    }

    private void StartUsefulAnimation()
    {
        if (usefulTween != null && usefulTween.IsActive())
            return;

        transform.localScale = originalScale;
        usefulTween = transform
            .DOScale(originalScale * usefulScale, usefulScaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopUsefulAnimation()
    {
        if (usefulTween != null)
        {
            usefulTween.Kill();
            usefulTween = null;
        }

        transform.localScale = originalScale;
    }
}
