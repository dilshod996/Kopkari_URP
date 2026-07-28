using System;
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
        saveButton.onClick.AddListener(GetSelectedItem);
        closeButton.onClick.AddListener(ClosePage);
        languageDropdown.dropdownEvent.AddListener(OnDropdownSelected);
    }
    private void OnDisable()
    {
        saveButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        languageDropdown.dropdownEvent.RemoveListener(OnDropdownSelected);
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
}
//[Header("UI Refs")]
//[SerializeField] private Slider volumeSlider; // 0..100
//[SerializeField] private Toggle soundToggle;  // on/off

//private bool _ignoreEvents;

//private void OnEnable()
//{
//    SyncFromPrefs();
//    HookUI();
//}

//private void OnDisable()
//{
//    UnhookUI();
//}

//private void SyncFromPrefs()
//{
//    _ignoreEvents = true;

//    int state = PlayerPrefs.GetInt(SoundManager.PREF_SOUND_STATE, 1);
//    int vol100 = PlayerPrefs.GetInt(SoundManager.PREF_SOUND_VOL_100, 100);

//    if (soundToggle != null) soundToggle.isOn = (state == 1);

//    if (volumeSlider != null)
//    {
//        volumeSlider.minValue = 0;
//        volumeSlider.maxValue = 100;
//        volumeSlider.wholeNumbers = true;
//        volumeSlider.value = vol100;
//        volumeSlider.interactable = (state == 1); // OFF bo'lsa slider disable
//    }

//    _ignoreEvents = false;
//}

//private void HookUI()
//{
//    if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
//    if (soundToggle != null) soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
//}

//private void UnhookUI()
//{
//    if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
//    if (soundToggle != null) soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
//}

//private void OnVolumeChanged(float v)
//{
//    if (_ignoreEvents) return;
//    if (SoundManager.Instance == null) return;

//    SoundManager.Instance.SetVolume100(Mathf.RoundToInt(v));
//}

//private void OnSoundToggleChanged(bool on)
//{
//    if (_ignoreEvents) return;
//    if (SoundManager.Instance == null) return;

//    SoundManager.Instance.SetSoundState(on);

//    if (volumeSlider != null)
//        volumeSlider.interactable = on;
//}
