using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyCoins : MonoBehaviour
{
    [SerializeField] private Button coinButton;
    [SerializeField] private Button nyufiyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAddBtton;

    [Header("Selected State")]
    [SerializeField] private GameObject coinSelectedObject;
    [SerializeField] private GameObject nyufiySelectedObject;

    [SerializeField] private TMP_Text coinTitle;
    [SerializeField] private TMP_Text nyufiyTitle;
    [SerializeField] private TMP_Text moneyNotEnoughText;

    [SerializeField] private GameObject coinSection;
    [SerializeField] private GameObject nyufiySection;
    [SerializeField] private GameObject moneyNotEnoughSection;
    [SerializeField] private float animDuration = 3f;

    [SerializeField] private int watchAddAmount = 300;

    private Tween pulseTween;
    private void OnEnable()
    {
        UITrasilations();
        if (coinButton != null)
            coinButton.onClick.AddListener(OpenCoinSection);
        if (nyufiyButton != null)
            nyufiyButton.onClick.AddListener(OpenNyufiySection);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePage);
        if (HomeMainUI.Instance != null)
            HomeMainUI.Instance.OnCoinsButtonPressed += EnablePages;
        CoinCard.OnMoneyNotEnough += MoneyNotEnoughText;
        if(moneyNotEnoughSection != null && moneyNotEnoughSection.activeSelf)
            moneyNotEnoughSection.SetActive(false);
        if (watchAddBtton != null)
            watchAddBtton.onClick.AddListener(WatchAddForNyufiy);

    }
    private void OnDisable()
    {
        if (coinButton != null)
            coinButton.onClick.RemoveListener(OpenCoinSection);
        if (nyufiyButton != null)
            nyufiyButton.onClick.RemoveListener(OpenNyufiySection);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePage);
        if (watchAddBtton != null)
            watchAddBtton.onClick.RemoveListener(WatchAddForNyufiy);
        if (HomeMainUI.Instance != null)
            HomeMainUI.Instance.OnCoinsButtonPressed -= EnablePages;
        CoinCard.OnMoneyNotEnough -= MoneyNotEnoughText;
        DOTween.Kill(this);
        pulseTween?.Kill();
    }
    private void WatchAddForNyufiy()
    {
        // Bu yerda reklama ko‘rish logikasini qo‘shing
        // Agar reklama muvaffaqiyatli ko‘rilsa, foydalanuvchiga watchAddAmount miqdorida Nyufiy bering
        Debug.Log($"User watched an ad and received {watchAddAmount} Nyufiy.");
        // Masalan:
        // UserInventory.Instance.AddNyufiy(watchAddAmount);
        GameAnalyticsEvents.RewardedAdClicked(
           placement: "coin_shop",
           rewardType: "nyufiy",
           rewardAmount: watchAddAmount
       );

        if (AdsManager.Instance == null)
        {
            GameAnalyticsEvents.RewardedAdFailed("coin_shop");
            return;
        }

        AdsManager.Instance.ShowRewarded(() =>
        {
            if (CurrencyManager.Instance == null)
            {
                GameAnalyticsEvents.RewardedAdFailed("coin_shop");
                return;
            }

            CurrencyManager.Instance.AddNyufiy(watchAddAmount, true);

            GameAnalyticsEvents.RewardedAdCompleted(
                placement: "coin_shop",
                rewardType: "nyufiy",
                rewardAmount: watchAddAmount
            );

            GameAnalyticsEvents.CoinRewardClaimed(
                source: "rewarded_ad_coin_shop",
                amount: watchAddAmount
            );

        },
        () => GameAnalyticsEvents.RewardedAdFailed("coin_shop"));
    }

    public void OpenCoinSection()
    {
        ShowSection(true);
    }
    public void OpenNyufiySection()
    {
        ShowSection(false);
    }

    private void EnablePages(bool enabled)
    {
        ShowSection(enabled);
    }

    public void ShowCoinCards()
    {
        ShowSection(true);
    }

    public void ShowNyufiyCards()
    {
        ShowSection(false);
    }

    private void ShowSection(bool showCoinCards)
    {
        if (coinSection != null)
            coinSection.SetActive(showCoinCards);
        if (nyufiySection != null)
            nyufiySection.SetActive(!showCoinCards);

        if (coinSelectedObject != null)
            coinSelectedObject.SetActive(showCoinCards);
        if (nyufiySelectedObject != null)
            nyufiySelectedObject.SetActive(!showCoinCards);

        CloseMoneyNotEnough();
    }
    private void UITrasilations()
    {
        if (LanguageManager.Instance != null)
        {
            if (coinTitle != null) coinTitle.text = LanguageManager.Instance.GetText(390);
            if (nyufiyTitle != null) nyufiyTitle.text = LanguageManager.Instance.GetText(389);
            if (moneyNotEnoughText != null) moneyNotEnoughText.text = LanguageManager.Instance.GetText(333);
            //closeText.text = LanguageManager.Instance.GetText(362);
        }
    }
    private void ClosePage()
    {
        if (HomeMainUI.Instance != null)
            HomeMainUI.Instance.HideUI(this);
        else
            gameObject.SetActive(false);
    }
    private void MoneyNotEnoughText()
    {
        if (moneyNotEnoughSection == null)
            return;

        moneyNotEnoughSection.SetActive(true);

        RectTransform rt = moneyNotEnoughSection.GetComponent<RectTransform>();
        CanvasGroup cg = moneyNotEnoughSection.GetComponent<CanvasGroup>();

        if (rt == null || cg == null)
        {
            Debug.LogWarning("Money not enough section needs RectTransform and CanvasGroup components.");
            return;
        }

        DOTween.Kill(this);
        rt.DOKill();
        cg.DOKill();
        pulseTween?.Kill();

        // Reset
        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        // Paydo bo‘lish anim
        cg.DOFade(1f, 0.15f).SetEase(Ease.OutQuad);
        rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        // 4 sekund davomida yengil pulse
        pulseTween = rt
            .DOScale(1.03f, 0.9f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 4 sekunddan keyin pulse to‘xtaydi, lekin UI qoladi
        DOVirtual.DelayedCall(animDuration, () =>
        {
            pulseTween?.Kill();
            rt.localScale = Vector3.one; // final holat
        }).SetTarget(this);
    }
    public void CloseMoneyNotEnough()
    {
        if (moneyNotEnoughSection == null)
            return;

        DOTween.Kill(this);

        // Agar o‘chib bo‘lsa — hech narsa qilmaymiz
        if (!moneyNotEnoughSection.activeSelf) return;

        RectTransform rt = moneyNotEnoughSection.GetComponent<RectTransform>();
        CanvasGroup cg = moneyNotEnoughSection.GetComponent<CanvasGroup>();

        // HAMMA animlarni darhol to‘xtatamiz
        if (rt != null) rt.DOKill();
        if (cg != null) cg.DOKill();
        pulseTween?.Kill();

        // Reset (optional, lekin toza holat uchun)
        if (cg != null) cg.alpha = 1f;
        if (rt != null) rt.localScale = Vector3.one;

        moneyNotEnoughSection.SetActive(false);
    }

}
