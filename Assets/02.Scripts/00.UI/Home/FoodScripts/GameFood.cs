using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFood : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text nyufiyText;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text backText;

    [Header("Bottom")]
    [SerializeField] private TMP_Text adsText;
    [SerializeField] private Button watchBtn;
    [SerializeField] private TMP_Text adsAmount;
    [SerializeField] private Button replayBtn;
    [SerializeField] private TMP_Text replayText;

    [Header("Main Details")]
    [SerializeField] private TMP_Text waterText;
    [SerializeField] private TMP_Text appleText;
    [SerializeField] private TMP_Text bugdoyText;
    [SerializeField] private TMP_Text arpaText;
    [SerializeField] private TMP_Text boosterText;
    [SerializeField] private Button waterBtn, appleBtn, bugdoyBtn, arpaBtn, boosterBtn;

    private int amountWatch = 0;
    private int coin = 0;
    private int nyufiy = 0;

    [Header("Sliders")]
    [SerializeField] private ProgressBar powerSlider, coolingSlider, staminaSlider;
    [SerializeField] private TMP_Text powerText, coolingText, staminaText;

    [SerializeField] private GameObject adsPanel;
    [SerializeField] private RectTransform nyufiyBgObj;
    [SerializeField] private RectTransform slidersBgObj;
    [SerializeField] private TMP_Text notEnoughResourceText;

    public SceneLoadManager.SceneType sceneType;
    private void OnEnable()
    {
        HookButtons(true);

        GetCoins();
        UITransilation();
        GetResources();
        replayBtn.onClick.AddListener(PlayMore);
    }

    private void OnDisable()
    {
        HookButtons(false);
        replayBtn.onClick.RemoveListener(PlayMore);
    }

    #region UI Transilations
    private void UITransilation()
    {
        var lang = LanguageManager.Instance;
        if (lang != null)
        {
            backText.text = lang.GetText(362);
            //adsText.text = lang.GetText(363);
            waterText.text = lang.GetText(111);
            appleText.text = lang.GetText(110);
            bugdoyText.text = lang.GetText(108);
            arpaText.text = lang.GetText(111);
            boosterText.text = lang.GetText(112);
            powerText.text = lang.GetText(326);
            coolingText.text = lang.GetText(327);
            staminaText.text = lang.GetText(328);
        }
    }
    #endregion

    #region Button Actions
    private void HookButtons(bool hook)
    {
        if (waterBtn == null || appleBtn == null || bugdoyBtn == null || arpaBtn == null || boosterBtn == null)
            return;

        if (hook)
        {
            waterBtn.onClick.AddListener(OnWater);
            appleBtn.onClick.AddListener(OnApple);
            bugdoyBtn.onClick.AddListener(OnBugdoy);
            arpaBtn.onClick.AddListener(OnArpa);
            boosterBtn.onClick.AddListener(OnBooster);
        }
        else
        {
            waterBtn.onClick.RemoveListener(OnWater);
            appleBtn.onClick.RemoveListener(OnApple);
            bugdoyBtn.onClick.RemoveListener(OnBugdoy);
            arpaBtn.onClick.RemoveListener(OnArpa);
            boosterBtn.onClick.RemoveListener(OnBooster);
        }
    }

    private void OnWater() => TryBuyAndApply(powerAdd: 0f, coolingAdd: 6f, staminaAdd: 3f, costNyufiy: 500);
    private void OnApple() => TryBuyAndApply(powerAdd: 4f, coolingAdd: 2f, staminaAdd: 4f, costNyufiy: 750);
    private void OnBugdoy() => TryBuyAndApply(powerAdd: 7f, coolingAdd: 2f, staminaAdd: 5f, costNyufiy: 900);
    private void OnArpa() => TryBuyAndApply(powerAdd: 9f, coolingAdd: 3f, staminaAdd: 6f, costNyufiy: 1400);
    private void OnBooster() => TryBuyAndApply(powerAdd: 0f, coolingAdd: 5f, staminaAdd: 15f, costNyufiy: 1780);

    private void TryBuyAndApply(float powerAdd, float coolingAdd, float staminaAdd, int costNyufiy)
    {
        // 1) Nyufiy yetarlimi?
        if (nyufiy < costNyufiy)
        {
            PlayScaleAnim(nyufiyBgObj);
            EnableAdsPanel(true);
            return;
        }

        // 2) Balansdan yechamiz
        nyufiy -= costNyufiy;
        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiy);

        // 3) Statlarni olib, qo¡®shib, clamp qilib saqlaymiz
        float p = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float c = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float s = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);

        p = Mathf.Clamp(p + powerAdd, 0f, 100f);
        c = Mathf.Clamp(c + coolingAdd, 0f, 100f);
        s = Mathf.Clamp(s + staminaAdd, 0f, 100f);

        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, p);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, c);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, s);

        PlayerPrefs.Save();

        // 4) UI yangilash
        UpdateTexts(nyufiy, coin);
        UpdateSliders(p, c, s);
    }
    #endregion

    #region Get Coin & Nyufiy Data
    private void GetCoins()
    {
        coin = PlayerPrefs.GetInt(Constants.Coins.Coin);
        nyufiy = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        UpdateTexts(nyufiy, coin);
    }

    private void UpdateTexts(int nyufiy, int coin)
    {
        nyufiyText.text = $"{nyufiy:N0}";
        coinText.text = $"{coin:N0}";
    }

    private void GetResources()
    {
        float horsePowerMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float horseCoolingMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float horseStaminaMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);

        UpdateSliders(horsePowerMain, horseCoolingMain, horseStaminaMain);
    }

    private void UpdateSliders(float horsePower, float horseCooling, float horseStamina)
    {
        powerSlider.currentPercent = horsePower;
        powerSlider.UpdateUI();

        coolingSlider.currentPercent = horseCooling;
        coolingSlider.UpdateUI();

        staminaSlider.currentPercent = horseStamina;
        staminaSlider.UpdateUI();
    }
    private void PlayScaleAnim(RectTransform transform)
    {
        if (transform == null) return;

        LeanTween.cancel(transform);

        transform.localScale = Vector3.one;

        LeanTween.scale(transform, Vector3.one * 0.9f, 1.3f)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong(1);
    }
    private void EnableAdsPanel(bool state)
    {
        if (state)
        {
            notEnoughResourceText.gameObject.SetActive(false);
            adsPanel.SetActive(true);
            adsText.text = LanguageManager.Instance?.GetText(363);
        }
        else 
            adsPanel.SetActive(false);

    }
    #endregion

    #region Replay Section
    public void PlayMore()
    {
        CheckResources();
        
    }
    private void CheckResources()
    {
        float horsePowerMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float horseCoolingMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float horseStaminaMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);

        if (horsePowerMain < 30f || horseCoolingMain < 30f || horseStaminaMain < 30f)
        {
            PlayScaleAnim(slidersBgObj);
            EnableAdsPanel(false);
            notEnoughResourceText.gameObject.SetActive(true);
            notEnoughResourceText.text = LanguageManager.Instance.GetText(364);
        }
        else
        {
            if (KopkariMainUI.Instance != null)
            {
                KopkariMainUI.Instance.HideUI(this);
            }
            else
            {
                UIButtonActions.Instance.HideUI(this);
            }
            Clear();
            SceneLoadManager.Instance.LoadScene(sceneType);
        }
    }
    public void Clear()
    {
        StopAllCoroutines();
    }
    #endregion
}
