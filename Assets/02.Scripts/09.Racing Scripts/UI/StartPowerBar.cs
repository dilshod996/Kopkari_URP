using System;
using UnityEngine;
using UnityEngine.UI;

public class StartPowerBar : MonoBehaviour
{
    [Header("UI")]
    public Slider powerSlider;
    public Button confirmButton;

    [Header("Settings")]
    public float sliderSpeed = 1f;
    public float perfectThreshold = 0.9f;
    public float autoStartTime = 3f;   // 3 sekund

    private bool isIncreasing = true;
    private bool hasSelected = false;
    private float timer = 0f;

    public static Action<float> OnStartPowerSelected;
    public static Action OnSliderEnabled;
    public bool isTutorial = false;

    private void OnEnable()
    {
        hasSelected = false;
        timer = 0f;
        powerSlider.value = 0f;
        isIncreasing = true;
        OnSliderEnabled?.Invoke();
        confirmButton.onClick.AddListener(OnClickConfirm);
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnClickConfirm);
    }

    private void Update()
    {
        if (hasSelected) return;

        // Slider ping-pong animation
        float delta = sliderSpeed * Time.deltaTime;

        if (isIncreasing)
        {
            powerSlider.value += delta;
            if (powerSlider.value >= 1f)
                isIncreasing = false;
        }
        else
        {
            powerSlider.value -= delta;
            if (powerSlider.value <= 0f)
                isIncreasing = true;
        }

        // AUTO TIMER
        if(isTutorial)
            return;
        timer += Time.deltaTime;
        if (timer >= autoStartTime)
        {
            AutoStartDefault();
        }
    }

    private void OnClickConfirm()
    {
        if (hasSelected) return;

        hasSelected = true;

        float value = powerSlider.value;

        OnStartPowerSelected?.Invoke(value);

        gameObject.SetActive(false);
    }

    private void AutoStartDefault()
    {
        if (hasSelected) return;

        hasSelected = true;

        OnStartPowerSelected?.Invoke(0f);

        gameObject.SetActive(false);
    }
}
