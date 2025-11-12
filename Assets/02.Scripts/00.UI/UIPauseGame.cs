using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseGame : MonoBehaviour
{
    [SerializeField] private Button ResumeBtn;
    [SerializeField] private Button LobbyBackBtn;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text titlePause;
    [SerializeField] private TMP_Text resumeText;
    [SerializeField] private TMP_Text backLobbyText;
    [SerializeField] private TMP_Text settingsText;
    [SerializeField] private TMP_Text changeHorseText;

    void OpenPanel()
    {
        Time.timeScale = 0f; 
    }
    private void OnEnable()
    {
        Time.timeScale = 0f;
        UpdateTexts();
        ResumeBtn.onClick.AddListener(ResumeGame);
        LobbyBackBtn.onClick.AddListener(BackLobby);
    }
    private void OnDisable()
    {
        ResumeBtn.onClick.RemoveListener(ResumeGame);
        LobbyBackBtn.onClick.RemoveListener(BackLobby);
    }
    void ResumeGame()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    void BackLobby()
    {
        Time.timeScale = 1f;
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }

    private void UpdateTexts()
    {
        titlePause.text = LanguageManager.Instance.GetText(301);
        resumeText.text = LanguageManager.Instance.GetText(253);
        backLobbyText.text = LanguageManager.Instance.GetText(302);
        settingsText.text = LanguageManager.Instance.GetText(26);
        changeHorseText.text = LanguageManager.Instance.GetText(303);
    }
}
