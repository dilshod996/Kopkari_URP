using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConditionCheck : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Button openFoodPanel;
    [SerializeField] private TMP_Text BtnText;



    [SerializeField] private ProgressBar powerSlider, coolingSlider, staminaSlider;
    [SerializeField] private TMP_Text powerText, coolingText, staminaText;
    [SerializeField] private GameObject powerAlarm, coolingAlarm, staminaAlarm;
    [SerializeField] private TMP_Text powerAlarmMsg, coolingAlarmMsg, staminaAlarmMsg;

    private void OnEnable()
    {
        UITransilitaions();
        GetResources();
        openFoodPanel.onClick.AddListener(OpenFoodpanel);
    }
    private void OnDisable()
    {
        openFoodPanel.onClick.RemoveAllListeners();
    }
    private void UITransilitaions()
    {
        if (LanguageManager.Instance != null)
        {
            title.text = LanguageManager.Instance.GetText(199);
            description.text = LanguageManager.Instance.GetText(200);
            powerText.text = LanguageManager.Instance.GetText(326);
            coolingText.text = LanguageManager.Instance.GetText(327);
            staminaText.text = LanguageManager.Instance.GetText(328);
        }
    }
    private void GetResources()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());

        UpdateSliders(current.Power, current.Cooling, current.Stamina);
    }
    private void UpdateSliders(float powerValue, float coolingValue, float staminValue)
    {
        powerSlider.currentPercent = powerValue;
        coolingSlider.currentPercent = coolingValue;
        staminaSlider.currentPercent = staminValue;
        AlarmObjectAnabler(powerValue, desiredValue: 20, powerAlarm, powerAlarmMsg, 201);
        AlarmObjectAnabler(coolingValue, desiredValue: 10, coolingAlarm, coolingAlarmMsg, 202);
        AlarmObjectAnabler(staminValue, desiredValue:30, staminaAlarm, staminaAlarmMsg, 203);
        powerSlider.UpdateUI();
        coolingSlider.UpdateUI();
        staminaSlider.UpdateUI();
    }
    private void AlarmObjectAnabler(float value, float desiredValue, GameObject alarmObject, TMP_Text alarmText, int msgCode)
    {
        if (value < desiredValue && alarmObject!=null)
        {
            alarmObject.SetActive(true);
            alarmText.text =$"{LanguageManager.Instance.GetText(msgCode)}: {desiredValue}" ;
        }
        else
        {
            alarmObject.SetActive(false);
        }
    }
    private void OpenFoodpanel()
    {
        if (HomeMainUI.Instance != null)
        {
            HomeMainUI.Instance.SHowFoodPanel();
        }
        else
        {
            if (UIButtonActions.Instance != null)
            {
                UIButtonActions.Instance.OpenFoodPanel();
            }
        }
        this.gameObject.SetActive(false);
    }

}
