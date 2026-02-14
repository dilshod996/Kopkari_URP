using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;

public class AvatarCustomUIManager : MonoBehaviour
{
    #region Main Pages (Player / Horse)
    [Header("Main Pages (Player / Horse)")]
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
    [SerializeField] private Button saveBtn;
    [SerializeField] private Button playerButton;
    [SerializeField] private Button horseButton;

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
    [SerializeField] CanvasGroup bottomCanvasGroup;
    [SerializeField] RectTransform topObj;
   // [SerializeField] GameObject startingPage;

    public static event Action OnSavedBtnClicked;
    private bool isBottomTopShowed=false;

    [SerializeField] private RightPopup rightPopup;
    private void Awake()
    {
        SetPanelX(closeX);
        SetCanvasVisible(false);
        GetSetCoins();
        //if(!startingPage.activeSelf)
        //    startingPage.SetActive(true);
    }
    private void OnEnable()
    {
        OptionItemUI.OnCoinUpdated += GetSetCoins;
        //AvatarCustomManager.OnAllSet += RemovStartPage;
        OptionItemUI.OnNotEnoughCoins += MoneyNotEnough;
    }
    private void OnDestroy()
    {
        OptionItemUI.OnCoinUpdated -= GetSetCoins;
        //AvatarCustomManager.OnAllSet -= RemovStartPage;
        OptionItemUI.OnNotEnoughCoins -= MoneyNotEnough;
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
        int coin = PlayerPrefs.GetInt(Constants.Coins.Coin);
        int nyufiy = PlayerPrefs.GetInt(Constants.Coins.Nyufiy);
        nyufiyText.text = nyufiy > 0 ? $"{nyufiy:N0}" : "0";
        coinText.text = coin > 0 ? $"{coin:N0}" : "0";
    }
    private void StartingDetails()
    {
        PlayMoveY(bottomObj, fromY:-50, toY:37, t:0.5f,stayTime:5f, cg:bottomCanvasGroup);
        PlayMoveY(topObj, fromY: 70, toY: -50, t: 1f);
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

    private void SaveSkins()
    {
        OnSavedBtnClicked?.Invoke();
    }
    private void RemovStartPage()
    {
        //startingPage.SetActive(false);
    }

    #region Right Popup Detials
    private void MoneyNotEnough()
    {
        rightPopup.ShowRightPopup("Coin is not enough", coinImage.sprite);
    }
    #endregion
}
