using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIPrizes : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text normalMissionBtnText;
    [SerializeField] private TMP_Text pressedMissionBtnText;
    [SerializeField] private TMP_Text normalPrizesBtnText;
    [SerializeField] private TMP_Text pressedPrizesBtnText;


    [Header("Settings")]
    [SerializeField] private ModalWindowManager modalWindowManager;
    [SerializeField] private NotificationManager notificationManager;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        title.text =  LanguageManager.Instance.GetText(72);
        normalMissionBtnText.text = LanguageManager.Instance.GetText(73);
        pressedMissionBtnText.text = LanguageManager.Instance.GetText(73);
        normalPrizesBtnText.text = LanguageManager.Instance.GetText(74);
        pressedPrizesBtnText.text = LanguageManager.Instance.GetText(74);
    }
}
