using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapCardScaler : MonoBehaviour
{
    public enum MapScalerType
    {
        Racing,
        Kopkari,
        Archery
    }
    public MapScalerType mapType = MapScalerType.Racing;
    public ScrollRect scrollRect;
    public float lerpSpeed = 5f;

    public MapCard[] mapCards;
    private MapCard targetCard = null;
    private bool isScrollingToCard = false;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TMP_Text closeText;

    [Header("Main Background")]
    [SerializeField] private Image mainBackgroundImage; // <-- background image
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.15f;

    private MapCard currentMainCard;
    private Coroutine bgFadeRoutine;

    [Header("Top/Bottom UI Anim")]
    [SerializeField] private RectTransform topImageRect;
    [SerializeField] private RectTransform bottomImageRect;
    [SerializeField] private float slideDuration = 0.5f;

    private Coroutine uiSlideRoutine;

    private const float OFF_TOP_L = -2400f;
    private const float OFF_TOP_R = 2400f;
    private const float OFF_BOTTOM_L = 2400f;
    private const float OFF_BOTTOM_R = -2400f;
    void Start()
    {
        foreach (var card in mapCards)
            card.Initialize(this);

        StartCoroutine(CenterCardAfterFrame());
    }

    void Update()
    {
        // New Input System — foydalanuvchi ekranga tegayotganini aniqlash
        if ((Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed))
        {
            isScrollingToCard = false;
            targetCard = null;
        }

        HandleSmoothScroll();
        UpdateMainCardScalingAndShadow();
    }

    void HandleSmoothScroll()
    {
        if (isScrollingToCard && targetCard != null)
        {
            Vector3 center = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
            Vector3 cardCenter = targetCard.transform.TransformPoint(targetCard.GetComponent<RectTransform>().rect.center);

            float distance = center.x - cardCenter.x;
            Vector2 newPos = scrollRect.content.anchoredPosition + new Vector2(distance, 0);

            scrollRect.content.anchoredPosition =
                Vector2.Lerp(scrollRect.content.anchoredPosition, newPos, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(distance) < 1f)
            {
                isScrollingToCard = false;
                targetCard = null;
            }
        }
    }

    void UpdateMainCardScalingAndShadow()
    {
        if (mapCards == null || mapCards.Length == 0) return;

        Vector3 center = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        MapCard closestCard = null;
        float closestDistance = Mathf.Infinity;

        foreach (var card in mapCards)
        {
            float distance = Mathf.Abs(card.transform.position.x - center.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCard = card;
            }
        }

        // scale/shadow update
        foreach (var card in mapCards)
            card.SetAsMain(card == closestCard);

        // ✅ background update (faqat main card o'zgarganda)
        if (closestCard != null && closestCard != currentMainCard)
        {
            currentMainCard = closestCard;
            ApplyMainBackgroundFromCard(currentMainCard);
        }
    }

    private void ApplyMainBackgroundFromCard(MapCard card)
    {
        if (mainBackgroundImage == null || card == null) return;

        // MapCard ichida sprite bo'lishi kerak:
        Sprite bg = card.BackgroundSprite; // <-- MapCard dan olamiz
        if (bg == null) return;

        if (!useFade)
        {
            mainBackgroundImage.sprite = bg;
            return;
        }

        // ✅ faqat background fade coroutine'ni to'xtatamiz
        if (bgFadeRoutine != null)
        {
            StopCoroutine(bgFadeRoutine);
            bgFadeRoutine = null;
        }
        bgFadeRoutine = StartCoroutine(FadeSwitchBackground(bg));
    }

    private IEnumerator FadeSwitchBackground(Sprite newSprite)
    {
        // Fade out
        float t = 0f;
        Color c = mainBackgroundImage.color;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            mainBackgroundImage.color = c;
            yield return null;
        }

        // Switch sprite
        mainBackgroundImage.sprite = newSprite;

        // Fade in
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            mainBackgroundImage.color = c;
            yield return null;
        }

        c.a = 1f;
        mainBackgroundImage.color = c;
        bgFadeRoutine = null; // ✅
    }

    public void ScrollToCard(MapCard card)
    {
        targetCard = card;
        isScrollingToCard = true;
    }

    IEnumerator CenterCardAfterFrame()
    {
        yield return null;

        // mapCards ni yangilab olamiz
        mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();

        // ✅ page ochilganda qaysi card main bo'lsa background ham shunga o‘tadi
        //(sen hozir second cardni markazga olib kelasanyapti)
        if (mapCards.Length >= 2)
        {
            ScrollToCard(mapCards[1]);
            // backgroundni darrov moslab qo'yamiz (scroll tugashini kutmay)
            currentMainCard = mapCards[1];
            ApplyMainBackgroundFromCard(currentMainCard);
        }
        else if (mapCards.Length == 1)
        {
            currentMainCard = mapCards[0];
            ApplyMainBackgroundFromCard(currentMainCard);
        }
        //ScrollToCard(mapCards[0]);
        //currentMainCard = mapCards[0];
        //ApplyMainBackgroundFromCard(currentMainCard);
    }

    private void OnEnable()
    {
        UITransilitions();
        closeBtn.onClick.AddListener(ClosePage);

        //bool canScroll = IsTutorialFinished();
        //scrollRect.enabled = canScroll;
        //scrollRect.horizontal = canScroll;

        if (uiSlideRoutine != null) StopCoroutine(uiSlideRoutine);
        uiSlideRoutine = StartCoroutine(PlayTopBottomSlideNextFrame());
        StartCoroutine(ApplyBackgroundNextFrame());
    }
    private void UITransilitions()
    {
        if (mapType == MapScalerType.Racing)
        {
            titleText.text = LanguageManager.Instance.GetText(375);
        }
        else if(mapType == MapScalerType.Kopkari)
        {
            titleText.text = LanguageManager.Instance.GetText(482);
        }
        closeText.text = LanguageManager.Instance.GetText(362);
    }
    private IEnumerator ApplyBackgroundNextFrame()
    {
        yield return null;
        UpdateMainCardScalingAndShadow(); // current main ni topadi va backgroundni set qiladi
    }

    private void ClosePage()
    {
        this.gameObject.SetActive(false);
        HomeMainUI.Instance.OpenGameMainPanel();
    }

    void OnDisable()
    {
        if (bgFadeRoutine != null)
        {
            StopCoroutine(bgFadeRoutine);
            bgFadeRoutine = null;
        }
        mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();
        if (mapCards.Length >= 2)
        {
            float total = mapCards.Length - 1;
            float normalizedPos = Mathf.Clamp01(1f / total);
            scrollRect.horizontalNormalizedPosition = normalizedPos;
        }
        if (uiSlideRoutine != null) StopCoroutine(uiSlideRoutine);
        uiSlideRoutine = null;

        ResetTopBottomToOff();
        closeBtn.onClick.RemoveListener(ClosePage);
    }
    private bool IsTutorialFinished()
    {
        return PlayerPrefs.GetInt(Constants.Tutorial.TutorialPlay, 0) == 1;
    }


    #region Top And Bottom UI Anim
    private IEnumerator PlayTopBottomSlideNextFrame()
    {
        // ✅ bir frame kutamiz (UI settle bo‘lsin)
        yield return null;
        // xohlasang yanada “qattiqroq”:
        // yield return new WaitForEndOfFrame();

        ResetTopBottomToOff(); // (-2285/2285) holatiga tushiradi

        // yana 1 frame ham bersa yanada stabil bo‘ladi (ixtiyoriy)
        yield return null;

        // endi animatsiya
        yield return SlideTopBottomToZero();
        uiSlideRoutine = null;
    }



    // ✅ Left/Right ni to‘g‘ri qo‘yadi (Right ichkarida manfiy bo‘ladi!)
    private static void SetLeftRight(RectTransform rt, float left, float right)
    {
        if (!rt) return;
        var min = rt.offsetMin;
        var max = rt.offsetMax;
        min.x = left;      // left
        max.x = -right;    // right (!!!)
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    private static (float left, float right) GetLeftRight(RectTransform rt)
    {
        if (!rt) return (0, 0);
        float left = rt.offsetMin.x;
        float right = -rt.offsetMax.x; // (!!!)
        return (left, right);
    }

    private void ResetTopBottomToOff()
    {
        SetLeftRight(topImageRect, OFF_TOP_L, OFF_TOP_R);
        SetLeftRight(bottomImageRect, OFF_BOTTOM_L, OFF_BOTTOM_R);
    }

    private IEnumerator SlideTopBottomToZero()
    {
        float t = 0f;

        var (topL0, topR0) = GetLeftRight(topImageRect);
        var (botL0, botR0) = GetLeftRight(bottomImageRect);

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / slideDuration);

            // simple ease out
            float eased = 1f - Mathf.Pow(1f - a, 3f);

            SetLeftRight(topImageRect,
                Mathf.Lerp(topL0, 0f, eased),
                Mathf.Lerp(topR0, 0f, eased));

            SetLeftRight(bottomImageRect,
                Mathf.Lerp(botL0, 0f, eased),
                Mathf.Lerp(botR0, 0f, eased));

            yield return null;
        }

        SetLeftRight(topImageRect, 0f, 0f);
        SetLeftRight(bottomImageRect, 0f, 0f);
    }

    #endregion
}
