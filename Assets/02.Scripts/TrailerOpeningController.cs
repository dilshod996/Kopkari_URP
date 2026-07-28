using System.Collections;
using System.Reflection;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the rider, cameras, and audio for the Nomadic Rivals trailer opening.
/// </summary>
public sealed class TrailerOpeningController : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private UnityEvent onOpeningFinished = new UnityEvent();

    [Header("Opening Fade")]
    [Tooltip("Assign a full-screen black UI panel with a CanvasGroup. The sequence still works when omitted.")]
    [SerializeField] private CanvasGroup openingFadeCanvasGroup;
    [SerializeField, Min(0f)] private float openingFadeOutDuration = 1.25f;
    [SerializeField] private AnimationCurve openingFadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Disables the fade panel after it becomes transparent.")]
    [SerializeField] private bool disableFadeObjectWhenClear = true;
    [Tooltip("How long the opening Intro camera remains visible before transitioning to Establishing.")]
    [SerializeField, Min(0f)] private float introCameraDuration = 1.5f;
    [SerializeField, Min(0f)] private float introToEstablishingFadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float introToEstablishingBlackHoldDuration = 0.05f;
    [SerializeField, Min(0f)] private float introToEstablishingFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float eagleRevealToSecondFadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float eagleRevealToSecondBlackHoldDuration = 0.05f;
    [SerializeField, Min(0f)] private float eagleRevealToSecondFadeOutDuration = 0.3f;
    [SerializeField] private bool fadeToBlackAtEnd = true;
    [SerializeField, Min(0f)] private float endingFadeToBlackDuration = 1f;

    [Header("Rider Animator")]
    [SerializeField] private Animator riderAnimator;
    [SerializeField, Min(0)] private int animatorLayer;
    [SerializeField] private string sittingStateName = "Srv_SitBonfireLoop";
    [SerializeField] private string standUpStateName = "Srv_SitBonfireStop";
    [SerializeField] private string searchStateName = "Search";
    [SerializeField] private string lookDistanceStateName = "LookDistance";
    [SerializeField, Min(0f)] private float animationCrossfadeDuration = 0.2f;
    [SerializeField, Range(0.5f, 1f)] private float standUpCompletionNormalizedTime = 0.9f;
    [SerializeField, Min(0f)] private float animatorStateEntryTimeout = 1f;
    [Tooltip("Starts the stand animation first, then changes camera when the rider begins to move.")]
    [SerializeField, Min(0f)] private float standUpCameraSwitchDelay = 0.12f;

    [Header("Shot Timing")]
    [SerializeField, Min(0f)] private float sittingDuration = 4f;
    [SerializeField, Min(0f)] private float eagleReactionDuration = 2f;
    [SerializeField, Min(0f)] private float standUpFallbackDuration = 3f;
    [SerializeField, Min(0f)] private float searchDuration = 3f;
    [SerializeField, Min(0f)] private float lookDistanceDuration = 4f;
    [SerializeField, Min(0f)] private float secondLookDistanceDuration = 4f;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCameraBase introCamera;
    [SerializeField] private CinemachineVirtualCameraBase establishingCamera;
    [SerializeField] private CinemachineVirtualCameraBase reactionCamera;
    [SerializeField] private CinemachineVirtualCameraBase standUpCamera;
    [SerializeField] private CinemachineVirtualCameraBase searchCamera;
    [SerializeField] private CinemachineVirtualCameraBase eagleRevealCamera;
    [SerializeField] private CinemachineVirtualCameraBase secondLookDistanceCamera;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority = 0;

    [Header("Optional Cinemachine Blends")]
    [Tooltip("When omitted, the Cinemachine Brain's existing blend settings are used.")]
    [SerializeField] private CinemachineBrain cinematicBrain;
    [SerializeField] private CinemachineBlendDefinition.Style cameraBlendStyle =
        CinemachineBlendDefinition.Style.EaseInOut;
    [Tooltip("Normally zero because the black opening overlay hides the initial camera cut.")]
    [SerializeField, Min(0f)] private float establishingCameraBlendDuration;
    [Tooltip("Keep at zero for a hard cut on the eagle sound.")]
    [SerializeField, Min(0f)] private float reactionCameraBlendDuration;
    [Tooltip("A short blend after the stand animation begins.")]
    [SerializeField, Min(0f)] private float standUpCameraBlendDuration = 0.18f;
    [Tooltip("A longer, smooth transition into the Search shot.")]
    [SerializeField, Min(0f)] private float searchCameraBlendDuration = 0.55f;
    [Tooltip("Keep short; compose the reveal camera with foreground shoulder occlusion in the scene.")]
    [SerializeField, Min(0f)] private float eagleRevealCameraBlendDuration = 0.12f;
    [SerializeField, Min(0f)] private float secondLookDistanceCameraBlendDuration = 0.12f;

    [Header("Second Look-Distance Shot Culling")]
    [Tooltip("Assign the real Main Camera that has the Cinemachine Brain, not a virtual camera.")]
    [SerializeField] private Camera renderingCamera;
    [Tooltip("Layers selected here are hidden only during the second LookDistance shot.")]
    [SerializeField] private LayerMask secondLookDistanceHiddenLayers;

    [Header("Eagle Sequence")]
    [Tooltip("Optional. Starts when the second LookDistance shot begins.")]
    [SerializeField] private TrailerEagleDropController eagleDropController;
    [Tooltip("Prevents the opening from fading out before the eagle/drop sequence finishes.")]
    [SerializeField] private bool waitForEagleSequenceBeforeFinishing = true;

    [Header("Eagle Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip eagleSfx;
    [SerializeField, Min(0f)] private float eagleSfxDelay;

    [Header("Optional Seated Head Look")]
    [Tooltip("The reaction target. Configure this Transform as a Source Object on the Multi-Aim Constraint.")]
    [SerializeField] private Transform seatedHeadLookTarget;
    [Tooltip("Assign an Animation Rigging Multi-Aim Constraint here. Kept as Component so Animation Rigging remains optional.")]
    [SerializeField] private Component seatedHeadLookConstraint;
    [SerializeField, Range(0f, 1f)] private float seatedHeadLookWeight = 1f;
    [SerializeField, Min(0f)] private float seatedHeadLookBlendDuration = 0.35f;

    private Coroutine openingCoroutine;
    private bool isPlaying;
    private CinemachineVirtualCameraBase currentCinematicCamera;

    private int sittingStateHash;
    private int standUpStateHash;
    private int searchStateHash;
    private int lookDistanceStateHash;

    private PropertyInfo headLookWeightProperty;
    private float originalHeadLookWeight;
    private bool hasHeadLookWeight;

    private CinemachineBlendDefinition originalBrainBlend;
    private bool hasStoredBrainBlend;

    private int originalRenderingCameraCullingMask;
    private bool hasStoredRenderingCameraCullingMask;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        CacheAnimatorStateHashes();
        CacheHeadLookConstraint();

        // Prime the overlay before the first rendered frame when playback starts automatically.
        if (playOnStart && openingFadeCanvasGroup != null)
        {
            SetFadeAlpha(1f, true);
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayOpening();
        }
    }

    private void OnDisable()
    {
        StopOpening();
    }

    /// <summary>Starts the trailer opening from its first shot.</summary>
    public void PlayOpening()
    {
        if (isPlaying)
        {
            Debug.LogWarning($"{nameof(TrailerOpeningController)} on '{name}' is already playing.", this);
            return;
        }

        if (riderAnimator == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' cannot play because no rider Animator is assigned.",
                this);
            return;
        }

        if (riderAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                $"Animator '{riderAnimator.name}' has no Animator Controller assigned.",
                riderAnimator);
            return;
        }

        if (animatorLayer < 0 || animatorLayer >= riderAnimator.layerCount)
        {
            Debug.LogWarning(
                $"Animator layer {animatorLayer} is invalid for Animator '{riderAnimator.name}'.",
                riderAnimator);
            return;
        }

        CacheAnimatorStateHashes();
        CacheHeadLookConstraint();
        StoreBrainBlend();
        StoreRenderingCameraCullingMask();
        PrepareOpeningFade();

        if (cinematicBrain == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' has no Cinemachine Brain assigned. " +
                "Camera priorities will still switch, but the per-shot blend durations will use the scene Brain settings.",
                this);
        }
        else if (cinematicBrain.m_CustomBlends != null)
        {
            Debug.LogWarning(
                $"Cinemachine Brain '{cinematicBrain.name}' has a Custom Blends asset. Matching entries in that " +
                "asset override this controller's per-shot blend durations.",
                cinematicBrain);
        }

        isPlaying = true;
        openingCoroutine = StartCoroutine(OpeningSequence());
    }

    /// <summary>Cancels playback and releases all cinematic overrides.</summary>
    public void StopOpening()
    {
        if (openingCoroutine != null)
        {
            StopCoroutine(openingCoroutine);
            openingCoroutine = null;
        }

        isPlaying = false;
        RestoreHeadLookWeight();
        RestoreBrainBlend();
        RestoreRenderingCameraCullingMask();
        if (eagleDropController != null)
        {
            eagleDropController.StopEagleDrop();
        }

        ClearOpeningFade();
        SetAllCamerasToInactive();
    }

    private IEnumerator OpeningSequence()
    {
        // Opening Intro shot: reveal it using the existing fade-out effect.
        SwitchCamera(introCamera, 0f);
        PlayAnimatorState(sittingStateHash, sittingStateName, 0f);
        yield return FadeOutOpeningOverlay();
        yield return WaitForDuration(introCameraDuration);

        // Fade through black so the Intro-to-Establishing camera change is invisible.
        yield return FadeToEstablishingCamera();

        // Wide establishing shot: rider remains seated by the bonfire.
        yield return WaitForDuration(sittingDuration);

        // Reaction close shot: eagle cry and optional seated head reaction.
        yield return PlayEagleReaction();

        // Begin the movement on the reaction shot, then cut/blend on the rider's motion.
        PlayAnimatorState(standUpStateHash, standUpStateName, animationCrossfadeDuration);
        yield return WaitForDuration(standUpCameraSwitchDelay);
        SwitchCamera(standUpCamera, standUpCameraBlendDuration);
        yield return WaitForAnimatorStateCompletion(
            standUpStateHash,
            standUpStateName,
            standUpCompletionNormalizedTime,
            standUpFallbackDuration);

        // Search medium shot.
        SwitchCamera(searchCamera, searchCameraBlendDuration);
        PlayAnimatorState(searchStateHash, searchStateName, animationCrossfadeDuration);
        yield return WaitForDuration(searchDuration);

        // Over-the-shoulder eagle reveal.
        SwitchCamera(eagleRevealCamera, eagleRevealCameraBlendDuration);
        PlayAnimatorState(lookDistanceStateHash, lookDistanceStateName, animationCrossfadeDuration);
        yield return WaitForDuration(lookDistanceDuration);

        // Fade through black, then replay LookDistance from the beginning on camera two.
        yield return TransitionToSecondLookDistanceShot();
        yield return WaitForDuration(secondLookDistanceDuration);

        if (waitForEagleSequenceBeforeFinishing && eagleDropController != null)
        {
            while (eagleDropController.IsPlaying)
            {
                yield return null;
            }
        }

        RestoreHeadLookWeight();
        if (fadeToBlackAtEnd)
        {
            yield return FadeOverlay(0f, 1f, endingFadeToBlackDuration, false);
        }

        RestoreRenderingCameraCullingMask();
        RestoreBrainBlend();
        openingCoroutine = null;
        isPlaying = false;
        onOpeningFinished?.Invoke();
    }

    /// <summary>
    /// Makes exactly one of this controller's assigned cinematic cameras active by priority.
    /// </summary>
    public void SwitchCamera(CinemachineVirtualCameraBase targetCamera, float blendDuration)
    {
        if (currentCinematicCamera == secondLookDistanceCamera &&
            targetCamera != secondLookDistanceCamera)
        {
            RestoreRenderingCameraCullingMask();
        }

        SetAllCamerasToInactive();

        if (targetCamera == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' tried to switch to an unassigned camera.",
                this);
            return;
        }

        ApplyCameraBlend(blendDuration);
        targetCamera.Priority = activeCameraPriority;
        currentCinematicCamera = targetCamera;
    }

    /// <summary>Crossfades the rider to a cached Animator state hash.</summary>
    public void PlayAnimatorState(int stateHash, string stateName, float transitionDuration)
    {
        if (riderAnimator == null)
        {
            Debug.LogWarning($"{nameof(TrailerOpeningController)} has no rider Animator assigned.", this);
            return;
        }

        if (stateHash == 0)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} cannot play an empty Animator state name.",
                this);
            return;
        }

        if (!riderAnimator.HasState(animatorLayer, stateHash))
        {
            Debug.LogWarning(
                $"Animator '{riderAnimator.name}' does not contain state '{stateName}' on layer {animatorLayer}.",
                riderAnimator);
            return;
        }

        riderAnimator.CrossFadeInFixedTime(
            stateHash,
            Mathf.Max(0f, transitionDuration),
            animatorLayer,
            0f);
    }

    /// <summary>
    /// Waits for a state to be entered and reach the requested normalized time.
    /// The fallback duration prevents a missing or stalled state from blocking the sequence.
    /// </summary>
    public IEnumerator WaitForAnimatorStateCompletion(
        int stateHash,
        string stateName,
        float completionNormalizedTime,
        float fallbackDuration)
    {
        if (riderAnimator == null || stateHash == 0)
        {
            yield return WaitForDuration(fallbackDuration);
            yield break;
        }

        float totalElapsed = 0f;
        float entryElapsed = 0f;
        bool enteredState = false;

        while (totalElapsed < fallbackDuration)
        {
            AnimatorStateInfo stateInfo = riderAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
            bool isRequestedState = StateInfoMatches(stateInfo, stateHash);

            if (isRequestedState)
            {
                enteredState = true;
                if (!riderAnimator.IsInTransition(animatorLayer) &&
                    stateInfo.normalizedTime >= completionNormalizedTime)
                {
                    yield break;
                }
            }
            else if (!enteredState)
            {
                entryElapsed += DeltaTime;
                if (entryElapsed >= animatorStateEntryTimeout)
                {
                    Debug.LogWarning(
                        $"Animator '{riderAnimator.name}' did not enter state '{stateName}'. " +
                        "Using the stand-up fallback duration.",
                        riderAnimator);
                    yield return WaitForDuration(Mathf.Max(0f, fallbackDuration - totalElapsed));
                    yield break;
                }
            }

            float frameDelta = DeltaTime;
            totalElapsed += frameDelta;
            yield return null;
        }

        if (fallbackDuration > 0f)
        {
            Debug.LogWarning(
                $"Animator state '{stateName}' did not reach normalized time " +
                $"{completionNormalizedTime:0.##} within {fallbackDuration:0.##} seconds. Continuing.",
                riderAnimator);
        }
    }

    private IEnumerator PlayEagleReaction()
    {
        float duration = Mathf.Max(0f, eagleReactionDuration);
        float elapsed = 0f;
        bool canUseHeadLook = CanUseHeadLook();

        if (audioSource == null || eagleSfx == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' is missing its AudioSource or Eagle SFX clip.",
                this);
        }

        // Keep the rider seated in the reaction shot until the delayed cue.
        yield return WaitForDuration(eagleSfxDelay);

        if (audioSource != null && eagleSfx != null)
        {
            audioSource.PlayOneShot(eagleSfx);
        }

        // The reaction camera cut is motivated by, and occurs on, the eagle sound.
        SwitchCamera(reactionCamera, reactionCameraBlendDuration);

        while (elapsed < duration)
        {
            if (canUseHeadLook)
            {
                SetHeadLookWeight(EvaluateHeadLookWeight(elapsed, duration));
            }

            elapsed += DeltaTime;
            yield return null;
        }

        RestoreHeadLookWeight();
    }

    private IEnumerator FadeOutOpeningOverlay()
    {
        if (openingFadeCanvasGroup == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' has no opening fade CanvasGroup assigned. " +
                "Continuing without the opening fade.",
                this);
            yield break;
        }

        yield return FadeOverlay(1f, 0f, openingFadeOutDuration, true);
        ClearOpeningFade();
    }

    private IEnumerator FadeToEstablishingCamera()
    {
        if (openingFadeCanvasGroup == null)
        {
            SwitchCamera(establishingCamera, establishingCameraBlendDuration);
            yield break;
        }

        yield return FadeOverlay(
            0f,
            1f,
            introToEstablishingFadeInDuration,
            true);

        SwitchCamera(establishingCamera, establishingCameraBlendDuration);
        yield return WaitForDuration(introToEstablishingBlackHoldDuration);

        yield return FadeOverlay(
            1f,
            0f,
            introToEstablishingFadeOutDuration,
            true);

        ClearOpeningFade();
    }

    private IEnumerator TransitionToSecondLookDistanceShot()
    {
        if (openingFadeCanvasGroup == null)
        {
            BeginSecondLookDistanceShot();
            yield break;
        }

        yield return FadeOverlay(
            0f,
            1f,
            eagleRevealToSecondFadeInDuration,
            true);

        BeginSecondLookDistanceShot();
        yield return WaitForDuration(eagleRevealToSecondBlackHoldDuration);

        yield return FadeOverlay(
            1f,
            0f,
            eagleRevealToSecondFadeOutDuration,
            true);

        ClearOpeningFade();
    }

    private void BeginSecondLookDistanceShot()
    {
        ApplySecondLookDistanceCullingMask();
        SwitchCamera(secondLookDistanceCamera, secondLookDistanceCameraBlendDuration);
        PlayAnimatorState(lookDistanceStateHash, lookDistanceStateName, animationCrossfadeDuration);

        if (eagleDropController != null)
        {
            eagleDropController.PlayEagleDrop(this);
        }
    }

    private IEnumerator FadeOverlay(
        float fromAlpha,
        float toAlpha,
        float duration,
        bool blocksRaycasts)
    {
        if (openingFadeCanvasGroup == null)
        {
            yield break;
        }

        SetFadeAlpha(fromAlpha, blocksRaycasts);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            SetFadeAlpha(toAlpha, blocksRaycasts && toAlpha > 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = openingFadeCurve != null
                ? openingFadeCurve.Evaluate(normalizedTime)
                : normalizedTime;

            openingFadeCanvasGroup.alpha =
                Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(easedTime));
            yield return null;
        }

        SetFadeAlpha(toAlpha, blocksRaycasts && toAlpha > 0f);
    }

    private void PrepareOpeningFade()
    {
        if (openingFadeCanvasGroup != null)
        {
            SetFadeAlpha(1f, true);
        }
    }

    private void SetFadeAlpha(float alpha, bool blocksRaycasts)
    {
        if (openingFadeCanvasGroup == null)
        {
            return;
        }

        if (!openingFadeCanvasGroup.gameObject.activeSelf)
        {
            openingFadeCanvasGroup.gameObject.SetActive(true);
        }

        openingFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        openingFadeCanvasGroup.interactable = false;
        openingFadeCanvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private void ClearOpeningFade()
    {
        if (openingFadeCanvasGroup == null)
        {
            return;
        }

        openingFadeCanvasGroup.alpha = 0f;
        openingFadeCanvasGroup.interactable = false;
        openingFadeCanvasGroup.blocksRaycasts = false;

        // Never disable this controller if both components were placed on the same object.
        if (disableFadeObjectWhenClear && openingFadeCanvasGroup.gameObject != gameObject)
        {
            openingFadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private float EvaluateHeadLookWeight(float elapsed, float duration)
    {
        float blendTime = Mathf.Min(seatedHeadLookBlendDuration, duration * 0.5f);
        float influence = 1f;

        if (blendTime > 0f)
        {
            if (elapsed < blendTime)
            {
                influence = elapsed / blendTime;
            }
            else if (elapsed > duration - blendTime)
            {
                influence = (duration - elapsed) / blendTime;
            }
        }

        return Mathf.Lerp(originalHeadLookWeight, seatedHeadLookWeight, Mathf.Clamp01(influence));
    }

    private bool CanUseHeadLook()
    {
        if (seatedHeadLookConstraint == null && seatedHeadLookTarget == null)
        {
            return false;
        }

        if (seatedHeadLookConstraint == null || seatedHeadLookTarget == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' needs both a seated head-look target " +
                "and a Multi-Aim Constraint for the optional reaction.",
                this);
            return false;
        }

        if (!hasHeadLookWeight)
        {
            Debug.LogWarning(
                $"Component '{seatedHeadLookConstraint.GetType().Name}' has no writable float 'weight' property. " +
                "Assign an Animation Rigging Multi-Aim Constraint.",
                seatedHeadLookConstraint);
            return false;
        }

        return true;
    }

    private void CacheHeadLookConstraint()
    {
        headLookWeightProperty = null;
        hasHeadLookWeight = false;

        if (seatedHeadLookConstraint == null)
        {
            return;
        }

        headLookWeightProperty = seatedHeadLookConstraint.GetType().GetProperty(
            "weight",
            BindingFlags.Instance | BindingFlags.Public);

        if (headLookWeightProperty == null ||
            headLookWeightProperty.PropertyType != typeof(float) ||
            !headLookWeightProperty.CanRead ||
            !headLookWeightProperty.CanWrite)
        {
            headLookWeightProperty = null;
            return;
        }

        originalHeadLookWeight = (float)headLookWeightProperty.GetValue(seatedHeadLookConstraint, null);
        hasHeadLookWeight = true;
    }

    private void SetHeadLookWeight(float weight)
    {
        if (!hasHeadLookWeight || seatedHeadLookConstraint == null)
        {
            return;
        }

        headLookWeightProperty.SetValue(
            seatedHeadLookConstraint,
            Mathf.Clamp01(weight),
            null);
    }

    private void RestoreHeadLookWeight()
    {
        if (hasHeadLookWeight)
        {
            SetHeadLookWeight(originalHeadLookWeight);
        }
    }

    private void SetAllCamerasToInactive()
    {
        SetCameraPriority(currentCinematicCamera, inactiveCameraPriority);
        currentCinematicCamera = null;

        SetCameraPriority(introCamera, inactiveCameraPriority);
        SetCameraPriority(establishingCamera, inactiveCameraPriority);
        SetCameraPriority(reactionCamera, inactiveCameraPriority);
        SetCameraPriority(standUpCamera, inactiveCameraPriority);
        SetCameraPriority(searchCamera, inactiveCameraPriority);
        SetCameraPriority(eagleRevealCamera, inactiveCameraPriority);
        SetCameraPriority(secondLookDistanceCamera, inactiveCameraPriority);
    }

    private static void SetCameraPriority(CinemachineVirtualCameraBase camera, int priority)
    {
        if (camera != null)
        {
            camera.Priority = priority;
        }
    }

    private void ApplyCameraBlend(float duration)
    {
        if (cinematicBrain != null)
        {
            CinemachineBlendDefinition.Style style = duration <= 0f
                ? CinemachineBlendDefinition.Style.Cut
                : cameraBlendStyle;

            cinematicBrain.m_DefaultBlend =
                new CinemachineBlendDefinition(style, Mathf.Max(0f, duration));
        }
    }

    private void StoreBrainBlend()
    {
        if (cinematicBrain != null)
        {
            originalBrainBlend = cinematicBrain.m_DefaultBlend;
            hasStoredBrainBlend = true;
        }
    }

    private void RestoreBrainBlend()
    {
        if (cinematicBrain != null && hasStoredBrainBlend)
        {
            cinematicBrain.m_DefaultBlend = originalBrainBlend;
        }

        hasStoredBrainBlend = false;
    }

    private void StoreRenderingCameraCullingMask()
    {
        if (renderingCamera == null && cinematicBrain != null)
        {
            renderingCamera = cinematicBrain.GetComponent<Camera>();
        }

        if (renderingCamera == null)
        {
            hasStoredRenderingCameraCullingMask = false;
            return;
        }

        originalRenderingCameraCullingMask = renderingCamera.cullingMask;
        hasStoredRenderingCameraCullingMask = true;
    }

    private void ApplySecondLookDistanceCullingMask()
    {
        if (secondLookDistanceHiddenLayers.value == 0)
        {
            return;
        }

        if (!hasStoredRenderingCameraCullingMask || renderingCamera == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerOpeningController)} on '{name}' cannot hide layers for the second " +
                "LookDistance shot because no rendering Camera is assigned.",
                this);
            return;
        }

        renderingCamera.cullingMask =
            originalRenderingCameraCullingMask & ~secondLookDistanceHiddenLayers.value;
    }

    private void RestoreRenderingCameraCullingMask()
    {
        if (hasStoredRenderingCameraCullingMask && renderingCamera != null)
        {
            renderingCamera.cullingMask = originalRenderingCameraCullingMask;
        }

        hasStoredRenderingCameraCullingMask = false;
    }

    private void CacheAnimatorStateHashes()
    {
        sittingStateHash = ResolveAnimatorStateHash(sittingStateName);
        standUpStateHash = ResolveAnimatorStateHash(standUpStateName);
        searchStateHash = ResolveAnimatorStateHash(searchStateName);
        lookDistanceStateHash = ResolveAnimatorStateHash(lookDistanceStateName);
    }

    private int ResolveAnimatorStateHash(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return 0;
        }

        int configuredHash = Animator.StringToHash(stateName);
        if (riderAnimator == null ||
            animatorLayer < 0 ||
            animatorLayer >= riderAnimator.layerCount ||
            riderAnimator.HasState(animatorLayer, configuredHash))
        {
            return configuredHash;
        }

        string fullPath = $"{riderAnimator.GetLayerName(animatorLayer)}.{stateName}";
        int fullPathHash = Animator.StringToHash(fullPath);
        return riderAnimator.HasState(animatorLayer, fullPathHash) ? fullPathHash : configuredHash;
    }

    private static bool StateInfoMatches(AnimatorStateInfo stateInfo, int stateHash)
    {
        return stateInfo.shortNameHash == stateHash || stateInfo.fullPathHash == stateHash;
    }

    private IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);

        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            yield return null;
        }
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void OnValidate()
    {
        animatorLayer = Mathf.Max(0, animatorLayer);
        if (activeCameraPriority <= inactiveCameraPriority)
        {
            activeCameraPriority = inactiveCameraPriority + 1;
        }

        animationCrossfadeDuration = Mathf.Max(0f, animationCrossfadeDuration);
        animatorStateEntryTimeout = Mathf.Max(0f, animatorStateEntryTimeout);
        standUpCameraSwitchDelay = Mathf.Max(0f, standUpCameraSwitchDelay);
        openingFadeOutDuration = Mathf.Max(0f, openingFadeOutDuration);
        introCameraDuration = Mathf.Max(0f, introCameraDuration);
        introToEstablishingFadeInDuration = Mathf.Max(0f, introToEstablishingFadeInDuration);
        introToEstablishingBlackHoldDuration =
            Mathf.Max(0f, introToEstablishingBlackHoldDuration);
        introToEstablishingFadeOutDuration = Mathf.Max(0f, introToEstablishingFadeOutDuration);
        eagleRevealToSecondFadeInDuration =
            Mathf.Max(0f, eagleRevealToSecondFadeInDuration);
        eagleRevealToSecondBlackHoldDuration =
            Mathf.Max(0f, eagleRevealToSecondBlackHoldDuration);
        eagleRevealToSecondFadeOutDuration =
            Mathf.Max(0f, eagleRevealToSecondFadeOutDuration);
        endingFadeToBlackDuration = Mathf.Max(0f, endingFadeToBlackDuration);
        sittingDuration = Mathf.Max(0f, sittingDuration);
        eagleReactionDuration = Mathf.Max(0f, eagleReactionDuration);
        standUpFallbackDuration = Mathf.Max(0f, standUpFallbackDuration);
        searchDuration = Mathf.Max(0f, searchDuration);
        lookDistanceDuration = Mathf.Max(0f, lookDistanceDuration);
        secondLookDistanceDuration = Mathf.Max(0f, secondLookDistanceDuration);
        eagleSfxDelay = Mathf.Max(0f, eagleSfxDelay);
        seatedHeadLookBlendDuration = Mathf.Max(0f, seatedHeadLookBlendDuration);
    }
}
