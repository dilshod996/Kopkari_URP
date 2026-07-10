using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using MalbersAnimations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RaceIntroFlowController : MonoBehaviour
{
    [Header("Camera Positions")]
    [SerializeField] private Transform camPos1;
    [SerializeField] private Transform camPos2;
    [SerializeField] private List<Transform> introCameraPositions = new List<Transform>();

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCamera introCamera;
    [SerializeField] private CinemachineVirtualCamera gameplayCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private bool activateIntroCameraOnAwake = true;
    [SerializeField] private int introActivePriority = 100;
    [SerializeField] private int gameplayActivePriority = 10;
    [SerializeField] private int inactivePriority = -1;

    [Header("Gameplay Camera Start View")]
    [SerializeField] private ThirdPersonFollowTarget gameplayFollowTarget;
    [SerializeField] private float gameplayStartYaw = 160f;
    [SerializeField] private float gameplayStartPitch = 15f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float blinkCloseDuration = 0.18f;
    [SerializeField] private float blinkOpenDuration = 0.32f;
    [SerializeField] private float blinkClosedHoldDuration = 0.05f;

    [Header("Intro UI")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject playersListPrefab;
    [SerializeField] private float playersListStartDelay = 0.25f;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject launchMeterRoot;
    [SerializeField] private bool activateLaunchMeterRootOnComplete;

    [Header("Timing")]
    [SerializeField] private float camPos1Duration = 1.75f;
    [SerializeField] private float camPos2Duration = 1.25f;
    [SerializeField] private float playersListDuration = 1.75f;
    [SerializeField] private float gameplayBlendDuration = 0.8f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onIntroComplete;

    private Coroutine introRoutine;
    private Action completionCallback;
    private CinemachineBlendDefinition originalDefaultBlend;
    private int originalGameplayPriority;
    private Transform selectedCamPos1;
    private Transform selectedCamPos2;
    private bool hasOriginalBlend;
    private bool isPlaying;
    private bool skipAllowed;
    private bool skipRequested;
    private bool completionInvoked;
    private bool completeIntroOnResume;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(RequestSkip);

        RacingPlayers.OnStartRequested += RequestSkip;

        HideSkipButton();
        HidePlayersListImmediate();

        if (launchMeterRoot != null)
            launchMeterRoot.SetActive(false);

        if (activateIntroCameraOnAwake)
            ActivateIntroCameraAtCamPos1();
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(RequestSkip);

        RacingPlayers.OnStartRequested -= RequestSkip;

        KillRunningTweens();
    }

    private void OnDisable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        isPlaying = false;
        skipAllowed = false;
        skipRequested = false;
        completeIntroOnResume = false;
        KillRunningTweens();
    }

    private void OnApplicationPause(bool paused)
    {
        if (!isPlaying || completionInvoked)
            return;

        if (paused)
        {
            completeIntroOnResume = true;

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            ForceBlackIntroPauseState();
            return;
        }

        if (!completeIntroOnResume)
            return;

        completeIntroOnResume = false;
        introRoutine = StartCoroutine(CompleteIntroThroughGameplayCamera());
    }

    public void PlayIntro()
    {
        PlayIntro(null);
    }

    public void PlayIntro(Action onComplete)
    {
        if (isPlaying)
            return;

        completionCallback = onComplete;
        introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    public void RequestSkip()
    {
        if (!isPlaying || !skipAllowed || skipRequested)
            return;

        skipRequested = true;
        HidePlayersListImmediate();
        HideSkipButton();
    }

    public void ActivateIntroCameraAtCamPos1()
    {
        SelectIntroCameraPositions();
        MoveIntroCameraTo(selectedCamPos1);

        if (introCamera == null)
            return;

        int targetPriority = introActivePriority;
        if (gameplayCamera != null)
            targetPriority = Mathf.Max(targetPriority, gameplayCamera.Priority + 1);

        introCamera.Priority = targetPriority;
        introCamera.PreviousStateIsValid = false;
        SetFadeAlpha(1f);
    }

    private IEnumerator PlayIntroRoutine()
    {
        isPlaying = true;
        skipAllowed = false;
        skipRequested = false;
        completionInvoked = false;

        CacheCinemachineState();
        PrepareIntroState();
        SelectIntroCameraPositions();

        if (introCamera == null || gameplayCamera == null)
        {
            Debug.LogWarning($"{nameof(RaceIntroFlowController)} needs intro and gameplay virtual cameras.", this);
            yield return CompleteIntroThroughGameplayCamera();
            yield break;
        }

        MoveIntroCameraTo(selectedCamPos1);
        SetIntroCameraActive();
        yield return null;

        yield return BlinkOpen();
        yield return WaitOrSkip(camPos1Duration, false);
        yield return BlinkClosed();

        MoveIntroCameraTo(selectedCamPos2);
        yield return null;

        yield return BlinkOpen();
        skipAllowed = true;
        ShowSkipButton();

        yield return PlayCamPos2PlayersListSequence();

        yield return CompleteIntroThroughGameplayCamera();
    }

    private void PrepareIntroState()
    {
        SetFadeAlpha(1f);
        HideSkipButton();
        HidePlayersListImmediate();
        HideLaunchMeterRoot();
    }

    private void CacheCinemachineState()
    {
        if (cinemachineBrain == null && Camera.main != null)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        if (cinemachineBrain != null)
        {
            originalDefaultBlend = cinemachineBrain.m_DefaultBlend;
            hasOriginalBlend = true;
        }

        if (gameplayCamera != null)
            originalGameplayPriority = gameplayCamera.Priority;
    }

    private void SetIntroCameraActive()
    {
        introCamera.Priority = Mathf.Max(introActivePriority, gameplayCamera.Priority + 1);
        introCamera.PreviousStateIsValid = false;
    }

    private void SelectIntroCameraPositions()
    {
        int validCount = CountValidIntroCameraPositions();

        if (validCount <= 0)
        {
            selectedCamPos1 = camPos1;
            selectedCamPos2 = camPos2 != null ? camPos2 : camPos1;
            return;
        }

        Transform first = GetValidIntroCameraPositionAt(UnityEngine.Random.Range(0, validCount));

        if (validCount == 1)
        {
            selectedCamPos1 = first;
            selectedCamPos2 = first;
            return;
        }

        Transform second = first;
        int safety = 0;

        while (second == first && safety < 12)
        {
            second = GetValidIntroCameraPositionAt(UnityEngine.Random.Range(0, validCount));
            safety++;
        }

        if (second == first)
            second = GetNextDifferentIntroCameraPosition(first);

        selectedCamPos1 = first;
        selectedCamPos2 = second;
    }

    private int CountValidIntroCameraPositions()
    {
        if (introCameraPositions == null)
            return 0;

        int count = 0;
        for (int i = 0; i < introCameraPositions.Count; i++)
        {
            if (introCameraPositions[i] != null)
                count++;
        }

        return count;
    }

    private Transform GetValidIntroCameraPositionAt(int validIndex)
    {
        if (introCameraPositions == null)
            return null;

        int currentValidIndex = 0;
        for (int i = 0; i < introCameraPositions.Count; i++)
        {
            Transform position = introCameraPositions[i];
            if (position == null)
                continue;

            if (currentValidIndex == validIndex)
                return position;

            currentValidIndex++;
        }

        return null;
    }

    private Transform GetNextDifferentIntroCameraPosition(Transform first)
    {
        if (introCameraPositions == null)
            return first;

        for (int i = 0; i < introCameraPositions.Count; i++)
        {
            Transform position = introCameraPositions[i];
            if (position != null && position != first)
                return position;
        }

        return first;
    }

    private void MoveIntroCameraTo(Transform target)
    {
        if (introCamera == null || target == null)
            return;

        introCamera.transform.SetPositionAndRotation(target.position, target.rotation);
        introCamera.PreviousStateIsValid = false;
    }

    private IEnumerator CompleteIntroThroughGameplayCamera()
    {
        skipAllowed = false;
        HideSkipButton();

        yield return BlinkClosed();

        HidePlayersListImmediate();

        SetGameplayBlend();

        SwitchToGameplayCamera();
        yield return WaitOrSkip(gameplayBlendDuration, false);

        PrepareGameplayCameraStartView();
        yield return null;
        PrepareGameplayCameraStartView();
        yield return new WaitForEndOfFrame();
        PrepareGameplayCameraStartView();

        yield return BlinkOpen();

        RestoreCinemachineBlend();

        if (activateLaunchMeterRootOnComplete && launchMeterRoot != null)
            launchMeterRoot.SetActive(true);

        InvokeCompletionOnce();

        introRoutine = null;
        isPlaying = false;
        skipRequested = false;
        completeIntroOnResume = false;
    }

    private void PrepareGameplayCameraStartView()
    {
        if (gameplayFollowTarget == null && gameplayCamera != null)
            gameplayFollowTarget = gameplayCamera.GetComponent<ThirdPersonFollowTarget>();

        if (gameplayFollowTarget == null)
            return;

        gameplayFollowTarget.SetLookBackMode(false);
        gameplayFollowTarget.SetLook(Vector2.zero);

        if (gameplayFollowTarget.Target != null && gameplayFollowTarget.Target.Value != null)
        {
            gameplayFollowTarget._cinemachineTargetYaw = gameplayStartYaw;
            gameplayFollowTarget._cinemachineTargetPitch = gameplayStartPitch;
            gameplayFollowTarget.TargetTeleport(false);
            ApplyGameplayCameraPivotStartView();
        }
    }

    private void ApplyGameplayCameraPivotStartView()
    {
        if (gameplayFollowTarget == null || gameplayFollowTarget.CamPivot == null)
            return;

        if (gameplayFollowTarget.Target == null || gameplayFollowTarget.Target.Value == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(gameplayStartPitch, gameplayStartYaw, 0f);

        if (gameplayFollowTarget.UseUpVector && gameplayFollowTarget.UpVector != null)
            targetRotation = Quaternion.FromToRotation(Vector3.up, gameplayFollowTarget.UpVector.up) * targetRotation;

        gameplayFollowTarget.CamPivot.SetPositionAndRotation(
            gameplayFollowTarget.Target.Value.position,
            targetRotation);

        if (gameplayCamera != null)
            gameplayCamera.PreviousStateIsValid = false;
    }

    private void SwitchToGameplayCamera()
    {
        if (gameplayCamera != null)
        {
            int targetPriority = originalGameplayPriority > inactivePriority
                ? originalGameplayPriority
                : gameplayActivePriority;

            gameplayCamera.Priority = Mathf.Max(targetPriority, inactivePriority + 1);
            gameplayCamera.PreviousStateIsValid = false;
        }

        if (introCamera != null)
        {
            introCamera.Priority = inactivePriority;
            introCamera.PreviousStateIsValid = false;
        }
    }

    private void SetGameplayBlend()
    {
        if (cinemachineBrain == null)
            return;

        cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Style.EaseInOut,
            Mathf.Max(0f, gameplayBlendDuration));
    }

    private void RestoreCinemachineBlend()
    {
        if (!hasOriginalBlend || cinemachineBrain == null)
            return;

        cinemachineBrain.m_DefaultBlend = originalDefaultBlend;
    }

    private IEnumerator BlinkClosed()
    {
        yield return FadeTo(1f, blinkCloseDuration);
        yield return WaitOrSkip(blinkClosedHoldDuration, false);
    }

    private IEnumerator BlinkOpen()
    {
        yield return FadeTo(0f, blinkOpenDuration);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        yield return FadeTo(targetAlpha, fadeDuration);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        fadeCanvasGroup.DOKill();
        Tween tween = fadeCanvasGroup
            .DOFade(targetAlpha, Mathf.Max(0f, duration))
            .SetEase(Ease.InOutQuad)
            .SetUpdate(useUnscaledTime);

        yield return WaitForTween(tween);

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    private IEnumerator PlayCamPos2PlayersListSequence()
    {
        float listDelay = Mathf.Max(0f, playersListStartDelay);
        float totalDuration = Mathf.Max(camPos2Duration, listDelay + playersListDuration);
        float elapsed = 0f;
        bool listShown = false;

        while (elapsed < totalDuration)
        {
            if (skipRequested)
                yield break;

            if (!listShown && elapsed >= listDelay)
            {
                ShowPlayersList();
                listShown = true;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        if (!listShown && !skipRequested)
            ShowPlayersList();
    }

    private void ShowPlayersList()
    {
        if (playersListPrefab != null)
            playersListPrefab.SetActive(true);
    }

    private void HidePlayersListImmediate()
    {
        if (playersListPrefab != null)
            playersListPrefab.SetActive(false);
    }

    private void ShowSkipButton()
    {
        if (skipButton == null)
            return;

        skipButton.gameObject.SetActive(true);
        skipButton.interactable = true;
    }

    private void HideSkipButton()
    {
        if (skipButton == null)
            return;

        skipButton.interactable = false;
        skipButton.gameObject.SetActive(false);
    }

    private void HideLaunchMeterRoot()
    {
        if (launchMeterRoot != null)
            launchMeterRoot.SetActive(false);
    }

    private IEnumerator WaitOrSkip(float duration, bool allowSkip)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (allowSkip && skipRequested)
                yield break;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForTween(Tween tween)
    {
        while (tween != null && tween.IsActive() && tween.IsPlaying())
            yield return null;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.DOKill();
        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = alpha > 0f;
    }

    private void KillRunningTweens()
    {
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.DOKill();
    }

    private void ForceBlackIntroPauseState()
    {
        KillRunningTweens();
        SetFadeAlpha(1f);
        HideSkipButton();
        HidePlayersListImmediate();
        skipAllowed = false;
        skipRequested = true;
    }

    private void InvokeCompletionOnce()
    {
        if (completionInvoked)
            return;

        completionInvoked = true;
        onIntroComplete?.Invoke();
        completionCallback?.Invoke();
        completionCallback = null;
    }
}
