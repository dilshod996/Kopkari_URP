using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public enum RacingControllerType
{
    Reins = 0,
    Buttons = 1,
    Tilt = 2
}

public class RacingControllerSelecterUI : MonoBehaviour
{
    public const string ControllerPrefsKey = "Racing_Controller_Type";

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;

    [Header("Reins Text")]
    [SerializeField] private TMP_Text reinsTopTitle;
    [SerializeField] private TMP_Text reinsControllerText;
    [SerializeField] private TMP_Text reinsInfoText;
    [SerializeField] private TMP_Text reinSelectText;

    [Header("Button Text")]
    [SerializeField] private TMP_Text buttonsTopTitle;
    [SerializeField] private TMP_Text buttonsControllerText;
    [SerializeField] private TMP_Text buttonsInfoText;
    [SerializeField] private TMP_Text buttonsSelectText;

    [Header("Tilt")]
    [SerializeField] private TMP_Text tiltTopTitle;
    [SerializeField] private TMP_Text tiltControllerText;
    [SerializeField] private TMP_Text tiltInfoText;
    [SerializeField] private TMP_Text tiltSelectText;


    [Header("Buttons")]
    [SerializeField] private Button reinsButton;
    [SerializeField] private Button buttonsButton;
    [SerializeField] private Button tiltButton;

    [Header("Icons")]
    [SerializeField] private RectTransform reinsIcon;
    [SerializeField] private RectTransform buttonsIcon;
    [SerializeField] private RectTransform tiltIcon;

    [Header("Icon Pulse")]
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float pulseDuration = 0.55f;
    [SerializeField] private float pulseDelayStep = 0.15f;

    public RacingControllerType selectedController;

    [SerializeField] private LaunchTimingMeterUI launchTimingMeterPrefab;

    public static event Action<RacingControllerType> OnControllerSelected;
    private Vector3 reinsIconStartScale;
    private Vector3 buttonsIconStartScale;
    private Vector3 tiltIconStartScale;
    private bool iconScalesCached;

    private void Awake()
    {
        CacheIconScales();

        if (reinsButton != null)
            reinsButton.onClick.AddListener(SelectReins);

        if (buttonsButton != null)
            buttonsButton.onClick.AddListener(SelectButtons);

        if (tiltButton != null)
            tiltButton.onClick.AddListener(SelectTilt);
    }
    private void OnEnable()
    {
        UITexts();
        StartIconPulse();
    }

    private void OnDisable()
    {
        StopIconPulse();
    }

    private void OnDestroy()
    {
        StopIconPulse();

        if (reinsButton != null)
            reinsButton.onClick.RemoveListener(SelectReins);

        if (buttonsButton != null)
            buttonsButton.onClick.RemoveListener(SelectButtons);

        if (tiltButton != null)
            tiltButton.onClick.RemoveListener(SelectTilt);
    }
    private void SelectController(RacingControllerType controllerType)
    {
        selectedController = controllerType;

        PlayerPrefs.SetInt(ControllerPrefsKey, (int)selectedController);
        PlayerPrefs.Save();
        OnControllerSelected?.Invoke(selectedController);
    }

    public void SelectReins()
    {
        SelectController(RacingControllerType.Reins);
        OpenLaunchMeter();
    }

    public void SelectButtons()
    {
        SelectController(RacingControllerType.Buttons);
        OpenLaunchMeter();
    }

    public void SelectTilt()
    {
        SelectController(RacingControllerType.Tilt);
        OpenLaunchMeter();
    }








    public static bool HasSavedControllerSelection()
    {
        if (!PlayerPrefs.HasKey(ControllerPrefsKey))
            return false;

        int savedValue = PlayerPrefs.GetInt(ControllerPrefsKey);
        return Enum.IsDefined(typeof(RacingControllerType), savedValue);
    }

    public static RacingControllerType GetSavedControllerOrDefault(RacingControllerType fallback = RacingControllerType.Buttons)
    {
        if (!HasSavedControllerSelection())
            return fallback;

        return (RacingControllerType)PlayerPrefs.GetInt(ControllerPrefsKey);
    }

    public void ApplySavedControllerSelection()
    {
        selectedController = GetSavedControllerOrDefault();
        OnControllerSelected?.Invoke(selectedController);
    }

    public void ShowLaunchMeter()
    {
        OpenLaunchMeter();
    }

    private void OpenLaunchMeter()
    {
        
        gameObject.SetActive(false);

        if (launchTimingMeterPrefab != null)
        {
            launchTimingMeterPrefab.gameObject.SetActive(true);
            launchTimingMeterPrefab.StartLaunchMeter();
        }
           
        else
            Debug.LogError($"{nameof(RacingControllerSelecterUI)} is missing a LaunchTimingMeterUI reference.", this);
    }

    private Transform GetLaunchMeterParent()
    {
        return transform.parent != null ? transform.parent : transform;
    }



    private void CacheIconScales()
    {
        if (iconScalesCached)
            return;

        reinsIconStartScale = reinsIcon != null ? reinsIcon.localScale : Vector3.one;
        buttonsIconStartScale = buttonsIcon != null ? buttonsIcon.localScale : Vector3.one;
        tiltIconStartScale = tiltIcon != null ? tiltIcon.localScale : Vector3.one;
        iconScalesCached = true;
    }

    private void StartIconPulse()
    {
        CacheIconScales();
        StartIconPulse(reinsIcon, reinsIconStartScale, 0f);
        StartIconPulse(buttonsIcon, buttonsIconStartScale, pulseDelayStep);
        StartIconPulse(tiltIcon, tiltIconStartScale, pulseDelayStep * 2f);
    }

    private void StartIconPulse(RectTransform icon, Vector3 startScale, float delay)
    {
        if (icon == null)
            return;

        LeanTween.cancel(icon.gameObject);
        icon.localScale = startScale;
        LeanTween.scale(icon.gameObject, startScale * pulseScale, pulseDuration)
            .setDelay(delay)
            .setEaseInOutSine()
            .setLoopPingPong();
    }

    private void StopIconPulse()
    {
        CacheIconScales();
        StopIconPulse(reinsIcon, reinsIconStartScale);
        StopIconPulse(buttonsIcon, buttonsIconStartScale);
        StopIconPulse(tiltIcon, tiltIconStartScale);
    }

    private void StopIconPulse(RectTransform icon, Vector3 startScale)
    {
        if (icon == null)
            return;

        LeanTween.cancel(icon.gameObject);
        icon.localScale = startScale;
    }

    private void UITexts()
    {
        var lang = LanguageManager.Instance;
        if (lang == null)
            return;
        titleText.text = lang.GetText(517);
        infoText.text = lang.GetText(541);

        reinsTopTitle.text = lang.GetText(536);
        reinsControllerText.text = lang.GetText(515);
        reinsInfoText.text = lang.GetText(538);

        reinSelectText.text = lang.GetText(68);
        buttonsSelectText.text = lang.GetText(68);
        tiltSelectText.text = lang.GetText(68);

        buttonsTopTitle.text = lang.GetText(514);
        buttonsControllerText.text = lang.GetText(516);
        buttonsInfoText.text = lang.GetText(539);

        tiltTopTitle.text = lang.GetText(537);
        tiltControllerText.text = lang.GetText(535);
        tiltInfoText.text = lang.GetText(540);
    }

}
