using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class JomboyManager : BaseManager
{
    public static JomboyManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    //protected override void Start()
    //{
    //    base.Start();
    //    if (pickableObj != null)
    //    {
    //        pickableObj.OnPicked.AddListener(OnUloqPicked);
    //    }
    //}
    //protected override void Update()
    //{
    //    base.Update();
    //    // GameFinished holatida Prize mavjud bo¡®lsa, davom etadi
    //    if (roomState == RoomState.GameFinished || roomState == RoomState.LambReachTarget)
    //    {
    //        WinOrLosePage(); // prize indexni bu yerda tekshirish shart emas
    //    }
    //}
    //public async void GetMounCam()
    //{
    //    cameraManager.UseMountCamera();
    //    await Task.Delay(1500); // Kamera o¡®tishi uchun 1 soniya kutamiz
    //}
    //public override void GameStartedAction(bool state)
    //{
    //    timeText.color = new Color32(41, 9, 219, 255);

    //    if (state && currentPrizeIndex >= prizeConfig.prizes.Count)
    //    {
    //        WinPanel?.WinPage(); // prize tugagan bo¡®lsa

    //        Debug.Log("O'yin tugadi. Barcha sovrinlar o'ynaldi.");
    //        return;
    //    }
    //    else if (currentPrizeIndex >= prizeConfig.prizes.Count)
    //    {
    //        Debug.Log("O'yin tugadi. Barcha sovrinlar o'ynaldi.");
    //        //return;
    //    }
    //    else
    //    {
    //        var prize = prizeConfig.prizes[currentPrizeIndex]; /// Prize indexni olib olamiz

    //        // Main vaqt prizeConfig ichida
    //        mainTime = prize.roundTime;

    //        // pozitsiyalarni prize indexga qarab o¡®zgartirish
    //        if (currentPrizeIndex > 0 && currentPrizeIndex < lambPositions.Count)
    //        {
    //            ContinueGameChanger(lambPositions[currentPrizeIndex - 1], lambPositions[currentPrizeIndex]);
    //        }
    //    }


    //    GameObjectsEnable(state);
    //}
    //public override void WinOrLosePage()
    //{
    //    roomState = RoomState.None;
    //    currentPrizeIndex++;

    //    if (IsCatched) IsCatched = false;
    //    Debug.Log("Prize index: " + currentPrizeIndex);
    //    if (LambOwner == "dima" && mainTime > 0)
    //    {
    //        HandleWin();
    //    }
    //    else
    //    {
    //        HandleLose();
    //    }


    //}


    //public override void HandleWin()
    //{
    //    var prize = prizeConfig.prizes[currentPrizeIndex - 1];
    //    GameStartedAction(false);
    //    ShowWinPage(prize);
    //    dollyCamera?.BackToDolly(7f);

    //}
    //public override void HandleLose()
    //{
    //    if (currentPrizeIndex > prizeConfig.prizes.Count - 1)
    //    {
    //        WinPanel?.WinPage(); // prize tugagan bo¡®lsa
    //        return;
    //    }
    //    var prize = prizeConfig.prizes[currentPrizeIndex];
    //    GameStartedAction(false);

    //    ShowLoosePage(prize);

    //    dollyCamera?.BackToDolly(3f); // Yutqazganda boshqa holat


    //}
    //private async void ShowWinPage(PrizeData prize)
    //{
    //    smallWinPage.gameObject.SetActive(true);
    //    await Task.Yield();
    //    smallWinPage.WinSessionEnable(prize);
    //}

    //private async void ShowLoosePage(PrizeData prize)
    //{
    //    LoosePanel.gameObject.SetActive(true);
    //    await Task.Yield();
    //    LoosePanel.UserLost(prize);
    //}
}
