using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeHorseConditionTutorialController : MonoBehaviour
{
    public static bool IsTutorialActive { get; private set; }

    private enum TutorialState
    {
        None,
        HorseCondition,
        WaitingForFoodPanel,
        ChooseFood,
        FeedHorse,
        Finished
    }

    [Header("Direct References")]
    [SerializeField] private HomeMainUI homeUI;
    [SerializeField] private MapShowPopup mapShowPopup;
    [SerializeField] private ResourceInfoDetailsPopup foodDetailsPopup;

    [Header("Targets")]
    [Tooltip("Use the common parent RectTransform containing Power, Cooling, and Stamina.")]
    [SerializeField] private RectTransform horseConditionTarget;
    [Tooltip("Use the RectTransform containing the selectable horse foods.")]
    [SerializeField] private RectTransform foodSelectionTarget;
    [Tooltip("Use the Buy/confirm button inside ResourceInfoDetailsPopup.")]
    [SerializeField] private RectTransform feedHorseTarget;

    [Header("Shared Home Presentation")]
    [SerializeField] private HomeTutorialPresentation presentation;
    [SerializeField] private Vector2 highlightPadding = new Vector2(26f, 20f);
    [SerializeField] private float popupCanvasMargin = 32f;
    [SerializeField] private float popupTargetGap = 42f;

    private TutorialState state;
    private Coroutine showRoutine;
    private Tween pulseTween;
    private bool foodPanelReady;

    private RectTransform PresentationCanvasRect =>
        presentation != null ? presentation.PresentationRect : null;

    private void OnEnable()
    {
        if (mapShowPopup != null)
            mapShowPopup.HorseConditionEntryBlocked += HandleConditionBlocked;

        if (homeUI != null)
            homeUI.FoodPanelShown += HandleFoodPanelShown;

        if (foodDetailsPopup != null)
        {
            foodDetailsPopup.DetailsShown += HandleFoodDetailsShown;
            foodDetailsPopup.DetailsClosed += HandleFoodDetailsClosed;
        }

        FoodInfo.OnFoodAddToHorse += HandleFoodApplied;
        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        if (mapShowPopup != null)
            mapShowPopup.HorseConditionEntryBlocked -= HandleConditionBlocked;

        if (homeUI != null)
            homeUI.FoodPanelShown -= HandleFoodPanelShown;

        if (foodDetailsPopup != null)
        {
            foodDetailsPopup.DetailsShown -= HandleFoodDetailsShown;
            foodDetailsPopup.DetailsClosed -= HandleFoodDetailsClosed;
        }

        FoodInfo.OnFoodAddToHorse -= HandleFoodApplied;
        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;

        if (IsTutorialActive)
            FinishTutorial(false);
    }

    public void StartTutorialForTesting()
    {
        StartTutorial();
    }

    public void FinishTutorialForTesting()
    {
        CompleteHorseConditionLesson();
        FinishTutorial(true);
    }

    private void HandleConditionBlocked(HorseConditionStats current)
    {
        if (HomeTutorialProgress.HasContextLesson(
                HomeTutorialProgress.ContextLesson.HorseCondition))
        {
            return;
        }

        StartTutorial();
    }

    private void StartTutorial()
    {
        if (!isActiveAndEnabled)
            return;

        if (!HasRequiredReferences())
        {
            Debug.LogError(
                $"{nameof(HomeHorseConditionTutorialController)} has missing Inspector references.",
                this);
            return;
        }

        StopShowRoutine();
        StopPresentationAnimation();
        ConfigurePresentation();
        foodPanelReady = foodSelectionTarget.gameObject.activeInHierarchy;
        state = TutorialState.None;
        IsTutorialActive = true;
        QueueState(TutorialState.HorseCondition);
    }

    private void HandleFoodPanelShown()
    {
        foodPanelReady = true;

        if (IsTutorialActive &&
            state == TutorialState.WaitingForFoodPanel)
        {
            QueueState(TutorialState.ChooseFood);
        }
    }

    private void HandleFoodDetailsShown(
        ResourceInfoDetailsPopup.DetailsMode mode,
        bool canBuy)
    {
        if (!IsTutorialActive ||
            mode != ResourceInfoDetailsPopup.DetailsMode.HorseResource)
        {
            return;
        }

        QueueState(TutorialState.FeedHorse);
    }

    private void HandleFoodDetailsClosed(
        ResourceInfoDetailsPopup.DetailsMode mode)
    {
        if (!IsTutorialActive ||
            state != TutorialState.FeedHorse ||
            mode != ResourceInfoDetailsPopup.DetailsMode.HorseResource)
        {
            return;
        }

        QueueState(TutorialState.ChooseFood);
    }

    private void HandleFoodApplied(
        float powerPercent,
        float coolingPercent,
        float staminaPercent)
    {
        if (IsTutorialActive)
        {
            CompleteHorseConditionLesson();
            FinishTutorial(true);
        }
    }

    private void HandleNextClicked()
    {
        if (!IsTutorialActive ||
            state != TutorialState.HorseCondition)
        {
            return;
        }

        if (foodPanelReady ||
            foodSelectionTarget.gameObject.activeInHierarchy)
        {
            QueueState(TutorialState.ChooseFood);
            return;
        }

        state = TutorialState.WaitingForFoodPanel;
        presentation.NextButton.interactable = false;
    }

    private void HandleLanguageChanged(string language)
    {
        if (!IsTutorialActive ||
            state == TutorialState.WaitingForFoodPanel)
        {
            return;
        }

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
            case TutorialState.HorseCondition:
                ShowStep(
                    horseConditionTarget,
                    HomeTutorialTextIds.HorseCondition,
                    HomeTutorialTextIds.HorseConditionDescription,
                    true,
                    true,
                    true);
                break;

            case TutorialState.ChooseFood:
                ShowStep(
                    foodSelectionTarget,
                    HomeTutorialTextIds.RestoreHorseCondition,
                    HomeTutorialTextIds.RestoreHorseConditionDescription,
                    true);
                break;

            case TutorialState.FeedHorse:
                ShowStep(
                    feedHorseTarget,
                    HomeTutorialTextIds.FeedHorse,
                    HomeTutorialTextIds.FeedHorseDescription,
                    false);
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

        bool hasTarget =
            target != null &&
            target.gameObject.activeInHierarchy;

        presentation.KeepPopupOnTop();
        if (!hasTarget)
        {
            presentation.ParkHighlight();
            PlacePopup(null);
            StopPresentationAnimation();
            return;
        }

        presentation.ParkHighlight();
        AttachHighlightBehindTarget(target);
        FitHighlightToTarget(target);
        PlacePopup(target);
        StartHighlightPulse();
    }

    private void ConfigurePresentation()
    {
        if (presentation.Highlight.TryGetComponent(
                out Graphic highlightGraphic))
        {
            highlightGraphic.raycastTarget = false;
        }

        Graphic[] popupGraphics =
            presentation.Popup.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in popupGraphics)
            graphic.raycastTarget = false;

        if (presentation.NextButton.TryGetComponent(
                out Graphic nextButtonGraphic))
        {
            nextButtonGraphic.raycastTarget = true;
        }

        presentation.NextButton.onClick.RemoveListener(HandleNextClicked);
        presentation.NextButton.onClick.AddListener(HandleNextClicked);
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
        highlight.SetSiblingIndex(target.GetSiblingIndex());
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
        UITargetPlacementSettings placement =
            UITargetPlacementSettings.Default;
        placement.canvasMargin = Mathf.Max(popupCanvasMargin, 0f);
        placement.targetGap = Mathf.Max(popupTargetGap, 0f);
        placement.rebuildLayoutBeforePlace = true;

        UITargetRelativePlacer.Place(
            presentation.Popup,
            target,
            PresentationCanvasRect,
            placement);
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

    private void StopShowRoutine()
    {
        if (showRoutine == null)
            return;

        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private void FinishTutorial(bool playSuccessHaptic)
    {
        StopShowRoutine();
        StopPresentationAnimation();

        if (presentation != null)
        {
            presentation.NextButton.onClick.RemoveListener(HandleNextClicked);
            presentation.NextButton.gameObject.SetActive(false);
            presentation.RaycastFilter.ClearTarget();
            presentation.Blocker.raycastTarget = false;
            presentation.ParkHighlight();
            presentation.PresentationRoot.SetActive(false);
        }

        state = TutorialState.Finished;
        foodPanelReady = false;
        IsTutorialActive = false;

        if (playSuccessHaptic)
            HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
    }

    private bool HasRequiredReferences()
    {
        return homeUI != null &&
               mapShowPopup != null &&
               foodDetailsPopup != null &&
               horseConditionTarget != null &&
               foodSelectionTarget != null &&
               feedHorseTarget != null &&
               presentation != null &&
               presentation.HasRequiredReferences;
    }

    private static void CompleteHorseConditionLesson()
    {
        HomeTutorialProgress.CompleteContextStep(
            Constants.HomeTutorial.HorseCondition,
            HomeTutorialProgress.ContextLesson.HorseCondition);
    }

    private static void SetText(TMP_Text target, int languageId)
    {
        if (target == null)
            return;

        target.text = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(languageId)
            : $"#{languageId}";
    }
}
