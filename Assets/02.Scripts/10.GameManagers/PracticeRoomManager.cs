using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PracticeRoomManager : BaseManager
{
    [SerializeField] private ModalWindowManager Popup;
    public TutorialScript tutorial;


    private void Awake()
    {


        Instance = this;
        //PlayerPrefs.DeleteAll();
    }
    void Start()
    {
        Popup.confirmButton.onClick.AddListener(BackToLobby);
    }
    protected override void Update()
    {
      base.Update();
    }
    public void PopupAppear()
    {
        Popup.UpdateUICustom("O'yindan chiqayapsizmi?", "Hali o'yin tugagani yo'qku davom etdirmaysizmi?");
    }
    public void BackToLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
        if (Time.timeScale == 0)
        {
            Debug.Log("Timescale: 0");
            Time.timeScale = 1;
        }
    }
    public override void TriggerEvent()
    {
        base.TriggerEvent();
        tutorial.SHowBoboginamNearUloq("Sizga aytgandim tushib ketadi deya, endi qayta harakat qiling..");

    }

    public void GettingFinish()
    {
        if (IsCatched)
        {
            IsCatched=false;
        }
        tutorial.MoreWithUloq();
    }


}
