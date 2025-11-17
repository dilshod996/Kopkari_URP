using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{

    [SerializeField] private LobbyManager lobbyManager;

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
    [SerializeField] private TMP_Text saveChangesTitle;

    [Header("Settings Details")]
    [SerializeField] private CustomDropdown languageDropdown;
    [SerializeField] private SliderManager soundSlider;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button deleteButton;

    [SerializeField] private TMP_Text saveBtnText;
    [SerializeField] private TMP_Text deleteBtnText;
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

    void Start()
    {
        saveButton.onClick.AddListener(GetSelectedItem);
    }

    private void OnEnable()
    {
        SettingsPanelText();
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
        lobbyManager.MainLobbyText();
        gameObject.SetActive(false);
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
}
