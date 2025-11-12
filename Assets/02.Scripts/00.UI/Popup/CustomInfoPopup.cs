using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomInfoPopup : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    [Header("IconAndName")]
    [SerializeField] private Image iconCostum;
    [SerializeField] private TMP_Text nameCostum;

    [Header("InfoMainOrSecondary")]
    [SerializeField] private GameObject mainInfoPage;
    [SerializeField] private GameObject secondaryInfoPage;
    [SerializeField] private TMP_Text mainInfoText;

    [Header("Popup DetailsText")]
    [SerializeField] private TMP_Text firstText;
    [SerializeField] private TMP_Text secondText;
    [SerializeField] private TMP_Text thirdText;

    [Header("ChoparDetails")]
    [SerializeField] private TMP_Text catchTime;
    [SerializeField] private TMP_Text strength;
    [SerializeField] private TMP_Text power;

    [Header("InfoButtons")]
    [SerializeField] private GameObject ButtonsParent;
    [SerializeField] private GameObject buyButtonSection;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [SerializeField] private GameObject equipButtonSection;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;

    public float timer = 3f;
    public bool enableTimer = true;

    [SerializeField] private ModalWindowManager modalWindowManager;
    void Start()
    {
        if(animator == null)
            animator = GetComponent<Animator>();
        buyButton.onClick.AddListener(() => { ShowWorkMessage(); });
    }


    #region ChoparData

    /// <summary>
    /// Bu Chopar uchun ishlatiladi
    /// </summary>
    /// <param name="chopar"></param>
    public void SetChoparData(ChoparData chopar)
    {
        OpenNotification();
        if (chopar.isEquipped.Equals(1))
        {
            ButtonsParent.SetActive(false);
        }
        else
        {
            ButtonsParent.SetActive(true);
            if (chopar.isOpen.Equals(0))
            {
                buyButtonSection.SetActive(true);
                equipButtonSection.SetActive(false);
                buyButtonText.text = chopar.cost.ToString();
            }
            else
            {
                buyButtonSection.SetActive(false);
                equipButtonSection.SetActive(true);
                equipButtonText.text = "Foydalanish";
            }
        }
        iconCostum.sprite = chopar.iconName;
        nameCostum.text = chopar.name;
        firstText.text = "Uloq ushlash: ";
        secondText.text = "Quvvat: ";
        thirdText.text = "Kuch: ";
        catchTime.text = chopar.catchTime.ToString() + "s";
        strength.text = chopar.strength.ToString() +"/100";
        power.text = chopar.power.ToString() + "/100";
        buyButtonText.text = chopar.cost.ToString() + " so'm";

    }
    /// <summary>
    /// Bu ChoparPart uchun ishlatiladi
    /// </summary>
    /// <param name="choparParts"></param>
    public void SetChoparDetails(ChoparParts choparParts)
    {
        OpenNotification();
        if (choparParts.isEquipped.Equals(1))
        {
            ButtonsParent.SetActive(false);
        }
        else
        {
            ButtonsParent.SetActive(true);
            if (choparParts.isOpen.Equals(0))
            {
                buyButtonSection.SetActive(true);
                equipButtonSection.SetActive(false);
                buyButtonText.text = choparParts.cost.ToString();
            }
            else
            {
                buyButtonSection.SetActive(false);
                equipButtonSection.SetActive(true);
                equipButtonText.text = "Foydalanish";
            }
        }
        iconCostum.sprite = choparParts.iconName;
        nameCostum.text = choparParts.name;
        firstText.text = "Hayot: ";
        secondText.text = "Himoya: " ;
        thirdText.text = "Og'irligi: ";
        catchTime.text = choparParts.life.ToString() + "+ kun";
        strength.text = choparParts.defend.ToString() + "%";
        power.text = choparParts.height.ToString() + "kg";
        buyButtonText.text = choparParts.cost.ToString() + " so'm";
    }
    #endregion

    #region HorseData

    public void SetHorseData(HorseData horseData)
    {
        OpenNotification();
        if (horseData.isEquipped.Equals(1))
        {
            ButtonsParent.SetActive(false);
        }
        else
        {
            ButtonsParent.SetActive(true);
            if (horseData.isOpen.Equals(0))
            {
                buyButtonSection.SetActive(true);
                equipButtonSection.SetActive(false);
                buyButtonText.text = horseData.cost.ToString();
            }
            else
            {
                buyButtonSection.SetActive(false);
                equipButtonSection.SetActive(true);
                equipButtonText.text = "Foydalanish";
            }
        }
        iconCostum.sprite = horseData.iconName;
        nameCostum.text = horseData.name;
        firstText.text = "Tezlik vaqti: ";
        secondText.text = "Tezlik: ";
        thirdText.text = "Energiya: ";
        catchTime.text = horseData.boostSpeedTime.ToString() + "s";
        strength.text = horseData.speed.ToString() + "/100";
        power.text = horseData.energy.ToString() + "/100";
        buyButtonText.text = horseData.cost.ToString() + " so'm";
    }
    public void SetHorseDetails(HorseParts horsePartsData)
    {
        OpenNotification();
        if (horsePartsData.isEquipped.Equals(1))
        {
            ButtonsParent.SetActive(false);
        }
        else
        {
            ButtonsParent.SetActive(true);
            if (horsePartsData.isOpen.Equals(0))
            {
                buyButtonSection.SetActive(true);
                equipButtonSection.SetActive(false);
                buyButtonText.text = horsePartsData.cost.ToString();
            }
            else
            {
                buyButtonSection.SetActive(false);
                equipButtonSection.SetActive(true);
                equipButtonText.text = "Foydalanish";
            }
        }
        iconCostum.sprite = horsePartsData.iconName;
        nameCostum.text = horsePartsData.name;
        firstText.text = "Hayot: ";
        secondText.text = "Himoya: ";
        thirdText.text = "Og'irligi: ";
        catchTime.text = horsePartsData.life.ToString() + " kun";
        strength.text = horsePartsData.defend.ToString() + "%";
        power.text = horsePartsData.weight.ToString() + " kg";
        buyButtonText.text = horsePartsData.cost.ToString() + " so'm";
    }
    #endregion

    #region Popup Enable & Disable
    public void OpenNotification()
    {
        StopCoroutine("StartTimer");
        animator.Play("In");
        if (enableTimer == true)
            StartCoroutine("StartTimer");

    }
    public void CloseNotification()
    {
        animator.Play("Out");
    }

    IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(timer);
        CloseNotification();
    }
    private void ShowWorkMessage()
    {
        modalWindowManager.UpdateUICustom("Jarayonda", "Hali ish jarayonida ekan. Iltimos kuting...");
    }
    #endregion
}
