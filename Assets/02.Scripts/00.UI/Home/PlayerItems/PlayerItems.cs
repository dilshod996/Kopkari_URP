using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItems : MonoBehaviour
{
    //[SerializeField] private Button horseNutritionPage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAddButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountTitleText;
    [SerializeField] private TMP_Text moneyNotEnoughText;
    [SerializeField] private TMP_Text backText;
    //[SerializeField] private TMP_Text horseNutritionText;
    [SerializeField] private GameObject bottomMoneyNotEnoughtObj;
    [SerializeField] private TMP_Text watchAmountText;
    private void OnEnable()
    {
        UIText();
        closeButton.onClick.AddListener(ClosePage);
        //horseNutritionPage.onClick.AddListener(OpenHorseNutritionPage);
        PlayerResourse.OnMoneyNotEnough += MoneyNotEnoughText;
        watchAddButton.onClick.AddListener(WatchAdds);  
    }
    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(ClosePage);
        //horseNutritionPage.onClick.RemoveListener(OpenHorseNutritionPage);
        PlayerResourse.OnMoneyNotEnough -= MoneyNotEnoughText;
        watchAddButton.onClick.RemoveAllListeners();
    }
    private void UIText()
    {
        if (bottomMoneyNotEnoughtObj.activeSelf)
        {
            bottomMoneyNotEnoughtObj.SetActive(false);
        }
        if(LanguageManager.Instance != null)
        {
            titleText.text = LanguageManager.Instance.GetText(386);
            backText.text = LanguageManager.Instance.GetText(362);
           // horseNutritionText.text = LanguageManager.Instance.GetText(395);
            amountTitleText.text = LanguageManager.Instance.GetText(220);
        }
    }
    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void MoneyNotEnoughText()
    {
        int rewardAmount = Mathf.Max(0, PlayerResourse.LastFailedResourceCost - CurrencyManager.Instance.Nyufiy);
        if (watchAmountText != null)
            watchAmountText.text = $"+{rewardAmount:N0}";

        if(!bottomMoneyNotEnoughtObj.activeSelf)
        {
            bottomMoneyNotEnoughtObj.SetActive(true);
            moneyNotEnoughText.text = LanguageManager.Instance.GetText(363);
        }
    }
    private void WatchAdds()
    {
        int rewardAmount = Mathf.Max(0, PlayerResourse.LastFailedResourceCost - CurrencyManager.Instance.Nyufiy);
        if (rewardAmount <= 0)
            return;

        if (watchAmountText != null)
            watchAmountText.text = $"+{rewardAmount:N0}";

        GameAnalyticsEvents.RewardedAdClicked(
            placement: "coin_shop",
            rewardType: "nyufiy",
            rewardAmount: rewardAmount
        );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            CurrencyManager.Instance.AddNyufiy(rewardAmount, true);

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: rewardAmount
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: rewardAmount
            );

        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }
}
