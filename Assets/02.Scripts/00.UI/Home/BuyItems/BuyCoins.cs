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

    [SerializeField] private TMP_Text coinTitle;
    [SerializeField] private TMP_Text coinTitle2;
    [SerializeField] private TMP_Text nyufiyTitle;
    [SerializeField] private TMP_Text nyufiyTitle2;
    [SerializeField] private TMP_Text moneyNotEnoughText;

    [SerializeField] private GameObject coinSection;
    [SerializeField] private GameObject nyufiySection;
    [SerializeField] private GameObject moneyNotEnoughSection;
    [SerializeField] private float animDuration = 3f;

    private Tween pulseTween;
    private void OnEnable()
    {
        UITrasilations();
        coinButton.onClick.AddListener(OpenCoinSection);
        nyufiyButton.onClick.AddListener(OpenNyufiySection);
        closeButton.onClick.AddListener(ClosePage);
        HomeMainUI.Instance.OnCoinsButtonPressed += EnablePages;
        CoinCard.OnMoneyNotEnough += MoneyNotEnoughText;
        if(moneyNotEnoughSection.activeSelf)
            moneyNotEnoughSection.SetActive(false);
        
    }
    private void OnDisable()
    {
        coinButton.onClick.RemoveListener(OpenCoinSection);
        nyufiyButton.onClick.RemoveListener(OpenNyufiySection);
        closeButton.onClick.RemoveListener(ClosePage);
        HomeMainUI.Instance.OnCoinsButtonPressed -= EnablePages;
        CoinCard.OnMoneyNotEnough -= MoneyNotEnoughText;
    }

    private void OpenCoinSection()
    {
        //HomeMainUI.Instance.ShowUI(coinSection);
        coinSection.SetActive(true);
        if (nyufiySection.activeSelf)
            nyufiySection.SetActive(false);
        CloseMoneyNotEnough();
    }
    private void OpenNyufiySection()
    {
        nyufiySection.SetActive(true);

        if (coinSection.activeSelf)
            coinSection.SetActive(false);
        CloseMoneyNotEnough();
    }

    private void EnablePages(bool enabled)
    {
        if(enabled)
        {
            EnableCoinSection();
        }
        else
        {
            EnableNyufiySection();
        }
        CloseMoneyNotEnough();
    }
    private void EnableCoinSection()
    {
        if (nyufiySection.activeSelf)
        {
            nyufiySection.SetActive(false);
        }
        coinSection.SetActive(true);
        
    }
    private void EnableNyufiySection()
    {
        if (coinSection.activeSelf)
        {
            coinSection.SetActive(false);
        }
        nyufiySection.SetActive(true);
    }
    private void UITrasilations()
    {
        if (LanguageManager.Instance != null)
        {
            coinTitle.text = LanguageManager.Instance.GetText(390);
            coinTitle2.text = LanguageManager.Instance.GetText(390);
            nyufiyTitle.text = LanguageManager.Instance.GetText(389);
            nyufiyTitle2.text = LanguageManager.Instance.GetText(389);
            moneyNotEnoughText.text = LanguageManager.Instance.GetText(333);
            //closeText.text = LanguageManager.Instance.GetText(362);
        }
    }
    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void MoneyNotEnoughText()
    {
        moneyNotEnoughSection.SetActive(true);

        RectTransform rt = moneyNotEnoughSection.GetComponent<RectTransform>();
        CanvasGroup cg = moneyNotEnoughSection.GetComponent<CanvasGroup>();

        // Old tweensni to‘xtatish
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
        // Agar o‘chib bo‘lsa — hech narsa qilmaymiz
        if (!moneyNotEnoughSection.activeSelf) return;

        RectTransform rt = moneyNotEnoughSection.GetComponent<RectTransform>();
        CanvasGroup cg = moneyNotEnoughSection.GetComponent<CanvasGroup>();

        // HAMMA animlarni darhol to‘xtatamiz
        rt.DOKill();
        cg.DOKill();
        pulseTween?.Kill();

        // Reset (optional, lekin toza holat uchun)
        if (cg != null) cg.alpha = 1f;
        if (rt != null) rt.localScale = Vector3.one;

        moneyNotEnoughSection.SetActive(false);
    }

}
