using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HorseDetails : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text playerSuppliesText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button playerSuppliesButton;
    [SerializeField] private GameObject MoneyNotEnoghObj;
    [SerializeField] private TMP_Text moneyNotEnoughText;
    [SerializeField] private Button watchAddBtn;
    private void OnEnable()
    {
        TextTransilations();
        backButton.onClick.AddListener(ClosePage);
        playerSuppliesButton.onClick.AddListener(OpenPlayerSuppliesPage);
        FoodInfo.OnMoneyNotEnough += OpenNotMoney;
        MoneyNotEnoghObj?.SetActive(false);
    }
    private void OnDisable()
    {
        backButton.onClick.RemoveListener(ClosePage);
        playerSuppliesButton.onClick.RemoveListener(OpenPlayerSuppliesPage);
        FoodInfo.OnMoneyNotEnough -= OpenNotMoney;
    }
    private void TextTransilations()
    {
        if(LanguageManager.Instance != null)
        {
            title.text = LanguageManager.Instance.GetText(395);
            backText.text = LanguageManager.Instance.GetText(362);
            playerSuppliesText.text = LanguageManager.Instance.GetText(386);
        }
    }
    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OpenPlayerSuppliesPage()
    {
        HomeMainUI.Instance.ShowSuppliesPanel();
        if (this.gameObject.activeSelf)
        {
            ClosePage();
        }
    }
    private void OpenNotMoney()
    {
        if (MoneyNotEnoghObj != null)
        {
            MoneyNotEnoghObj.SetActive(true);
            moneyNotEnoughText.text = LanguageManager.Instance.GetText(333);
        }
    }
}
