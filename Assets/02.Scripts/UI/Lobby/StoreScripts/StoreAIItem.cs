using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreAIItem : MonoBehaviour
{
    [Header("Item ID")]
    [SerializeField] private int itemID;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text excost;
    [SerializeField] private TMP_Text costText;

    [SerializeField] private int titleID;
    [SerializeField] private int excostValue;
    [SerializeField] private int costValue;

    [Header("UI Settings")]
    [SerializeField] private Button buyButton;

    void Start()
    {
        
    }
    private void OnEnable()
    {
        UITranslitions();
    }
    private void UITranslitions()
    {
        titleText.text = LanguageManager.Instance.GetText(titleID);
        excost.text = excostValue.ToString() + " " + LanguageManager.Instance.GetText(58);
        costText.text = costValue.ToString() + " " + LanguageManager.Instance.GetText(58);
    }
}
