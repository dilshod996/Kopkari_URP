using System;
using System.Collections;
using System.Collections.Generic;
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


    [Header("Auto Play")]
    [SerializeField] private bool playOnStart = true;

    [Header("Left Panel Settings")]
    [SerializeField] private RectTransform leftRect;
    [Header("Right Panel Settings")]
    [SerializeField] private RectTransform rightRect;
    [SerializeField] private float startXRight = 200f;   // o‘ngdan kiradi
    [SerializeField] private float targetXRight = -143f; // final pozitsiya

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

    [Header("UI Buttons")]
    [SerializeField] private Button playBtn;
    [Header("UI Pages")]
    [SerializeField] private GameplayMode playMode;
    [SerializeField] private GameObject dailyUIRewards;
    [SerializeField] private RewardPopup rewardPopup;

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

    [SerializeField] private Image fadeImage;      // rangini inspector’da berasan
    [SerializeField] private float fadeDuration = 1f;

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
        if (playOnStart)
        {
            PlayLeft();
            PlayRight();
        }
        playBtn.onClick.AddListener(() =>
        {
            ShowUI(playMode);
        });
        CheckNewDayAndNotify();
    }

    private void OnEnable()
    {
        OnNewDayAvailable += HandleNewDay;
        lastInputTime = Time.realtimeSinceStartup;

        if (touchAction != null)
        {
            touchAction.Enable();
            touchAction.performed += OnTouch;
        }

        InvokeRepeating(nameof(CheckIdle), 1f, 1f);
       
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

    }

    #region Beginning Right & Left Animations & FadeOut Image

    public void PlayLeft()
    {
        if (leftRect == null) return;
        Play(leftRect, -startXRight, -targetXRight);
    }

    public void PlayRight()
    {
        if (rightRect == null) return;
        Play(rightRect, startXRight, targetXRight);
    }

    /// <summary>
    /// Universal slide + punch scale
    /// </summary>
    private void Play(RectTransform rect, float startX, float targetX)
    {
        // Start pozitsiyani beramiz
        var pos = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(startX, pos.y);
        rect.localScale = Vector3.one;

        // Slide anima
        LeanTween.moveX(rect, targetX, moveTime)
            .setEase(ease)
            .setOnComplete(() => PunchScale(rect));
    }

    private void PunchScale(RectTransform rect)
    {
        // Scale 1 → 1.2 → 1
        LeanTween.scale(rect, Vector3.one * punchScaleM, scaleTime)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                LeanTween.scale(rect, Vector3.one, scaleTime * 0.8f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });
    }
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
    #endregion

}
