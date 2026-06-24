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

    [Header("Sliders")]
    [SerializeField] private TMP_Text conditionTitleText;
    [SerializeField] private ProgressBar powerSlider, coolingSlider, staminaSlider;
    [SerializeField] private RectTransform notEnoughPowerBg, notEnoughCoolingBg, notEnoughStaminaBg;
    [SerializeField] private TMP_Text notEnoughPowerText, notEnoughCoolingText, notEnoughStaminaText;
    [SerializeField] private TMP_Text powerText, coolingText, staminaText;

    [SerializeField] private GameObject bottomAlarmObj;
    [SerializeField] private TMP_Text bottomAlarmText;

    private static readonly Color goodConditionColor = new Color32(47, 255, 135, 255);
    private static readonly Color stableConditionColor = new Color32(255, 199, 117, 255);
    private static readonly Color badConditionColor = new Color32(238, 32, 30, 255);
    private float mPower;
    private float mCooling;
    private float mStamina;
    private bool resourceUpdated=false;

    public int amountWatch = 300;
    private int coin = 0;
    private int nyufiy = 0;


    [SerializeField] private GameObject adsPanel;
    [SerializeField] private RectTransform nyufiyBgObj;


    public SceneLoadManager.SceneType sceneType;

    private void OnEnable()
    {
        GetCoins();
        UITransilation();
        GetResources();
        EnableAdsPanel(false);
        replayBtn.onClick.AddListener(PlayMore);
        backButton.onClick.AddListener(BackHome);
        CurrencyManager.Instance.OnNyufiyChanged += UpdateOnlyNyufiy;
        FoodInfo.OnFoodAddToHorse += ApplyFoodBuffs;
        FoodInfo.OnMoneyNotEnough += AdsPanel;
        watchBtn.onClick.AddListener(OnAdsButtonAction);
    }

    private void OnDisable()
    {
        replayBtn.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        watchBtn.onClick.RemoveAllListeners();
        CurrencyManager.Instance.OnNyufiyChanged -= UpdateOnlyNyufiy;
        FoodInfo.OnFoodAddToHorse -= ApplyFoodBuffs;
        FoodInfo.OnMoneyNotEnough -= AdsPanel;
    }
    private void OnDestroy()
    {
        SetData();
    }
    private void BackHome()
    {
        if (sceneType.Equals(SceneLoadManager.SceneType.None))
        {
            HomeMainUI.Instance.HideUI(this);
            return;
        }
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
    public void PlayAgainText()
    {
        switch (sceneType)
        {
            case SceneLoadManager.SceneType.SecondRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, "This time is your win!");
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, "Egypt People waiting you race");
                break;
            case SceneLoadManager.SceneType.Kansas:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Kansas, "Kansas is ready for");
                break;
        }
    }
    #region UI Transilations
    private void UITransilation()
    {
        var lang = LanguageManager.Instance;
        if (lang != null)
        {
            if (sceneType.Equals(SceneLoadManager.SceneType.None))
            {
                backText.text = lang.GetText(428);
            }
            else
            {
                backText.text = lang.GetText(302);
            }
            powerText.text = lang.GetText(326);
            coolingText.text = lang.GetText(327);
            staminaText.text = lang.GetText(328);

        }
    }
    #endregion

    #region Get Coin & Nyufiy Data
    private void GetCoins()
    {
        coin = CurrencyManager.Instance.Coin;
        nyufiy = CurrencyManager.Instance.Nyufiy;
        UpdateTexts(nyufiy, coin);
    }
    private void UpdateOnlyNyufiy(int amount)
    {
        nyufiyText.text = amount > 0 ? $"{amount:N0}" : "0";
    }
    private void UpdateTexts(int nyufiy, int coin)
    {
        nyufiyText.text = $"{nyufiy:N0}";
        coinText.text = $"{coin:N0}";
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
            if(bottomAlarmObj !=null &&  bottomAlarmObj.activeSelf)
            {
                bottomAlarmObj.SetActive(false);
            }
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
        if (mPower < Constants.HorseConditionNum.Power || mCooling < Constants.HorseConditionNum.Cool || mStamina < Constants.HorseConditionNum.Stamina)
        {
            PlayResourceAnim();
        }
        else
        {
            Clear();
            PlayAgainText();
            SetData();
            SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
        }
    }
    private void PlayResourceAnim()
    {
        if (mPower < Constants.HorseConditionNum.Power)
        {
            PlayScaleAnim(notEnoughPowerBg);
        }
        if (mCooling < Constants.HorseConditionNum.Cool)
        {
            PlayScaleAnim(notEnoughCoolingBg);
        }
        if (mStamina < Constants.HorseConditionNum.Stamina)
        {
            PlayScaleAnim(notEnoughStaminaBg);
        }
    }
    public void Clear()
    {
        StopAllCoroutines();
    }
    private void GetResources()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());

        mPower = current.Power;
        mCooling = current.Cooling;
        mStamina = current.Stamina;
        UpdateSliders(mPower, mCooling, mStamina);
    }
    private void UpdateSliders(float powerValue, float coolingValue, float staminValue)
    {
        powerSlider.currentPercent = powerValue;
        coolingSlider.currentPercent = coolingValue;
        staminaSlider.currentPercent = staminValue;
        powerSlider.UpdateUI();
        coolingSlider.UpdateUI();
        staminaSlider.UpdateUI();
        SetText(notEnoughPowerText, powerValue, Constants.HorseConditionNum.Power);
        SetText(notEnoughCoolingText, coolingValue, Constants.HorseConditionNum.Cool);
        SetText(notEnoughStaminaText, staminValue, Constants.HorseConditionNum.Stamina);

    }
    private void ApplyFoodBuffs(float powerPercent, float coolingPercent, float staminaPercent)
    {
        resourceUpdated = true;

        // 2) Bufflarni qo¡®shamiz
        HorseConditionStats current = new HorseConditionStats(mPower, mCooling, mStamina);
        HorseConditionStats updated = HorseConditionStatsService.AddFood(
            powerPercent,
            coolingPercent,
            staminaPercent,
            current);

        mPower = updated.Power;
        mCooling = updated.Cooling;
        mStamina = updated.Stamina;



        UpdateSliders(mPower, mCooling, mStamina);
        //MainUIState(false);
    }
    private void SetData()
    {
        if(!resourceUpdated)
            return;
        // 3) Yangi qiymatlarni saqlaymiz
        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(mPower, mCooling, mStamina));
    }
    #endregion

    #region Conditions
    private void SetText(TMP_Text detailText, float num, float limitNum)
    {
        if (num >= Constants.HorseConditionNum.GoodCondition)
        {
            GoodCondition(detailText);
        }
        else if(num<Constants.HorseConditionNum.GoodCondition && num>limitNum)
        {
            StableCondition(detailText);
        }
        else
        {
            BadCondition(detailText);
            if (sceneType.Equals(SceneLoadManager.SceneType.None))
            {
                BottomMessage();
            }
        }
    }
    private void GoodCondition(TMP_Text text)
    {
        if(text != null)
        {
            text.color = goodConditionColor;
            text.text = LanguageManager.Instance.GetText(211);
        }
    }
    private void StableCondition(TMP_Text text) { 
        if(text != null)
        {
            text.color = stableConditionColor;
            text.text = LanguageManager.Instance.GetText(212);
        }
    }
    private void BadCondition(TMP_Text text)
    {
        if(text != null)
        {
            text.color = badConditionColor;
            text.text = LanguageManager.Instance.GetText(213);
            
        }
    }
    private void AdsPanel()
    {
        //UIButtonActions.Instance?.ShowUI(checkCondition);
        EnableAdsPanel(true);
        //notEnoughResourceText.gameObject.SetActive(true);
        //notEnoughResourceText.text = LanguageManager.Instance.GetText(364);
    }
    private void BottomMessage()
    {
        if(bottomAlarmObj != null)
        {
            PlayResourceAnim();
            bottomAlarmObj.gameObject.SetActive(true);
            bottomAlarmText.text = LanguageManager.Instance.GetText(200);
        }
    }
    #endregion

    #region Ads Section
    public void OnAdsButtonAction()
    {
        GameAnalyticsEvents.RewardedAdClicked(
            placement: "coin_shop",
            rewardType: "nyufiy",
            rewardAmount: amountWatch
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(amountWatch, true);

            UpdateTexts(
                CurrencyManager.Instance.Nyufiy,
                CurrencyManager.Instance.Coin
            );

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: amountWatch
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: amountWatch
            );

            EnableAdsPanel(false);
        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }
    #endregion
}
