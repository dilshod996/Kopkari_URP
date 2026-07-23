using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MalbersAnimations;
using MalbersAnimations.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KopkariMainUI : MonoBehaviour
{
    public static KopkariMainUI Instance;

    #region Inspector - Buttons
    [Header("Buttons")]
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button shootWebBtn;
    [SerializeField] private Button chainContainerBtn;
    [SerializeField] private Button fakeUlakBtn;
    [SerializeField] private Button pushButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button cameraSwitchButton;
    #endregion

    #region Kopkari Intro
    [Header("Kopkari Intro")]
    [SerializeField] private KopkariIntro introPage;
    #endregion

    #region Inspector - Texts
    [Header("Buttons Data Texts")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text webSnareCounter;
    [SerializeField] private TMP_Text fakeUlakCountText;
    [SerializeField, Min(0.1f)] private float fakeUlakCooldown = 3f;
    #endregion

    #region Effects / Sprint
    [SerializeField] private Image sprintImg;

    #endregion
    #region Inspector - Sprint Timings
    [Header("Sprint Timings")]
    public float drainDuration = 5f;
    public float refillDelay = 6f;
    public float refillDuration = 3f;

    private float refillRate => 1f / Mathf.Max(0.0001f, refillDuration);
    private float drainRate => 1f / Mathf.Max(0.0001f, drainDuration);
    [SerializeField] private Slider sprintSlider;
    #endregion
    #region Inspector - Others
    [Header("Pages")]
    [SerializeField] private KopkariResultUI resultPage;
    [SerializeField] private GameObject pickupButton;
    [SerializeField] private UIGetLamp pickupProgress;
    [SerializeField] private KopkariRoundChangePopup kopkariRoundChangeUI;
    [SerializeField] private ComboPrize comboPrizeUI;
    [SerializeField] private UIPauseGame pauseMenu;
    [SerializeField] private HowToPlay howToPlayPage;
    [SerializeField] private GameObject foodPanel;

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;

    public bool WeaponInHand;
    #endregion

    [Header("Top Slider && BottomUI")]
    [SerializeField] private RectTransform bottomUI;
    [SerializeField] private GameObject matchStatusBackground;
    [SerializeField] private TMP_Text mainTimeText;
    private int lastDisplayedMainTimeSeconds = int.MinValue;
    [SerializeField] private TMP_Text roundProgressText;
    [Header("Horse Health")]
    [SerializeField] private Slider horseHealthSlider;
    [SerializeField] private TMP_Text horseHealthAmountText;
    [SerializeField] private string horseHealthStatName = "Health";
    [Header("Carrier Grip")]
    [SerializeField] private GameObject carrierInfoBackground;
    [SerializeField] private TMP_Text carrierGripAmountText;
    [SerializeField] private Slider carrierGripSlider;
    [SerializeField] private TMP_Text carrierNameText;
    [Header("Local Grip Feedback")]
    [SerializeField, Min(0.01f)] private float gripLossPunchScale = 0.06f;
    [SerializeField, Min(0.05f)] private float gripLossPunchDuration = 0.2f;
    private Tween carrierGripFeedbackTween;
    private Vector3 carrierInfoBaseScale = Vector3.one;
    private bool keepDropFeedbackVisible;


    #region Show and Hide UI Animation Data

    [Header("Scale Animation")]
    [SerializeField] private float punchScaleM = 1.2f;
    [SerializeField] private float scaleTime = 0.2f;
    [Header("Fade Settings For UI Pages")]
    [SerializeField] private bool useFade = true;

    #endregion

    #region Game Canvases
    [SerializeField] private GameObject mobileCanvas;
    [SerializeField] private GameObject roomCanvas;
    #endregion[Header("Game Canvases")]

    #region Events (DO NOT REMOVE)
    public static Action OnSprintStart;   // ✅ QAYTDI
    public static Action OnSprintEnd;       // Speed restore
    public static Action<float> OnSprintHold;

    public static Action OnWebSnareBtnEnable;
    public static Action OnWebSnareStart;
    public static Action OnWebSnareFinish;
    public static Action OnEverythingReadyStart;
    public static Action OnHorsePushEffect;
    public static Action<BoostersContainer> OnBindRequested;
    #endregion

    #region Runtime - Sprint State
    private bool isPressing;
    private bool isDamaged;
    private bool canSprint = true;
    private bool isPointerHeld;
    private bool autoSprintBoostActive;
    private bool webSnareShotActive;
    private bool fakeUlakCooldownActive;
    private bool fakeUlakFocusAvailable;
    private bool horseHealthDepletionHandled;

    private Coroutine drainRoutine;
    private Coroutine refillRoutine;
    private float totalHoldTime = 0f;
    private float totalWebSnareTime = 0f;
    #endregion
    private Coroutine canvasRoutine;
    private Coroutine moveBottomRoutine;
    private bool loadingCompleted;
    private BoostersContainer boundBoosters;
    private MalbersAnimations.Stat boundHorseHealth;
    #region Awake/Start/OnEnable/OnDisable
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);

        introPage?.PrepareHidden();
        kopkariRoundChangeUI?.HideAll();
        comboPrizeUI?.Hide();
        if (carrierInfoBackground != null)
            carrierInfoBaseScale = carrierInfoBackground.transform.localScale;
        HideCarrierGrip();
        UpdateCarrierGrip(100f, 100f);
        SetMatchStatusVisible(false);
        UpdateMainTime(0f);
        UpdateRoundProgress(0, 0);
        UpdateHorseHealthUI(1f, 1f);
    }
    private void OnEnable()
    {
        KopkariManager.OnSceneReady += CompleteLoadingPanel;
        if (KopkariManager.IsSceneReady)
            CompleteLoadingPanel();

        KopkariManager.OnGameStartFinishState += CanvasEnable;
        KopkariManager.OnGoatOwnerChanged += HandleCarrierOwnerChanged;
        KopkariManager.OnFakeUlakDiversionStateChanged += HandleFakeUlakDiversionStateChanged;
        AIKopkariRider.OnCarrierGripChanged += HandleAICarrierGripChanged;
        PlayerDataManager.OnRiderAndHorse += BindHorseHealth;
        OnBindRequested += Bind;
        HoldInputForwarder.OnPickupFocusChanged += HandleFakeUlakPickupFocusChanged;
        Booster.OnSprintFull += HandleSprintFull;

        BoostersContainer.OnSprintEffectStart += ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd += HideSprintEffectNoForce;
        BoostersContainer.OnAutoSprintBoostStart += StopManualSprintForAutoBoost;

        BoostersContainer.OnWalkZoneAdded += UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved += UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded += UpdateDefendText;
        BoostersContainer.OnDefendRemoved += UpdateDefendText;

        BoostersContainer.OnWebSnareAdded += UpdateWebCount;
        BoostersContainer.OnFakeUlakAdded += UpdateFakeUlakText;
        BoostersContainer.OnFakeUlakRemoved += UpdateFakeUlakText;

       // RacingController.OnRacingFinished += ShowResultPage;
        //KopkariManager.OnGameStarted += GetData;

        BoostersContainer.OnDefendState += HandleDefendStateChanged;
        BoostersContainer.OnWalkZoneDamaged += EnableSprint;
        BoostersContainer.OnWebSnareDamaged += EnableSprint;
        BoostersContainer.OnObstacleDamage += OnObstacleDamageHandler;
        KopkariManager.OnMainGameStarted += MoveUP;
        if (pushButton != null)
            pushButton.onClick.AddListener(PushEffectStart);
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseMenu);
        if (cameraSwitchButton != null)
            cameraSwitchButton.onClick.AddListener(ToggleCameraView);

        if (KopkariManager.Instance != null)
        {
            fakeUlakCooldownActive = KopkariManager.Instance.IsFakeUlakDiversionActive;
            HandleCarrierOwnerChanged(KopkariManager.Instance.currentGoatOwner);
            BindHorseHealth(KopkariManager.Instance.horseAnimal, KopkariManager.Instance.LocalRiderAnimal);
        }

        fakeUlakFocusAvailable = pickupButton != null && pickupButton.activeInHierarchy;
        RefreshFakeUlakButtonState();

        RestoreSprintRefillAfterEnable();
    }

    private void OnDisable()
    {
        KopkariManager.OnSceneReady -= CompleteLoadingPanel;

        KopkariManager.OnGameStartFinishState -= CanvasEnable;
        KopkariManager.OnGoatOwnerChanged -= HandleCarrierOwnerChanged;
        KopkariManager.OnFakeUlakDiversionStateChanged -= HandleFakeUlakDiversionStateChanged;
        AIKopkariRider.OnCarrierGripChanged -= HandleAICarrierGripChanged;
        PlayerDataManager.OnRiderAndHorse -= BindHorseHealth;
        UnbindHorseHealth();
        OnBindRequested -= Bind;
        HoldInputForwarder.OnPickupFocusChanged -= HandleFakeUlakPickupFocusChanged;

        Booster.OnSprintFull -= HandleSprintFull;

        BoostersContainer.OnSprintEffectStart -= ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd -= HideSprintEffectNoForce;
        BoostersContainer.OnAutoSprintBoostStart -= StopManualSprintForAutoBoost;

        BoostersContainer.OnWalkZoneAdded -= UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved -= UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded -= UpdateDefendText;
        BoostersContainer.OnDefendRemoved -= UpdateDefendText;

        BoostersContainer.OnWebSnareAdded -= UpdateWebCount;
        BoostersContainer.OnFakeUlakAdded -= UpdateFakeUlakText;
        BoostersContainer.OnFakeUlakRemoved -= UpdateFakeUlakText;

        //KopkariManager.OnRacingFinished -= ShowResultPage;
        //RacingController.OnRacingStarted -= GetData;
        BoostersContainer.OnDefendState -= HandleDefendStateChanged;
        BoostersContainer.OnWalkZoneDamaged -= EnableSprint;
        BoostersContainer.OnWebSnareDamaged -= EnableSprint;
        BoostersContainer.OnObstacleDamage -= OnObstacleDamageHandler;
        KopkariManager.OnMainGameStarted -= MoveUP;
        if (pushButton != null)
            pushButton.onClick.RemoveListener(PushEffectStart);
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(PauseMenu);
        if (cameraSwitchButton != null)
            cameraSwitchButton.onClick.RemoveListener(ToggleCameraView);
        if (walkZoneBtn != null)
            walkZoneBtn.onClick.RemoveListener(HandleWalkZoneClicked);
        if (defendBtn != null)
            defendBtn.onClick.RemoveListener(HandleDefendClicked);
        if (fakeUlakBtn != null)
            fakeUlakBtn.onClick.RemoveListener(HandleFakeUlakClicked);
        ReleaseSprintForUIInterruption();
        StopIfRunning(ref refillRoutine);
        StopCarrierGripFeedback();
        StopAllCoroutines();
        canvasRoutine = null;
        moveBottomRoutine = null;
        autoSprintBoostActive = false;
    }
    #endregion

    #region Text Updates
    public void UpdateDefendText(int count)
    {
        if (defendCountText != null)
            defendCountText.text = count.ToString();
        SetDefendState(count > 0);
    }

    public void UpdateWalkZoneText(int count)
    {
        if (walkZoneCountText != null)
            walkZoneCountText.text = count.ToString();
        SetWalkZoneState(count > 0);
    }

    public void UpdateWebCount(int count)
    {
        if (webSnareCounter != null)
            webSnareCounter.text = count.ToString();
        SetWebState(count > 0);
    }

    public void UpdateFakeUlakText(int count)
    {
        if (fakeUlakCountText != null)
            fakeUlakCountText.SetText("{0}", Mathf.Max(0, count));
        RefreshFakeUlakButtonState(count);
    }

    private void BindHorseHealth(MAnimal horse, MAnimal rider)
    {
        UnbindHorseHealth();
        horseHealthDepletionHandled = false;
        if (horse == null)
            return;

        MalbersAnimations.Stats stats = horse.GetComponent<MalbersAnimations.Stats>();
        if (stats == null)
            stats = horse.GetComponentInParent<MalbersAnimations.Stats>();
        if (stats == null)
            stats = horse.GetComponentInChildren<MalbersAnimations.Stats>(true);

        boundHorseHealth = stats != null ? stats.Stat_Get(horseHealthStatName) : null;

        // Some horse prefabs keep Stats beside the damage receiver instead of
        // on the MAnimal object. Follow that exact receiver reference as a fallback.
        if (boundHorseHealth == null)
        {
            MDamageable[] damageables = horse.GetComponentsInChildren<MDamageable>(true);
            for (int i = 0; i < damageables.Length; i++)
            {
                MalbersAnimations.Stats damageStats = damageables[i] != null ? damageables[i].stats : null;
                MalbersAnimations.Stat health = damageStats != null
                    ? damageStats.Stat_Get(horseHealthStatName)
                    : null;
                if (health == null)
                    continue;

                boundHorseHealth = health;
                break;
            }
        }

        if (boundHorseHealth == null)
        {
            Debug.LogWarning($"[{nameof(KopkariMainUI)}] The local horse has no '{horseHealthStatName}' Malbers stat.", horse);
            return;
        }

        boundHorseHealth.OnValueChange.AddListener(HandleHorseHealthChanged);
        boundHorseHealth.OnMaxValueChange.AddListener(HandleHorseHealthMaxChanged);
        RefreshHorseHealthUI();
    }

    private void UnbindHorseHealth()
    {
        if (boundHorseHealth == null)
            return;

        boundHorseHealth.OnValueChange.RemoveListener(HandleHorseHealthChanged);
        boundHorseHealth.OnMaxValueChange.RemoveListener(HandleHorseHealthMaxChanged);
        boundHorseHealth = null;
    }

    private void HandleHorseHealthChanged(float value)
    {
        RefreshHorseHealthUI();

        if (value > 0f || horseHealthDepletionHandled)
            return;

        KopkariManager manager = KopkariManager.Instance;
        if (manager != null && manager.roomState != KopkariManager.RoomState.GameStarted)
            return;

        horseHealthDepletionHandled = true;
        ShowResult();
    }

    private void HandleHorseHealthMaxChanged(float value)
    {
        RefreshHorseHealthUI();
    }

    private void RefreshHorseHealthUI()
    {
        if (boundHorseHealth == null)
            return;

        UpdateHorseHealthUI(boundHorseHealth.Value, boundHorseHealth.MaxValue);
    }

    private void UpdateHorseHealthUI(float current, float maximum)
    {
        float safeMaximum = Mathf.Max(0.0001f, maximum);
        float safeCurrent = Mathf.Clamp(current, 0f, safeMaximum);

        if (horseHealthSlider != null)
        {
            horseHealthSlider.minValue = 0f;
            horseHealthSlider.maxValue = safeMaximum;
            horseHealthSlider.SetValueWithoutNotify(safeCurrent);
        }

        if (horseHealthAmountText != null)
            horseHealthAmountText.text = $"{Mathf.CeilToInt(safeCurrent)} / {Mathf.CeilToInt(safeMaximum)}";
    }
    #endregion

    #region Button State Updates
    public void SetSprintState(bool state)
    {
        canSprint = state;
        RefreshSprintButtonState();
    }

    private void RefreshSprintButtonState()
    {
        bool hasEnergy = sprintSlider == null || sprintSlider.value >= 0.001f;
        bool interactable =
            canSprint &&
            !autoSprintBoostActive &&
            !isDamaged &&
            hasEnergy &&
            CanSprintNow();

        if (sprintBtn != null)
            sprintBtn.interactable = interactable;
    }

    private void RestoreSprintRefillAfterEnable()
    {
        if (sprintSlider == null || sprintSlider.value >= 1f || isDamaged || autoSprintBoostActive)
        {
            RefreshSprintButtonState();
            return;
        }

        // Coroutines are stopped when this GameObject is disabled. If the UI is
        // enabled with partially depleted energy, restart the refill that was lost.
        SetSprintState(false);
        StopIfRunning(ref refillRoutine);
        refillRoutine = StartCoroutine(RefillDelayedCoroutine());
    }

    private void EnsureSprintRefillRunning()
    {
        if (!isActiveAndEnabled || sprintSlider == null || sprintSlider.value >= 1f ||
            refillRoutine != null)
        {
            return;
        }

        refillRoutine = StartCoroutine(RefillDelayedCoroutine());
    }

    public void SetJumpState(bool state)
    {
        if (jumpBtn != null)
            jumpBtn.interactable = state;
    }
    public void SetDefendState(bool state)
    {
        if (!state)
        {
            if (defendBtn != null)
                defendBtn.interactable = false;
            return;
        }

        RefreshDefendButtonState();
    }
    public void SetWalkZoneState(bool state)
    {
        if (walkZoneBtn != null)
            walkZoneBtn.interactable = state;
    }

    public void SetWebState(bool state)
    {
        if (shootWebBtn != null)
            shootWebBtn.interactable = state;
    }
    #endregion

    #region Data (Inventory)
    public void GetData()
    {
        int defCount = GetItemAmount(Constants.PlayerItems.Defense);
        int slowCount = GetItemAmount(Constants.PlayerItems.SlowDown);
        int webCount = GetItemAmount(Constants.PlayerItems.WebSnare);
        int fakeUlakCount = GetItemAmount(Constants.PlayerItems.FakeUlak);

        InitializeData(defCount, slowCount, webCount);
        UpdateFakeUlakText(fakeUlakCount);
    }

    public void InitializeData(int defendCount, int walkZoneCount, int webCount)
    {
        UpdateDefendText(defendCount);
        UpdateWalkZoneText(walkZoneCount);
        UpdateWebCount(webCount);
    }

    private int GetItemAmount(string itemKey)
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.GetItemAmount(itemKey);

        int defaultAmount = itemKey == Constants.PlayerItems.FakeUlak ? 3 : 0;
        return PlayerPrefs.GetInt(itemKey, defaultAmount);
    }

    #endregion

    #region Bind Player BoostersContainer
    public void Bind(BoostersContainer boosters)
    {
        if (boosters == null || boosters.isNpc)
            return;

        boundBoosters = boosters;

        if (walkZoneBtn != null)
        {
            walkZoneBtn.onClick.RemoveListener(HandleWalkZoneClicked);
            walkZoneBtn.onClick.AddListener(HandleWalkZoneClicked);
        }

        if (defendBtn != null)
        {
            defendBtn.onClick.RemoveListener(HandleDefendClicked);
            defendBtn.onClick.AddListener(HandleDefendClicked);
        }

        if (fakeUlakBtn != null)
        {
            fakeUlakBtn.onClick.RemoveListener(HandleFakeUlakClicked);
            fakeUlakBtn.onClick.AddListener(HandleFakeUlakClicked);
        }

        RefreshDefendButtonState();
        RefreshFakeUlakButtonState();
    }

    private void HandleWalkZoneClicked() => boundBoosters?.DropWalkTrap();

    private void HandleDefendClicked()
    {
        if (boundBoosters == null || IsBoundDefendRunning())
            return;

        SetDefendState(false);
        boundBoosters.DefendPlayer();
    }

    private void HandleDefendStateChanged(bool available)
    {
        if (!available)
        {
            SetDefendState(false);
            return;
        }

        RefreshDefendButtonState();
    }

    private void RefreshDefendButtonState(int? countOverride = null)
    {
        if (defendBtn == null)
            return;

        int count = countOverride ?? GetItemAmount(Constants.PlayerItems.Defense);
        defendBtn.interactable = count > 0 && !IsBoundDefendRunning();
    }

    private bool IsBoundDefendRunning()
    {
        return boundBoosters != null &&
               (boundBoosters.isDefend ||
                (boundBoosters.defendQobiq != null && boundBoosters.defendQobiq.activeSelf));
    }

    private void HandleFakeUlakClicked()
    {
        if (boundBoosters == null || fakeUlakCooldownActive || !fakeUlakFocusAvailable)
            return;

        boundBoosters.TryUseFakeUlak(fakeUlakCooldown);
        RefreshFakeUlakButtonState();
    }

    private void HandleFakeUlakDiversionStateChanged(bool active)
    {
        fakeUlakCooldownActive = active;
        RefreshFakeUlakButtonState();
    }

    private void HandleFakeUlakPickupFocusChanged(bool focused)
    {
        fakeUlakFocusAvailable = focused;
        RefreshFakeUlakButtonState();
    }

    private void RefreshFakeUlakButtonState(int? countOverride = null)
    {
        if (fakeUlakBtn == null)
            return;

        int count = countOverride ?? GetItemAmount(Constants.PlayerItems.FakeUlak);
        KopkariManager manager = KopkariManager.Instance;
        bool managerReady = manager != null &&
                            manager.currentGoatOwner == null &&
                            manager.CanActivateFakeUlakDiversion;
        bool pickupFocused = fakeUlakFocusAvailable &&
                             pickupButton != null &&
                             pickupButton.activeInHierarchy;
        fakeUlakBtn.interactable = count > 0 &&
                                   !fakeUlakCooldownActive &&
                                   pickupFocused &&
                                   managerReady;
    }
    #endregion

    #region UI Pages (Show/Hide)
    public void ShowUI(MonoBehaviour ui)
    {
        if (ui != null)
            ShowUI(ui.gameObject);
    }

    public void HideUI(MonoBehaviour ui)
    {
        if (ui != null)
            HideUI(ui.gameObject);
    }

    public void ShowUI(GameObject page)
    {
        if (!page) return;

        ReleaseSprintForUIInterruption();

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        page.SetActive(true);
        if (rt == null)
            return;

        rt.localScale = Vector3.one * startScale;
        if (cg != null)
        {
            cg.alpha = 0f;
            LeanTween.alphaCanvas(cg, 1f, fadeTime).setIgnoreTimeScale(true);
        }

        LeanTween.scale(rt, Vector3.one * punchScale, animTime)
            .setIgnoreTimeScale(true)
            .setEase(easeIn)
            .setOnComplete(() =>
            {
                LeanTween.scale(rt, Vector3.one, animTime * 0.7f)
                    .setIgnoreTimeScale(true)
                    .setEase(easeOut);
            });
    }

    public void HideUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        if (rt == null)
        {
            page.SetActive(false);
            return;
        }

        if (cg != null)
            LeanTween.alphaCanvas(cg, 0f, fadeTime).setIgnoreTimeScale(true);
        LeanTween.scale(rt, Vector3.one * startScale, animTime)
            .setIgnoreTimeScale(true)
            .setEase(easeOut)
            .setOnComplete(() => page.SetActive(false));
    }
    #endregion

    #region Sprint
    public void OnSprintButtonDown(BaseEventData data) => HandlePointerDown();
    public void OnSprintButtonUp(BaseEventData data) => HandlePointerUp();

    private void HandlePointerDown()
    {
        isPointerHeld = true;

        // ✅ EventTrigger interactable=false bo‘lsa ham callback chaqirishi mumkin
        if (sprintBtn != null && !sprintBtn.interactable)
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.LowCondition);
            return;
        }

        Debug.Log("Pressed");
        if (isDamaged || isPressing || (sprintSlider != null && sprintSlider.value <= 0.0001f))
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.LowCondition);
            return;
        }

        StartSprintDrain(stopRefillRoutine: true);
    }

    private void StartSprintDrain(bool stopRefillRoutine)
    {
        isPressing = true;
        OnSprintStart?.Invoke();
        sprintImg?.gameObject.SetActive(true);

        if (stopRefillRoutine)
            StopIfRunning(ref refillRoutine);

        StopIfRunning(ref drainRoutine);

        drainRoutine = StartCoroutine(DrainCoroutine());

        RefreshSprintButtonState();
    }

    private void HandlePointerUp()
    {
        isPointerHeld = false;

        // Agar hozir pressing bo‘lsa — har doim release qilamiz
        if (isPressing)
        {
            ForceReleaseSprint(startRefill: true);
            return;
        }

        // pressing bo'lmasa, shunda interactable false bo'lsa skip qilsa ham bo'ladi
        if (sprintBtn != null && !sprintBtn.interactable)
            return;
    }


    private IEnumerator DrainCoroutine()
    {
        while (isPressing)
        {
            if (sprintSlider != null)
                sprintSlider.value = Mathf.Max(0f, sprintSlider.value - drainRate * Time.unscaledDeltaTime);

            totalHoldTime += Time.unscaledDeltaTime;
            OnSprintHold?.Invoke(totalHoldTime);

            if (sprintSlider != null && sprintSlider.value <= 0.0001f)
            {
                sprintSlider.value = 0f;
                HomeHapticsManager.Instance?.Play(HomeHapticId.LowCondition);

                ForceReleaseSprint(startRefill: true);
                SetSprintState(false);
                yield break;
            }

            yield return null;
        }
    }

    private void ForceReleaseSprint(bool startRefill)
    {
        if (isPressing)
        {
            isPressing = false;

        }
        OnSprintEnd?.Invoke();
        sprintImg?.gameObject.SetActive(false);
        StopIfRunning(ref drainRoutine);

        if (startRefill)
        {
            StopIfRunning(ref refillRoutine);
            refillRoutine = StartCoroutine(RefillDelayedCoroutine());
        }

        RefreshSprintButtonState();
    }

    private IEnumerator RefillDelayedCoroutine()
    {
        yield return new WaitForSecondsRealtime(refillDelay);

        SetSprintState(false);

        while (!isPressing && sprintSlider != null && sprintSlider.value < 1f)
        {
            sprintSlider.value = Mathf.Min(1f, sprintSlider.value + refillRate * Time.deltaTime);
            yield return null;
        }

        if (sprintSlider != null)
            sprintSlider.value = Mathf.Clamp01(sprintSlider.value);

        if (!isDamaged)
        {
            SetSprintState(true);

            if (isPointerHeld && CanSprintNow() && sprintBtn != null && sprintBtn.interactable)
                StartSprintDrain(stopRefillRoutine: false);
        }

        refillRoutine = null;
    }

    public void HandleSprintFull()
    {
        StopIfRunning(ref drainRoutine);
        StopIfRunning(ref refillRoutine);

        if (sprintSlider != null)
            sprintSlider.value = 1f;

        SetSprintState(true);

        // agar user bosib turgan bo‘lsa drain davom etadi
        if (isPressing && CanSprintNow())
            drainRoutine = StartCoroutine(DrainCoroutine());
    }

    private bool CanSprintNow()
    {
        if (!canSprint) return false;
        if (sprintSlider != null && sprintSlider.value <= 0.0001f) return false;
        return true;
    }
    #endregion

    #region Damage / Debuff
    private void EnableSprint(bool debuffOn)
    {
        // debuff tushganda bosib turgan bo'lsa avval release
        if (debuffOn && isPressing)
            ForceReleaseSprint(startRefill: true);

        isDamaged = debuffOn;

        if (debuffOn && sprintImg != null && sprintImg.gameObject.activeSelf)
            sprintImg.gameObject.SetActive(false);

        RefreshSprintButtonState();
    }
    #endregion

    #region Game Started Methods

    private void CanvasEnable(bool state)
    {
        // Oldingi coroutine bo‘lsa to‘xtatamiz
        if (canvasRoutine != null)
        {
            StopCoroutine(canvasRoutine);
            Debug.Log("clean");
            canvasRoutine = null;
        }

        if (state)
        {
            // Darhol yoqiladi
            SetMobileCanvasVisible(true);
            if (roomCanvas != null)
                roomCanvas.SetActive(true);
            SetMatchStatusVisible(true);
            Debug.Log("Yoq");
        }
        else
        {
            ReleaseSprintForUIInterruption();
            SetMobileCanvasVisible(false);
            SetMatchStatusVisible(false);
            // 2 sekunddan keyin o'chadi
            canvasRoutine = StartCoroutine(DisableCanvasDelayed());
        }
    }

    private IEnumerator DisableCanvasDelayed()
    {
        
        MoveDown();
        yield return new WaitForSeconds(2f);
        Debug.Log("Uchir");
        if (mobileCanvas != null)
            mobileCanvas.SetActive(false);
        if (roomCanvas != null)
            roomCanvas.SetActive(false);

        canvasRoutine = null;
    }
    #endregion

    #region LoadingPanel
    public void LoadingPanel(float time)
    {
        if (KopkariManager.IsSceneReady)
            CompleteLoadingPanel();
    }

    private void CompleteLoadingPanel()
    {
        if (loadingCompleted) return;
        loadingCompleted = true;

        GetData();

        if (introPage != null)
        {
            introPage.gameObject.SetActive(true);
            introPage.Play(StartMatchAfterIntro);
        }
        else
        {
            StartMatchAfterIntro();
        }
    }

    private void StartMatchAfterIntro()
    {
        OnEverythingReadyStart?.Invoke();
    }

    public void ShowRoundChange()
    {
        ShowRoundChange(KopkariRoundChangePopup.DisplayReason.RoundFinished);
    }

    public void ShowRoundChange(string unusedDetails)
    {
        ShowRoundChange(KopkariRoundChangePopup.DisplayReason.RoundFinished);
    }

    public void ShowRoundChange(KopkariRoundChangePopup.DisplayReason reason)
    {
        ReleaseSprintForUIInterruption();
        if (kopkariRoundChangeUI == null)
            return;

        bool canStartNextRound = KopkariManager.Instance != null &&
                                  KopkariManager.Instance.HasPreparedNextRound;
        ShowUI(kopkariRoundChangeUI);
        kopkariRoundChangeUI.ShowRoundChange(canStartNextRound, reason);
    }

    public void HideRoundChange()
    {
        kopkariRoundChangeUI?.HideRoundChange();
    }

    public void ShowRoundFoodPanel()
    {
        if (foodPanel == null)
            return;

        GameFood gameFood = foodPanel.GetComponent<GameFood>();
        if (gameFood == null)
            gameFood = foodPanel.GetComponentInChildren<GameFood>(true);
        gameFood?.ShowForKopkariRoundChange(
            kopkariRoundChangeUI != null ? kopkariRoundChangeUI.CriticalConditionPercent : 15f);
        ShowUI(foodPanel);
    }

    public void ShowHowToPlayPage()
    {
        if (howToPlayPage == null)
            return;

       // HideUI(pauseMenu);
        ShowUI(howToPlayPage);
    }

    public void HideHowToPlayPage()
    {
        if (howToPlayPage == null)
            return;

        HideUI(howToPlayPage);
        ShowUI(pauseMenu);
    }

    public void HideRoundFoodPanel()
    {
        HideUI(foodPanel);
        kopkariRoundChangeUI?.RefreshHorseConditionAttention();
    }

    public void ShowRoundWarmupCountdown(int seconds)
    {
        ShowRoundWarmupCountdown(
            seconds,
            KopkariRoundChangePopup.WarmupPhase.ReachWarmupPoint);
    }

    public void ShowRoundWarmupCountdown(
        int seconds,
        KopkariRoundChangePopup.WarmupPhase phase)
    {
        ReleaseSprintForUIInterruption();
        if (kopkariRoundChangeUI == null)
            return;

        // The countdown updates once per second. Animate only when its root is
        // first activated, not every time the displayed number changes.
        if (!kopkariRoundChangeUI.gameObject.activeSelf)
            ShowUI(kopkariRoundChangeUI);

        kopkariRoundChangeUI.ShowWarmupCountdown(seconds, phase);
    }

    public void HideRoundWarmupCountdown()
    {
        kopkariRoundChangeUI?.HideWarmupCountdown();
    }

    public void ShowResult()
    {
        HidePickupForRoundTransition();
        kopkariRoundChangeUI?.HideAll();
        HideCombo();
        HideCarrierGrip();
        if (KopkariManager.Instance != null)
            KopkariManager.Instance.FinishMatch();
        else
            KopkariResultsManager.Instance?.EndRace();
        CanvasEnable(false);

        if (resultPage == null)
            return;

        bool wasAlreadyActive = resultPage.gameObject.activeSelf;
        ShowUI(resultPage);
        if (wasAlreadyActive)
            resultPage.RefreshFromResults();
    }

    public void HidePickupForRoundTransition()
    {
        pickupProgress?.CancelImmediately();
        if (pickupButton != null)
            pickupButton.SetActive(false);
    }

    public void UpdateMainTime(float remainingTime)
    {
        if (mainTimeText == null)
            return;

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        if (totalSeconds == lastDisplayedMainTimeSeconds)
            return;

        lastDisplayedMainTimeSeconds = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        mainTimeText.SetText("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetMobileCanvasVisible(bool visible)
    {
        if (mobileCanvas != null)
            mobileCanvas.SetActive(visible);

        if (visible)
            EnsureSprintRefillRunning();
    }

    public void SetMatchStatusVisible(bool visible)
    {
        if (matchStatusBackground == null)
            return;

        bool isBecomingVisible = visible && !matchStatusBackground.activeSelf;
        matchStatusBackground.SetActive(visible);
        if (isBecomingVisible)
            lastDisplayedMainTimeSeconds = int.MinValue;
    }

    public void UpdateRoundProgress(int roundNumber, int totalRounds)
    {
        if (roundProgressText == null)
            return;

        int total = Mathf.Max(0, totalRounds);
        int current = total > 0 ? Mathf.Clamp(roundNumber, 0, total) : 0;
        roundProgressText.text = $"{current}/{total}";
    }

    public void ShowCombo()
    {
        KopkariManager manager = KopkariManager.Instance;
        if (manager == null || comboPrizeUI == null)
            return;

        comboPrizeUI.Show(manager.CurrentComboTime, manager.CurrentComboPrize);
    }

    public void HideCombo()
    {
        comboPrizeUI?.Hide();
    }

    public bool TryCompleteCombo()
    {
        return comboPrizeUI != null && comboPrizeUI.TryComplete();
    }

    public void ShowCarrierGrip(float currentGrip, float maximumGrip)
    {
        if (keepDropFeedbackVisible)
            StopCarrierGripFeedback();
        UpdateCarrierGrip(currentGrip, maximumGrip);
        if (carrierInfoBackground != null)
            carrierInfoBackground.SetActive(true);
    }

    public void UpdateCarrierGrip(float currentGrip, float maximumGrip)
    {
        float maximum = Mathf.Max(1f, maximumGrip);
        float normalizedGrip = Mathf.Clamp01(currentGrip / maximum);

        if (carrierGripAmountText != null)
            carrierGripAmountText.text = $"{Mathf.CeilToInt(normalizedGrip * 100f)}%";

        if (carrierGripSlider != null)
        {
            carrierGripSlider.minValue = 0f;
            carrierGripSlider.maxValue = 1f;
            carrierGripSlider.value = normalizedGrip;
        }
    }

    public void PlayLocalCarrierGripLossFeedback(bool dropped)
    {
        if (carrierInfoBackground != null)
        {
            Transform target = carrierInfoBackground.transform;
            StopCarrierGripFeedback();
            target.localScale = carrierInfoBaseScale;
            float strength = dropped ? gripLossPunchScale * 1.35f : gripLossPunchScale;
            float duration = dropped ? gripLossPunchDuration * 1.2f : gripLossPunchDuration;
            carrierGripFeedbackTween = target
                .DOPunchScale(Vector3.one * strength, duration, dropped ? 8 : 5, 0.65f)
                .SetEase(Ease.OutQuad)
                .SetLink(carrierInfoBackground, LinkBehaviour.KillOnDisable);
            keepDropFeedbackVisible = dropped;
        }

        HomeHapticsManager.Instance?.Play(
            dropped ? HomeHapticId.CarrierGripDrop : HomeHapticId.CarrierGripLoss);
    }

    private void StopCarrierGripFeedback()
    {
        if (carrierGripFeedbackTween != null && carrierGripFeedbackTween.IsActive())
            carrierGripFeedbackTween.Kill(false);
        carrierGripFeedbackTween = null;
        keepDropFeedbackVisible = false;

        if (carrierInfoBackground != null)
            carrierInfoBackground.transform.localScale = carrierInfoBaseScale;
    }

    public void HideCarrierGrip()
    {
        if (keepDropFeedbackVisible && carrierGripFeedbackTween != null &&
            carrierGripFeedbackTween.IsActive() && carrierInfoBackground != null)
        {
            carrierGripFeedbackTween.OnComplete(() =>
            {
                carrierGripFeedbackTween = null;
                keepDropFeedbackVisible = false;
                carrierInfoBackground.transform.localScale = carrierInfoBaseScale;
                carrierInfoBackground.SetActive(false);
            });
            return;
        }

        StopCarrierGripFeedback();
        if (carrierInfoBackground != null)
            carrierInfoBackground.SetActive(false);
    }

    private void HandleCarrierOwnerChanged(GameObject ownerRoot)
    {
        RefreshFakeUlakButtonState();

        if (ownerRoot == null)
        {
            HideCarrierGrip();
            return;
        }

        KopkariManager manager = KopkariManager.Instance;
        if (manager != null && manager.IsLocalRiderTransform(ownerRoot.transform))
        {
            string playerName = PlayerPrefs.GetString(Constants.Player.UsernameKey, "You");
            ShowCarrierName(string.IsNullOrWhiteSpace(playerName) ? "You" : playerName);
            return;
        }

        AIKopkariRider rider = ownerRoot.GetComponentInChildren<AIKopkariRider>(true);
        if (rider == null)
            rider = ownerRoot.GetComponentInParent<AIKopkariRider>();
        if (rider == null)
        {
            HideCarrierGrip();
            return;
        }

        ShowCarrierName(string.IsNullOrWhiteSpace(rider.RiderName) ? "Rider" : rider.RiderName);
        ShowCarrierGrip(rider.CurrentGrip, rider.MaximumGrip);
    }

    private void HandleAICarrierGripChanged(AIKopkariRider rider, float currentGrip, float maximumGrip)
    {
        KopkariManager manager = KopkariManager.Instance;
        if (rider == null || manager == null || manager.currentGoatOwner == null ||
            manager.currentGoatOwner != rider.transform.root.gameObject)
        {
            return;
        }

        ShowCarrierName(string.IsNullOrWhiteSpace(rider.RiderName) ? "Rider" : rider.RiderName);
        ShowCarrierGrip(currentGrip, maximumGrip);
    }

    private void ShowCarrierName(string carrierName)
    {
        if (carrierNameText != null)
            carrierNameText.text = carrierName;
        if (carrierInfoBackground != null)
            carrierInfoBackground.SetActive(true);
    }
    #endregion

    #region Chain
    public void WebSnareBtnEvent() => OnWebSnareBtnEnable?.Invoke();

    public void OnWebSnoreButtonDown(BaseEventData data)
    {
        bool success = TrySpendItem(Constants.PlayerItems.WebSnare, 1);

        if (!success)
        {
            UpdateWebCount(GetItemAmount(Constants.PlayerItems.WebSnare));
            HomeHapticsManager.Instance?.Play(HomeHapticId.LowCondition);
            return;
        }

        int countSnare = GetItemAmount(Constants.PlayerItems.WebSnare);

        UpdateWebCount(countSnare);
        webSnareShotActive = true;
        OnWebSnareStart?.Invoke();
        StartCoroutine(OnShootCooling(countSnare));
    }

    private IEnumerator OnShootCooling(int snareCount)
    {
        chainContainerBtn.interactable = false;
        yield return new WaitForSeconds(1f);

        if (snareCount <= 0) OnClickChain();
        else chainContainerBtn.interactable = true;
    }

    public void OnWebSnoreButtonUp(BaseEventData data)
    {
        FinishActiveWebSnare();
    }

    private void FinishActiveWebSnare()
    {
        if (!webSnareShotActive)
            return;

        webSnareShotActive = false;
        OnWebSnareFinish?.Invoke();
    }

    private bool TrySpendItem(string itemKey, int amount)
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.SpendItem(itemKey, amount, false);

        int current = PlayerPrefs.GetInt(itemKey, 0);
        if (amount <= 0 || current < amount)
            return false;

        PlayerPrefs.SetInt(itemKey, current - amount);
        PlayerPrefs.Save();
        return true;
    }

    public void OnClickChain()
    {
        bool newState = !chainContainerBtn.gameObject.activeSelf;
        if (!chainContainerBtn.interactable) chainContainerBtn.interactable = true;

        WeaponInHand = newState;
        chainContainerBtn.gameObject.SetActive(newState);
        OnWebSnareBtnEnable?.Invoke();
    }
    public void DisableWebSnare()
    {
        if (WeaponInHand)
        {
            OnClickChain();
        }
    }
    #endregion

    #region Bottom UI
    public void PlayerDataRegister()
    {
        KopkariResultsManager resultsManager = KopkariResultsManager.Instance;
        if (resultsManager == null)
            return;

        string namePlayer = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        int playerid = PlayerPrefs.GetInt(Constants.Player.Userid, 0);
        string teamName = PlayerPrefs.GetString(Constants.Player.TeamName);
        resultsManager.Register(playerid, namePlayer, teamName, true);

    }
    public void MoveUP()
    {
        MoveBottomUI(28, 1f);
        RefreshFakeUlakButtonState();
        PlayerDataRegister();
        int roundNumber = KopkariManager.Instance != null
            ? KopkariManager.Instance.CurrentRoundNumber
            : 1;
        KopkariResultsManager.Instance?.StartRound(roundNumber);
    }
    public void MoveDown()
    {
        MoveBottomUI(-50,1f);
    }
    public void MoveBottomUI(float targetY, float duration)
    {
        if (bottomUI == null)
            return;

        if (moveBottomRoutine != null)
        {
            StopCoroutine(moveBottomRoutine);
            moveBottomRoutine = null;
        }

        moveBottomRoutine = StartCoroutine(MoveBottomUICo(targetY, duration));
    }

    private IEnumerator MoveBottomUICo(float targetY, float duration)
    {
        Vector2 startPos = bottomUI.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.SmoothStep(0f, 1f, t / duration);
            bottomUI.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            yield return null;
        }

        bottomUI.anchoredPosition = endPos;
        moveBottomRoutine = null;
    }
    #endregion

    #region Push Effect
    private void PushEffectStart()
    {
        OnHorsePushEffect?.Invoke();
    }
    #endregion

    #region Other Button Actions
    private void PauseMenu()
    {
        ShowUI(pauseMenu);
    }

    private void ToggleCameraView()
    {
        KopkariManager.Instance?.ToggleCameraView();
    }
    #endregion

    #region Utils
    private void StopIfRunning(ref Coroutine c)
    {
        if (c != null)
        {
            StopCoroutine(c);
            c = null;
        }
    }

    private void ShowSprintEffectNoForce()
    {
        if (sprintImg != null) sprintImg.gameObject.SetActive(true);
        SetSprintState(false);
    }

    private void HideSprintEffectNoForce()
    {
        autoSprintBoostActive = false;

        if (sprintImg != null) sprintImg.gameObject.SetActive(false);
        SetSprintState(true);
    }

    private void StopManualSprintForAutoBoost()
    {
        autoSprintBoostActive = true;

        if (isPressing)
        {
            isPressing = false;
            OnSprintEnd?.Invoke();
        }

        StopIfRunning(ref drainRoutine);

        if (sprintSlider != null && sprintSlider.value < 1f && refillRoutine == null)
            refillRoutine = StartCoroutine(RefillDelayedCoroutine());

        if (sprintImg != null) sprintImg.gameObject.SetActive(true);

        SetSprintState(false);
    }

    public void ReleaseSprintForUIInterruption()
    {
        bool shouldNotifySprintEnd = isPressing || autoSprintBoostActive ||
                                     (sprintImg != null && sprintImg.gameObject.activeSelf);

        isPointerHeld = false;
        isPressing = false;
        autoSprintBoostActive = false;
        StopIfRunning(ref drainRoutine);

        if (sprintImg != null)
            sprintImg.gameObject.SetActive(false);

        if (shouldNotifySprintEnd)
            OnSprintEnd?.Invoke();

        FinishActiveWebSnare();
        SetSprintState(!isDamaged);
        EnsureSprintRefillRunning();
    }

    private void OnObstacleDamageHandler(bool isDamaged)
    {
        EnableSprint(isDamaged);
    }
    #endregion

    #region Game Over Panel
    public void GameOverShow()
    {
        HideCombo();
        HideCarrierGrip();
        SetMobileCanvasVisible(false);
        SetMatchStatusVisible(false);
    }
    #endregion

    #region Game Stats
    public float GetTotalHoldTime()
    {
        float autoBoostTime = KopkariManager.Instance != null ? KopkariManager.Instance.GetBoostTime() : 0f;
        Debug.Log("[AUTO BOOST]" + autoBoostTime);
        return totalHoldTime + autoBoostTime;
    }
    public float GetTotalWebSnareTime()
    {
        float webSnareTime = KopkariManager.Instance != null ? KopkariManager.Instance.GetWebSnareDamageTime() : 0f;
        return totalWebSnareTime + webSnareTime;
    }

    public void ResetMatchUsageTimes()
    {
        totalHoldTime = 0f;
        totalWebSnareTime = 0f;
    }
    #endregion
}
