using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class KopkariMainUI : MonoBehaviour
{
    public static KopkariMainUI Instance;

    [Header("Buttons")]
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button hitBtn;
    [SerializeField] private Button shootWebBtn;
    [SerializeField] private Button chainContainerBtn;

    #region Effects / Sprint/ Hit / Walk
    [SerializeField] private Image sprintImg;
    [SerializeField] private Image slowImg;
    [SerializeField] private Image shockImg;

    #endregion

    [Header("Stats/Sprint/Hit")]
    [SerializeField] private Slider sprintSlider;
    [SerializeField] private Slider hitCountSlider;
    [Header("Sprint Timings")]
    public float drainDuration = 5f;    // Necha sekundda tugaydi
    public float refillDelay = 6f;      // Tugagandan keyin necha sekund kutadi
    public float refillDuration = 3f;   // Necha sekundda qayta to‘ladi
    private bool isPressing = false;
    private Coroutine drainRoutine;
    private Coroutine refillRoutine;

    private float totalHoldTime = 0f;
    [Header("Pages")]
    [SerializeField] private GameObject loadingPanel;



    #region Show and Hide UI Animation Data

    [Header("Scale Animation")]
    [SerializeField] private float punchScaleM = 1.2f;
    [SerializeField] private float scaleTime = 0.2f;

    [Header("Scale Settings")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punchScale = 1.1f;
    [SerializeField] private float animTime = 0.2f;
    [SerializeField] private LeanTweenType easeIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType easeOut = LeanTweenType.easeInOutQuad;
    [Header("Fade Settings For UI Pages")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeTime = 0.2f;
    #endregion

    #region Game Canvases
    [SerializeField] private GameObject mobileCanvas;
    [SerializeField] private GameObject roomCanvas;
    #endregion[Header("Game Canvases")]

    [Header("Events")]
    public static Action OnSprintStart;     // Speed → 6
    public static Action OnSprintEnd;       // Speed → 5
    public static Action<float> OnSprintHold;
    public static Action OnWebSnareBtnEnable;
    public static Action OnWebSnareStart;
    public static Action OnWebSnareFinish;

    #region Awake/Start/OnEnable/OnDisable
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }
    #endregion

    #region Button State Updates
    public void SetSprintState(bool state) => sprintBtn.interactable = state;
    public void SetJumpState(bool state) => jumpBtn.interactable = state;
    public void SetDefendState(bool state) => defendBtn.interactable = state;
    public void SetWalkZoneState(bool state) => walkZoneBtn.interactable = state;
    public void SetHitState(bool state) => hitBtn.interactable = state;

    public void SetWebState(bool state) => shootWebBtn.interactable = state;
    #endregion

    #region Show and Hide UI Pages
    public void ShowUI(MonoBehaviour ui) => ShowUI(ui.gameObject);
    public void HideUI(MonoBehaviour ui) => HideUI(ui.gameObject);

    public void ShowUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>(); // oldindan bor deb faraz qilamiz

        page.SetActive(true);
        rt.localScale = Vector3.one * startScale;
        cg.alpha = 0f;

        // fade
        LeanTween.alphaCanvas(cg, 1f, fadeTime);

        // scale
        LeanTween.scale(rt, Vector3.one * punchScale, animTime)
            .setEase(easeIn)
            .setOnComplete(() =>
            {
                LeanTween.scale(rt, Vector3.one, animTime * 0.7f)
                    .setEase(easeOut);
            });
    }

    public void HideUI(GameObject page)
    {
        if (!page) return;

        RectTransform rt = page.GetComponent<RectTransform>();
        CanvasGroup cg = page.GetComponent<CanvasGroup>();

        // fade
        LeanTween.alphaCanvas(cg, 0f, fadeTime);

        // scale + deactivate
        LeanTween.scale(rt, Vector3.one * startScale, animTime)
            .setEase(easeOut)
            .setOnComplete(() =>
            {
                page.SetActive(false);
            });
    }
    #endregion

    #region Sprint Data
    public void OnSprintButtonDown(BaseEventData data)
    {
        HandlePointerDown();
    }

    public void OnSprintButtonUp(BaseEventData data)
    {
        HandlePointerUp();
    }
    private void HandlePointerDown()
    {
        isPressing = true;
        OnSprintStart?.Invoke();   // 🔥 Sprint ON
        sprintImg?.gameObject.SetActive(true);
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);
        if (refillRoutine != null)
            StopCoroutine(refillRoutine);

        drainRoutine = StartCoroutine(DrainCoroutine());
    }

    private void HandlePointerUp()
    {
        isPressing = false;
        OnSprintEnd?.Invoke();     // 🔥 Sprint OFF
        sprintImg?.gameObject.SetActive(false);
        if (drainRoutine != null)
            StopCoroutine(drainRoutine);

        refillRoutine = StartCoroutine(RefillDelayedCoroutine());
    }
    private IEnumerator DrainCoroutine()
    {
        float startValue = sprintSlider.value;
        float t = 0f;

        while (t < drainDuration && isPressing)
        {
            t += Time.deltaTime;
            sprintSlider.value = Mathf.Lerp(startValue, 0f, t / drainDuration);
            totalHoldTime = totalHoldTime + Time.deltaTime;
            OnSprintHold?.Invoke(totalHoldTime);
            yield return null;
        }

        sprintSlider.value = 0f;

        // Agar tugab qolsa -> kutish + refill
        if (isPressing)
        {
            isPressing = false;
            OnSprintEnd?.Invoke();     // 🔥 Sprint OFF
            SetSprintState(false);
            refillRoutine = StartCoroutine(RefillDelayedCoroutine());
        }
    }

    private IEnumerator RefillDelayedCoroutine()
    {
        // 6 sekund kutadi
        yield return new WaitForSeconds(refillDelay);
        SetSprintState(true);
        float startValue = sprintSlider.value;
        float t = 0f;

        while (t < refillDuration)
        {
            t += Time.deltaTime;
            sprintSlider.value = Mathf.Lerp(startValue, 1f, t / refillDuration);
            yield return null;
        }

        sprintSlider.value = 1f;
    }

    // 🔥 Booster eliksir eventi – birdan FULL qilish
    private void HandleSprintFull()
    {
        // Hozirgi barcha harakatlarni to‘xtatamiz
        if (drainRoutine != null) StopCoroutine(drainRoutine);
        if (refillRoutine != null) StopCoroutine(refillRoutine);

        // Cooldown kutmasdan zudlik bilan FULL
        sprintSlider.value = 1f;

        // Agar hozir ham player bosib turgan bo‘lsa, yana drainni qayta boshlab yuborsak ham bo‘ladi:
        if (isPressing)
        {
            drainRoutine = StartCoroutine(DrainCoroutine());
        }
    }
    private void ShowSprintEffect()
    {
        sprintImg.gameObject.SetActive(true);
        SetSprintState(false);
    }
    private void HideSprintEffect()
    {
        sprintImg?.gameObject.SetActive(false);
        SetSprintState(true);
    }
    #endregion

}
