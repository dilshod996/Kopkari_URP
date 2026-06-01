using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public enum RacingControllerType
{
    Reins = 0,
    Buttons = 1
}

public class RacingControllerSelecterUI : MonoBehaviour
{
    private const string ControllerPrefsKey = "Racing_Controller_Type";

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hardText;
    [SerializeField] private TMP_Text easyText;
    [SerializeField] private TMP_Text reinsText;
    [SerializeField] private TMP_Text buttonsText;


    [Header("Buttons")]
    [SerializeField] private Button reinsButton;
    [SerializeField] private Button buttonsButton;

    public RacingControllerType selectedController;

    [SerializeField] private StartPowerBar powerBar;

    public static event Action<RacingControllerType> OnControllerSelected;
    private void Awake()
    {
        if (reinsButton != null)
            reinsButton.onClick.AddListener(SelectReins);

        if (buttonsButton != null)
            buttonsButton.onClick.AddListener(SelectButtons);
    }
    private void OnEnable()
    {
        UITexts();
    }
    private void OnDestroy()
    {
        if (reinsButton != null)
            reinsButton.onClick.RemoveListener(SelectReins);

        if (buttonsButton != null)
            buttonsButton.onClick.RemoveListener(SelectButtons);
    }
    private void SelectController(RacingControllerType controllerType)
    {
        selectedController = controllerType;

        PlayerPrefs.SetInt(ControllerPrefsKey, (int)selectedController);
        PlayerPrefs.Save();
        OnControllerSelected?.Invoke(selectedController);
    }

    public void SelectReins()
    {
        SelectController(RacingControllerType.Reins);
        this.gameObject.SetActive(false);
        powerBar.gameObject.SetActive(true);
    }

    public void SelectButtons()
    {
        SelectController(RacingControllerType.Buttons);
        this.gameObject.SetActive(false);
        powerBar.gameObject.SetActive(true);
    }
    private void UITexts()
    {
        var lang = LanguageManager.Instance;
        if(lang == null)
            return;
        titleText.text = lang.GetText(517);
        hardText.text = lang.GetText(513);
        easyText.text = lang.GetText(514);
        reinsText.text = lang.GetText(515);
        buttonsText.text = lang.GetText(516);
    }

}
