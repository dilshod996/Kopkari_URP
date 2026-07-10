using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LaunchTimingMeterUI : MonoBehaviour
{
    public enum LaunchResult
    {
        Miss,
        Bad,
        Good,
        Perfect
    }

    [Header("Main UI")]
    [SerializeField] private CanvasGroup meterContainer;
    [SerializeField] private CanvasGroup gameUIGroup;

    [Header("Meter")]
    [SerializeField] private RectTransform meterArea;
    [SerializeField] private RectTransform marker;
    [SerializeField] private Button meterClickButton;

    [Header("Result Text")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text countdownText;

    [Header("UI Text Fields")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text perfectText;
    [SerializeField] private TMP_Text tooEarlyText;
    [SerializeField] private TMP_Text tooLateText;
    [SerializeField] private TMP_Text readyGoText;
    [SerializeField] private TMP_Text selectionDataText;
    [Header("Ready Go Image")]
    [SerializeField] private RectTransform readyGoImage;
    [SerializeField] private CanvasGroup readyGoGroup;

    [SerializeField] private RectTransform countDownTextBg;

    [Header("Timing")]
    [SerializeField] private float launchLifetimeDuration = 5f;
    [SerializeField] private float meterMoveDuration = 2f;
    [SerializeField] private float resultShowDuration = 0.75f;
    [SerializeField] private float readyGoShowDuration = 0.8f;

    [Header("Zones 0..1")]
    [SerializeField, Range(0f, 1f)] private float perfectStart = 0.45f;
    [SerializeField, Range(0f, 1f)] private float perfectEnd = 0.55f;

    [SerializeField, Range(0f, 1f)] private float goodStart = 0.32f;
    [SerializeField, Range(0f, 1f)] private float goodEnd = 0.68f;

    [Header("Boost Values")]
    [SerializeField] private float perfectBoostMultiplier = 1f;
    [SerializeField] private float goodBoostMultiplier = 0.65f;
    [SerializeField] private float badBoostMultiplier = 0.3f;

    [SerializeField] private float perfectBoostDuration = 4f;
    [SerializeField] private float goodBoostDuration = 2.5f;
    [SerializeField] private float badBoostDuration = 1f;

    public Action<LaunchResult, float, float> OnLaunchFinished;
    // result, boostMultiplier, boostDuration
    public static Action OnLaunchMeterStarted;
    public static Action<LaunchResult, float, float> OnLaunchFinishedGlobal;

    private Tween markerTween;
    private Tween countdownTween;
    private Tween lifetimeTween;
    private Sequence flowSequence;

    private bool isRunning;
    private bool hasClicked;
    private float currentNormalized;

    private Vector2 readyGoOriginalPos;

    private void Awake()
    {
        if (meterClickButton != null)
            meterClickButton.onClick.AddListener(OnMeterClicked);

        if (readyGoImage != null)
            readyGoOriginalPos = readyGoImage.anchoredPosition;
    }
    private void OnEnable()
    {
        LangaugeUpdate();
    }
    private void OnDestroy()
    {
        markerTween?.Kill();
        countdownTween?.Kill();
        lifetimeTween?.Kill();
        flowSequence?.Kill();

        if (meterClickButton != null)
            meterClickButton.onClick.RemoveListener(OnMeterClicked);

    }

    private void OnDisable()
    {
        markerTween?.Kill();
        countdownTween?.Kill();
        lifetimeTween?.Kill();
        flowSequence?.Kill();
    }

    private void LangaugeUpdate()
    {
        var lang = LanguageManager.Instance;
        if (lang == null) return;
        titleText.text = lang.GetText(542);
        detailsText.text = lang.GetText(543);
        perfectText.text = lang.GetText(568);
        tooEarlyText.text = lang.GetText(570);
        tooLateText.text = lang.GetText(569);
    }
    public void StartLaunchMeter()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureTimingDefaults();

        if (!HasFullMeterUI())
        {
            Debug.LogWarning($"{nameof(LaunchTimingMeterUI)} is missing required launch meter UI references.", this);
            return;
        }

        markerTween?.Kill();
        countdownTween?.Kill();
        lifetimeTween?.Kill();
        flowSequence?.Kill();

        isRunning = true;
        hasClicked = false;
        currentNormalized = 0f;

        SetGameUI(false);
        SetupMeterStartState();
        SetupCountdownStartState();
        SetupReadyGoStartState();

        AnimateMeterIn();
        StartMarkerMove();
        StartCountdown();
        StartLifetimeTimer();
        OnLaunchMeterStarted?.Invoke();
    }

    private void EnsureTimingDefaults()
    {
        if (launchLifetimeDuration <= 0f) launchLifetimeDuration = 5f;
        if (meterMoveDuration <= 0f) meterMoveDuration = 2f;
        if (resultShowDuration <= 0f) resultShowDuration = 0.75f;
        if (readyGoShowDuration <= 0f) readyGoShowDuration = 0.8f;

        if (perfectEnd <= perfectStart)
        {
            perfectStart = 0.45f;
            perfectEnd = 0.55f;
        }

        if (goodEnd <= goodStart)
        {
            goodStart = 0.32f;
            goodEnd = 0.68f;
        }

        if (perfectBoostMultiplier <= 0f) perfectBoostMultiplier = 1f;
        if (goodBoostMultiplier <= 0f) goodBoostMultiplier = 0.65f;
        if (badBoostMultiplier <= 0f) badBoostMultiplier = 0.3f;

        if (perfectBoostDuration <= 0f) perfectBoostDuration = 3f;
        if (goodBoostDuration <= 0f) goodBoostDuration = 2f;
        if (badBoostDuration <= 0f) badBoostDuration = 1f;

    }

    private bool HasFullMeterUI()
    {
        return meterContainer != null
            && meterArea != null
            && marker != null
            && meterClickButton != null;
    }

    private void SetupMeterStartState()
    {
        if (meterContainer != null)
        {
            meterContainer.gameObject.SetActive(true);
            meterContainer.alpha = 0f;
            meterContainer.interactable = true;
            meterContainer.blocksRaycasts = true;
            meterContainer.transform.localScale = Vector3.one * 0.92f;
        }

        if (resultText != null)
        {
            resultText.text = "";
            resultText.alpha = 0f;
            resultText.transform.localScale = Vector3.one * 0.8f;
        }

        if (meterClickButton != null)
            meterClickButton.interactable = true;

        UpdateMarkerPosition(0f);
    }

    private void SetupCountdownStartState()
    {
        if (countDownTextBg != null)
            countDownTextBg.gameObject.SetActive(true);

        if (countdownText == null) return;

        countdownText.gameObject.SetActive(true);
        countdownText.alpha = 1f;
        countdownText.text = Mathf.CeilToInt(launchLifetimeDuration).ToString();
    }

    private void SetupReadyGoStartState()
    {
        if (readyGoGroup == null || readyGoImage == null) return;

        readyGoGroup.gameObject.SetActive(false);
        readyGoGroup.alpha = 0f;

        readyGoImage.anchoredPosition = readyGoOriginalPos + new Vector2(0f, -220f);
        readyGoImage.localScale = Vector3.one * 0.75f;
    }

    private void AnimateMeterIn()
    {
        if (meterContainer == null) return;

        meterContainer.DOFade(1f, 0.25f).SetEase(Ease.OutQuad);
        meterContainer.transform
            .DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack);
    }

    private void StartMarkerMove()
    {
        if (meterArea == null || marker == null) return;

        markerTween?.Kill();

        markerTween = DOTween.To(
                () => currentNormalized,
                value =>
                {
                    currentNormalized = value;
                    UpdateMarkerPosition(currentNormalized);
                },
                1f,
                meterMoveDuration
            )
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                currentNormalized = 0f;
                UpdateMarkerPosition(currentNormalized);
            })
            .SetLoops(-1, LoopType.Restart);
    }

    private void StartCountdown()
    {
        if (countdownText == null) return;

        int maxSeconds = Mathf.CeilToInt(launchLifetimeDuration);

        countdownTween = DOTween.To(
                () => launchLifetimeDuration,
                remaining =>
                {
                    int seconds = Mathf.Clamp(Mathf.CeilToInt(remaining), 1, maxSeconds);
                    countdownText.text = seconds.ToString();
                },
                0f,
                launchLifetimeDuration
            )
            .SetEase(Ease.Linear);
    }

    private void StartLifetimeTimer()
    {
        lifetimeTween = DOVirtual.DelayedCall(launchLifetimeDuration, () =>
        {
            if (!hasClicked)
                FinishMeter(LaunchResult.Miss, 0f, 0f);
        });
    }

    private void UpdateMarkerPosition(float normalized)
    {
        if (meterArea == null || marker == null) return;

        float width = meterArea.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, normalized);

        Vector2 pos = marker.anchoredPosition;
        pos.x = x;
        marker.anchoredPosition = pos;
    }

    private void OnMeterClicked()
    {
        if (!isRunning || hasClicked) return;

        hasClicked = true;
        isRunning = false;

        if (meterClickButton != null)
            meterClickButton.interactable = false;

        markerTween?.Kill();
        countdownTween?.Kill();
        lifetimeTween?.Kill();

        LaunchResult result = GetLaunchResult(currentNormalized);
        float boostMultiplier = GetBoostMultiplier(result);
        float boostDuration = GetBoostDuration(result);

        FinishMeter(result, boostMultiplier, boostDuration);
    }

    private LaunchResult GetLaunchResult(float value)
    {
        if (value >= perfectStart && value <= perfectEnd)
            return LaunchResult.Perfect;

        if (value >= goodStart && value <= goodEnd)
            return LaunchResult.Good;

        return LaunchResult.Bad;
    }

    private float GetBoostMultiplier(LaunchResult result)
    {
        return result switch
        {
            LaunchResult.Perfect => perfectBoostMultiplier,
            LaunchResult.Good => goodBoostMultiplier,
            LaunchResult.Bad => badBoostMultiplier,
            _ => 0f
        };
    }

    private float GetBoostDuration(LaunchResult result)
    {
        return result switch
        {
            LaunchResult.Perfect => perfectBoostDuration,
            LaunchResult.Good => goodBoostDuration,
            LaunchResult.Bad => badBoostDuration,
            _ => 0f
        };
    }

    private void FinishMeter(LaunchResult result, float boostMultiplier, float boostDuration)
    {
        markerTween?.Kill();
        countdownTween?.Kill();
        lifetimeTween?.Kill();
        flowSequence?.Kill();

        isRunning = false;
        hasClicked = true;

        string resultLabel = LanguageManager.Instance?.GetText(GetResultText(result));
        HideCountdown();

        flowSequence = DOTween.Sequence();

        flowSequence.AppendCallback(() =>
        {
            ShowResult(resultLabel);
        });

        flowSequence.AppendInterval(resultShowDuration);

        flowSequence.AppendCallback(() =>
        {
            HideMeterContainer();
        });

        flowSequence.AppendInterval(0.25f);

        flowSequence.AppendCallback(() =>
        {
            ShowReadyGo();
        });

        flowSequence.AppendInterval(readyGoShowDuration);

        flowSequence.AppendCallback(() =>
        {
            HideReadyGo();
        });

        flowSequence.AppendInterval(0.25f);

        flowSequence.AppendCallback(() =>
        {
            SetGameUI(true);
            OnLaunchFinished?.Invoke(result, boostMultiplier, boostDuration);
            OnLaunchFinishedGlobal?.Invoke(result, boostMultiplier, boostDuration);
            CloseLaunchMeter();
        });
    }

    private int GetResultText(LaunchResult result)
    {
        return result switch
        {
            LaunchResult.Perfect => 544,
            LaunchResult.Good => 545,
            LaunchResult.Bad => 546,
            LaunchResult.Miss => 547,
            _ => 547
        };
    }

    private void ShowResult(string text)
    {
        if (resultText == null) return;

        resultText.text = text;
        resultText.alpha = 0f;
        resultText.transform.localScale = Vector3.one * 0.75f;

        resultText.DOFade(1f, 0.18f).SetEase(Ease.OutQuad);
        resultText.transform
            .DOScale(1.12f, 0.22f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                resultText.transform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad);
            });
    }

    private void HideCountdown()
    {
        if (countDownTextBg != null)
            countDownTextBg.gameObject.SetActive(false);

        if (countdownText == null) return;

        countdownText.alpha = 0f;
        countdownText.gameObject.SetActive(false);
    }

    private void CloseLaunchMeter()
    {
        gameObject.SetActive(false);
    }

    private void HideMeterContainer()
    {
        if (meterContainer == null) return;

        meterContainer.interactable = false;
        meterContainer.blocksRaycasts = false;

        meterContainer.DOFade(0f, 0.25f).SetEase(Ease.InQuad);
        meterContainer.transform
            .DOScale(0.92f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                meterContainer.gameObject.SetActive(false);
            });
    }

    private void ShowReadyGo()
    {
        if (readyGoGroup == null || readyGoImage == null) return;

        readyGoGroup.gameObject.SetActive(true);
        readyGoGroup.alpha = 0f;

        readyGoImage.anchoredPosition = readyGoOriginalPos + new Vector2(0f, -220f);
        readyGoImage.localScale = Vector3.one * 0.75f;

        readyGoGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad);

        readyGoImage
            .DOAnchorPos(readyGoOriginalPos, 0.35f)
            .SetEase(Ease.OutBack);

        readyGoImage
            .DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack);
    }

    private void HideReadyGo()
    {
        if (readyGoGroup == null || readyGoImage == null) return;

        readyGoGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
        readyGoImage
            .DOScale(1.2f, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                readyGoGroup.gameObject.SetActive(false);
                readyGoImage.localScale = Vector3.one;
                readyGoImage.anchoredPosition = readyGoOriginalPos;
            });
    }

    private void SetGameUI(bool visible)
    {
        if (gameUIGroup == null) return;

        gameUIGroup.gameObject.SetActive(true);

        if (visible)
        {
            gameUIGroup.alpha = 0f;
            gameUIGroup.interactable = true;
            gameUIGroup.blocksRaycasts = true;

            gameUIGroup
                .DOFade(1f, 0.25f)
                .SetEase(Ease.OutQuad);
        }
        else
        {
            gameUIGroup.alpha = 0f;
            gameUIGroup.interactable = false;
            gameUIGroup.blocksRaycasts = false;
        }
    }
}
