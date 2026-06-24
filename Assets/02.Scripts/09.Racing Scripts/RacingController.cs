using Cinemachine;
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
using DG.Tweening;
using UnluckSoftware;
public class RacingController : MonoBehaviour
{
    public static RacingController Instance { get; protected set; }

    public enum RacingType
    {
        None,
        Training,
        Zarafshan,
        Egypt,
        Kansas
    }
    public RacingType mapType = RacingType.None;
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
    [SerializeField] private RacingControllerSelecterUI controllSelector;

    [Header("Walk Zone Prefab")]
   // public GameObject walkZonePrefab;
    public GameObject oneTimeFlashEffect;
    public GameObject walkZoneFlash;
    public GameObject triggerPointProjectile;
    public GameObject explostionVFX;
    [Header("Camera Details")]
    [SerializeField] private ThirdPersonFollowTarget mainCam;
    [SerializeField] private ThirdPersonFollowTarget finishCam;
    [SerializeField] private ThirdPersonFollowTarget sprintCam;
    [SerializeField] private ThirdPersonFollowTarget firstPersonCam;

    [Header("Weather Controller")]
    [SerializeField] private StylizedWeatherController weatherController;
    public enum CameraTypes
    {
        ThirdMain,
        Sprint,
        Final,
        First
    }
    public CameraTypes cameraTypes = CameraTypes.ThirdMain;
    private Coroutine _fpCullCo;

    public static Action<bool> OnFirstPersonCamera;
    public static Action<bool> OnTargetDismessed;

    public float cameraDistance = 4.5f;
    [SerializeField] private float frontDistance = 6f;
    [SerializeField] private float backDistance = 3f;
    [SerializeField] private float backOffsetY = 0.4f;

    [SerializeField] private float firstPersonBackDistance = 0.5f;
    [SerializeField] private float firstPersonOffsetY = 0f;

    [Header("Starting Point Slider Values")]
    [SerializeField] private int defaultSpeedIndex = 5;  // odatiy tezlik
    [SerializeField] private int boostSpeedIndex = 6;    // max tezlik
    [SerializeField] private float boostTimeMultiplier = 4f; // slider * 4 sekund
    private Coroutine boostRoutine;

    private float boostTime;
    private float penaltyTime;

    public static Action<float> OnOverallBoostTime;
    public static Action<float> OnOverallPenaltyTime;

