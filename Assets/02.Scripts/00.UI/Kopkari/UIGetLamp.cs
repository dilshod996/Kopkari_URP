using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UIGetLamp : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage; // Image: Filled Vertical, origin Bottom
    [SerializeField] private GameObject fillImgBg;

    [Header("Pickup Settings")]
    [SerializeField] private float holdTime = 5f;   // 0→100% to‘lish vaqti
    [SerializeField] private float decayTime = 3f;  // 100→0% tushish vaqti
    [SerializeField] private bool resetAfterPerform = true;

    private bool isHolding;
    private float progress01;              // 0..1
    private Coroutine runningCR;
    private float buildRate;               // 1/holdTime (precomputed)
    private float decayRate;               // 1/decayTime (precomputed)


    public static Action OnPlayerGotLamp;
    private void OnEnable()
    {
        buildRate = (holdTime > 0f) ? 1f / holdTime : 999f;
        decayRate = (decayTime > 0f) ? 1f / decayTime : 0f;
    }


    public void BeginHold()
    {
        KopkariMainUI.Instance.DisableWebSnare();
        isHolding = true;

        if (fillImage)
        {
            // agar ilgari 0 bo‘lgan bo‘lsa, yangi jarayonni 0 dan boshlatamiz
            if (progress01 <= 0.0001f)
                progress01 = 0f;

            fillImage.fillAmount = progress01;
            fillImage.gameObject.SetActive(true);
            fillImgBg.SetActive(true);
        }

        StopRunning();
        runningCR = StartCoroutine(HoldRoutine());

        //BaseManager.Instance.CurrentCondition = BaseManager.PlayerCondition.GettingTarget;
    }


    public void EndHold()
    {
        isHolding = false;
        StopRunning();

        if (decayRate > 0f && progress01 > 0f)
            runningCR = StartCoroutine(DecayRoutine());
        else
            TryHideWhenEmpty();
    }

    private IEnumerator HoldRoutine()
    {
        while (isHolding && progress01 < 1f)
        {
            progress01 += buildRate * Time.deltaTime;
            if (fillImage) fillImage.fillAmount = progress01;
            yield return null;
        }

        if (progress01 >= 1f)
        {
            PerformAction();

            if (resetAfterPerform)
            {
                progress01 = 0f;
                if (fillImage)
                {
                    fillImage.fillAmount = 0f;
                    fillImage.gameObject.SetActive(false);
                    fillImgBg.SetActive(false);
                }
                isHolding = false;
            }
        }

        runningCR = null;
    }

    private IEnumerator DecayRoutine()
    {
        while (!isHolding && progress01 > 0f)
        {
            progress01 -= decayRate * Time.deltaTime;
            if (fillImage) fillImage.fillAmount = progress01;
            yield return null;
        }

        TryHideWhenEmpty();
        runningCR = null;
    }

    private void TryHideWhenEmpty()
    {
        if (fillImage && progress01 <= 0.0001f)
        {
            fillImage.gameObject.SetActive(false);
            fillImgBg.SetActive(false);
        }
            
    }

    private void PerformAction()
    {
        BaseManager.Instance.LambOwner = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        OnPlayerGotLamp?.Invoke();
        //playerData.PickupObj();
        Debug.Log("✅ Uloq olindi!");
    }

    private void StopRunning()
    {
        if (runningCR != null)
        {
            StopCoroutine(runningCR);
            runningCR = null;
        }
    }

    private void OnDisable()
    {
        isHolding = false;
        StopRunning();
        // fillImage ni bu yerda o‘chirmaymiz — decay routine hal qiladi
    }
}
