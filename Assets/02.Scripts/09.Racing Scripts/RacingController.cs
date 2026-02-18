using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.HAP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
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
    [SerializeField] private GameObject sliderObject;

    [Header("Walk Zone Prefab")]
    public GameObject walkZonePrefab;
    public GameObject oneTimeFlashEffect;
    public GameObject walkZoneFlash;
    public GameObject triggerPointProjectile;
    public GameObject explostionVFX;
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

    [SerializeField] private GameObject winningPanelBG;
    #region Racin Agents
    [Header("Agents Registry")]
    [SerializeField] private List<RacingAgent> allAgents = new List<RacingAgent>(16);

    // Duplicate bo'lmasin + O(1) check
    private readonly HashSet<RacingAgent> _agentSet = new HashSet<RacingAgent>();
    public IReadOnlyList<RacingAgent> AllAgents => allAgents;
    public bool IsRaceOver { get; private set; }
    public float raceEndTime { get; private set; }
    public float RaceEndTime => raceEndTime;
    #endregion

    public GameOverTypes gameOverType = GameOverTypes.None;


    #region Race Start and End
    public bool HasStarted { get; private set; }
    public bool HasFinished { get; private set; }
    private bool _isPaused;

    private float _accumulated;
    private float _runStartTime;

    public float ElapsedTime
    {
        get
        {
            if (!HasStarted) return 0f;
            if (HasFinished) return _accumulated;
            if (_isPaused) return _accumulated;
            return _accumulated + (Time.time - _runStartTime);
        }
    }

    #endregion
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
    async void Start()
    {
        InitLeaderboardPanelHidden();
        SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);
        SimplePool.CreatePool(oneTimeFlashEffect, prewarm: 5, maxSize: 8, expandable: true);
        SimplePool.CreatePool(walkZoneFlash, prewarm: 5, maxSize: 8, expandable: true);
        SimplePool.CreatePool(triggerPointProjectile, prewarm: 10, maxSize:30, expandable:true);
        SimplePool.CreatePool(explostionVFX, prewarm: 10, maxSize: 15, expandable: true);
        await ApplyRandomSkinsToAllAI();
        ////GetSetAnimal(HorseMine.Instance.horseAnimal);
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);

        LoadingPanel(2f);
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
        //RacingResultPage.OnGetRiderRank += PlayFinalAnim;
        UILookBackButton.OnCameraPressedState += CameraBackState;
        FoodInfo.OnFoodAddToHorse += AddFoods;

        StartSound();

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
       // RacingResultPage.OnGetRiderRank -= PlayFinalAnim;
        UILookBackButton.OnCameraPressedState -= CameraBackState;
        FoodInfo.OnFoodAddToHorse -= AddFoods;
        riderAnimal = null;
        horse = null;
        ClearAgents();
    }
    #endregion

    #region Start and End race
    public void BeginRace()
    {
        if (HasStarted) return;
        HasStarted = true;
        HasFinished = false;
        _accumulated = 0f;
        _runStartTime = Time.time;
        _isPaused = false;
    }

    public void FinishRace()
    {
        if (!HasStarted || HasFinished) return;
        if (!_isPaused) _accumulated += (Time.time - _runStartTime);
        HasFinished = true;
    }

    public void PauseRaceTime()
    {
        if (!HasStarted || HasFinished || _isPaused) return;
        _accumulated += (Time.time - _runStartTime);
        _isPaused = true;
    }

    public void ResumeRaceTime()
    {
        if (!HasStarted || HasFinished || !_isPaused) return;
        _runStartTime = Time.time;
        _isPaused = false;
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
    private async void StartSound()
    {
        var clip  = await AddressablesService.Instance.LoadAssetAsync<AudioClip>(Constants.RoomSound.RacingSound);
        if (clip != null)
        {
            SoundManager.Instance.PlayRoom(clip);
        }
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
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);
        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);
        UIButtonActions.Instance.DisableShootChainOrSprint();
        yield return new WaitForEndOfFrame();
        if (SceneLoadManager.Instance != null)
        {
            float x;
            float y;
            switch (SceneLoadManager.Instance.CurrentSceneType)
            {
                case SceneLoadManager.SceneType.SecondRacing:
                    x = -189f;
                    y = -3f;
                    break;
                default:
                    x = 10f;
                    y = -3f;
                    break;
            }
            CameraPostionCheck(x, y);           
        }
        else
        {
            CameraPostionCheck(-10f, -3f);
        }
            
        //CameraPostionCheck(-189f, -3f); changed here
        HideLeaderboardPanel();
        mobileCanvasPanel.gameObject.SetActive(false);
        int playerRank = RacingLeaderboard.Instance.PlayerRank();
        PlayFinalAnim(playerRank);
        if (winningPanelBG != null) { winningPanelBG.SetActive(true); }
        SoundManager.Instance.StopRoomSmooth();
        yield return new WaitForSeconds(2f);
        PlayFinalSound();
        OnRacingFinished?.Invoke();
        horse.StopMoving();

        if (SceneLoadManager.Instance != null)
        {
            float x;
            float y;
            switch (SceneLoadManager.Instance.CurrentSceneType)
            {
                case SceneLoadManager.SceneType.SecondRacing:
                    x = -98f;
                    y = -8f;
                    break;
                default:
                    x = 10f;
                    y = -5f;
                    break;
            }
            CameraPostionCheck(x, y);
        }
        else
        {
            CameraPostionCheck(10, -5);
        }

        



        //yield return new WaitForSeconds(1f);
       // horse.StopMoving();

        action?.Invoke();
    }
    private void StopMyHorse()
    {
        if (boostRoutine != null)
            StopCoroutine(boostRoutine);
        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);
        StartCoroutine(DelayStopHorse());
    }
    private IEnumerator DelayStopHorse()
    {
        yield return new WaitForSeconds(2);
        horse.StopMoving();
    }
    private void HorseSprint()
    {
        if (horse != null) { 
            SprintCameraEnable();
        }
    }

    private void HorseDefaultSpeed()
    {
        if (horse != null) { 
            SprintCameraDisable();
            Debug.Log("Camera back");
        }
    }
    #endregion
 

    #region Camera Details

    private void SprintCameraEnable()
    {
        sprintCam.SetPriority(true);
    }
    private void SprintCameraDisable() { sprintCam.SetPriority(false); }
    public void CameraPostionCheck(float yMove, float xMove)
    {
        horse.UseCameraInput = false;
        finishCam.SetFinishViewSmooth(
            cameraDistance,  // masofa
            yMove,             // yaw -> Inspector’da 18 bo'lishi uchun
            xMove,             // pitch (istaganingcha o'zgartirishing mumkin)
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
        Debug.Log("[EndTime BoosterContainer]" + boostTime);
    }
    public float GetPenaltyTime()
    {
        return penaltyTime;
    }
    public void SetPenaltyTime(float time)
    {
        penaltyTime = penaltyTime + time;
    }
    #endregion

    #region AI Random skins
    private async Task ApplyRandomSkinsToAllAI()
    {
        for (int i = 0; i < aiRiders.Count; i++)
        {
            var rider = aiRiders[i];
            if (rider == null) continue;

            var rs = rider.randomSkin; // yoki rider.randomSkin
            if (rs == null) continue;

            await rs.ApplyRandomAsync();
        }
    }
    #endregion

    #region Racing Agents
    // Call this when race ends
    public void SetRaceOver(bool value) => IsRaceOver = value;

    public void RegisterAgent(RacingAgent a)
    {
        if (a == null) return;

        // old null'larni tozalab turamiz (vaqti-vaqti bilan)
        // (xohlasang alohida methodda ham qilamiz)
        if (_agentSet.Add(a))
        {
            allAgents.Add(a);
        }
    }

    public void RemoveAgent(RacingAgent a)
    {
        if (a == null) return;

        if (_agentSet.Remove(a))
        {
            // List.Remove O(n) lekin agent count odatda kichik (10-30)
            allAgents.Remove(a);
        }
    }

    public void ClearAgents()
    {
        allAgents.Clear();
        _agentSet.Clear();
    }
    public float EndBoostTime()
    {
        return boostTime;
    }
    public void PlayerFailedSpecialReach(RacingAgent playerAgent, MonoBehaviour triggerPoint)
    {
        if (IsRaceOver) return;

        // shu yerda SENING game over / lose / stop race logikang
        // Misol:
        // GameOverPage();
        // StopRace();

        playerAgent.EndRace();
        EndRacing();
        GamOverType(GameOverTypes.ByTime);
        UIButtonActions.Instance.ShowGameOver();

    }
    public void EndRacing()
    {
        FinishRace();
        IsRaceOver = true;
        StopMyHorse();
        mobileCanvasPanel.gameObject.SetActive(false);       
        leaderboard.gameObject.SetActive(false);
    }
    public void EndRacingCollision()
    {
        GamOverType(GameOverTypes.ObstacleHit);
        EndRacing();
    }

    public void GamOverType(GameOverTypes types)
    {
        gameOverType = types;
    }
    #endregion

    #region Game Start Slider
    public void LoadingPanel(float time)
    {
        StartCoroutine(LoadingPanelDisabler(time));
    }

    private IEnumerator LoadingPanelDisabler(float time)
    {
        yield return new WaitForSeconds(time);
        UIOverlayRoot.I?.HideCurrentPanel();
        if (sliderObject != null) sliderObject.SetActive(true);
        Finalsound();
    }
    private async void Finalsound()
    {
        await AddressablesService.Instance.PreloadDependenciesAsync("Makarena");
    }
    public async void PlayFinalSound()
    {
        var audioClip = await AddressablesService.Instance.LoadAssetAsync<AudioClip>("Makarena");
        if (audioClip != null)
            SoundEffect(audioClip);
   
    }
    public void SoundEffect(AudioClip clip)
    {
        SoundManager.Instance?.PlayRoom(clip);
    }
    #endregion

    #region Horse Power/Cooling/Stamina
    private void AddFoods(float powerPercent, float coolingPercent, float staminaPercent)
    {
        float foolPercentage = 100f;
        // 1) PlayerPrefs dagi qiymatlarni olamiz
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power, foolPercentage);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling, foolPercentage);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina, foolPercentage);

        // 2) Bufflarni qo‘shamiz
        currentPower = Mathf.Clamp(currentPower + powerPercent, 0f, 100f);
        currentCooling = Mathf.Clamp(currentCooling + coolingPercent, 0f, 100f);
        currentStamina = Mathf.Clamp(currentStamina + staminaPercent, 0f, 100f);

        // 3) Yangi qiymatlarni saqlaymiz
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, currentPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, currentCooling);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, currentStamina);
    }
    #endregion

}
