using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodInfo : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text nameFood;
    [SerializeField] private TMP_Text amountFood;
    [SerializeField] private Image imageofFood;

    [SerializeField] private int nameFoodId=-1;
    [SerializeField] private int amountFoodId=-1;
    [Header("UI Settings")]
    [SerializeField] private Button selectBtn;

    [SerializeField] private FoodShowerPopup foodPopup;
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
        selectBtn.onClick.AddListener(ShowFoodDetails);
        FoodShowerPopup.OnFoodAmountChanged += HandleFoodAmountChanged;
    }
    private void OnDisable()
    {
        selectBtn.onClick.RemoveAllListeners();
        FoodShowerPopup.OnFoodAmountChanged -= HandleFoodAmountChanged;
    }

    private void TextTransilations()
    {
        if (nameFoodId != -1)
            nameFood.text = LanguageManager.Instance.GetText(nameFoodId);
        if (amountFoodId != -1)
            amountFood.text = $"{FoodReserve()} {LanguageManager.Instance.GetText(amountFoodId)}";
    }
    public void ShowFoodDetails()
    {
        foodPopup.gameObject.SetActive(true);
        foodPopup.SHowFoodDetails(nameFoodId, food, imageofFood.sprite);
        SoundManager.Instance.PlayUI(UISoundType.PopupOpen);
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
    private int FoodReserve()
    {
        string key = GetFoodKey(food);

        int current = PlayerPrefs.GetInt(key, 0);
        return current;
    }
    private void HandleFoodAmountChanged(HorseFood changedType, int newAmount)
    {
        if (changedType != food)
            return; // Bu signal boshqa ovqat uchun – e’tibor bermaymiz

        amountFood.text = $"{newAmount.ToString()} {LanguageManager.Instance.GetText(amountFoodId)}";
    }
}
