using System;
using System.Collections;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RacingTutorials : MonoBehaviour
{
    [SerializeField] private GameObject tutorialImages;
    [SerializeField] private EsportUITutorial tutorial;

    [Header("Step Indexes")]
    [SerializeField] private int sliderStepIndex = 0;
    [SerializeField] private int rightReinStepIndex = 1;
    [SerializeField] private int leftReinStepIndex = 2;
    [SerializeField] private int firstCamera = 3;
    [SerializeField] private int thirdCamera = 4;
    [SerializeField] private int defentStepIndex = 5;
    [SerializeField] private int defentShowStepIndex = 6;
    [SerializeField] private int sprintShowStepIndex = 7;
    [SerializeField] private int sprintSliderStepIndex = 8;
    [SerializeField] private int sliderFullerStepIndex = 9;
    [SerializeField] private int checkPointStepIndex = 10;
    [SerializeField] private int webSnareBtnStepIndex = 11;
    [SerializeField] private int webSnareShooterStepIndex = 12;
    [SerializeField] private int whereShootStepIndex = 13;
    [SerializeField] private int webSnareDamagedStepIndex = 14;
    [SerializeField] private int walkZonePickStepIndex = 15;
    [SerializeField] private int autoSprinterStepIndex = 16;
    [SerializeField] private int wallObstacleStepIndex = 17;
    //Result Page
    [SerializeField] private int resultPlaceStepIndex = 18;
    [SerializeField] private int horseLevelStepIndex = 19;
    [SerializeField] private int horseConditionsStepIndex = 20;
    [SerializeField] private int racingStatsStepIndex = 21;
    [SerializeField] private int recordTapStepIndex = 22;
    [SerializeField] private int additionalPointStepIndex = 23;
    [SerializeField] private int coinsStepIndex = 24;

    [Header("Delays")]
    [SerializeField] private float afterSliderDelay = 0.7f;
    [SerializeField] private float betweenReinsDelay = 0.8f;
    [SerializeField] private float afterReinsDelay = 1f;
    [SerializeField] private float afterFPDelay = 2f;
    [SerializeField] private float afterDefend = 2f;

    [Header("Buttons")]
    [SerializeField] private Button defendButton;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button webSnareBtn;
    [SerializeField] private Button walkZonePickUpBtn;
    [SerializeField] private Button skipButton;

    [Header("Button Texts")]
    [SerializeField] private TMP_Text defendText;
    [SerializeField] private TMP_Text webSnareCountText;
    [SerializeField] private TMP_Text walkZoneCountText;

    [SerializeField] GameObject obstacleSliderObj;

    private Coroutine tutorialFlowRoutine;

    public static Action<Booster.BoosterType, Booster.BoosterMode> OnItemPicked;
    public static Action OnWallObstacleTutorial;
    public static Action OnShowResultPageTutorial;
    public static Action<bool> OnDontShowTutorial;

    private bool isTargetDismessed =false;
    private bool isSprintFinished =false;
    private bool isWallObstacleShown = false;
    private int finalStepPointer = 0;
    private int[] finalSteps;

    private bool skipTutorialsThisRun = false;
    private bool allowReplayTutorialThisRun = false;
    private bool isSprintShown=false;
    private enum TutorialState
    {
        None,
        Slider,
        RightRein,
        LeftRein,
        Camera,
        Defend,
        Sprint,
        SprintSlider,
        SprintFuller,
        CheckPoint,
        WebSnare,
        WebSnareShooter,
        WhereToShoot,
        WebSnareDamaged,
        WalkZonePickup,
        AutoSprinter,
        WallObstacle,
        ResultPage,
        Finished
    }

    private TutorialState currentState = TutorialState.None;
    private void Start()
    {
        finalSteps = new int[]
        {
        horseLevelStepIndex,
        horseConditionsStepIndex,
        racingStatsStepIndex,
        recordTapStepIndex,
        additionalPointStepIndex,
        coinsStepIndex
        };
        CheckTutorialReplayPopup();
    }
    private void OnEnable()
    {
        StartPowerBar.OnSliderEnabled += StartSliderTutorial;
        StartPowerBar.OnStartPowerSelected += OnSliderSelected;

        ReinZone.OnRightReinUsed += OnRightReinUsed;
        ReinZone.OnLeftReinUsed += OnLeftReinUsed;
        RacingController.OnFirstPersonCamera += OnFirstPersonCameraUsed;
        OnItemPicked += PickedItem;
        defendButton.onClick.AddListener(DefendActivated);
        UIButtonActions.OnSprintStart += SprintStart;
        skipButton.onClick.AddListener(FinishTutorial);
        ReverseWarningUI.OnTimerShowed += CheckPointTutorial;
        PlayerDisablerRacing.OnHorseStopped += ShowWebSnare;
        webSnareBtn.onClick.AddListener(WebSnareBtnClick);
        UIButtonActions.OnWebSnareFinish += ShowShootPoint;
        RacingController.OnTargetDismessed += TargetState;
        BoostersContainer.OnVerySlowState += DamagedWebSnare;
        walkZonePickUpBtn.onClick.AddListener(FinishTutorial);
        OnWallObstacleTutorial += ShowWallObstacle;
        OnShowResultPageTutorial += ShowResultPageTutorial;
    }

    private void OnDisable()
    {
        StartPowerBar.OnSliderEnabled -= StartSliderTutorial;
        StartPowerBar.OnStartPowerSelected -= OnSliderSelected;

        ReinZone.OnRightReinUsed -= OnRightReinUsed;
        ReinZone.OnLeftReinUsed -= OnLeftReinUsed;
        RacingController.OnFirstPersonCamera -= OnFirstPersonCameraUsed;
        OnItemPicked -= PickedItem;
        UIButtonActions.OnSprintStart -= SprintStart;
        defendButton.onClick.RemoveAllListeners();
        skipButton.onClick.RemoveAllListeners();
        ReverseWarningUI.OnTimerShowed -= CheckPointTutorial;
        PlayerDisablerRacing.OnHorseStopped -= ShowWebSnare;
        webSnareBtn.onClick.RemoveAllListeners();
        UIButtonActions.OnWebSnareFinish -= ShowShootPoint;
        RacingController.OnTargetDismessed -= TargetState;
        BoostersContainer.OnVerySlowState -= DamagedWebSnare;
        walkZonePickUpBtn.onClick.RemoveAllListeners();
        OnWallObstacleTutorial -= ShowWallObstacle;
        OnShowResultPageTutorial -= ShowResultPageTutorial;
    }

    private void StartSliderTutorial()
    {
        if (!CanShowTutorial()) return;
        tutorialImages.SetActive(true);
        currentState = TutorialState.Slider;
        tutorial.ShowStep(sliderStepIndex);
    }

    private void OnSliderSelected(float selectedAmount = 0)
    {
        if (currentState != TutorialState.Slider) return;

        if (tutorialFlowRoutine != null)
            StopCoroutine(tutorialFlowRoutine);
        FinishTutorial();
        tutorialFlowRoutine = StartCoroutine(ShowRightReinAfterDelay());
    }

    private IEnumerator ShowRightReinAfterDelay()
    {
        
        yield return new WaitForSecondsRealtime(afterSliderDelay);
        tutorialImages.SetActive(true);
        currentState = TutorialState.RightRein;
        Time.timeScale = 0f;
        tutorial.ShowStep(rightReinStepIndex);
    }

    public void OnRightReinUsed()
    {
        if (!CanShowTutorial()) return;
        if (currentState != TutorialState.RightRein) return;

        if (tutorialFlowRoutine != null)
            StopCoroutine(tutorialFlowRoutine);
        Time.timeScale = 1f;
        tutorialFlowRoutine = StartCoroutine(ShowLeftReinAfterDelay());
    }

    private IEnumerator ShowLeftReinAfterDelay()
    {
        yield return new WaitForSecondsRealtime(betweenReinsDelay);
        currentState = TutorialState.LeftRein;
        Time.timeScale = 0f;
        tutorial.ShowStep(leftReinStepIndex);
    }

    public void OnLeftReinUsed()
    {
        if (!CanShowTutorial()) return;
        if (currentState != TutorialState.LeftRein) return;
        Time.timeScale = 1f;
        FinishTutorial();
        //StartCoroutine(ShowCameraAfterDelay());
    }
    private IEnumerator ShowCameraAfterDelay()
    {
        yield return new WaitForSecondsRealtime(afterReinsDelay);
        tutorialImages.SetActive(true);
        currentState = TutorialState.Camera;
        Time.timeScale = 0f;
        currentState = TutorialState.Camera;
        tutorial.ShowStep(firstCamera);
    }
    public void OnFirstPersonCameraUsed(bool state)
    {
        if (!CanShowTutorial()) return;
        if (currentState != TutorialState.Camera) return;
        Time.timeScale = 1f;
        if (state)
        {
            FinishTutorial();
            StartCoroutine(ShowThirdPersonCameraDelay());
        }
        else
        {
            FinishTutorial();
        }
    }
    private IEnumerator ShowThirdPersonCameraDelay()
    {
        yield return new WaitForSecondsRealtime(afterFPDelay);
        tutorialImages.SetActive(true);
        currentState = TutorialState.Camera;
        Time.timeScale = 0f;
        currentState = TutorialState.Camera;
        tutorial.ShowStep(thirdCamera);
    }

    public void PickedItem(Booster.BoosterType type, Booster.BoosterMode mode)
    {
        if (!CanShowTutorial()) return;
        if (mode==Booster.BoosterMode.Pickup)
        {
            switch (type)
            {
                case Booster.BoosterType.Defend:
                    if (tutorialFlowRoutine != null)
                        tutorialFlowRoutine = null;
                    tutorialFlowRoutine = StartCoroutine(ShowDefendButtonPoint());
                    break;
                case Booster.BoosterType.WalkZone:
                    Debug.Log("Walk zone picked");
                    GotWalkZonePickup();
                    break;
                case Booster.BoosterType.SprintFull:
                    defendButton.interactable = false;
                    tutorialImages.SetActive(true);
                    tutorial.ShowStep(sliderFullerStepIndex);
                    currentState = TutorialState.SprintFuller;
                    Time.timeScale = 0f;
                    break;
                case Booster.BoosterType.WebSnare:
                    webSnareBtn.gameObject.SetActive(true);
                    webSnareBtn.interactable = false;
                    webSnareCountText.text = "1";
                    break;
                case Booster.BoosterType.SetSpeedSprint:
                    tutorialImages.SetActive(true);
                    tutorial.ShowStep(autoSprinterStepIndex);
                    currentState = TutorialState.AutoSprinter;
                    Time.timeScale = 0f;
                    break;
            }
        }
        else
        {
            if(type == Booster.BoosterType.WalkZone)
            {
                if (tutorialFlowRoutine != null)
                    tutorialFlowRoutine = null;
                tutorialFlowRoutine = StartCoroutine(DefendActivator());
            }
        }
      
    }
    IEnumerator ShowDefendButtonPoint()
    {
        yield return null;
        defendButton.gameObject.SetActive(true);
        defendButton.interactable = false;
        defendText.text = "1";
        tutorialImages.SetActive(true);
        tutorial.ShowStep(defentStepIndex);
        currentState = TutorialState.Defend;
        Time.timeScale = 0f;
    }
    IEnumerator DefendActivator()
    {
        yield return new WaitForSecondsRealtime(2f);
        defendButton.interactable = true;
        tutorialImages.SetActive(true);
        currentState = TutorialState.Defend;
        tutorial.ShowStep(defentShowStepIndex);
        Time.timeScale = 0f;

    }
    private void DefendActivated()
    {
        if (!CanShowTutorial()) return;
        Time.timeScale = 1f;
        //defendText.text = "0";

        FinishTutorial();
        if (isSprintFinished)
            return;
        isSprintFinished = true;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(ShowSprintTutorial());
    }
    private IEnumerator ShowSprintTutorial()
    {
        yield return null;
        defendButton.interactable = false;
        yield return new WaitForSecondsRealtime(afterDefend);
        currentState = TutorialState.Sprint;
        sprintButton.gameObject.SetActive(true);
        tutorialImages.SetActive(true);
        tutorial.ShowStep(sprintShowStepIndex);
        Time.timeScale = 0f;
    }
    private void SprintStart()
    {
        if (!CanShowTutorial()) return;
        if(isSprintShown)
            return;
        isSprintShown = true;
        Time.timeScale = 1f;
        FinishTutorial();
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(SprintSlider());
    }
    private IEnumerator SprintSlider()
    {
        yield return new WaitForSecondsRealtime(afterDefend);
        currentState = TutorialState.SprintSlider;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(sprintSliderStepIndex);
        Time.timeScale = 0f;

    }
    public void CheckPointTutorial()
    {
        if (!CanShowTutorial()) return;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(DelayChekPointTutorial());
    }
    private IEnumerator DelayChekPointTutorial()
    {
        yield return new WaitForSecondsRealtime(afterReinsDelay);
        currentState = TutorialState.CheckPoint;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(checkPointStepIndex);
        Time.timeScale = 0f;
    }
    #region Web Snare

    public void ShowWebSnare()
    {
        if (!CanShowTutorial()) return;
        webSnareBtn.interactable = true;
        currentState = TutorialState.WebSnare;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(webSnareBtnStepIndex);

    }
    private void WebSnareBtnClick()
    {
        if (!CanShowTutorial()) return;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(WebSnareClicked());
    }
    private IEnumerator WebSnareClicked()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        currentState = TutorialState.WebSnareShooter;
        tutorial.ShowStep(webSnareShooterStepIndex);
    }
    private void TargetState(bool state)
    {
        if (!CanShowTutorial()) return;
        isTargetDismessed = state;
        if (state)
        {
            UIButtonActions.Instance?.HideShootChain();
            FinishTutorial();
        }
            
    }
    public void ShowShootPoint()
    {
        if (!CanShowTutorial()) return;
        if (isTargetDismessed)
        {
            FinishTutorial();
            return;
        }
            
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(ShowWhereToShoot());
    }
    private IEnumerator ShowWhereToShoot()
    {       
        yield return new WaitForSecondsRealtime(afterSliderDelay);
        if (isTargetDismessed)
            yield break;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(whereShootStepIndex);
        currentState = TutorialState.WhereToShoot;
    }
    public void DamagedWebSnare()
    {
        if (!CanShowTutorial()) return;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(DelaywebnareDamage());
    }
    IEnumerator DelaywebnareDamage()
    {
        yield return new WaitForSecondsRealtime(afterSliderDelay);
        currentState = TutorialState.WebSnareDamaged;
        defendButton.interactable = true;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(webSnareDamagedStepIndex);
        Time.timeScale = 0f;
    }
    #endregion
    public void GotWalkZonePickup()
    {
        if (!CanShowTutorial()) return;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(DelayWalkZonepickUp());
    }
    IEnumerator DelayWalkZonepickUp()
    {
        yield return new WaitForSecondsRealtime(afterReinsDelay);
        currentState = TutorialState.WalkZonePickup;
        walkZonePickUpBtn.gameObject.SetActive(true);
        walkZoneCountText.text = "1";
        tutorialImages.SetActive(true);
        tutorial.ShowStep(walkZonePickStepIndex);
        Time.timeScale = 0f;
    }

    public void ShowWallObstacle()
    {
        if (!CanShowTutorial()) return;
        if (isWallObstacleShown)
            return;
        isWallObstacleShown = true;
        if(obstacleSliderObj != null)
            obstacleSliderObj.SetActive(true);
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(WallObstacleDelay());
    }
    IEnumerator WallObstacleDelay()
    {
        yield return new WaitForSecondsRealtime(afterReinsDelay);
        currentState = TutorialState.WallObstacle;
        tutorialImages.SetActive(true);
        tutorial.ShowStep(wallObstacleStepIndex);
        Time.timeScale = 0f;
    }
    private void FinishTutorial()
    {
        if (!CanShowTutorial()) return;
        if (currentState == TutorialState.ResultPage)
        {
            if (finalStepPointer < finalSteps.Length)
            {
                tutorial.ShowStep(finalSteps[finalStepPointer]);
                finalStepPointer++;
                return;
            }
            currentState = TutorialState.None;
            if (!PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
            {
                PlayerPrefs.SetInt(Constants.Tutorial.TutorialPlay, 1);
            }

            DataManager.Instance?.SetTutorialDone();
        }
        //else if (currentState == TutorialState.WebSnare)

        if (Time.timeScale == 0)
        {
            Time.timeScale = 1f;
        }

        currentState = TutorialState.Finished;
        tutorialImages.SetActive(false);
        tutorial.Finish();
    }
    #region Result Page Tutorial
    public void ShowResultPageTutorial()
    {
        if (!CanShowTutorial()) return;
        if (tutorialFlowRoutine != null)
            tutorialFlowRoutine = null;
        tutorialFlowRoutine = StartCoroutine(ResultPlace());
    }
    private IEnumerator ResultPlace()
    {
        yield return new WaitForSecondsRealtime(afterSliderDelay);
        tutorialImages.SetActive(true);
        tutorial.ShowStep(resultPlaceStepIndex);
        currentState= TutorialState.ResultPage;
    }
    #endregion
    #region Skip tutorial
    private bool CanShowTutorial()
    {
        return !skipTutorialsThisRun;
    }
    public void CheckTutorialReplayPopup()
    {
        skipTutorialsThisRun = false;
        allowReplayTutorialThisRun = false;

        // agar birinchi marta bo‘lsa popup chiqmaydi
        if (!PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
            return;

        Time.timeScale = 0f;

        // 🔥 shu yerda o‘zingdagi popupni chaqirasan
        // title, desc, yes, no
        UIOverlayRoot.I.Confirm(
            titleId: 475,          // o‘zingning id ni qo‘yasan
            descId: 476,           // "Would you like to play the tutorial again?"
            okTextId: 1,         // "Yes"
            cancelTextId: 2,     // "No"
            onOk: OnReplayTutorialYes,
            onCancel: OnReplayTutorialNo
        );
    }
    public void OnReplayTutorialYes()
    {
        skipTutorialsThisRun = false;
        allowReplayTutorialThisRun = true;
        Time.timeScale = 1f;

        // tutorialni boshidan boshlash
        StartSliderTutorial();
    }

    public void OnReplayTutorialNo()
    {
        skipTutorialsThisRun = true;
        allowReplayTutorialThisRun = false;
        Time.timeScale = 1f;

        tutorialImages.SetActive(false);
        tutorial.Finish();
        defendButton.gameObject.SetActive(true);
        defendButton.interactable = true;
        webSnareBtn.gameObject.SetActive(true);
        webSnareBtn.interactable = true;
        sprintButton.gameObject.SetActive(true);
        OnDontShowTutorial?.Invoke(true);
    }
    #endregion
}
