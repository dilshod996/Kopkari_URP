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
    [SerializeField] private GameObject objectiveIndicatorRoot;
    [SerializeField] private RectTransform objectiveIndicatorTarget;

    [Header("Input")]
    [SerializeField, Range(0.1f, 1f)] private float joystickCompletionThreshold = 0.35f;
    [SerializeField, Range(0.02f, 0.3f)] private float cameraCompletionThreshold = 0.08f;
    [SerializeField, Range(0.5f, 4f)] private float cameraPracticeDuration = 1.5f;
    [SerializeField, Range(0.25f, 2f)] private float thirdPersonSettleDuration = 1f;
    [SerializeField, Range(1f, 6f)] private float sliderPreviewDuration = 3f;

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
    private GameObject tutorialCanvasObject;
    private GameObject presentationRoot;
    private Image blocker;
    private RectTransform highlight;
    private RectTransform popup;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private Button nextButton;
    private TMP_Text nextButtonText;
    private bool presentationHiddenForPauseMenu;
    private bool pickupFocusAvailable;
    private float cameraPracticeElapsed;
    private bool pendingGripDamageTutorial;
    private bool pendingHorseHealthTutorial;
    private bool gripDepletionObserved;
    private bool pendingLostUlakTutorial;
    private bool pendingOpponentCarrierTutorial;
    private bool pendingFakeUlakTutorial;
    private bool gripDamageTutorialShown;
    private bool horseHealthTutorialShown;
    private bool fakeUlakTutorialShown;
    private bool lostUlakTutorialShown;
    private bool opponentCarrierTutorialShown;
    private bool localPlayerHadUlak;
    private TutorialState contextReturnState = TutorialState.Finished;

    private static readonly Color BackdropColor = new Color(0.015f, 0.025f, 0.05f, 0.72f);
    private static readonly Color PanelColor = new Color(0.035f, 0.07f, 0.13f, 0.97f);
    private static readonly Color AccentColor = new Color(1f, 0.72f, 0.12f, 1f);

    private void Awake()
    {
        if (mainUI == null)
            mainUI = GetComponent<KopkariMainUI>();
        BuildRuntimeUI();
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
        if (tutorialCanvasObject != null)
            Destroy(tutorialCanvasObject);
    }

    private void Update()
    {
        if (presentationRoot == null)
            return;

        if (gripDepletionObserved && !fakeUlakTutorialShown &&
            mainUI != null && mainUI.IsFakeUlakInteractable)
            pendingFakeUlakTutorial = true;

        if (TryShowPendingContextTutorial())
            return;

        if (state == TutorialState.None || state == TutorialState.Finished)
            return;

        bool pauseMenuActive = KopkariMainUI.IsGameplayPaused;
        if (pauseMenuActive && presentationRoot.activeSelf)
        {
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

        AIKopkariRider.SetRegistanTutorialRestrictions(true, false, false);
        ShouldPauseMainTime = true;
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(false);
        if (startRoutine != null)
            StopCoroutine(startRoutine);
        startRoutine = StartCoroutine(StartAfterGameplayUIIsReady());
    }

    private IEnumerator StartAfterGameplayUIIsReady()
    {
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
            objectiveIndicatorRoot == null || objectiveIndicatorTarget == null ||
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

        ShowJoystickExplanation();
    }

    private void ShowJoystickExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.JoystickExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);

        ShowPresentation(
            movementJoystickTarget,
            joystickPlacement,
            "MOVE YOUR HORSE",
            "Use the movement joystick to ride in any direction.",
            "TRY IT",
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
            "YOUR TURN",
            "Drag the joystick to move your horse.",
            string.Empty,
            false,
            false);
    }

    private void HandleJoystickAxisChanged(Vector2 axis)
    {
        if (axis.sqrMagnitude < joystickCompletionThreshold * joystickCompletionThreshold)
            return;

        if (state == TutorialState.WaitingForJoystickInput)
            ShowCameraExplanation();
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
            "LOOK AROUND",
            "Drag this area to rotate the camera and look around the field.",
            "TRY IT",
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
            "YOUR TURN",
            "Keep dragging the camera area and look around the field for a moment.",
            string.Empty,
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
            "MATCH STATUS",
            "This panel shows the main time and your round progress.",
            "NEXT",
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
            "CHANGE CAMERA",
            "Tap once for first-person view, then tap again to return to third-person.",
            "TRY IT",
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
            "FIRST-PERSON",
            "Tap the camera button to switch to first-person view.",
            string.Empty,
            false,
            false);
    }

    private void HandleFirstPersonCamera(bool firstPerson)
    {
        if (state == TutorialState.WaitingForFirstPerson && firstPerson)
        {
            state = TutorialState.WaitingForThirdPerson;
            ShowPresentation(
                mainUI.CameraSwitchTutorialTarget,
                cameraPlacement,
                "THIRD-PERSON",
                "Good. Tap the same button again to return to third-person view.",
                string.Empty,
                false,
                false);
        }
        else if (state == TutorialState.WaitingForThirdPerson && !firstPerson)
        {
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
            "LOOK BEHIND",
            "Hold this button to see behind your rider. Release it to look forward.",
            "TRY IT",
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
            "HOLD LOOK BACK",
            "Press and hold the back-camera button.",
            string.Empty,
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
                "RELEASE",
                "Now release the button to return to the forward view.",
                string.Empty,
                false,
                false);
        }
        else if (state == TutorialState.WaitingForLookBackRelease && !pressed)
        {
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
            "SPRINT",
            "Use Sprint while your horse is moving to ride faster.",
            "TRY IT",
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
            "HOLD SPRINT",
            "Keep moving and hold the Sprint button.",
            string.Empty,
            false,
            false);
    }

    private void HandleSprintStarted()
    {
        if (state == TutorialState.WaitingForSprintUse)
            ShowSprintSliderPreview();
    }

    private void ShowSprintSliderPreview()
    {
        state = TutorialState.SprintSliderPreview;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);

        ShowPresentation(
            sprintSliderTarget,
            sliderPlacement,
            "SPRINT STAMINA",
            "Sprint drains this slider. It refills after you release Sprint.",
            string.Empty,
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
            ShowUloqIndicatorExplanation();
    }

    private void ShowUloqIndicatorExplanation()
    {
        state = TutorialState.UloqIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        objectiveIndicator?.RefreshNow();
        objectiveIndicator?.SetTutorialPreview(true);
        if (objectiveIndicatorRoot != null)
            objectiveIndicatorRoot.SetActive(true);

        ShowPresentation(
            objectiveIndicatorTarget,
            indicatorPlacement,
            "FIND THE ULOQ",
            "The indicator and meter show your distance to the Uloq.",
            "GO TO ULOQ",
            true,
            true);
    }

    private void WaitForPickupAvailability()
    {
        objectiveIndicator?.SetTutorialPreview(false);
        state = TutorialState.WaitingForPickupAvailability;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            objectiveIndicatorTarget,
            indicatorPlacement,
            "GET CLOSER",
            "Follow the Uloq indicator. The pickup button appears when you are close enough.",
            string.Empty,
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
        state = TutorialState.PickupButtonExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.PickupButtonTutorialTarget,
            sprintPlacement,
            "PICK UP THE ULOQ",
            "You are close enough. Press and hold this button to pick up the Uloq.",
            "TRY IT",
            true,
            true);
    }

    private void BeginPickupPractice()
    {
        state = TutorialState.WaitingForPickupPress;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.PickupButtonTutorialTarget,
            sprintPlacement,
            "HOLD TO PICK UP",
            "Keep the button held while you remain close to the Uloq.",
            string.Empty,
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
                "PICKUP PROGRESS",
                "Keep holding. When this slider becomes full, you get the Uloq.",
                string.Empty,
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
                "KEEP HOLDING",
                "The slider was not full. Hold the pickup button again.",
                string.Empty,
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
        if (state == TutorialState.WaitingForPlayerPickup ||
            state == TutorialState.WaitingForPickupPress ||
            state == TutorialState.PickupSliderExplanation)
            ShowTargetIndicatorExplanation();
    }

    private void ShowTargetIndicatorExplanation()
    {
        state = TutorialState.TargetIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        objectiveIndicator?.RefreshNow();
        objectiveIndicator?.SetTutorialPreview(true);
        ShowPresentation(
            objectiveIndicatorTarget,
            indicatorPlacement,
            "RIDE TO SALYM",
            "You have the Uloq. The indicator and meter now point to the target.",
            "NEXT",
            true,
            true);
    }

    private void WaitForCombo()
    {
        objectiveIndicator?.SetTutorialPreview(false);
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
            "COMBO PRIZE",
            "Reach Salym before this timer ends to earn the extra Nyufiy shown here.",
            "NEXT",
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
            "CARRIER GRIP",
            "This shows the carrier name and grip. If your grip reaches 0%, you lose the Uloq.",
            "CONTINUE",
            true,
            true);
    }

    private void ReleaseCompetitionAndWaitForNextRound()
    {
        AIKopkariRider.SetRegistanTutorialRestrictions(false, true, true);
        objectiveIndicator?.SetTutorialPreview(false);
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
        if (pendingHorseHealthTutorial && !horseHealthTutorialShown)
        {
            contextReturnState = state;
            pendingHorseHealthTutorial = false;
            horseHealthTutorialShown = true;
            ShowHorseHealthExplanation();
            return true;
        }

        return false;
    }

    private bool IsContextTutorialSlotAvailable()
    {
        if (presentationRoot != null && presentationRoot.activeSelf)
            return false;

        return state == TutorialState.WaitingForNextRoundWarmup ||
               state == TutorialState.WaitingForWarmupArrival ||
               state == TutorialState.Finished;
    }

    private void ShowGripDamageExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.GripDamageExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.CarrierTutorialTarget,
            matchStatusPlacement,
            "YOUR GRIP IS DROPPING",
            "Rivals are damaging your grip. Be careful - at 0% you lose the Uloq.",
            "HOW TO DEFEND",
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
            "DEFEND THE ULOQ",
            "Use the Defend button to protect your grip and keep control of the Uloq.",
            "GOT IT",
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
            "FAKE ULOQ",
            "After losing the Uloq, use this when you are near it to distract rival riders.",
            "GOT IT",
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
            "ANOTHER RIDER HAS THE ULOQ",
            "Chase the carrier and take the Uloq, or reduce the carrier's grip to 0%.",
            "NEXT",
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
            "WEB SNARE",
            "Hit the Uloq carrier with Web Snare to make that rider lose the Uloq.",
            mainUI.IsShootWebInteractable ? "TRY IT" : "GOT IT",
            true,
            true);
    }

    private void BeginWebSnareButtonPractice()
    {
        if (!mainUI.IsShootWebInteractable)
        {
            RestoreContextTutorialState();
            return;
        }

        state = TutorialState.WaitingForWebSnareButtonClick;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        ShowPresentation(
            mainUI.ShootWebTutorialTarget,
            sprintPlacement,
            "EQUIP WEB SNARE",
            "Press this button to open the Web Snare shooting control.",
            string.Empty,
            false,
            false);
    }

    private void HandleWebSnareButtonEnabled()
    {
        if (state != TutorialState.WaitingForWebSnareButtonClick ||
            mainUI == null || !mainUI.IsChainContainerVisible)
            return;

        ShowChainContainerExplanation();
    }

    private void ShowChainContainerExplanation()
    {
        state = TutorialState.ChainContainerExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.ChainContainerTutorialTarget,
            sprintPlacement,
            "SHOOT WEB SNARE",
            "Press and hold this button to aim and shoot Web Snare at the Uloq carrier.",
            "TRY IT",
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
            "YOUR TURN",
            "Press and hold to shoot Web Snare.",
            string.Empty,
            false,
            false);
    }

    private void HandleWebSnareStarted()
    {
        if (state == TutorialState.WaitingForChainContainerPress)
            RestoreContextTutorialState();
    }

    private void ShowLostUlakExplanation()
    {
        IsTutorialActive = true;
        state = TutorialState.LostUlakExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        objectiveIndicator?.RefreshNow();
        objectiveIndicator?.SetTutorialPreview(true);
        ShowPresentation(
            objectiveIndicatorTarget,
            indicatorPlacement,
            "YOU LOST THE ULOQ",
            "Go back to the Uloq. Follow this indicator and distance meter.",
            "GO BACK",
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
            "HORSE HEALTH",
            "Damage lowers your horse's health. If it reaches zero, your match ends.",
            "GOT IT",
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
            "NEXT ROUND WARMUP",
            "Reach the warmup point before this time expires.",
            "SHOW THE WAY",
            true,
            true);
    }

    private void ShowWarmupIndicatorExplanation()
    {
        state = TutorialState.WarmupIndicatorExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        objectiveIndicator?.RefreshNow();
        objectiveIndicator?.SetTutorialPreview(true);
        ShowPresentation(
            objectiveIndicatorTarget,
            indicatorPlacement,
            "WARMUP POINT",
            "Follow this indicator and meter to reach the warmup point.",
            "GO",
            false,
            true);
    }

    private void WaitForWarmupArrival()
    {
        objectiveIndicator?.SetTutorialPreview(false);
        state = TutorialState.WaitingForWarmupArrival;
        TutorialPauseController.Apply(TutorialTimeMode.KeepPlaying);
        HidePresentation();
    }

    private void ShowRoundStartExplanation()
    {
        state = TutorialState.RoundStartExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        objectiveIndicator?.SetTutorialPreview(false);
        ShowPresentation(
            mainUI.WarmupTutorialTarget,
            matchStatusPlacement,
            "READY FOR THE NEXT ROUND",
            "You reached the warmup point. The new round starts when this countdown finishes.",
            "GOT IT",
            true,
            true);
    }

    private void ShowWalkZoneExplanation()
    {
        state = TutorialState.WalkZoneExplanation;
        TutorialPauseController.Apply(TutorialTimeMode.PauseGame);
        ShowPresentation(
            mainUI.WalkZoneTutorialTarget,
            sprintPlacement,
            "WALK ZONE",
            "Use Walk Zone while carrying the Uloq. Chasers entering it are slowed down.",
            "CONTINUE",
            true,
            true);
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
                WaitForPickupAvailability();
                break;
            case TutorialState.PickupButtonExplanation:
                BeginPickupPractice();
                break;
            case TutorialState.TargetIndicatorExplanation:
                WaitForCombo();
                break;
            case TutorialState.ComboExplanation:
                WaitForCarrier();
                break;
            case TutorialState.CarrierExplanation:
                ShowWalkZoneExplanation();
                break;
            case TutorialState.WalkZoneExplanation:
                ReleaseCompetitionAndWaitForNextRound();
                break;
            case TutorialState.WarmupBackgroundExplanation:
                ShowWarmupIndicatorExplanation();
                break;
            case TutorialState.WarmupIndicatorExplanation:
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
            case TutorialState.LostUlakExplanation:
            case TutorialState.FakeUlakExplanation:
            case TutorialState.HorseHealthExplanation:
                if (state == TutorialState.LostUlakExplanation)
                    objectiveIndicator?.SetTutorialPreview(false);
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
        objectiveIndicator?.SetTutorialPreview(false);
        AIKopkariRider.SetRegistanTutorialRestrictions(false, true, true);
        KopkariManager.Instance?.SetRegistanTutorialUlakVisible(true);
        ShouldPauseMainTime = false;
        TutorialPauseController.ResumeAll();
        state = completed ? TutorialState.Finished : TutorialState.None;
        IsTutorialActive = false;
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

    }

    private void ShowPresentation(
        RectTransform target,
        UITargetPlacementSettings placement,
        string title,
        string description,
        string buttonLabel,
        bool blockInput,
        bool showButton)
    {
        ShowPresentation(
            target,
            null,
            placement,
            title,
            description,
            buttonLabel,
            blockInput,
            showButton);
    }

    private void ShowPresentation(
        RectTransform target,
        RectTransform secondaryTarget,
        UITargetPlacementSettings placement,
        string title,
        string description,
        string buttonLabel,
        bool blockInput,
        bool showButton)
    {
        if (presentationRoot == null)
            return;

        presentationHiddenForPauseMenu = false;
        presentationRoot.SetActive(true);
        blocker.color = blockInput ? BackdropColor : new Color(0f, 0f, 0f, 0.18f);
        blocker.raycastTarget = blockInput;
        titleText.text = title;
        descriptionText.text = description;
        nextButton.gameObject.SetActive(showButton);
        nextButtonText.text = buttonLabel;

        FitHighlightToTargets(target, secondaryTarget, 22f);
        UITargetRelativePlacer.Place(popup, target, tutorialCanvasObject.transform as RectTransform, placement);
    }

    private void HidePresentation()
    {
        presentationHiddenForPauseMenu = false;
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
            case TutorialState.WaitingForPickupAvailability:
            case TutorialState.TargetIndicatorExplanation:
            case TutorialState.WarmupIndicatorExplanation:
                PlaceAt(objectiveIndicatorTarget, indicatorPlacement);
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
            case TutorialState.LostUlakExplanation:
                PlaceAt(objectiveIndicatorTarget, indicatorPlacement);
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
        if (highlight == null || target == null || tutorialCanvasObject == null)
            return;

        GetTargetBounds(target, out Vector2 min, out Vector2 max);
        if (secondaryTarget != null)
        {
            GetTargetBounds(secondaryTarget, out Vector2 secondaryMin, out Vector2 secondaryMax);
            min = Vector2.Min(min, secondaryMin);
            max = Vector2.Max(max, secondaryMax);
        }

        highlight.anchoredPosition = (min + max) * 0.5f;
        highlight.sizeDelta = max - min + Vector2.one * (padding * 2f);
    }

    private void GetTargetBounds(RectTransform target, out Vector2 min, out Vector2 max)
    {
        RectTransform canvasRect = tutorialCanvasObject.transform as RectTransform;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera targetCamera = null;
        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            targetCamera = targetCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]),
            null,
            out min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]),
            null,
            out max);
    }

    private void BuildRuntimeUI()
    {
        tutorialCanvasObject = new GameObject(
            "Registan Tutorial Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = tutorialCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = tutorialCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        presentationRoot = CreateUIObject("Presentation", tutorialCanvasObject.transform).gameObject;
        StretchToParent(presentationRoot.transform as RectTransform);

        blocker = CreateImage("Backdrop", presentationRoot.transform, BackdropColor);
        StretchToParent(blocker.rectTransform);

        Image highlightImage = CreateImage("Target Highlight", presentationRoot.transform, new Color(1f, 0.72f, 0.12f, 0.18f));
        highlightImage.raycastTarget = false;
        highlight = highlightImage.rectTransform;
        highlight.anchorMin = highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        Outline outline = highlightImage.gameObject.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(5f, -5f);

        Image panelImage = CreateImage("Popup", presentationRoot.transform, PanelColor);
        panelImage.raycastTarget = false;
        popup = panelImage.rectTransform;
        popup.sizeDelta = new Vector2(620f, 260f);

        Outline panelOutline = panelImage.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1f, 0.72f, 0.12f, 0.7f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        titleText = CreateText("Title", popup, 38f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        SetRect(titleText.rectTransform, new Vector2(34f, -26f), new Vector2(-34f, -82f), true);
        titleText.color = AccentColor;

        descriptionText = CreateText("Description", popup, 28f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(descriptionText.rectTransform, new Vector2(34f, -88f), new Vector2(-34f, -165f), true);
        descriptionText.color = Color.white;

        Image buttonImage = CreateImage("Next Button", popup, AccentColor);
        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-30f, 24f);
        buttonRect.sizeDelta = new Vector2(210f, 62f);

        nextButton = buttonImage.gameObject.AddComponent<Button>();
        nextButton.targetGraphic = buttonImage;
        nextButton.onClick.AddListener(HandleNextClicked);

        nextButtonText = CreateText("Label", buttonRect, 27f, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchToParent(nextButtonText.rectTransform);
        nextButtonText.color = new Color(0.04f, 0.05f, 0.07f, 1f);
        nextButtonText.raycastTarget = false;
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateUIObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 topLeft, Vector2 bottomRight, bool stretchHorizontal)
    {
        rect.anchorMin = stretchHorizontal ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
        rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
    }
}
