using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonActions : MonoBehaviour
{
    #region Singleton
    public static UIButtonActions Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    #endregion

    #region Inspector - Buttons
    [Header("Buttons")]
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button hitBtn;
    [SerializeField] private Button shootWebBtn;
    [SerializeField] private Button chainContainerBtn;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Slider cameraSwitchSlider;
    #endregion

    #region Inspector - Texts
    [Header("Buttons Data Texts")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text hitCountText;
    [SerializeField] private TMP_Text chainCounter;
    #endregion

    #region Inspector - UI Effects

    [Header("Slow Effect")]
    [SerializeField] private Image slowImg;
    [SerializeField] private float slowLife = 10f;

    [Header("Sprint Effect")]
    [SerializeField] private Image sprintImg;
    [SerializeField] private Slider sprintSlider;
    #endregion

    #region Inspector - Sprint Timings
    [Header("Sprint Timings")]
    public float drainDuration = 5f;
    public float refillDelay = 6f;
    public float refillDuration = 3f;

    private float refillRate => 1f / Mathf.Max(0.0001f, refillDuration);
    private float drainRate => 1f / Mathf.Max(0.0001f, drainDuration);
    #endregion

    #region Inspector - Others
    [Header("Hit Count Slider")]
    public Slider hitCountSlider;
    [Header("Pages")]
    [SerializeField] private GameObject resultPage;
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private UIPauseGame pauseMenu;
    [SerializeField] private Image blinkOverlay;      // UI Image

    [Header("Animation Settings")]
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private float animTime = 0.35f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float overshootScale = 1.05f;
    [SerializeField] private float slideDistance = 300f;
    [Header("Neon Border")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Color flashColor = new Color(0f, 1f, 1f, 1f); // cyan
    [SerializeField] private float flashDuration = 0.15f;
    private Color _originalBorderColor;
    private Tween _currentTween;

    public bool WeaponInHand;
    public Sprite obstacleHitSprite;
    #endregion

    #region Events (DO NOT REMOVE)
    public static Action OnSprintStart;   // ✅ QAYTDI
    public static Action OnSprintEnd;       // Speed restore
    //public static Action<float> OnSprintHold;

    public static Action OnWebSnareBtnEnable;
    public static Action OnWebSnareStart;
    public static Action OnWebSnareFinish;

    public static Action<BoostersContainer> OnBindRequested;
    #endregion

    #region Runtime - Sprint State
    private bool isPressing;
    private bool isDamaged;
    private bool canSprint = true;

    private Coroutine drainRoutine;
    private Coroutine refillRoutine;
    private float totalHoldTime = 0f;
    private float totalWebSnareTime = 0f;
    #endregion

    #region Speed State UI
    [Header("Speed Icon & Text Details")]
    [SerializeField] private Image speedStateImage;
    [SerializeField] private Sprite runStateSprite;
    [SerializeField] private Sprite slowStateSprite;
    [SerializeField] private Sprite verySlowSprite;
    [SerializeField] private TMP_Text speedTitleText;

    public enum HorseSpeedState
    {
        Run,
        Slow,
        VerySlow
    }
    public HorseSpeedState speedState = HorseSpeedState.Run;
    #endregion

    [Header("Popup Data")]
    [SerializeField] UISpeechBuble speechBubble;
    [SerializeField] RightPopup rightPopup;
    [SerializeField] private ReverseWarningUI reverseWarningUI;
    [Header("Game Over")]
    [SerializeField] GameOver gameOverPanel;

    public bool isFinished = false;
    private bool _pausedByApp;
    #region Unity Events (OnEnable/Disable)
    private void OnEnable()
    {
        isFinished = false;
        Booster.OnSprintFull += HandleSprintFull;

        BoostersContainer.OnSprintEffectStart += ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd += HideSprintEffectNoForce;

        BoostersContainer.OnWalkZoneAdded += UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved += UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded += UpdateDefendText;
        BoostersContainer.OnDefendRemoved += UpdateDefendText;

        BoostersContainer.OnWebSnareAdded += UpdateWebCount;

        PlayerDataManager.OnShowFinalPage += ShowResultPage;
        RacingController.OnRacingStarted += GetData;

        BoostersContainer.OnDefendState += SetDefendStateTime;
        HorseMine.OnObstacleTouchedEvent += PlayShock;

        BoostersContainer.OnNormalState += NormalState;
        BoostersContainer.OnSlowState += SlowState;
        BoostersContainer.OnVerySlowState += VerySlowState;

        OnBindRequested += Bind;

        BoostersContainer.OnWalkZoneDamaged += EnableSprint;
        BoostersContainer.OnWebSnareDamaged += EnableSprint;
        BoostersContainer.OnObstacleDamage += OnObstacleDamageHandler;

        // ✅ UI start holatini to‘g‘ri qo‘yib olamiz
        SetSprintState(true);
        pauseButton.onClick.AddListener(PauseMenu);
        cameraSwitchSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        Booster.OnSprintFull -= HandleSprintFull;

        BoostersContainer.OnSprintEffectStart -= ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd -= HideSprintEffectNoForce;

        BoostersContainer.OnWalkZoneAdded -= UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved -= UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded -= UpdateDefendText;
        BoostersContainer.OnDefendRemoved -= UpdateDefendText;

        BoostersContainer.OnWebSnareAdded -= UpdateWebCount;

        PlayerDataManager.OnShowFinalPage -= ShowResultPage;
        RacingController.OnRacingStarted -= GetData;

        BoostersContainer.OnDefendState -= SetDefendStateTime;
        HorseMine.OnObstacleTouchedEvent -= PlayShock;

        BoostersContainer.OnNormalState -= NormalState;
        BoostersContainer.OnSlowState -= SlowState;
        BoostersContainer.OnVerySlowState -= VerySlowState;

        OnBindRequested -= Bind;

        BoostersContainer.OnWalkZoneDamaged -= EnableSprint;
        BoostersContainer.OnWebSnareDamaged -= EnableSprint;
        BoostersContainer.OnObstacleDamage -= OnObstacleDamageHandler;

        StopIfRunning(ref drainRoutine);
        StopIfRunning(ref refillRoutine);
        isPressing = false;
        totalHoldTime = 0f;
        pauseButton.onClick.RemoveListener(PauseMenu);
    }
    private void OnApplicationPause(bool pause)
    {
        if (isFinished) return;

        if (pause)
        {
            PauseBySystem();
        }
        // ❗ bu yerda Resume qilmang (user continue bosganda qilasiz)
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (isFinished) return;

        if (!hasFocus)
        {
            PauseBySystem();
        }
        // hasFocus == true bo‘lsa ham Resume qilmang
    }

    private void PauseBySystem()
    {
        if (_pausedByApp) return; // ✅ double-calldan saqlaydi
        _pausedByApp = true;
        var racingController = RacingController.Instance;
        if(racingController.mapType != RacingController.RacingType.Training)
        {
            pauseMenu.gameObject.SetActive(true);
        }
        // ✅ global timer pause
        racingController.PauseRaceTime();

        // ixtiyoriy: agar audio/vfx ham to‘xtasin desa
        // Time.timeScale = 0f;  // (agar sen game’ni to‘liq muzlatmoqchi bo‘lsang)
    }
    #endregion

    #region Text Updates
    public void UpdateDefendText(int count)
    {
        defendCountText.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.Defense, count);
        SetDefendState(count > 0);
    }

    public void UpdateWalkZoneText(int count)
    {
        walkZoneCountText.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.SlowDown, count);
        SetWalkZoneState(count > 0);
    }

    public void UpdateHitText(int count)
    {
        hitCountText.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.Whip, count);
        SetHitState(count > 0);
    }

    public void UpdateWebCount(int count)
    {
        chainCounter.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.WebSnare, count);
        SetWebState(count > 0);
    }
    #endregion

    #region Button State Updates
    public void SetSprintState(bool state)
    {
        // canSprint — umumiy ruxsat (page/scene)
        canSprint = state;
       
        // ✅ “toki slider to‘lmaguncha bosilmasin”
        bool sliderFull = (sprintSlider == null) || (sprintSlider.value >= 0.001f);

        // ✅ debuff bo‘lsa ham bosilmasin
        bool interactable =
            canSprint &&
            !isDamaged &&
            !isPressing &&
            sliderFull &&
            CanSprintNow();

        if (sprintBtn != null)
            sprintBtn.interactable = interactable;

    }

    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetDefendStateTime(bool state)
    {
        if (!state)
        {
            defendBtn.interactable = false;
        }
        else
        {
            int defendCount = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
            if (defendCount > 0)
            {
                defendBtn.interactable = true;
            }
            else
            {
                defendBtn.interactable= false;
            }
        }
    }
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;
    public void SetWebState(bool state) => shootWebBtn.interactable = state;
    #endregion

    #region UI Pages (Show/Hide)
    public void ShowUI(MonoBehaviour ui) => ShowUI(ui.gameObject);
    public void HideUI(MonoBehaviour ui) => HideUI(ui.gameObject);

    public void ShowUI(GameObject page, Action onComplete = null)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        if (rt == null || cg == null) return;

        page.SetActive(true);
        _currentTween?.Kill();

        cg.alpha = 0f;
        rt.localScale = Vector3.one * startScale;
        rt.anchoredPosition = new Vector2(0, -slideDistance);

        Sequence seq = DOTween.Sequence();

        seq.Join(cg.DOFade(1f, fadeTime));
        seq.Join(rt.DOAnchorPos(Vector2.zero, animTime).SetEase(Ease.OutExpo));
        seq.Join(rt.DOScale(overshootScale, animTime * 0.8f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, 0.15f).SetEase(Ease.OutQuad));

        if (borderImage != null)
        {
            borderImage.gameObject.SetActive(true);
            borderImage.color = _originalBorderColor;

            borderImage
                .DOColor(flashColor, flashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    borderImage.gameObject.SetActive(false);
                });
        }

        seq.OnComplete(() =>
        {
            Canvas.ForceUpdateCanvases();
            onComplete?.Invoke();
        });

        _currentTween = seq;
    }

    public void HideUI(GameObject page, Action onComplete = null)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        if (rt == null || cg == null) return;

        _currentTween?.Kill();

        Sequence seq = DOTween.Sequence();

        // Fade out
        seq.Join(cg.DOFade(0f, fadeTime));

        // Slide down fast
        seq.Join(rt.DOAnchorPos(new Vector2(0, -slideDistance), animTime)
            .SetEase(Ease.InExpo));

        // Slight shrink
        seq.Join(rt.DOScale(startScale, animTime)
            .SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            page.SetActive(false);
            onComplete?.Invoke();
        });

        _currentTween = seq;
    }
    #endregion

    #region Sprint
    public void OnSprintButtonDown(BaseEventData data) => HandlePointerDown();
    public void OnSprintButtonUp(BaseEventData data) => HandlePointerUp();

    private void HandlePointerDown()
    {
        // ✅ EventTrigger interactable=false bo‘lsa ham callback chaqirishi mumkin
        if (sprintBtn != null && !sprintBtn.interactable) return;
        Debug.Log("Pressed");
        if (isDamaged) return;
        if (isPressing) return;
        if (sprintSlider.value <= 0.0001f) return;

        isPressing = true;
        OnSprintStart?.Invoke();
        sprintImg?.gameObject.SetActive(true);

        StopIfRunning(ref refillRoutine);
        StopIfRunning(ref drainRoutine);

        drainRoutine = StartCoroutine(DrainCoroutine());

        // tugma holatini yangila
        SetSprintState(canSprint);
    }

    private void HandlePointerUp()
    {
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
            //Debug.Log("[HOLD] time" + totalHoldTime);
            //OnSprintHold?.Invoke(totalHoldTime);

            if (sprintSlider != null && sprintSlider.value <= 0.0001f)
            {
                sprintSlider.value = 0f;

                // ✅ tugadi: release + refill boshlansin (bosilganda qaytmasin)
                ForceReleaseSprint(startRefill: true);

                // refill davomida bosilmasin
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

        // holatni qayta hisobla
        SetSprintState(canSprint);
    }

    private IEnumerator RefillDelayedCoroutine()
    {
        yield return new WaitForSecondsRealtime(refillDelay);

        // refill jarayonida bosilmasin
        SetSprintState(false);

        while (!isPressing && sprintSlider != null && sprintSlider.value < 1f)
        {
            sprintSlider.value = Mathf.Min(1f, sprintSlider.value + refillRate * Time.unscaledDeltaTime);
            yield return null;
        }

        if (sprintSlider != null)
            sprintSlider.value = Mathf.Clamp01(sprintSlider.value);

        // ✅ to‘lgandan keyin (debuff yo‘q bo‘lsa) sprint qaytadi
        if (!isDamaged)
            SetSprintState(true);

        refillRoutine = null;
    }

    public void HandleSprintFull()
    {
        StopIfRunning(ref drainRoutine);
        StopIfRunning(ref refillRoutine);

        if (sprintSlider != null)
            sprintSlider.value = 1f;

        // full bo‘lganda tugma qaytsin (agar debuff bo‘lmasa)
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

        // ✅ debuff ON paytida sprint bosilmasin, OFF bo‘lsa slider holatiga qarab qaytsin
        SetSprintState(!debuffOn);
    }
    #endregion

    #region Data (PlayerPrefs)
    public void GetData()
    {
        int defCount = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
        int slowCount = PlayerPrefs.GetInt(Constants.PlayerItems.SlowDown);
        int webCount = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare);

        int whipCount = PlayerPrefs.GetInt(Constants.PlayerItems.Whip);

        InitializeData(defCount, slowCount, whipCount, webCount);
    }

    public void InitializeData(int defendCount, int walkZoneCount, int hitCount, int webCount)
    {
        UpdateDefendText(defendCount);
        UpdateWalkZoneText(walkZoneCount);
        UpdateHitText(hitCount);
        UpdateWebCount(webCount);
        NormalState();
    }

    private void SaveToPrefs(string prefsName, int value)
    {
        PlayerPrefs.SetInt(prefsName, value);
        PlayerPrefs.Save();
    }
    #endregion

    #region Bind Player BoostersContainer
    public void Bind(BoostersContainer boosters)
    {
        if (walkZoneBtn)
        {
           // walkZoneBtn.onClick.RemoveAllListeners();
            walkZoneBtn.onClick.AddListener(() =>
            {
                if (boosters != null && !boosters.isNpc)
                    boosters.DropWalkTrap();
            });
        }

        if (defendBtn)
        {
            //defendBtn.onClick.RemoveAllListeners();
            defendBtn.onClick.AddListener(() =>
            {
                if (boosters != null && !boosters.isNpc)
                {
                    boosters.DefendPlayer();
                    SetSpeedState(HorseSpeedState.Run);
                }
            });
        }
    }
    #endregion

    #region UI Effects (minimal)
    public void PlayShock()
    {
        if(!hitCountSlider.gameObject.activeSelf)
            return;
        
        hitCountSlider.value = Mathf.Max(0, hitCountSlider.value - 1);
        HomeHapticsManager.Instance?.Play(HomeHapticId.LowCondition);

        if (hitCountSlider.value == 0 && !isFinished)
        {
            RacingController.Instance.EndRacingCollision();
            ShowGameOver();
        }
    }

    public void SprintEffect(bool value)
    {
        if (sprintImg != null) sprintImg.gameObject.SetActive(value);
        if (value && slowImg != null) slowImg.gameObject.SetActive(false);
    }
    #endregion

    #region Chain
    public void WebSnareBtnEvent() => OnWebSnareBtnEnable?.Invoke();

    public void OnWebSnoreButtonDown(BaseEventData data)
    {
        if (RacingController.Instance?.mapType == RacingController.RacingType.Training)
        {
            StartCoroutine(OnShootCooling(6));
            OnWebSnareStart?.Invoke();
            return;
        }

        int countSnare = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare);
        if (countSnare > 0) countSnare--;

        UpdateWebCount(countSnare);
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
        //if (RacingController.Instance?.mapType == RacingController.RacingType.Training)
        //    return;
        OnWebSnareFinish?.Invoke(); 
    }

    public void OnClickChain()
    {
        bool newState = !chainContainerBtn.gameObject.activeSelf;
        if (!chainContainerBtn.interactable) chainContainerBtn.interactable = true;

        WeaponInHand = newState;
        chainContainerBtn.gameObject?.SetActive(newState);
        OnWebSnareBtnEnable?.Invoke();
    }
    #endregion

    #region Pages
    public void OpenFoodPanel()
    {
        ShowUI(foodPanel);
    }
    public void ShowResultPage()
    {
        ShowUI(resultPage);
    }
    public void ShowResultTutorial()
    {
        ShowUI(resultPage, RacingTutorials.OnShowResultPageTutorial);
    }
    public void DisableShootChainOrSprint()
    {
        if (sprintImg != null && sprintImg.gameObject.activeSelf) sprintImg.gameObject.SetActive(false);
        HideShootChain();
    }
    public void HideShootChain()
    {
        if (WeaponInHand) OnClickChain();
    }
    #endregion

    #region Speed State
    public void SetSpeedState(HorseSpeedState state)
    {
        if (speedState == state) return;

        switch (state)
        {
            case HorseSpeedState.Run:
                speedTitleText.text = (LanguageManager.Instance != null) ? LanguageManager.Instance.GetText(365) : "Stable";
                speedStateImage.sprite = runStateSprite;
                break;

            case HorseSpeedState.Slow:
                speedTitleText.text = (LanguageManager.Instance != null) ? LanguageManager.Instance.GetText(366) : "Slow";
                speedStateImage.sprite = slowStateSprite;
                break;

            case HorseSpeedState.VerySlow:
                speedTitleText.text = (LanguageManager.Instance != null) ? LanguageManager.Instance.GetText(367) : "Stuck";
                speedStateImage.sprite = verySlowSprite;
                break;
        }

        BoosterUIAnimator.RaiseBoosterPicked(Booster.BoosterType.SpeedState, speedStateImage.sprite);
        speedState = state;
    }

    public void NormalState() => SetSpeedState(HorseSpeedState.Run);
    public void SlowState() => SetSpeedState(HorseSpeedState.Slow);
    public void VerySlowState() => SetSpeedState(HorseSpeedState.VerySlow);
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
        if (sprintImg != null) sprintImg.gameObject.SetActive(false);
        SetSprintState(true);
    }
    private void OnObstacleDamageHandler(bool isDamaged)
    {
        // slow visual (xohlasang)
        if (isDamaged)
            PlaySlow();

        // sprintni bloklash / qaytarish
        EnableSprint(isDamaged);
    }

    public void PlaySlow()
    {
        SlowState();
    }

    public void SliderValueRestore()
    {
        if (hitCountSlider == null) return;
        hitCountSlider.value = hitCountSlider.maxValue;
    }
    #endregion

    #region Other Button Actions
    private void PauseMenu()
    {
        RacingController.Instance.PauseRaceTime();
        ShowUI(pauseMenu);
    }
    #endregion

    #region Blink Image Effect
    public IEnumerator FadeBlink(float from, float to, float duration)
    {
        if (blinkOverlay == null) yield break;

        float t = 0f;
        var c = blinkOverlay.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // UI uchun timeScale’dan mustaqil
            float k = Mathf.Clamp01(t / duration);
            c.a = Mathf.Lerp(from, to, k);
            blinkOverlay.color = c;
            yield return null;
        }

        c.a = to;
        blinkOverlay.color = c;
    }
    #endregion

    #region Popup Speech Bubble
    public void ShowAndHideSpeech(string speech)
    {
        StartCoroutine(SpeechCoroutine(speech));
    }
    private IEnumerator SpeechCoroutine(string text)
    {
        SpeechBubbleEnable(text);
        yield return new WaitForSeconds(3.5f);
        SpeechBubbleDisable();
    }
    public void SpeechBubbleEnable(string text)
    {
        // speechBubble.gameObject.SetActive(true);
        speechBubble.Show(text);
    }
    public void SpeechBubbleDisable()
    {
        speechBubble.Hide();
    }
    public void StartReverse()
    {
        reverseWarningUI.StartReverse();
    }
    // Mana shu yerda speech bubble berish kerak ortga ketayapsan deb reversewarninui event bilan
    public void ClearReverse()
    {
        reverseWarningUI.ClearReverse();
    }
    public void ShowSpecialTrigger(float timer)
    {
        reverseWarningUI.ShowPanel(timer);
    }
    public void HideSpecialTrigger()
    {
        reverseWarningUI.HidePanelNotTimeBased();
    }

    public void EliminitedRider(string name, Sprite sprite = null, bool disqualified=true)
    {
        rightPopup.EnqueueEliminatedRider(name, sprite, disqualified);
    }
    #endregion

    #region Game Over
    public void ShowGameOver()
    {
        EndRace();
        ShowUI(gameOverPanel);
    }
    #endregion

    #region EndRacing
    public float GetTotalHoldTime()
    {
        float autoBoostTime = RacingController.Instance.GetBoostTime();
        Debug.Log("[AUTO BOOST]" + autoBoostTime);
        totalHoldTime += autoBoostTime;
        return totalHoldTime;
    }
    public float GetTotalWebSnareTime()
    {
        float get = RacingController.Instance.GetPenaltyTime();
        totalWebSnareTime += get;
        return totalWebSnareTime;
    }
    public void EndRace()
    {
        Debug.Log("[GAME FINISHED]" + isFinished);
        isFinished = true;
    }
    #endregion

    #region Camera Switcher
    private void OnSliderChanged(float value)
    {
        if (isFinished)
            return;
        if (value >= 0.5f)
        {
            RacingController.Instance.FirstPersonEnable();
        }
        else
        {
            RacingController.Instance.FirstPersonDisable();
        }
    }
    #endregion
}
