using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIStore : MonoBehaviour
{
    private const int MarketPageCount = 4;

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
    private Tween _packTween;

    private void OnEnable()
    {
        ValidateConfiguration();

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
        if (idx < 0 || idx >= MarketPageCount)
        {
            Debug.LogWarning($"Cannot select invalid market page index {idx}.", this);
            return;
        }

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
        {
            HomeMainUI.Instance.HideUI(this);
            return;
        }

        // The store may also be previewed or loaded without the Home controller.
        gameObject.SetActive(false);
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

        int nextIndex = FindPackIndex(_currentIndex + 1, 1);
        if (nextIndex >= 0)
            AnimateTo(nextIndex, dir: +1);
    }

    private void Prev()
    {
        if (_isAnimating) return;

        int previousIndex = FindPackIndex(_currentIndex - 1, -1);
        if (previousIndex >= 0)
            AnimateTo(previousIndex, dir: -1);
    }

    private void AnimateTo(int newIndex, int dir)
    {
        if (!IsValidPackIndex(newIndex) || !IsValidPackIndex(_currentIndex))
        {
            _isAnimating = false;
            RefreshButtons();
            return;
        }

        _isAnimating = true;
        SetButtons(false);

        RectTransform current = packs[_currentIndex];
        current.DOKill();
        current.gameObject.SetActive(false);

        RectTransform next = packs[newIndex];
        next.DOKill();
        next.gameObject.SetActive(true);

        float targetY = next.anchoredPosition.y;
        next.anchoredPosition = new Vector2(dir * Mathf.Abs(moveX), targetY);

        _packTween = next.DOAnchorPosX(0f, Mathf.Max(0f, duration))
            .SetEase(ease)
            .OnComplete(() =>
            {
                _currentIndex = newIndex;
                _isAnimating = false;
                _packTween = null;
                RefreshButtons();
            });
    }

    private void ShowImmediate(int index)
    {
        int validIndex = FindNearestPackIndex(index);
        if (validIndex < 0)
        {
            _currentIndex = 0;
            _isAnimating = false;
            RefreshButtons();
            return;
        }

        for (int i = 0; i < packs.Length; i++)
        {
            if (packs[i] == null) continue;

            bool on = (i == validIndex);
            packs[i].gameObject.SetActive(on);

            if (on)
            {
                packs[i].DOKill();
                packs[i].anchoredPosition = Vector2.zero;
            }
        }

        _currentIndex = validIndex;
        _isAnimating = false;
    }

    private void RefreshButtons()
    {
        if (prevButton != null)
            prevButton.interactable = !_isAnimating && FindPackIndex(_currentIndex - 1, -1) >= 0;
        if (nextButton != null)
            nextButton.interactable = !_isAnimating && FindPackIndex(_currentIndex + 1, 1) >= 0;
    }

    private void SetButtons(bool value)
    {
        if (prevButton != null)
            prevButton.interactable = value && FindPackIndex(_currentIndex - 1, -1) >= 0;
        if (nextButton != null)
            nextButton.interactable = value && FindPackIndex(_currentIndex + 1, 1) >= 0;
    }

    private void KillPackTweens()
    {
        _packTween?.Kill();
        _packTween = null;

        if (packs == null) return;

        foreach (var pack in packs)
        {
            if (pack != null)
                pack.DOKill();
        }
    }

    private bool IsValidPackIndex(int index)
    {
        return packs != null
            && index >= 0
            && index < packs.Length
            && packs[index] != null;
    }

    private int FindPackIndex(int startIndex, int step)
    {
        if (packs == null || packs.Length == 0 || step == 0)
            return -1;

        for (int i = startIndex; i >= 0 && i < packs.Length; i += step)
        {
            if (packs[i] != null)
                return i;
        }

        return -1;
    }

    private int FindNearestPackIndex(int preferredIndex)
    {
        if (packs == null || packs.Length == 0)
            return -1;

        int clampedIndex = Mathf.Clamp(preferredIndex, 0, packs.Length - 1);
        if (packs[clampedIndex] != null)
            return clampedIndex;

        int nextIndex = FindPackIndex(clampedIndex + 1, 1);
        return nextIndex >= 0 ? nextIndex : FindPackIndex(clampedIndex - 1, -1);
    }
    #endregion

    private void ValidateConfiguration()
    {
        WarnIfMissing(titleText, nameof(titleText));
        WarnIfMissing(backBtnText, nameof(backBtnText));
        WarnIfMissing(currencyText, nameof(currencyText));
        WarnIfMissing(playerItemsText, nameof(playerItemsText));
        WarnIfMissing(mapText, nameof(mapText));
        WarnIfMissing(skinText, nameof(skinText));

        WarnIfMissing(backButton, nameof(backButton));
        WarnIfMissing(currencyBtn, nameof(currencyBtn));
        WarnIfMissing(playerItemsBtn, nameof(playerItemsBtn));
        WarnIfMissing(mapBtn, nameof(mapBtn));
        WarnIfMissing(skinBtn, nameof(skinBtn));
        WarnIfMissing(prevButton, nameof(prevButton));
        WarnIfMissing(nextButton, nameof(nextButton));

        if (pages == null || pages.Length < MarketPageCount)
            Debug.LogWarning($"{nameof(UIStore)} requires {MarketPageCount} page entries.", this);
        else
            WarnAboutNullEntries(pages, nameof(pages), MarketPageCount);

        if (clickedObjs == null || clickedObjs.Length < MarketPageCount)
            Debug.LogWarning($"{nameof(UIStore)} requires {MarketPageCount} selected-tab objects.", this);
        else
            WarnAboutNullEntries(clickedObjs, nameof(clickedObjs), MarketPageCount);

        if (packs == null || packs.Length == 0)
            Debug.LogWarning($"{nameof(UIStore)} has no nomadic packs assigned.", this);
        else
            WarnAboutNullEntries(packs, nameof(packs), packs.Length);
    }

    private void WarnIfMissing(Object reference, string fieldName)
    {
        if (reference == null)
            Debug.LogWarning($"{nameof(UIStore)} is missing the '{fieldName}' reference.", this);
    }

    private void WarnAboutNullEntries<T>(T[] items, string fieldName, int count) where T : Object
    {
        int checkedCount = Mathf.Min(items.Length, count);
        for (int i = 0; i < checkedCount; i++)
        {
            if (items[i] == null)
                Debug.LogWarning($"{nameof(UIStore)} has an empty '{fieldName}' entry at index {i}.", this);
        }
    }

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
        if (button == null || action == null) return;

        // Protect against duplicate subscriptions if the object is toggled unusually.
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }
}

