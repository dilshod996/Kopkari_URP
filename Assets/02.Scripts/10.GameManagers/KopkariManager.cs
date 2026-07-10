using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;
using Michsky.UI.ModernUIPack;

public class KopkariManager : MonoBehaviour
{
    [Header("-------------HorseAnimal---------------")]
    public MAnimal horseAnimal;
    public MAnimal playerAnim;
    public PlayerDataManager playerDataManager;
    public ModalWindowManager modalWindowPopup;


    [Header("Main Time")]
    public TMP_Text timeText;
    public float mainTime = 0f;
    private float totalMainTime;

    [Header("Lamp Realated")]
    public string LambOwner;
    [SerializeField] private List<Transform> lambPositions;
    [SerializeField] private GameObject myLamb;
    [SerializeField] private GameObject startVFX;
    [SerializeField] private GameObject targetPos;
    [SerializeField] private GameObject targetVFX;
    [SerializeField] private GameObject finalFlag;

    [Header("Gameplay")]
    public Pickable pickableObj;

    public GameObject currentGoatOwner;

    [SerializeField] private bool isCatched;
    public float lampCatchTime = 30f;
    public bool IsCatched { get => isCatched; set => isCatched = value; }

    public int catchCounter = 0;

    public enum RoomState
    {
        None,
        Warmup,
        GameStarted,
        WaterDropped,
        TimeFinished,
        HorseStamenaFinished,
        RiderStamenaFinished,
        LambReachTarget,
        GameFinished,
        PlayerEliminated,
        Won
    }
    public RoomState roomState = RoomState.None;

    [Header("Pooled VFX")]
    public VFXPool pool;

    public enum PlayerCondition
    {
        Start,
        GettingTarget,
        GotTarget,
        NearTarget,
        AwayTarget,
        DroppedTarget,
        TakenTargetOthers,
        WinnerSession,
        LoserSession,
        SpeedUp,
        StaminaLimit,
        HealthLimit,
        CatchLimit,
        WaterEntered,
        EagleWatching,
        MapLimit,
        MainTimeOver,
        None
    }
    public PlayerCondition currentCondition = PlayerCondition.None;

    #region Camera Details
    [SerializeField] private ThirdPersonFollowTarget mainCam;
    [SerializeField] private ThirdPersonFollowTarget sprintCam;
    [SerializeField] private float frontDistance = 6f;
    [SerializeField] private float backDistance = 3f;
    [SerializeField] private float backOffsetY = 0.4f;
    [SerializeField] private float startupCameraYaw = -46f;
    [SerializeField] private float startupCameraPitch = 0f;
    #endregion

    #region Horse and Player Data
    [SerializeField] private int defaultSpeedIndex = 5;
    [SerializeField] private int boostSpeedIndex = 6;
    #endregion

    #region Events
    public static Action<bool> OnGameStartFinishState;
    public static Action OnGameStarted;
    public static Action OnGameEnded;
    public static Action<float> OnGoatPickedTime;
    public static Action OnResetTarget;
    public static Action<bool> OnGoatPicked;
    public static Action OnTimeFinished;
    public static Action OnSceneReady;
    public static bool IsSceneReady { get; private set; }
    #endregion

    [Header("Checkpoints")]
    [SerializeField] private List<CheckpointTrigger> checkpoints = new List<CheckpointTrigger>();
    private int passedCheckpointCount = 0;

    [Header("Local Player")]
    [SerializeField] private GameObject LocalRiderRoot;
    private Coroutine pickUpTimerCoroutine;

    [Header("Room Resources")]
    public GameObject walkZonePrefab;
    public GameObject oneTimeGetEffect;
    public GameObject walkZoneFlash;

    [Header("Player Start Point")]
    public Transform startTarget;
    public float warmUpTime;
    private bool playerReachedStart = false;

    public static Transform CurrentStartPoint { get; private set; }
    public static float CurrentWarmupTime { get; private set; }
    public static Action<Transform, float> OnStartPoint;
    public static Action<Transform> OnHorseTransform;

    [Header("Popup Data")]
    public UISpeechBuble speechBubble;

