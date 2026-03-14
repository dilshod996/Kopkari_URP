using DG.Tweening;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class HomeMainUI : MonoBehaviour
{
    public enum HomeEnvironment
    {
        ZarafshanEnvironment,
        EgyptEnvironment,
        TexasEnvironment,
        ChinaEnvironment
    }
    public static HomeMainUI Instance { get; private set; }

    public HomeEnvironment environment;
    [Header("MainUI Parent Object")]
    [SerializeField] private GameObject mainUIPanel;

    [Header("Touch bo‘lmasa necha sekunddan keyin yashirish")]
    [SerializeField] private float idleTime = 5f;

    [Header("Input Action")]
    public InputAction touchAction;

    private float lastInputTime;
    private bool hidden = false;

    #region UI Anim data
    [Header("Auto Play")]
    [SerializeField] private bool playOnStart = true;

    [Header("Movement Common Settings")]
    [SerializeField] private float moveTime = 0.35f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeOutCubic;

    [Header("Scale Animation")]
    [SerializeField] private float punchScaleM = 1.2f;
    [SerializeField] private float scaleTime = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private float animTime = 0.35f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float overshootScale = 1.05f;
    [SerializeField] private float slideDistance = 300f;

    private Tween _currentTween;
    [Header("Neon Border")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Color flashColor = new Color(0f, 1f, 1f, 1f); // cyan
    [SerializeField] private float flashDuration = 0.15f;

    private Color _originalBorderColor;

    [SerializeField] private float durationHorseResouceBg = 5f;     // qancha davom etadi
    [SerializeField] private float scaleMin = 1f;     // boshlanish scale
    [SerializeField] private float scaleMax = 1.05f;  // maksimal scale

    #endregion

    [Header("UI Buttons")]
    [SerializeField] private Button playBtn;
    [SerializeField] private Button collectionBtn;
    [SerializeField] private Button competationsBtn;
    [SerializeField] private Button marketBtn;
    [SerializeField] private Button nyufiyButton;
    [SerializeField] private Button korakButton;
    [SerializeField] private Button envChangeBtn;

    [Header("UI Pages")]

    [SerializeField] private GameplayMode playMode;
    [SerializeField] private GameObject racingMaps;
    [SerializeField] private GameObject leaguePanel;
    [SerializeField] private GameObject collectionsPage;
    [SerializeField] private GameObject marketPage;
    [SerializeField] private GameObject racingFields;
    [SerializeField] private GameObject kopkariFields;
    [SerializeField] private DailyRewardUI dailyRewardUI; 
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private GameObject suppliesPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject userDetailsPanel;
    [SerializeField] private GameObject competationsPanel;
    [SerializeField] private GameObject horseResourcesObject;
    [SerializeField] private GameObject coinsPage;
    [SerializeField] private EnvironmentChangeUI environmentChangePanel;
    [SerializeField] private EnvironmentLoadingUI environmentLoadingUI;
    [SerializeField] private ConditionCheck conditionCheckObj;
    [SerializeField] private GameObject tutorialPanels;
    [SerializeField] private EsportUITutorial tutorial;

    #region Horse and Player Data

    [Header("PlayerData")]
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text defenseText, slowDownText, webText, whipText;

    [SerializeField] private TMP_Text defenseAmountText, webAmountText, slowDownAmountText, whipAmountText;

    [Header("HorseData")]
    [SerializeField] private TMP_Text horseName;
    [SerializeField] private ProgressBar horsePower;
    [SerializeField] private ProgressBar horseStamina;
    [SerializeField] private ProgressBar horseCooling;
    [SerializeField] private TMP_Text powerText, staminaText, coolingText;
    private float foolPercentage=100f;

    #endregion

    [SerializeField] private TMP_Text customText, tournoment, playText, collections, storeText, lobbyName;

    [Header("Coins")]
    [SerializeField] private TMP_Text nyufiyText, coinText;

    [Header("Sale")]
    [SerializeField] private Button saleBtn;
    [SerializeField] private TMP_Text saleText;


    #region Popup UI
    //[Header("PopupUI")]
    //[SerializeField] private RectTransform popupRect;
    //[SerializeField] private TMP_Text popupText;
    //private Vector2 hiddenPos = new Vector2(0, 120f);   
    //private Vector2 shownPos = new Vector2(0, -120f);  
    //private float animTimePopup = 0.4f;     
    //[SerializeField] private float appearTime = 4f;
    //private Coroutine activeRoutinePopup;
    #endregion

    #region Update Horse resources timing

    // Full bo‘lish vaqti minutda
    private const float PowerRegenMinutes = 240f;   // 4 hours
    private const float CoolingRegenMinutes = 300f; // 5 hours
    private const float StaminaRegenMinutes = 180f; // 3 hours

    #endregion

    #region Right Popup
    [Header("UI")]
    [SerializeField] private RightPopup rightPopup;

    #endregion
    #region Map Sprites
    [SerializeField] private Sprite zarafshanSprite;
    #endregion

    public event Action<bool> OnCoinsButtonPressed;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        //if (SceneLoadManager.Instance.PreviousSceneType == SceneLoadManager.SceneType.Intro)
        //{
        //    fadeImage.gameObject.SetActive(true);
        //}
        //SetEnvironmentLoading();
    }


    private void OnEnable()
    {
        ApplyOfflineRegen();
        AvailableMap();
        CheckHomeTutorial();
        saleBtn.onClick.AddListener(OnClickNextDayDebug);
        lastInputTime = Time.realtimeSinceStartup;

        //if (touchAction != null)
        //{
        //    touchAction.Enable();
        //    touchAction.performed += OnTouch;
        //}
        RiderStatistcs();
        HorseStatistcs();
        if(LanguageManager.Instance != null) UITransilations();
        //PlayerResourse.OnNyufiyUpdated += UpdateNyufiy;
        FoodInfo.OnNyufiyUpdate += UpdateOnlyNyufiy;
        PlayerResourse.OnResourseUpdated += UpdatePlayerResource;
        FoodInfo.OnFoodAddToHorse += ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged += FoodPanelState;
        LobbyManager.OnNameChanged += LobbyName;
        //StartingInfo();
        playBtn.onClick.AddListener(OpenGameMainPanel);
        marketBtn.onClick.AddListener(OpenMarketPage);
        nyufiyButton.onClick.AddListener(NyufiyClicked);
        korakButton.onClick.AddListener(QorakClicked);
        collectionBtn.onClick.AddListener(OpenCollectionPage);
        competationsBtn.onClick.AddListener(OpenCompetationsPanel);
        envChangeBtn.onClick.AddListener(OpenEnvironmentChangePanel);
        NameTutorial();
        CheckTutorialRewardOnReturn();
    }
    private void OnDisable()
    {
       // OnNewDayAvailable -= HandleNewDay;
        //if (touchAction != null)
        //{
        //    touchAction.performed -= OnTouch;
        //    touchAction.Disable();
        //}

        CancelInvoke(nameof(CheckIdle));
        PlayerResourse.OnResourseUpdated -= UpdatePlayerResource;
        FoodInfo.OnNyufiyUpdate -= UpdateOnlyNyufiy;
        FoodInfo.OnFoodAddToHorse -= ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged -= FoodPanelState;
        LobbyManager.OnNameChanged -= LobbyName;
        playBtn.onClick.RemoveAllListeners();
        nyufiyButton.onClick.RemoveAllListeners();
        korakButton.onClick.RemoveAllListeners();
        collectionBtn.onClick.RemoveAllListeners();
        competationsBtn.onClick.RemoveAllListeners();
        envChangeBtn.onClick.RemoveAllListeners();
        marketBtn.onClick.RemoveAllListeners();
    }
    #region Player Supplies

    private void UpdatePlayerResource(string itemkey)
    {
        switch (itemkey)
        { 
            case Constants.PlayerItems.Defense:
                defenseAmountText.text = $"X{GetInt(Constants.PlayerItems.Defense)}";
                break;
            case Constants.PlayerItems.SlowDown:
                slowDownAmountText.text = $"X{GetInt(Constants.PlayerItems.SlowDown)}";
                break;
            case Constants.PlayerItems.WebSnare:
                webAmountText.text = $"X{GetInt(Constants.PlayerItems.WebSnare)}";
                break;
            case Constants.PlayerItems.Whip:
                break;
            case Constants.PlayerItems.Horsedust:
                break;
            default: break;
        }
        UpdateNyufiy();
    }
    public void UpdatePlayerResources(string itemkey, int amount)
    {
        switch (itemkey)
        {
            case Constants.PlayerItems.Defense:
                defenseAmountText.text = $"X{amount}";
                break;
            case Constants.PlayerItems.SlowDown:
                slowDownAmountText.text = $"X{amount}";
                break;
            case Constants.PlayerItems.WebSnare:
                webAmountText.text = $"X{amount}";
                break;
            case Constants.PlayerItems.Whip:
                break;
            case Constants.PlayerItems.Horsedust:
                break;
            default: break;
        }
    }
    public void ShowSuppliesPanel()
    {
        ShowUI(suppliesPanel);
    }
    public void HideSuppliesPanel()
    {
        HideUI(suppliesPanel);
    }
    #endregion

    #region Prefs Data
    private void RiderStatistcs()
    {
        if (PlayerPrefs.HasKey(Constants.Player.UsernameKey))
        {
            playerName.text = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        }
        defenseAmountText.text = $"X{GetOrInitInt(Constants.PlayerItems.Defense, 3)}";
        slowDownAmountText.text = $"X{GetOrInitInt(Constants.PlayerItems.SlowDown, 3)}";
        webAmountText.text = $"X{GetOrInitInt(Constants.PlayerItems.WebSnare, 3)}";
        whipAmountText.text = $"X{GetOrInitInt(Constants.PlayerItems.Whip, 0)}";


        // Coins – default 0
        int nyufiyAmount = GetOrInitInt(Constants.Coins.Nyufiy, 0);
        //if (nyufiyAmount < 1000)
        //{
        //    nyufiyAmount = 4000;
        //    PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
        //}
        int coinAmount = GetOrInitInt(Constants.Coins.Coin, 0);

        // Formatlash xohishingga qarab
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        coinText.text = coinAmount > 0 ? $"{coinAmount:N0}" : "0";
    }
    public void UpdatePlayerName(string value)
    {
        playerName.text = value;
    }
    public void UpdateNyufiy()
    {
        int nyufiyAmount = GetOrInitInt(Constants.Coins.Nyufiy, 0);
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        int coinAmount = GetOrInitInt(Constants.Coins.Coin, 0);
        coinText.text = coinAmount > 0 ? $"{coinAmount:N0}" : "0";
    }
    private void UpdateOnlyNyufiy(int amount)
    {
        nyufiyText.text = amount > 0 ? $"{amount:N0}" : "0";
    }
    private void HorseStatistcs()
    {
        // Horse name (bor bo'lsa chiqaramiz)
        if (PlayerPrefs.HasKey(Constants.Horse.HorseNameKey))
        {
            horseName.text = PlayerPrefs.GetString(Constants.Horse.HorseNameKey);
        }

        // Team name (doim default bo'lsa ham bo'ladi)
        EnsureString(Constants.Player.TeamName, "Kaja Riders");

        // Horse stats – hammasi bitta pattern
        horsePower.currentPercent = GetOrInitFloat(Constants.HorseCondition.Power, foolPercentage);
        horseStamina.currentPercent = GetOrInitFloat(Constants.HorseCondition.Stamina, foolPercentage);
        horseCooling.currentPercent = GetOrInitFloat(Constants.HorseCondition.Cooling, foolPercentage);

        horsePower.UpdateUI();
        horseStamina.UpdateUI();
        horseCooling.UpdateUI();


    }

    // Local helper: float qiymatni o'qiydi, bo'lmasa default yozib qaytaradi
    float GetOrInitFloat(string key, float defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetFloat(key);

        PlayerPrefs.SetFloat(key, defaultValue);
        return defaultValue;
    }
    int GetOrInitInt(string key, int defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key);
        }
            

        PlayerPrefs.SetInt(key, defaultValue);
        return defaultValue;
    }
    int GetInt(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key);
        }

        return 0;
    }
    // Local helper: string qiymatni yo'qligida default qo'yib ketadi
    void EnsureString(string key, string defaultValue)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetString(key, defaultValue);
        }
    }
    #endregion

    #region HorseBoost Resources
    public void FoodPanelState(bool state)
    {
        if (state) SHowFoodPanel();
        else HideFoodPanel();
    }
    public void SHowFoodPanel()
    {
        ShowUI(foodPanel);
    }
    public void HideFoodPanel()
    {
        HideUI(foodPanel);
    }
    private void ApplyFoodBuffs(float powerPercent, float coolingPercent, float staminaPercent)
    {
        // 1) PlayerPrefs dagi qiymatlarni olamiz
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power, foolPercentage);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling, foolPercentage);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina, foolPercentage);

        // 2) Bufflarni qo‘shamiz
        currentPower = Mathf.Clamp(currentPower + powerPercent, 0f, 100f);
        currentCooling = Mathf.Clamp(currentCooling + coolingPercent, 0f, 100f);
        currentStamina = Mathf.Clamp(currentStamina + staminaPercent, 0f, 100f);

        // 3) Yangi qiymatlarni saqlaymiz
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, currentPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, currentCooling);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, currentStamina);

        // 4) UI barlarni yangilaymiz
        horsePower.currentPercent = currentPower;
        horseCooling.currentPercent = currentCooling;
        horseStamina.currentPercent = currentStamina;

        horsePower.UpdateUI();
        horseCooling.UpdateUI();
        horseStamina.UpdateUI();
        //MainUIState(false);
    }

    #endregion

    #region Transilations
    public void UITransilations()
    {
        webText.text = LanguageManager.Instance.GetText(322);
        defenseText.text = LanguageManager.Instance.GetText(324);
        slowDownText.text = LanguageManager.Instance.GetText(323);
        whipText.text = LanguageManager.Instance.GetText(325);

        powerText.text = $"{LanguageManager.Instance.GetText(326)}...";
        staminaText.text = $"{LanguageManager.Instance.GetText(328)}...";
        coolingText.text = $"{LanguageManager.Instance.GetText(327)}...";
        saleText.text = LanguageManager.Instance.GetText(329);
        customText.text = LanguageManager.Instance.GetText(21);
        tournoment.text = LanguageManager.Instance.GetText(24);
        playText.text = LanguageManager.Instance.GetText(23);
        collections.text = LanguageManager.Instance.GetText(320);
        storeText.text = LanguageManager.Instance.GetText(25);
    }
    #endregion

    #region Beginning FadeOut Image

    public void RemoveInitialImage()
    {
        StartCoroutine(FadeOut());
    }
    public IEnumerator FadeOut()  // 1 -> 0
    {
        yield return new WaitForSeconds(0.6f);
        environmentLoadingUI.gameObject.SetActive(false);
    }

    #endregion

    #region SHow and Hide UI Pages
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

    #region Daily Reward
    public void CheckHomeTutorial()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.HomeTutorial))
        {
            return;
        }
        TryOpenDailyRewardIfAvailable();
    }
    public void TryOpenDailyRewardIfAvailable()
    {
        if (dailyRewardUI == null) return;

        // UI inactive bo‘lsa ham, canClaimni hisoblab beradi:
        bool canClaim = dailyRewardUI.PeekCanClaimToday();

        dailyRewardUI.gameObject.SetActive(canClaim);
    }
    public void OnClickNextDayDebug()
    {
#if UNITY_EDITOR
        if (dailyRewardUI == null) return;

        dailyRewardUI.Debug_ForceNextDay();
        TryOpenDailyRewardIfAvailable();
#endif
    }


    #endregion

    #region Reward Popup
    public void DisplayRewardPopup(Sprite rewarIcon,string amountReward, string name)
    {
        rewardPopup.gameObject.SetActive(true);
        rewardPopup.PlaySuccess(rewarIcon, LanguageManager.Instance.GetText(408), amountReward, name);
        //ShowUI(rewardPopup);
    }
    public void DisplayAutoReward(Sprite rewardSprite, string title, string amount, string nameReward)
    {
        rewardPopup.gameObject.SetActive(true);
        rewardPopup.PlaySuccess(rewardSprite, title,amount, nameReward);
        UpdateNyufiy();
    }
    #endregion

    #region MainUIPanle Show & Hide
    private void OnTouch(InputAction.CallbackContext ctx)
    {
        lastInputTime = Time.realtimeSinceStartup;

        if (hidden)
            ShowUI();
    }

    private void CheckIdle()
    {
        if (!hidden && Time.realtimeSinceStartup - lastInputTime >= idleTime)
        {
            HideUI();
        }
    }

    private void HideUI()
    {
        mainUIPanel.SetActive(false);
        hidden = true;
    }

    private void ShowUI()
    {
        mainUIPanel.SetActive(true);
        hidden = false;
    }
    public void MainUIState(bool state)
    {
        mainUIPanel.SetActive(state);
    }
    #endregion

    #region Popup Code

    //public void OpenConditionCriticalPopup()
    //{
    //    ShowUI(conditionCheckObj);
    //}
    //private void StartingInfo()
    //{
    //    StartCoroutine(NotiPopup());
    //}
    //IEnumerator NotiPopup()
    //{
    //    yield return new WaitForSeconds(3f);
    //    AppearPopup(LanguageManager.Instance.GetText(41));
    //}
    //public void HorseResourceFinishPopup(string message)
    //{
    //    AppearPopup(message, HorseResourcesScaleAnim);
    //}
    //public void AppearPopup(string message, Action action=null)
    //{
    //    popupText.text = message;

    //    // Agar popup allaqachon animda bo‘lsa — qaytadan boshlaymiz
    //    if (activeRoutinePopup != null)
    //        StopCoroutine(activeRoutinePopup);

    //    activeRoutinePopup = StartCoroutine(PopupRoutine());
    //    action?.Invoke();
    //}

    //private IEnumerator PopupRoutine()
    //{
    //    // 1) Pastga tushish
    //    yield return MoveUI(popupRect, hiddenPos, shownPos, animTimePopup);

    //    // 2) 4 sekund tursin
    //    yield return new WaitForSeconds(appearTime);

    //    // 3) Yana yuqoriga chiqib ketish
    //    yield return MoveUI(popupRect, shownPos, hiddenPos, animTime);
    //}

    //private IEnumerator MoveUI(RectTransform rect, Vector2 start, Vector2 end, float duration)
    //{
    //    float t = 0f;

    //    while (t < duration)
    //    {
    //        t += Time.deltaTime;
    //        rect.anchoredPosition = Vector2.Lerp(start, end, t / duration);
    //        yield return null;
    //    }

    //    rect.anchoredPosition = end;
    //}
    //private void HorseResourcesScaleAnim()
    //{
    //    StartCoroutine(PulseRoutine());
    //    HideUI(racingFields);
    //    if(playMode.gameObject.activeSelf) playMode.gameObject.SetActive(false);
    //}
    //private IEnumerator PulseRoutine()
    //{
    //    float t = 0f;
    //    RectTransform rt = horseResourcesObject.GetComponent<RectTransform>();

    //    while (t < durationHorseResouceBg)
    //    {
    //        t += Time.deltaTime;

    //        // 0 → 1 → 0 yurak urishi effekti
    //        float pingPong = Mathf.PingPong(Time.time * 2, 1f);

    //        float scale = Mathf.Lerp(scaleMin, scaleMax, pingPong);

    //        rt.localScale = new Vector3(scale, scale, 1);

    //        yield return null;
    //    }

    //    rt.localScale = Vector3.one;
    //}

    #endregion

    #region Time Horse Resources Update
    private void ApplyOfflineRegen()
    {
        Debug.Log("Regen Started");

        if (!PlayerPrefs.HasKey(Constants.Timer.LastUpdateTime))
        {
            Debug.Log("Time key not exist");
            PlayerPrefs.SetString(Constants.Timer.LastUpdateTime, DateTimeOffset.UtcNow.ToString("O"));
            return;
        }

        string raw = PlayerPrefs.GetString(Constants.Timer.LastUpdateTime);
        Debug.Log("Raw stored time: " + raw);

        DateTimeOffset lastTime;
        if (!DateTimeOffset.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastTime))
        {
            Debug.Log("Parse failed, set lastTime = now");
            lastTime = DateTimeOffset.UtcNow;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        float elapsedMinutes = (float)(now - lastTime).TotalMinutes;
        Debug.Log($"LastTime: {lastTime:o}, Now: {now:o}, elapsedMinutes: {elapsedMinutes}");

        if (elapsedMinutes <= 0f)
        {
            Debug.Log("Elapsed minutes <= 0, regen SKIPPED");
            return;
        }

        // Hozirgi statlar
        float power = PlayerPrefs.GetFloat(Constants.HorseCondition.Power, foolPercentage);
        float cooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling, foolPercentage);
        float stamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina, foolPercentage);

        // Har bir minut bo‘yicha regen
        float powerPerMin = foolPercentage / PowerRegenMinutes;
        float coolingPerMin = foolPercentage / CoolingRegenMinutes;
        float staminaPerMin = foolPercentage / StaminaRegenMinutes;

        Debug.Log("Offline adding resources: " + $"{powerPerMin} {coolingPerMin} {staminaPerMin}");

        power += powerPerMin * elapsedMinutes;
        cooling += coolingPerMin * elapsedMinutes;
        stamina += staminaPerMin * elapsedMinutes;

        power = Mathf.Clamp(power, 0f, foolPercentage);
        cooling = Mathf.Clamp(cooling, 0f, foolPercentage);
        stamina = Mathf.Clamp(stamina, 0f, foolPercentage);

        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, power);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, cooling);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, stamina);

        PlayerPrefs.SetString(Constants.Timer.LastUpdateTime, DateTimeOffset.UtcNow.ToString("O"));
        PlayerPrefs.Save();

        Debug.Log($"Regen applied. New stats: P={power}, C={cooling}, S={stamina}");
    }

    #endregion

    #region Coins
    
    public void QorakClicked()
    {
        ShowUI(coinsPage);
        OnCoinsButtonPressed?.Invoke(true);
    }
    public void NyufiyClicked()
    {
        ShowUI(coinsPage);
        OnCoinsButtonPressed?.Invoke(false);
    }
    public void CloseCoinsPage()
    {
        //coinsPage.SetActive(false);
        HideUI(coinsPage);
    }
    #endregion

    #region Other Pages
    public void OpenMarketPage()
    {
        ShowUI(marketPage);
    }
    public void OpenCompetationsPanel()
    {
        ShowUI(competationsPanel);
    }
    public void OpenGameMainPanel()
    {
        ShowUI(playMode.gameObject, () =>
        {
            ShowGameModesTutorial();
        });
    }
    public void OpenRacingMaps()
    {
        ShowUI(racingMaps, () =>
        {
            ShowRacingRoomTutorial();
        });
    }
    public void OpenKopkariMaps()
    {
        ShowUI(leaguePanel);
    }
    public void CloseRacingField()
    {
        racingFields.SetActive(false);
    }
    public void CloseKopkariFeld()
    {
        kopkariFields.SetActive(false);
    }
    public void OpenSettingsPanel()
    {
        ShowUI(settingsPanel, () =>
        {
            ShowLanguageDropdown();
        });
    }
    public void CloseSettingsPanel()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.Settings))
        {
            tutorialPanels.SetActive(false);
            tutorial.Finish();
            PlayerPrefs.SetInt(Constants.Tutorial.Settings, 1);
        }
        HideUI(settingsPanel, () =>
        {
            ShowNameBtn();
        });
    }
    public void OpenUserDetailsPanel()
    {
        ShowUI(userDetailsPanel, () => {
            ShowNameField();
        });
        CloseTutorialPanel();
    }
    public void CloseUserDetailsPanel()
    {
        HideUI(userDetailsPanel, () =>
        {
            ShowPlayButton();
        });
    }
    public void OpenCollectionPage()
    {
        ShowUI(collectionsPage);
    }
    public void OpenEnvironmentChangePanel()
    {
        if (!environmentChangePanel.gameObject.activeSelf)
            environmentChangePanel.gameObject.SetActive(true);
        environmentChangePanel.Toggle();
    }
    #endregion

    #region Right Popup
    /// <summary>
    /// Popup ni ochadi va ichidagi textni o‘rnatadi
    /// </summary>
    public void ShowRightPopup(string message, Sprite iconSprite)
    {
       rightPopup.ShowRightPopup(message, iconSprite);
    }
    #endregion

    #region Open Map 
    private void AvailableMap()
    {
        int getInitialMap = PlayerPrefs.GetInt(Constants.MapNames.RacingTraining);
        if (getInitialMap > 0)
        {
            return;
        }
        PlayerPrefs.SetInt(Constants.MapNames.RacingTraining, 1);
    }
    private void LobbyName(string name)
    {
        switch (name)
        {
            case Constants.MapNames.Zarafshan:
                lobbyName.text = LanguageManager.Instance.GetText(27);
                break;
            case Constants.MapNames.Egypt:
                lobbyName.text= LanguageManager.Instance.GetText(410);
                break;
            default:
                lobbyName.text = "Unknown";
                break;
        }
        
    }
    public void SetEnvironmentLoading()
    {
        string getEnvrionmentName = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment);
        if (!environmentLoadingUI.gameObject.activeSelf)
        {
            environmentLoadingUI.gameObject.SetActive(true);
        }
        environmentLoadingUI.SetMapData(getEnvrionmentName);
        LobbyName(getEnvrionmentName);
    }
    #endregion

    #region Tutorials

    #region Name Tutorial
    public void NameTutorial()
    {

        if (!PlayerPrefs.HasKey(Constants.Tutorial.Settings))
        {
            ShowSettingsBtn();
        }
        else if (!PlayerPrefs.HasKey(Constants.Tutorial.Name))
        {
            ShowNameBtn();
        }
        else if (!PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
        {
            StartCoroutine(ShowPlayButtonDelay());
        }

    }
    private void CheckTutorialRewardOnReturn()
    {
        bool tutorialPlayed = PlayerPrefs.GetInt(Constants.Tutorial.TutorialPlay, 0) == 1;
        bool rewardGiven = PlayerPrefs.GetInt(Constants.Tutorial.TutorialReward, 0) == 1;

        if (!tutorialPlayed || rewardGiven)
            return;


        DisplayAutoReward(zarafshanSprite, LanguageManager.Instance.GetText(483), LanguageManager.Instance.GetText(484), LanguageManager.Instance.GetText(376));
        PlayerPrefs.SetInt(Constants.MapNames.Zarafshan, 1);
        PlayerPrefs.SetInt(Constants.Tutorial.TutorialReward, 1);
        PlayerPrefs.Save();
    }
    public void ShowNameBtn()
    {
        tutorial.ShowStep(0);
        tutorialPanels.SetActive(true);
    }
    IEnumerator ShowPlayButtonDelay()
    {
        yield return new WaitForSeconds(0.6f);
        ShowPlayButton();
    }
    public void ShowNameField()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.Name))
        {
            tutorial.ShowStep(1);
            tutorialPanels.SetActive(true);
        }
    }
    public void ShowSaveBtn()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.Name))
        {
            tutorial.ShowStep(2);
            tutorialPanels.SetActive(true);
        }
    }
    public void FinishNameTutorial()
    {
        if (!PlayerPrefs.HasKey(Constants.Tutorial.Name))
        {
            tutorialPanels.SetActive(false);
            tutorial.Finish();
            PlayerPrefs.SetInt(Constants.Tutorial.Name, 1);
        }
        CloseUserDetailsPanel();
    }
    public void CloseTutorialPanel()
    {
        if (tutorialPanels.activeSelf)
        {
            tutorialPanels.SetActive(false);
            //tutorial.Finish();
        }
    }
    #endregion

    #region Play Tutorial
    
    public void ShowPlayButton()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
        {
            return;
        }
        tutorialPanels.SetActive(true);
        tutorial.ShowStep(3);
    }
    public void ShowGameModesTutorial()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
        {
            return;
        }
        tutorialPanels.SetActive(true);
        tutorial.ShowStep(4);
    }
    public void ShowRacingRoomTutorial()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay))
        {
            return;
        }
        tutorial.ShowStep(5);
    }
    #endregion

    #region Settings
    public void ShowSettingsBtn()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.Settings))
        {
            return;
        }
        tutorialPanels.SetActive(true);
        tutorial.ShowStep(6);
    }
    public void ShowLanguageDropdown()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.Settings))
        {
            return;
        }
        tutorialPanels.SetActive(true);
        tutorial.ShowStep(7);
    }
    public void ShowSettingsSave()
    {
        if (PlayerPrefs.HasKey(Constants.Tutorial.Settings))
        {
            return;
        }
        tutorialPanels.SetActive(true);
        tutorial.ShowStep(8);
    }
    #endregion

    #endregion
}
