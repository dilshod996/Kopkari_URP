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
    Buttons = 1,
    Tilt = 2
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
    [SerializeField] private TMP_Text tiltText;


    [Header("Buttons")]
    [SerializeField] private Button reinsButton;
    [SerializeField] private Button buttonsButton;
    [SerializeField] private Button tiltButton;

    [Header("Tilt Runtime Option")]
    [SerializeField] private bool createTiltOptionIfMissing = true;
    [SerializeField] private string tiltFallbackText = "Tilt";

    public RacingControllerType selectedController;

    [SerializeField] private StartPowerBar powerBar;

    public static event Action<RacingControllerType> OnControllerSelected;
    private void Awake()
    {
        EnsureTiltOption();

        if (reinsButton != null)
            reinsButton.onClick.AddListener(SelectReins);

        if (buttonsButton != null)
            buttonsButton.onClick.AddListener(SelectButtons);

        if (tiltButton != null)
            tiltButton.onClick.AddListener(SelectTilt);
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

        if (tiltButton != null)
            tiltButton.onClick.RemoveListener(SelectTilt);
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

    public void SelectTilt()
    {
        SelectController(RacingControllerType.Tilt);
        this.gameObject.SetActive(false);
        powerBar.gameObject.SetActive(true);
    }

    private void EnsureTiltOption()
    {
        if (!createTiltOptionIfMissing || tiltButton != null || buttonsButton == null)
            return;

        Button clonedButton = Instantiate(buttonsButton, buttonsButton.transform.parent);
        clonedButton.name = "TiltChooseBtn";
        clonedButton.onClick = new Button.ButtonClickedEvent();
        tiltButton = clonedButton;

        TMP_Text[] buttonTexts = clonedButton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < buttonTexts.Length; i++)
        {
            buttonTexts[i].text = tiltFallbackText;
        }

        if (buttonsText != null)
        {
            TMP_Text clonedText = Instantiate(buttonsText, buttonsText.transform.parent);
            clonedText.name = "TiltText";
            clonedText.text = tiltFallbackText;
            tiltText = clonedText;
        }

        ArrangeThreeControllerOptions();
    }

    private void ArrangeThreeControllerOptions()
    {
        SetCenteredOption(reinsButton, -310f);
        SetCenteredOption(tiltButton, 0f);
        SetCenteredOption(buttonsButton, 310f);

        SetCenteredText(reinsText, -310f);
        SetCenteredText(tiltText, 0f);
        SetCenteredText(buttonsText, 310f);

        HideVsLabel();
    }

    private void SetCenteredOption(Button button, float x)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, -20f);
    }

    private void SetCenteredText(TMP_Text text, float x)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 50f);
    }

    private void HideVsLabel()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].text == "VS")
            {
                texts[i].gameObject.SetActive(false);
                return;
            }
        }
    }

    private void UITexts()
    {
        var lang = LanguageManager.Instance;
        if(lang == null)
        {
            if (tiltText != null)
                tiltText.text = tiltFallbackText;

            return;
        }
        titleText.text = lang.GetText(517);
        hardText.text = lang.GetText(513);
        easyText.text = lang.GetText(514);
        reinsText.text = lang.GetText(515);
        buttonsText.text = lang.GetText(516);
        if (tiltText != null)
            tiltText.text = tiltFallbackText;
    }

}