    private int UserId;
    private bool roundEnded = false;
    private bool droppedReported = false;
    private bool timeFinishedHandled = false;
    // ✅ Eski BaseManager.Instance o‘rniga shu ishlaydi
    public static KopkariManager Instance { get; private set; }

    private bool poolsCreated = false;
    private bool sceneReadySignaled = false;
    private Coroutine sceneReadyRoutine;

    //Horse Statistics
    private float webSnareDamageTime;
    private float boostTime;
    public GameOverTypes gameOverTypes = GameOverTypes.None;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        IsSceneReady = false;
        sceneReadySignaled = false;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        if (modalWindowPopup != null)
            modalWindowPopup.onConfirm.AddListener(MoveLobby);
    }

    private void Start()
    {
        if (pickableObj != null)
        {
            pickableObj.OnPicked.RemoveListener(OnUloqPicked);
            pickableObj.OnPicked.AddListener(OnUloqPicked);
        }

        // ✅ Pool create faqat bir marta
        CreatePoolsOnce();

        RegisterStartPoint(startTarget, warmUpTime);
        DisableMainGameObjects();
        if(gameOverTypes != GameOverTypes.None)
        {
            gameOverTypes = GameOverTypes.None;
        }
    }

    private void Update()
    {
        switch (roomState)
        {
            case RoomState.Warmup:
                WarmUpTick();
                break;

            case RoomState.GameStarted:
                MainGameTimeTick();
                break;
            case RoomState.TimeFinished:
                GameOverAction();
                break;
            case RoomState.GameFinished:
                break;
        }
    }

    private void OnEnable()
    {
        HorseMine.OnReachedStartTarget += PlayerReachPoint;

        OnGameStartFinishState += GameStartedAction;

        KopkariMainUI.OnSprintStart += HorseSprint;
        KopkariMainUI.OnSprintEnd += HorseDefaultSpeed;

        BoostersContainer.OnSprintEffectStart += SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd += SprintCameraDisable;

        PlayerDataManager.OnLocalPlayerObject += RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse += RegisterPlayerAndHorse;

        // Kopkari scene flow
        KopkariMainUI.OnEverythingReadyStart += StartGame;
        UILookBackButton.OnCameraPressedState += CameraBackState;
        BoostersContainer.OnBoostTime += SetBoostTime;
        BoostersContainer.OnPenaltyTime += SetPenaltyTime;
    }

    private void OnDisable()
    {
        HorseMine.OnReachedStartTarget -= PlayerReachPoint;

        OnGameStartFinishState -= GameStartedAction;

        KopkariMainUI.OnSprintStart -= HorseSprint;
        KopkariMainUI.OnSprintEnd -= HorseDefaultSpeed;

        BoostersContainer.OnSprintEffectStart -= SprintCameraEnable;
        BoostersContainer.OnSprintEffectEnd -= SprintCameraDisable;

        PlayerDataManager.OnLocalPlayerObject -= RegisterLocalRider;
        PlayerDataManager.OnRiderAndHorse -= RegisterPlayerAndHorse;

        // Kopkari scene flow
        KopkariMainUI.OnEverythingReadyStart -= StartGame;
        UILookBackButton.OnCameraPressedState -= CameraBackState;
        BoostersContainer.OnPenaltyTime -= SetPenaltyTime;
        BoostersContainer.OnBoostTime -= SetBoostTime;
    }

    private void OnDestroy()
    {
        if (modalWindowPopup != null)
            modalWindowPopup.onConfirm.RemoveListener(MoveLobby);

        if (Instance == this) Instance = null;
        IsSceneReady = false;
        SimplePool.ClearAll();
    }
    #region Warm Up
    private void WarmUpTick()
    {
        if (playerReachedStart)
        {
            roomState = RoomState.GameStarted;
            return;
        }

        if (warmUpTime > 0f)
        {
            warmUpTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(warmUpTime / 60);
            int seconds = Mathf.FloorToInt(warmUpTime % 60);

            if (timeText != null)
            {
                timeText.SetText($"{minutes:00}:{seconds:00}");
                if (warmUpTime <= 3f) timeText.color = Color.red;
            }
        }
        else
        {
            HandlePlayerNotReachedInTime();
        }
    }
    private void HandlePlayerReachedStart()
    {
        playerReachedStart = true;
        EnableULoq();
        StartMainGame();

        if (timeText != null) timeText.color = Color.white;
    }

    private void PlayerReachPoint()
    {
        StartCoroutine(DelayReachStart());

        if (horseAnimal != null)
            OnHorseTransform?.Invoke(horseAnimal.transform);
    }

    private IEnumerator DelayReachStart()
    {
        speechBubble?.ShowPopup(LanguageManager.Instance?.GetText(508)); //Are you ready!!!
        DisableStartPoint();

        yield return new WaitForSeconds(2f);

        speechBubble?.ShowPopup(LanguageManager.Instance?.GetText(508)); //LET'S GOOO!!!
        HandlePlayerReachedStart();
    }

    private void HandlePlayerNotReachedInTime()
    {
        roomState = RoomState.PlayerEliminated;
        if (timeText != null) timeText.SetText("DQ");
        gameOverTypes = GameOverTypes.KopkariStartFailed;
        KopkariMainUI.Instance.GameOverShow();
        // mana shu yerda mobile ui di yopib game over page di chiqarish kerak
        // Bu yerda xohlasang control lock / popup / retry qo‘shasan
    }
    private void StartMainGame()
    {
        totalMainTime = mainTime;
        roomState = RoomState.GameStarted;
        webSnareDamageTime = 0;
    }
    #endregion

    #region Game Starting
    private void CreatePoolsOnce()
    {
        if (poolsCreated) return;
        poolsCreated = true;

        if (walkZonePrefab != null)
            SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);

        if (oneTimeGetEffect != null)
            SimplePool.CreatePool(oneTimeGetEffect, prewarm: 5, maxSize: 40, expandable: true);

        if (walkZoneFlash != null)
            SimplePool.CreatePool(walkZoneFlash, prewarm: 5, maxSize: 40, expandable: true);
    }
    private void MainGameTimeTick()
    {

        if (mainTime > 0f && !timeFinishedHandled)
        {
            mainTime -= Time.deltaTime;

            if (mainTime < 0f)
                mainTime = 0f;

            int minutes = Mathf.FloorToInt(mainTime / 60);
            int seconds = Mathf.FloorToInt(mainTime % 60);

            if (timeText != null)
            {
                timeText.SetText($"{minutes:00}:{seconds:00}");
                if (mainTime <= 3f) timeText.color = Color.red;
            }
        }
        else
        {
            if (timeText != null) timeText.SetText("00:00");
            roomState = RoomState.TimeFinished;
            gameOverTypes = GameOverTypes.ByTime;
        }
    }
    public float GetUsedMainTime()
    {
        return totalMainTime - mainTime;
    }

    #endregion
    public void DisableMainGameObjects()
    {
        myLamb?.SetActive(false);
        startVFX?.SetActive(false);
        targetPos?.SetActive(false);
        targetVFX?.SetActive(false);
    }

    public void EnableULoq()
    {
        myLamb?.SetActive(true);
        startVFX?.SetActive(true);
    }

    private void RegisterPlayerAndHorse(MAnimal horse, MAnimal player)
    {
        horseAnimal = horse;
        playerAnim = player;

        if (sceneReadyRoutine != null)
            StopCoroutine(sceneReadyRoutine);

        sceneReadyRoutine = StartCoroutine(CompleteSceneReadyWhenCameraSettled());
    }

    private IEnumerator CompleteSceneReadyWhenCameraSettled()
    {
        if (sceneReadySignaled) yield break;

        yield return null;
        yield return new WaitForEndOfFrame();

        if (mainCam != null)
        {
            if ((mainCam.Target == null || mainCam.Target.Value == null) && horseAnimal != null)
                mainCam.SetTarget(horseAnimal.transform);

            mainCam.SetLookBackMode(false);

            if (mainCam.Target != null && mainCam.Target.Value != null)
            {
                mainCam.TargetTeleport(false);
                mainCam._cinemachineTargetYaw = startupCameraYaw;
                mainCam._cinemachineTargetPitch = startupCameraPitch;
            }
        }

        for (int i = 0; i < 3; i++)
            yield return null;

        var brain = mainCam != null ? mainCam.Brain : null;
        float waitUntil = Time.unscaledTime + 2f;

        while (brain != null && brain.IsBlending && Time.unscaledTime < waitUntil)
            yield return null;

        yield return new WaitForEndOfFrame();

        sceneReadySignaled = true;
        IsSceneReady = true;
        SceneLoadManager.Instance?.SetAssetInstantiationFinished(true);
        OnSceneReady?.Invoke();
    }

    public void RegisterStartPoint(Transform point, float time)
    {
        CurrentStartPoint = point;
        CurrentWarmupTime = time;
        OnStartPoint?.Invoke(point, time);
    }

    private void DisableStartPoint()
    {
        if (startTarget != null)
            startTarget.gameObject.SetActive(false);
    }

    private void StartPosState(bool state)
    {
        startVFX?.SetActive(state);
    }

    public void FinalPosState(bool state)
    {
        if (state && targetPos != null && targetPos.activeSelf) return;

        targetPos?.SetActive(state);
        targetVFX?.SetActive(state);
        if (finalFlag != null) finalFlag.SetActive(state);
    }



    #region Pick Up Uloq
    public void StartPickUpTime()
    {
        if (pickUpTimerCoroutine != null)
        {
            StopCoroutine(pickUpTimerCoroutine);
            pickUpTimerCoroutine = null;
        }

        IsCatched = true;
        lampCatchTime = 30f;

        OnGoatPickedTime?.Invoke(lampCatchTime);
        pickUpTimerCoroutine = StartCoroutine(PickUpTimerRoutine());
    }

    private IEnumerator PickUpTimerRoutine()
    {
        float t = lampCatchTime;

        while (t > 0f && IsCatched)
        {
            yield return new WaitForSeconds(1f);
            t--;
            OnGoatPickedTime?.Invoke(t);
        }

        if (t <= 0f && IsCatched)
        {
            TriggerEvent();
        }

        StopPickUpTime();
    }

    private void OnUloqPicked(GameObject pickerObj)
    {
        NotifyGoatOwner(pickerObj.transform.root.gameObject, true);
        StartPosState(false);
        StartCoroutine(NotifyRoom());
    }

    private IEnumerator NotifyRoom()
    {
        yield return new WaitForSeconds(0.5f);

        var kopkariResult = KopkariResultsManager.Instance;
        string pickerName = PlayerPrefs.GetString(Constants.Player.UsernameKey);

        if (kopkariResult != null && kopkariResult.UloqOwner == pickerName)
        {
            speechBubble?.ShowPopup(LanguageManager.Instance?.GetText(510)); // Lets go!!! Faster Polvon"
            TriggerPointNotFinished();
        }
        else
        {
            if (kopkariResult != null)
            {
                string ownerName = $"<color=#FFD700>{kopkariResult.UloqOwner} Polvon</color>";
                string message = string.Format(LanguageManager.Instance.GetText(511), ownerName);
                speechBubble?.ShowPopup(message);
            }
               // speechBubble?.ShowPopup($"<color=#FFD700>{kopkariResult.UloqOwner} Polvon</color> has taken the Ulak.");
        }
    }

    public void NotifyGoatOwner(GameObject ownerRoot, bool hasGoat)
    {
        bool isLocalPlayer = (LocalRiderRoot != null && ownerRoot == LocalRiderRoot);

        if (hasGoat)
        {
            currentGoatOwner = ownerRoot;

            if (isLocalPlayer)
            {
                OnGoatPicked?.Invoke(true);
                IsCatched = true;
                StartPickUpTime();

                KopkariResultsManager.Instance?.OnLambPicked(UserId);
            }
            else
            {
                if (IsCatched)
                    StopPickUpTime();

                IsCatched = false;
            }
        }
        else
        {
            if (currentGoatOwner == ownerRoot)
            {
                if (isLocalPlayer)
                    StopPickUpTime();

                currentGoatOwner = null;
                IsCatched = false;
            }
        }
    }
    #endregion

    #region Drop Uloq
    public void TriggerEvent()
    {
        IsCatched = false;
        currentGoatOwner = null;
        playerDataManager?.DropObject();
    }

    public void StopPickUpTime()
    {
        IsCatched = false;

        if (pickUpTimerCoroutine != null)
        {
            StopCoroutine(pickUpTimerCoroutine);
            pickUpTimerCoroutine = null;
        }

        OnGoatPickedTime?.Invoke(0f);
        OnGoatPicked?.Invoke(false);

        if (!droppedReported)
        {
            droppedReported = true;
            KopkariResultsManager.Instance?.OnLambDropped(UserId);
        }
    }
    #endregion

    public void WinOrLosePage()
    {
        Debug.Log("KopkariManager: WinOrLosePage (implement qilinadi)");
        // bu yerda sen final UI/Result page ochasan
    }

    public void HandleWin()
    {
        currentCondition = PlayerCondition.WinnerSession;
        Debug.Log("KopkariManager: HandleWin");
    }

    public void HandleLose()
    {
        currentCondition = PlayerCondition.LoserSession;
        Debug.Log("KopkariManager: HandleLose");
    }

    public void GameStartedAction(bool state)
    {
        if (state) roomState = RoomState.Warmup;
    }

    public void StartGame()
    {
        droppedReported = false;
        roundEnded = false;

        passedCheckpointCount = 0;
        foreach (var cp in checkpoints) cp.ResetPassed();

        OnResetTarget?.Invoke();
        OnGameStartFinishState?.Invoke(true);
        OnGameStarted?.Invoke();

        speechBubble?.ShowPopup(LanguageManager.Instance?.GetText(507));//You have 30 seconds to reach your position!
        UserId = PlayerPrefs.GetInt(Constants.Player.Userid, 0);
    }

    public void ContinueGame()
    {
        // kerak bo‘lsa implement qilasan
    }

    public void ContinueGameChanger(Transform lambPos, Transform targetTr)
    {
        if (myLamb != null && lambPos != null)
            myLamb.transform.SetPositionAndRotation(lambPos.position, lambPos.rotation);

        if (startVFX != null && lambPos != null)
            startVFX.transform.position = lambPos.position;

        if (targetPos != null && targetTr != null)
            targetPos.transform.SetPositionAndRotation(targetTr.position, targetTr.rotation);

        if (targetVFX != null && targetTr != null)
            targetVFX.transform.position = targetTr.position;
    }
    /// <summary>
    /// Finish is here
    /// </summary>
    public void MarkPlayerReachedTarget()
    {
        if (roundEnded) return;
        if (roomState != RoomState.GameStarted) return;

        if (!IsCatched) return;
        if (currentGoatOwner == null) return;
        if (LocalRiderRoot != null && currentGoatOwner != LocalRiderRoot) return;

        KopkariMainUI.Instance?.UpdateSlider();

        roundEnded = true;

        TriggerEvent();
        roomState = RoomState.GameFinished;
        StopPickUpTime();

        if (pickableObj != null)
            pickableObj.gameObject.SetActive(false);

        OnGameStartFinishState?.Invoke(false);
        WinOrLosePage();
    }

    public void RegisterLocalRider(GameObject riderRoot)
    {
        LocalRiderRoot = riderRoot;
    }

    #region Camera Section
    public void CameraBackState(bool state)
    {
        if (state) LookBack();
        else MainCam();
    }

    public void LookBack()
    {
        if (mainCam == null) return;
        if (horseAnimal != null) horseAnimal.UseCameraInput = false;

        mainCam.SetCameraDistance(backDistance);
        mainCam.AddVerticalOffset(backOffsetY);
        mainCam.SetLookBackMode(true);
    }

    public void MainCam()
    {
        if (mainCam == null) return;

        mainCam.SetCameraDistance(frontDistance);
        mainCam.AddVerticalOffset(0f);
        mainCam.SetLookBackMode(false);

        StartCoroutine(EnableHorseInputDelayed());
    }

    private IEnumerator EnableHorseInputDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        if (horseAnimal != null) horseAnimal.UseCameraInput = true;
    }

    private void SprintCameraEnable()
    {
        if (sprintCam != null) sprintCam.SetPriority(true);
    }

    private void SprintCameraDisable()
    {
        if (sprintCam != null) sprintCam.SetPriority(false);
    }
    #endregion

    #region CheckPoints
    public void OnCheckpointReached(CheckpointTrigger checkpoint, GameObject riderObj)
    {
        if (roomState != RoomState.GameStarted) return;

        if (!IsCatched || currentGoatOwner == null)
        {
            Debug.Log("[Checkpoint] Uloqsiz o‘tildi – hisoblanmadi");
            return;
        }

        if (riderObj == null) return;
        if (riderObj.transform.root.gameObject != currentGoatOwner)
        {
            Debug.Log("[Checkpoint] Bu riderda uloq yo‘q – hisoblanmadi");
            return;
        }

        if (checkpoint == null) return;

        if (checkpoint.IsPassedWithGoat)
        {
            Debug.Log("[Checkpoint] Bu checkpoint oldin ham uloq bilan o‘tilgan");
            return;
        }

        checkpoint.MarkPassedWithGoat();
        passedCheckpointCount++;

        KopkariMainUI.Instance?.UpdateSlider();
        KopkariResultsManager.Instance?.OnTriggerPoint(UserId);

        Debug.Log($"[Checkpoint] {riderObj.name} uloq bilan checkpoint o'tdi. Jami: {passedCheckpointCount}/{checkpoints.Count}");

        if (passedCheckpointCount >= checkpoints.Count && checkpoints.Count > 0)
            OnAllCheckpointsCompleted();
    }

    private void TriggerPointNotFinished()
    {
        if (passedCheckpointCount != checkpoints.Count)
            FinalPosState(false);
    }

    public void OnAllCheckpointsCompleted()
    {
        Debug.Log("[Checkpoint] 🔥 Barcha checkpointlar uloq bilan o‘tilgan!");
        FinalPosState(true);
    }
    #endregion

    #region Horse Speed
    private void HorseSprint()
    {
        if (horseAnimal == null) return;
        horseAnimal.Speed_CurrentIndex_Set(boostSpeedIndex);
        SprintCameraEnable();
    }

    private void HorseDefaultSpeed()
    {
        if (horseAnimal == null) return;
        horseAnimal.Speed_CurrentIndex_Set(defaultSpeedIndex);
        SprintCameraDisable();
    }
    #endregion

    #region Game Over Actions
    private void GameOverAction()
    {
        if (timeFinishedHandled) return;
        timeFinishedHandled = true;

        OnTimeFinished?.Invoke();     // NPC, boshqa riderlar shu eventni ushlaydi

        //HandleLose();
    }
    public void OffsideAction()
    {
        gameOverTypes = GameOverTypes.Offside;
        GameOverAction();
    }
    #endregion

    #region Horse Statistics(Damages)

    public void SetPenaltyTime(float time)
    {
        webSnareDamageTime = webSnareDamageTime + time;
    }
    public float GetWebSnareDamageTime()
    {
        return webSnareDamageTime;
    }
    public float GetBoostTime()
    {
        return boostTime;
    }
    public void SetBoostTime(float time)
    {
        boostTime = boostTime + time;
    }
    #endregion
    // Scene navigation hooks
    public void MoveLobby()
    {
        SceneLoadManager.Instance?.LoadScene(SceneLoadManager.SceneType.Home);
    }

    public void BackMessage()
    {
        if (modalWindowPopup == null) return;

        string title = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(280) : string.Empty;
        string desc = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(281) : string.Empty;
        string confirm = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(1) : string.Empty;
        string cancel = LanguageManager.Instance != null ? LanguageManager.Instance.GetText(2) : string.Empty;

        modalWindowPopup.UpdateUICustomWithButtons(title, desc, confirm, cancel);
    }

    public void SpeedShaderActive(bool state)
    {
    }
}
