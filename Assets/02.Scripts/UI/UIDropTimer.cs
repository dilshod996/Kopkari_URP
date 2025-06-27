using MalbersAnimations.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDropTimer : MonoBehaviour
{
    public MEvent DropEvent;
    public TMP_Text timerText; // Unity UI Text elementi
    private float countdown = 30f; // 30 sekundlik taymer
    private bool eventTriggered = false;
    private bool isCounting = false;

    void OnEnable()
    {
        // Taymer 30 sekundga o'rnatiladi
        countdown = 30f;

        // Rangni hex formatdan o'qish (0C2335)
        Color newColor;
        if (ColorUtility.TryParseHtmlString("#0C2335", out newColor))
        {
            timerText.color = newColor;
        }

        // Taymer matnini yangilash (misol uchun: "00:30")
        timerText.text = "00:" + Mathf.Ceil(countdown).ToString("00");
    }

    void Update()
    {
        if (isCounting)
        {
            if (countdown > 0)
            {
                countdown -= Time.deltaTime;


                timerText.text = "00:" + Mathf.Ceil(countdown).ToString("00");

                if (countdown <= 3)
                {
                    timerText.color = Color.red;
                }
            }
            else if (!eventTriggered)
            {
                TriggerEvent();
                eventTriggered = false;
                isCounting = false; // Taymer to'xtaydi
            }
        }
    }

    public void StartTimer()
    {
        isCounting = true;
    }
    void TriggerEvent()
    {
        // Taymer tugaganda bo'ladigan event
        Debug.Log("Taymer tugadi! Event ishga tushdi!");
        // Qo'shimcha event kodlari shu yerga yoziladi
        DropEvent.Invoke();
    }
}
