using System;
using Lofelt.NiceVibrations;
using MalbersExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RacingSettingsPanel : MonoBehaviour
{
    public const string VibrationPrefsKey = "VibrationState";
    private const int TitleTextId = 26;
    private const int SoundTextId = 562;
    private const int ControllerTextId = 564;
    private const int ButtonsTextId = 566;
    private const int ReinsTextId = 565;
    private const int TiltTextId = 567;
    private const int VibrationsTextId = 32;
    private const int SaveTextId = 39;
    private const int CloseTextId = 137;

    [Header("Sound")]
    [SerializeField] private Slider soundSlider;

    [Header("Vibration")]
    [SerializeField] private Slider vibrationSlider;
    [SerializeField] private HapticReceiver hapticReceiver;

    [Header("Controller Buttons")]
    [SerializeField] private Button reinsButton;
    [SerializeField] private Button buttonsButton;
    [SerializeField] private Button tiltButton;

    [Header("Panel Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text soundText;
    [SerializeField] private TMP_Text controllerText;
    [SerializeField] private TMP_Text buttonsText;
    [SerializeField] private TMP_Text reinsText;
    [SerializeField] private TMP_Text tiltText;
    [SerializeField] private TMP_Text vibrationsText;
    [SerializeField] private TMP_Text saveText;
    [SerializeField] private TMP_Text closeText;

    [Header("Selected Controller Objects")]
    [SerializeField] private GameObject reinsSelectedObject;
    [SerializeField] private GameObject buttonsSelectedObject;
    [SerializeField] private GameObject tiltSelectedObject;

    [Header("Runtime Controller")]
    [SerializeField] private JoystickTurnMixer joystickTurnMixer;
    [SerializeField] private bool findJoystickTurnMixerIfMissing = true;

    private bool _ignoreUiEvents;
    private bool _pendingSoundOn;
    private bool _pendingVibrationOn;
    private RacingControllerType _pendingControllerType;

    private void Awake()
    {
        if (hapticReceiver == null)
            hapticReceiver = FindObjectOfType<HapticReceiver>();

        if (joystickTurnMixer == null && findJoystickTurnMixerIfMissing)
            joystickTurnMixer = FindObjectOfType<JoystickTurnMixer>();
    }

    private void OnEnable()
    {
        AddListeners();
        ApplyLocalizedTexts();
        RefreshFromSavedSettings();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void RefreshFromSavedSettings()
    {
        _ignoreUiEvents = true;

        bool soundOn = SoundManager.Instance != null
            ? SoundManager.Instance.SoundOn
            : PlayerPrefs.GetInt(SoundManager.PREF_SOUND_STATE, 1) == 1;
        bool vibrationOn = GetSavedVibrationState();
        RacingControllerType controllerType = RacingControllerSelecterUI.GetSavedControllerOrDefault();
        _pendingSoundOn = soundOn;
        _pendingVibrationOn = vibrationOn;
        _pendingControllerType = controllerType;

        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(soundOn ? 1f : 0f);

        if (vibrationSlider != null)
            vibrationSlider.SetValueWithoutNotify(vibrationOn ? 1f : 0f);

        SetControllerUi(controllerType);

        _ignoreUiEvents = false;
    }

    public void OnSoundSliderChanged(float value)
    {
        if (_ignoreUiEvents)
            return;

        _pendingSoundOn = SliderValueToBool(value);
    }

    public void OnVibrationSliderChanged(float value)
    {
        if (_ignoreUiEvents)
            return;

        _pendingVibrationOn = SliderValueToBool(value);
    }

    public void SelectReinsController()
    {
        SelectController(RacingControllerType.Reins);
    }

    public void SelectButtonsController()
    {
        SelectController(RacingControllerType.Buttons);
    }

    public void SelectTiltController()
    {
        SelectController(RacingControllerType.Tilt);
    }

    public void SelectController(int controllerType)
    {
        if (!Enum.IsDefined(typeof(RacingControllerType), controllerType))
            controllerType = (int)RacingControllerType.Buttons;

        SelectController((RacingControllerType)controllerType);
    }

    public void SelectController(RacingControllerType controllerType)
    {
        _pendingControllerType = controllerType;
        SetControllerUi(controllerType);
    }

    public void SaveAndClose()
    {
        SetSoundState(_pendingSoundOn);
        ApplyVibrationState(_pendingVibrationOn);
        PlayerPrefs.SetInt(RacingControllerSelecterUI.ControllerPrefsKey, (int)_pendingControllerType);
        PlayerPrefs.Save();

        SetControllerUi(_pendingControllerType);

        if (joystickTurnMixer == null && findJoystickTurnMixerIfMissing)
            joystickTurnMixer = FindObjectOfType<JoystickTurnMixer>();

        if (joystickTurnMixer != null)
            joystickTurnMixer.SetControllerType(_pendingControllerType);

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (UIButtonActions.Instance != null)
            UIButtonActions.Instance.HideUI(this);
        else
            gameObject.SetActive(false);
    }

    private void AddListeners()
    {
        SetupBoolSlider(soundSlider);
        SetupBoolSlider(vibrationSlider);

        if (soundSlider != null)
            soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);

        if (vibrationSlider != null)
            vibrationSlider.onValueChanged.AddListener(OnVibrationSliderChanged);

        if (reinsButton != null)
            reinsButton.onClick.AddListener(SelectReinsController);

        if (buttonsButton != null)
            buttonsButton.onClick.AddListener(SelectButtonsController);

        if (tiltButton != null)
            tiltButton.onClick.AddListener(SelectTiltController);

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveAndClose);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void RemoveListeners()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(OnSoundSliderChanged);

        if (vibrationSlider != null)
            vibrationSlider.onValueChanged.RemoveListener(OnVibrationSliderChanged);

        if (reinsButton != null)
            reinsButton.onClick.RemoveListener(SelectReinsController);

        if (buttonsButton != null)
            buttonsButton.onClick.RemoveListener(SelectButtonsController);

        if (tiltButton != null)
            tiltButton.onClick.RemoveListener(SelectTiltController);

        if (saveButton != null)
            saveButton.onClick.RemoveListener(SaveAndClose);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

    private void SetSoundState(bool isOn)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSoundState(isOn);
            return;
        }

        PlayerPrefs.SetInt(SoundManager.PREF_SOUND_STATE, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool GetSavedVibrationState()
    {
        if (PlayerPrefs.HasKey(VibrationPrefsKey))
            return PlayerPrefs.GetInt(VibrationPrefsKey, 1) == 1;

        return hapticReceiver == null || hapticReceiver.hapticsEnabled;
    }

    private void ApplyVibrationState(bool isOn)
    {
        if (hapticReceiver != null)
            hapticReceiver.hapticsEnabled = isOn;

        PlayerPrefs.SetInt(VibrationPrefsKey, isOn ? 1 : 0);
    }

    private void SetControllerUi(RacingControllerType controllerType)
    {
        bool useReins = controllerType == RacingControllerType.Reins;
        bool useButtons = controllerType == RacingControllerType.Buttons;
        bool useTilt = controllerType == RacingControllerType.Tilt;

        SetActiveIfAssigned(reinsSelectedObject, useReins);
        SetActiveIfAssigned(buttonsSelectedObject, useButtons);
        SetActiveIfAssigned(tiltSelectedObject, useTilt);
    }

    private static void SetupBoolSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = true;
    }

    private static bool SliderValueToBool(float value)
    {
        return value >= 0.5f;
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private void ApplyLocalizedTexts()
    {
        if (LanguageManager.Instance == null)
            return;

        ApplyLocalizedText(titleText, TitleTextId);
        ApplyLocalizedText(soundText, SoundTextId);
        ApplyLocalizedText(controllerText, ControllerTextId);
        ApplyLocalizedText(buttonsText, ButtonsTextId);
        ApplyLocalizedText(reinsText, ReinsTextId);
        ApplyLocalizedText(tiltText, TiltTextId);
        ApplyLocalizedText(vibrationsText, VibrationsTextId);
        ApplyLocalizedText(saveText, SaveTextId);
        ApplyLocalizedText(closeText, CloseTextId);
    }

    private static void ApplyLocalizedText(TMP_Text targetText, int languageTextId)
    {
        if (targetText == null || LanguageManager.Instance == null)
            return;

        targetText.text = LanguageManager.Instance.GetText(languageTextId);
    }
}
