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
    [SerializeField] private Button howToPlay;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text titlePause;
    [SerializeField] private TMP_Text resumeText;
    [SerializeField] private TMP_Text backLobbyText;
    [SerializeField] private TMP_Text howToPlayText;
    private bool _paused;

    [Header("Pages")]
    [SerializeField] private HowToPlay howToPlayPage;
    private void Awake()
    {
        ResumeBtn.onClick.AddListener(ResumeGame);
        LobbyBackBtn.onClick.AddListener(BackLobby);
        howToPlay.onClick.AddListener(EnableHowToPlay);
    }

    private void OnEnable()
    {
        UpdateTexts();
        if (!_paused)
            StartCoroutine(PauseNextFrame());  
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        _paused = false;
        if(KopkariMainUI.Instance != null)
        {
            KopkariMainUI.Instance.HideUI(this);
        }
        else
        {
            UIButtonActions.Instance.HideUI(this);
        }
        
    }

    void BackLobby()
    {
        Time.timeScale = 1f;
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }

    private void UpdateTexts()
    {
        if(LanguageManager.Instance != null)
        {
            titlePause.text = LanguageManager.Instance.GetText(301);
            resumeText.text = LanguageManager.Instance.GetText(253);
            backLobbyText.text = LanguageManager.Instance.GetText(302);
            howToPlayText.text = LanguageManager.Instance.GetText(195);
        }
    }
    private IEnumerator PauseNextFrame()
    {
        yield return new WaitForSecondsRealtime(0.45f);
        Time.timeScale = 0f;
        _paused = true;
    }

    private void EnableHowToPlay()
    {
        if (howToPlayPage != null)
        {
            howToPlayPage.gameObject.SetActive(true);
        }
    }
}
