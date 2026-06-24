using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class AvatarCustomUIManager : MonoBehaviour
{
    #region Main Pages (Player / Horse)
    [Header("Main Pages (Player / Horse)")]
    [SerializeField] private TMP_Text player_text;
    [SerializeField] private TMP_Text horse_text;
    [SerializeField] private GameObject[] mainPages;       // 0=PlayerPanel, 1=HorsePanel
    [SerializeField] private GameObject[] clickedObjsMain; // 0=Player btn selected img, 1=Horse btn selected img
    private int _currentMainIndex = -1;
    #endregion

    #region Player Skin Pages (5 types)
    [Header("Player Skin Pages (5 types)")]
    [SerializeField] private GameObject[] playerSkinPages;        // size=5
    [SerializeField] private GameObject[] clickedObjsPlayerSkins; // size=5 (button select images)
    private int _currentPlayerSkinIndex = -1;
    #endregion

    #region Horse Skin Pages
    [Header("Horse Skin Pages")]
    [SerializeField] private GameObject[] horseSkinPages;
    [SerializeField] private GameObject[] clickedObjsHorseSkins;
    private int _currentHorseSkinIndex = -1;
    #endregion

    #region Right Panel (Stretched Right Root)
    [Header("Right Panel (Stretched Right Root)")]
    [SerializeField] private RectTransform rightPanelRoot;
    [SerializeField] private CanvasGroup rightCanvasGroup;

    [Header("Panel Animation")]
    [SerializeField] private float openX = -300f;
    [SerializeField] private float closeX = 500f;
    [SerializeField] private float animDuration = 0.35f;
    [SerializeField] private Ease easePanel = Ease.OutCubic;

    private Tween _panelTween;
    #endregion

    [Header("Buttons (UI)")]
    [SerializeField] private Button backLobby;
    [SerializeField] private TMP_Text backLobbyText;
    [SerializeField] private Button saveBtn;
    [SerializeField] private TMP_Text saveBtnText;
    [SerializeField] private GameObject pendingButtonRoot;
    [SerializeField] private Button pendingButton;
    [SerializeField] private TMP_Text pendingDetailsText;
    [SerializeField] private Button playerButton;
    [SerializeField] private Button horseButton;
    private Tween _pendingButtonTween;

    // Player skin buttons (5)
    [Header("Player Skin Buttons (5)")]
    [SerializeField] private Button[] playerSkinButtons; // size=5 (Hair, Face, Body, Foot, ...)

    // Horse skin buttons (x)
    [Header("Horse Skin Buttons")]
    [SerializeField] private Button[] horseSkinButtons;  // size = horseSkinPages length
    private AvatarCustomManager _gm;
    [Header("Top Details(Coins)")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image coinImage;
    [SerializeField] private TMP_Text nyufiyText;
    [SerializeField] RectTransform bottomObj;
    [SerializeField] TMP_Text bottomText;
    [SerializeField] CanvasGroup bottomCanvasGroup;
    [SerializeField] RectTransform topObj;
   // [SerializeField] GameObject startingPage;

    public static event Action OnSavedBtnClicked;
    public static event Action OnRevertPreviewRequested;
    private bool isBottomTopShowed=false;
    private bool _currencySubscribed;
    private Coroutine _currencyWaitRoutine;

    [SerializeField] private RightPopup rightPopup;
    private void Awake()
    {
        SetPanelX(closeX);
        SetCanvasVisible(false);
        GetSetCoins();
        RefreshPendingDetails();
        //if(!startingPage.activeSelf)
        //    startingPage.SetActive(true);
    }
    private void OnEnable()
    {
        OptionItemUI.OnCoinUpdated += GetSetCoins;
        AvatarCustomizationCart.OnChanged += RefreshPendingDetails;
        //AvatarCustomManager.OnAllSet += RemovStartPage;
        OptionItemUI.OnNotEnoughCoins += MoneyNotEnough;
        SubscribeCurrencyEvents();

        if (!_currencySubscribed)
            _currencyWaitRoutine = StartCoroutine(WaitForCurrencyManager());

        RefreshPendingDetails();
        UITransilations();
    }
    private void OnDisable()
    {
        OptionItemUI.OnCoinUpdated -= GetSetCoins;
        AvatarCustomizationCart.OnChanged -= RefreshPendingDetails;
        //AvatarCustomManager.OnAllSet -= RemovStartPage;
        OptionItemUI.OnNotEnoughCoins -= MoneyNotEnough;

        if (_currencyWaitRoutine != null)
        {
            StopCoroutine(_currencyWaitRoutine);
            _currencyWaitRoutine = null;
        }

        UnsubscribeCurrencyEvents();
        StopPendingButtonAnimation();
    }

    private void UITransilations()
    {
        if (backLobbyText != null)
            backLobbyText.text = LanguageManager.Instance.GetText(362);
        if (saveBtnText != null)
            saveBtnText.text = LanguageManager.Instance.GetText(39);
        if (pendingDetailsText != null)
            pendingDetailsText.text = LanguageManager.Instance.GetText(435);
        if (player_text != null)
            player_text.text = LanguageManager.Instance.GetText(528);
        if (horse_text != null)
            horse_text.text = LanguageManager.Instance.GetText(89);
    }

    #region Button Events
    public void Bind(AvatarCustomManager gm)
    {
        _gm = gm;

        RemoveListeners();   // qayta bind bo'lsa tozalab tashlaydi
        AddListeners();
    }

    private void AddListeners()
    {
        if (_gm == null) return;

        // Main tab buttons
        if (playerButton != null)
            playerButton.onClick.AddListener(() =>
            {
                _gm.GoToSpot(AvatarCustomTypes.CamSpot.Player);
                SelectMainPage(AvatarCustomTypes.MainPages.PlayerPage);
                // xohlasang default hair ham:
                SelectPlayerSkinPage(0);
            });

        if (horseButton != null)
            horseButton.onClick.AddListener(() =>
            {
                _gm.GoToSpot(AvatarCustomTypes.CamSpot.Horse);
                SelectMainPage(AvatarCustomTypes.MainPages.HorsePage);
                SelectHorseSkinPage(0);
            });

        if (backLobby != null)
            backLobby.onClick.AddListener(() => _gm.BackPublic());

        if (saveBtn != null)
            saveBtn.onClick.AddListener(SaveSkins); // agar sende Save yo'q bo'lsa, remove qil

        if (pendingButton != null)
            pendingButton.onClick.AddListener(ShowPendingDetails);

        // Player skin buttons
        if (playerSkinButtons != null)
        {
            for (int i = 0; i < playerSkinButtons.Length; i++)
            {
                int idx = i;
                if (playerSkinButtons[idx] == null) continue;

                playerSkinButtons[idx].onClick.AddListener(() =>
                {
                    SelectPlayerSkinPage(idx);
                });
            }
        }

        // Horse skin buttons
        if (horseSkinButtons != null)
        {
            for (int i = 0; i < horseSkinButtons.Length; i++)
            {
                int idx = i;
                if (horseSkinButtons[idx] == null) continue;

                horseSkinButtons[idx].onClick.AddListener(() =>
                {
                    SelectHorseSkinPage(idx);
                });
            }
        }
    }

    private void RemoveListeners()
    {
        if (playerButton != null) playerButton.onClick.RemoveAllListeners();
        if (horseButton != null) horseButton.onClick.RemoveAllListeners();
        if (backLobby != null) backLobby.onClick.RemoveAllListeners();
        if (saveBtn != null) saveBtn.onClick.RemoveAllListeners();
        if (pendingButton != null) pendingButton.onClick.RemoveAllListeners();

        if (playerSkinButtons != null)
            foreach (var b in playerSkinButtons)
                if (b != null) b.onClick.RemoveAllListeners();

        if (horseSkinButtons != null)
            foreach (var b in horseSkinButtons)
                if (b != null) b.onClick.RemoveAllListeners();
    }
    #endregion

    // -------------------------
    // PUBLIC FLOW (GameManager chaqiradi)
    // -------------------------

    public void OnPlayerArrived()
    {
        // Main -> PlayerPanel
        SelectMainPage(AvatarCustomTypes.MainPages.PlayerPage);

        // Default: PlayerSkins.Hair (index 0 deb olamiz)
        SelectPlayerSkinPage(0);

        OpenRightPanel();
        if (!isBottomTopShowed)
            StartingDetails();
    }

    public void OnHorseArrived()
    {
        // Main -> HorsePanel
        SelectMainPage(AvatarCustomTypes.MainPages.HorsePage);

        // Default: HorseSkins.Hair (index 0)
        SelectHorseSkinPage(0);

        OpenRightPanel();
        if(!isBottomTopShowed)
            StartingDetails();
    }

    // -------------------------
    // MAIN PAGES
    // -------------------------
    public void SelectMainPage(AvatarCustomTypes.MainPages page)
    {
        SelectPageInternal((int)page, ref _currentMainIndex, clickedObjsMain, mainPages);
    }

    // -------------------------
    // PLAYER SKINS
    // -------------------------
    // Agar enum bo'yicha chaqirmoqchi bo'lsang:
    public void SelectPlayerSkinPage(AvatarCustomTypes.PlayerSkins skin)
    {
        SelectPlayerSkinPage((int)skin);
    }

    // Index bo'yicha (Hair=0 default)
    public void SelectPlayerSkinPage(int idx)
    {
        //switch (idx)
        //{
        //    case 1:
        //        _gm.GoToSpot(AvatarCustomTypes.CamSpot.HeadPlayer);
        //        break;
        //    case 2:
        //        _gm.GoToSpot(AvatarCustomTypes.CamSpot.UpperBodyPlayer);
        //        break;
        //    case 3:
        //        _gm.GoToSpot(AvatarCustomTypes.CamSpot.Player);
        //        break;
        //}
        SelectPageInternal(idx, ref _currentPlayerSkinIndex, clickedObjsPlayerSkins, playerSkinPages);
    }

    // -------------------------
    // HORSE SKINS
    // -------------------------
    public void SelectHorseSkinPage(AvatarCustomTypes.HorseSkins skin)
    {
        SelectHorseSkinPage((int)skin);
    }

    public void SelectHorseSkinPage(int idx)
    {
        SelectPageInternal(idx, ref _currentHorseSkinIndex, clickedObjsHorseSkins, horseSkinPages);
    }

    // -------------------------
    // CORE (Store dagi SelectPage pattern)
    // -------------------------
    private void SelectPageInternal(int idx, ref int currentIdx, GameObject[] clickedObjs, GameObject[] pages)
    {
        if (pages == null || clickedObjs == null) return;
        if (idx < 0 || idx >= pages.Length) return;
        if (clickedObjs.Length != pages.Length)
        {
            Debug.LogWarning("❌ clickedObjs.Length va pages.Length teng bo'lishi kerak!");
            return;
        }

        if (currentIdx == idx) return; // qayta bosilsa bekor
        currentIdx = idx;

        for (int i = 0; i < clickedObjs.Length; i++)
        {
            bool on = (i == idx);

            var c = clickedObjs[i];
            if (c != null && c.activeSelf != on) c.SetActive(on);

            var p = pages[i];
            if (p != null && p.activeSelf != on) p.SetActive(on);
        }
    }

    // -------------------------
    // RIGHT PANEL
    // -------------------------
    public void OpenRightPanel()
    {
        _panelTween?.Kill();
        if (rightPanelRoot != null) rightPanelRoot.gameObject.SetActive(true);

        SetCanvasVisible(true);

        if (rightPanelRoot != null)
        {
            _panelTween = rightPanelRoot
                .DOAnchorPosX(openX, animDuration)
                .SetEase(easePanel);
        }
    }

    public void CloseRightPanel()
    {
        _panelTween?.Kill();

        if (rightPanelRoot == null)
        {
            SetCanvasVisible(false);
            return;
        }

        _panelTween = rightPanelRoot
            .DOAnchorPosX(closeX, animDuration)
            .SetEase(easePanel)
            .OnComplete(() => SetCanvasVisible(false));
    }

    private void SetCanvasVisible(bool visible)
    {
        if (rightCanvasGroup == null) return;
        rightCanvasGroup.alpha = visible ? 1f : 0f;
        rightCanvasGroup.interactable = visible;
        rightCanvasGroup.blocksRaycasts = visible;
    }

    private void SetPanelX(float x)
    {
        if (rightPanelRoot == null) return;
        var p = rightPanelRoot.anchoredPosition;
        rightPanelRoot.anchoredPosition = new Vector2(x, p.y);
    }
    #region Coin Details
    private void GetSetCoins()
    {
        int nyufiy = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.Nyufiy
            : PlayerPrefs.GetInt(Constants.Coins.Nyufiy, 0);

        int coin = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.Coin
            : PlayerPrefs.GetInt(Constants.Coins.Coin, 0);

        SetNyufiyText(nyufiy);
        SetCoinText(coin);
    }

    private IEnumerator WaitForCurrencyManager()
    {
        yield return new WaitUntil(() => CurrencyManager.Instance != null);
        _currencyWaitRoutine = null;
        SubscribeCurrencyEvents();
    }

    private void SubscribeCurrencyEvents()
    {
        if (_currencySubscribed || CurrencyManager.Instance == null)
            return;

        CurrencyManager.Instance.OnNyufiyChanged += SetNyufiyText;
        CurrencyManager.Instance.OnCoinChanged += SetCoinText;
        _currencySubscribed = true;
        GetSetCoins();
    }

    private void UnsubscribeCurrencyEvents()
    {
        if (!_currencySubscribed || CurrencyManager.Instance == null)
        {
            _currencySubscribed = false;
            return;
        }

        CurrencyManager.Instance.OnNyufiyChanged -= SetNyufiyText;
        CurrencyManager.Instance.OnCoinChanged -= SetCoinText;
        _currencySubscribed = false;
    }

    private void SetNyufiyText(int amount)
    {
        if (nyufiyText != null)
            nyufiyText.text = $"{amount:N0}";
    }

    private void SetCoinText(int amount)
    {
        if (coinText != null)
            coinText.text = $"{amount:N0}";
    }
    private void StartingDetails()
    {
        PlayMoveY(bottomObj, fromY:-50, toY:37, t:0.5f,stayTime:5f, cg:bottomCanvasGroup);
        PlayMoveY(topObj, fromY: 70, toY: -58, t: 1f);
        isBottomTopShowed = true;
    }

    // ===============================
    // 1️⃣ FAQAT CHIQISH (isBack = false)
    // ===============================
    private Tween PlayMoveY(
        RectTransform rect,
        float fromY,
        float toY,
        float t,
        CanvasGroup cg = null,
        Ease ease = Ease.OutCubic)
    {
        if (rect == null) return null;

        rect.DOKill();
        cg?.DOKill();

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, fromY);
        if (cg != null) cg.alpha = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        seq.Append(rect.DOAnchorPosY(toY, t).SetEase(ease));

        if (cg != null)
            seq.Join(cg.DOFade(1f, t));

        return seq;
    }

    // =========================================
    // 2️⃣ CHIQISH + KUTISH + QAYTISH (isBack=true)
    // =========================================
    private Tween PlayMoveY(
        RectTransform rect,
        float fromY,
        float toY,
        float t,
        float stayTime,
        CanvasGroup cg = null,
        Ease ease = Ease.OutCubic)
    {
        if (rect == null) return null;

        rect.DOKill();
        cg?.DOKill();

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, fromY);
        if (cg != null) cg.alpha = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // chiqish
        seq.Append(rect.DOAnchorPosY(toY, t).SetEase(ease));
        if (cg != null) seq.Join(cg.DOFade(1f, t));

        // kutish
        if (stayTime > 0f)
            seq.AppendInterval(stayTime);

        // qaytish
        seq.Append(rect.DOAnchorPosY(fromY, t).SetEase(Ease.InCubic));
        if (cg != null) seq.Join(cg.DOFade(0f, t));

        return seq;
    }
    #endregion

    public static void RevertPendingPreviews()
    {
        OnRevertPreviewRequested?.Invoke();
        AvatarCustomizationCart.Clear();
    }

    private void SaveSkins()
    {
        if (!AvatarCustomizationCart.HasPending)
        {
            OnSavedBtnClicked?.Invoke();
            return;
        }

        int totalCost = AvatarCustomizationCart.GetTotalLockedCost();
        if (totalCost <= 0)
        {
            CommitPendingPreviews();
            return;
        }

        ShowCheckoutPopup(totalCost);
    }

    private void ShowPendingDetails()
    {
        if (!AvatarCustomizationCart.HasPending)
        {
            RefreshPendingDetails();
            return;
        }

        ShowCheckoutPopup(AvatarCustomizationCart.GetTotalLockedCost());
    }

    private void RefreshPendingDetails()
    {
        bool hasPending = AvatarCustomizationCart.HasPending;

        if (pendingButtonRoot != null)
            pendingButtonRoot.SetActive(hasPending);
        else if (pendingButton != null)
            pendingButton.gameObject.SetActive(hasPending);

        if (!hasPending)
        {
            StopPendingButtonAnimation();
            if (pendingDetailsText != null)
                pendingDetailsText.text = "";
            return;
        }

        int totalCost = AvatarCustomizationCart.GetTotalLockedCost();
        if (pendingDetailsText != null)
            pendingDetailsText.text = $"{LanguageManager.Instance.GetText(526)}: {totalCost}";
        SetPendingButtonAnimation(totalCost > 0);
    }

    private void SetPendingButtonAnimation(bool active)
    {
        Transform target = pendingButtonRoot != null
            ? pendingButtonRoot.transform
            : pendingButton != null ? pendingButton.transform : null;

        if (target == null)
            return;

        if (!active)
        {
            StopPendingButtonAnimation();
            return;
        }

        if (_pendingButtonTween != null && _pendingButtonTween.IsActive())
            return;

        target.localScale = Vector3.one;
        _pendingButtonTween = target
            .DOScale(1.08f, 0.45f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPendingButtonAnimation()
    {
        _pendingButtonTween?.Kill(false);
        _pendingButtonTween = null;

        Transform target = pendingButtonRoot != null
            ? pendingButtonRoot.transform
            : pendingButton != null ? pendingButton.transform : null;

        if (target != null)
            target.localScale = Vector3.one;
    }

    private void ShowCheckoutPopup(int totalCost)
    {
        int titleId = 522;
        int messageId = totalCost > 0 ? 523 : 524;
        int okTextId = totalCost > 0 ? 525 : 39;
        int cancelTextId = 2;

        if (UIOverlayRoot.I == null)
        {
            TryPayAndCommit(totalCost);
            return;
        }

        string title = LanguageManager.Instance.GetText(titleId);

        string message = totalCost > 0
            ? LanguageManager.Instance.GetText(messageId, totalCost)
            : LanguageManager.Instance.GetText(messageId);

        string okText = LanguageManager.Instance.GetText(okTextId);
        string cancelText = LanguageManager.Instance.GetText(cancelTextId);

        UIOverlayRoot.I.Confirm(
            title,
            message,
            okText,
            cancelText,
            onOk: () => TryPayAndCommit(totalCost),
            onCancel: DiscardPendingPreviews
        );
    }
    private void TryPayAndCommit(int totalCost)
    {
        if (totalCost > 0)
        {
            CurrencyManager currency = CurrencyManager.Instance;
            if (currency == null || !currency.SpendCoin(totalCost, true))
            {
                HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
                MoneyNotEnough();
                return;
            }
        }

        GetSetCoins();
        CommitPendingPreviews();
    }

    private void CommitPendingPreviews()
    {
        AvatarCustomizationCart.UnlockPendingItems();
        OnSavedBtnClicked?.Invoke();
        AvatarCustomizationCart.Clear();
        GetSetCoins();
        RefreshPendingDetails();
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
    }

    private void DiscardPendingPreviews()
    {
        RevertPendingPreviews();
    }
    private void RemovStartPage()
    {
        //startingPage.SetActive(false);
    }

    #region Right Popup Detials
    private void MoneyNotEnough()
    {
        string message = LanguageManager.Instance.GetText(527);
        rightPopup.ShowRightPopup(message, coinImage.sprite);
    }
    #endregion
}
