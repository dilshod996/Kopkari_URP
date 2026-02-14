using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button playAgain;
    [SerializeField] private Button backLobby;

    public SceneLoadManager.SceneType sceneType;

    private void OnEnable()
    {
        backLobby.onClick.AddListener(BackHome);
        playAgain.onClick.AddListener(PlayAgainAction);
    }

    private void OnDisable()
    {
        playAgain.onClick.RemoveAllListeners();
        backLobby.onClick.RemoveAllListeners();
    }

    public void PlayAgainAction()
    {
        PlayAgainText();
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    public void PlayAgainText()
    {
        switch(sceneType)
        {
            case SceneLoadManager.SceneType.SecondRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, "This time is your win!");
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, "Egypt People waiting you race");
                break;
        }
    }
    private void BackHome()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, "Back To Home");
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
}
