using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class UIStore : MonoBehaviour
{
    public enum MarketPages
    {
        Currencies,
        Boosters,
        Maps,
        Skins
    }

    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text backBtnText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text boosterText;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text skinText;

    [Header("Buttons")]
    [SerializeField] private Button currencyBtn;
    [SerializeField] private Button boosterBtn;
    [SerializeField] private Button mapBtn;
    [SerializeField] private Button skinBtn;
    [SerializeField] private Button backButton;

    [Header("Clicked Objects")]
    [SerializeField] private GameObject[] clickedObjs;


    [Header("Pages")]
    [SerializeField] private GameObject[] pages;

    private int _currentIndexPage = -1;

    [Header("Packs (order matters: 0..3)")]
    [SerializeField] private RectTransform[] packs;   // 4 ta pack obj

    [Header("ButtonsPacks")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Positions")]
    [SerializeField] private RectTransform mainPos;

    [Header("Move Settings")]
    [SerializeField] private float moveX = 900f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    private int _currentIndex = 0;
    private bool _isAnimating;
    private void OnEnable()
    {
        backButton.onClick.AddListener(BackAction);

        currencyBtn.onClick.AddListener(() => SelectPage(MarketPages.Currencies));
        boosterBtn.onClick.AddListener(() => SelectPage(MarketPages.Boosters));
        mapBtn.onClick.AddListener(() => SelectPage(MarketPages.Maps));
        skinBtn.onClick.AddListener(() => SelectPage(MarketPages.Skins));

        UITextTranslations();

        // default page
        SelectPage(MarketPages.Currencies);
        InitalsNomadPacks();
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveAllListeners();
        currencyBtn.onClick.RemoveAllListeners();
        boosterBtn.onClick.RemoveAllListeners();
        mapBtn.onClick.RemoveAllListeners();
        skinBtn.onClick.RemoveAllListeners();
        DisableNomadicPacks();
    }

    private void SelectPage(MarketPages page)
    {
        int idx = (int)page;
        if (_currentIndexPage == idx) return; // qayta bosilsa bekor

        _currentIndexPage = idx;

        for (int i = 0; i < clickedObjs.Length; i++)
        {
            bool on = (i == idx);

            var c = clickedObjs[i];
            if (c != null && c.activeSelf != on) c.SetActive(on);

            var p = pages[i];
            if (p != null && p.activeSelf != on) p.SetActive(on);
        }
    }

    private void UITextTranslations()
    {
        var language = LanguageManager.Instance;
        if (language == null) return;
        backBtnText.text = language.GetText(362);
        titleText.text = language.GetText(25);
        currencyText.text = language.GetText(412);
        boosterText.text = language.GetText(413);
        mapText.text = language.GetText(414);
        skinText.text = language.GetText(415);
    }

    private void BackAction()
    {
        HomeMainUI.Instance.HideUI(this);
    }

    #region Nomadic Packs
    private void InitalsNomadPacks()
    {
        prevButton.onClick.AddListener(Prev);
        nextButton.onClick.AddListener(Next);

        ShowImmediate(0);
        RefreshButtons(); 
    }
    private void DisableNomadicPacks()
    {
        prevButton.onClick.RemoveListener(Prev);
        nextButton.onClick.RemoveListener(Next);
    }
    private void Next()
    {
        if (_isAnimating) return;
        if (_currentIndex >= packs.Length - 1) return;

        AnimateTo(_currentIndex + 1, dir: +1);
    }

    private void Prev()
    {
        if (_isAnimating) return;
        if (_currentIndex <= 0) return;

        AnimateTo(_currentIndex - 1, dir: -1);
    }

    private void AnimateTo(int newIndex, int dir)
    {
        _isAnimating = true;
        SetButtons(false);

        // 🔴 eski packni darhol o‘chiramiz
        packs[_currentIndex].DOKill();
        packs[_currentIndex].gameObject.SetActive(false);

        RectTransform next = packs[newIndex];
        next.DOKill();
        next.gameObject.SetActive(true);

        // start pos
        next.anchoredPosition = new Vector2(dir * moveX, 0f);

        // faqat yangi pack animatsiya qilinadi
        next.DOAnchorPosX(0f, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                _currentIndex = newIndex;
                _isAnimating = false;
                RefreshButtons();
                SetButtons(true);
            });
    }

    private void ShowImmediate(int index)
    {
        for (int i = 0; i < packs.Length; i++)
        {
            bool on = (i == index);
            packs[i].gameObject.SetActive(on);

            if (on)
            {
                packs[i].DOKill();
                packs[i].anchoredPosition = Vector2.zero;
            }
        }

        _currentIndex = index;
    }

    private void RefreshButtons()
    {
        prevButton.interactable = _currentIndex > 0;
        nextButton.interactable = _currentIndex < packs.Length - 1;
    }

    private void SetButtons(bool value)
    {
        prevButton.interactable = value && _currentIndex > 0;
        nextButton.interactable = value && _currentIndex < packs.Length - 1;
    }
    #endregion
}

