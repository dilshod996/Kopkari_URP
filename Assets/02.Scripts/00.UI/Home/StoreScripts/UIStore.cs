using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIStore : MonoBehaviour
{
    public enum MarketPages
    {
        Currencies,
        PlayerItems,
        Maps,
        Skins
    }

    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text backBtnText;
    [SerializeField] private TMP_Text currencyText;
    [FormerlySerializedAs("boosterText")]
    [SerializeField] private TMP_Text playerItemsText;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text skinText;

    [Header("Buttons")]
    [SerializeField] private Button currencyBtn;
    [FormerlySerializedAs("boosterBtn")]
    [SerializeField] private Button playerItemsBtn;
    [SerializeField] private Button mapBtn;
    [SerializeField] private Button skinBtn;
    [SerializeField] private Button backButton;

    [Header("Tab Text Colors")]
    [SerializeField] private Color selectedTabTextColor = new Color32(0x00, 0xDC, 0xFF, 0xFF);
    [SerializeField] private Color unselectedTabTextColor = new Color32(0xEE, 0xD3, 0x66, 0xFF);

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
        AddButtonListener(backButton, BackAction);

        AddButtonListener(currencyBtn, SelectCurrencies);
        AddButtonListener(playerItemsBtn, SelectPlayerItems);
        AddButtonListener(mapBtn, SelectMaps);
        AddButtonListener(skinBtn, SelectSkins);

        UITextTranslations();

        // default page
        SelectPage(MarketPages.Currencies);
        InitalsNomadPacks();
    }

    private void OnDisable()
    {
        RemoveButtonListener(backButton, BackAction);
        RemoveButtonListener(currencyBtn, SelectCurrencies);
        RemoveButtonListener(playerItemsBtn, SelectPlayerItems);
        RemoveButtonListener(mapBtn, SelectMaps);
        RemoveButtonListener(skinBtn, SelectSkins);
        DisableNomadicPacks();
        KillPackTweens();
        _isAnimating = false;
    }

    private void SelectPage(MarketPages page)
    {
        int idx = (int)page;

        _currentIndexPage = idx;

        int count = Mathf.Max(clickedObjs != null ? clickedObjs.Length : 0, pages != null ? pages.Length : 0);
        for (int i = 0; i < count; i++)
        {
            bool on = (i == idx);

            SetActiveIfNeeded(GetItem(clickedObjs, i), on);
            SetActiveIfNeeded(GetItem(pages, i), on);
        }

        RefreshTabTextColors(idx);
    }

    private void SelectCurrencies()
    {
        SelectPage(MarketPages.Currencies);
    }

    private void SelectPlayerItems()
    {
        SelectPage(MarketPages.PlayerItems);
    }

    private void SelectMaps()
    {
        SelectPage(MarketPages.Maps);
    }

    private void SelectSkins()
    {
        SelectPage(MarketPages.Skins);
    }

    private void UITextTranslations()
    {
        var language = LanguageManager.Instance;
        if (language == null) return;
        SetText(backBtnText, language.GetText(362));
        SetText(titleText, language.GetText(25));
        SetText(currencyText, language.GetText(412));
        SetText(playerItemsText, language.GetText(386));
        SetText(mapText, language.GetText(414));
        SetText(skinText, language.GetText(415));
    }

    private void RefreshTabTextColors(int selectedIndex)
    {
        SetTextColor(currencyText, selectedIndex == (int)MarketPages.Currencies);
        SetTextColor(playerItemsText, selectedIndex == (int)MarketPages.PlayerItems);
        SetTextColor(mapText, selectedIndex == (int)MarketPages.Maps);
        SetTextColor(skinText, selectedIndex == (int)MarketPages.Skins);
    }

    private void BackAction()
    {
        if (HomeMainUI.Instance != null)
            HomeMainUI.Instance.HideUI(this);
    }

    #region Nomadic Packs
    private void InitalsNomadPacks()
    {
        AddButtonListener(prevButton, Prev);
        AddButtonListener(nextButton, Next);

        ShowImmediate(0);
        RefreshButtons(); 
    }
    private void DisableNomadicPacks()
    {
        RemoveButtonListener(prevButton, Prev);
        RemoveButtonListener(nextButton, Next);
    }
    private void Next()
    {
        if (_isAnimating) return;
        if (packs == null || _currentIndex >= packs.Length - 1) return;

        AnimateTo(_currentIndex + 1, dir: +1);
    }

    private void Prev()
    {
        if (_isAnimating) return;
        if (packs == null) return;
        if (_currentIndex <= 0) return;

        AnimateTo(_currentIndex - 1, dir: -1);
    }

    private void AnimateTo(int newIndex, int dir)
    {
        if (packs == null || newIndex < 0 || newIndex >= packs.Length) return;
        if (_currentIndex < 0 || _currentIndex >= packs.Length) return;
        if (packs[_currentIndex] == null || packs[newIndex] == null) return;

        _isAnimating = true;
        SetButtons(false);

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
        if (packs == null || packs.Length == 0)
        {
            _currentIndex = 0;
            return;
        }

        index = Mathf.Clamp(index, 0, packs.Length - 1);
        for (int i = 0; i < packs.Length; i++)
        {
            if (packs[i] == null) continue;

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
        bool hasPacks = packs != null && packs.Length > 0;
        if (prevButton != null)
            prevButton.interactable = hasPacks && _currentIndex > 0;
        if (nextButton != null)
            nextButton.interactable = hasPacks && _currentIndex < packs.Length - 1;
    }

    private void SetButtons(bool value)
    {
        bool hasPacks = packs != null && packs.Length > 0;
        if (prevButton != null)
            prevButton.interactable = value && hasPacks && _currentIndex > 0;
        if (nextButton != null)
            nextButton.interactable = value && hasPacks && _currentIndex < packs.Length - 1;
    }

    private void KillPackTweens()
    {
        if (packs == null) return;

        foreach (var pack in packs)
        {
            if (pack != null)
                pack.DOKill();
        }
    }
    #endregion

    private static GameObject GetItem(GameObject[] items, int index)
    {
        return items != null && index >= 0 && index < items.Length ? items[index] : null;
    }

    private static void SetActiveIfNeeded(GameObject obj, bool value)
    {
        if (obj != null && obj.activeSelf != value)
            obj.SetActive(value);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private void SetTextColor(TMP_Text text, bool selected)
    {
        if (text != null)
            text.color = selected ? selectedTabTextColor : unselectedTabTextColor;
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }
}

