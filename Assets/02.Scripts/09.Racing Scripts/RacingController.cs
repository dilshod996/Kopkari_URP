using MalbersAnimations;
using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RacingController : MonoBehaviour
{
    public static RacingController Instance { get; protected set; }
    public MAnimal horse;
    public MAnimal riderAnimal;
    [SerializeField] private List<AIRacingRider> aiRiders;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private float countdownDelay = 1f; // har bosqich oralig‘i
    [SerializeField] private float startTextDuration = 0.3f; // "Start" yozuvi qancha turadi



    [Header("Leaderboard Fade-In")]
    [SerializeField] private RacingLeaderboard leaderboard;
    [SerializeField] private CanvasGroup leaderboardGroup;        // leaderboard panel (CanvasGroup kerak)
    [SerializeField] private RectTransform leaderboardRoot;       // ixtiyoriy: scale pop anim uchun
    [SerializeField] private float leaderboardFadeDuration = 0.35f;
    [SerializeField] private LeanTweenType leaderboardEase = LeanTweenType.easeOutCubic;
    [SerializeField] private float leaderboardPopScale = 1.03f;   // 1.0 = popsiz



    [SerializeField] private GameObject mobileCanvasPanel;


    [Header("Sprint UI Effect")]
    [SerializeField] private Image sprintImg;

    [Header("Reverse UI")]
    [SerializeField] private RectTransform reversePanel;     // boshida SetActive(false)
    [SerializeField] private TMP_Text reverseTimeText;
    [SerializeField] private float slideDuration = 0.25f;    // anim vaqti
    [SerializeField] private float panelShownY = -165f;         // ko‘rinadigan y
    [SerializeField] private float panelHiddenY = 150f;      // yuqoriga yashirin y (anchored)
    [SerializeField] private float reverseGraceTime = 5f;    // sekund
    [SerializeField] private float uiTick = 0.2f;            // progress text yangilash
    private Coroutine reverseCo;
    private bool reverseActive;
    private float tLeft;

    [Header("Popup Data")]
    [SerializeField] UISpeechBuble speechBubble;

    [Header("Game Over")]
    [SerializeField] GameOver gameOverPanel;
    [Header("Walk Zone Prefab")]
    public GameObject walkZonePrefab;
    [Header("Camera Details")]
    [SerializeField] private ThirdPersonFollowTarget mainCam;
    [SerializeField] private ThirdPersonFollowTarget finishCam;
    [SerializeField] private ThirdPersonFollowTarget sprintCam;

    public float cameraDistance = 4.5f;
    [SerializeField] private float frontDistance = 6f;
    [SerializeField] private float backDistance = 3f;
    [SerializeField] private float backOffsetY = 0.4f;

    [Header("Starting Point Slider Values")]
    [SerializeField] private int defaultSpeedIndex = 5;  // odatiy tezlik
    [SerializeField] private int boostSpeedIndex = 6;    // max tezlik
    [SerializeField] private float boostTimeMultiplier = 4f; // slider * 4 sekund
    private Coroutine boostRoutine;

    private float boostTime;
    private float penaltyTime;

    public static Action<float> OnOverallBoostTime;
    public static Action<float> OnOverallPenaltyTime;

    public static Action OnRacingFinished;
    public static Action OnRacingStarted;

    #region Starting Functions
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }
    void Start()
    {
        InitLeaderboardPanelHidden();
        SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);
        //GetSetAnimal(HorseMine.Instance.horseAnimal);
    }
    private void OnEnable()
    {
        StartPowerBar.OnStartPowerSelected += OnPowerSelected;
        PlayerDataManager.OnRiderAndHorse += GetSetAnimal;
        BoostersContainer.OnBoostTime += SetBoostTime;
        BoostersContainer.OnPenaltyTime += SetPenaltyTime;
        UIButtonActions.OnSprintStart += HorseSprint;
        UIButtonActions.OnSprintEnd += HorseDefaultSpeed;
        BoostersContainer.OnSprintEffectStart += SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd += SprintCameraDisable;
        RacingResultPage.OnGetRiderRank += PlayFinalAnim;
        UILookBackButton.OnCameraPressedState += CameraBackState;

    }
    private void OnDestroy()
    {
        // poolingdagi barcha aktiv WalkZone obyektlarni qaytaradi
        SimplePool.ClearAll();
        StartPowerBar.OnStartPowerSelected -= OnPowerSelected;
        PlayerDataManager.OnRiderAndHorse -= GetSetAnimal;
        BoostersContainer.OnBoostTime -= SetBoostTime;
        BoostersContainer.OnPenaltyTime -= SetPenaltyTime;
        UIButtonActions.OnSprintStart -= HorseSprint;
        UIButtonActions.OnSprintEnd -= HorseDefaultSpeed;
        BoostersContainer.OnSprintEffectStart -= SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd -= SprintCameraDisable;
        RacingResultPage.OnGetRiderRank -= PlayFinalAnim;
        UILookBackButton.OnCameraPressedState -= CameraBackState;
        riderAnimal = null;
        horse = null;
    }
    #endregion

    #region Removed Boshlanish player ruyxat
    //public void OnStartButtonPressed()
    //{
    //    StartCoroutine(StartCountdown());
    //}

    //private IEnumerator StartCountdown()
    //{
    //    countText.gameObject.SetActive(true);

    //    for (int i = 3; i >= 1; i--)
    //    {
    //        countText.text = i.ToString();
    //        yield return new WaitForSeconds(countdownDelay);
    //    }

    //    // "Start" yozuvi
    //    countText.text = "Start!";
    //    yield return new WaitForSeconds(startTextDuration);

    //    //countText.gameObject.SetActive(false);

    //    // endi poyga boshlanadi
    //    //StartRacing();
    //}
    #endregion

    #region Start and Stop Racing
    public void StartRacing()
    {
        EnableNavMesh();
        ShowLeaderboardPanel();
        RacingLeaderboard.Instance.StartRace();
        OnRacingStarted?.Invoke();
    }

    public void StopRacing()
    {
        DisableNavmesh();
        //StopAlwaysForward();
    }
    #endregion

    #region LeaderBoard
    private void InitLeaderboardPanelHidden()
    {
        if (!leaderboardGroup) return;

        leaderboardGroup.gameObject.SetActive(true); // alpha 0: ko‘rinmaydi, lekin mavjud
        leaderboardGroup.alpha = 0f;
        leaderboardGroup.interactable = false;
        leaderboardGroup.blocksRaycasts = false;

        if (leaderboardRoot)
        {
            leaderboardRoot.localScale = Vector3.one; // boshlang‘ich holat
        }
    }

    private void ShowLeaderboardPanel()
    {
        if (!leaderboardGroup) return;

        // avvalgi tweennni to‘xtatib qo‘yish xavfsiz
        LeanTween.cancel(leaderboardGroup.gameObject);

        // ixtiyoriy: kichik pop effekt
        if (leaderboardRoot && leaderboardPopScale > 1f)
        {
            leaderboardRoot.localScale = Vector3.one * leaderboardPopScale;
            LeanTween.scale(leaderboardRoot, Vector3.one, leaderboardFadeDuration)
                     .setEase(leaderboardEase);
        }

        // alpha fade-in
        leaderboardGroup.alpha = 0f;
        LeanTween.alphaCanvas(leaderboardGroup, 1f, leaderboardFadeDuration)
                 .setEase(leaderboardEase)
                 .setOnComplete(() =>
                 {
                     leaderboardGroup.interactable = true;
                     leaderboardGroup.blocksRaycasts = true;
                 });
    }
    private void HideLeaderboardPanel()
    {
        if (!leaderboardGroup) return;

        leaderboardGroup.interactable = false;
        leaderboardGroup.blocksRaycasts = false;

        LeanTween.cancel(leaderboardGroup.gameObject);
        LeanTween.alphaCanvas(leaderboardGroup, 0f, leaderboardFadeDuration)
                 .setEase(leaderboardEase);
    }
    #endregion

    #region AI Horses
    public void EnableNavMesh()
    {
        for(int i = 0; i < aiRiders.Count; i++)
        {
            aiRiders[i].EnableNavmesh();
        }
    }
    public void DisableNavmesh()
    {
        for(int i = 0;i < aiRiders.Count; i++)
        {
            aiRiders[i].DisableNavmesh();
        }
    }
    #endregion

    #region Rider Details
    public void PlayFinalAnim(int ranking)
    {
        switch(ranking)
        {
            case 1: case 2: case 3:
                riderAnimal?.Mode_Activate(16, 1);
                break;
            default:
                riderAnimal?.Mode_Activate(16, 2);
                break;
        }
    }

    #endregion

    #region Horse Manage
    private void OnPowerSelected(float sliderValue)
    {
        StartHorseRun(horse, sliderValue);
        StartRacing();
    }
    public void StartHorseRun(MAnimal mAnimal, float sliderValue)
    {
        StartCoroutine(HorseRunStarter(mAnimal, sliderValue));
    }

    private IEnumerator HorseRunStarter(MAnimal mAnimal, float sliderValue)
    {
        horse = mAnimal;

        // horse null bo‘lmaguncha kutadi
        yield return new WaitUntil(() => horse != null);

        horse.Always_Forward(true);
        mobileCanvasPanel.gameObject.SetActive(true);

        // shu yerda tezlik hisoblanadi
        CalculateSpeed(sliderValue);
    }

    public void CalculateSpeed(float sliderValue)
    {
        if (horse == null)
        {
            return;
        }

        // 0..1 oralig‘iga qisib qo‘yamiz
        sliderValue = Mathf.Clamp01(sliderValue);

        // default (bosilmagan) holat: faqat 4-speedda yuradi
        if (sliderValue <= 0f)
        {
            horse.Speed_CurrentIndex_Set(defaultSpeedIndex);
            return;
        }

        // necha sekund boost bo‘lishini hisoblaymiz
        float boostDuration = sliderValue * boostTimeMultiplier;

        // agar avvalgi coroutine ishlayotgan bo‘lsa – to‘xtatamiz
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);

        boostRoutine = StartCoroutine(SpeedBoostRoutine(boostDuration));
    }

    private IEnumerator SpeedBoostRoutine(float duration)
    {
        // 1) Avval default speedga qo‘yib olamiz (ishonch uchun)
        //horse.Speed_CurrentIndex_Set(defaultSpeedIndex);

        // 2) Boost ON → max tezlik
        sprintImg.gameObject.SetActive(true);
        horse.Speed_CurrentIndex_Set(boostSpeedIndex);

        // 3) Sliderga qarab hisoblangan vaqt kutamiz
        yield return new WaitForSeconds(duration);
        sprintImg.gameObject.SetActive(false);

        // 4) Yana default speedga qaytaramiz
        horse.Speed_CurrentIndex_Set(defaultSpeedIndex);

        boostRoutine = null;
    }

    public void GetSetAnimal(MAnimal horseAnimal, MAnimal riderAnim)
    {
        horse = horseAnimal;
        riderAnimal = riderAnim;
    }

    public void StopHorseRun()
    {
        StartCoroutine(HorseStopAction());
    }
    private IEnumerator HorseStopAction(Action action=null)
    {
        // 3 soniyadan so‘ng to‘xtatamiz misol uchun
        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);
        CameraPostionCheck();
        HideLeaderboardPanel();
        mobileCanvasPanel.gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        horse.StopMoving();
        OnRacingFinished?.Invoke();
        action?.Invoke();
    }

    private void HorseSprint()
    {
        if (horse != null) { horse.Speed_CurrentIndex_Set(boostSpeedIndex);
            SprintCameraEnable();
        }
    }

    private void HorseDefaultSpeed()
    {
        if (horse != null) { horse.Speed_CurrentIndex_Set(defaultSpeedIndex);  SprintCameraDisable(); }
    }
    #endregion

    #region Scene Details.
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Home);
    }
    #endregion


    #region Horse Back Running
    public void StartReverse()
    {
        // Timer reset (agar allaqachon aktiv bo‘lsa ham yangilaymiz)
        tLeft = reverseGraceTime;
        SpeechBubbleEnable("Ogohlantirish orqaga yugurayapsan!");
        if (!reverseActive)
        {
            reverseActive = true;
            ShowPanel(); // SetActive(true) + slide in

            if (reverseCo != null) StopCoroutine(reverseCo);
            reverseCo = StartCoroutine(ReverseCountdown());
        }
        // reverseActive bo‘lsa ham faqat tLeft yangilandi (UI shu korutinada yangilanadi)
    }

    public void ClearReverse()
    {
        if (!reverseActive) return;
        SpeechBubbleDisable();
        reverseActive = false;
        if (reverseCo != null) { StopCoroutine(reverseCo); reverseCo = null; }
        HidePanel(); // slide out + SetActive(false)

    }

    // ===== UI Anim (LeanTween) =====
    private void ShowPanel()
    {
        if (!reversePanel) return;

        reversePanel.gameObject.SetActive(true); // LT uchun active bo‘lishi shart
        LeanTween.cancel(reversePanel);

        // start pozitsiya: hiddenY
        var ap = reversePanel.anchoredPosition;
        reversePanel.anchoredPosition = new Vector2(ap.x, panelHiddenY);

        LeanTween.value(reversePanel.gameObject, panelHiddenY, panelShownY, slideDuration)
                 .setEaseOutCubic()
                 .setOnUpdate((float y) =>
                 {
                     var p = reversePanel.anchoredPosition;
                     reversePanel.anchoredPosition = new Vector2(p.x, y);
                 });
    }

    private void HidePanel()
    {
        if (!reversePanel) return;

        LeanTween.cancel(reversePanel);
        var ap = reversePanel.anchoredPosition;

        LeanTween.value(reversePanel.gameObject, ap.y, panelHiddenY, slideDuration)
                 .setEaseInCubic()
                 .setOnUpdate((float y) =>
                 {
                     var p = reversePanel.anchoredPosition;
                     reversePanel.anchoredPosition = new Vector2(p.x, y);
                 })
                 .setOnComplete(() =>
                 {
                     if (reverseTimeText) reverseTimeText.text = "";
                     reversePanel.gameObject.SetActive(false); // qayta inactive
                 });
    }

    // ===== Countdown (Update yo‘q) =====
    private IEnumerator ReverseCountdown()
    {
        var wait = new WaitForSecondsRealtime(uiTick);

        while (reverseActive && tLeft > 0f)
        {
            if (reverseTimeText) reverseTimeText.text = $"{tLeft:0}";
            tLeft -= uiTick;
            penaltyTime += uiTick;
            yield return wait;
        }

        reverseCo = null;

        if (reverseActive)
        {
            // Timeout → DQ
            reverseActive = false;
            HidePanel();
            Disqualify();
        }
    }

    // ===== DQ logika =====
    private void Disqualify()
    {
        // Bu yerda sizning o‘yindagi jazo:
        // - Malbers: hayvonni bloklash
        // - Result page/popup
        // - Leaderboardga signal
        // Masalan:
        // var animal = FindObjectOfType<MalbersAnimations.Controller.MAnimal>();
        // if (animal) animal.Lock(true);
        StartCoroutine(HorseStopAction(GameOverPanel));
        SpeechBubbleDisable();
        Debug.Log("[RacingController] Reverse timeout -> DQ");
    }

    private void GameOverPanel()
    {
        gameOverPanel.gameObject.SetActive(true);
    }
    #endregion

    #region Popup Speech Bubble
    public void ShowAndHideSpeech(string speech)
    {
        StartCoroutine(SpeechCoroutine(speech));
    }
    private IEnumerator SpeechCoroutine(string text)
    {
        SpeechBubbleEnable(text);
        yield return new WaitForSeconds(3.5f);
        SpeechBubbleDisable();
    }
    public void SpeechBubbleEnable(string text)
    {
       // speechBubble.gameObject.SetActive(true);
        speechBubble.Show(text);
    }
    public void SpeechBubbleDisable()
    {
        speechBubble.Hide();
    }
    #endregion

    #region Camera Details

    private void SprintCameraEnable()
    {
        sprintCam.SetPriority(true);
    }
    private void SprintCameraDisable() { sprintCam.SetPriority(false); }
    public void CameraPostionCheck()
    {
        horse.UseCameraInput = false;
        finishCam.SetFinishViewSmooth(
            cameraDistance,  // masofa
            -32f,             // yaw -> Inspector’da 18 bo'lishi uchun
            -4f,             // pitch (istaganingcha o'zgartirishing mumkin)
            -0.85f,  // pastga/pasga offset kerak bo'lsa
            1f               // 1 soniyada aylanib borsin, xohlasang 0.5 / 2f qil
        );
    }

    private void CameraBackState(bool state)
    {
        if (state) LookBack();
        else MainCam();
    }
    public void LookBack()
    {
        if (mainCam == null) return;

        horse.UseCameraInput = false;

        // masofani va offsetni biroz o'zgartirishni xohlasang:
        mainCam.SetCameraDistance(backDistance);
        mainCam.AddVerticalOffset(backOffsetY);

        // faqat flag'ni yoqamiz
        mainCam.SetLookBackMode(true);
    }

    public void MainCam()
    {
        if (mainCam == null) return;

        // masofani va offsetni front holatga qaytaramiz
        mainCam.SetCameraDistance(frontDistance);
        mainCam.AddVerticalOffset(0f); // yoki front uchun alohida offset bo'lsa o'shani

        mainCam.SetLookBackMode(false);

        StartCoroutine(EnableHorseInputDelayed());
    }

    private IEnumerator EnableHorseInputDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        horse.UseCameraInput = true;
    }



    #endregion

    #region Horse Statistics
    public float GetBoostTime()
    {
        return boostTime;
    }
    public void SetBoostTime(float time)
    {
        boostTime = boostTime + time;
        OnOverallBoostTime?.Invoke(boostTime);

    }
    public float GetPenaltyTime()
    {
        return penaltyTime;
    }
    public void SetPenaltyTime(float time)
    {
        penaltyTime = penaltyTime + time;
        OnOverallPenaltyTime?.Invoke(penaltyTime);
    }
    #endregion

}
