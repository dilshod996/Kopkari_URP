using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InitialGiftShower : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text giftText;
    [Header("UI Buttons")]
    [SerializeField] private Button getButton;

    private void OnEnable()
    {
        UITransilations();
    }
    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(96);
        descriptionText.text = LanguageManager.Instance.GetText(97);
        buttonText.text = LanguageManager.Instance.GetText(98);
        giftText.text =  "1x " +  LanguageManager.Instance.GetText(85);
    }
}
