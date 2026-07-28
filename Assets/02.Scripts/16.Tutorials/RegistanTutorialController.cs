using System.Collections;
using MalbersAnimations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RegistanTutorialController : MonoBehaviour
{
    public static bool IsTutorialActive { get; private set; }
    public static bool ShouldPauseMainTime { get; private set; }

    private enum TutorialState
    {
        None,
        JoystickExplanation,
        WaitingForJoystickInput,
        CameraExplanation,
        WaitingForCameraInput,
        MatchStatusExplanation,
        CameraViewExplanation,
        WaitingForFirstPerson,
        WaitingForThirdPerson,
        WaitingForThirdPersonTransition,
        LookBackExplanation,
        WaitingForLookBackPress,
        WaitingForLookBackRelease,
        WaitingForMovementBeforeSprint,
        SprintExplanation,
        WaitingForSprintUse,
        SprintSliderPreview,
        UloqIndicatorExplanation,
        WaitingForPickupAvailability,
        PickupButtonExplanation,
        WaitingForPickupPress,
        PickupSliderExplanation,
        WaitingForPlayerPickup,
        TargetIndicatorExplanation,
        WaitingForCombo,
        ComboExplanation,
        WaitingForCarrier,
        CarrierExplanation,
        WalkZoneExplanation,
        WaitingForNextRoundWarmup,
        WarmupBackgroundExplanation,
        WarmupIndicatorExplanation,
        WaitingForWarmupArrival,
        RoundStartExplanation,
        GripDamageExplanation,
        DefendExplanation,
        LostUlakExplanation,
        OpponentCarrierExplanation,
        WebSnareButtonExplanation,
        WaitingForWebSnareButtonClick,
        ChainContainerExplanation,
        WaitingForChainContainerPress,
        FakeUlakExplanation,
        HorseHealthExplanation,
        Finished
    }

    [Header("Registan Targets")]
    [SerializeField] private KopkariMainUI mainUI;
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private RectTransform movementJoystickTarget;
    [SerializeField] private MobileJoystick cameraJoystick;
    [SerializeField] private RectTransform cameraJoystickTarget;
    [SerializeField] private RectTransform lookBackTarget;
    [SerializeField] private Button sprintButton;
    [SerializeField] private RectTransform sprintButtonTarget;
    [SerializeField] private Slider sprintSlider;
    [SerializeField] private RectTransform sprintSliderTarget;
    [SerializeField] private RectTransform matchStatusTarget;
    [SerializeField] private KopkariObjectiveIndicator objectiveIndicator;
    [SerializeField] private RectTransform objectiveIndicatorTarget;
    [SerializeField] private RegistanTutorialPresentation presentationPrefab;

    [Header("Highlight Hosts")]
    [Tooltip("First-sibling highlight host under the main UICanvas.")]
    [SerializeField] private RectTransform mainHighlightHost;
    [Tooltip("First-sibling highlight host under MobileUICanvas.")]
    [SerializeField] private RectTransform mobileHighlightHost;
    [Tooltip("MobileUICanvas root used to select the mobile highlight host.")]
    [SerializeField] private RectTransform mobileCanvasRoot;

    [Header("Input")]
    [SerializeField, Range(0.1f, 1f)] private float joystickCompletionThreshold = 0.35f;
    [SerializeField, Range(0.02f, 0.3f)] private float cameraCompletionThreshold = 0.08f;
    [SerializeField, Range(0.5f, 4f)] private float cameraPracticeDuration = 1.5f;
    [SerializeField, Range(0.25f, 2f)] private float thirdPersonSettleDuration = 1f;
    [SerializeField, Range(1f, 6f)] private float sliderPreviewDuration = 3f;
    [SerializeField, Range(3f, 8f)] private float walkZoneTutorialDelay = 4.5f;
    [SerializeField, Range(0.25f, 3f)] private float webSnareShootTutorialDelay = 1f;
    [SerializeField, Range(2f, 10f)] private float cloudProgressWaitTimeout = 6f;

    [Header("Popup Placement")]
    [SerializeField] private UITargetPlacementSettings joystickPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Right,
        popupOffset = new Vector2(40f, 80f),
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };
    [SerializeField] private UITargetPlacementSettings indicatorPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Auto,
        popupOffset = Vector2.zero,
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };
    [SerializeField] private UITargetPlacementSettings cameraPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Left,
        popupOffset = new Vector2(-40f, 0f),
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };
    [SerializeField] private UITargetPlacementSettings sprintPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Top,
        popupOffset = Vector2.zero,
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };
    [SerializeField] private UITargetPlacementSettings sliderPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Bottom,
        popupOffset = Vector2.zero,
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };
    [SerializeField] private UITargetPlacementSettings matchStatusPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Bottom,
        popupOffset = Vector2.zero,
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };

    private TutorialState state;
    private Coroutine startRoutine;
    private Coroutine sliderPreviewRoutine;
    private Coroutine cameraTransitionRoutine;
    private Coroutine walkZoneDelayRoutine;
    private Coroutine webSnareTutorialDelayRoutine;
    private GameObject tutorialCanvasObject;
    private GameObject presentationRoot;
    private Image blocker;
    private RectTransform highlight;
    private RectTransform defaultHighlightParent;
    private int defaultHighlightSiblingIndex;
    private RectTransform popup;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private Button nextButton;
    private TMP_Text nextButtonText;
    private RegistanTutorialPresentation presentation;
    private bool ownsPresentationInstance;
    private bool presentationHiddenForPauseMenu;
    private bool pickupFocusAvailable;
    private float cameraPracticeElapsed;
    private bool pendingGripDamageTutorial;
    private bool pendingWalkZoneTutorial;
    private bool pendingHorseHealthTutorial;
    private bool gripDepletionObserved;
    private bool pendingLostUlakTutorial;
    private bool pendingOpponentCarrierTutorial;
    private bool pendingFakeUlakTutorial;
    private bool gripDamageTutorialShown;
    private bool walkZoneTutorialShown;
    private bool walkZoneTutorialUnlocked;
    private bool horseHealthTutorialShown;
    private bool fakeUlakTutorialShown;
    private bool lostUlakTutorialShown;
    private bool opponentCarrierTutorialShown;
    private bool localPlayerHadUlak;
    private float nextProgressCompletionCheck;
    private KopkariTutorialProgress.CoreCheckpoint savedCheckpoint;
    private TutorialState contextReturnState = TutorialState.Finished;

    private static readonly Color BackdropColor = new Color(0.015f, 0.025f, 0.05f, 0.72f);

    private void Awake()
    {
        if (mainUI == null)
            mainUI = GetComponent<KopkariMainUI>();
        CreatePresentation();
        HidePresentation();
    }

    private void OnEnable()
    {
        KopkariManager.OnGameStartFinishState += HandleGameStartFinishState;
        if (movementJoystick != null)
            movementJoystick.OnAxisChange.AddListener(HandleJoystickAxisChanged);
        KopkariMainUI.OnSprintStart += HandleSprintStarted;
        KopkariManager.OnFirstPersonCamera += HandleFirstPersonCamera;
        UILookBackButton.OnCameraPressedState += HandleLookBackState;
        HoldInputForwarder.OnPickupFocusChanged += HandlePickupFocusChanged;
        UIGetLamp.OnPlayerHoldChanged += HandlePickupHoldChanged;
        KopkariManager.OnGoatOwnerChanged += HandleGoatOwnerChanged;
        KopkariMainUI.OnComboVisibilityChanged += HandleComboVisibilityChanged;
        KopkariMainUI.OnCarrierVisibilityChanged += HandleCarrierVisibilityChanged;
        KopkariMainUI.OnHorseHealthDamaged += HandleHorseHealthDamaged;
        KopkariMainUI.OnFakeUlakInteractableChanged += HandleFakeUlakInteractableChanged;
        KopkariManager.OnLocalPlayerGripDamaged += HandleLocalPlayerGripDamaged;
        KopkariManager.OnLocalPlayerGripDepleted += HandleLocalPlayerGripDepleted;
        KopkariMainUI.OnWebSnareBtnEnable += HandleWebSnareButtonEnabled;
        KopkariMainUI.OnWebSnareStart += HandleWebSnareStarted;
    }

    private void OnDisable()
    {
        KopkariManager.OnGameStartFinishState -= HandleGameStartFinishState;
        if (movementJoystick != null)
            movementJoystick.OnAxisChange.RemoveListener(HandleJoystickAxisChanged);
        KopkariMainUI.OnSprintStart -= HandleSprintStarted;
        KopkariManager.OnFirstPersonCamera -= HandleFirstPersonCamera;
        UILookBackButton.OnCameraPressedState -= HandleLookBackState;
        HoldInputForwarder.OnPickupFocusChanged -= HandlePickupFocusChanged;
        UIGetLamp.OnPlayerHoldChanged -= HandlePickupHoldChanged;
        KopkariManager.OnGoatOwnerChanged -= HandleGoatOwnerChanged;
        KopkariMainUI.OnComboVisibilityChanged -= HandleComboVisibilityChanged;
        KopkariMainUI.OnCarrierVisibilityChanged -= HandleCarrierVisibilityChanged;
        KopkariMainUI.OnHorseHealthDamaged -= HandleHorseHealthDamaged;
        KopkariMainUI.OnFakeUlakInteractableChanged -= HandleFakeUlakInteractableChanged;
        KopkariManager.OnLocalPlayerGripDamaged -= HandleLocalPlayerGripDamaged;
        KopkariManager.OnLocalPlayerGripDepleted -= HandleLocalPlayerGripDepleted;
        KopkariMainUI.OnWebSnareBtnEnable -= HandleWebSnareButtonEnabled;
        KopkariMainUI.OnWebSnareStart -= HandleWebSnareStarted;

        StopOwnedCoroutines();
        FinishTutorial(false);
    }

    private void OnDestroy()
    {
        RestoreHighlightParent();
        if (ownsPresentationInstance && tutorialCanvasObject != null)
            Destroy(tutorialCanvasObject);
    }

    private void Update()
    {
        if (presentationRoot == null)
            return;

        if (state != TutorialState.None &&
            state != TutorialState.Finished &&
            Time.unscaledTime >= nextProgressCompletionCheck)
        {
            nextProgressCompletionCheck = Time.unscaledTime + 1f;
            if (KopkariTutorialProgress.LoadLocal().Completed)
            {
                FinishCompletedTutorialSetup();
                return;
            }
        }

        RefreshTutorialObjectivePreview();

        if (gripDepletionObserved && !fakeUlakTutorialShown &&
            mainUI != null && mainUI.IsFakeUlakInteractable)
            pendingFakeUlakTutorial = true;

        RefreshPendingOpponentCarrier();

        if (TryShowPendingContextTutorial())
            return;

        if (state == TutorialState.None || state == TutorialState.Finished)
            return;

        bool pauseMenuActive = KopkariMainUI.IsGameplayPaused;
        if (pauseMenuActive && presentationRoot.activeSelf)
        {
            if (highlight != null)
                highlight.gameObject.SetActive(false);
            presentationRoot.SetActive(false);
            presentationHiddenForPauseMenu = true;
        }
        else if (!pauseMenuActive && presentationHiddenForPauseMenu)
        {
            presentationRoot.SetActive(true);
            presentationHiddenForPauseMenu = false;
            RefreshCurrentPlacement();
        }

        if (state == TutorialState.WaitingForCameraInput &&
            cameraJoystick != null &&
            cameraJoystick.AxisValue.sqrMagnitude >=
            cameraCompletionThreshold * cameraCompletionThreshold)
        {
            cameraPracticeElapsed += Time.unscaledDeltaTime;
            if (cameraPracticeElapsed >= Mathf.Max(0.5f, cameraPracticeDuration))
            {
                CompleteCoreStep(
                    Constants.KopkariTutorial.CameraJoystick,
                    KopkariTutorialProgress.CoreCheckpoint.MatchStatus);
                ShowMatchStatusExplanation();
                return;
            }
        }

        KopkariManager manager = KopkariManager.Instance;
        if (state == TutorialState.WaitingForPickupAvailability &&
            (pickupFocusAvailable ||
             (mainUI != null && mainUI.PickupButtonTutorialTarget != null &&
              mainUI.PickupButtonTutorialTarget.gameObject.activeInHierarchy)))
        {
            ShowPickupButtonExplanation();
        }
        else if (state == TutorialState.WaitingForCombo &&
                 mainUI != null && mainUI.IsComboVisible)
        {
            ShowComboExplanation();
        }
        else if (state == TutorialState.WaitingForCarrier &&
                 mainUI != null && mainUI.IsCarrierVisible)
        {
            ShowCarrierExplanation();
        }
        else if (state == TutorialState.WaitingForNextRoundWarmup &&
                 manager != null && manager.IsRoundWarmupActive)
        {
            savedCheckpoint = KopkariTutorialProgress.LoadLocal().Checkpoint;
            if (savedCheckpoint >= KopkariTutorialProgress.CoreCheckpoint.WarmupArrival)
                WaitForWarmupArrival();
            else if (savedCheckpoint >=
                     KopkariTutorialProgress.CoreCheckpoint.WarmupIndicator)
                ShowWarmupIndicatorExplanation();
            else
                ShowWarmupBackgroundExplanation();
        }
        else if (state == TutorialState.WaitingForWarmupArrival &&
                 manager != null && manager.IsRoundStartCountdownActive)
        {
            RectTransform warmupTarget = mainUI != null ? mainUI.WarmupTutorialTarget : null;
            if (warmupTarget != null && warmupTarget.gameObject.activeInHierarchy)
                ShowRoundStartExplanation();
        }
    }

    private void HandleGameStartFinishState(bool gameStarted)
    {
        if (!gameStarted)
        {
            FinishTutorial(false);
            return;
        }

        // The tutorial is a one-time flow for this Registan scene session.
        // Starting the next round must not restart it after the final message.
        if (state == TutorialState.Finished)
            return;

        if (state != TutorialState.None)
            return;

        KopkariTutorialProgress.State localProgress = KopkariTutorialProgress.LoadLocal();
        if (localProgress.Completed)
        {
            FinishCompletedTutorialSetup();
            return;
        }

        AIKopkariRider.SetRegistanTutorialRestrictions(true, false, false);
        ShouldPauseMainTime = true;
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(false);
        if (startRoutine != null)
            StopCoroutine(startRoutine);
        startRoutine = StartCoroutine(StartAfterGameplayUIIsReady());
    }

    private IEnumerator StartAfterGameplayUIIsReady()
    {
        if (!KopkariTutorialProgress.HasAnyLocalData &&
            DataManager.Instance != null)
        {
            DataManager.Instance.EnsureKopkariTutorialStateLoaded();
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(2f, cloudProgressWaitTimeout);
            while (!DataManager.Instance.IsKopkariTutorialStateLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        KopkariTutorialProgress.State progress = KopkariTutorialProgress.LoadLocal();
        if (progress.Completed)
        {
            startRoutine = null;
            FinishCompletedTutorialSetup();
            yield break;
        }

        savedCheckpoint = progress.Checkpoint;
        ApplySavedContextProgress(progress.Context);

        // CanvasEnable is invoked from the same game-start event. Waiting one
        // frame makes this independent of subscriber order and lets UI layout settle.
        yield return null;
        Canvas.ForceUpdateCanvases();
        startRoutine = null;

        if (mainUI == null ||
            movementJoystick == null || movementJoystickTarget == null ||
            cameraJoystick == null || cameraJoystickTarget == null ||
            mainUI.CameraSwitchTutorialTarget == null || lookBackTarget == null ||
            sprintButton == null || sprintButtonTarget == null ||
            sprintSlider == null || sprintSliderTarget == null ||
            matchStatusTarget == null || objectiveIndicator == null ||
            objectiveIndicatorTarget == null || presentation == null ||
            mainUI.PickupButtonTutorialTarget == null ||
            mainUI.PickupProgressTutorialTarget == null ||
            mainUI.ComboPrizeTutorialTarget == null ||
            mainUI.CarrierTutorialTarget == null ||
            mainUI.HorseHealthTutorialTarget == null ||
            mainUI.DefendTutorialTarget == null ||
            mainUI.WalkZoneTutorialTarget == null ||
            mainUI.ShootWebTutorialTarget == null ||
            mainUI.ChainContainerTutorialTarget == null ||
            mainUI.FakeUlakTutorialTarget == null ||
            mainUI.RoundChangeTutorialTarget == null ||
            mainUI.WarmupTutorialTarget == null)
        {
            Debug.LogWarning($"[{nameof(RegistanTutorialController)}] One or more Registan tutorial references are missing.", this);
            FinishTutorial(false);
            yield break;
        }

        StartFromSavedCheckpoint();
    }

    private void StartFromSavedCheckpoint()
    {
        switch (savedCheckpoint)
        {
            case KopkariTutorialProgress.CoreCheckpoint.Joystick:
                ShowJoystickExplanation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.CameraJoystick:
                ShowCameraExplanation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.MatchStatus:
                ShowMatchStatusExplanation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.CameraView:
                ShowCameraViewExplanation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.BackCamera:
                ShowLookBackExplanation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.Sprint:
                ConfigureUloqPracticePhase();
                state = TutorialState.WaitingForMovementBeforeSprint;
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                HidePresentation();
                break;
            case KopkariTutorialProgress.CoreCheckpoint.UloqIndicator:
                ConfigureUloqPracticePhase();
                ShowUloqIndicatorExplanation();
                break;
            default:
                ConfigureUloqPracticePhase();
                WaitForPickupAvailability();
                break;
        }
    }

    private void ConfigureUloqPracticePhase()
    {
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(true);
        ShouldPauseMainTime = false;
        AIKopkariRider.SetRegistanTutorialRestrictions(true, true, false);
        IsTutorialActive = true;
    }

    private void FinishCompletedTutorialSetup()
    {
        KopkariTutorialProgress.State progress = KopkariTutorialProgress.LoadLocal();
        savedCheckpoint = progress.Checkpoint;
        ApplySavedContextProgress(progress.Context);
        StopOwnedCoroutines();
        HidePresentation();
        HideTutorialObjectivePreview();
        AIKopkariRider.SetRegistanTutorialRestrictions(false, true, true);
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(true);
        ShouldPauseMainTime = false;
        TutorialPauseController.ResumeAll();
        state = TutorialState.Finished;
        IsTutorialActive = false;
    }

    private void ApplySavedContextProgress(KopkariTutorialProgress.ContextLesson context)
    {
        walkZoneTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.WalkZone) != 0;
        gripDamageTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.Defend) != 0;
        lostUlakTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.LostUloq) != 0;
        fakeUlakTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.FakeUloq) != 0;
        opponentCarrierTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.OpponentCarrier) != 0;
        horseHealthTutorialShown =
            (context & KopkariTutorialProgress.ContextLesson.HorseHealth) != 0;
        walkZoneTutorialUnlocked =
            savedCheckpoint >= KopkariTutorialProgress.CoreCheckpoint.NextRound;
    }

    private void ShowJoystickExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.JoystickExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);

        ShowPresentation(
            movementJoystickTarget,
            joystickPlacement,
            RegistanTutorialTextIds.MoveYourHorse,
            RegistanTutorialTextIds.UseMovementJoystick,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginJoystickPractice()
    {
        state = TutorialState.WaitingForJoystickInput;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        ShowPresentation(
            movementJoystickTarget,
            joystickPlacement,
            RegistanTutorialTextIds.YourTurn,
            RegistanTutorialTextIds.DragJoystick,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleJoystickAxisChanged(Vector2 axis)
    {
        if (axis.sqrMagnitude < joystickCompletionThreshold * joystickCompletionThreshold)
            return;

        if (state == TutorialState.WaitingForJoystickInput)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.Joystick,
                KopkariTutorialProgress.CoreCheckpoint.CameraJoystick);
            ShowCameraExplanation();
        }
        else if (state == TutorialState.WaitingForMovementBeforeSprint)
            ShowSprintExplanation();
    }

    private void ShowCameraExplanation()
    {
        state = TutorialState.CameraExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);

        ShowPresentation(
            cameraJoystickTarget,
            cameraPlacement,
            RegistanTutorialTextIds.LookAround,
            RegistanTutorialTextIds.DragCameraArea,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginCameraPractice()
    {
        state = TutorialState.WaitingForCameraInput;
        cameraPracticeElapsed = 0f;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        ShowPresentation(
            cameraJoystickTarget,
            cameraPlacement,
            RegistanTutorialTextIds.YourTurn,
            RegistanTutorialTextIds.KeepDraggingCamera,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void ShowMatchStatusExplanation()
    {
        state = TutorialState.MatchStatusExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            matchStatusTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.MatchStatus,
            RegistanTutorialTextIds.MatchStatusDescription,
            RegistanTutorialTextIds.Next,
            true,
            true);
    }

    private void ShowCameraViewExplanation()
    {
        state = TutorialState.CameraViewExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.CameraSwitchTutorialTarget,
            cameraPlacement,
            RegistanTutorialTextIds.ChangeCamera,
            RegistanTutorialTextIds.ChangeCameraDescription,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginCameraViewPractice()
    {
        state = TutorialState.WaitingForFirstPerson;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.CameraSwitchTutorialTarget,
            cameraPlacement,
            RegistanTutorialTextIds.FirstPerson,
            RegistanTutorialTextIds.FirstPersonDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleFirstPersonCamera(bool firstPerson)
    {
        if (state == TutorialState.WaitingForFirstPerson && firstPerson)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.CameraFirstPerson,
                KopkariTutorialProgress.CoreCheckpoint.CameraView);
            state = TutorialState.WaitingForThirdPerson;
            ShowPresentation(
                mainUI.CameraSwitchTutorialTarget,
                cameraPlacement,
                RegistanTutorialTextIds.ThirdPerson,
                RegistanTutorialTextIds.ThirdPersonDescription,
                RegistanTutorialTextIds.None,
                false,
                false);
        }
        else if (state == TutorialState.WaitingForThirdPerson && !firstPerson)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.CameraThirdPerson,
                KopkariTutorialProgress.CoreCheckpoint.BackCamera);
            state = TutorialState.WaitingForThirdPersonTransition;
            TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
            HidePresentation();

            if (cameraTransitionRoutine != null)
                StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = StartCoroutine(WaitForThirdPersonCamera());
        }
    }

    private IEnumerator WaitForThirdPersonCamera()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, thirdPersonSettleDuration));
        cameraTransitionRoutine = null;

        if (state == TutorialState.WaitingForThirdPersonTransition)
            ShowLookBackExplanation();
    }

    private void ShowLookBackExplanation()
    {
        state = TutorialState.LookBackExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            lookBackTarget,
            cameraPlacement,
            RegistanTutorialTextIds.LookBehind,
            RegistanTutorialTextIds.LookBehindDescription,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginLookBackPractice()
    {
        state = TutorialState.WaitingForLookBackPress;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            lookBackTarget,
            cameraPlacement,
            RegistanTutorialTextIds.HoldLookBack,
            RegistanTutorialTextIds.HoldLookBackDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleLookBackState(bool pressed)
    {
        if (state == TutorialState.WaitingForLookBackPress && pressed)
        {
            state = TutorialState.WaitingForLookBackRelease;
            ShowPresentation(
                lookBackTarget,
                cameraPlacement,
                RegistanTutorialTextIds.Release,
                RegistanTutorialTextIds.ReleaseLookBackDescription,
                RegistanTutorialTextIds.None,
                false,
                false);
        }
        else if (state == TutorialState.WaitingForLookBackRelease && !pressed)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.BackCamera,
                KopkariTutorialProgress.CoreCheckpoint.Sprint);
            KopkariManager.Instance?.SetRegistanTutorialUlakVisible(true);
            ShouldPauseMainTime = false;
            AIKopkariRider.SetRegistanTutorialRestrictions(true, true, false);
            state = TutorialState.WaitingForMovementBeforeSprint;
            TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
            HidePresentation();
        }
    }

    private void ShowSprintExplanation()
    {
        state = TutorialState.SprintExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);

        ShowPresentation(
            sprintButtonTarget,
            sprintPlacement,
            RegistanTutorialTextIds.Sprint,
            RegistanTutorialTextIds.SprintDescription,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginSprintPractice()
    {
        state = TutorialState.WaitingForSprintUse;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        ShowPresentation(
            sprintButtonTarget,
            sprintPlacement,
            RegistanTutorialTextIds.HoldSprint,
            RegistanTutorialTextIds.HoldSprintDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleSprintStarted()
    {
        if (state == TutorialState.WaitingForSprintUse)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.SprintButton,
                KopkariTutorialProgress.CoreCheckpoint.Sprint);
            ShowSprintSliderPreview();
        }
    }

    private void ShowSprintSliderPreview()
    {
        state = TutorialState.SprintSliderPreview;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        ShowPresentation(
            sprintSliderTarget,
            sliderPlacement,
            RegistanTutorialTextIds.SprintStamina,
            RegistanTutorialTextIds.SprintStaminaDescription,
            RegistanTutorialTextIds.None,
            false,
            false);

        if (sliderPreviewRoutine != null)
            StopCoroutine(sliderPreviewRoutine);
        sliderPreviewRoutine = StartCoroutine(HideSliderPreviewAfterDelay());
    }

    private IEnumerator HideSliderPreviewAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(1f, sliderPreviewDuration));
        sliderPreviewRoutine = null;

        if (state == TutorialState.SprintSliderPreview)
        {
            CompleteCoreStep(
                Constants.KopkariTutorial.SprintSlider,
                KopkariTutorialProgress.CoreCheckpoint.UloqIndicator);
            ShowUloqIndicatorExplanation();
        }
    }

    private void ShowUloqIndicatorExplanation()
    {
        state = TutorialState.UloqIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowTutorialObjectivePreview();

        ShowPresentation(
            GetObjectiveTutorialTarget(),
            indicatorPlacement,
            RegistanTutorialTextIds.FindUloq,
            RegistanTutorialTextIds.FindUloqDescription,
            RegistanTutorialTextIds.GoToUloq,
            true,
            true);
    }

    private void WaitForPickupAvailability()
    {
        ShowTutorialObjectivePreview();
        state = TutorialState.WaitingForPickupAvailability;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            GetObjectiveTutorialTarget(),
            indicatorPlacement,
            RegistanTutorialTextIds.GetCloser,
            RegistanTutorialTextIds.GetCloserDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandlePickupFocusChanged(bool focused)
    {
        pickupFocusAvailable = focused;
        if (focused && state == TutorialState.WaitingForPickupAvailability)
            ShowPickupButtonExplanation();
        else if (!focused &&
                 (state == TutorialState.PickupButtonExplanation ||
                  state == TutorialState.WaitingForPickupPress))
            WaitForPickupAvailability();
    }

    private void ShowPickupButtonExplanation()
    {
        HideTutorialObjectivePreview();
        state = TutorialState.PickupButtonExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.PickupButtonTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.PickUpUloq,
            RegistanTutorialTextIds.PickUpUloqDescription,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginPickupPractice()
    {
        CompleteCoreStep(
            Constants.KopkariTutorial.PickupButton,
            KopkariTutorialProgress.LoadLocal().Checkpoint);
        state = TutorialState.WaitingForPickupPress;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.PickupButtonTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.HoldToPickUp,
            RegistanTutorialTextIds.HoldToPickUpDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandlePickupHoldChanged(bool holding)
    {
        if (holding && state == TutorialState.WaitingForPickupPress)
        {
            state = TutorialState.PickupSliderExplanation;
            TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
            ShowPresentation(
                mainUI.PickupProgressTutorialTarget,
                sliderPlacement,
                RegistanTutorialTextIds.PickupProgress,
                RegistanTutorialTextIds.PickupProgressDescription,
                RegistanTutorialTextIds.None,
                false,
                false);
            state = TutorialState.WaitingForPlayerPickup;
        }
        else if (!holding && state == TutorialState.WaitingForPlayerPickup &&
                 (KopkariManager.Instance == null ||
                  KopkariManager.Instance.currentGoatOwner == null))
        {
            state = TutorialState.WaitingForPickupPress;
            ShowPresentation(
                mainUI.PickupButtonTutorialTarget,
                sprintPlacement,
                RegistanTutorialTextIds.KeepHolding,
                RegistanTutorialTextIds.KeepHoldingDescription,
                RegistanTutorialTextIds.None,
                false,
                false);
        }
    }

    private void HandleGoatOwnerChanged(GameObject ownerRoot)
    {
        KopkariManager manager = KopkariManager.Instance;
        if (manager == null)
            return;

        bool localPlayerOwnsUlak = ownerRoot != null &&
                                   manager.IsLocalRiderTransform(ownerRoot.transform);
        if (!localPlayerOwnsUlak)
        {
            if (walkZoneDelayRoutine != null)
            {
                StopCoroutine(walkZoneDelayRoutine);
                walkZoneDelayRoutine = null;
            }
            pendingWalkZoneTutorial = false;

            if (localPlayerHadUlak &&
                manager.roomState == KopkariManager.RoomState.GameStarted &&
                !lostUlakTutorialShown)
            {
                pendingLostUlakTutorial = true;
                pendingFakeUlakTutorial = true;
            }

            localPlayerHadUlak = false;

            if (ownerRoot != null &&
                manager.roomState == KopkariManager.RoomState.GameStarted &&
                !opponentCarrierTutorialShown)
            {
                pendingOpponentCarrierTutorial = true;
            }
            return;
        }

        localPlayerHadUlak = true;
        if (walkZoneTutorialUnlocked && !walkZoneTutorialShown)
            ScheduleWalkZoneTutorial();
        if (state == TutorialState.WaitingForPlayerPickup ||
            state == TutorialState.WaitingForPickupPress ||
            state == TutorialState.PickupSliderExplanation ||
            state == TutorialState.WaitingForPickupAvailability)
        {
            ContinueAfterLocalPlayerPickup();
        }
    }

    private void ContinueAfterLocalPlayerPickup()
    {
        CompleteCoreStep(
            Constants.KopkariTutorial.PickupProgress,
            KopkariTutorialProgress.CoreCheckpoint.TargetIndicator);
        savedCheckpoint = KopkariTutorialProgress.LoadLocal().Checkpoint;

        if (savedCheckpoint <= KopkariTutorialProgress.CoreCheckpoint.TargetIndicator)
        {
            ShowTargetIndicatorExplanation();
        }
        else if (savedCheckpoint == KopkariTutorialProgress.CoreCheckpoint.ComboPrize)
        {
            WaitForCombo();
        }
        else if (savedCheckpoint == KopkariTutorialProgress.CoreCheckpoint.Carrier)
        {
            WaitForCarrier();
        }
        else
        {
            ReleaseCompetitionAndScheduleWalkZone();
        }
    }

    private void ShowTargetIndicatorExplanation()
    {
        state = TutorialState.TargetIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowTutorialObjectivePreview();
        ShowPresentation(
            GetObjectiveTutorialTarget(),
            indicatorPlacement,
            RegistanTutorialTextIds.RideToSalym,
            RegistanTutorialTextIds.RideToSalymDescription,
            RegistanTutorialTextIds.Next,
            true,
            true);
    }

    private void WaitForCombo()
    {
        HideTutorialObjectivePreview();
        if (mainUI.IsComboVisible)
        {
            ShowComboExplanation();
            return;
        }

        state = TutorialState.WaitingForCombo;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        HidePresentation();
    }

    private void HandleComboVisibilityChanged(bool visible)
    {
        if (visible && state == TutorialState.WaitingForCombo)
            ShowComboExplanation();
    }

    private void ShowComboExplanation()
    {
        state = TutorialState.ComboExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.ComboPrizeTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.ComboPrize,
            RegistanTutorialTextIds.ComboPrizeDescription,
            RegistanTutorialTextIds.Next,
            true,
            true);
    }

    private void WaitForCarrier()
    {
        if (mainUI.IsCarrierVisible)
        {
            ShowCarrierExplanation();
            return;
        }

        state = TutorialState.WaitingForCarrier;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        HidePresentation();
    }

    private void HandleCarrierVisibilityChanged(bool visible)
    {
        if (visible && state == TutorialState.WaitingForCarrier)
            ShowCarrierExplanation();
    }

    private void ShowCarrierExplanation()
    {
        state = TutorialState.CarrierExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.CarrierTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.CarrierGrip,
            RegistanTutorialTextIds.CarrierGripDescription,
            RegistanTutorialTextIds.Continue,
            true,
            true);
    }

    private void ReleaseCompetitionAndWaitForNextRound()
    {
        AIKopkariRider.SetRegistanTutorialRestrictions(false, true, true);
        HideTutorialObjectivePreview();
        state = TutorialState.WaitingForNextRoundWarmup;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        HidePresentation();
    }

    private void HandleHorseHealthDamaged(float currentHealth, float maximumHealth)
    {
        if (!horseHealthTutorialShown)
            pendingHorseHealthTutorial = true;
    }

    private void HandleLocalPlayerGripDamaged(float currentGrip, float maximumGrip)
    {
        if (!gripDamageTutorialShown)
            pendingGripDamageTutorial = true;
    }

    private void HandleLocalPlayerGripDepleted()
    {
        gripDepletionObserved = true;
        if (!lostUlakTutorialShown)
            pendingLostUlakTutorial = true;
        if (!fakeUlakTutorialShown)
            pendingFakeUlakTutorial = true;
    }

    private void HandleFakeUlakInteractableChanged(bool interactable)
    {
        if (interactable && gripDepletionObserved && !fakeUlakTutorialShown)
            pendingFakeUlakTutorial = true;
    }

    private bool TryShowPendingContextTutorial()
    {
        if (!IsContextTutorialSlotAvailable())
            return false;

        if (pendingGripDamageTutorial && !gripDamageTutorialShown)
        {
            contextReturnState = state;
            pendingGripDamageTutorial = false;
            gripDamageTutorialShown = true;
            ShowGripDamageExplanation();
            return true;
        }

        if (pendingWalkZoneTutorial && !walkZoneTutorialShown)
        {
            if (!DoesLocalPlayerOwnUlak())
            {
                pendingWalkZoneTutorial = false;
            }
            else
            {
                contextReturnState = state;
                pendingWalkZoneTutorial = false;
                walkZoneTutorialShown = true;
                ShowWalkZoneExplanation();
                return true;
            }
        }

        if (pendingLostUlakTutorial && !lostUlakTutorialShown)
        {
            contextReturnState = state;
            pendingLostUlakTutorial = false;
            lostUlakTutorialShown = true;
            ShowLostUlakExplanation();
            return true;
        }

        if (pendingFakeUlakTutorial && !fakeUlakTutorialShown)
        {
            contextReturnState = state;
            pendingFakeUlakTutorial = false;
            fakeUlakTutorialShown = true;
            ShowFakeUlakExplanation();
            return true;
        }

        if (pendingOpponentCarrierTutorial && !opponentCarrierTutorialShown)
        {
            contextReturnState = state;
            pendingOpponentCarrierTutorial = false;
            opponentCarrierTutorialShown = true;
            ShowOpponentCarrierExplanation();
            return true;
        }

        // Horse health is deliberately last, but a silent waiting state is an
        // available slot; it does not need to wait for the next round to start.
        if (pendingHorseHealthTutorial &&
            !horseHealthTutorialShown &&
            IsCoreFlowReadyForHorseHealth())
        {
            contextReturnState = state;
            pendingHorseHealthTutorial = false;
            horseHealthTutorialShown = true;
            ShowHorseHealthExplanation();
            return true;
        }

        return false;
    }

    private bool IsCoreFlowReadyForHorseHealth()
    {
        return savedCheckpoint >= KopkariTutorialProgress.CoreCheckpoint.NextRound ||
               state == TutorialState.WaitingForNextRoundWarmup ||
               state == TutorialState.WaitingForWarmupArrival ||
               state == TutorialState.Finished;
    }

    private void RefreshPendingOpponentCarrier()
    {
        if (opponentCarrierTutorialShown ||
            pendingOpponentCarrierTutorial ||
            mainUI == null ||
            !mainUI.IsCarrierVisible)
        {
            return;
        }

        KopkariManager manager = KopkariManager.Instance;
        GameObject ownerRoot = manager != null ? manager.currentGoatOwner : null;
        if (manager == null ||
            ownerRoot == null ||
            manager.roomState != KopkariManager.RoomState.GameStarted ||
            manager.IsLocalRiderTransform(ownerRoot.transform))
        {
            return;
        }

        // Ownership can already be established when a saved tutorial resumes,
        // so do not depend only on receiving a fresh owner-change event.
        pendingOpponentCarrierTutorial = true;
    }

    private bool IsContextTutorialSlotAvailable()
    {
        if (presentationRoot != null && presentationRoot.activeSelf)
            return false;

        // Any silent tutorial state can be safely interrupted and restored.
        // Limiting this to late-round waits left damage and carrier lessons
        // queued forever after resuming at pickup/combo/practice checkpoints.
        return state != TutorialState.None;
    }

    private void ShowGripDamageExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.GripDamageExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.CarrierTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.GripDropping,
            RegistanTutorialTextIds.GripDroppingDescription,
            RegistanTutorialTextIds.HowToDefend,
            true,
            true);
    }

    private void ShowDefendExplanation()
    {
        state = TutorialState.DefendExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.DefendTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.DefendUloq,
            RegistanTutorialTextIds.DefendUloqDescription,
            RegistanTutorialTextIds.GotIt,
            true,
            true);
    }

    private void ShowFakeUlakExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.FakeUlakExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.FakeUlakTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.FakeUloq,
            RegistanTutorialTextIds.FakeUloqDescription,
            RegistanTutorialTextIds.GotIt,
            true,
            true);
    }

    private void ShowOpponentCarrierExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.OpponentCarrierExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.CarrierTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.OpponentHasUloq,
            RegistanTutorialTextIds.OpponentHasUloqDescription,
            RegistanTutorialTextIds.Next,
            true,
            true);
    }

    private void ShowWebSnareButtonExplanation()
    {
        state = TutorialState.WebSnareButtonExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.ShootWebTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.WebSnare,
            RegistanTutorialTextIds.WebSnareDescription,
            mainUI.IsShootWebInteractable
                ? RegistanTutorialTextIds.TryIt
                : RegistanTutorialTextIds.GotIt,
            true,
            true);
    }

    private void BeginWebSnareButtonPractice()
    {
        if (!mainUI.IsShootWebInteractable)
        {
            CompleteContextStep(
                Constants.KopkariTutorial.OpponentCarrier,
                KopkariTutorialProgress.ContextLesson.OpponentCarrier);
            CompleteContextStep(
                Constants.KopkariTutorial.WebSnare,
                KopkariTutorialProgress.ContextLesson.WebSnare);
            RestoreContextTutorialState();
            return;
        }

        state = TutorialState.WaitingForWebSnareButtonClick;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.ShootWebTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.EquipWebSnare,
            RegistanTutorialTextIds.EquipWebSnareDescription,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleWebSnareButtonEnabled()
    {
        if (state != TutorialState.WaitingForWebSnareButtonClick || mainUI == null)
            return;

        if (webSnareTutorialDelayRoutine != null)
            StopCoroutine(webSnareTutorialDelayRoutine);
        webSnareTutorialDelayRoutine = StartCoroutine(
            WaitForWebSnareShootingButton());
    }

    private IEnumerator WaitForWebSnareShootingButton()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.25f, webSnareShootTutorialDelay));

        float readyDeadline = Time.realtimeSinceStartup + 2f;
        while (state == TutorialState.WaitingForWebSnareButtonClick &&
               mainUI != null &&
               !mainUI.IsChainContainerVisible &&
               Time.realtimeSinceStartup < readyDeadline)
        {
            yield return null;
        }

        if (state != TutorialState.WaitingForWebSnareButtonClick ||
            mainUI == null ||
            !mainUI.IsChainContainerVisible)
        {
            webSnareTutorialDelayRoutine = null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;
        webSnareTutorialDelayRoutine = null;
        ShowChainContainerExplanation();
    }

    private void ShowChainContainerExplanation()
    {
        state = TutorialState.ChainContainerExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.ChainContainerTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.ShootWebSnare,
            RegistanTutorialTextIds.ShootWebSnareDescription,
            RegistanTutorialTextIds.TryIt,
            true,
            true);
    }

    private void BeginChainContainerPractice()
    {
        state = TutorialState.WaitingForChainContainerPress;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.ChainContainerTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.YourTurn,
            RegistanTutorialTextIds.ShootWebSnarePractice,
            RegistanTutorialTextIds.None,
            false,
            false);
    }

    private void HandleWebSnareStarted()
    {
        if (state == TutorialState.WaitingForChainContainerPress)
        {
            CompleteContextStep(
                Constants.KopkariTutorial.OpponentCarrier,
                KopkariTutorialProgress.ContextLesson.OpponentCarrier);
            CompleteContextStep(
                Constants.KopkariTutorial.WebSnare,
                KopkariTutorialProgress.ContextLesson.WebSnare);
            CompleteContextStep(
                Constants.KopkariTutorial.ChainContainer,
                KopkariTutorialProgress.ContextLesson.ChainContainer);
            RestoreContextTutorialState();
        }
    }

    private void ShowLostUlakExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.LostUlakExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowTutorialObjectivePreview();
        ShowPresentation(
            GetObjectiveTutorialTarget(),
            indicatorPlacement,
            RegistanTutorialTextIds.LostUloq,
            RegistanTutorialTextIds.LostUloqDescription,
            RegistanTutorialTextIds.GoBack,
            true,
            true);
    }

    private void ShowHorseHealthExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.HorseHealthExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.HorseHealthTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.HorseHealth,
            RegistanTutorialTextIds.HorseHealthDescription,
            RegistanTutorialTextIds.GotIt,
            true,
            true);
    }

    private void RestoreContextTutorialState()
    {
        HidePresentation();
        TutorialPauseController.ResumeAll();
        state = contextReturnState;
        IsTutorialActive = state != TutorialState.Finished;
    }

    private void ShowWarmupBackgroundExplanation()
    {
        state = TutorialState.WarmupBackgroundExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.WarmupTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.NextRoundWarmup,
            RegistanTutorialTextIds.NextRoundWarmupDescription,
            RegistanTutorialTextIds.ShowTheWay,
            true,
            true);
    }

    private void ShowWarmupIndicatorExplanation()
    {
        state = TutorialState.WarmupIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowTutorialObjectivePreview();
        ShowPresentation(
            GetObjectiveTutorialTarget(),
            indicatorPlacement,
            RegistanTutorialTextIds.WarmupPoint,
            RegistanTutorialTextIds.WarmupPointDescription,
            RegistanTutorialTextIds.Go,
            false,
            true);
    }

    private void WaitForWarmupArrival()
    {
        HideTutorialObjectivePreview();
        state = TutorialState.WaitingForWarmupArrival;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        HidePresentation();
    }

    private void ShowRoundStartExplanation()
    {
        state = TutorialState.RoundStartExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        HideTutorialObjectivePreview();
        ShowPresentation(
            mainUI.WarmupTutorialTarget,
            matchStatusPlacement,
            RegistanTutorialTextIds.ReadyNextRound,
            RegistanTutorialTextIds.ReadyNextRoundDescription,
            RegistanTutorialTextIds.GotIt,
            true,
            true);
    }

    private void ShowWalkZoneExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.WalkZoneExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.WalkZoneTutorialTarget,
            sprintPlacement,
            RegistanTutorialTextIds.WalkZone,
            RegistanTutorialTextIds.WalkZoneDescription,
            RegistanTutorialTextIds.Continue,
            true,
            true);
    }

    private void ReleaseCompetitionAndScheduleWalkZone()
    {
        walkZoneTutorialUnlocked = true;
        ReleaseCompetitionAndWaitForNextRound();
        ScheduleWalkZoneTutorial();
    }

    private void ScheduleWalkZoneTutorial()
    {
        if (walkZoneTutorialShown || walkZoneDelayRoutine != null ||
            !DoesLocalPlayerOwnUlak())
            return;

        walkZoneDelayRoutine = StartCoroutine(WaitToOfferWalkZoneTutorial());
    }

    private IEnumerator WaitToOfferWalkZoneTutorial()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(3f, walkZoneTutorialDelay));
        walkZoneDelayRoutine = null;

        if (!walkZoneTutorialShown && DoesLocalPlayerOwnUlak())
            pendingWalkZoneTutorial = true;
    }

    private static bool DoesLocalPlayerOwnUlak()
    {
        KopkariManager manager = KopkariManager.Instance;
        return manager != null &&
               manager.currentGoatOwner != null &&
               manager.IsLocalRiderTransform(manager.currentGoatOwner.transform);
    }

    private void HandleNextClicked()
    {
        switch (state)
        {
            case TutorialState.JoystickExplanation:
                BeginJoystickPractice();
                break;
            case TutorialState.CameraExplanation:
                BeginCameraPractice();
                break;
            case TutorialState.MatchStatusExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.MatchStatus,
                    KopkariTutorialProgress.CoreCheckpoint.CameraView);
                ShowCameraViewExplanation();
                break;
            case TutorialState.CameraViewExplanation:
                BeginCameraViewPractice();
                break;
            case TutorialState.LookBackExplanation:
                BeginLookBackPractice();
                break;
            case TutorialState.SprintExplanation:
                BeginSprintPractice();
                break;
            case TutorialState.UloqIndicatorExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.UloqIndicator,
                    KopkariTutorialProgress.CoreCheckpoint.Pickup);
                WaitForPickupAvailability();
                break;
            case TutorialState.PickupButtonExplanation:
                BeginPickupPractice();
                break;
            case TutorialState.TargetIndicatorExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.TargetIndicator,
                    KopkariTutorialProgress.CoreCheckpoint.ComboPrize);
                WaitForCombo();
                break;
            case TutorialState.ComboExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.ComboPrize,
                    KopkariTutorialProgress.CoreCheckpoint.Carrier);
                WaitForCarrier();
                break;
            case TutorialState.CarrierExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.Carrier,
                    KopkariTutorialProgress.CoreCheckpoint.NextRound);
                ReleaseCompetitionAndScheduleWalkZone();
                break;
            case TutorialState.WalkZoneExplanation:
                CompleteContextStep(
                    Constants.KopkariTutorial.WalkZone,
                    KopkariTutorialProgress.ContextLesson.WalkZone);
                RestoreContextTutorialState();
                break;
            case TutorialState.WarmupBackgroundExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.WarmupBackground,
                    KopkariTutorialProgress.CoreCheckpoint.WarmupIndicator);
                ShowWarmupIndicatorExplanation();
                break;
            case TutorialState.WarmupIndicatorExplanation:
                CompleteCoreStep(
                    Constants.KopkariTutorial.WarmupIndicator,
                    KopkariTutorialProgress.CoreCheckpoint.WarmupArrival);
                WaitForWarmupArrival();
                break;
            case TutorialState.RoundStartExplanation:
                FinishTutorial(true);
                TryShowPendingContextTutorial();
                break;
            case TutorialState.GripDamageExplanation:
                ShowDefendExplanation();
                break;
            case TutorialState.OpponentCarrierExplanation:
                ShowWebSnareButtonExplanation();
                break;
            case TutorialState.WebSnareButtonExplanation:
                BeginWebSnareButtonPractice();
                break;
            case TutorialState.ChainContainerExplanation:
                BeginChainContainerPractice();
                break;
            case TutorialState.DefendExplanation:
                CompleteContextStep(
                    Constants.KopkariTutorial.GripDamage,
                    KopkariTutorialProgress.ContextLesson.GripDamage);
                CompleteContextStep(
                    Constants.KopkariTutorial.Defend,
                    KopkariTutorialProgress.ContextLesson.Defend);
                RestoreContextTutorialState();
                break;
            case TutorialState.LostUlakExplanation:
                CompleteContextStep(
                    Constants.KopkariTutorial.LostUloq,
                    KopkariTutorialProgress.ContextLesson.LostUloq);
                HideTutorialObjectivePreview();
                RestoreContextTutorialState();
                break;
            case TutorialState.FakeUlakExplanation:
                CompleteContextStep(
                    Constants.KopkariTutorial.FakeUloq,
                    KopkariTutorialProgress.ContextLesson.FakeUloq);
                RestoreContextTutorialState();
                break;
            case TutorialState.HorseHealthExplanation:
                CompleteContextStep(
                    Constants.KopkariTutorial.HorseHealth,
                    KopkariTutorialProgress.ContextLesson.HorseHealth);
                RestoreContextTutorialState();
                break;
        }
    }

    private void FinishTutorial(bool completed)
    {
        if (state == TutorialState.None && !completed)
            return;

        StopOwnedCoroutines();
        HidePresentation();
        HideTutorialObjectivePreview();
        AIKopkariRider.SetRegistanTutorialRestrictions(false, true, true);
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(true);
        ShouldPauseMainTime = false;
        TutorialPauseController.ResumeAll();
        if (completed)
            KopkariTutorialProgress.CompleteTutorial();
        state = completed ? TutorialState.Finished : TutorialState.None;
        IsTutorialActive = false;
    }

    private void CompleteCoreStep(
        string stepKey,
        KopkariTutorialProgress.CoreCheckpoint nextCheckpoint)
    {
        KopkariTutorialProgress.CompleteCoreStep(stepKey, nextCheckpoint);
        savedCheckpoint = KopkariTutorialProgress.LoadLocal().Checkpoint;
    }

    private static void CompleteContextStep(
        string stepKey,
        KopkariTutorialProgress.ContextLesson lesson)
    {
        KopkariTutorialProgress.CompleteContextStep(stepKey, lesson);
    }

    private void StopOwnedCoroutines()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }

        if (sliderPreviewRoutine != null)
        {
            StopCoroutine(sliderPreviewRoutine);
            sliderPreviewRoutine = null;
        }

        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }

        if (walkZoneDelayRoutine != null)
        {
            StopCoroutine(walkZoneDelayRoutine);
            walkZoneDelayRoutine = null;
        }

        if (webSnareTutorialDelayRoutine != null)
        {
            StopCoroutine(webSnareTutorialDelayRoutine);
            webSnareTutorialDelayRoutine = null;
        }

    }

    private void ShowPresentation(
        RectTransform target,
        UITargetPlacementSettings placement,
        int titleId,
        int descriptionId,
        int buttonLabelId,
        bool blockInput,
        bool showButton)
    {
        ShowPresentation(
            target,
            null,
            placement,
            titleId,
            descriptionId,
            buttonLabelId,
            blockInput,
            showButton);
    }

    private void ShowPresentation(
        RectTransform target,
        RectTransform secondaryTarget,
        UITargetPlacementSettings placement,
        int titleId,
        int descriptionId,
        int buttonLabelId,
        bool blockInput,
        bool showButton)
    {
        if (presentationRoot == null)
            return;

        presentationHiddenForPauseMenu = false;
        presentationRoot.SetActive(true);
        blocker.color = blockInput ? BackdropColor : new Color(0f, 0f, 0f, 0.18f);
        blocker.raycastTarget = blockInput;
        titleText.text = GetTutorialText(titleId);
        descriptionText.text = GetTutorialText(descriptionId);
        nextButton.gameObject.SetActive(showButton);
        nextButtonText.text = GetTutorialText(buttonLabelId);

        FitHighlightToTargets(target, secondaryTarget, 22f);
        UITargetRelativePlacer.Place(popup, target, tutorialCanvasObject.transform as RectTransform, placement);
    }

    private static string GetTutorialText(int languageId)
    {
        if (languageId <= 0)
            return string.Empty;

        LanguageManager languageManager = LanguageManager.Instance;
        return languageManager != null
            ? languageManager.GetText(languageId)
            : $"#{languageId}";
    }

    private void HidePresentation()
    {
        presentationHiddenForPauseMenu = false;
        if (highlight != null)
            highlight.gameObject.SetActive(false);
        if (presentationRoot != null)
            presentationRoot.SetActive(false);
    }

    private void RefreshCurrentPlacement()
    {
        switch (state)
        {
            case TutorialState.JoystickExplanation:
            case TutorialState.WaitingForJoystickInput:
                FitHighlightToTargets(movementJoystickTarget, null, 22f);
                UITargetRelativePlacer.Place(
                    popup,
                    movementJoystickTarget,
                    tutorialCanvasObject.transform as RectTransform,
                    joystickPlacement);
                break;
            case TutorialState.CameraExplanation:
            case TutorialState.WaitingForCameraInput:
                FitHighlightToTargets(cameraJoystickTarget, null, 22f);
                UITargetRelativePlacer.Place(
                    popup,
                    cameraJoystickTarget,
                    tutorialCanvasObject.transform as RectTransform,
                    cameraPlacement);
                break;
            case TutorialState.MatchStatusExplanation:
                PlaceAt(matchStatusTarget, matchStatusPlacement);
                break;
            case TutorialState.CameraViewExplanation:
            case TutorialState.WaitingForFirstPerson:
            case TutorialState.WaitingForThirdPerson:
                PlaceAt(mainUI != null ? mainUI.CameraSwitchTutorialTarget : null, cameraPlacement);
                break;
            case TutorialState.LookBackExplanation:
            case TutorialState.WaitingForLookBackPress:
            case TutorialState.WaitingForLookBackRelease:
                PlaceAt(lookBackTarget, cameraPlacement);
                break;
            case TutorialState.SprintExplanation:
            case TutorialState.WaitingForSprintUse:
                FitHighlightToTargets(sprintButtonTarget, null, 22f);
                UITargetRelativePlacer.Place(
                    popup,
                    sprintButtonTarget,
                    tutorialCanvasObject.transform as RectTransform,
                    sprintPlacement);
                break;
            case TutorialState.SprintSliderPreview:
                FitHighlightToTargets(sprintSliderTarget, null, 22f);
                UITargetRelativePlacer.Place(
                    popup,
                    sprintSliderTarget,
                    tutorialCanvasObject.transform as RectTransform,
                    sliderPlacement);
                break;
            case TutorialState.UloqIndicatorExplanation:
            case TutorialState.TargetIndicatorExplanation:
            case TutorialState.WarmupIndicatorExplanation:
            case TutorialState.LostUlakExplanation:
                PlaceAt(GetObjectiveTutorialTarget(), indicatorPlacement);
                break;
            case TutorialState.WaitingForPickupAvailability:
                PlaceAt(GetObjectiveTutorialTarget(), indicatorPlacement);
                break;
            case TutorialState.PickupButtonExplanation:
            case TutorialState.WaitingForPickupPress:
                PlaceAt(mainUI != null ? mainUI.PickupButtonTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.PickupSliderExplanation:
            case TutorialState.WaitingForPlayerPickup:
                PlaceAt(mainUI != null ? mainUI.PickupProgressTutorialTarget : null, sliderPlacement);
                break;
            case TutorialState.ComboExplanation:
                PlaceAt(mainUI != null ? mainUI.ComboPrizeTutorialTarget : null, matchStatusPlacement);
                break;
            case TutorialState.CarrierExplanation:
            case TutorialState.GripDamageExplanation:
            case TutorialState.OpponentCarrierExplanation:
                PlaceAt(mainUI != null ? mainUI.CarrierTutorialTarget : null, matchStatusPlacement);
                break;
            case TutorialState.WalkZoneExplanation:
                PlaceAt(mainUI != null ? mainUI.WalkZoneTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.DefendExplanation:
                PlaceAt(mainUI != null ? mainUI.DefendTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.FakeUlakExplanation:
                PlaceAt(mainUI != null ? mainUI.FakeUlakTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.WebSnareButtonExplanation:
            case TutorialState.WaitingForWebSnareButtonClick:
                PlaceAt(mainUI != null ? mainUI.ShootWebTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.ChainContainerExplanation:
            case TutorialState.WaitingForChainContainerPress:
                PlaceAt(mainUI != null ? mainUI.ChainContainerTutorialTarget : null, sprintPlacement);
                break;
            case TutorialState.HorseHealthExplanation:
                PlaceAt(mainUI != null ? mainUI.HorseHealthTutorialTarget : null, matchStatusPlacement);
                break;
            case TutorialState.WarmupBackgroundExplanation:
                PlaceAt(mainUI != null ? mainUI.WarmupTutorialTarget : null, matchStatusPlacement);
                break;
            case TutorialState.RoundStartExplanation:
                PlaceAt(mainUI != null ? mainUI.WarmupTutorialTarget : null, matchStatusPlacement);
                break;
        }
    }

    private void PlaceAt(RectTransform target, UITargetPlacementSettings placement)
    {
        if (target == null)
            return;

        FitHighlightToTargets(target, null, 22f);
        UITargetRelativePlacer.Place(
            popup,
            target,
            tutorialCanvasObject.transform as RectTransform,
            placement);
    }

    private void FitHighlightToTargets(RectTransform target, RectTransform secondaryTarget, float padding)
    {
        if (highlight == null)
            return;

        if (target == null)
        {
            highlight.gameObject.SetActive(false);
            return;
        }

        RectTransform highlightHost = ResolveHighlightHost(target);
        if (highlightHost == null)
        {
            highlight.gameObject.SetActive(false);
            return;
        }

        if (highlight.parent != highlightHost)
            highlight.SetParent(highlightHost, false);

        highlight.gameObject.SetActive(true);
        GetTargetBounds(target, highlightHost, out Vector2 min, out Vector2 max);
        if (secondaryTarget != null)
        {
            GetTargetBounds(
                secondaryTarget,
                highlightHost,
                out Vector2 secondaryMin,
                out Vector2 secondaryMax);
            min = Vector2.Min(min, secondaryMin);
            max = Vector2.Max(max, secondaryMax);
        }

        highlight.anchorMin = new Vector2(0.5f, 0.5f);
        highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        highlight.anchoredPosition = (min + max) * 0.5f;
        highlight.sizeDelta = max - min + Vector2.one * (padding * 2f);
    }

    private static void GetTargetBounds(
        RectTransform target,
        RectTransform highlightHost,
        out Vector2 min,
        out Vector2 max)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera targetCamera = GetCanvasCamera(target);
        Camera hostCamera = GetCanvasCamera(highlightHost);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightHost,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]),
            hostCamera,
            out min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightHost,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]),
            hostCamera,
            out max);
    }

    private RectTransform ResolveHighlightHost(RectTransform target)
    {
        if (target != null &&
            presentation != null &&
            target.IsChildOf(presentation.transform))
        {
            return defaultHighlightParent;
        }

        if (target != null &&
            mobileCanvasRoot != null &&
            mobileHighlightHost != null &&
            target.IsChildOf(mobileCanvasRoot))
        {
            return mobileHighlightHost;
        }

        if (mainHighlightHost != null)
            return mainHighlightHost;

        return defaultHighlightParent;
    }

    private void RestoreHighlightParent()
    {
        if (highlight == null ||
            defaultHighlightParent == null ||
            highlight.parent == defaultHighlightParent)
        {
            return;
        }

        highlight.SetParent(defaultHighlightParent, false);
        highlight.SetSiblingIndex(
            Mathf.Clamp(
                defaultHighlightSiblingIndex,
                0,
                Mathf.Max(0, defaultHighlightParent.childCount - 1)));
    }

    private static Camera GetCanvasCamera(RectTransform target)
    {
        Canvas canvas = target != null ? target.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private RectTransform GetObjectiveTutorialTarget()
    {
        return presentation != null && presentation.ObjectivePreviewTarget != null
            ? presentation.ObjectivePreviewTarget
            : objectiveIndicatorTarget;
    }

    private void ShowTutorialObjectivePreview()
    {
        RefreshTutorialObjectivePreview(true);
    }

    private void HideTutorialObjectivePreview()
    {
        presentation?.SetObjectivePreview(false);
    }

    private void RefreshTutorialObjectivePreview(bool forceShow = false)
    {
        if (presentation == null)
            return;

        bool shouldShow = forceShow ||
                          state == TutorialState.UloqIndicatorExplanation ||
                          state == TutorialState.WaitingForPickupAvailability ||
                          state == TutorialState.TargetIndicatorExplanation ||
                          state == TutorialState.WarmupIndicatorExplanation ||
                          state == TutorialState.LostUlakExplanation;
        if (!shouldShow)
            return;

        if (objectiveIndicator != null &&
            objectiveIndicator.TryGetTutorialSnapshot(
                out KopkariObjectiveIndicator.ObjectiveKind objectiveKind,
                out _,
                out Sprite icon,
                out Color color,
                out int distanceMeters))
        {
            presentation.SetObjectivePreview(
                true,
                GetTutorialText(GetObjectiveLabelId(objectiveKind)),
                distanceMeters + " m",
                icon,
                color);
        }
        else
        {
            presentation.SetObjectivePreview(false);
        }
    }

    private static int GetObjectiveLabelId(KopkariObjectiveIndicator.ObjectiveKind objectiveKind)
    {
        switch (objectiveKind)
        {
            case KopkariObjectiveIndicator.ObjectiveKind.Warmup:
                return RegistanTutorialTextIds.ObjectiveWarmup;
            case KopkariObjectiveIndicator.ObjectiveKind.Target:
                return RegistanTutorialTextIds.ObjectiveSalym;
            case KopkariObjectiveIndicator.ObjectiveKind.Uloq:
                return RegistanTutorialTextIds.ObjectiveUloq;
            default:
                return RegistanTutorialTextIds.None;
        }
    }

    private void CreatePresentation()
    {
        if (presentationPrefab == null)
        {
            Debug.LogError(
                $"[{nameof(RegistanTutorialController)}] Presentation prefab is not assigned.",
                this);
            return;
        }

        bool assignedFromScene = presentationPrefab.gameObject.scene.IsValid();
        presentation = assignedFromScene
            ? presentationPrefab
            : Instantiate(presentationPrefab);
        ownsPresentationInstance = !assignedFromScene;

        if (!presentation.HasRequiredReferences)
        {
            Debug.LogError(
                $"[{nameof(RegistanTutorialController)}] Presentation prefab has missing UI references.",
                presentation);
            if (ownsPresentationInstance)
                Destroy(presentation.gameObject);
            presentation = null;
            ownsPresentationInstance = false;
            return;
        }

        tutorialCanvasObject = presentation.gameObject;
        presentationRoot = presentation.PresentationRoot;
        blocker = presentation.Blocker;
        highlight = presentation.Highlight;
        defaultHighlightParent = highlight.parent as RectTransform;
        defaultHighlightSiblingIndex = highlight.GetSiblingIndex();
        popup = presentation.Popup;
        titleText = presentation.TitleText;
        descriptionText = presentation.DescriptionText;
        nextButton = presentation.NextButton;
        nextButtonText = presentation.NextButtonText;
        nextButton.onClick.AddListener(HandleNextClicked);
    }
}
