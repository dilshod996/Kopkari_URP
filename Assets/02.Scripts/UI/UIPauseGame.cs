using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseGame : MonoBehaviour
{
    [SerializeField] private Button StopGame;
    [SerializeField] private Button ResumeBtn;
    [SerializeField] private Button LobbyBackBtn;

    void Start()
    {

        ResumeBtn.onClick.AddListener(ResumeGame);
        LobbyBackBtn.onClick.AddListener(BackLobby);
    }

    void OpenPanel()
    {
        Time.timeScale = 0f; 
    }
    private void OnEnable()
    {
        Time.timeScale = 0f;
    }
    void ResumeGame()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    void BackLobby()
    {
        SceneLoadMangager.Instance.LoadScene(SceneLoadMangager.SceneType.Lobby);;
    }
}
