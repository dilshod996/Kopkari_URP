using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapShowPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text mapName;
    [SerializeField] private Image mapImage;
    [SerializeField] private TMP_Text mapDetails;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Transilation Texts")]
    [SerializeField] private TMP_Text buyBtnText;
    [SerializeField] private TMP_Text cancelBtnText;

    [SerializeField] private GameObject notEnoughObj;
    [SerializeField] private TMP_Text notEnoughQorak;
    [SerializeField] private Button moveQorakPage;
    [SerializeField] private TMP_Text moveQorakPageBtnText;

    public MapCard.MapType mapType;
    [SerializeField] private float animDuration = 3;
    private int mapCost;
    private string mapConstantName;
    private Tween pulseTween;
    private void OnEnable()
    {
        cancelBtn.onClick.AddListener(ClosePopup);
        buyBtn.onClick.AddListener(BuyMap);
        if (notEnoughObj.activeSelf)
        {
            notEnoughObj.SetActive(false);
        }
        UITransilations();
        moveQorakPage.onClick.AddListener(MoveQorakPage);
    }
    private void OnDisable()
    {
        cancelBtn.onClick.RemoveListener(ClosePopup);
        buyBtn.onClick.RemoveListener(BuyMap);
        moveQorakPage.onClick.RemoveListener(MoveQorakPage);
    }
    private void UITransilations()
    {
        if (LanguageManager.Instance != null)
        {
            cancelBtnText.text = LanguageManager.Instance.GetText(362);
            notEnoughQorak.text = LanguageManager.Instance.GetText(406);
            moveQorakPageBtnText.text = LanguageManager.Instance.GetText(390);
        }
    }
    public void SetMapData(string mapname, Sprite mapSprite, int cost, string mapdetails, string mapConstantname, MapCard.MapType type)
    {
        mapName.text = mapname;
        mapImage.sprite = mapSprite;
        buyBtnText.text = cost.ToString();
        mapDetails.text= mapdetails;
        mapCost = cost;
        mapConstantName = mapConstantname;
        mapType = type;
    }
    public void ClosePopup()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void BuyMap()
    {
        int getQorak = PlayerPrefs.GetInt(Constants.Coins.Coin);
        if (mapCost > getQorak)
        {
            //money not enough
            MoneyNotEnoughText();
        }
        else 
        {
            //buy map
            CloseOpenPages();
            getQorak -= mapCost;
            PlayerPrefs.SetInt(Constants.Coins.Coin, getQorak);
            HomeMainUI.Instance.DisplayAutoReward(mapImage.sprite, LanguageManager.Instance.GetText(409), LanguageManager.Instance.GetText(405), mapName.text);
            PlayerPrefs.SetInt(mapConstantName, 1);
        }
    }
    private void MoveQorakPage()
    {
        HomeMainUI.Instance.QorakClicked();
        CloseOpenPages();
    }
    private void CloseOpenPages()
    {
        this.gameObject.SetActive(false);
        if (mapType == MapCard.MapType.Racing)
        {
            HomeMainUI.Instance.CloseRacingField();
        }
        else
        {
            HomeMainUI.Instance.CloseKopkariFeld();
        }
    }
    private void MoneyNotEnoughText()
    {
        notEnoughObj.SetActive(true);

        RectTransform rt = notEnoughObj.GetComponent<RectTransform>();
        CanvasGroup cg = notEnoughObj.GetComponent<CanvasGroup>();

        // Old tweensni to¡®xtatish
        rt.DOKill();
        cg.DOKill();
        pulseTween?.Kill();

        // Reset
        rt.localScale = Vector3.one * 0.9f;
        cg.alpha = 0f;

        // Paydo bo¡®lish anim
        cg.DOFade(1f, 0.15f).SetEase(Ease.OutQuad);
        rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        // 4 sekund davomida yengil pulse
        pulseTween = rt
            .DOScale(1.03f, 0.9f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 4 sekunddan keyin pulse to¡®xtaydi, lekin UI qoladi
        DOVirtual.DelayedCall(animDuration, () =>
        {
            pulseTween?.Kill();
            rt.localScale = Vector3.one; // final holat
        }).SetTarget(this);
    }
}
