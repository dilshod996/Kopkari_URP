using System;
using Lofelt.NiceVibrations;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public event Action LanguageDropdownOpened;
    public event Action<int> LanguageSelected;

    [Header("Settings Texts")]
    [SerializeField] private TMP_Text titleSettings;
    [SerializeField] private TMP_Text languageTextTitle;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text notifTitleText;
    [SerializeField] private TMP_Text notifOpen;
    [SerializeField] private TMP_Text notifClose;
    [SerializeField] private TMP_Text soundText;
    [SerializeField] private TMP_Text vibrationText;
    [SerializeField] private TMP_Text vibrationOpen;
    [SerializeField] private TMP_Text vibrationClose;
    [SerializeField] private TMP_Text deleteAccountTitle;

    [Header("Settings Details")]
    [SerializeField] private CustomDropdown languageDropdown;
    [SerializeField] private SliderManager soundSlider;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private HapticReceiver hapticReceiver;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private TMP_Text saveBtnText;
    [SerializeField] private TMP_Text deleteBtnText;
    [SerializeField] private TMP_Text closeText;
    public enum Language
    {
        Uzbek,
        Kazak,
        Kyrgyz,
        English,
        Afghan,
        Turkman,
        Tadjik,
        Uygur,
        Russian
    }



    private bool dropdownWasOpen;
    private bool previousSaveInteractable;
    private bool previousCloseInteractable;
    private bool ignoreSettingsEvents;
    private bool isDeletingAccount;

    private void Awake()
    {
        if (hapticReceiver == null)
            hapticReceiver = FindObjectOfType<HapticReceiver>();
    }

    private void OnEnable()
    {
        dropdownWasOpen = languageDropdown != null && languageDropdown.isOn;
        previousSaveInteractable = saveButton != null && saveButton.interactable;
        previousCloseInteractable = closeButton != null && closeButton.interactable;
        if (HomeTutorialController.IsTutorialActive)
        {
            if (saveButton != null)
                saveButton.interactable = false;
            if (closeButton != null)
                closeButton.interactable = false;
        }

        SettingsPanelText();
        RefreshAudioAndVibrationSettings();

        if (saveButton != null)
            saveButton.onClick.AddListener(GetSelectedItem);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteAccount);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePage);
        if (languageDropdown != null)
            languageDropdown.dropdownEvent.AddListener(OnDropdownSelected);
        if (soundSlider != null && soundSlider.mainSlider != null)
            soundSlider.mainSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }

    private void OnDisable()
    {
        if (saveButton != null)
            saveButton.onClick.RemoveListener(GetSelectedItem);
        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(DeleteAccount);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePage);
        if (languageDropdown != null)
            languageDropdown.dropdownEvent.RemoveListener(OnDropdownSelected);
        if (soundSlider != null && soundSlider.mainSlider != null)
            soundSlider.mainSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.RemoveListener(OnVibrationChanged);

        dropdownWasOpen = false;
        if (saveButton != null)
            saveButton.interactable = previousSaveInteractable;
        if (closeButton != null)
            closeButton.interactable = previousCloseInteractable;
    }

    private void Update()
    {
        if (languageDropdown == null)
            return;

        bool dropdownIsOpen = languageDropdown.isOn;
        if (dropdownIsOpen && !dropdownWasOpen)
            LanguageDropdownOpened?.Invoke();

        dropdownWasOpen = dropdownIsOpen;
    }
    private void SettingsPanelText()
    {
        titleSettings.text = LanguageManager.Instance.GetText(26);
        languageTextTitle.text = LanguageManager.Instance.GetText(30);
        infoText.text = LanguageManager.Instance.GetText(29);
        notifTitleText.text = LanguageManager.Instance.GetText(31);
        vibrationText.text = LanguageManager.Instance.GetText(32);
        soundText.text = LanguageManager.Instance.GetText(33);
        deleteAccountTitle.text = LanguageManager.Instance.GetText(34);
       // saveChangesTitle.text = LanguageManager.Instance.GetText(35);

        notifOpen.text = LanguageManager.Instance.GetText(36);
        notifClose.text = LanguageManager.Instance.GetText(37);
        vibrationOpen.text = LanguageManager.Instance.GetText(36);
        vibrationClose.text = LanguageManager.Instance.GetText(37);

        deleteBtnText.text = LanguageManager.Instance.GetText(38);
        saveBtnText.text = LanguageManager.Instance.GetText(39);
        closeText.text = LanguageManager.Instance.GetText(362);
        GetLanguageCode();
    }
    private void GetLanguageCode()
    {
        switch (PlayerPrefs.GetString("language"))
        {
            case "uzbek":
                languageDropdown.selectedItemIndex = 0;
                break;
            case "russian":
                languageDropdown.selectedItemIndex = 3;
                break;
            case "english":
                languageDropdown.selectedItemIndex = 4;
                break;
            case "kazak":
                languageDropdown.selectedItemIndex = 1;
                break;
            default:
                languageDropdown.selectedItemIndex = 4;
                break;
        }
    }
    private void GetSelectedItem()
    {
        Debug.Log("Selected item: " + languageDropdown.selectedItemIndex);
        SetLanguage(languageDropdown.selectedItemIndex);
        HomeMainUI.Instance?.UITransilations();
        //lobbyManager.MainLobbyText();
        HomeMainUI.Instance.CloseSettingsPanel();
        //LanguageManager.Instance.SetLanguage(languageDropdown.selectedItemIndex);
    }
    public void SetLanguage(int langNum)
    {
        string lang=string.Empty;
        switch (langNum)
        {
            case 0:
                lang = "uzbek";
                break;
            case 3:
                lang = "russian";
                break;
            case 4:
                lang = "english";
                break;
            case 1:
                lang = "kazak";
                break;
            default:
                lang = "english";
                break;
        }
        LanguageManager.Instance.SetLanguage(lang);
    }

    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OnDropdownSelected(int selectedIndex)
    {
        Debug.Log("selected event id" + selectedIndex);
        if (HomeTutorialController.IsTutorialActive && saveButton != null)
            saveButton.interactable = true;
        LanguageSelected?.Invoke(selectedIndex);
    }

    private void RefreshAudioAndVibrationSettings()
    {
        ignoreSettingsEvents = true;

        if (soundSlider != null && soundSlider.mainSlider != null)
        {
            Slider slider = soundSlider.mainSlider;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;

            bool soundOn = SoundManager.Instance != null
                ? SoundManager.Instance.SoundOn
                : PlayerPrefs.GetInt(SoundManager.PREF_SOUND_STATE, 1) == 1;
            int savedVolume = PlayerPrefs.GetInt(SoundManager.PREF_SOUND_VOL_100, 100);
            slider.SetValueWithoutNotify(soundOn ? savedVolume : 0f);
            soundSlider.UpdateUI();
        }

        bool vibrationOn =
            PlayerPrefs.GetInt(RacingSettingsPanel.VibrationPrefsKey, 1) == 1;
        if (vibrationToggle != null)
        {
            vibrationToggle.SetIsOnWithoutNotify(vibrationOn);
            CustomToggle customToggle = vibrationToggle.GetComponent<CustomToggle>();
            if (customToggle != null &&
                customToggle.toggleObject != null &&
                customToggle.toggleAnimator != null)
            {
                customToggle.UpdateState();
            }
        }
        ApplyVibrationState(vibrationOn, save: false);

        ignoreSettingsEvents = false;
    }

    private void OnSoundVolumeChanged(float value)
    {
        if (ignoreSettingsEvents)
            return;

        int volume100 = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        bool soundOn = volume100 > 0;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume100(volume100);
            SoundManager.Instance.SetSoundState(soundOn);
        }
        else
        {
            PlayerPrefs.SetInt(SoundManager.PREF_SOUND_VOL_100, volume100);
            PlayerPrefs.SetInt(SoundManager.PREF_SOUND_STATE, soundOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void OnVibrationChanged(bool isOn)
    {
        if (ignoreSettingsEvents)
            return;

        ApplyVibrationState(isOn, save: true);
    }

    private void ApplyVibrationState(bool isOn, bool save)
    {
        if (hapticReceiver == null)
            hapticReceiver = FindObjectOfType<HapticReceiver>();
        if (hapticReceiver != null)
            hapticReceiver.hapticsEnabled = isOn;

        if (!save)
            return;

        PlayerPrefs.SetInt(
            RacingSettingsPanel.VibrationPrefsKey,
            isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private async void DeleteAccount()
    {
        if (isDeletingAccount)
            return;

        isDeletingAccount = true;
        if (deleteButton != null)
            deleteButton.interactable = false;

        try
        {
            if (FirebaseManager.Instance == null)
                throw new InvalidOperationException("FirebaseManager is not available.");

            await FirebaseManager.Instance.DeleteCurrentUserAsync();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Application.Quit();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Account deletion failed: {exception}", this);
            isDeletingAccount = false;
            if (deleteButton != null)
                deleteButton.interactable = true;
        }
    }
}
