using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeTutorialController : MonoBehaviour
{
    public static bool IsTutorialActive { get; private set; }

    private enum TutorialState
    {
        None,
        SettingsButton,
        LanguageSelection,
        SelectLanguage,
        SettingsSave,
        ProfileButton,
        NameAndCountry,
        ProfileSave,
        PlayButton,
        GameModeSelection,
        RacingMode,
        RacingMapSelection,
        EnterRacingRoom,
        Finished
    }

    [Header("Direct UI References")]
    [SerializeField] private HomeMainUI homeUI;
    [SerializeField] private Settings settingsUI;
    [SerializeField] private UserDetails userDetailsUI;
    [SerializeField] private MapShowPopup mapShowPopup;

    [Header("Seven Home Targets")]
    [SerializeField] private RectTransform settingsButtonTarget;
    [SerializeField] private RectTransform languageSelectionTarget;
    [SerializeField] private RectTransform settingsSaveTarget;
    [SerializeField] private RectTransform profileButtonTarget;
    [SerializeField] private RectTransform nameAndCountryTarget;
    [SerializeField] private RectTransform profileSaveTarget;

    [Header("Play And Racing Targets")]
    [SerializeField] private RectTransform playButtonTarget;
    [SerializeField] private RectTransform gameModeSelectionTarget;
    [SerializeField] private RectTransform racingModeTarget;
    [SerializeField] private RectTransform racingMapSelectionTarget;
    [SerializeField] private RectTransform enterRacingRoomTarget;

    [Header("Home Presentation")]
    [SerializeField] private HomeTutorialPresentation presentation;
    [SerializeField] private Vector2 highlightPadding = new Vector2(26f, 20f);
    [SerializeField] private float popupCanvasMargin = 32f;
    [SerializeField] private float popupTargetGap = 42f;
    [SerializeField] private float cloudProgressWaitTimeout = 5f;

    private TutorialState state;
    private TutorialState resumeStateAfterPanelShown;
    private Coroutine startRoutine;
    private Coroutine showRoutine;
    private Tween pulseTween;
    private bool waitingForProfilePanelClose;

    private RectTransform PresentationCanvasRect =>
        presentation != null ? presentation.PresentationRect : null;

    private void Awake()
    {
        IsTutorialActive = true;
        ConfigurePresentation();
        HidePresentation();
    }

    private void OnEnable()
    {
        if (homeUI != null)
        {
            homeUI.SettingsPanelShown += HandleSettingsPanelShown;
            homeUI.SettingsPanelClosed += HandleSettingsPanelClosed;
            homeUI.UserDetailsPanelShown += HandleUserDetailsPanelShown;
            homeUI.UserDetailsPanelClosed += HandleUserDetailsPanelClosed;
            homeUI.GameModePanelShown += HandleGameModePanelShown;
            homeUI.RacingMapsShown += HandleRacingMapsShown;
        }

        if (settingsUI != null)
        {
            settingsUI.LanguageDropdownOpened += HandleLanguageDropdownOpened;
            settingsUI.LanguageSelected += HandleLanguageSelected;
        }

        if (userDetailsUI != null)
        {
            userDetailsUI.NameAndCountryReady += HandleNameAndCountryReady;
            userDetailsUI.ProfileSaved += HandleProfileSaved;
        }

        if (mapShowPopup != null)
        {
            mapShowPopup.MapDetailsShown += HandleMapDetailsShown;
            mapShowPopup.RacingRoomEntryRequested +=
                HandleRacingRoomEntryRequested;
        }

        if (presentation != null && presentation.NextButton != null)
            presentation.NextButton.onClick.AddListener(HandleNextClicked);

        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
        StartTutorial();
    }

    private void OnDisable()
    {
        if (homeUI != null)
        {
            homeUI.SettingsPanelShown -= HandleSettingsPanelShown;
            homeUI.SettingsPanelClosed -= HandleSettingsPanelClosed;
            homeUI.UserDetailsPanelShown -= HandleUserDetailsPanelShown;
            homeUI.UserDetailsPanelClosed -= HandleUserDetailsPanelClosed;
            homeUI.GameModePanelShown -= HandleGameModePanelShown;
            homeUI.RacingMapsShown -= HandleRacingMapsShown;
        }

        if (settingsUI != null)
        {
            settingsUI.LanguageDropdownOpened -= HandleLanguageDropdownOpened;
            settingsUI.LanguageSelected -= HandleLanguageSelected;
        }

        if (userDetailsUI != null)
        {
            userDetailsUI.NameAndCountryReady -= HandleNameAndCountryReady;
            userDetailsUI.ProfileSaved -= HandleProfileSaved;
        }

        if (mapShowPopup != null)
        {
            mapShowPopup.MapDetailsShown -= HandleMapDetailsShown;
            mapShowPopup.RacingRoomEntryRequested -=
                HandleRacingRoomEntryRequested;
        }

        if (presentation != null && presentation.NextButton != null)
            presentation.NextButton.onClick.RemoveListener(HandleNextClicked);

        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;
        StopStartRoutine();
        StopShowRoutine();
        StopPresentationAnimation();
        HidePresentation();
        IsTutorialActive = false;
    }

    public void StartTutorialForTesting()
    {
        BeginTutorial(true);
    }

    public void FinishTutorialForTesting()
    {
        HomeTutorialProgress.CompleteTutorial();
        FinishTutorial(true);
    }

    [ContextMenu("Delete Home Tutorial Progress For Testing")]
    public void DeleteHomeTutorialProgressForTesting()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteHomeTutorialProgressForTesting();
            return;
        }

        HomeTutorialProgress.DeleteAllLocalProgress();
        Debug.LogWarning(
            "Home tutorial PlayerPrefs were deleted, but DataManager was unavailable for Firebase deletion.");
    }

    private void StartTutorial()
    {
        BeginTutorial(false);
    }

    private void BeginTutorial(bool forceFromBeginning)
    {
        if (!isActiveAndEnabled)
            return;

        if (!HasRequiredReferences())
        {
            Debug.LogError(
                $"{nameof(HomeTutorialController)} has missing Inspector references.",
                this);
            IsTutorialActive = false;
            return;
        }

        IsTutorialActive = true;
        state = TutorialState.None;
        resumeStateAfterPanelShown = TutorialState.None;
        StopStartRoutine();
        startRoutine =
            StartCoroutine(StartFromSavedProgress(forceFromBeginning));
    }

    private IEnumerator StartFromSavedProgress(bool forceFromBeginning)
    {
        if (!forceFromBeginning &&
            !HomeTutorialProgress.HasAnyLocalData &&
            DataManager.Instance != null)
        {
            DataManager.Instance.EnsureHomeTutorialStateLoaded();
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(2f, cloudProgressWaitTimeout);
            while (!DataManager.Instance.IsHomeTutorialStateLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        HomeTutorialProgress.State progress =
            HomeTutorialProgress.LoadLocal();
        if (!forceFromBeginning && progress.Completed)
        {
            startRoutine = null;
            FinishCompletedTutorialSetup();
            yield break;
        }

        yield return null;
        Canvas.ForceUpdateCanvases();
        startRoutine = null;

        ResumeFromCheckpoint(
            forceFromBeginning
                ? HomeTutorialProgress.CoreCheckpoint.Settings
                : progress.Checkpoint);
    }

    private void ResumeFromCheckpoint(
        HomeTutorialProgress.CoreCheckpoint checkpoint)
    {
        switch (checkpoint)
        {
            case HomeTutorialProgress.CoreCheckpoint.Settings:
                QueueState(TutorialState.SettingsButton);
                break;

            case HomeTutorialProgress.CoreCheckpoint.Profile:
                QueueState(TutorialState.ProfileButton);
                break;

            case HomeTutorialProgress.CoreCheckpoint.Play:
                QueueState(TutorialState.PlayButton);
                break;

            case HomeTutorialProgress.CoreCheckpoint.GameMode:
                resumeStateAfterPanelShown =
                    TutorialState.GameModeSelection;
                homeUI.OpenGameMainPanel();
                break;

            case HomeTutorialProgress.CoreCheckpoint.RacingMode:
                resumeStateAfterPanelShown = TutorialState.RacingMode;
                homeUI.OpenGameMainPanel();
                break;

            case HomeTutorialProgress.CoreCheckpoint.RacingMap:
            case HomeTutorialProgress.CoreCheckpoint.EnterRacingRoom:
                resumeStateAfterPanelShown =
                    TutorialState.RacingMapSelection;
                homeUI.OpenRacingMaps();
                break;

            default:
                FinishCompletedTutorialSetup();
                break;
        }
    }

    private void FinishCompletedTutorialSetup()
    {
        StopShowRoutine();
        StopPresentationAnimation();
        HidePresentation();
        state = TutorialState.Finished;
        IsTutorialActive = false;
        homeUI?.OnHomeTutorialFinishedForTesting();
    }

    private void HandleSettingsPanelShown()
    {
        if (state == TutorialState.SettingsButton)
            QueueState(TutorialState.LanguageSelection);
    }

    private void HandleLanguageDropdownOpened()
    {
        if (state == TutorialState.LanguageSelection)
            QueueState(TutorialState.SelectLanguage);
    }

    private void HandleLanguageSelected(int selectedIndex)
    {
        if (state == TutorialState.LanguageSelection ||
            state == TutorialState.SelectLanguage)
        {
            QueueState(TutorialState.SettingsSave);
        }
    }

    private void HandleSettingsPanelClosed()
    {
        if (state == TutorialState.SettingsSave)
        {
            CompleteCoreStep(
                Constants.HomeTutorial.Settings,
                HomeTutorialProgress.CoreCheckpoint.Profile);
            QueueState(TutorialState.ProfileButton);
        }
    }

    private void HandleUserDetailsPanelShown()
    {
        if (state == TutorialState.ProfileButton)
            QueueState(TutorialState.NameAndCountry);
    }

    private void HandleNameAndCountryReady()
    {
        if (state == TutorialState.NameAndCountry)
            QueueState(TutorialState.ProfileSave);
    }

    private void HandleProfileSaved()
    {
        if (state == TutorialState.ProfileSave)
            waitingForProfilePanelClose = true;
    }

    private void HandleUserDetailsPanelClosed()
    {
        if (state != TutorialState.ProfileSave ||
            !waitingForProfilePanelClose)
        {
            return;
        }

        waitingForProfilePanelClose = false;
        CompleteCoreStep(
            Constants.HomeTutorial.Profile,
            HomeTutorialProgress.CoreCheckpoint.Play);
        QueueState(TutorialState.PlayButton);
    }

    private void HandleGameModePanelShown()
    {
        if (resumeStateAfterPanelShown != TutorialState.None)
        {
            TutorialState resumedState = resumeStateAfterPanelShown;
            resumeStateAfterPanelShown = TutorialState.None;
            QueueState(resumedState);
            return;
        }

        if (state == TutorialState.PlayButton)
        {
            CompleteCoreStep(
                Constants.HomeTutorial.Play,
                HomeTutorialProgress.CoreCheckpoint.GameMode);
            QueueState(TutorialState.GameModeSelection);
        }
    }

    private void HandleRacingMapsShown()
    {
        if (resumeStateAfterPanelShown ==
            TutorialState.RacingMapSelection)
        {
            resumeStateAfterPanelShown = TutorialState.None;
            QueueState(TutorialState.RacingMapSelection);
            return;
        }

        if (state == TutorialState.RacingMode)
        {
            CompleteCoreStep(
                Constants.HomeTutorial.RacingMode,
                HomeTutorialProgress.CoreCheckpoint.RacingMap);
            QueueState(TutorialState.RacingMapSelection);
        }
    }

    private void HandleMapDetailsShown(MapCard.MapDetailsData data)
    {
        if (state == TutorialState.RacingMapSelection &&
            data.MapType == MapCard.MapType.Racing)
        {
            CompleteCoreStep(
                Constants.HomeTutorial.RacingMap,
                HomeTutorialProgress.CoreCheckpoint.EnterRacingRoom);
            QueueState(TutorialState.EnterRacingRoom);
        }
    }

    private void HandleRacingRoomEntryRequested(
        MapCard.MapDetailsData data)
    {
        if (state == TutorialState.EnterRacingRoom &&
            data.MapType == MapCard.MapType.Racing)
        {
            HomeTutorialProgress.CompleteTutorial();
            FinishTutorial(false);
        }
    }

    private void HandleNextClicked()
    {
        if (state == TutorialState.GameModeSelection)
        {
            CompleteCoreStep(
                Constants.HomeTutorial.GameMode,
                HomeTutorialProgress.CoreCheckpoint.RacingMode);
            QueueState(TutorialState.RacingMode);
        }
    }

    private void HandleLanguageChanged(string language)
    {
        if (IsTutorialActive)
            QueueState(state);
    }

    private void QueueState(TutorialState nextState)
    {
        StopShowRoutine();
        showRoutine = StartCoroutine(ShowStateAfterLayout(nextState));
    }

    private IEnumerator ShowStateAfterLayout(TutorialState nextState)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        showRoutine = null;
        ShowState(nextState);
    }

    private void ShowState(TutorialState nextState)
    {
        if (!IsTutorialActive)
            return;

        state = nextState;
        switch (state)
        {
            case TutorialState.SettingsButton:
                ShowStep(
                    settingsButtonTarget,
                    HomeTutorialTextIds.SettingsButton,
                    HomeTutorialTextIds.SettingsButtonDescription,
                    true);
                break;

            case TutorialState.LanguageSelection:
                ShowStep(
                    languageSelectionTarget,
                    HomeTutorialTextIds.LanguageSelection,
                    HomeTutorialTextIds.LanguageSelectionDescription,
                    true);
                break;

            case TutorialState.SelectLanguage:
                ShowStep(
                    languageSelectionTarget,
                    HomeTutorialTextIds.SelectLanguage,
                    HomeTutorialTextIds.SelectLanguageDescription,
                    false);
                break;

            case TutorialState.SettingsSave:
                ShowStep(
                    settingsSaveTarget,
                    HomeTutorialTextIds.SettingsSave,
                    HomeTutorialTextIds.SettingsSaveDescription,
                    true);
                break;

            case TutorialState.ProfileButton:
                ShowStep(
                    profileButtonTarget,
                    HomeTutorialTextIds.ProfileButton,
                    HomeTutorialTextIds.ProfileButtonDescription,
                    true);
                break;

            case TutorialState.NameAndCountry:
                ShowStep(
                    nameAndCountryTarget,
                    HomeTutorialTextIds.NameAndCountry,
                    HomeTutorialTextIds.NameAndCountryDescription,
                    false);
                break;

            case TutorialState.ProfileSave:
                ShowStep(
                    profileSaveTarget,
                    HomeTutorialTextIds.ProfileSave,
                    HomeTutorialTextIds.ProfileSaveDescription,
                    true);
                break;

            case TutorialState.PlayButton:
                ShowStep(
                    playButtonTarget,
                    HomeTutorialTextIds.PlayButton,
                    HomeTutorialTextIds.PlayButtonDescription,
                    true);
                break;

            case TutorialState.GameModeSelection:
                ShowStep(
                    gameModeSelectionTarget,
                    HomeTutorialTextIds.GameModeSelection,
                    HomeTutorialTextIds.GameModeSelectionDescription,
                    false,
                    true,
                    true);
                break;

            case TutorialState.RacingMode:
                ShowStep(
                    racingModeTarget,
                    HomeTutorialTextIds.RacingMode,
                    HomeTutorialTextIds.RacingModeDescription,
                    true);
                break;

            case TutorialState.RacingMapSelection:
                ShowStep(
                    racingMapSelectionTarget,
                    HomeTutorialTextIds.RacingMapSelection,
                    HomeTutorialTextIds.RacingMapSelectionDescription,
                    true);
                break;

            case TutorialState.EnterRacingRoom:
                ShowStep(
                    enterRacingRoomTarget,
                    HomeTutorialTextIds.EnterRacingRoom,
                    HomeTutorialTextIds.EnterRacingRoomDescription,
                    true);
                break;
        }
    }

    private void ShowStep(
        RectTransform target,
        int titleId,
        int descriptionId,
        bool allowOnlyTargetInput,
        bool showNextButton = false,
        bool blockAllUnderlyingInput = false)
    {
        if (presentation == null)
            return;

        presentation.PresentationRoot.SetActive(true);
        SetText(presentation.TitleText, titleId);
        SetText(presentation.DescriptionText, descriptionId);
        presentation.NextButton.gameObject.SetActive(showNextButton);
        if (showNextButton)
        {
            SetText(
                presentation.NextButtonText,
                HomeTutorialTextIds.Continue);
            presentation.NextButton.interactable = true;
        }

        presentation.Blocker.raycastTarget =
            allowOnlyTargetInput || blockAllUnderlyingInput;
        if (blockAllUnderlyingInput)
            presentation.RaycastFilter.ClearTarget();
        else
            presentation.RaycastFilter.SetTarget(
                target,
                allowOnlyTargetInput);

        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        presentation.KeepPopupOnTop();

        if (hasTarget)
        {
            presentation.ParkHighlight();
            AttachHighlightBehindTarget(target);
            FitHighlightToTarget(target);
            PlacePopup(target);
            StartHighlightPulse();
        }
        else
        {
            presentation.ParkHighlight();
            PlacePopup(null);
            StopPresentationAnimation();
        }
    }

    private void ConfigurePresentation()
    {
        if (presentation == null)
            return;

        if (presentation.Highlight.TryGetComponent(out Graphic highlightGraphic))
            highlightGraphic.raycastTarget = false;

        presentation.Blocker.raycastTarget = true;

        Graphic[] popupGraphics =
            presentation.Popup.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in popupGraphics)
            graphic.raycastTarget = false;

        if (presentation.NextButton.TryGetComponent(
                out Graphic nextButtonGraphic))
        {
            nextButtonGraphic.raycastTarget = true;
        }

        presentation.NextButton.gameObject.SetActive(false);
        presentation.ParkHighlight();
        presentation.KeepPopupOnTop();
    }

    private void AttachHighlightBehindTarget(RectTransform target)
    {
        RectTransform highlight = presentation.Highlight;
        RectTransform targetParent = target.parent as RectTransform;
        if (highlight == null || targetParent == null)
            return;

        highlight.SetParent(targetParent, false);
        int targetSiblingIndex = target.GetSiblingIndex();
        highlight.SetSiblingIndex(targetSiblingIndex);
        highlight.gameObject.SetActive(true);
    }

    private void FitHighlightToTarget(RectTransform target)
    {
        RectTransform highlight = presentation.Highlight;
        if (highlight == null || highlight.parent != target.parent)
            return;

        highlight.anchorMin = target.anchorMin;
        highlight.anchorMax = target.anchorMax;
        highlight.pivot = target.pivot;
        highlight.anchoredPosition = target.anchoredPosition;
        highlight.sizeDelta = target.sizeDelta + highlightPadding * 2f;
        highlight.localRotation = target.localRotation;
        highlight.localScale = target.localScale;
    }

    private void PlacePopup(RectTransform target)
    {
        UITargetPlacementSettings placement = UITargetPlacementSettings.Default;
        placement.canvasMargin = Mathf.Max(popupCanvasMargin, 0f);
        placement.targetGap = Mathf.Max(popupTargetGap, 0f);
        placement.rebuildLayoutBeforePlace = true;
        UITargetRelativePlacer.Place(
            presentation.Popup,
            target,
            PresentationCanvasRect,
            placement);
    }

    private static void GetTargetBounds(
        RectTransform target,
        RectTransform canvasRect,
        out Vector2 targetMin,
        out Vector2 targetMax)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas == null ||
                              targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]),
            null,
            out targetMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]),
            null,
            out targetMax);
    }

    private void StartHighlightPulse()
    {
        StopPresentationAnimation();
        presentation.Highlight.localScale = Vector3.one;
        pulseTween = presentation.Highlight
            .DOScale(1.035f, 0.55f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopPresentationAnimation()
    {
        pulseTween?.Kill();
        pulseTween = null;
        presentation?.Highlight?.DOKill();
    }

    private static void SetText(TMP_Text target, int languageId)
    {
        if (target == null)
            return;

        target.text = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(languageId)
            : $"#{languageId}";
    }

    private bool HasRequiredReferences()
    {
        return homeUI != null &&
               settingsUI != null &&
               userDetailsUI != null &&
               mapShowPopup != null &&
               settingsButtonTarget != null &&
               languageSelectionTarget != null &&
               settingsSaveTarget != null &&
               profileButtonTarget != null &&
               nameAndCountryTarget != null &&
               profileSaveTarget != null &&
               playButtonTarget != null &&
               gameModeSelectionTarget != null &&
               racingModeTarget != null &&
               racingMapSelectionTarget != null &&
               enterRacingRoomTarget != null &&
               presentation != null &&
               presentation.HasRequiredReferences;
    }

    private void FinishTutorial(bool resumeHomePopups)
    {
        StopStartRoutine();
        StopShowRoutine();
        StopPresentationAnimation();
        HidePresentation();
        state = TutorialState.Finished;
        IsTutorialActive = false;
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        if (resumeHomePopups)
            homeUI?.OnHomeTutorialFinishedForTesting();
    }

    private static void CompleteCoreStep(
        string stepKey,
        HomeTutorialProgress.CoreCheckpoint nextCheckpoint)
    {
        HomeTutorialProgress.CompleteCoreStep(stepKey, nextCheckpoint);
    }

    private void HidePresentation()
    {
        if (presentation == null)
            return;

        presentation.RaycastFilter?.ClearTarget();
        presentation.ParkHighlight();
        if (presentation.NextButton != null)
            presentation.NextButton.gameObject.SetActive(false);
        if (presentation.PresentationRoot != null)
            presentation.PresentationRoot.SetActive(false);
    }

    private void StopShowRoutine()
    {
        if (showRoutine == null)
            return;

        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private void StopStartRoutine()
    {
        if (startRoutine == null)
            return;

        StopCoroutine(startRoutine);
        startRoutine = null;
    }
}
