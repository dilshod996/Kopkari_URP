using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MalbersExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RacingTutorialController : MonoBehaviour
{
    public static bool IsTutorialActive { get; private set; }

    private enum TutorialState
    {
        None,
        LaunchExplanation,
        WaitingForLaunch,
        ControlExplanation,
        WaitingForLeftControl,
        WaitingForRightControl,
        CameraExplanation,
        WaitingForFirstPerson,
        WaitingForThirdPerson,
        WaitingForThirdPersonTransition,
        LookBackExplanation,
        WaitingForLookBackPress,
        WaitingForLookBackRelease,
        WaitingForFrontCameraTransition,
        SprintExplanation,
        WaitingForSprint,
        SprintSliderExplanation,
        ContextExplanation,
        WaitingForSpecialTrigger,
        SpecialTriggerExplanation,
        WaitingForFinish,
        FinishExplanation,
        WaitingForResult,
        ResultExplanation,
        Finished
    }

    private enum ContextLesson
    {
        Defense,
        SlowTrap,
        WebSnare,
        SprintFull,
        AutoSprint,
        WalkZoneHazard,
        WebSnareAffected,
        HitCounter,
        MiniMap,
        Leaderboard,
        SpecialGate
    }

    [Header("Racing References")]
    [SerializeField] private RacingController racingController;
    [SerializeField] private LaunchTimingMeterUI launchMeter;
    [SerializeField] private JoystickTurnMixer steering;
    [SerializeField] private TurnButton leftTurnButton;
    [SerializeField] private TurnButton rightTurnButton;
    [SerializeField] private ReinZone leftRein;
    [SerializeField] private ReinZone rightRein;

    [Header("UI Targets")]
    [SerializeField] private Button cameraSwitchButton;
    [SerializeField] private RectTransform lookBackTarget;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Slider sprintSlider;
    [SerializeField] private Button defenseButton;
    [SerializeField] private Button walkZoneButton;
    [SerializeField] private Button webSnareButton;
    [SerializeField] private Slider hitCountSlider;
    [SerializeField] private RectTransform miniMapTarget;
    [SerializeField] private RectTransform leaderboardTarget;
    [SerializeField] private RectTransform specialTriggerTarget;
    [SerializeField] private Button pauseButton;

    [Header("Highlight Hosts")]
    [Tooltip("Full-screen first-sibling host under the main UICanvas.")]
    [SerializeField] private RectTransform mainHighlightHost;
    [Tooltip("Full-screen first-sibling host under MobileUICanvas.")]
    [SerializeField] private RectTransform mobileHighlightHost;
    [Tooltip("MobileUICanvas root used to select the mobile highlight host.")]
    [SerializeField] private RectTransform mobileCanvasRoot;

    [Header("Presentation")]
    [SerializeField] private RacingTutorialPresentation presentationPrefab;
    [SerializeField] private UITargetPlacementSettings defaultPlacement = new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Auto,
        popupOffset = Vector2.zero,
        targetGap = 42f,
        canvasMargin = 32f,
        rebuildLayoutBeforePlace = true
    };

    [Header("Input Thresholds")]
    [SerializeField, Range(0.1f, 1f)] private float steeringThreshold = 0.3f;

    [Header("Transition Timing")]
    [SerializeField, Range(0.25f, 2f)] private float thirdPersonSettleDuration = 1f;
    [SerializeField, Range(0.25f, 2f)] private float lookBackReturnSettleDuration = 0.8f;
    [SerializeField, Min(0f)] private float specialPanelSettleDelay = 0.28f;
    [SerializeField, Min(0f)] private float boosterFlySettleDelay = 0.8f;
    [SerializeField, Range(2f, 10f)] private float cloudProgressWaitTimeout = 6f;

    private TutorialState state;
    private RacingControllerType selectedController;
    private RacingTutorialPresentation presentation;
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
    private bool ownsPresentation;
    private bool tutorialStarted;
    private bool standaloneControlTutorial;
    private bool controllerChangeTutorialActive;
    private bool pendingSpecialTriggerDuringControl;
    private bool previousPauseInteractable;
    private bool specialTriggerExplained;
    private bool resultPending;
    private TutorialState interruptedState;
    private TutorialState contextInterruptedState;
    private TutorialState controllerChangeInterruptedState;
    private readonly Queue<ContextLesson> contextLessonQueue = new();
    private readonly HashSet<ContextLesson> queuedContextLessons = new();
    private readonly HashSet<ContextLesson> explainedContextLessons = new();
    private ContextLesson currentContextLesson;
    private bool contextLessonActive;
    private Coroutine gameplayUiRoutine;
    private Coroutine startRoutine;
    private Coroutine launchUiRoutine;
    private Coroutine cameraTransitionRoutine;
    private Coroutine lookBackTransitionRoutine;
    private Coroutine specialUiRoutine;
    private Coroutine contextUiRoutine;
    private RacingTutorialProgress.CoreCheckpoint savedCheckpoint;

    private static readonly Color BackdropColor =
        new Color(0.015f, 0.025f, 0.05f, 0.72f);
    private static readonly Color PracticeBackdropColor =
        new Color(0f, 0f, 0f, 0.18f);

    private void Awake()
    {
        CreatePresentation();
        HidePresentation();
    }

    private void OnEnable()
    {
        RacingControllerSelecterUI.OnControllerSelected += HandleControllerSelected;
        RacingSettingsPanel.OnControllerApplied += HandleControllerApplied;
        LaunchTimingMeterUI.OnLaunchMeterStarted += HandleLaunchStarted;
        LaunchTimingMeterUI.OnLaunchFinishedGlobal += HandleLaunchFinished;
        RacingController.OnFirstPersonCamera += HandleFirstPersonCamera;
        UILookBackButton.OnCameraPressedState += HandleLookBackState;
        UIButtonActions.OnSprintStart += HandleSprintStarted;
        SpecialReachTriggerPoint.OnFirstAIRiderEntered += HandleFirstAIRiderEntered;
        RacingController.OnRacingFinished += HandleRaceFinished;
        PlayerDataManager.OnShowFinalPage += HandleResultReady;
        ReinZone.OnLeftReinUsed += HandleLeftReinUsed;
        ReinZone.OnRightReinUsed += HandleRightReinUsed;
        BoostersContainer.OnDefendAdded += HandleDefenseAdded;
        BoostersContainer.OnWalkZoneAdded += HandleWalkZoneAdded;
        BoostersContainer.OnWebSnareAdded += HandleWebSnareAdded;
        Booster.OnSprintFull += HandleSprintFull;
        BoostersContainer.OnAutoSprintBoostStart += HandleAutoSprintStarted;
        BoostersContainer.OnWalkZoneDamaged += HandleWalkZoneDamaged;
        BoostersContainer.OnWebSnareDamaged += HandleWebSnareDamaged;
        HorseMine.OnObstacleTouchedEvent += HandleRacingObstacleTouched;
        RacingLeaderboard.OnLeaderboardShown += HandleLeaderboardShown;
    }

    private void OnDisable()
    {
        RacingControllerSelecterUI.OnControllerSelected -= HandleControllerSelected;
        RacingSettingsPanel.OnControllerApplied -= HandleControllerApplied;
        LaunchTimingMeterUI.OnLaunchMeterStarted -= HandleLaunchStarted;
        LaunchTimingMeterUI.OnLaunchFinishedGlobal -= HandleLaunchFinished;
        RacingController.OnFirstPersonCamera -= HandleFirstPersonCamera;
        UILookBackButton.OnCameraPressedState -= HandleLookBackState;
        UIButtonActions.OnSprintStart -= HandleSprintStarted;
        SpecialReachTriggerPoint.OnFirstAIRiderEntered -= HandleFirstAIRiderEntered;
        RacingController.OnRacingFinished -= HandleRaceFinished;
        PlayerDataManager.OnShowFinalPage -= HandleResultReady;
        ReinZone.OnLeftReinUsed -= HandleLeftReinUsed;
        ReinZone.OnRightReinUsed -= HandleRightReinUsed;
        BoostersContainer.OnDefendAdded -= HandleDefenseAdded;
        BoostersContainer.OnWalkZoneAdded -= HandleWalkZoneAdded;
        BoostersContainer.OnWebSnareAdded -= HandleWebSnareAdded;
        Booster.OnSprintFull -= HandleSprintFull;
        BoostersContainer.OnAutoSprintBoostStart -= HandleAutoSprintStarted;
        BoostersContainer.OnWalkZoneDamaged -= HandleWalkZoneDamaged;
        BoostersContainer.OnWebSnareDamaged -= HandleWebSnareDamaged;
        HorseMine.OnObstacleTouchedEvent -= HandleRacingObstacleTouched;
        RacingLeaderboard.OnLeaderboardShown -= HandleLeaderboardShown;

        if (gameplayUiRoutine != null)
        {
            StopCoroutine(gameplayUiRoutine);
            gameplayUiRoutine = null;
        }
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
        if (launchUiRoutine != null)
        {
            StopCoroutine(launchUiRoutine);
            launchUiRoutine = null;
        }
        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }
        if (lookBackTransitionRoutine != null)
        {
            StopCoroutine(lookBackTransitionRoutine);
            lookBackTransitionRoutine = null;
        }
        if (specialUiRoutine != null)
        {
            StopCoroutine(specialUiRoutine);
            specialUiRoutine = null;
        }
        if (contextUiRoutine != null)
        {
            StopCoroutine(contextUiRoutine);
            contextUiRoutine = null;
        }

        FinishTutorial();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNextClicked);

        RestoreHighlightParent();

        if (ownsPresentation && presentation != null)
            Destroy(presentation.gameObject);
    }

    private void Update()
    {
        if (!IsTutorialActive)
            return;

        if (state == TutorialState.WaitingForLeftControl &&
            selectedController == RacingControllerType.Buttons &&
            leftTurnButton != null && leftTurnButton.IsPressed)
        {
            ShowRightControlPractice();
        }
        else if (state == TutorialState.WaitingForRightControl &&
                 selectedController == RacingControllerType.Buttons &&
                 rightTurnButton != null && rightTurnButton.IsPressed)
        {
            CompleteControlPractice();
        }
        else if (state == TutorialState.WaitingForLeftControl &&
                 selectedController == RacingControllerType.Tilt &&
                 steering != null && steering.AxisValue.x <= -steeringThreshold)
        {
            ShowRightControlPractice();
        }
        else if (state == TutorialState.WaitingForRightControl &&
                 selectedController == RacingControllerType.Tilt &&
                 steering != null && steering.AxisValue.x >= steeringThreshold)
        {
            CompleteControlPractice();
        }
    }

    private void HandleControllerSelected(RacingControllerType controllerType)
    {
        selectedController = controllerType;
    }

    private void HandleControllerApplied(RacingControllerType controllerType)
    {
        selectedController = controllerType;

        if (presentation == null ||
            RacingTutorialProgress.IsControllerLessonCompleted(controllerType))
        {
            return;
        }

        if (!IsTutorialActive)
        {
            BeginTutorial();
            standaloneControlTutorial = true;
            ShowControlExplanation();
            return;
        }

        if (state == TutorialState.ControlExplanation ||
            state == TutorialState.WaitingForLeftControl ||
            state == TutorialState.WaitingForRightControl)
        {
            ShowControlExplanation();
            return;
        }

        controllerChangeInterruptedState = state;
        controllerChangeTutorialActive = true;
        ShowControlExplanation();
    }

    private void HandleLaunchStarted()
    {
        if (presentation == null)
            return;

        selectedController =
            RacingControllerSelecterUI.GetSavedControllerOrDefault();
        BeginTutorial();
        state = TutorialState.None;
        launchMeter?.SetTutorialPaused(true);

        CanvasGroup meterGroup = launchMeter != null
            ? launchMeter.meterContainer
            : null;
        if (meterGroup != null)
        {
            meterGroup.DOKill();
            meterGroup.transform.DOKill();
            meterGroup.gameObject.SetActive(true);
            meterGroup.alpha = 1f;
            meterGroup.transform.localScale = Vector3.one;
        }

        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        if (startRoutine != null)
            StopCoroutine(startRoutine);
        startRoutine = StartCoroutine(StartFromSavedProgress());
    }

    private IEnumerator StartFromSavedProgress()
    {
        if (!RacingTutorialProgress.HasAnyLocalData &&
            DataManager.Instance != null)
        {
            DataManager.Instance.EnsureRacingTutorialStateLoaded();
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(2f, cloudProgressWaitTimeout);
            while (!DataManager.Instance.IsRacingTutorialStateLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        RacingTutorialProgress.State progress = RacingTutorialProgress.LoadLocal();
        savedCheckpoint = progress.Checkpoint;
        ApplySavedContextProgress(progress.Context);

        if (progress.Completed)
        {
            startRoutine = null;
            FinishCompletedTutorialSetup();
            yield break;
        }

        yield return null;
        Canvas.ForceUpdateCanvases();
        startRoutine = null;

        if (savedCheckpoint == RacingTutorialProgress.CoreCheckpoint.LaunchMeter)
        {
            state = TutorialState.LaunchExplanation;
            if (launchUiRoutine != null)
                StopCoroutine(launchUiRoutine);
            launchUiRoutine = StartCoroutine(ShowLaunchExplanationAfterLayout());
            yield break;
        }

        launchMeter?.SetTutorialPaused(false);
        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
    }

    private IEnumerator ShowLaunchExplanationAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        launchUiRoutine = null;

        if (state != TutorialState.LaunchExplanation)
            yield break;

        ShowPresentation(
            launchMeter != null && launchMeter.meterContainer != null
                ? launchMeter.meterContainer.transform as RectTransform
                : null,
            RacingTutorialTextIds.StartBoost,
            RacingTutorialTextIds.StartBoostDescription,
            RacingTutorialTextIds.TryIt,
            true,
            true);
    }

    private void HandleLaunchFinished(
        LaunchTimingMeterUI.LaunchResult result,
        float boostMultiplier,
        float boostDuration)
    {
        if (!IsTutorialActive)
            return;

        if (gameplayUiRoutine != null)
            StopCoroutine(gameplayUiRoutine);
        gameplayUiRoutine = StartCoroutine(ResumeAfterLaunch());
    }

    private IEnumerator ResumeAfterLaunch()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        gameplayUiRoutine = null;

        switch (savedCheckpoint)
        {
            case RacingTutorialProgress.CoreCheckpoint.LaunchMeter:
            case RacingTutorialProgress.CoreCheckpoint.Controls:
                ShowControlExplanation();
                break;
            case RacingTutorialProgress.CoreCheckpoint.Camera:
                ShowCameraExplanation();
                break;
            case RacingTutorialProgress.CoreCheckpoint.LookBack:
                ShowLookBackExplanation();
                break;
            case RacingTutorialProgress.CoreCheckpoint.Sprint:
                ShowSprintExplanation();
                break;
            default:
                state = specialTriggerExplained
                    ? TutorialState.WaitingForFinish
                    : TutorialState.WaitingForSpecialTrigger;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                QueueContextLesson(ContextLesson.MiniMap);
                TryShowNextContextLesson();
                break;
        }
    }

    private void ShowControlExplanation()
    {
        state = TutorialState.ControlExplanation;

        switch (selectedController)
        {
            case RacingControllerType.Reins:
                ShowPausedStep(
                    leftRein != null ? leftRein.transform as RectTransform : null,
                    RacingTutorialTextIds.ReinsControl,
                    RacingTutorialTextIds.ReinsControlDescription,
                    RacingTutorialTextIds.Practice);
                break;

            case RacingControllerType.Tilt:
                ShowPausedStep(
                    steering != null ? steering.transform as RectTransform : null,
                    RacingTutorialTextIds.TiltControl,
                    RacingTutorialTextIds.TiltControlDescription,
                    RacingTutorialTextIds.Practice);
                break;

            default:
                selectedController = RacingControllerType.Buttons;
                ShowPausedStep(
                    leftTurnButton != null ? leftTurnButton.transform as RectTransform : null,
                    RacingTutorialTextIds.ButtonControl,
                    RacingTutorialTextIds.ButtonControlDescription,
                    RacingTutorialTextIds.Practice);
                break;
        }
    }

    private void BeginControlPractice()
    {
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        state = TutorialState.WaitingForLeftControl;

        int descriptionId = selectedController switch
        {
            RacingControllerType.Reins => RacingTutorialTextIds.PullLeftRein,
            RacingControllerType.Tilt => RacingTutorialTextIds.TiltLeft,
            _ => RacingTutorialTextIds.HoldLeftButton
        };

        RectTransform target = selectedController switch
        {
            RacingControllerType.Reins =>
                leftRein != null ? leftRein.transform as RectTransform : null,
            RacingControllerType.Buttons =>
                leftTurnButton != null ? leftTurnButton.transform as RectTransform : null,
            _ => steering != null ? steering.transform as RectTransform : null
        };

        ShowPractice(target, RacingTutorialTextIds.YourTurn, descriptionId);
    }

    private void ShowRightControlPractice()
    {
        state = TutorialState.WaitingForRightControl;

        int descriptionId = selectedController switch
        {
            RacingControllerType.Reins => RacingTutorialTextIds.PullRightRein,
            RacingControllerType.Tilt => RacingTutorialTextIds.TiltRight,
            _ => RacingTutorialTextIds.HoldRightButton
        };

        RectTransform target = selectedController switch
        {
            RacingControllerType.Reins =>
                rightRein != null ? rightRein.transform as RectTransform : null,
            RacingControllerType.Buttons =>
                rightTurnButton != null ? rightTurnButton.transform as RectTransform : null,
            _ => steering != null ? steering.transform as RectTransform : null
        };

        ShowPractice(target, RacingTutorialTextIds.Good, descriptionId);
    }

    private void HandleLeftReinUsed()
    {
        if (IsTutorialActive &&
            selectedController == RacingControllerType.Reins &&
            state == TutorialState.WaitingForLeftControl)
        {
            ShowRightControlPractice();
        }
    }

    private void HandleRightReinUsed()
    {
        if (IsTutorialActive &&
            selectedController == RacingControllerType.Reins &&
            state == TutorialState.WaitingForRightControl)
        {
            CompleteControlPractice();
        }
    }

    private void CompleteControlPractice()
    {
        RacingTutorialProgress.CompleteControllerLesson(selectedController);

        if (standaloneControlTutorial)
        {
            standaloneControlTutorial = false;
            FinishTutorial();
            return;
        }

        if (controllerChangeTutorialActive)
        {
            TutorialState resumeState = controllerChangeInterruptedState;
            controllerChangeTutorialActive = false;
            controllerChangeInterruptedState = TutorialState.None;
            RestoreTutorialState(resumeState);

            if (pendingSpecialTriggerDuringControl)
            {
                pendingSpecialTriggerDuringControl = false;
                HandleFirstAIRiderEntered();
                return;
            }

            TryShowNextContextLesson();
            return;
        }

        CompleteCoreStep(
            Constants.RacingTutorial.Controls,
            RacingTutorialProgress.CoreCheckpoint.Camera);
        ShowCameraExplanation();
    }

    private void ShowCameraExplanation()
    {
        state = TutorialState.CameraExplanation;
        ShowPausedStep(
            cameraSwitchButton != null
                ? cameraSwitchButton.transform as RectTransform
                : null,
            RacingTutorialTextIds.ChangeCamera,
            RacingTutorialTextIds.ChangeCameraDescription,
            RacingTutorialTextIds.TryIt);
    }

    private void HandleFirstPersonCamera(bool firstPerson)
    {
        if (!IsTutorialActive)
            return;

        if (state == TutorialState.WaitingForFirstPerson && firstPerson)
        {
            state = TutorialState.WaitingForThirdPerson;
            ShowPractice(
                cameraSwitchButton != null
                    ? cameraSwitchButton.transform as RectTransform
                    : null,
                RacingTutorialTextIds.ThirdPerson,
                RacingTutorialTextIds.ThirdPersonDescription);
        }
        else if (state == TutorialState.WaitingForThirdPerson && !firstPerson)
        {
            CompleteCoreStep(
                Constants.RacingTutorial.Camera,
                RacingTutorialProgress.CoreCheckpoint.LookBack);
            BeginThirdPersonTransitionWait();
        }
    }

    private void BeginThirdPersonTransitionWait()
    {
        state = TutorialState.WaitingForThirdPersonTransition;
        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        if (cameraTransitionRoutine != null)
            StopCoroutine(cameraTransitionRoutine);
        cameraTransitionRoutine = StartCoroutine(WaitForThirdPersonCamera());
    }

    private IEnumerator WaitForThirdPersonCamera()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.25f, thirdPersonSettleDuration));
        cameraTransitionRoutine = null;

        if (state == TutorialState.WaitingForThirdPersonTransition)
            ShowLookBackExplanation();
    }

    private void ShowLookBackExplanation()
    {
        state = TutorialState.LookBackExplanation;
        ShowPausedStep(
            lookBackTarget,
            RacingTutorialTextIds.LookBehind,
            RacingTutorialTextIds.LookBehindDescription,
            RacingTutorialTextIds.TryIt);
    }

    private void HandleLookBackState(bool pressed)
    {
        if (!IsTutorialActive)
            return;

        if (state == TutorialState.WaitingForLookBackPress && pressed)
        {
            state = TutorialState.WaitingForLookBackRelease;
            ShowPractice(
                lookBackTarget,
                RacingTutorialTextIds.LookingBack,
                RacingTutorialTextIds.ReleaseLookBackDescription);
        }
        else if (state == TutorialState.WaitingForLookBackRelease && !pressed)
        {
            CompleteCoreStep(
                Constants.RacingTutorial.LookBack,
                RacingTutorialProgress.CoreCheckpoint.Sprint);
            BeginFrontCameraTransitionWait();
        }
    }

    private void BeginFrontCameraTransitionWait()
    {
        state = TutorialState.WaitingForFrontCameraTransition;
        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        if (lookBackTransitionRoutine != null)
            StopCoroutine(lookBackTransitionRoutine);
        lookBackTransitionRoutine = StartCoroutine(WaitForFrontCamera());
    }

    private IEnumerator WaitForFrontCamera()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.25f, lookBackReturnSettleDuration));
        lookBackTransitionRoutine = null;

        if (state == TutorialState.WaitingForFrontCameraTransition)
            ShowSprintExplanation();
    }

    private void ShowSprintExplanation()
    {
        state = TutorialState.SprintExplanation;
        ShowPausedStep(
            sprintButton != null ? sprintButton.transform as RectTransform : null,
            RacingTutorialTextIds.Sprint,
            RacingTutorialTextIds.SprintDescription,
            RacingTutorialTextIds.TryIt);
    }

    private void HandleSprintStarted()
    {
        if (!IsTutorialActive || state != TutorialState.WaitingForSprint)
            return;

        state = TutorialState.SprintSliderExplanation;
        ShowPausedStep(
            sprintSlider != null ? sprintSlider.transform as RectTransform : null,
            RacingTutorialTextIds.SprintStamina,
            RacingTutorialTextIds.SprintStaminaDescription,
            RacingTutorialTextIds.Continue);
    }

    private void HandleDefenseAdded(int count)
    {
        QueueContextLesson(ContextLesson.Defense);
    }

    private void HandleWalkZoneAdded(int count)
    {
        QueueContextLesson(ContextLesson.SlowTrap);
    }

    private void HandleWebSnareAdded(int count)
    {
        QueueContextLesson(ContextLesson.WebSnare);
    }

    private void HandleSprintFull()
    {
        QueueContextLesson(ContextLesson.SprintFull);
    }

    private void HandleAutoSprintStarted()
    {
        QueueContextLesson(ContextLesson.AutoSprint);
    }

    private void HandleWalkZoneDamaged(bool damaged)
    {
        if (damaged)
            QueueContextLesson(ContextLesson.WalkZoneHazard);
    }

    private void HandleWebSnareDamaged(bool damaged)
    {
        if (damaged)
            QueueContextLesson(ContextLesson.WebSnareAffected);
    }

    private void HandleRacingObstacleTouched()
    {
        QueueContextLesson(ContextLesson.HitCounter);
    }

    private void HandleLeaderboardShown()
    {
        QueueContextLesson(ContextLesson.Leaderboard);
    }

    private void QueueContextLesson(ContextLesson lesson)
    {
        if (!IsTutorialActive ||
            standaloneControlTutorial ||
            explainedContextLessons.Contains(lesson) ||
            queuedContextLessons.Contains(lesson) ||
            (contextLessonActive && currentContextLesson == lesson))
        {
            return;
        }

        queuedContextLessons.Add(lesson);
        contextLessonQueue.Enqueue(lesson);
        TryShowNextContextLesson();
    }

    private void TryShowNextContextLesson()
    {
        if (!IsTutorialActive ||
            standaloneControlTutorial ||
            controllerChangeTutorialActive ||
            contextLessonActive ||
            contextLessonQueue.Count == 0 ||
            state == TutorialState.SpecialTriggerExplanation ||
            state == TutorialState.WaitingForThirdPersonTransition ||
            state == TutorialState.WaitingForFrontCameraTransition ||
            state >= TutorialState.FinishExplanation)
        {
            return;
        }

        currentContextLesson = contextLessonQueue.Dequeue();
        queuedContextLessons.Remove(currentContextLesson);
        explainedContextLessons.Add(currentContextLesson);
        contextLessonActive = true;
        contextInterruptedState = state;
        state = TutorialState.ContextExplanation;

        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        contextUiRoutine = StartCoroutine(ShowContextAfterVisualSettles());
    }

    private IEnumerator ShowContextAfterVisualSettles()
    {
        float settleDelay = GetContextSettleDelay(currentContextLesson);
        if (settleDelay > 0f)
            yield return new WaitForSecondsRealtime(settleDelay);

        contextUiRoutine = null;
        ShowCurrentContextLesson();
    }

    private float GetContextSettleDelay(ContextLesson lesson)
    {
        switch (lesson)
        {
            case ContextLesson.HitCounter:
                return 0.15f;
            case ContextLesson.MiniMap:
            case ContextLesson.Leaderboard:
                return 0.05f;
            default:
                return boosterFlySettleDelay;
        }
    }

    private void ShowCurrentContextLesson()
    {
        if (!contextLessonActive)
            return;

        state = TutorialState.ContextExplanation;
        GetContextLessonContent(
            currentContextLesson,
            out RectTransform target,
            out int titleId,
            out int descriptionId);

        ShowPresentation(
            target,
            titleId,
            descriptionId,
            RacingTutorialTextIds.Continue,
            true,
            true);
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
    }

    private void GetContextLessonContent(
        ContextLesson lesson,
        out RectTransform target,
        out int titleId,
        out int descriptionId)
    {
        switch (lesson)
        {
            case ContextLesson.Defense:
                target = defenseButton != null
                    ? defenseButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.ShieldAdded;
                descriptionId = RacingTutorialTextIds.ShieldAddedDescription;
                break;

            case ContextLesson.SlowTrap:
                target = walkZoneButton != null
                    ? walkZoneButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.SlowTrapAdded;
                descriptionId = RacingTutorialTextIds.SlowTrapAddedDescription;
                break;

            case ContextLesson.WebSnare:
                target = webSnareButton != null
                    ? webSnareButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.WebSnareAdded;
                descriptionId = RacingTutorialTextIds.WebSnareAddedDescription;
                break;

            case ContextLesson.SprintFull:
                target = sprintSlider != null
                    ? sprintSlider.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.SprintRefilled;
                descriptionId = RacingTutorialTextIds.SprintRefilledDescription;
                break;

            case ContextLesson.AutoSprint:
                target = sprintButton != null
                    ? sprintButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.AutoSprint;
                descriptionId = RacingTutorialTextIds.AutoSprintDescription;
                break;

            case ContextLesson.WalkZoneHazard:
                target = defenseButton != null
                    ? defenseButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.SlowingZone;
                descriptionId = RacingTutorialTextIds.SlowingZoneDescription;
                break;

            case ContextLesson.WebSnareAffected:
                target = defenseButton != null
                    ? defenseButton.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.RivalAttack;
                descriptionId = RacingTutorialTextIds.RivalAttackDescription;
                break;

            case ContextLesson.HitCounter:
                target = hitCountSlider != null
                    ? hitCountSlider.transform as RectTransform
                    : null;
                titleId = RacingTutorialTextIds.ObstacleDamage;
                descriptionId = RacingTutorialTextIds.ObstacleDamageDescription;
                break;

            case ContextLesson.MiniMap:
                target = miniMapTarget;
                titleId = RacingTutorialTextIds.RaceMap;
                descriptionId = RacingTutorialTextIds.RaceMapDescription;
                break;

            default:
                target = leaderboardTarget;
                titleId = RacingTutorialTextIds.RaceLeaderboard;
                descriptionId = RacingTutorialTextIds.RaceLeaderboardDescription;
                break;
        }
    }

    private void HandleFirstAIRiderEntered()
    {
        if (standaloneControlTutorial)
            return;

        if (controllerChangeTutorialActive)
        {
            pendingSpecialTriggerDuringControl = true;
            return;
        }

        if (!IsTutorialActive ||
            specialTriggerExplained ||
            state >= TutorialState.FinishExplanation)
        {
            return;
        }

        specialTriggerExplained = true;
        if (contextUiRoutine != null)
        {
            StopCoroutine(contextUiRoutine);
            contextUiRoutine = null;
        }
        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }
        if (lookBackTransitionRoutine != null)
        {
            StopCoroutine(lookBackTransitionRoutine);
            lookBackTransitionRoutine = null;
        }
        interruptedState = state;
        state = TutorialState.SpecialTriggerExplanation;
        HidePresentation();
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        specialUiRoutine = StartCoroutine(ShowSpecialTriggerAfterPanelSettles());
    }

    private IEnumerator ShowSpecialTriggerAfterPanelSettles()
    {
        if (specialPanelSettleDelay > 0f)
            yield return new WaitForSecondsRealtime(specialPanelSettleDelay);

        specialUiRoutine = null;
        ShowPresentation(
            specialTriggerTarget,
            RacingTutorialTextIds.SpecialGate,
            RacingTutorialTextIds.SpecialGateDescription,
            RacingTutorialTextIds.Ride,
            true,
            true);
    }

    private void HandleRaceFinished(int rank)
    {
        if (standaloneControlTutorial)
        {
            FinishTutorial();
            return;
        }

        if (controllerChangeTutorialActive)
        {
            controllerChangeTutorialActive = false;
            controllerChangeInterruptedState = TutorialState.None;
        }

        if (!IsTutorialActive)
            return;

        if (savedCheckpoint >= RacingTutorialProgress.CoreCheckpoint.Result)
        {
            state = TutorialState.WaitingForResult;
            HidePresentation();
            TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
            if (resultPending)
                ShowResultExplanation();
            return;
        }

        state = TutorialState.FinishExplanation;
        ShowPausedStep(
            null,
            RacingTutorialTextIds.RaceFinished,
            RacingTutorialTextIds.RaceFinishedDescription,
            RacingTutorialTextIds.Continue);
    }

    private void HandleResultReady()
    {
        if (!IsTutorialActive)
            return;

        resultPending = true;
        if (state == TutorialState.WaitingForResult)
            ShowResultExplanation();
    }

    private void ShowResultExplanation()
    {
        resultPending = false;
        state = TutorialState.ResultExplanation;
        ShowPausedStep(
            null,
            RacingTutorialTextIds.Results,
            RacingTutorialTextIds.ResultsDescription,
            RacingTutorialTextIds.Done);
    }

    private void HandleNextClicked()
    {
        switch (state)
        {
            case TutorialState.LaunchExplanation:
                CompleteCoreStep(
                    Constants.RacingTutorial.LaunchMeter,
                    RacingTutorialProgress.CoreCheckpoint.Controls);
                launchMeter?.SetTutorialPaused(false);
                state = TutorialState.WaitingForLaunch;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                break;

            case TutorialState.ControlExplanation:
                BeginControlPractice();
                break;

            case TutorialState.CameraExplanation:
                state = TutorialState.WaitingForFirstPerson;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                break;

            case TutorialState.LookBackExplanation:
                state = TutorialState.WaitingForLookBackPress;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                TryShowNextContextLesson();
                break;

            case TutorialState.SprintExplanation:
                state = TutorialState.WaitingForSprint;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                TryShowNextContextLesson();
                break;

            case TutorialState.SprintSliderExplanation:
                CompleteCoreStep(
                    Constants.RacingTutorial.Sprint,
                    RacingTutorialProgress.CoreCheckpoint.Racing);
                state = specialTriggerExplained
                    ? TutorialState.WaitingForFinish
                    : TutorialState.WaitingForSpecialTrigger;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                QueueContextLesson(ContextLesson.MiniMap);
                break;

            case TutorialState.SpecialTriggerExplanation:
                CompleteContextStep(ContextLesson.SpecialGate);
                ResumeInterruptedStep();
                break;

            case TutorialState.ContextExplanation:
                CompleteCurrentContextLesson();
                break;

            case TutorialState.FinishExplanation:
                CompleteCoreStep(
                    Constants.RacingTutorial.Finish,
                    RacingTutorialProgress.CoreCheckpoint.Result);
                state = TutorialState.WaitingForResult;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                if (resultPending)
                    ShowResultExplanation();
                break;

            case TutorialState.ResultExplanation:
                RacingTutorialProgress.CompleteTutorial();
                FinishTutorial();
                break;
        }
    }

    private void BeginTutorial()
    {
        if (IsTutorialActive)
            return;

        IsTutorialActive = true;
        tutorialStarted = true;
        specialTriggerExplained = false;
        resultPending = false;
        interruptedState = TutorialState.None;
        contextInterruptedState = TutorialState.None;
        controllerChangeInterruptedState = TutorialState.None;
        controllerChangeTutorialActive = false;
        pendingSpecialTriggerDuringControl = false;
        contextLessonActive = false;
        contextLessonQueue.Clear();
        queuedContextLessons.Clear();
        explainedContextLessons.Clear();
        if (pauseButton != null)
        {
            previousPauseInteractable = pauseButton.interactable;
            pauseButton.interactable = false;
        }
    }

    private void FinishTutorial()
    {
        if (!tutorialStarted)
            return;

        TutorialPauseController.ResumeAll();
        launchMeter?.SetTutorialPaused(false);
        HidePresentation();

        if (contextUiRoutine != null)
        {
            StopCoroutine(contextUiRoutine);
            contextUiRoutine = null;
        }
        contextLessonQueue.Clear();
        queuedContextLessons.Clear();
        contextLessonActive = false;

        if (pauseButton != null)
            pauseButton.interactable = previousPauseInteractable;

        IsTutorialActive = false;
        tutorialStarted = false;
        standaloneControlTutorial = false;
        controllerChangeTutorialActive = false;
        controllerChangeInterruptedState = TutorialState.None;
        pendingSpecialTriggerDuringControl = false;
        state = TutorialState.Finished;
    }

    private void FinishCompletedTutorialSetup()
    {
        launchMeter?.SetTutorialPaused(false);
        TutorialPauseController.ResumeAll();
        HidePresentation();

        if (pauseButton != null)
            pauseButton.interactable = previousPauseInteractable;

        IsTutorialActive = false;
        tutorialStarted = false;
        standaloneControlTutorial = false;
        controllerChangeTutorialActive = false;
        controllerChangeInterruptedState = TutorialState.None;
        pendingSpecialTriggerDuringControl = false;
        state = TutorialState.Finished;
    }

    private void ApplySavedContextProgress(
        RacingTutorialProgress.ContextLesson context)
    {
        explainedContextLessons.Clear();
        foreach (ContextLesson lesson in System.Enum.GetValues(typeof(ContextLesson)))
        {
            RacingTutorialProgress.ContextLesson savedLesson =
                GetSavedContextLesson(lesson);
            if (savedLesson != RacingTutorialProgress.ContextLesson.None &&
                (context & savedLesson) == savedLesson)
            {
                explainedContextLessons.Add(lesson);
            }
        }

        specialTriggerExplained =
            (context & RacingTutorialProgress.ContextLesson.SpecialGate) != 0;
    }

    private void CompleteCoreStep(
        string stepKey,
        RacingTutorialProgress.CoreCheckpoint nextCheckpoint)
    {
        RacingTutorialProgress.CompleteCoreStep(stepKey, nextCheckpoint);
        savedCheckpoint = RacingTutorialProgress.LoadLocal().Checkpoint;
    }

    private void CompleteContextStep(ContextLesson lesson)
    {
        RacingTutorialProgress.ContextLesson savedLesson =
            GetSavedContextLesson(lesson);
        if (savedLesson == RacingTutorialProgress.ContextLesson.None)
            return;

        RacingTutorialProgress.CompleteContextStep(
            GetContextStepKey(lesson),
            savedLesson);
    }

    private static RacingTutorialProgress.ContextLesson GetSavedContextLesson(
        ContextLesson lesson)
    {
        return lesson switch
        {
            ContextLesson.Defense => RacingTutorialProgress.ContextLesson.Defense,
            ContextLesson.SlowTrap => RacingTutorialProgress.ContextLesson.SlowTrap,
            ContextLesson.WebSnare => RacingTutorialProgress.ContextLesson.WebSnare,
            ContextLesson.SprintFull => RacingTutorialProgress.ContextLesson.SprintFull,
            ContextLesson.AutoSprint => RacingTutorialProgress.ContextLesson.AutoSprint,
            ContextLesson.WalkZoneHazard =>
                RacingTutorialProgress.ContextLesson.WalkZoneHazard,
            ContextLesson.WebSnareAffected =>
                RacingTutorialProgress.ContextLesson.WebSnareAffected,
            ContextLesson.HitCounter => RacingTutorialProgress.ContextLesson.HitCounter,
            ContextLesson.MiniMap => RacingTutorialProgress.ContextLesson.MiniMap,
            ContextLesson.Leaderboard =>
                RacingTutorialProgress.ContextLesson.Leaderboard,
            ContextLesson.SpecialGate =>
                RacingTutorialProgress.ContextLesson.SpecialGate,
            _ => RacingTutorialProgress.ContextLesson.None
        };
    }

    private static string GetContextStepKey(ContextLesson lesson)
    {
        return lesson switch
        {
            ContextLesson.Defense => Constants.RacingTutorial.Defense,
            ContextLesson.SlowTrap => Constants.RacingTutorial.SlowTrap,
            ContextLesson.WebSnare => Constants.RacingTutorial.WebSnare,
            ContextLesson.SprintFull => Constants.RacingTutorial.SprintFull,
            ContextLesson.AutoSprint => Constants.RacingTutorial.AutoSprint,
            ContextLesson.WalkZoneHazard => Constants.RacingTutorial.WalkZoneHazard,
            ContextLesson.WebSnareAffected =>
                Constants.RacingTutorial.WebSnareAffected,
            ContextLesson.HitCounter => Constants.RacingTutorial.HitCounter,
            ContextLesson.MiniMap => Constants.RacingTutorial.MiniMap,
            ContextLesson.Leaderboard => Constants.RacingTutorial.Leaderboard,
            ContextLesson.SpecialGate => Constants.RacingTutorial.SpecialGate,
            _ => string.Empty
        };
    }

    [ContextMenu("Delete Racing Tutorial Progress For Testing")]
    public void DeleteRacingTutorialProgressForTesting()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.DeleteRacingTutorialProgressForTesting();
        else
            RacingTutorialProgress.DeleteAllLocalProgress();
    }

    private void ResumeInterruptedStep()
    {
        TutorialState resumeState = interruptedState;
        interruptedState = TutorialState.None;
        RestoreTutorialState(resumeState);

        if (resumeState != TutorialState.ContextExplanation)
            TryShowNextContextLesson();
    }

    private void CompleteCurrentContextLesson()
    {
        if (!contextLessonActive)
            return;

        CompleteContextStep(currentContextLesson);
        contextLessonActive = false;
        TutorialState resumeState = contextInterruptedState;
        contextInterruptedState = TutorialState.None;
        RestoreTutorialState(resumeState);
        TryShowNextContextLesson();
    }

    private void RestoreTutorialState(TutorialState resumeState)
    {
        switch (resumeState)
        {
            case TutorialState.ContextExplanation:
                ShowCurrentContextLesson();
                break;
            case TutorialState.ControlExplanation:
                ShowControlExplanation();
                break;
            case TutorialState.WaitingForLeftControl:
                BeginControlPractice();
                break;
            case TutorialState.WaitingForRightControl:
                ShowRightControlPractice();
                break;
            case TutorialState.CameraExplanation:
                ShowCameraExplanation();
                break;
            case TutorialState.WaitingForFirstPerson:
                state = TutorialState.WaitingForFirstPerson;
                ShowPractice(
                    cameraSwitchButton != null
                        ? cameraSwitchButton.transform as RectTransform
                        : null,
                    RacingTutorialTextIds.YourTurn,
                    RacingTutorialTextIds.FirstPersonDescription);
                break;
            case TutorialState.WaitingForThirdPerson:
                state = TutorialState.WaitingForThirdPerson;
                ShowPractice(
                    cameraSwitchButton != null
                        ? cameraSwitchButton.transform as RectTransform
                        : null,
                    RacingTutorialTextIds.ThirdPerson,
                    RacingTutorialTextIds.ThirdPersonDescription);
                break;
            case TutorialState.WaitingForThirdPersonTransition:
                BeginThirdPersonTransitionWait();
                break;
            case TutorialState.LookBackExplanation:
                ShowLookBackExplanation();
                break;
            case TutorialState.WaitingForLookBackPress:
                state = TutorialState.WaitingForLookBackPress;
                ShowPractice(
                    lookBackTarget,
                    RacingTutorialTextIds.YourTurn,
                    RacingTutorialTextIds.LookBehindDescription);
                break;
            case TutorialState.WaitingForLookBackRelease:
                state = TutorialState.WaitingForLookBackRelease;
                ShowPractice(
                    lookBackTarget,
                    RacingTutorialTextIds.LookingBack,
                    RacingTutorialTextIds.ReleaseLookBackDescription);
                break;
            case TutorialState.WaitingForFrontCameraTransition:
                BeginFrontCameraTransitionWait();
                break;
            case TutorialState.SprintExplanation:
                ShowSprintExplanation();
                break;
            case TutorialState.WaitingForSprint:
                state = TutorialState.WaitingForSprint;
                ShowPractice(
                    sprintButton != null
                        ? sprintButton.transform as RectTransform
                        : null,
                    RacingTutorialTextIds.HoldSprint,
                    RacingTutorialTextIds.HoldSprintDescription);
                break;
            case TutorialState.SprintSliderExplanation:
                state = TutorialState.SprintSliderExplanation;
                ShowPausedStep(
                    sprintSlider != null
                        ? sprintSlider.transform as RectTransform
                        : null,
                    RacingTutorialTextIds.SprintStamina,
                    RacingTutorialTextIds.SprintStaminaDescription,
                    RacingTutorialTextIds.Continue);
                break;
            case TutorialState.WaitingForSpecialTrigger:
                state = specialTriggerExplained
                    ? TutorialState.WaitingForFinish
                    : TutorialState.WaitingForSpecialTrigger;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                break;
            case TutorialState.WaitingForFinish:
                state = TutorialState.WaitingForFinish;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                break;
            default:
                state = resumeState;
                HidePresentation();
                TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
                break;
        }
    }

    private void ShowPausedStep(
        RectTransform target,
        int titleId,
        int descriptionId,
        int buttonLabelId)
    {
        ShowPresentation(
            target,
            titleId,
            descriptionId,
            buttonLabelId,
            true,
            true);
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
    }

    private void ShowPractice(
        RectTransform target,
        int titleId,
        int descriptionId)
    {
        ShowPresentation(
            target,
            titleId,
            descriptionId,
            RacingTutorialTextIds.None,
            false,
            false);
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
    }

    private void ShowPresentation(
        RectTransform target,
        int titleId,
        int descriptionId,
        int buttonLabelId,
        bool blockInput,
        bool showButton)
    {
        if (presentationRoot == null)
            return;

        presentationRoot.SetActive(true);
        blocker.color = blockInput ? BackdropColor : PracticeBackdropColor;
        blocker.raycastTarget = blockInput;
        titleText.text = GetTutorialText(titleId);
        descriptionText.text = GetTutorialText(descriptionId);
        nextButton.gameObject.SetActive(showButton);
        nextButtonText.text = GetTutorialText(buttonLabelId);

        FitHighlight(target, 22f);
        UITargetRelativePlacer.Place(
            popup,
            target,
            presentation.transform as RectTransform,
            defaultPlacement);
    }

    private static string GetTutorialText(int languageId)
    {
        if (languageId <= 0)
            return string.Empty;

        LanguageManager languageManager = LanguageManager.Instance;
        if (languageManager == null)
            return $"#{languageId}";

        string text = languageManager.GetText(languageId);
        return string.IsNullOrEmpty(text) ? $"#{languageId}" : text;
    }

    private void HidePresentation()
    {
        if (highlight != null)
            highlight.gameObject.SetActive(false);

        if (presentationRoot != null)
            presentationRoot.SetActive(false);
    }

    private void FitHighlight(RectTransform target, float padding)
    {
        if (highlight == null)
            return;

        if (target == null || presentation == null)
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
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Camera targetCamera = GetCanvasCamera(target);
        Camera hostCamera = GetCanvasCamera(highlightHost);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightHost,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]),
            hostCamera,
            out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            highlightHost,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]),
            hostCamera,
            out Vector2 max);

        highlight.anchorMin = new Vector2(0.5f, 0.5f);
        highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        highlight.anchoredPosition = (min + max) * 0.5f;
        highlight.sizeDelta = max - min + Vector2.one * (padding * 2f);
    }

    private RectTransform ResolveHighlightHost(RectTransform target)
    {
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
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return canvas.worldCamera;
    }

    private void CreatePresentation()
    {
        if (presentationPrefab == null)
        {
            Debug.LogError(
                $"[{nameof(RacingTutorialController)}] Presentation prefab is not assigned.",
                this);
            return;
        }

        bool sceneInstanceAssigned = presentationPrefab.gameObject.scene.IsValid();
        presentation = sceneInstanceAssigned
            ? presentationPrefab
            : Instantiate(presentationPrefab);
        ownsPresentation = !sceneInstanceAssigned;

        if (!presentation.HasRequiredReferences)
        {
            Debug.LogError(
                $"[{nameof(RacingTutorialController)}] Presentation references are incomplete.",
                presentation);
            if (ownsPresentation)
                Destroy(presentation.gameObject);
            presentation = null;
            ownsPresentation = false;
            return;
        }

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
