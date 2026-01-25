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
    public static HomeMainUI Instance { get; private set; }
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

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;

    [Header("Fade Settings For UI Pages")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private Image fadeImage;      // rangini inspector’da berasan
    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] private float durationHorseResouceBg = 5f;     // qancha davom etadi
    [SerializeField] private float scaleMin = 1f;     // boshlanish scale
    [SerializeField] private float scaleMax = 1.05f;  // maksimal scale

    #endregion

    [Header("UI Buttons")]
    [SerializeField] private Button playBtn;
    [SerializeField] private Button collectionBtn;
    [SerializeField] private Button competationsBtn;
    [SerializeField] private Button nyufiyButton;
    [SerializeField] private Button korakButton;

    [Header("UI Pages")]

    [SerializeField] private GameplayMode playMode;
    [SerializeField] private GameObject collectionsPage;
    [SerializeField] private GameObject racingFields;
    [SerializeField] private GameObject kopkariFields;
    [SerializeField] private GameObject dailyUIRewards;
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private GameObject suppliesPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject userDetailsPanel;
    [SerializeField] private GameObject competationsPanel;
    [SerializeField] private GameObject horseResourcesObject;
    [SerializeField] private GameObject coinsPage;


    #region Reward System Parametrs

    [Header("Monthly Settings")]
    [SerializeField] private int monthCycleLength = 31;
    public int MonthCycleLength => monthCycleLength;


    private int lastDayInCycle = 0;   // oxirgi olingan daily kun (1..7) yoki 0
    private int streakDay = 1; // default
    private const int WEEK_CYCLE_LENGTH = 8; // 1..8 (8 - BIG)
    public int TodayDayIndex { get; private set; } = 1;
    public bool CanClaimToday { get; private set; } = false;

    public int CurrentMonthProgress { get; private set; } = 0;
    public int LastClaimedDay => lastDayInCycle;

    public event Action OnNewDayAvailable;
    public event Action<int> OnClaimCompleted;
    public event Action OnMonthlyRewardReady;

    private Sprite cachedRewardIcon;
    private string cachedRewardAmount;
    private string cacheRewardName;
    private string cacheRewardTitle;

    #endregion

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
    [Header("PopupUI")]
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private TMP_Text popupText;
    private Vector2 hiddenPos = new Vector2(0, 120f);   
    private Vector2 shownPos = new Vector2(0, -120f);  
    private float animTimePopup = 0.4f;     
    [SerializeField] private float appearTime = 4f;
    private Coroutine activeRoutinePopup;
    #endregion

    #region Update Horse resources timing

    // Full bo‘lish vaqti minutda
    private const float PowerRegenMinutes = 240f;   // 4 hours
    private const float CoolingRegenMinutes = 300f; // 5 hours
    private const float StaminaRegenMinutes = 180f; // 3 hours

    #endregion

    #region Right Popup
    [Header("UI")]
    [SerializeField] private GameObject rightPopup;
    [SerializeField] private RectTransform popupRT;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image icon;

    [Header("Positions")]
    [SerializeField] private float showX = 301f;
    [SerializeField] private float hideX = -315f;

    [Header("Timings")]
    [SerializeField] private float showDuration = 0.32f;
    [SerializeField] private float hideDuration = 0.25f;
    [SerializeField] private float stayTime = 2.2f;

    private Tween sequenceTween;
    #endregion

    public event Action<bool> OnCoinsButtonPressed;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        LoadState();
        if (SceneLoadManager.Instance.PreviousSceneType == SceneLoadManager.SceneType.Intro)
        {
            fadeImage.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        playBtn.onClick.AddListener(() =>
        {
            OpenGameMainPanel();
        });
        CheckNewDayAndNotify();
    }

    private void OnEnable()
    {
        ApplyOfflineRegen();
        AvailableMap();
        OnNewDayAvailable += HandleNewDay;
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
        FoodShowerPopup.OnBuyBtnPressed += UpdateNyufiy;
        PlayerResourse.OnResourseUpdated += UpdatePlayerResource;
        FoodShowerPopup.OnFoodGivenWithStats += ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged += FoodPanelState;
        StartingInfo();
        nyufiyButton.onClick.AddListener(NyufiyClicked);
        korakButton.onClick.AddListener(QorakClicked);
        collectionBtn.onClick.AddListener(OpenCollectionPage);
        competationsBtn.onClick.AddListener(OpenCompetationsPanel);
    }
    private void OnDisable()
    {
        OnNewDayAvailable -= HandleNewDay;
        //if (touchAction != null)
        //{
        //    touchAction.performed -= OnTouch;
        //    touchAction.Disable();
        //}

        CancelInvoke(nameof(CheckIdle));
        //PlayerResourse.OnNyufiyUpdated -= UpdateNyufiy;
        PlayerResourse.OnResourseUpdated -= UpdatePlayerResource;
        FoodShowerPopup.OnBuyBtnPressed -= UpdateNyufiy;
        FoodShowerPopup.OnFoodGivenWithStats -= ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged -= FoodPanelState;
        nyufiyButton.onClick.RemoveListener(NyufiyClicked);
        korakButton.onClick.RemoveListener(QorakClicked);
        collectionBtn.onClick.RemoveListener(OpenCollectionPage);
        competationsBtn.onClick.RemoveListener(OpenCompetationsPanel);
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
        if (nyufiyAmount < 1000)
        {
            nyufiyAmount = 4000;
            PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiyAmount);
        }
        int coinAmount = GetOrInitInt(Constants.Coins.Coin, 0);

        // Formatlash xohishingga qarab
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        coinText.text = coinAmount > 0 ? $"{coinAmount:N0}" : "0";
    }

    public void UpdateNyufiy()
    {
        int nyufiyAmount = GetOrInitInt(Constants.Coins.Nyufiy, 0);
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        int qorakAmount = GetOrInitInt(Constants.Coins.Coin, 0);
        coinText.text = qorakAmount > 0 ? $"{qorakAmount:N0}" : "0";
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
        PlayerPrefs.Save();

        // 4) UI barlarni yangilaymiz
        horsePower.currentPercent = currentPower;
        horseCooling.currentPercent = currentCooling;
        horseStamina.currentPercent = currentStamina;

        horsePower.UpdateUI();
        horseCooling.UpdateUI();
        horseStamina.UpdateUI();
        MainUIState(false);
    }

    #endregion

    #region Transilations
    public void UITransilations()
    {
        webText.text = LanguageManager.Instance.GetText(322);
        defenseText.text = LanguageManager.Instance.GetText(324);
        slowDownText.text = LanguageManager.Instance.GetText(323);
        whipText.text = LanguageManager.Instance.GetText(325);

        powerText.text = LanguageManager.Instance.GetText(326);
        staminaText.text = LanguageManager.Instance.GetText(328);
        coolingText.text = LanguageManager.Instance.GetText(327);
        saleText.text = LanguageManager.Instance.GetText(329);
        customText.text = LanguageManager.Instance.GetText(21);
        tournoment.text = LanguageManager.Instance.GetText(24);
        playText.text = LanguageManager.Instance.GetText(23);
        collections.text = LanguageManager.Instance.GetText(320);
        storeText.text = LanguageManager.Instance.GetText(25);
        lobbyName.text = LanguageManager.Instance.GetText(27);
    }
    #endregion

    #region Beginning FadeOut Image

    public void RemoveInitialImage()
    {
        if (SceneLoadManager.Instance.PreviousSceneType == SceneLoadManager.SceneType.Intro)
        {
            StartCoroutine(FadeOut());
        }
    }
    public IEnumerator FadeOut()  // 1 -> 0
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeDuration);

            c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;

            yield return null;
        }

        c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        // Oxirida umuman ko‘rinmasin desang:
        fadeImage.gameObject.SetActive(false);
    }

    #endregion

    #region SHow and Hide UI Pages
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

    #region Monthly & Daily Rewards
    private void HandleNewDay()
    {
        if (dailyUIRewards != null)
            ShowUI(dailyUIRewards);
    }
    private void LoadState()
    {
        streakDay = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_STREAK_DAY, 1);
        CurrentMonthProgress = PlayerPrefs.GetInt(Constants.DailyPrizes.PREF_MONTH_PROGRESS, 0); // xohlasang olib tashlaymiz
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_STREAK_DAY, streakDay);
        PlayerPrefs.SetInt(Constants.DailyPrizes.PREF_MONTH_PROGRESS, CurrentMonthProgress);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Boshqa scriptdan: GameManager.Start() dan chaqiriladi
    /// </summary>
    public void CheckNewDayAndNotify()
    {
        CanClaimToday = false;

        string lastClaimStr = PlayerPrefs.GetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE, string.Empty);
        DateTime today = DateTime.UtcNow.Date;

        if (string.IsNullOrEmpty(lastClaimStr))
        {
            streakDay = Mathf.Clamp(streakDay, 1, monthCycleLength);
            TodayDayIndex = ((streakDay - 1) % 8) + 1;
            CanClaimToday = true;

            OnNewDayAvailable?.Invoke();
            return;
        }

        if (!DateTime.TryParseExact(lastClaimStr, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime lastClaimDate))
        {
            // parse fail -> reset
            streakDay = 1;
            TodayDayIndex = 1;
            CanClaimToday = true;
            SaveState();
            OnNewDayAvailable?.Invoke();
            return;
        }

        int diffDays = (today - lastClaimDate.Date).Days;

        if (diffDays <= 0)
        {
            CanClaimToday = false;
            return;
        }

        if (diffDays == 1)
        {
            // continue streak (lekin 31 dan oshsa yana 1)
            if (streakDay >= monthCycleLength) streakDay = 1;

            TodayDayIndex = ((streakDay - 1) % 8) + 1;
            CanClaimToday = true;
            SaveState();
        }
        else
        {
            // missed day -> reset streak
            streakDay = 1;
            TodayDayIndex = 1;
            CanClaimToday = true;
            SaveState();
        }

        OnNewDayAvailable?.Invoke();
    }


    /// <summary>
    /// UI dagi Claim tugmasidan chaqiriladi
    /// </summary>
    public void ClaimToday()
    {
        if (!CanClaimToday) return;

        // 31-kun bo‘lsa daily emas, monthly beramiz (daily UI ko‘rsatmaslik ham shu yerda)
        if (streakDay >= monthCycleLength)
        {
            GiveMonthlyReward();
            PlayerPrefs.SetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE, DateTime.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

            // monthly claim bo‘lgach reset
            streakDay = 1;
            SaveState();

            CanClaimToday = false;
            OnMonthlyRewardReady?.Invoke();
            // xohlasang monthly popup ko‘rsat
            return;
        }

        // Daily reward absolute day bo‘yicha:
        GiveRewardForStreakDay(streakDay);

        PlayerPrefs.SetString(Constants.DailyPrizes.PREF_LAST_CLAIM_DATE, DateTime.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        // keyingi kunga tayyorlab qo‘yamiz
        streakDay++;
        if (streakDay > monthCycleLength) streakDay = 1;

        SaveState();

        CanClaimToday = false;
        OnClaimCompleted?.Invoke(TodayDayIndex); // UI uchun cycle index qaytarsang ham bo‘ladi
        DisplayRewardPopup();
    }

    #region Reward logika
    private void GiveRewardForStreakDay(int day)
    {
        // Safety
        if (day <= 0)
        {
            Debug.LogWarning($"[DailyReward] Invalid day: {day}");
            return;
        }

        // 1) BIG prize (8, 16, 24, 32...)
        if (day % 8 == 0)
        {
            Debug.Log($"🎁 [DailyReward] Day {day} => BIG PRIZE (rule: day%8==0)");
            GiveBigPrize(day);
            return;
        }

        // 2) Random prize (7, 14, 21, 28...)
        if (day % 7 == 0)
        {
            int seed = day * 9973; // deterministic seed (xohlasang olib tashla)
            int pick = Mathf.Abs(seed) % 4; // 0..3
            Debug.Log($"🎲 [DailyReward] Day {day} => RANDOM PRIZE (rule: day%7==0, pick={pick})");
            GiveRandomPrize(day, pick);
            return;
        }

        // 3) Player resources (4, 12, 20...)
        if (day % 4 == 0)
        {
            Debug.Log($"🧰 [DailyReward] Day {day} => PLAYER RESOURCES (rule: day%4==0)");
            GivePlayerResources(day);
            return;
        }

        // 4) Horse foods (3, 6, 9, 15...)
        if (day % 3 == 0)
        {
            Debug.Log($"🥕 [DailyReward] Day {day} => HORSE FOODS (rule: day%3==0)");
            GiveHorseFoods(day);
            return;
        }

        // 5) Odd days => Coins
        if ((day & 1) == 1)
        {
            int coins = 100 + (day * 15); // formula: xohlasang balans qilamiz
            Debug.Log($"🪙 [DailyReward] Day {day} => COINS {coins} (rule: odd day)");
            GiveCoins(coins);
            return;
        }

        // 6) Remaining days
        Debug.Log($"✅ [DailyReward] Day {day} => DEFAULT REWARD (rule: none matched)");
        GiveDefaultReward(day);
    }

    // --- Reward handlers (hozircha log, keyin real economy qo‘shasan) ---

    private void GiveBigPrize(int day)
    {
        // masalan: skin shard / big booster / taqa+nyufiy bundle
        Debug.Log($"🌟 [Reward] BIG prize granted for day {day}");
    }

    private void GiveRandomPrize(int day, int pick)
    {
        // pick 0..3: o‘zing xohlagan random turlari
        switch (pick)
        {
            case 0:
                Debug.Log($"🎲 [Reward] Random => Coins bundle");
                GiveCoins(250 + day * 10);
                break;
            case 1:
                Debug.Log($"🎲 [Reward] Random => Horse food pack");
                GiveHorseFoods(day); // yoki alohida pack
                break;
            case 2:
                Debug.Log($"🎲 [Reward] Random => Player resources pack");
                GivePlayerResources(day);
                break;
            default:
                Debug.Log($"🎲 [Reward] Random => Booster/Fragment");
                GiveDefaultReward(day);
                break;
        }
    }

    private void GivePlayerResources(int day)
    {
        // masalan: Taqa / Energy / Tickets / Gems
        int amount = 1 + (day / 4);
        Debug.Log($"🧰 [Reward] Player resources pack x{amount} (day={day})");
    }

    private void GiveHorseFoods(int day)
    {
        // masalan: apple/wheat/barley/water
        int amount = 2 + (day / 3);
        Debug.Log($"🥕 [Reward] Horse foods pack x{amount} (day={day})");
    }

    private void GiveCoins(int amount)
    {
        Debug.Log($"🪙 [Reward] Coins +{amount}");
    }

    private void GiveDefaultReward(int day)
    {
        // masalan: fragment, small booster, cosmetic token
        Debug.Log($"✅ [Reward] Default reward for day {day} (e.g., fragment/booster)");
    }


    private void GiveMonthlyReward()
    {
        Debug.Log("🌟 MONTHLY BIG REWARD");
        // Bu yerga monthly sovga logikasi
    }
    #endregion
    #endregion

    #region Reward Popup
    public void CacheReward(Sprite icon, string title, string amount, string nameReward)
    {
        cachedRewardIcon = icon;
        cachedRewardAmount = amount;
        cacheRewardName = nameReward;
        cacheRewardTitle = title;
    }
    public void DisplayRewardPopup()
    {
        rewardPopup.gameObject.SetActive(true);
        rewardPopup.PlaySuccess(cachedRewardIcon, cacheRewardTitle,cachedRewardAmount, cacheRewardName);
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

    private void StartingInfo()
    {
        StartCoroutine(NotiPopup());
    }
    IEnumerator NotiPopup()
    {
        yield return new WaitForSeconds(3f);
        AppearPopup(LanguageManager.Instance.GetText(41));
    }
    public void HorseResourceFinishPopup(string message)
    {
        AppearPopup(message, HorseResourcesScaleAnim);
    }
    public void AppearPopup(string message, Action action=null)
    {
        popupText.text = message;

        // Agar popup allaqachon animda bo‘lsa — qaytadan boshlaymiz
        if (activeRoutinePopup != null)
            StopCoroutine(activeRoutinePopup);

        activeRoutinePopup = StartCoroutine(PopupRoutine());
        action?.Invoke();
    }

    private IEnumerator PopupRoutine()
    {
        // 1) Pastga tushish
        yield return MoveUI(popupRect, hiddenPos, shownPos, animTimePopup);

        // 2) 4 sekund tursin
        yield return new WaitForSeconds(appearTime);

        // 3) Yana yuqoriga chiqib ketish
        yield return MoveUI(popupRect, shownPos, hiddenPos, animTime);
    }

    private IEnumerator MoveUI(RectTransform rect, Vector2 start, Vector2 end, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, end, t / duration);
            yield return null;
        }

        rect.anchoredPosition = end;
    }
    private void HorseResourcesScaleAnim()
    {
        StartCoroutine(PulseRoutine());
        HideUI(racingFields);
        if(playMode.gameObject.activeSelf) playMode.gameObject.SetActive(false);
    }
    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        RectTransform rt = horseResourcesObject.GetComponent<RectTransform>();

        while (t < durationHorseResouceBg)
        {
            t += Time.deltaTime;

            // 0 → 1 → 0 yurak urishi effekti
            float pingPong = Mathf.PingPong(Time.time * 2, 1f);

            float scale = Mathf.Lerp(scaleMin, scaleMax, pingPong);

            rt.localScale = new Vector3(scale, scale, 1);

            yield return null;
        }

        rt.localScale = Vector3.one;
    }

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
    private void NyufiyClicked()
    {
        ShowUI(coinsPage);
        OnCoinsButtonPressed?.Invoke(false);
    }
    public void CloseCoinsPage()
    {
        HideUI(coinsPage);
    }
    #endregion

    #region Other Pages
    public void OpenCompetationsPanel()
    {
        ShowUI(competationsPanel);
    }
    public void OpenGameMainPanel()
    {
        ShowUI(playMode);
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
        ShowUI(settingsPanel);
    }
    public void OpenUserDetailsPanel()
    {
        ShowUI(userDetailsPanel);
    }
    public void OpenCollectionPage()
    {
        ShowUI(collectionsPage);
    }
    #endregion

    #region Right Popup
    /// <summary>
    /// Popup ni ochadi va ichidagi textni o‘rnatadi
    /// </summary>
    public void ShowRightPopup(string message, Sprite iconSprite)
    {
        // Text set
        messageText.text = message;
        icon.sprite = iconSprite; 

        // Kill old tweens
        popupRT.DOKill();
        canvasGroup.DOKill();
        sequenceTween?.Kill();

        // Initial state (hidden)
        popupRT.anchoredPosition =
            new Vector2(hideX, popupRT.anchoredPosition.y);

        popupRT.localScale = Vector3.one * 0.96f;
        canvasGroup.alpha = 0f;

        rightPopup.gameObject.SetActive(true);

        // Sequence
        Sequence seq = DOTween.Sequence();

        // SHOW
        seq.Append(
            popupRT.DOAnchorPosX(showX, showDuration)
                   .SetEase(Ease.OutCubic)
        );

        seq.Join(
            popupRT.DOScale(1f, 0.25f)
                   .SetEase(Ease.OutBack)
        );

        seq.Join(
            canvasGroup.DOFade(1f, 0.15f)
        );

        // STAY
        seq.AppendInterval(stayTime);

        // HIDE
        seq.Append(
            popupRT.DOAnchorPosX(hideX, hideDuration)
                   .SetEase(Ease.InCubic)
        );

        seq.Join(
            canvasGroup.DOFade(0f, 0.15f)
        );

        seq.OnComplete(() =>
        {
            rightPopup.gameObject.SetActive(false);
        });

        sequenceTween = seq;
    }
    #endregion

    #region Open Map 
    private void AvailableMap()
    {
        int getInitialMap = PlayerPrefs.GetInt(Constants.MapNames.Zarafshan);
        if (getInitialMap > 0)
        {
            return;
        }
        PlayerPrefs.SetInt(Constants.MapNames.Zarafshan, 1);
    }
    #endregion
}
