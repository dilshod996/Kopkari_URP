using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserDetails : MonoBehaviour
{
    [Header("UI Texts")]    
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text levelLabelText;
    [SerializeField] private TMP_Text teamLabelText;
    [SerializeField] private TMP_Text versionLabelText;
    [SerializeField] private TMP_Text saveBtnText;
    [SerializeField] private TMP_Text statusLabelText;
    [SerializeField] private TMP_Text rankingLabelText;


    [Header("UI Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private CustomDropdown countryDropdown;
    [SerializeField] private TMP_Text levelCountText;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text closeText;

    [SerializeField] private PlayerPrefsData playerPrefsObj;
    //[SerializeField] private LobbyManager lobbyManager;

    [Header("User Details Settings")]
    [SerializeField] private float scaleUp = 1.2f;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private GameObject nameContainerObj;
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
    private void ShowNameFieldShower()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.Name))
        {
            StartCoroutine(DelayNameFieldShow());

        }
    }
    IEnumerator DelayNameFieldShow()
    {
        yield return new WaitForEndOfFrame(); 
        //HomeMainUI.Instance.ShowNameField();
    }
    public void OnNameInputClosed(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            HomeMainUI.Instance.ShowSaveBtn(); // Save button tutorial
        }
    }
    private void SaveUsername()
    {
        string newUsername = nameInputField.text.Trim();

        if (!string.IsNullOrEmpty(newUsername))
        {
            PlayerPrefs.SetString(Constants.Player.UsernameKey, newUsername);
            Debug.Log("Yangi username saqlandi: " + newUsername);
            playerName.text = newUsername;
            PlayerPrefs.SetInt(Constants.Player.CountryName, countryDropdown.selectedItemIndex);
            PlayerPrefs.Save();
            HomeMainUI.Instance.UpdatePlayerName(newUsername);
            //gameObject.SetActive(false);
            HomeMainUI.Instance.FinishNameTutorial();
        }
        else
        {
            //StartScaleLoop(nameContainerObj);
            Debug.Log("Username bo'sh bo'lishi mumkin emas.");
        }


    }
    private void OnEnable()
    {
       // nameContainerObj.transform.localScale = Vector3.one; // Reset scale to original size
        CountrySelection();
        UITransilations();
    }

    private void CloseEvent()
    {
        HomeMainUI.Instance.CloseUserDetailsPanel();
        //if (!PlayerPrefs.HasKey(Constants.Player.UsernameKey))
        //{
        //    StartScaleLoop(nameContainerObj);
        //}
        //else
        //{
        //    gameObject.SetActive(false);
        //    if (!PlayerPrefs.HasKey("horseData"))
        //    {
        //        playerPrefsObj.gameObject.SetActive(true);
        //        playerPrefsObj.HorseDataCheck();
        //    }
        //}
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
        versionLabelText.text = LanguageManager.Instance.GetText(103);
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
}
