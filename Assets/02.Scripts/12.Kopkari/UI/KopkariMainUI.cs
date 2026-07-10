using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Button hitBtn;
    [SerializeField] private Button shootWebBtn;
    [SerializeField] private Button chainContainerBtn;
    [SerializeField] private Button pushButton;
    [SerializeField] private Button pauseButton;
    #endregion
    #region Inspector - Texts
    [Header("Buttons Data Texts")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text hitCountText;
    [SerializeField] private TMP_Text webSnareCounter;
    #endregion

    #region Effects / Sprint/ Hit / Walk
    [SerializeField] private Image sprintImg;
    [SerializeField] private Image slowImg;
    [SerializeField] private Image shockImg;

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
    [Header("Hit Count Slider")]
    public Slider hitCountSlider;

    [Header("Pages")]
    [SerializeField] private KopkariResultUI resultPage;
    [SerializeField] private UIPauseGame pauseMenu;
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private GameOver gameOverPage;

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;

    public bool WeaponInHand;
    public Sprite obstacleHitSprite;
    #endregion

    [Header("Top Slider && BottomUI")]
    [SerializeField] private RectTransform bottomUI;
    [SerializeField] private Slider topUloqSlider;
    [SerializeField] private GameObject[] pointTexts; // 0..4
    [SerializeField] private GameObject[] pointFlags; // 0..4

    private int sliderCount = 0;

    #region Projectiles
    [Header("Projectiles")]
    [SerializeField] private TMP_Text uloqPushCounterText;

    #endregion

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

    #region Lamp Show Ui
    [SerializeField] private GoatDistanceUI goalDistanceUI;
    #endregion

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

    private Coroutine drainRoutine;
    private Coroutine refillRoutine;
    private float totalHoldTime = 0f;
    private float totalWebSnareTime = 0f;
    #endregion
    private Coroutine canvasRoutine;
    private Coroutine moveBottomRoutine;
    private bool loadingCompleted;

    public Sprite tiggerFlag;
    #region Awake/Start/OnEnable/OnDisable
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }
    private void OnEnable()
    {
        KopkariManager.OnSceneReady += CompleteLoadingPanel;
        if (KopkariManager.IsSceneReady)
            CompleteLoadingPanel();

        KopkariManager.OnGameStartFinishState += CanvasEnable;
        OnBindRequested += Bind;
        Booster.OnSprintFull += HandleSprintFull;

        BoostersContainer.OnSprintEffectStart += ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd += HideSprintEffectNoForce;
        BoostersContainer.OnAutoSprintBoostStart += StopManualSprintForAutoBoost;

        BoostersContainer.OnWalkZoneAdded += UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved += UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded += UpdateDefendText;
        BoostersContainer.OnDefendRemoved += UpdateDefendText;

        BoostersContainer.OnWebSnareAdded += UpdateWebCount;

       // RacingController.OnRacingFinished += ShowResultPage;
        //KopkariManager.OnGameStarted += GetData;

        BoostersContainer.OnDefendState += SetDefendState;
        BoostersContainer.OnWalkZoneDamaged += EnableSprint;
        BoostersContainer.OnWebSnareDamaged += EnableSprint;
        BoostersContainer.OnObstacleDamage += OnObstacleDamageHandler;
        HorseMine.OnReachedStartTarget += MoveUP;
        KopkariManager.OnGoatPicked += ShowMeters;
        TargetReachEvent.OnRoundEnded += DisableMeters;
        pushButton.onClick.AddListener(PushEffectStart);
        pauseButton.onClick.AddListener(PauseMenu);

        KopkariManager.OnTimeFinished += GameOverShow;
    }

    private void OnDisable()
    {
        KopkariManager.OnSceneReady -= CompleteLoadingPanel;

        KopkariManager.OnGameStartFinishState -= CanvasEnable;
        OnBindRequested -= Bind;

        Booster.OnSprintFull -= HandleSprintFull;

        BoostersContainer.OnSprintEffectStart -= ShowSprintEffectNoForce;
        BoostersContainer.OnSprintEffectEnd -= HideSprintEffectNoForce;
        BoostersContainer.OnAutoSprintBoostStart -= StopManualSprintForAutoBoost;

        BoostersContainer.OnWalkZoneAdded -= UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved -= UpdateWalkZoneText;

        BoostersContainer.OnDefendAdded -= UpdateDefendText;
        BoostersContainer.OnDefendRemoved -= UpdateDefendText;

        BoostersContainer.OnWebSnareAdded -= UpdateWebCount;

        //KopkariManager.OnRacingFinished -= ShowResultPage;
        //RacingController.OnRacingStarted -= GetData;
        KopkariManager.OnTimeFinished -= GameOverShow;
        BoostersContainer.OnDefendState -= SetDefendState;
        BoostersContainer.OnWalkZoneDamaged -= EnableSprint;
        BoostersContainer.OnWebSnareDamaged -= EnableSprint;
        BoostersContainer.OnObstacleDamage -= OnObstacleDamageHandler;
        HorseMine.OnReachedStartTarget -= MoveUP;
        TargetReachEvent.OnRoundEnded -= DisableMeters;
        KopkariManager.OnGoatPicked -= ShowMeters;
        pushButton.onClick.RemoveListener(PushEffectStart);
        pauseButton.onClick.RemoveListener(PauseMenu);
        isPointerHeld = false;
        autoSprintBoostActive = false;
    }
    #endregion

    #region Text Updates
    public void UpdateDefendText(int count)
    {
        defendCountText.text = count.ToString();
        SaveItem(Constants.PlayerItems.Defense, count);
        SetDefendState(count > 0);
    }

    public void UpdateWalkZoneText(int count)
    {
        walkZoneCountText.text = count.ToString();
        SaveItem(Constants.PlayerItems.SlowDown, count);
        SetWalkZoneState(count > 0);
    }

    public void UpdateHitText(int count)
    {
        hitCountText.text = count.ToString();
        SaveItem(Constants.PlayerItems.Whip, count);
        SetHitState(count > 0);
    }

    public void UpdateWebCount(int count)
    {
        webSnareCounter.text = count.ToString();
        SaveItem(Constants.PlayerItems.WebSnare, count);
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
            !autoSprintBoostActive &&
            !isDamaged &&
            !isPressing &&
            sliderFull &&
            CanSprintNow();

        if (sprintBtn != null)
            sprintBtn.interactable = interactable;

    }

    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;
    public void SetWebState(bool state) => shootWebBtn.interactable = state;
    #endregion

    #region Data (Inventory)
    public void GetData()
    {
        int defCount = GetItemAmount(Constants.PlayerItems.Defense);
        int slowCount = GetItemAmount(Constants.PlayerItems.SlowDown);
        int webCount = GetItemAmount(Constants.PlayerItems.WebSnare);
        int whipCount = GetItemAmount(Constants.PlayerItems.Whip);

        InitializeData(defCount, slowCount, whipCount, webCount);
    }

    public void InitializeData(int defendCount, int walkZoneCount, int hitCount, int webCount)
    {
        UpdateDefendText(defendCount);
        UpdateWalkZoneText(walkZoneCount);
        UpdateHitText(hitCount);
        UpdateWebCount(webCount);
    }

    private int GetItemAmount(string itemKey)
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.GetItemAmount(itemKey);

        return PlayerPrefs.GetInt(itemKey, 0);
    }

    private void SaveItem(string itemKey, int value)
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetItemAmountFromGame(itemKey, value, false);
            return;
        }

        PlayerPrefs.SetInt(itemKey, value);
        PlayerPrefs.Save();
    }
    #endregion

    #region Bind Player BoostersContainer
    public void Bind(BoostersContainer boosters)
    {
        if (walkZoneBtn)
        {
            walkZoneBtn.onClick.RemoveAllListeners();
            walkZoneBtn.onClick.AddListener(() =>
            {
                if (boosters != null && !boosters.isNpc)
                    boosters.DropWalkTrap();
            });
        }

        if (defendBtn)
        {
            defendBtn.onClick.RemoveAllListeners();
            defendBtn.onClick.AddListener(() =>
            {
                if (boosters != null && !boosters.isNpc)
                {
                    boosters.DefendPlayer();
                }
            });
        }
    }
    #endregion

    #region UI Effects (minimal)
    public void PlayShock()
    {
        if (!shockImg) return;
        if (hitCountSlider != null) hitCountSlider.value--;
    }

    public void SprintEffect(bool value)
    {
        if (sprintImg != null) sprintImg.gameObject.SetActive(value);
        if (value && slowImg != null) slowImg.gameObject.SetActive(false);
    }
    #endregion

    #region UI Pages (Show/Hide)
    public void ShowUI(MonoBehaviour ui) => ShowUI(ui.gameObject);
    public void HideUI(MonoBehaviour ui) => HideUI(ui.gameObject);

    public void ShowUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        page.SetActive(true);
        rt.localScale = Vector3.one * startScale;
        cg.alpha = 0f;

        LeanTween.alphaCanvas(cg, 1f, fadeTime);
        LeanTween.scale(rt, Vector3.one * punchScale, animTime)
            .setEase(easeIn)
            .setOnComplete(() =>
            {
                LeanTween.scale(rt, Vector3.one, animTime * 0.7f).setEase(easeOut);
            });
    }

    public void HideUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        LeanTween.alphaCanvas(cg, 0f, fadeTime);
        LeanTween.scale(rt, Vector3.one * startScale, animTime)
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
        if (isDamaged || isPressing || sprintSlider.value <= 0.0001f)
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

        // tugma holatini yangila
        SetSprintState(canSprint);
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
            mobileCanvas.SetActive(true);
            roomCanvas.SetActive(true);
            Debug.Log("Yoq");
        }
        else
        {
            // 2 sekunddan keyin o‘chadi
            canvasRoutine = StartCoroutine(DisableCanvasDelayed());
            ShowUI(resultPage);
        }
    }

    private IEnumerator DisableCanvasDelayed()
    {
        
        MoveDown();
        yield return new WaitForSeconds(2f);
        Debug.Log("Uchir");
        mobileCanvas.SetActive(false);
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

        OnEverythingReadyStart?.Invoke();
        GetData();
    }
    #endregion

    #region Chain
    public void WebSnareBtnEvent() => OnWebSnareBtnEnable?.Invoke();

    public void OnWebSnoreButtonDown(BaseEventData data)
    {
        bool success = false;

        if (DataManager.Instance != null)
            success = DataManager.Instance.SpendItem(Constants.PlayerItems.WebSnare, 1, false);

        int countSnare = GetItemAmount(Constants.PlayerItems.WebSnare);

        if (!success)
            countSnare = 0;

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

    public void OnWebSnoreButtonUp(BaseEventData data) => OnWebSnareFinish?.Invoke();

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

    #region Bottom Ui && Top Slider uloq
    public void PlayerDataRegister()
    {
        string namePlayer = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        int playerid = PlayerPrefs.GetInt(Constants.Player.Userid, 0);
        string teamName = PlayerPrefs.GetString(Constants.Player.TeamName);
        KopkariResultsManager.Instance.Register(playerid, namePlayer, teamName, true);

    }
    public void MoveUP()
    {
        MoveBottomUI(28, 1f);
        ShowMeters(false);
        KopkariResultsManager.Instance?.StartRace();
        PlayerDataRegister();
    }
    public void MoveDown()
    {
        MoveBottomUI(-50,1f);
    }
    public void MoveBottomUI(float targetY, float duration)
    {
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


    public void UpdateSlider()
    {
        sliderCount++;
        sliderCount = Mathf.Clamp(sliderCount, 0, 4);

        topUloqSlider.value = sliderCount;
        BoosterUIAnimator.RaiseBoosterPicked(
            Booster.BoosterType.TriggerPoint,
            tiggerFlag // icon sprite
        );
        UpdateFlag(sliderCount);
    }

    private void UpdateFlag(int pointNumber) // 1..4 keladi
    {
        int idx = pointNumber - 1; // 0..3 ga map

        for (int i = 0; i < pointTexts.Length; i++)
        {
            // ✅ Flaglar: ortda qolganlar ham ON bo‘lib qoladi (o‘chmaydi)
            if (pointFlags[i] != null && i <= idx)
                pointFlags[i].SetActive(true);

            // ✅ Textlar: faqat ortda qolganlar OFF
            if (pointTexts[i] != null && i < idx)
                pointTexts[i].SetActive(false);
        }

        if (pointNumber == 3)
        {
            Debug.Log("You are near to final!");
            //KopkariManager.Instance?.FinalPosState(true);
        }

           
    }

    #endregion

    #region Push Effect
    private void PushEffectStart()
    {
        OnHorsePushEffect?.Invoke();
    }
    #endregion

    #region Goat show meters/ Finish Events
    private void DisableMeters()
    {
        ShowMeters(true);

        //goalDistanceUI.ForceHide();
        //MoveDown();
    }
    private void ShowMeters(bool hasGoat)
    {
        if(KopkariManager.Instance.roomState==KopkariManager.RoomState.GameFinished)
        {
            //goalDistanceUI.Hide();
            return;
        }
        else          
            goalDistanceUI.SHowHide(hasGoat);
    }
    #endregion

    #region Other Button Actions
    private void PauseMenu()
    {
        ShowUI(pauseMenu);
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
            isPressing = false;

        StopIfRunning(ref drainRoutine);

        if (sprintSlider != null && sprintSlider.value < 1f && refillRoutine == null)
            refillRoutine = StartCoroutine(RefillDelayedCoroutine());

        if (sprintImg != null) sprintImg.gameObject.SetActive(true);

        SetSprintState(false);
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

    }

    public void SliderValueRestore()
    {
        if (hitCountSlider == null) return;
        hitCountSlider.value = hitCountSlider.maxValue;
    }
    #endregion

    #region Game Over Panel
    public void GameOverShow()
    {
        if(mobileCanvas != null) mobileCanvas.SetActive(false); 
        ShowUI(gameOverPage);
    }
    #endregion

    #region Game Stats
    public float GetTotalHoldTime()
    {
        float autoBoostTime = KopkariManager.Instance.GetBoostTime();
        Debug.Log("[AUTO BOOST]" + autoBoostTime);
        totalHoldTime += autoBoostTime;
        return totalHoldTime;
    }
    public float GetTotalWebSnareTime()
    {
        float get = KopkariManager.Instance.GetWebSnareDamageTime();
        totalWebSnareTime += get;
        return totalWebSnareTime;
    }
    #endregion
}
