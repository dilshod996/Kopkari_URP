using MalbersAnimations.Scriptables;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonActions : MonoBehaviour
{
    public static UIButtonActions Instance { get; private set; }

    [Header("Buttons")]
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button hitBtn;
    [SerializeField] private Button shootChainBtn;
    [SerializeField] private Button chainContainerBtn;

    [Header("Buttons Data Texts")]
    [SerializeField] private TMP_Text defendCountText;
    [SerializeField] private TMP_Text walkZoneCountText;
    [SerializeField] private TMP_Text hitCountText;
    [SerializeField] private TMP_Text chainCounter;

    [Header("Shock Effect")]
    [SerializeField] private Image shockImg;
    [SerializeField] private float shockLife = 0.25f;

    [Header("Slow Effect")]
    [SerializeField] private Image slowImg;
    [SerializeField] private float slowLife = 10f;

    [Header("Sprint Effect")]
    [SerializeField] private Image sprintImg;
   // [SerializeField] private float sprintLife = 3f;

    // umumiy parametrlari (o‘zgarmaydi)
    private const string shockFloat = "_ShockAmount";
    private const string slowFloat = "_SlowAmount";
    private const float fadeIn = 0.2f;
    private const float fadeOut = 0.3f;

    private int tweenUp = -1;
    private int tweenDown = -1;

    [Header("Hit Count Slider")]
    public Slider hitCountSlider;

    [Header("Chain Data")]
    [SerializeField] private IntVar chainCount;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    #region Text Updates
    public void UpdateDefendText(int count) => defendCountText.text = count.ToString();
    public void UpdateWalkZoneText(int count) => walkZoneCountText.text = count.ToString();
    public void UpdateHitText(int count) => hitCountText.text = count.ToString();

    public void UpdateChainCount(int count)=> chainCounter.text = count.ToString();
    #endregion

    #region Button State Updates
    public void SetSprintState(bool state) => sprintBtn.interactable = state;
    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;

    public void SetChainState(bool state) => shootChainBtn.interactable = state;
    #endregion

    /// <summary>
    /// Dastlabki qiymatlar va button holatini sozlash.
    /// </summary>
    public void InitializeData(int defendCount, int walkZoneCount, int hitZoneCount)
    {
        UpdateDefendText(defendCount);
        UpdateWalkZoneText(walkZoneCount);
        UpdateHitText(hitZoneCount);
        UpdateChainCount(chainCount.Value);

        SetDefendState(defendCount > 0);
        SetWalkZoneState(walkZoneCount > 0);
        SetHitState(hitZoneCount > 0);
        SetChainState(chainCount.Value > 0);
    }

    /// <summary>
    /// Playerga tegishli BoostersContainer ga UI ni bog‘laydi
    /// </summary>
    public void Bind(BoostersContainer boosters)
    {
        if (walkZoneBtn)
        {
            walkZoneBtn.onClick.RemoveAllListeners();
            walkZoneBtn.onClick.AddListener(() =>
            {
                if (boosters != null && !boosters.isNpc)
                    boosters.DropWalkTrap();
            });
        }
        if (defendBtn) {
            defendBtn.onClick.RemoveAllListeners();
            defendBtn.onClick.AddListener(() =>
            {
                if(boosters != null && !boosters.isNpc)
                {
                    boosters.DefendPlayer();
                }
            });
        }
        // xohlasangiz boshqa tugmalarni ham shu yerda bog‘laysiz
        // defendBtn.onClick.AddListener(boosters.DefendPlayer);
        // ...
    }
    public void SliderValueRestore()
    {
        hitCountSlider.value = hitCountSlider.maxValue;
    }
    #region UI Effects
    private void InitEffect(Image img, string floatName)
    {
        if (!img) return;
        var mat = img.material;
        if (mat && mat.HasProperty(floatName))
            mat.SetFloat(floatName, 0f);

        img.gameObject.SetActive(false);
    }

    // 🔹 Shock Effect
    public void PlayShock()
    {
        if (!shockImg) return;
        hitCountSlider.value--;
        PlayShaderEffect(shockImg, shockFloat, shockLife);
    }

    // 🔹 Slow Effect
    public void PlaySlow()
    {
        if (!slowImg) return;
        PlayShaderEffect(slowImg, slowFloat, slowLife);
        StopSingleEffect(shockImg, shockFloat);
    }

    // 🔹 Sprint Effect (faqat aktiv/inaktiv)
    //public void PlaySprint(bool enable)
    //{
    //    if (!sprintImg) return;
    //    sprintImg.gameObject.SetActive(enable);

    //    if (enable)
    //    {
    //        LeanTween.cancel(sprintImg.gameObject);
    //        LeanTween.delayedCall(sprintImg.gameObject, sprintLife, () =>
    //        {
    //            if (sprintImg) sprintImg.gameObject.SetActive(false);
    //        }).setIgnoreTimeScale(true);
    //    }
    //}

    // 🔹 Umumiy shader-based effekt helper
    private void PlayShaderEffect(Image img, string floatName, float life)
    {
        if (!img) return;
        img.gameObject.SetActive(true);

        if (tweenUp != -1) LeanTween.cancel(tweenUp);
        if (tweenDown != -1) LeanTween.cancel(tweenDown);

        float current = GetFloatSafe(img, floatName);

        tweenUp = LeanTween.value(img.gameObject, current, 1f, fadeIn)
            .setEaseOutQuad()
            .setIgnoreTimeScale(true)
            .setOnUpdate(v => SetFloatSafe(img, floatName, v))
            .setOnComplete(() =>
            {
                LeanTween.delayedCall(img.gameObject, life, () =>
                {
                    tweenDown = LeanTween.value(img.gameObject, 1f, 0f, fadeOut)
                        .setEaseInCubic()
                        .setIgnoreTimeScale(true)
                        .setOnUpdate(v => SetFloatSafe(img, floatName, v))
                        .setOnComplete(() => img.gameObject.SetActive(false))
                        .id;
                }).setIgnoreTimeScale(true);
            }).id;
    }

    private float GetFloatSafe(Image img, string prop)
    {
        var mat = img.material;
        return (mat && mat.HasProperty(prop)) ? mat.GetFloat(prop) : 0f;
    }

    private void SetFloatSafe(Image img, string prop, float v)
    {
        var mat = img.material;
        if (mat && mat.HasProperty(prop))
        {
            mat.SetFloat(prop, v);
            img.SetMaterialDirty();
        }
    }
    private void StopSingleEffect(Image img, string floatProp)
    {
        if (!img) return;

        LeanTween.cancel(img.gameObject);
        SetFloatSafe(img, floatProp, 0f);
        img.gameObject.SetActive(false);
    }
    public void SprintEffect(bool value)
    {
        sprintImg.gameObject.SetActive(value);
        if (value)
        {
            StopSingleEffect(slowImg, slowFloat);
        }
    }
    #endregion

    #region Chain Section
    /// <summary>
    /// Bular ikkalasi ham Btn ga ulangan
    /// </summary>
    public void OnShootCHain()
    {
        UpdateChainCount(chainCount.Value);
    }
    public void OnClickChain()
    {
        bool newState = !chainContainerBtn.gameObject.activeSelf;
        chainContainerBtn.gameObject.SetActive(newState);
    }
    #endregion
}
