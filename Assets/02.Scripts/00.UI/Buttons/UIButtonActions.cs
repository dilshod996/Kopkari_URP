using MalbersAnimations.Scriptables;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class UIButtonActions : MonoBehaviour
{
    public static UIButtonActions Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button hitBtn;
    [SerializeField] private Button shootWebBtn;
    [SerializeField] private Button chainContainerBtn;

    [Header("Buttons Data Texts")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text hitCountText;
    [SerializeField] private TMP_Text chainCounter;

    [Header("Shock Effect")]
    [SerializeField] private Image shockImg;
    [SerializeField] private float shockLife = 0.25f;

    [Header("Slow Effect")]
    [SerializeField] private Image slowImg;
    [SerializeField] private float slowLife = 10f;

    [Header("Sprint Effect")]
    [SerializeField] private Image sprintImg;
    [SerializeField] private Slider sprintSlider;

    [Header("Sprint Timings")]
    public float drainDuration = 5f;    // Necha sekundda tugaydi
    public float refillDelay = 6f;      // Tugagandan keyin necha sekund kutadi
    public float refillDuration = 3f;   // Necha sekundda qayta to‘ladi
    private bool isPressing = false;
    private Coroutine drainRoutine;
    private Coroutine refillRoutine;

    private float totalHoldTime = 0f;
    // umumiy parametrlari (o‘zgarmaydi)
    private const string shockFloat = "_ShockAmount";
    private const string slowFloat = "_SlowAmount";
    private const float fadeIn = 0.2f;
    private const float fadeOut = 0.3f;

    private int tweenUp = -1;
    private int tweenDown = -1;

    [Header("Hit Count Slider")]
    public Slider hitCountSlider;


    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject sliderObject;
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
    [Header("Pages")]
    [SerializeField] private GameObject resultPage;
    [SerializeField] private GameObject foodPanel;
    [Header("Events")]
    public static Action OnSprintStart;     // Speed → 6
    public static Action OnSprintEnd;       // Speed → 5
    public static Action<float> OnSprintHold;
    public static Action OnWebSnareBtnEnable;
    public static Action OnWebSnareStart;
    public static Action OnWebSnareFinish;

    public static Action<BoostersContainer> OnBindRequested;

    public bool WeaponInHand;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void OnEnable()
    {
        LoadingPanel(3f);
        Booster.OnSprintFull += HandleSprintFull;
        BoostersContainer.OnSprintEffectStart += ShowSprintEffect;
        BoostersContainer.OnSprintEffectEnd += HideSprintEffect;
        BoostersContainer.OnWalkZoneAdded += UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved += UpdateWalkZoneText;
        BoostersContainer.OnDefendAdded += UpdateDefendText;
        BoostersContainer.OnDefendRemoved += UpdateDefendText;

        //THis is only for Racing Mode
        RacingController.OnRacingFinished += ShowResultPage;
        RacingController.OnRacingStarted += GetData;
        BoostersContainer.OnDefendState += SetDefendState;
        FoodShowerPopup.OnFoodPopupVisibilityChanged += FoodPanleState;
        OnBindRequested += Bind;
    }
    private void OnDisable()
    {
        Booster.OnSprintFull -= HandleSprintFull;
        BoostersContainer.OnSprintEffectStart -= ShowSprintEffect;
        BoostersContainer.OnSprintEffectEnd -= HideSprintEffect;
        BoostersContainer.OnWalkZoneAdded -= UpdateWalkZoneText;
        BoostersContainer.OnWalkZoneRemoved -= UpdateWalkZoneText;
        BoostersContainer.OnDefendAdded -= UpdateDefendText;
        BoostersContainer.OnDefendRemoved -= UpdateDefendText;
        RacingController.OnRacingFinished -= ShowResultPage;
        RacingController.OnRacingStarted -= GetData;
        BoostersContainer.OnDefendState -= SetDefendState;
        FoodShowerPopup.OnFoodPopupVisibilityChanged -= FoodPanleState;
        OnBindRequested -= Bind;
    }
    #region Text Updates
    public void UpdateDefendText(int count)
    {
        defendCountText.text = count.ToString();
        SaveToPrefs(Constants.PlayerItems.Defense, count);
        SetDefendState(count>0);
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
    public void SetSprintState(bool state) => sprintBtn.interactable = state;
    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;

    public void SetWebState(bool state) => shootWebBtn.interactable = state;
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
    // ========================= MAIN LOGIC =============================
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

    #region Player Items bor yoki yo'qligini tekshirish

    public void GetData()
    {
        int defentCount = PlayerPrefs.GetInt(Constants.PlayerItems.Defense);
        int slowDownCount = PlayerPrefs.GetInt(Constants.PlayerItems.SlowDown);
        int webCounter = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare);
        if (webCounter == 0)
        {
            webCounter = 4;

        }
        if (slowDownCount < 1)
        {
            slowDownCount = 5;
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
    #endregion
    /// <summary>
    /// Playerga tegishli BoostersContainer ga UI ni bog‘laydi
    /// </summary>
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
        if (defendBtn) {
            defendBtn.onClick.RemoveAllListeners();
            defendBtn.onClick.AddListener(() =>
            {
                if(boosters != null && !boosters.isNpc)
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
    #region UI Effects
    private void InitEffect(Image img, string floatName)
    {
        if (!img) return;
        var mat = img.material;
        if (mat && mat.HasProperty(floatName))
            mat.SetFloat(floatName, 0f);

        img.gameObject.SetActive(false);
    }

    // 🔹 Shock Effect
    public void PlayShock()
    {
        if (!shockImg) return;
        hitCountSlider.value--;
        PlayShaderEffect(shockImg, shockFloat, shockLife);
    }

    // 🔹 Slow Effect
    public void PlaySlow()
    {
        if (!slowImg) return;
        PlayShaderEffect(slowImg, slowFloat, slowLife);
        StopSingleEffect(shockImg, shockFloat);
    }

    private void PlayShaderEffect(Image img, string floatName, float life)
    {
        if (!img) return;
        img.gameObject.SetActive(true);

        if (tweenUp != -1) LeanTween.cancel(tweenUp);
        if (tweenDown != -1) LeanTween.cancel(tweenDown);

        float current = GetFloatSafe(img, floatName);

        tweenUp = LeanTween.value(img.gameObject, current, 1f, fadeIn)
            .setEaseOutQuad()
            .setIgnoreTimeScale(true)
            .setOnUpdate(v => SetFloatSafe(img, floatName, v))
            .setOnComplete(() =>
            {
                LeanTween.delayedCall(img.gameObject, life, () =>
                {
                    tweenDown = LeanTween.value(img.gameObject, 1f, 0f, fadeOut)
                        .setEaseInCubic()
                        .setIgnoreTimeScale(true)
                        .setOnUpdate(v => SetFloatSafe(img, floatName, v))
                        .setOnComplete(() => img.gameObject.SetActive(false))
                        .id;
                }).setIgnoreTimeScale(true);
            }).id;
    }

    private float GetFloatSafe(Image img, string prop)
    {
        var mat = img.material;
        return (mat && mat.HasProperty(prop)) ? mat.GetFloat(prop) : 0f;
    }

    private void SetFloatSafe(Image img, string prop, float v)
    {
        var mat = img.material;
        if (mat && mat.HasProperty(prop))
        {
            mat.SetFloat(prop, v);
            img.SetMaterialDirty();
        }
    }
    private void StopSingleEffect(Image img, string floatProp)
    {
        if (!img) return;

        LeanTween.cancel(img.gameObject);
        SetFloatSafe(img, floatProp, 0f);
        img.gameObject.SetActive(false);
    }
    public void SprintEffect(bool value)
    {
        sprintImg.gameObject.SetActive(value);
        if (value)
        {
            StopSingleEffect(slowImg, slowFloat);
        }
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
        Debug.Log("Web Snare COunt: " +  countSnare);
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
    #endregion

    #region Pages
    public void FoodPanleState(bool state)
    {
        if(state) ShowFoodPanel();
        else HideFoodPanel();
    }
    public void ShowFoodPanel()
    {
        ShowUI(foodPanel);
    }
    public void HideFoodPanel()
    {
        HideUI(foodPanel);
    }
    public void ShowResultPage()
    {
        ShowUI(resultPage);
        if (sprintImg.gameObject.activeSelf)
        {
            sprintImg.gameObject.SetActive(false);
        }
        if(slowImg.gameObject.activeSelf)
        {
            slowImg.gameObject.SetActive(false);
        }
        if(WeaponInHand)
            OnClickChain();
    }
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
        if (sliderObject != null) sliderObject.SetActive(true);
    }
    #endregion
}
