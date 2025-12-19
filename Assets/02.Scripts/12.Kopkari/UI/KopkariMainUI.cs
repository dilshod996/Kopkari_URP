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
 
    #region Effects / Sprint/ Hit / Walk
    [SerializeField] private Image sprintImg;
    [SerializeField] private Image slowImg;
    [SerializeField] private Image shockImg;

    #endregion

    [Header("Stats/Sprint/Hit")]
    [SerializeField] private Slider sprintSlider;
    [SerializeField] private Slider hitCountSlider;
    [Header("Sprint Timings")]
    public float drainDuration = 5f;    // Necha sekundda tugaydi
    public float refillDelay = 6f;      // Tugagandan keyin necha sekund kutadi
    public float refillDuration = 3f;   // Necha sekundda qayta to‘ladi
    private bool isPressing = false;
    private Coroutine drainRoutine;
    private Coroutine refillRoutine;

    private float totalHoldTime = 0f;
    [Header("Pages")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private KopkariResultUI resultUI;
    [SerializeField] private UIPauseGame pauseMenu;
    [Header("Top Slider && BottomUI")]
    [SerializeField] private RectTransform bottomUI;
    [SerializeField] private Slider topUloqSlider;
    [SerializeField] private GameObject[] pointTexts; // 0..4
    [SerializeField] private GameObject[] pointFlags; // 0..4

    private int sliderCount = 0;

    #region Projectiles
    [Header("Projectiles")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text hitCountText;
    [SerializeField] private TMP_Text webSnareCounter;
    [SerializeField] private TMP_Text uloqPushCounterText;

    #endregion

    #region Show and Hide UI Animation Data

    [Header("Scale Animation")]
    [SerializeField] private float punchScaleM = 1.2f;
    [SerializeField] private float scaleTime = 0.2f;

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;
    [Header("Fade Settings For UI Pages")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeTime = 0.2f;
    #endregion

    #region Game Canvases
    [SerializeField] private GameObject mobileCanvas;
    [SerializeField] private GameObject roomCanvas;
    #endregion[Header("Game Canvases")]

    #region Lamp Show Ui
    [SerializeField] private GoatDistanceUI goalDistanceUI;
    #endregion

    [Header("Events")]
    public static Action OnSprintStart;     // Speed → 6
    public static Action OnSprintEnd;       // Speed → 5
    public static Action<float> OnSprintHold;
    public static Action OnWebSnareBtnEnable;
    public static Action OnWebSnareStart;
    public static Action OnWebSnareFinish;

    public static Action OnEverythingReadyStart;
    public static Action<BoostersContainer> OnBoostersContainerStart;

    public static Action OnHorsePushEffect;

    public bool WeaponInHand;
    private Coroutine canvasRoutine;
    private Coroutine moveBottomRoutine;

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
        LoadingPanel(3f);
        BaseManager.OnGameStartFinishState += CanvasEnable;
        OnBoostersContainerStart += Bind;
        Booster.OnSprintFull += HandleSprintFull;
        BoostersContainer.OnSprintEffectStart += ShowSprintEffect;
        BoostersContainer.OnSprintEffectEnd += HideSprintEffect;
        BoostersContainer.OnWalkZoneAdded += UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved += UpdateWalkZoneText;
        BoostersContainer.OnDefendAdded += UpdateDefendText;
        BoostersContainer.OnDefendRemoved += UpdateDefendText;
        BoostersContainer.OnWebSnareAdded += UpdateWebCount;

        BoostersContainer.OnDefendState += SetDefendState;
        HorseMine.OnReachedStartTarget += MoveUP;
        BaseManager.OnGoatPicked += ShowMeters;
        TargetReachEvent.OnRoundEnded += DisableMeters;
        pushButton.onClick.AddListener(PushEffectStart);
        pauseButton.onClick.AddListener(PauseMenu);
    }

    private void OnDisable()
    {
        BaseManager.OnGameStartFinishState -= CanvasEnable;
        OnBoostersContainerStart -= Bind;
        Booster.OnSprintFull -= HandleSprintFull;
        BoostersContainer.OnSprintEffectStart -= ShowSprintEffect;
        BoostersContainer.OnSprintEffectEnd -= HideSprintEffect;
        BoostersContainer.OnWalkZoneAdded -= UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved -= UpdateWalkZoneText;
        BoostersContainer.OnDefendAdded -= UpdateDefendText;
        BoostersContainer.OnDefendRemoved -= UpdateDefendText;
        BoostersContainer.OnDefendState -= SetDefendState;
        HorseMine.OnReachedStartTarget -= MoveUP;
        BoostersContainer.OnWebSnareAdded -= UpdateWebCount;
        TargetReachEvent.OnRoundEnded -= DisableMeters;
        BaseManager.OnGoatPicked -= ShowMeters;
        pushButton.onClick.RemoveListener(PushEffectStart);
        pauseButton.onClick.RemoveListener(PauseMenu);
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
        webSnareCounter.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.WebSnare, count);
        SetWebState(count > 0);
    }

    #endregion

    #region Button State Updates
    public void SetSprintState(bool state) => sprintBtn.interactable = state;
    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;

    public void SetWebState(bool state) => shootWebBtn.interactable = state;
    #endregion

    #region Player Data
    public void GetData()
    {
        int defentCount = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
        int slowDownCount = PlayerPrefs.GetInt(Constants.PlayerItems.SlowDown);
        int webCounter = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare);
        if (webCounter == 0)
        {
            webCounter = 4;

        }
        int whipCount = PlayerPrefs.GetInt(Constants.PlayerItems.Whip);
        InitializeData(defentCount, slowDownCount, whipCount, webCounter);
    }
    /// <summary>
    /// Dastlabki qiymatlar va button holatini sozlash.
    /// </summary>
    public void InitializeData(int defendCount, int walkZoneCount, int hitCount, int webCount)
    {
        UpdateDefendText(defendCount);
        UpdateWalkZoneText(walkZoneCount);
        UpdateHitText(hitCount);
        UpdateWebCount(webCount);
    }
    private void SaveToPrefs(string prefsName, int value)
    {
        PlayerPrefs.SetInt(prefsName, value);
        PlayerPrefs.Save();
    }
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
        // xohlasangiz boshqa tugmalarni ham shu yerda bog‘laysiz
        // defendBtn.onClick.AddListener(boosters.DefendPlayer);
        // ...
    }
    public void SliderValueRestore()
    {
        hitCountSlider.value = hitCountSlider.maxValue;
    }
    #endregion

    #region Show and Hide UI Pages
    public void ShowUI(MonoBehaviour ui) => ShowUI(ui.gameObject);
    public void HideUI(MonoBehaviour ui) => HideUI(ui.gameObject);

    public void ShowUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>(); // oldindan bor deb faraz qilamiz

        page.SetActive(true);
        rt.localScale = Vector3.one * startScale;
        cg.alpha = 0f;

        // fade
        LeanTween.alphaCanvas(cg, 1f, fadeTime);

        // scale
        LeanTween.scale(rt, Vector3.one * punchScale, animTime)
            .setEase(easeIn)
            .setOnComplete(() =>
            {
                LeanTween.scale(rt, Vector3.one, animTime * 0.7f)
                    .setEase(easeOut);
            });
    }

    public void HideUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        // fade
        LeanTween.alphaCanvas(cg, 0f, fadeTime);

        // scale + deactivate
        LeanTween.scale(rt, Vector3.one * startScale, animTime)
            .setEase(easeOut)
            .setOnComplete(() =>
            {
                page.SetActive(false);
            });
    }
    #endregion

    #region Sprint Data
    public void OnSprintButtonDown(BaseEventData data)
    {
        HandlePointerDown();
    }

    public void OnSprintButtonUp(BaseEventData data)
    {
        HandlePointerUp();
    }
    private void HandlePointerDown()
    {
        isPressing = true;
        OnSprintStart?.Invoke();   // 🔥 Sprint ON
        sprintImg?.gameObject.SetActive(true);
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);
        if (refillRoutine != null)
            StopCoroutine(refillRoutine);

        drainRoutine = StartCoroutine(DrainCoroutine());
    }

    private void HandlePointerUp()
    {
        isPressing = false;
        OnSprintEnd?.Invoke();     // 🔥 Sprint OFF
        sprintImg?.gameObject.SetActive(false);
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);

        refillRoutine = StartCoroutine(RefillDelayedCoroutine());
    }
    private IEnumerator DrainCoroutine()
    {
        float startValue = sprintSlider.value;
        float t = 0f;

        while (t < drainDuration && isPressing)
        {
            t += Time.deltaTime;
            sprintSlider.value = Mathf.Lerp(startValue, 0f, t / drainDuration);
            totalHoldTime = totalHoldTime + Time.deltaTime;
            OnSprintHold?.Invoke(totalHoldTime);
            yield return null;
        }

        sprintSlider.value = 0f;

        // Agar tugab qolsa -> kutish + refill
        if (isPressing)
        {
            isPressing = false;
            OnSprintEnd?.Invoke();     // 🔥 Sprint OFF
            SetSprintState(false);
            refillRoutine = StartCoroutine(RefillDelayedCoroutine());
        }
    }

    private IEnumerator RefillDelayedCoroutine()
    {
        // 6 sekund kutadi
        yield return new WaitForSeconds(refillDelay);
        SetSprintState(true);
        float startValue = sprintSlider.value;
        float t = 0f;

        while (t < refillDuration)
        {
            t += Time.deltaTime;
            sprintSlider.value = Mathf.Lerp(startValue, 1f, t / refillDuration);
            yield return null;
        }

        sprintSlider.value = 1f;
    }

    // 🔥 Booster eliksir eventi – birdan FULL qilish
    private void HandleSprintFull()
    {
        // Hozirgi barcha harakatlarni to‘xtatamiz
        if (drainRoutine != null) StopCoroutine(drainRoutine);
        if (refillRoutine != null) StopCoroutine(refillRoutine);

        // Cooldown kutmasdan zudlik bilan FULL
        sprintSlider.value = 1f;

        // Agar hozir ham player bosib turgan bo‘lsa, yana drainni qayta boshlab yuborsak ham bo‘ladi:
        if (isPressing)
        {
            drainRoutine = StartCoroutine(DrainCoroutine());
        }
    }
    private void ShowSprintEffect()
    {
        sprintImg.gameObject.SetActive(true);
        SetSprintState(false);
    }
    private void HideSprintEffect()
    {
        sprintImg?.gameObject.SetActive(false);
        SetSprintState(true);
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
            ShowUI(resultUI);
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
        StartCoroutine(LoadingPanelDisabler(time));
    }
    private IEnumerator LoadingPanelDisabler(float time)
    {
        if (loadingPanel != null && !loadingPanel.activeSelf)
        {
            loadingPanel.SetActive(true);
        }
        yield return new WaitForSeconds(time);
        loadingPanel.SetActive(false);
        OnEverythingReadyStart?.Invoke();
        GetData();
    }
    #endregion

    #region Chain Section
    /// <summary>
    /// Bular ikkalasi ham Btn ga ulangan
    /// </summary>
    public void WebSnareBtnEvent()
    {
        OnWebSnareBtnEnable?.Invoke();
    }
    public void OnWebSnoreButtonDown(BaseEventData data)
    {
        int countSnare = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare);
        if (countSnare > 0)
        {
            countSnare--;
        }
        Debug.Log("Web Snare COunt: " + countSnare);
        UpdateWebCount(countSnare);
        OnWebSnareStart?.Invoke();
        StartCoroutine(OnShootCooling(countSnare));
        //if (countSnare <= 0) { OnClickChain(); }
    }

    private IEnumerator OnShootCooling(int snareCount)
    {
        chainContainerBtn.interactable = false;
        yield return new WaitForSeconds(1f);
        if (snareCount <= 0) { OnClickChain(); }
        else chainContainerBtn.interactable = true;
    }
    public void OnWebSnoreButtonUp(BaseEventData data)
    {
        OnWebSnareFinish?.Invoke();
    }
    public void OnClickChain()
    {
        bool newState = !chainContainerBtn.gameObject.activeSelf;
        if (chainContainerBtn.interactable == false) { chainContainerBtn.interactable = true; }
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
            //BaseManager.Instance?.FinalPosState(true);
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
        if(BaseManager.Instance.roomState==BaseManager.RoomState.GameFinished)
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
}
