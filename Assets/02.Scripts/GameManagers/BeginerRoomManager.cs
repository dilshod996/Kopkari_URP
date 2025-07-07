using System.Threading.Tasks;
using UnityEngine;

public class BeginerRoomManager : BaseManager
{
    [SerializeField] private AudioClip jomboySound;
    protected override void Awake()
    {
        base.Awake(); // ✅ MUHIM!
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayMusic(jomboySound);
        // BeginerRoomManager.Instance = this; // ❌ ENDI SHART EMAS
        //PlayerPrefs.DeleteAll();
        modalWindowPopup.onConfirm.AddListener(MoveLobby);
    }

    protected override void Update()
    {
        base.Update();

        if (roomState == RoomState.GameFinished)
        {
            WinOrLosePage();
        }
    }

    //public async void GetMounCam()
    //{
    //    cameraManager.UseMountCamera();
    //    await Task.Delay(1500);
    //}

    //public override void GameStartedAction(bool state)
    //{
    //    base.GameStartedAction(state);

    //    if (state && !prizeManager.HasMorePrizes())
    //    {
    //        WinPanel?.WinPage();
    //        Debug.Log("O'yin tugadi. Barcha sovrinlar o'ynaldi.");
    //        return;
    //    }

    //    PrizeData prize = prizeManager.GetCurrentPrize();
    //    if (prize != null)
    //    {
    //        mainTime = prize.roundTime;

    //        int index = prizeManager.CurrentPrizeIndex;
    //        if (index > 0 && index < lambPositions.Count)
    //        {
    //            ContinueGameChanger(lambPositions[index - 1], lambPositions[index]);
    //        }
    //    }

    //    GameObjectsEnable(state);
    //}
    public override void GameStartedAction(bool state)
    {
        base.GameStartedAction(state);

        if (state)
        {
            // Hozirgi prize ni olamiz
            var prize = prizeManager.GetCurrentPrize();
            if (prize != null)
            {
                mainTime = prize.roundTime;
            }

            // Game holatini o‘zgartiramiz
            roomState = RoomState.GameStarted;
        }
        else
        {
            roomState = RoomState.None;
        }
    }


    public override void StartGame()
    {
        base.StartGame();
    }

    public override void WinOrLosePage()
    {
        if (IsCatched)
            IsCatched = false;

        if (pickableObj != null)
            pickableObj.Drop(); // Uloqni tashlaymiz
        // 1. O'yinni to'xtatamiz
        GameObjectsEnable(false);
        GameStartedAction(false);

        // 2. Uloq qo'lida bo'lsa qaytaramiz (tozalaymiz)


        // 3. Hozirgi prize haqida log
        Debug.Log("WinOrLosePage: Current prize index: " + prizeManager.CurrentPrizeIndex);

        // 4. G‘alaba yoki mag‘lubiyatni aniqlaymiz
        var prize = prizeManager.GetCurrentPrize();
        string labmOwner = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        if (LambOwner == labmOwner && mainTime > 0)
        {
            Debug.Log("✅ G‘alaba – foydalanuvchi uloqni manzilga yetkazdi.");
            HandleWin();
            SavePrizeDataToPlayerPrefs(prize.winPrizes);
        }
        else
        {
            Debug.Log("❌ Mag‘lubiyat – foydalanuvchi uloqni yetkaza olmadi.");
            HandleLose();
            SavePrizeDataToPlayerPrefs(prize.losePrizes);
        }
    }

    public override void HandleWin()
    {
        base.HandleWin();
        var prize = prizeManager.GetCurrentPrize();
        //GameStartedAction(false);
        ShowWinPage(prize); // prizeManager.MoveToNextPrize() bu yerda emas
        //dollyCamera?.BackToDolly(7f);
    }


    public override void HandleLose()
    {
        base.HandleLose();
        var prize = prizeManager.GetCurrentPrize();
        //GameStartedAction(false);
        ShowLoosePage(prize);
        //dollyCamera?.BackToDolly(3f);
        //prizeManager.MoveToNextPrize();
    }

    private async void ShowWinPage(PrizeData prize)
    {
        if (prize == null)
        {
            Debug.LogWarning("Prize null keldi — win sahifasi uchun prize kerak.");
            return;
        }

        smallWinPage.gameObject.SetActive(true);
        await Task.Yield();

        smallWinPage.DisplayPrizes(prize);

        // Prize o‘ynaldi deb belgilanadi
        //prizeManager.MoveToNextPrize();
    }


    private async void ShowLoosePage(PrizeData prize)
    {
        LoosePanel.gameObject.SetActive(true);
        await Task.Yield();
        LoosePanel.UserLost(prize);
    }

    public void MoveLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }
    public void BackMessage()
    {
        modalWindowPopup.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(280), LanguageManager.Instance.GetText(281),
                LanguageManager.Instance.GetText(1), LanguageManager.Instance.GetText(2)); 
    }
}