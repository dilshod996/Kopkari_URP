using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapShowPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text mapName;
    [SerializeField] private Image mapImage;
    [SerializeField] private Image popupBackgroundImage;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Translation Texts")]
    [SerializeField] private TMP_Text buyBtnText;
    [SerializeField] private TMP_Text roomEntryCostText;
    [SerializeField] private TMP_Text winningRewardsText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text ridersText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text mapCostShowerText;

    [Header("Shower Texts")]
    [SerializeField] private TMP_Text roomEntryCostShower;
    [SerializeField] private TMP_Text mapCostAmountText;
    [SerializeField] private TMP_Text nyufiyAmountText;
    [SerializeField] private TMP_Text coinAmountText;
    [SerializeField] private TMP_Text distanceShower;
    [SerializeField] private TMP_Text ridersShower;
    [SerializeField] private TMP_Text statusShower;

    [Header("Details Sections")]
    [SerializeField] private GameObject mapBuyDetails;
    [SerializeField] private GameObject playDetails;
    [SerializeField] private GameObject moneySection;

    [Header("Language Ids")]
    [SerializeField] private int playTextId = 4;
    [SerializeField] private int buyTextId = 424;
    [SerializeField] private int mapCostTextId = -1;
    [SerializeField] private int roomEntryCostTextId = -1;
    [SerializeField] private int winningRewardsTextId = -1;
    [SerializeField] private int distanceTextId = -1;
    [SerializeField] private int ridersTextId = -1;
    [SerializeField] private int statusTextId = -1;
    [SerializeField] private int openTextId = -1;
    [SerializeField] private int lockTextId = -1;

    [Header("Status Colors")]
    [SerializeField] private Color openStatusColor = new Color32(0x08, 0xFF, 0x11, 0xFF);
    [SerializeField] private Color lockStatusColor = Color.red;

    [SerializeField] private GameObject notEnoughObj;
    [SerializeField] private TMP_Text notEnoughCoin;
    [SerializeField] private Button moveCoinPage;
    [SerializeField] private TMP_Text moveCoinPageBtnText;

    [SerializeField] private float animDuration = 3f;

    private MapCard.MapDetailsData currentMapData;
    private Tween pulseTween;
    private Tween moneySectionTween;
    private Tween mapImageTween;
    private float mapImageDefaultAlpha = 1f;
    private readonly List<string> preloadRacing = new List<string>();

    private void Awake()
    {
        if (mapImage != null)
            mapImageDefaultAlpha = mapImage.color.a;
    }

    private void OnEnable()
    {
        if (cancelBtn != null)
            cancelBtn.onClick.AddListener(ClosePopup);

        if (notEnoughObj != null && notEnoughObj.activeSelf)
            notEnoughObj.SetActive(false);

        UITransilations();

        if (moveCoinPage != null)
            moveCoinPage.onClick.AddListener(MoveCoinPage);
    }

    private void OnDisable()
    {
        if (cancelBtn != null)
            cancelBtn.onClick.RemoveListener(ClosePopup);

        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveListener(BuyMap);
            buyBtn.onClick.RemoveListener(EnterPlayRoom);
        }

        if (moveCoinPage != null)
            moveCoinPage.onClick.RemoveListener(MoveCoinPage);

        pulseTween?.Kill();
        moneySectionTween?.Kill();
        moneySectionTween = null;
        ResetMapImageAnimation();
        DOTween.Kill(this);
    }

    private void UITransilations()
    {

        SetLocalizedText(roomEntryCostText, roomEntryCostTextId, "Room Entry Text");
        SetLocalizedText(winningRewardsText, winningRewardsTextId, "Winning Rewards Text");
        SetLocalizedText(distanceText, distanceTextId, "Distance Text");
        SetLocalizedText(ridersText, ridersTextId, "Riders Text");
        SetLocalizedText(statusText, statusTextId, "Status Text");
        SetLocalizedText(mapCostShowerText, mapCostTextId, "Map Cost");

        if (notEnoughCoin != null)
            SetLocalizedText(notEnoughCoin, 406, "Not enough");

        if (moveCoinPageBtnText != null)
            SetLocalizedText(moveCoinPageBtnText, 390, "Coins");
    }

    public void SetMapData(MapCard.MapDetailsData data)
    {
        currentMapData = data;

        string localizedName = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(data.MapLangCode)
            : data.MapKey;

        if (mapName != null)
            mapName.text = localizedName;

        if (mapImage != null)
            mapImage.sprite = data.MapSprite;
        AnimateMapImage();

        if (popupBackgroundImage != null)
            popupBackgroundImage.color = data.BackgroundColor;

        bool isUnlocked = data.IsUnlocked || IsMapAlreadyUnlocked();
        currentMapData.IsUnlocked = isUnlocked;

        if (roomEntryCostShower != null)
            roomEntryCostShower.text = isUnlocked ? FormatSignedCost(data.PlayCost) : FormatCost(data.UnlockCost);

        if (mapCostAmountText != null)
            mapCostAmountText.text = FormatCost(data.UnlockCost);

        if (nyufiyAmountText != null)
            nyufiyAmountText.text = data.NyufiyAmount > 0 ? $"{data.NyufiyAmount:N0}" : "0";

        if (coinAmountText != null)
            coinAmountText.text = data.CoinAmount > 0 ? $"{data.CoinAmount:N0}" : "0";

        if (distanceShower != null)
            distanceShower.text = data.Distance > 0 ? $"{data.Distance:N0}" : "0";

        if (ridersShower != null)
            ridersShower.text = data.RidersAmount > 0 ? $"{data.RidersAmount:N0}" : "0";

        UpdateDetailsSections(isUnlocked);
        UpdateStatusText(isUnlocked);
        ConfigurePrimaryButton(isUnlocked);
    }

    public void ClosePopup()
    {
        HomeMainUI.Instance.HideUI(this);
    }

    private void ConfigurePrimaryButton(bool isUnlocked)
    {
        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveListener(BuyMap);
            buyBtn.onClick.RemoveListener(EnterPlayRoom);

            if (isUnlocked)
                buyBtn.onClick.AddListener(EnterPlayRoom);
            else
                buyBtn.onClick.AddListener(BuyMap);
        }

        SetLocalizedText(buyBtnText, isUnlocked ? playTextId : buyTextId, isUnlocked ? "Play" : "Buy");
    }

    private void UpdateDetailsSections(bool isUnlocked)
    {
        SetActive(mapBuyDetails, !isUnlocked);
        SetActive(playDetails, isUnlocked);
        SetActive(moneySection, true);

        if (isUnlocked)
        {
            moneySectionTween?.Kill();
            moneySectionTween = null;
            ResetMoneySection();
            return;
        }

        AnimateMoneySection();
    }

    private void AnimateMoneySection()
    {
        if (moneySection == null)
            return;

        RectTransform rt = moneySection.GetComponent<RectTransform>();
        Transform target = rt != null ? rt : moneySection.transform;
        CanvasGroup cg = moneySection.GetComponent<CanvasGroup>();

        target.DOKill();
        if (cg != null)
            cg.DOKill();

        target.localScale = Vector3.one * 0.94f;
        if (cg != null)
            cg.alpha = 0f;

        Sequence sequence = DOTween.Sequence().SetTarget(moneySection);

        if (cg != null)
            sequence.Join(cg.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));

        sequence.Join(target.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
        sequence.Append(target.DOScale(1.03f, 0.75f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
        sequence.AppendInterval(1.22f);
        sequence.OnKill(() => moneySectionTween = null);
        sequence.OnComplete(() =>
        {
            target.localScale = Vector3.one;
            if (cg != null)
                cg.alpha = 1f;
        });

        moneySectionTween = sequence;
    }

    private void ResetMoneySection()
    {
        if (moneySection == null)
            return;

        Transform target = moneySection.transform;
        CanvasGroup cg = moneySection.GetComponent<CanvasGroup>();

        target.DOKill();
        if (cg != null)
        {
            cg.DOKill();
            cg.alpha = 1f;
        }

        target.localScale = Vector3.one;
    }

    private void AnimateMapImage()
    {
        if (mapImage == null)
            return;

        mapImageTween?.Kill();
        mapImageTween = null;

        Transform target = mapImage.transform;
        target.DOKill();
        mapImage.DOKill();

        target.localScale = Vector3.one * 0.92f;
        Color color = mapImage.color;
        color.a = 0f;
        mapImage.color = color;

        Sequence sequence = DOTween.Sequence().SetTarget(mapImage);
        sequence.Join(mapImage.DOFade(mapImageDefaultAlpha, 0.16f).SetEase(Ease.OutQuad));
        sequence.Join(target.DOScale(1.03f, 0.28f).SetEase(Ease.OutBack));
        sequence.Append(target.DOScale(1f, 0.12f).SetEase(Ease.OutSine));
        sequence.Append(target.DOScale(1.025f, 1.1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
        sequence.OnKill(() => mapImageTween = null);

        mapImageTween = sequence;
    }

    private void ResetMapImageAnimation()
    {
        if (mapImage == null)
            return;

        mapImageTween?.Kill();
        mapImageTween = null;

        Transform target = mapImage.transform;
        target.DOKill();
        mapImage.DOKill();
        target.localScale = Vector3.one;

        Color color = mapImage.color;
        color.a = mapImageDefaultAlpha;
        mapImage.color = color;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private void UpdateStatusText(bool isUnlocked)
    {
        SetLocalizedText(statusShower, isUnlocked ? openTextId : lockTextId, isUnlocked ? "Open" : "Lock");

        if (statusShower != null)
            statusShower.color = isUnlocked ? openStatusColor : lockStatusColor;
    }

    private void SetLocalizedText(TMP_Text target, int textId, string fallback)
    {
        if (target == null)
            return;

        string localized = LanguageManager.Instance != null && textId >= 0
            ? LanguageManager.Instance.GetText(textId)
            : string.Empty;

        target.text = string.IsNullOrEmpty(localized) ? fallback : localized;
    }

    private static string FormatCost(int cost)
    {
        return cost > 0 ? $"{cost:N0}" : "0";
    }

    private static string FormatSignedCost(int cost)
    {
        return cost > 0 ? $"{cost:N0}" : "0";
    }

    private void BuyMap()
    {
        bool success = CurrencyManager.Instance.SpendCoin(currentMapData.UnlockCost, true);

        if (!success)
        {
            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            MoneyNotEnoughText();
            return;
        }

        if (DataManager.Instance != null)
        {
            DataManager.Instance.UnlockMap(currentMapData.MapKey, true);
        }
        else
        {
            PlayerPrefs.SetInt(currentMapData.MapKey, 1);
            PlayerPrefs.Save();
        }

        currentMapData.IsUnlocked = true;
        if (roomEntryCostShower != null)
            roomEntryCostShower.text = FormatSignedCost(currentMapData.PlayCost);
        UpdateDetailsSections(true);
        UpdateStatusText(true);
        ConfigurePrimaryButton(true);

        HomeMainUI.Instance.DisplayAutoReward(
            currentMapData.MapSprite,
            LanguageManager.Instance.GetText(409),
            LanguageManager.Instance.GetText(405),
            mapName != null ? mapName.text : currentMapData.MapKey
        );

        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
    }

    private bool IsMapAlreadyUnlocked()
    {
        if (IsAlwaysOpenMap(currentMapData.MapKey))
            return true;

        if (DataManager.Instance != null)
            return DataManager.Instance.IsMapUnlocked(currentMapData.MapKey);

        int defaultValue = IsAlwaysOpenMap(currentMapData.MapKey) ? 1 : 0;
        return PlayerPrefs.GetInt(currentMapData.MapKey, defaultValue) == 1;
    }

    private static bool IsAlwaysOpenMap(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return false;

        string normalizedKey = mapKey.Trim();

        return normalizedKey == Constants.MapNames.RacingTraining ||
               normalizedKey == Constants.MapNames.Zarafshan ||
               normalizedKey == "TrainingRacing" ||
               normalizedKey == "FirstRacing" ||
               normalizedKey == "Training";
    }

    private void EnterPlayRoom()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());

        if (current.Power < Constants.HorseConditionNum.Power ||
            current.Cooling < Constants.HorseConditionNum.Cool ||
            current.Stamina < Constants.HorseConditionNum.Stamina)
        {
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            HomeMainUI.Instance?.SHowFoodPanel();
            CloseMapField();
            return;
        }

        int defenseCheck = DataManager.Instance.GetItemAmount(Constants.PlayerItems.Defense);
        if (defenseCheck < 1)
            UIOverlayRoot.I.Confirm(493, 494, 496, 253, OpenTacticItemsPanel, SpendCostAndMoveRoom);
        else
            SpendCostAndMoveRoom();
    }

    private void SpendCostAndMoveRoom()
    {
        if (currentMapData.PlayCost > 0)
        {
            bool success = CurrencyManager.Instance.SpendNyufiy(currentMapData.PlayCost);
            if (!success)
            {
                Debug.Log("Money is not enough to play popup");
                UIOverlayRoot.I.Confirm(487, 488, 489, 490, MoveToShop, WatchAdds);
                return;
            }
        }

        MovingRoom();
    }

    private void MovingRoom()
    {
        UIOverlayRoot.I.ShowMovementPanel(currentMapData);

        preloadRacing.Clear();
        if (currentMapData.MapType == MapCard.MapType.Racing)
            preloadRacing.Add(Constants.RoomSound.RacingSound);

        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        SceneLoadManager.Instance.LoadSceneNew(currentMapData.MovingRoom, preloadRacing);
    }

    private void OpenTacticItemsPanel()
    {
        if(this.gameObject.activeSelf)
            { this.gameObject.SetActive(false); }
        HomeMainUI.Instance.ShowSuppliesPanel();
    }

    private void MoveToShop()
    {
        HomeMainUI.Instance.NyufiyClicked();
        CloseMapField();
    }

    private void WatchAdds()
    {
        GameAnalyticsEvents.RewardedAdClicked(
            placement: "coin_shop",
            rewardType: "nyufiy",
            rewardAmount: currentMapData.RewardAdAmount
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(currentMapData.RewardAdAmount, true);

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: currentMapData.RewardAdAmount
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: currentMapData.RewardAdAmount
            );
        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }

    private void MoveCoinPage()
    {
        HomeMainUI.Instance.CoinClicked();
        CloseMapField();
        gameObject.SetActive(false);
    }

    private void CloseMapField()
    {
        if (currentMapData.MapType == MapCard.MapType.Racing)
            HomeMainUI.Instance.CloseRacingField();
        else
            HomeMainUI.Instance.CloseKopkariFeld();
        this.gameObject.SetActive(false);
    }

    private void MoneyNotEnoughText()
    {
        if (notEnoughObj == null)
            return;

        notEnoughObj.SetActive(true);

        RectTransform rt = notEnoughObj.GetComponent<RectTransform>();
        CanvasGroup cg = notEnoughObj.GetComponent<CanvasGroup>();

        if (rt == null || cg == null)
            return;

        rt.DOKill();
        cg.DOKill();
        pulseTween?.Kill();
        DOTween.Kill(this);

        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        cg.DOFade(1f, 0.15f).SetEase(Ease.OutQuad);
        rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        pulseTween = rt
            .DOScale(1.03f, 0.9f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        DOVirtual.DelayedCall(animDuration, () =>
        {
            pulseTween?.Kill();
            rt.localScale = Vector3.one;
        }).SetTarget(this);
    }
}
