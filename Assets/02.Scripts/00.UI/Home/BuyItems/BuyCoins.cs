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
    [SerializeField] private TMP_Text closeText;

    [SerializeField] private GameObject coinSection;
    [SerializeField] private GameObject nyufiySection;


    private void OnEnable()
    {
        UITrasilations();
        coinButton.onClick.AddListener(OpenCoinSection);
        nyufiyButton.onClick.AddListener(OpenNyufiySection);
        closeButton.onClick.AddListener(ClosePage);
        HomeMainUI.Instance.OnCoinsButtonPressed += EnablePages;
    }
    private void OnDisable()
    {
        coinButton.onClick.RemoveListener(OpenCoinSection);
        nyufiyButton.onClick.RemoveListener(OpenNyufiySection);
        closeButton.onClick.RemoveListener(ClosePage);
        HomeMainUI.Instance.OnCoinsButtonPressed -= EnablePages;
    }

    private void OpenCoinSection()
    {
        HomeMainUI.Instance.ShowUI(coinSection);

        if (nyufiySection.activeSelf)
            nyufiySection.SetActive(false);
    }
    private void OpenNyufiySection()
    {
        HomeMainUI.Instance.ShowUI(nyufiySection);

        if (coinSection.activeSelf)
            coinSection.SetActive(false);
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
            closeText.text = LanguageManager.Instance.GetText(362);
        }
    }
    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
}