    public static Action<int> OnRacingFinished;
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
    private int _fpLayer;
    private Camera _mainCam;
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
        _fpLayer = LayerMask.NameToLayer("FP_Hide");
        _mainCam = Camera.main; // CinemachineBrain shu kamerada bo‘ladi
    }
    async void Start()
    {
        InitLeaderboardPanelHidden();
       // SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);
        SimplePool.CreatePool(oneTimeFlashEffect, prewarm: 5, maxSize: 8, expandable: true); 
        SimplePool.CreatePool(walkZoneFlash, prewarm: 5, maxSize: 8, expandable: true);
        SimplePool.CreatePool(triggerPointProjectile, prewarm: 10, maxSize:30, expandable:true);
        SimplePool.CreatePool(explostionVFX, prewarm: 10, maxSize: 15, expandable: true);
        await ApplyRandomSkinsToAllAI();
        ////GetSetAnimal(HorseMine.Instance.horseAnimal);
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);

        LoadingPanel(2f);
        await ApplySkyboxByMapType();
        ChangeWeather();
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

        //StartSound();

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
        GameAnalytics();
    }
    private void GameAnalytics()
    {
        string mapName = mapType.ToString();
        GameAnalyticsEvents.RaceStarted(mapName, "racing");
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


    #region Start and Stop Racing
    public void StartRacing()
    {
        if (mapType == RacingType.Training)
        {
            DisableNavmesh();
        }
        else
        {
            EnableNavMesh();
            OnRacingStarted?.Invoke();
        }
        ShowLeaderboardPanel();
        RacingLeaderboard.Instance.StartRace();

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
    public void DisableSpeed()
    {

        for (int i = 0; i < aiRiders.Count; i++)
        {
            aiRiders[i].DisableSpeed();
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
        if (mapType == RacingType.Training)
        {
            StartCoroutine(HorseStopAction(() =>
            {
                UIButtonActions.Instance?.ShowResultTutorial();
            }));
        }
        else
        {
            StartCoroutine(HorseStopAction());
        }

    }
    private IEnumerator HorseStopAction(Action action = null)
    {
        PrepareHorseStop();

        yield return new WaitForEndOfFrame();

        MoveCameraToStartFinalPose();
        HideGameplayUI();

        int playerRank = RacingLeaderboard.Instance.PlayerRank();


        PlayRaceFinishSequence(playerRank);

        yield return new WaitForSeconds(2f);
        horse.StopMoving();

        if (mapType == RacingType.Training)
        {
            action?.Invoke();
            yield break;
        }
        PlayFinalSound();
        OnRacingFinished?.Invoke(playerRank);

        MoveCameraToEndFinalPose();

        action?.Invoke();
    }

    private void PrepareHorseStop()
    {
        cameraTypes = CameraTypes.Final;
        FirstPersonDisable();

        if (boostRoutine != null)
            StopCoroutine(boostRoutine);

        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);

        UIButtonActions.Instance.DisableShootChainOrSprint();
    }

    private void HideGameplayUI()
    {
        HideLeaderboardPanel();
        mobileCanvasPanel.gameObject.SetActive(false);
    }

    private void PlayRaceFinishSequence(int playerRank)
    {
        PlayFinalAnim(playerRank);

        if (winningPanelBG != null)
            winningPanelBG.SetActive(true);

        SoundManager.Instance.StopRoomSmooth();
    }

    private void MoveCameraToStartFinalPose()
    {
        Vector2 pos = GetStartFinalCameraPosition(mapType);
        CameraPostionCheck(pos.x, pos.y);
    }

    private void MoveCameraToEndFinalPose()
    {
        Vector2 pos = GetEndFinalCameraPosition(mapType);
        CameraPostionCheck(pos.x, pos.y);
    }

    private Vector2 GetStartFinalCameraPosition(RacingType type)
    {
        switch (type)
        {
            case RacingType.Zarafshan:
                return new Vector2(-189f, -3f);

            case RacingType.Egypt:
                return new Vector2(10f, -3f);

            case RacingType.Training:
                return new Vector2(3f, -3f);

            case RacingType.None:
                return new Vector2(-10f, -3f);

            default:
                Debug.Log("StartFinalCameraPosition default ga tushdi");
                return new Vector2(10f, -3f);
        }
    }

    private Vector2 GetEndFinalCameraPosition(RacingType type)
    {
        switch (type)
        {
            case RacingType.Zarafshan:
                return new Vector2(-98f, -8f);

            case RacingType.Egypt:
                return new Vector2(10f, -5f);

            case RacingType.None:
                return new Vector2(10f, -5f);
            case RacingType.Kansas:
                return new Vector2(-57f, -8f);

            default:
                return new Vector2(10f, -5f);
        }
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
    public void StopHorseImmideate()
    {
        horse.Always_Forward(false);
        horse.StopMoving();
    }
    public void EnableSpeedAgain()
    {
        OnTargetDismessed?.Invoke(true);
        EnableNavMesh();
        horse.Always_Forward(true);
        horse.Speed_CurrentIndex_Set(5);
    }
    #endregion


    #region Camera Details
   

    private void SetFPCulling(bool isFP)
    {
        if (_mainCam == null) return;

        int mask = _mainCam.cullingMask;

        if (isFP)
        {
            mask &= ~(1 << _fpLayer);   // FP_Hide layerni olib tashla
            OnFirstPersonCamera?.Invoke(true);
        }
        else
        {
            mask |= (1 << _fpLayer);    // qayta ko‘rsat
            OnFirstPersonCamera?.Invoke(false);
        }


        _mainCam.cullingMask = mask;
    }
    public void FirstPersonEnable()
    {
        if (firstPersonCam == null) return;

        firstPersonCam.SetPriority(true);
        mainCam?.SetPriority(false);
        sprintCam?.SetPriority(false);
        finishCam?.SetPriority(false);

        cameraTypes = CameraTypes.First;
        // ✅ oldingi coroutine bo‘lsa to'xtatamiz
        if (_fpCullCo != null) StopCoroutine(_fpCullCo);
        _fpCullCo = StartCoroutine(DelayFPCullOn());

    }
    public void FirstPersonDisable()
    {
        if (firstPersonCam == null) return;

        // ✅ TP ga qaytayotganda darrov ko‘rsatamiz
        if (_fpCullCo != null) StopCoroutine(_fpCullCo);
        _fpCullCo = null;

        SetFPCulling(isFP: false);

        firstPersonCam?.SetPriority(false);
        mainCam.SetPriority(true);

        cameraTypes = CameraTypes.ThirdMain;
    }
    private IEnumerator DelayFPCullOn()
    {
        // Cinemachine linear 0.8s bo‘lsa, 0.85 yaxshi
        yield return new WaitForSecondsRealtime(0.85f);
        // FP holatda qolgan bo'lsa only
        if (cameraTypes == CameraTypes.First)
            SetFPCulling(isFP: true);

        _fpCullCo = null;
    }
    private void SprintCameraEnable()
    {
        if(cameraTypes==CameraTypes.First) 
            return;
        cameraTypes = CameraTypes.Sprint;
        sprintCam.SetPriority(true);
        
    }
    private void SprintCameraDisable() {
        sprintCam.SetPriority(false);
        if(cameraTypes!=CameraTypes.Sprint)
            return;
        cameraTypes = CameraTypes.ThirdMain;
    }
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
        if (cameraTypes == CameraTypes.ThirdMain)
        {
            mainCam.SetCameraDistance(backDistance);
            mainCam.AddVerticalOffset(backOffsetY);

            // faqat flag'ni yoqamiz
            mainCam.SetLookBackMode(true);
        }
        else if(cameraTypes == CameraTypes.First)
        {
            firstPersonCam.SetCameraDistance(firstPersonBackDistance);
            firstPersonCam.AddVerticalOffset(firstPersonOffsetY);

            // faqat flag'ni yoqamiz
            firstPersonCam.SetLookBackMode(true);
        }

    }

    public void MainCam()
    {
        if (mainCam == null) return;

        // masofani va offsetni front holatga qaytaramiz
        if (cameraTypes == CameraTypes.ThirdMain)
        {
            mainCam.SetCameraDistance(frontDistance);
            mainCam.AddVerticalOffset(0f); // yoki front uchun alohida offset bo'lsa o'shani

            mainCam.SetLookBackMode(false);
        }
        else if (cameraTypes == CameraTypes.First)
        {
            firstPersonCam.SetCameraDistance(-0.5f); //-0.5
            firstPersonCam.AddVerticalOffset(-0.1f); // yoki front uchun alohida offset bo'lsa o'shani

            firstPersonCam.SetLookBackMode(false);
        }

        StartCoroutine(EnableHorseInputDelayed());
    }

    private IEnumerator EnableHorseInputDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        horse.UseCameraInput = true;
    }

    private void HorseSprint()
    {
        if (horse != null)
        {
            SprintCameraEnable();
        }
    }

    private void HorseDefaultSpeed()
    {
        if (horse != null)
        {
            SprintCameraDisable();
            Debug.Log("Camera back");
        }
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
        if(controllSelector != null) { controllSelector.gameObject.SetActive(true); }
        //if (sliderObject != null) sliderObject.SetActive(true);
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

    #region Skybox
    private async Task ApplySkyboxByMapType()
    {
        string skyboxAddress = GetSkyboxAddress(mapType);

        if (string.IsNullOrEmpty(skyboxAddress))
            return;

        if (AddressablesService.Instance == null)
        {
            Debug.LogWarning("RacingController: AddressablesService is missing. Skybox cannot be loaded.");
            return;
        }

        Material skyboxMaterial = await AddressablesService.Instance.LoadAssetAsync<Material>(skyboxAddress);

        if (skyboxMaterial == null)
        {
            Debug.LogWarning("RacingController: Skybox material failed to load: " + skyboxAddress);
            return;
        }

        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }

    private string GetSkyboxAddress(RacingType racingType)
    {
        switch (racingType)
        {
            case RacingType.Zarafshan:
                return Constants.SkyBoxes.ZarafshanSkybox;

            case RacingType.Egypt:
                return Constants.SkyBoxes.EgyptSkybox;

            case RacingType.Kansas:
                return Constants.SkyBoxes.KansasSkybox;

            default:
                return null;
        }
    }
    #endregion

    #region Horse Power/Cooling/Stamina
    private void AddFoods(float powerPercent, float coolingPercent, float staminaPercent)
    {
        HorseConditionStatsService.AddFood(powerPercent, coolingPercent, staminaPercent);
    }
    #endregion

    #region Weather
    public void ChangeWeather()
    {
        if(mapType== RacingType.Zarafshan)
        {
            weatherController.ChangeWeather("Lightning");
        }
        else if(mapType == RacingType.Egypt)
        {
            weatherController.ChangeWeather("Dust");
        }
    }
    #endregion
}
