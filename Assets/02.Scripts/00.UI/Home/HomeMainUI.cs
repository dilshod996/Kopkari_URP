using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("UI Pages")]

    [SerializeField] private GameplayMode playMode;
    [SerializeField] private GameObject racingFields;
    [SerializeField] private GameObject dailyUIRewards;
    [SerializeField] private RewardPopup rewardPopup;
    [SerializeField] private GameObject foodPanel;

    [SerializeField] private GameObject horseResourcesObject;


    #region Reward System Parametrs

    [Header("Monthly Settings")]
    [SerializeField] private int monthCycleLength = 30; // slider max (30 kunlik sikl)

    private const string PREF_LAST_CLAIM_DATE = "DR_LastClaimDate";
    private const string PREF_DAY_IN_CYCLE = "DR_DayInCycle";     // 1..7 (oxirgi olingan kun)
    private const string PREF_MONTH_PROGRESS = "DR_MonthProgress";  // 0..monthCycleLength

    private int lastDayInCycle = 0;   // oxirgi olingan daily kun (1..7) yoki 0

    public int TodayDayIndex { get; private set; } = 1;
    public bool CanClaimToday { get; private set; } = false;

    public int CurrentMonthProgress { get; private set; } = 0;
    public int MonthCycleLength => monthCycleLength;
    public int LastClaimedDay => lastDayInCycle;

    public event Action OnNewDayAvailable;
    public event Action<int> OnClaimCompleted;
    public event Action OnMonthlyRewardReady;

    private Sprite cachedRewardIcon;
    private string cachedRewardAmount;

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
            ShowUI(playMode);
        });
        CheckNewDayAndNotify();
    }

    private void OnEnable()
    {
        Debug.Log("HOME UI SCRIPT ENABLED !");
        ApplyOfflineRegen();
        OnNewDayAvailable += HandleNewDay;
        lastInputTime = Time.realtimeSinceStartup;

        if (touchAction != null)
        {
            touchAction.Enable();
            touchAction.performed += OnTouch;
        }

        //InvokeRepeating(nameof(CheckIdle), 1f, 1f);
        RiderStatistcs();
        HorseStatistcs();
        if(LanguageManager.Instance != null) UITransilations();
        FoodShowerPopup.OnBuyBtnPressed += UpdateNyufiy;
        FoodShowerPopup.OnFoodGivenWithStats += ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged += FoodPanelState;
        StartingInfo();

    }
    private void OnDisable()
    {
        OnNewDayAvailable -= HandleNewDay;
        if (touchAction != null)
        {
            touchAction.performed -= OnTouch;
            touchAction.Disable();
        }

        CancelInvoke(nameof(CheckIdle));
        FoodShowerPopup.OnBuyBtnPressed -= UpdateNyufiy;
        FoodShowerPopup.OnFoodGivenWithStats -= ApplyFoodBuffs;
        FoodShowerPopup.OnFoodPopupVisibilityChanged -= FoodPanelState;
    }

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
        int coinAmount = GetOrInitInt(Constants.Coins.Coin, 0);

        // Formatlash xohishingga qarab
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
        coinText.text = coinAmount > 0 ? $"{coinAmount:N0}" : "0";
    }

    private void UpdateNyufiy()
    {
        int nyufiyAmount = GetOrInitInt(Constants.Coins.Nyufiy, 0);
        nyufiyText.text = nyufiyAmount > 0 ? $"{nyufiyAmount:N0}" : "0";
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
        lastDayInCycle = PlayerPrefs.GetInt(PREF_DAY_IN_CYCLE, 0);
        CurrentMonthProgress = PlayerPrefs.GetInt(PREF_MONTH_PROGRESS, 0);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PREF_DAY_IN_CYCLE, lastDayInCycle);
        PlayerPrefs.SetInt(PREF_MONTH_PROGRESS, CurrentMonthProgress);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Boshqa scriptdan: GameManager.Start() dan chaqiriladi
    /// </summary>
    public void CheckNewDayAndNotify()
    {
        CanClaimToday = false;

        string lastClaimStr = PlayerPrefs.GetString(PREF_LAST_CLAIM_DATE, string.Empty);
        DateTime today = DateTime.UtcNow.Date;

        // Birinchi marta
        if (string.IsNullOrEmpty(lastClaimStr))
        {
            TodayDayIndex = 1;
            CanClaimToday = true;
            OnNewDayAvailable?.Invoke();
            return;
        }

        if (!DateTime.TryParse(lastClaimStr, out DateTime lastClaimDate))
        {
            TodayDayIndex = 1;
            CanClaimToday = true;
            OnNewDayAvailable?.Invoke();
            return;
        }

        int diffDays = (today - lastClaimDate.Date).Days;

        if (diffDays <= 0)
        {
            // Bugun allaqachon claim bo'lgan yoki vaqt o'zgartirilgan
            CanClaimToday = false;
            return;
        }
        else if (diffDays == 1)
        {
            // Agar o‘tgan safar 7-kun bo‘lgan bo‘lsa → yangi hafta boshlanadi
            if (lastDayInCycle >= 7)
            {
                lastDayInCycle = 0;   // hamma kunlar yana "olinmagan" bo‘ladi
            }

            int nextDay = lastDayInCycle + 1; // 0 -> 1, 1 -> 2, ...
            TodayDayIndex = nextDay;
            CanClaimToday = true;

            SaveState(); // ixtiyoriy, lekin yaxshisi shu yerda ham saqlab qo‘yamiz
        }

        else
        {
            // 1 kundan ko'p o'tib ketgan -> reset weekly
            TodayDayIndex = 1;
            lastDayInCycle = 0; // barcha kunlar unclaimed
            SaveState();
            CanClaimToday = true;
        }

        OnNewDayAvailable?.Invoke();
    }

    /// <summary>
    /// UI dagi Claim tugmasidan chaqiriladi
    /// </summary>
    public void ClaimToday()
    {
        if (!CanClaimToday)
        {
            Debug.Log("❌ Bugun claim qilib bo‘lmaydi");
            return;
        }

        // 1) Haftalik daily reward
        GiveRewardForDay(TodayDayIndex);

        // 2) Monthly progress
        CurrentMonthProgress++;
        if (CurrentMonthProgress >= monthCycleLength)
        {
            CurrentMonthProgress = 0;
            GiveMonthlyReward();
            OnMonthlyRewardReady?.Invoke();
        }

        // 3) State saqlash
        lastDayInCycle = TodayDayIndex;
        PlayerPrefs.SetString(PREF_LAST_CLAIM_DATE, DateTime.UtcNow.Date.ToString("yyyy-MM-dd"));
        SaveState();

        CanClaimToday = false;

        // 4) UI uchun event
        OnClaimCompleted?.Invoke(TodayDayIndex);
        DisplayRewardPopup();
    }

    #region Reward logika
    private void GiveRewardForDay(int day)
    {
        switch (day)
        {
            case 1:
                Debug.Log("✅ Day 1: 100 coins");
                break;
            case 2:
                Debug.Log("✅ Day 2: stamina booster");
                break;
            case 3:
                Debug.Log("✅ Day 3: 150 coins");
                break;
            case 4:
                Debug.Log("✅ Day 4: defend item");
                break;
            case 5:
                Debug.Log("✅ Day 5: 200 coins");
                break;
            case 6:
                Debug.Log("✅ Day 6: special fragment");
                break;
            case 7:
                Debug.Log("🎁 Day 7: BIG prize");
                break;
        }
    }

    private void GiveMonthlyReward()
    {
        Debug.Log("🌟 MONTHLY BIG REWARD");
        // Bu yerga monthly sovga logikasi
    }
    #endregion
    #endregion

    #region Reward Popup
    public void CacheTodayReward(Sprite icon, string amount)
    {
        cachedRewardIcon = icon;
        cachedRewardAmount = amount;
    }
    public void DisplayRewardPopup()
    {
        rewardPopup.SetData(cachedRewardIcon, cachedRewardAmount);
        ShowUI(rewardPopup);
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
}
