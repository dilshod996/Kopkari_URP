using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodInfo : MonoBehaviour
{
    [SerializeField] private PrizeType prizeType; // Default food name
    [Header("UI Texts")]
    [SerializeField] private TMP_Text nameFood;
    [SerializeField] private TMP_Text amountFood;

    [SerializeField] private int nameFoodId=-1;
    [SerializeField] private int amountFoodId=-1;
    [Header("UI Settings")]
    [SerializeField] private Button addBtn;
    [SerializeField] private float amount = 0;
    [SerializeField] private float initialAmount = 0;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        GetFoodAmount();
        TextTransilations();
    }
    public void GetFoodAmount()
    {
        string foodName = prizeType.ToString().ToLower(); // Enum nomini kichik harflarda olish
        if (PlayerPrefs.HasKey(foodName))
        {
            amount = PlayerPrefs.GetFloat(foodName);
        }
        else
        {
            amount = initialAmount;
            PlayerPrefs.SetFloat(foodName, amount);
            PlayerPrefs.Save(); // ixtiyoriy, lekin foydali
        }
    }

    private void TextTransilations()
    {
        if (nameFoodId != -1)
            nameFood.text = LanguageManager.Instance.GetText(nameFoodId);
        if (amountFoodId != -1)
            amountFood.text = amount.ToString() + " " + LanguageManager.Instance.GetText(amountFoodId);
    }
}
