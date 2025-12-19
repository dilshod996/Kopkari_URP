using System.Threading.Tasks;
using UnityEngine;

public class BeginerRoomManager : BaseManager
{
    [SerializeField] private AudioClip jomboySound;
    protected override void Awake()
    {
        base.Awake();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMusic(jomboySound);
        modalWindowPopup.onConfirm.AddListener(MoveLobby);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        KopkariMainUI.OnEverythingReadyStart += StartGame;
        UILookBackButton.OnCameraPressedState += CameraBackState;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        KopkariMainUI.OnEverythingReadyStart -= StartGame;
        UILookBackButton.OnCameraPressedState -= CameraBackState;
    }
    protected override void Update()
    {
        base.Update();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    public override void GameStartedAction(bool state)
    {
        base.GameStartedAction(state);
    }


    public override void StartGame()
    {
        base.StartGame();
    }

    public override void WinOrLosePage()
    {
        base.WinOrLosePage();
    }

    public override void HandleWin()
    {
        base.HandleWin();
    }


    public override void HandleLose()
    {
        base.HandleLose();
    }


    public void MoveLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Home);
    }
    public void BackMessage()
    {
        modalWindowPopup.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(280), LanguageManager.Instance.GetText(281),
                LanguageManager.Instance.GetText(1), LanguageManager.Instance.GetText(2)); 
    }
    public override void CameraBackState(bool state)
    {
        base.CameraBackState(state);    
    }
    public override void OnCheckpointReached(CheckpointTrigger checkpoint, GameObject riderObj)
    {
        // Avval BaseManager logikasini chaqirmoqchi bo‘lsang:
        base.OnCheckpointReached(checkpoint, riderObj);
    }
    public override void OnAllCheckpointsCompleted()
    {
        base.OnAllCheckpointsCompleted();
    }
}