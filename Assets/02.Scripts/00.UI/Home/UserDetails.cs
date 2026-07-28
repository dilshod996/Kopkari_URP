using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserDetails : MonoBehaviour
{
    public event Action NameAndCountryReady;
    public event Action ProfileSaved;

    [Header("UI Texts")]    
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text levelLabelText;
    [SerializeField] private TMP_Text teamLabelText;
    [SerializeField] private TMP_Text saveBtnText;
    [SerializeField] private TMP_Text statusLabelText;
    [SerializeField] private TMP_Text rankingLabelText;


    [Header("UI Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private CustomDropdown countryDropdown;

    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text closeText;


    [Header("User Details Settings")]
    [SerializeField] private float scaleUp = 1.2f;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private GameObject nameContainerObj;

    [Header("Level details")]
    [SerializeField] private TMP_Text levelCountText;
    [SerializeField] private ProgressBar levelProgress;
    private bool previousSaveInteractable;
    private bool previousCloseInteractable;

    void Start()
    {
        if (PlayerPrefs.HasKey(Constants.Player.UsernameKey))
        {
            string currentUsername = PlayerPrefs.GetString(Constants.Player.UsernameKey);
            nameInputField.text = currentUsername;
        }

        // Save tugmasiga listener qo‘shish
        saveButton.onClick.AddListener(SaveUsername);
        closeButton.onClick.AddListener(CloseEvent);
        nameInputField.onEndEdit.AddListener(OnNameInputClosed);
        //ShowNameFieldShower();
    }
    public void OnNameInputClosed(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (HomeTutorialController.IsTutorialActive && saveButton != null)
                saveButton.interactable = true;
            NameAndCountryReady?.Invoke();
        }
    }
    private void SaveUsername()
    {
        string newUsername = nameInputField.text.Trim();

        if (!string.IsNullOrEmpty(newUsername))
        {
            int selectedCountry = countryDropdown.selectedItemIndex;

            PlayerPrefs.SetString(Constants.Player.UsernameKey, newUsername);
            playerName.text = newUsername;
            PlayerPrefs.SetInt(Constants.Player.CountryName, selectedCountry);
            PlayerPrefs.Save();

            DataManager.Instance?.SavePlayerProfile(newUsername, selectedCountry, true);

            HomeMainUI.Instance.UpdatePlayerName(newUsername);
            ProfileSaved?.Invoke();
            HomeMainUI.Instance.CloseUserDetailsPanel();
        }
        else
        {
            Debug.Log("Username bo'sh bo'lishi mumkin emas.");
        }


    }
    private void OnEnable()
    {
        previousSaveInteractable = saveButton != null && saveButton.interactable;
        previousCloseInteractable = closeButton != null && closeButton.interactable;
        if (HomeTutorialController.IsTutorialActive)
        {
            if (saveButton != null)
                saveButton.interactable = false;
            if (closeButton != null)
                closeButton.interactable = false;
        }

        CountrySelection();
        UITransilations();
        RefreshLevelUI();
    }

    private void OnDisable()
    {
        if (saveButton != null)
            saveButton.interactable = previousSaveInteractable;
        if (closeButton != null)
            closeButton.interactable = previousCloseInteractable;
    }

    private void CloseEvent()
    {
        HomeMainUI.Instance.CloseUserDetailsPanel();
    }

    private void CountrySelection()
    {
        int selectedCountry = PlayerPrefs.GetInt(Constants.Player.CountryName);
        countryDropdown.selectedItemIndex = selectedCountry;
    }
    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(99);
        statusLabelText.text = LanguageManager.Instance.GetText(276);
        levelLabelText.text = LanguageManager.Instance.GetText(101);
        teamLabelText.text = LanguageManager.Instance.GetText(102);
        saveBtnText.text = LanguageManager.Instance.GetText(39);
        closeText.text = LanguageManager.Instance.GetText(362);
        rankingLabelText.text = LanguageManager.Instance.GetText(247);
    }
    #region Scale Animation
    public void StartScaleLoop(GameObject targetObj)
    {
        if (targetObj != null)
        {
            StartCoroutine(LoopScale(targetObj));
        }
        else
        {
            Debug.LogWarning("Target object is null!");
        }
    }

    private IEnumerator LoopScale(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;

        while (true)
        {
            yield return StartCoroutine(ScaleTo(obj, originalScale * scaleUp));
            yield return StartCoroutine(ScaleTo(obj, originalScale));
        }
    }

    private IEnumerator ScaleTo(GameObject obj, Vector3 targetScale)
    {
        Vector3 startScale = obj.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        obj.transform.localScale = targetScale;
    }
    #endregion

    private void RefreshLevelUI()
    {
        if (DataManager.Instance == null)
            return;

        int currentLevel = DataManager.Instance.LevelAmount;
        int currentXp = DataManager.Instance.XP;

        if (levelCountText != null)
            levelCountText.text = currentLevel.ToString();

        if (levelProgress != null)
        {
            levelProgress.currentPercent = currentXp;
            levelProgress.UpdateUI();
        }
    }
}
