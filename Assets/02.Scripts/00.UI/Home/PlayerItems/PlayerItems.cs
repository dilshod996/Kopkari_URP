using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItems : MonoBehaviour
{
    //[SerializeField] private Button horseNutritionPage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAddButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountTitleText;
    [SerializeField] private TMP_Text moneyNotEnoughText;
    [SerializeField] private TMP_Text backText;
    //[SerializeField] private TMP_Text horseNutritionText;
    [SerializeField] private GameObject bottomMoneyNotEnoughtObj;

    private void OnEnable()
    {
        UIText();
        closeButton.onClick.AddListener(ClosePage);
        //horseNutritionPage.onClick.AddListener(OpenHorseNutritionPage);
        PlayerResourse.OnMoneyNotEnough += MoneyNotEnoughText;
    }
    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(ClosePage);
        //horseNutritionPage.onClick.RemoveListener(OpenHorseNutritionPage);
        PlayerResourse.OnMoneyNotEnough -= MoneyNotEnoughText;
    }
    private void UIText()
    {
        if (bottomMoneyNotEnoughtObj.activeSelf)
        {
            bottomMoneyNotEnoughtObj.SetActive(false);
        }
        if(LanguageManager.Instance != null)
        {
            titleText.text = LanguageManager.Instance.GetText(386);
            backText.text = LanguageManager.Instance.GetText(362);
           // horseNutritionText.text = LanguageManager.Instance.GetText(395);
            amountTitleText.text = LanguageManager.Instance.GetText(220);
        }
    }
    private void ClosePage()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OpenHorseNutritionPage()
    {
        HomeMainUI.Instance.SHowFoodPanel();
        if (this.gameObject.activeSelf)
        {
            ClosePage();
        }
    }
    private void MoneyNotEnoughText()
    {
        if(!bottomMoneyNotEnoughtObj.activeSelf)
        {
            bottomMoneyNotEnoughtObj.SetActive(true);
            moneyNotEnoughText.text = LanguageManager.Instance.GetText(363);
        }
    }
}
