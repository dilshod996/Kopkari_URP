using Cinemachine;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class TutorialScript : MonoBehaviour
{
    //main camera
    [SerializeField] private CameraSwitcher cameraChanger;

    private Animator handAC;
    [SerializeField] private GameObject tutorailBg;
    [SerializeField] private GameObject cameraUp;
    [SerializeField] private GameObject buttons;
    [SerializeField] private GameObject jumpButtonSideobjs;
    [SerializeField] private GameObject topButton;
    [SerializeField] private TMP_Text npcText;

    //Tutorialdan tashqarida bolgan npc
    [SerializeField] private GameObject boboginam;
    [SerializeField] private TMP_Text boboginamText;

    // uloq bilan boladigan jarayonlarr
    [SerializeField] private Button uloqEnableer;
    [SerializeField] private GameObject uloq;
    [SerializeField] private Transform uloqStartPos;
    [SerializeField] private GameObject wayShowArray;
    [SerializeField] private GameObject particles;

    [SerializeField] private NotificationManager notifBoboy;

    public bool isTimeExplained=false;
    public enum TutorialName
    {
        moveJoystick,
        cameraMove,
        jumpHorse,
        miniMap,
        uloqEnable,
        arrowShower,
        getUloqEnum,
        timeExplainEnum
    }

    private Coroutine uloqGetCoroutine;
    private void Start()
    {
        handAC = GetComponent<Animator>();
        CheckTutorialsPractice();
    }
    public void CloseAfterAnim()
    {
        tutorailBg.gameObject.SetActive(false);
    }
    public void HandAnimPlay(string animName)
    {
        if (AnimationExists(animName))
        {
            BgEnable();
            handAC.Play(animName);
        }
        else
        {
            Debug.Log("Animatsiya topilmadi: " + animName);
        }
    }
    private bool AnimationExists(string animName)
    {
        if (handAC.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animatorda hech qanday controller yo¡®q!");
            return false;
        }

        foreach (AnimationClip clip in handAC.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animName)
            {
                return true;
            }
        }
        return false;
    }
    public void BgEnable()
    {
        tutorailBg.SetActive(true);
    }

    #region Tutorial 
    public void TutorialSHow(string namePrefs)
    {
        if (PlayerPrefs.GetInt(namePrefs, 1) == 1)
        {
            HandAnimPlay(namePrefs);
            if (Enum.TryParse(namePrefs, out TutorialName tutorialEnum))
            {
                switch (tutorialEnum)
                {
                    case TutorialName.moveJoystick:
                        npcText.text = "Ha otam, otni bir choptiring qani bir ko'raylik...";
                        break;
                    case TutorialName.cameraMove:
                        if (cameraUp.activeSelf)
                        {
                            cameraUp.SetActive(false);
                        }
                        npcText.text = "Otam otni qayga olib boryapsiz bizga ko'rinmay turibdida...";
                        break;
                    case TutorialName.jumpHorse:
                        if (buttons.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                        }
                        npcText.text = "Bu polvon otizni sakrashini bir koraylik endi...";
                        break;
                    case TutorialName.miniMap:
                        if (jumpButtonSideobjs.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                            jumpButtonSideobjs.SetActive(false);
                        }
                        npcText.text = "Otam bu burgutda nima gaplar ekana xabar oldizmi...?";
                        break;
                    case TutorialName.uloqEnable:
                        if (jumpButtonSideobjs.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                            jumpButtonSideobjs.SetActive(false);
                            topButton.SetActive(false);
                        }
                        npcText.text = "Otaginam uloq chopmagan polvon polvonmi qani biz ham chopamizmi...";
                        if(!uloqEnableer.gameObject.activeSelf){
                            uloqEnableer.gameObject.SetActive(true);
                        }
                        break;
                    case TutorialName.arrowShower:
                        if (jumpButtonSideobjs.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                            jumpButtonSideobjs.SetActive(false);
                            topButton.SetActive(false);
                        }
                        npcText.text = "Polvon sizga bu ko'rsatkich yo'l ko'rsatadi qayga borishizdi orqasidan boramiz";
                        break;
                    case TutorialName.getUloqEnum:
                        if (jumpButtonSideobjs.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                            jumpButtonSideobjs.SetActive(false);
                            topButton.SetActive(false);
                        }
                        if (!uloqEnableer.gameObject.activeSelf)
                        {
                            uloqEnableer.gameObject.SetActive(true);
                        }
                        break;
                    case TutorialName.timeExplainEnum:
                        if (jumpButtonSideobjs.activeSelf)
                        {
                            cameraUp.SetActive(false);
                            buttons.SetActive(false);
                            jumpButtonSideobjs.SetActive(false);
                            topButton.SetActive(false);
                        }
                        if (!uloqEnableer.gameObject.activeSelf)
                        {
                            uloqEnableer.gameObject.SetActive(true);
                        }
                        break;
                }
            }
            Debug.Log("Show Tutorial : " + namePrefs);
            //PlayerPrefs.SetInt("moveJoystick", 0);
        }
    }

    public void SavePrefs(string namePrefs)
    {
        if (PlayerPrefs.GetInt(namePrefs, 1) == 1)
        {
            PlayerPrefs.SetInt(namePrefs, 0);
            if (Enum.TryParse(namePrefs, out TutorialName tutorialEnum))
            {
                switch (tutorialEnum)
                {
                    case TutorialName.moveJoystick:
                        cameraUp.SetActive(false);
                        // TutorialSHow("cameraMove");
                        break;
                    case TutorialName.cameraMove:
                        buttons.SetActive(false);
                        break;
                    case TutorialName.jumpHorse:
                        StartCoroutine(tutorialDelay("miniMap"));
                        jumpButtonSideobjs.SetActive(false);
                        break;
                    case TutorialName.miniMap:
                        topButton.SetActive(false);
                        StartWithUloq("Uloq bilan bir mashg'ulot o'tkizmaymizmi otaginam...");
                        break;
                    case TutorialName.uloqEnable:
                       // EnableUloqThings();
                        break;
                    case TutorialName.getUloqEnum:
                        break;
                    case TutorialName.timeExplainEnum:
                        break;
                }
                Debug.Log("Saqlandi: " + tutorialEnum);
            }
            else
            {
                Debug.LogError("Notugri tutorial nom berildi...");
            }

        }
        else
        {
            Debug.Log("Tutorial already saved");
        }
    }
    
    IEnumerator tutorialDelay(string delayTutorial)
    {
        yield return new WaitForSeconds(2f); 
        TutorialSHow(delayTutorial);
    }
    public void CheckTutorialsPractice()
    {
        List<TutorialName> unsavedTutorials = new List<TutorialName>();

        foreach (TutorialName tutorial in Enum.GetValues(typeof(TutorialName)))
        {
            if (PlayerPrefs.GetInt(tutorial.ToString(), 1) == 1) // Agar tutorial hali saqlanmagan bo¡®lsa
            {
                unsavedTutorials.Add(tutorial);
            }
        }

        // Agar hali bajarilmagan tutoriallar bo¡®lsa, ularni chiqaramiz
        if (unsavedTutorials.Count > 0)
        {
            TutorialSHow(unsavedTutorials[0].ToString());
            Debug.Log("Hali saqlanmagan tutoriallar: " + string.Join(", ", unsavedTutorials));
        }
        else
        {
            Debug.Log("Barcha tutoriallar bajarilgan!");
            if (!uloqEnableer.gameObject.activeSelf)
            {
                uloqEnableer.gameObject.SetActive(true);
            }
            if (jumpButtonSideobjs.activeSelf)
            {
                cameraUp.SetActive(false);
                buttons.SetActive(false);
                jumpButtonSideobjs.SetActive(false);
                topButton.SetActive(false);
            }
        }
    }

    #endregion


    #region Boboginam Tutorial

    private void StartWithUloq(string description)
    {
        
        StartCoroutine(ShowBoboginam(description));
    }
    IEnumerator ShowBoboginam(string text)
    {
        yield return new WaitUntil(() => cameraChanger.backFirstCam == true);
        notifBoboy.CustomBoboy(text);
        yield return new WaitForSeconds(3f);
        uloqEnableer.gameObject.SetActive(true);
        TutorialSHow("uloqEnable");
        
    }

    public void SHowBoboginamNearUloq(string description)
    {
        notifBoboy.CustomBoboy(description);
        // Agar avvalgi coroutine hali ishlayotgan bo'lsa, uni to'xtatamiz
        //if (uloqGetCoroutine != null)
        //{
        //    StopCoroutine(uloqGetCoroutine);
        //}

        //uloqGetCoroutine = StartCoroutine(BoboginamUloqNearMsg("Uloqqa yaqinroq keling polvon, yana ham yaqinroq"));
    }
    #endregion

    #region Uloq bilan Mashgulotlar
    public void EnableUloqThings(bool state)
    {
        uloq.SetActive(state);
        wayShowArray.SetActive(state);
        particles.SetActive(state);
        uloqEnableer.gameObject.SetActive(!state);
    }
    public void EnableArrayTutorial()
    {
        StartCoroutine(tutorialDelay("arrowShower"));
        EnableUloqThings(true);
    }

    public void SaveArrayPrefs()
    {
        SavePrefs("arrowShower");
    }

    public void SHowHotToGetUloqTutorial()
    {
        if(PlayerPrefs.GetInt("getUloqEnum", 1) == 1)
        {
            notifBoboy.CloseNotification();
            HandAnimPlay("getUloq");
            npcText.text = "Uloqni olish uchun mahkam ushlab turgin tugmani, polvon";
        }
        else
        {
            Debug.Log("saved already");
        }
        
    }

    //if want more with ulow game
    public void MoreWithUloq()
    {
        StartCoroutine(EnableUloqWanted());
    }
    IEnumerator EnableUloqWanted()
    {
        SHowBoboginamNearUloq("Ey barakallo polvon yashavor");
        EnableUloqThings(false);
        uloq.transform.SetPositionAndRotation(uloqStartPos.position, uloqStartPos.rotation);
        yield return new WaitForSeconds(3f);
        HandAnimPlay("uloqEnable");
        npcText.text = "Agar yana uloq bilan mashq qilmoqchi bo'lsang uloqni istagan vaqting qo'shishing mumkin";
    }
    //showing time tutorial
    public void ShowTimeTutorial(Action action)
    {
        StartCoroutine(DelayPickup(action));
        //HandAnimPlay("timeExplanation");
        //npcText.text = "Uloqni doim ushlab yura olmaysan, ma'lum vaqtdan keyin tushib ketadi E'TIBORLI bo'lgin, polvon";
    }
    IEnumerator DelayPickup(Action action)
    {
        if (PlayerPrefs.GetInt("timeExplainEnum", 1) == 1)
        {
            SavePrefs("getUloqEnum");
            HandAnimPlay("timeExplanation");
            npcText.text = "Uloqni doim ushlab yura olmaysan, ma'lum vaqtdan keyin tushib ketadi E'TIBORLI bo'lgin, polvon";
            //in here first time so delay a bit that user can read what is going on then pick up the uloqqq
            yield return new WaitUntil(() => isTimeExplained == true);
            action?.Invoke();
        }
        else
        {
            Debug.Log("timeExplainEnum saved already just do the action");
            action?.Invoke();
        }



    }
    // i will put to animation event
    public void AfterTimeShowTutorial()
    {
        CloseAfterAnim();
        SavePrefs("timeExplainEnum");
        isTimeExplained = true;
    }
    #endregion

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
