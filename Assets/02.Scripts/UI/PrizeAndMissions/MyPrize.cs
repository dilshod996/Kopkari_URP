using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MyPrize : MonoBehaviour
{
    public PrizeType prizeType; // Enum to define the type of prize
    [Header("UI Texts")]
    [SerializeField] private TMP_Text prizeNameText;
    [SerializeField] private TMP_Text prizeAmountText;
    [Header("Settings")]
    [SerializeField] private string prizeName = "Default Prize";
    [SerializeField] private int prizeAmount = 0;
    [SerializeField] private int prizeNameLangId = -1; // Language ID for the prize name, if needed
    [SerializeField] private int prizeAmountLangId = -1; // Language ID for the prize amount, if needed
    //[SerializeField] private int prizeType = -1; // Type of the prize, if needed
    void Start()
    {
        
    }

    private void OnEnable()
    {
        UpdatePrizeUI();
    }

    private void UpdatePrizeUI()
    {
        prizeNameText.text = LanguageManager.Instance.GetText(prizeNameLangId);
        string key = prizeType.ToString().ToLower();
        float amount = PlayerPrefs.GetFloat(key); // fallback qiymat
        string localizedName = LanguageManager.Instance.GetText(prizeAmountLangId);

        prizeAmountText.text = amount.ToString("0") + " " + localizedName;

    }
}
