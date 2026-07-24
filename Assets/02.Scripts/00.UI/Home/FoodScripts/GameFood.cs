using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFood : MonoBehaviour
{
    public enum FoodPageMode
    {
        Normal,
        KopkariRoundChange
    }

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
    [SerializeField] private int replayTextLanguageId = -1;
    [SerializeField] private int continueTextLanguageId = -1;

    [Header("Sliders")]
    [SerializeField] private TMP_Text conditionTitleText;
    [SerializeField] private ProgressBar powerSlider, coolingSlider, staminaSlider;
    [SerializeField] private RectTransform notEnoughPowerBg, notEnoughCoolingBg, notEnoughStaminaBg;
    [SerializeField] private TMP_Text notEnoughPowerText, notEnoughCoolingText, notEnoughStaminaText;
    [SerializeField] private TMP_Text powerText, coolingText, staminaText;

    [Header("Entry Requirement")]
    [SerializeField] private int requiredPrefixLanguageId = -1;
    [SerializeField, Range(0f, 100f)] private float roundRequiredPercent = 15f;

    [Header("Bottom Field")]
    [SerializeField] private GameObject bottomAlarmObj;
    [SerializeField] private TMP_Text bottomAlarmText;
    [SerializeField] private TMP_Text feedHorseText, feedHorseDescription;
    
    private static readonly Color goodConditionColor = new Color32(47, 255, 135, 255);
    private static readonly Color stableConditionColor = new Color32(255, 199, 117, 255);
    private static readonly Color badConditionColor = new Color32(238, 32, 30, 255);
    private float mPower;
    private float mCooling;
    private float mStamina;
    private bool resourceUpdated=false;
    private FoodPageMode pageMode = FoodPageMode.Normal;

    public int amountWatch = 300;
    private int coin = 0;
    private int nyufiy = 0;
    private int currentAdReward;


    [SerializeField] private GameObject adsPanel;
    [SerializeField] private RectTransform nyufiyBgObj;


    public SceneLoadManager.SceneType sceneType;

    private void OnEnable()
    {
        GetCoins();
        currentAdReward = amountWatch;
        UITransilation();
        GetResources();
        EnableAdsPanel(false);
        if(replayBtn != null)
            replayBtn.onClick.AddListener(PlayMore);
        if(backButton != null)
            backButton.onClick.AddListener(BackHome);
        CurrencyManager.Instance.OnNyufiyChanged += UpdateOnlyNyufiy;
        FoodInfo.OnFoodAddToHorse += ApplyFoodBuffs;
        FoodInfo.OnMoneyNotEnough += AdsPanel;
        UpdateAdsRewardText();
        if(watchBtn != null)
            watchBtn.onClick.AddListener(OnAdsButtonAction);
    }

    private void OnDisable()
    {
        if (replayBtn != null)
            replayBtn.onClick.RemoveAllListeners();
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
        if (watchBtn != null)
            watchBtn.onClick.RemoveAllListeners();
        CurrencyManager.Instance.OnNyufiyChanged -= UpdateOnlyNyufiy;
        FoodInfo.OnFoodAddToHorse -= ApplyFoodBuffs;
        FoodInfo.OnMoneyNotEnough -= AdsPanel;
        pageMode = FoodPageMode.Normal;
        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }
    private void OnDestroy()
    {
        SetData();
    }
    private void BackHome()
    {
        if (pageMode == FoodPageMode.KopkariRoundChange)
        {
            SetData();
            KopkariMainUI.Instance?.HideRoundFoodPanel();
            return;
        }

        if (SceneLoadManager.Instance.CurrentSceneType==SceneLoadManager.SceneType.Home)
        {
            HomeMainUI.Instance.HideUI(this);
            return;
        }
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
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
                backText.text = lang.GetText(362);
            }
            powerText.text = lang.GetText(326);
            coolingText.text = lang.GetText(327);
            staminaText.text = lang.GetText(328);
            if(feedHorseText != null)
            {
                feedHorseText.text = lang.GetText(559);
            }
            if(feedHorseDescription != null)
            {
                feedHorseDescription.text = lang.GetText(560);
            }

            if (backButton != null)
                backButton.gameObject.SetActive(pageMode != FoodPageMode.KopkariRoundChange);
            if (replayText != null)
            {
                int textId = pageMode == FoodPageMode.KopkariRoundChange
                    ? continueTextLanguageId
                    : replayTextLanguageId;
                if (textId >= 0)
                    replayText.text = lang.GetText(textId);
            }

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
        if (nyufiyText == null)
        {
            return;
        }
        nyufiyText.text = amount > 0 ? $"{amount:N0}" : "0";
    }
    private void UpdateTexts(int nyufiy, int coin)
    {
        if(nyufiyText==null || coinText == null)
        {
            return;
        }
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
        {
            adsPanel.SetActive(false);
            RefreshEntryRequirementUI();
        }

    }
    #endregion

    #region Replay Section
    public void ShowForKopkariRoundChange(float requiredPercent = 15f)
    {
        pageMode = FoodPageMode.KopkariRoundChange;
        roundRequiredPercent = Mathf.Clamp(requiredPercent, 0f, 100f);

        if (isActiveAndEnabled)
        {
            UITransilation();
            GetResources();
        }
    }

    public void PlayMore()
    {
        if (pageMode == FoodPageMode.KopkariRoundChange)
        {
            if (!MeetsCurrentEntryRequirement())
            {
                PlayResourceAnim();
                return;
            }

            SetData();
            KopkariMainUI.Instance?.HideRoundFoodPanel();
            return;
        }

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
            //PlayAgainText();
            SetData();
            SceneLoadManager.SceneType replayScene = ResolveReplaySceneType();
            if (replayScene == SceneLoadManager.SceneType.None)
            {
                Debug.LogWarning("[GameFood] Replay scene could not be resolved.");
                return;
            }

            UIOverlayRoot.I?.ShowMovementPanelForScene(replayScene);
            SceneLoadManager.Instance?.ReloadOrBackScene(replayScene);
        }
    }

    private SceneLoadManager.SceneType ResolveReplaySceneType()
    {
        if (sceneType != SceneLoadManager.SceneType.None)
            return sceneType;

        if (SceneLoadManager.Instance == null)
            return SceneLoadManager.SceneType.None;

        SceneLoadManager.SceneType currentScene = SceneLoadManager.Instance.CurrentSceneType;
        return currentScene == SceneLoadManager.SceneType.Home
            ? SceneLoadManager.SceneType.None
            : currentScene;
    }
    private void PlayResourceAnim()
    {
        GetEntryRequirements(out float requiredPower, out float requiredCooling, out float requiredStamina);

        if (mPower < requiredPower)
        {
            PlayScaleAnim(notEnoughPowerBg);
        }
        if (mCooling < requiredCooling)
        {
            PlayScaleAnim(notEnoughCoolingBg);
        }
        if (mStamina < requiredStamina)
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
        RefreshEntryRequirementUI();

    }
    private void ApplyFoodBuffs(float powerPercent, float coolingPercent, float staminaPercent)
    {
        resourceUpdated = true;

        // Apply the selected food buffs.
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
    private void RefreshEntryRequirementUI()
    {
        GetEntryRequirements(out float requiredPower, out float requiredCooling, out float requiredStamina);

        bool powerRequired = mPower < requiredPower;
        bool coolingRequired = mCooling < requiredCooling;
        bool staminaRequired = mStamina < requiredStamina;

        SetRequirementText(notEnoughPowerBg, notEnoughPowerText, powerRequired, mPower, requiredPower,
            HorseConditionStatsService.GetCachedMaxOrDefault().Power);
        SetRequirementText(notEnoughCoolingBg, notEnoughCoolingText, coolingRequired, mCooling, requiredCooling,
            HorseConditionStatsService.GetCachedMaxOrDefault().Cooling);
        SetRequirementText(notEnoughStaminaBg, notEnoughStaminaText, staminaRequired, mStamina, requiredStamina,
            HorseConditionStatsService.GetCachedMaxOrDefault().Stamina);
    }

    private void SetRequirementText(
        RectTransform background,
        TMP_Text detailText,
        bool isRequired,
        float current,
        float required,
        float maximum)
    {
        if (background != null)
            background.gameObject.SetActive(true);

        if (detailText == null)
            return;

        if (!isRequired)
        {
            SetText(detailText, current, required);
            return;
        }

        detailText.color = badConditionColor;
        if (pageMode == FoodPageMode.KopkariRoundChange)
        {
            float currentPercent = maximum > 0f ? Mathf.Clamp01(current / maximum) * 100f : 0f;
            float missingPercent = Mathf.Max(0f, roundRequiredPercent - currentPercent);
            detailText.text = $"{GetRequiredPrefix()}: {Mathf.CeilToInt(missingPercent)}%";
        }
        else
        {
            float missingAmount = Mathf.Max(0f, required - current);
            detailText.text = $"{GetRequiredPrefix()}: {Mathf.CeilToInt(missingAmount)}";
        }
    }

    private string GetRequiredPrefix()
    {
        LanguageManager language = LanguageManager.Instance;
        if (requiredPrefixLanguageId >= 0 && language != null && language.IsReady)
        {
            string localized = language.GetText(requiredPrefixLanguageId);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized.Trim().TrimEnd(':');
        }

        return "Required";
    }

    private bool MeetsCurrentEntryRequirement()
    {
        GetEntryRequirements(out float requiredPower, out float requiredCooling, out float requiredStamina);
        return mPower >= requiredPower && mCooling >= requiredCooling && mStamina >= requiredStamina;
    }

    private void GetEntryRequirements(out float power, out float cooling, out float stamina)
    {
        if (pageMode == FoodPageMode.KopkariRoundChange)
        {
            HorseConditionStats max = HorseConditionStatsService.GetCachedMaxOrDefault();
            float multiplier = roundRequiredPercent / 100f;
            power = max.Power * multiplier;
            cooling = max.Cooling * multiplier;
            stamina = max.Stamina * multiplier;
            return;
        }

        power = Constants.HorseConditionNum.Power;
        cooling = Constants.HorseConditionNum.Cool;
        stamina = Constants.HorseConditionNum.Stamina;
    }

    private void SetText(TMP_Text detailText, float num, float limitNum)
    {
        if (num >= Constants.HorseConditionNum.GoodCondition)
        {
            GoodCondition(detailText);
        }
        else if(num<Constants.HorseConditionNum.GoodCondition && num>=limitNum)
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
        int currentNyufiy = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.Nyufiy
            : 0;
        int missingNyufiy = Mathf.Max(0, FoodInfo.LastFailedFoodCost - currentNyufiy);
        currentAdReward = Mathf.Max(amountWatch, missingNyufiy);
        UpdateAdsRewardText();
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

    private void UpdateAdsRewardText()
    {
        if (adsAmount != null)
            adsAmount.text = $"+{currentAdReward:N0}";
    }
    #endregion

    #region Ads Section
    public void OnAdsButtonAction()
    {
        GameAnalyticsEvents.RewardedAdClicked(
            placement: "coin_shop",
            rewardType: "nyufiy",
            rewardAmount: currentAdReward
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(currentAdReward, true);

            UpdateTexts(
                CurrencyManager.Instance.Nyufiy,
                CurrencyManager.Instance.Coin
            );

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: currentAdReward
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: currentAdReward
            );

            EnableAdsPanel(false);
        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }
    #endregion
}
