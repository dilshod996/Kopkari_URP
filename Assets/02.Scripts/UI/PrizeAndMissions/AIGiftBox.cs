using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIGiftBox : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text giftName;
    [SerializeField] private TMP_Text costText;
    [Header("Settings")]
    [SerializeField] private Button getBtn;
    [SerializeField] private int giftId = 0;
    [SerializeField] private int giftCost = 0;
    [SerializeField] private int giftCostLangId = -1; // Language ID for the cost text, if needed
    [SerializeField] private int giftType = -1; // Amount of the cost, if needed
    [SerializeField] private int giftNameLangId = -1;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        Transiliations();
    }
    private void Transiliations()
    {
        giftName.text = LanguageManager.Instance.GetText(giftNameLangId);
        if (!giftType.Equals(-1))
        {
            costText.text = giftCost.ToString() + " " + LanguageManager.Instance.GetText(giftCostLangId) + " " + LanguageManager.Instance.GetText(giftType);
        }
        else
        {
            costText.text = giftCost.ToString() + " " + LanguageManager.Instance.GetText(giftCostLangId);
        }
        
    }
}
