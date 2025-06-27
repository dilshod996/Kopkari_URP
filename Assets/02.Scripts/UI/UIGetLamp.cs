using MalbersAnimations.Controller;
using MalbersAnimations.Events;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static BaseManager;

public class UIGetLamp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ProgressBar holdSlider;
    [SerializeField] private PlayerDataManager playerData;

    public bool isHolding = false;
    private float holdTime = 5f;

    private List<string> uloqMessages = new List<string>
    {
        "Uloqni mahkam ushla, qo¡®lingdan chiqib ketmasin!",
        //"Raqiblaring juda yaqin! Ko¡®proq kuch sarflang!",
        "Sizning ot kuchingiz yetarlimi? Hali ko¡®ramiz!",
        "Uloqni mahkam tort, raqiblar senga qarab yugurmoqda!",
        "Faqat eng mard chavandoz uloqni ko¡®tara oladi!",
        "Hamma ko¡®zlar sizda! G¡®alaba sizga bog¡®liq!",
        "Uloqni olib chiqish oson emas! Kuching yetadimi?",
        //"Otingni qattiq hayda! Hali jang tugagani yo¡®q!",
        "Qattiq tur! Uloq hozircha sening qo¡®lingda!",
       // "Kurash avjida! Qolganlari ham uloq uchun jon kuydiryapti!",
        "G¡®alabaga bir qadam qoldi! Bardam bo¡®ling!",
        "Bu sening imkoniyating! Uloqni ko¡®tar va maydondan olib chiq!",
        "Hamma kuchini ishga sol! Raqiblar juda yaqinlashdi!",
        "Boshqalardan oldin uloqni olib, o¡®zingni ko¡®rsat!",
        "O¡®zingni yo¡®qotma, uzoqni o¡®yla va harakatni to¡®g¡®ri tanla!"
    };
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("pressed");          
        isHolding = true;
        holdSlider.gameObject.SetActive(isHolding);
        StartCoroutine(HoldCoroutine());

    }
    private void Update()
    {
        if (!isHolding && this.gameObject.activeSelf)
        {
            holdSlider.gameObject.SetActive(false);
        }
    }
    private void OnDisable()
    {
        if (isHolding)
        {
            isHolding=false;
            holdSlider.gameObject.SetActive(false);
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {

        isHolding = false; // Avval isHolding ni false qilish
        StopCoroutine(HoldCoroutine()); // Korrutinani to¡®xtatish

        holdSlider.currentPercent = 0;
        holdSlider.gameObject.SetActive(isHolding);
  

    }

    private IEnumerator HoldCoroutine()
    {
        float timer = 0f;
        
        //holdSlider.currentPercent = 0;
        Debug.Log("Time: " + timer);
        BaseManager.Instance.CurrentCondition = PlayerCondition.GettingTarget;

        while (isHolding && timer < holdTime)
        {
            timer += Time.deltaTime;
            holdSlider.currentPercent = (timer / holdTime) * 100;
            yield return null;
        }

        if (timer >= holdTime)
        {
            PerformAction();
        }
    }
    private IEnumerator ResetSlider()
    {
        while (holdSlider.currentPercent > 0)
        {
            holdSlider.currentPercent -= (100 / holdTime) * Time.deltaTime;
            yield return null;
        }
    }

    private void PerformAction()
    {
        BaseManager.Instance.LambOwner = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        playerData.PickupObj();
        //BaseManager.Instance.CurrentCondition = PlayerCondition.GotTarget;
        //if(AIGameRoom.Instance!=null)
        //    AIGameRoom.Instance.LambOwner = "dima"; //DataManager.Instance.PlayerName;
        if (holdSlider.gameObject.activeSelf)
        {
            holdSlider.gameObject.SetActive(false);
        }
    }
}
