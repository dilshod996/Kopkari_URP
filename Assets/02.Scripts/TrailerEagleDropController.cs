using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the eagle flight, tracking shot, ulak release, and drop shot for the trailer.
/// </summary>
public sealed class TrailerEagleDropController : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private UnityEvent onEagleSequenceStarted = new UnityEvent();
    [SerializeField] private UnityEvent onUlakReleased = new UnityEvent();
    [SerializeField] private UnityEvent onEagleSequenceFinished = new UnityEvent();

    [Header("Eagle")]
    [Tooltip("Defaults to this component's Transform when omitted.")]
    [SerializeField] private Transform eagleRoot;
    [SerializeField] private Animator eagleAnimator;
    [SerializeField] private string flightStateName = "Fly";
    [SerializeField, Min(0f)] private float animationCrossfadeDuration = 0.2f;

    [Header("Flight Waypoints")]
    [SerializeField] private Transform[] flightWaypoints;
    [SerializeField, Min(0.01f)] private float flightSpeed = 12f;
    [SerializeField, Min(0f)] private float waypointReachDistance = 0.25f;
    [Tooltip("Smooths the velocity inherited by the ulak without slowing the eagle between waypoints.")]
    [SerializeField, Min(0.01f)] private float movementSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float rotationSpeed = 240f;
    [Tooltip("Higher values react faster. Lower values create wider, more cinematic turns.")]
    [SerializeField, Min(0.01f)] private float rotationDirectionSmoothTime = 0.35f;
    [SerializeField, Min(0.01f)] private float rotationResponsiveness = 5f;
    [SerializeField, Range(0f, 30f)] private float maximumBankAngle = 10f;
    [SerializeField, Min(0.01f)] private float bankSmoothTime = 0.25f;
    [SerializeField] private bool resetToFirstWaypointOnPlay = true;
    [Tooltip("Faces the eagle toward the next waypoint before it becomes visible.")]
    [SerializeField] private bool alignRotationToPathOnPlay = true;
    [SerializeField] private bool continueFacingFlightDirectionAtPathEnd = true;

    [Header("Ulak")]
    [SerializeField] private Transform ulak;
    [Tooltip("Optional eagle socket used to hold the ulak before release.")]
    [SerializeField] private Transform ulakCarrySocket;
    [SerializeField] private Rigidbody ulakRigidbody;
    [SerializeField] private Collider ulakCollider;
    [Tooltip("Seconds after PlayEagleDrop is called. Defaults to the requested three-second release.")]
    [SerializeField, Min(0f)] private float ulakReleaseDelay = 3f;
    [SerializeField] private Vector3 releaseVelocityOffset;
    [SerializeField] private bool inheritEagleVelocity = true;
    [Tooltip("1 uses normal gravity; lower values create a slower cinematic fall.")]
    [SerializeField, Range(0f, 1f)] private float ulakGravityScale = 0.45f;
    [Tooltip("Maximum speed along the gravity direction after release.")]
    [SerializeField, Min(0.1f)] private float maximumUlakFallSpeed = 7f;
    [SerializeField] private bool enableUlakColliderOnRelease = true;
    [SerializeField] private bool snapUlakToCarrySocketOnPlay = true;
    [SerializeField] private bool restoreUlakWhenStopped = true;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCameraBase eagleTrackingCamera;
    [SerializeField] private CinemachineVirtualCameraBase ulakDropCamera;
    [Tooltip("Priority used until the opening controller intentionally activates an eagle camera.")]
    [SerializeField] private int inactiveCameraPriority;
    [Tooltip("Use a stable Transform such as EagleParent, never an animated bone or mesh Transform.")]
    [SerializeField] private Transform eagleCameraFollowTarget;
    [Tooltip("Optional stable framing target. Defaults to Eagle Root.")]
    [SerializeField] private Transform eagleCameraLookAtTarget;
    [SerializeField] private bool overrideTrackingCameraTargetsWhilePlaying = true;
    [Tooltip("When controller targets are empty, snapshots the virtual camera's targets under Eagle Root. " +
             "This preserves framing without following animated bones.")]
    [SerializeField] private bool createStableCameraTargetProxies = true;
    [Tooltip("Allows the second LookDistance camera to establish the eagle before tracking begins.")]
    [SerializeField, Min(0f)] private float eagleTrackingCameraDelay = 1f;
    [SerializeField, Min(0f)] private float eagleTrackingCameraBlendDuration = 0.35f;
    [SerializeField] private bool fadeToEagleTrackingCamera = true;
    [SerializeField, Min(0f)] private float eagleCameraFadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float eagleCameraBlackHoldDuration = 0.05f;
    [SerializeField, Min(0f)] private float eagleCameraFadeOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float ulakDropCameraBlendDuration = 0.15f;
    [SerializeField, Min(0f)] private float postReleaseHoldDuration = 2.5f;
    [Tooltip("Keeps the inactive tracking camera updated before it becomes live, preventing a first-frame jump.")]
    [SerializeField] private bool prewarmEagleTrackingCamera = true;

    private Coroutine sequenceCoroutine;
    private Coroutine trackingCameraTransitionCoroutine;
    private TrailerOpeningController openingCameraController;
    private Vector3 eagleVelocity;
    private Vector3 eagleVelocitySmoothing;
    private Vector3 smoothedFlightDirection;
    private Vector3 flightDirectionSmoothing;
    private float currentBankAngle;
    private float bankSmoothingVelocity;
    private int currentWaypointIndex;
    private int flightStateHash;
    private bool isPlaying;
    private bool ulakReleased;

    private Transform originalUlakParent;
    private Vector3 originalUlakLocalPosition;
    private Quaternion originalUlakLocalRotation;
    private bool originalUlakRigidbodyIsKinematic;
    private bool originalUlakRigidbodyUseGravity;
    private bool originalUlakRigidbodyDetectCollisions;
    private RigidbodyInterpolation originalUlakRigidbodyInterpolation;
    private bool originalUlakColliderEnabled;
    private bool hasStoredUlakState;

    private bool originalAnimatorApplyRootMotion;
    private bool hasStoredAnimatorRootMotion;
    private CinemachineVirtualCameraBase.StandbyUpdateMode originalTrackingCameraStandbyMode;
    private bool hasStoredTrackingCameraStandbyMode;
    private Transform originalTrackingCameraFollow;
    private Transform originalTrackingCameraLookAt;
    private bool hasStoredTrackingCameraTargets;
    private Transform runtimeCameraFollowProxy;
    private Transform runtimeCameraLookAtProxy;

    public bool IsPlaying => isPlaying;
    public bool HasReleasedUlak => ulakReleased;

    private void Awake()
    {
        if (eagleRoot == null)
        {
            eagleRoot = transform;
        }

        if (ulak == null && ulakRigidbody != null)
        {
            ulak = ulakRigidbody.transform;
        }

        SetEagleCamerasInactive();
        CacheFlightStateHash();
    }

    private void OnDisable()
    {
        StopEagleDrop();
    }

    private void FixedUpdate()
    {
        ApplyCinematicUlakGravity();
    }

    /// <summary>
    /// Starts the eagle sequence. The opening controller is used to coordinate camera priorities and blends.
    /// </summary>
    public void PlayEagleDrop(TrailerOpeningController cameraController)
    {
        if (isPlaying)
        {
            Debug.LogWarning($"{nameof(TrailerEagleDropController)} on '{name}' is already playing.", this);
            return;
        }

        if (eagleRoot == null)
        {
            eagleRoot = transform;
        }

        if (hasStoredTrackingCameraTargets || hasStoredTrackingCameraStandbyMode)
        {
            RestoreTrackingCameraRuntimeState(true);
        }

        openingCameraController = cameraController;
        SetEagleCamerasInactive();
        CacheFlightStateHash();

        if (hasStoredUlakState)
        {
            RestoreUlakState();
        }

        StoreUlakState();
        PrepareUlakForCarry();
        PrepareFlightPath();
        PrepareAnimatorForScriptedMovement();
        PrepareTrackingCamera();
        PlayFlightAnimation();

        isPlaying = true;
        ulakReleased = false;
        sequenceCoroutine = StartCoroutine(EagleSequence());
        onEagleSequenceStarted?.Invoke();
    }

    /// <summary>
    /// Stops the sequence and restores the carried ulak when configured to do so.
    /// </summary>
    public void StopEagleDrop()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (trackingCameraTransitionCoroutine != null)
        {
            StopCoroutine(trackingCameraTransitionCoroutine);
            trackingCameraTransitionCoroutine = null;
        }

        if (restoreUlakWhenStopped)
        {
            RestoreUlakState();
        }

        RestoreAnimatorRootMotion();
        RestoreTrackingCameraRuntimeState(true);
        isPlaying = false;
        openingCameraController = null;
        eagleVelocity = Vector3.zero;
        SetEagleCamerasInactive();
    }

    private IEnumerator EagleSequence()
    {
        float elapsed = 0f;
        bool trackingCameraActivated = false;

        while (!ulakReleased)
        {
            UpdateFlight();

            if (!trackingCameraActivated && elapsed >= eagleTrackingCameraDelay)
            {
                BeginTrackingCameraTransition();
                trackingCameraActivated = true;
            }

            if (elapsed >= ulakReleaseDelay &&
                trackingCameraTransitionCoroutine == null)
            {
                ReleaseUlak();
                break;
            }

            elapsed += DeltaTime;
            yield return null;
        }

        float postReleaseElapsed = 0f;
        while (postReleaseElapsed < postReleaseHoldDuration)
        {
            UpdateFlight();
            postReleaseElapsed += DeltaTime;
            yield return null;
        }

        sequenceCoroutine = null;
        isPlaying = false;
        RestoreAnimatorRootMotion();
        RestoreTrackingCameraRuntimeState(ulakDropCamera != null);
        openingCameraController = null;
        onEagleSequenceFinished?.Invoke();
    }

    private void PrepareFlightPath()
    {
        eagleVelocity = Vector3.zero;
        eagleVelocitySmoothing = Vector3.zero;
        smoothedFlightDirection = Vector3.zero;
        flightDirectionSmoothing = Vector3.zero;
        currentBankAngle = 0f;
        bankSmoothingVelocity = 0f;
        currentWaypointIndex = 0;

        if (flightWaypoints == null || flightWaypoints.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(TrailerEagleDropController)} on '{name}' has no flight waypoints. " +
                "Release timing and cameras will still run.",
                this);
            return;
        }

        if (resetToFirstWaypointOnPlay)
        {
            int firstWaypointIndex = GetNextValidWaypointIndex(0);
            if (firstWaypointIndex >= 0)
            {
                Transform firstWaypoint = flightWaypoints[firstWaypointIndex];
                eagleRoot.position = firstWaypoint.position;
                if (!alignRotationToPathOnPlay)
                {
                    eagleRoot.rotation = firstWaypoint.rotation;
                }

                currentWaypointIndex = GetNextValidWaypointIndex(firstWaypointIndex + 1);
            }
        }

        if (alignRotationToPathOnPlay)
        {
            AlignRotationToCurrentPathDirection();
        }
    }

    private void AlignRotationToCurrentPathDirection()
    {
        Transform targetWaypoint = GetCurrentWaypoint();
        if (targetWaypoint == null)
        {
            return;
        }

        Vector3 initialDirection = targetWaypoint.position - eagleRoot.position;
        if (initialDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        initialDirection.Normalize();
        smoothedFlightDirection = initialDirection;
        flightDirectionSmoothing = Vector3.zero;
        currentBankAngle = 0f;
        bankSmoothingVelocity = 0f;
        eagleRoot.rotation = Quaternion.LookRotation(initialDirection, Vector3.up);
    }

    private void UpdateFlight()
    {
        Transform targetWaypoint = GetCurrentWaypoint();
        if (targetWaypoint == null)
        {
            if (!continueFacingFlightDirectionAtPathEnd)
            {
                eagleVelocity = Vector3.zero;
            }

            return;
        }

        Vector3 previousPosition = eagleRoot.position;
        float deltaTime = DeltaTime;
        eagleRoot.position = Vector3.MoveTowards(
            eagleRoot.position,
            targetWaypoint.position,
            flightSpeed * deltaTime);

        Vector3 frameMovement = eagleRoot.position - previousPosition;
        if (frameMovement.sqrMagnitude > 0.000001f && deltaTime > 0f)
        {
            Vector3 frameVelocity = frameMovement / deltaTime;
            eagleVelocity = Vector3.SmoothDamp(
                eagleVelocity,
                frameVelocity,
                ref eagleVelocitySmoothing,
                movementSmoothTime,
                Mathf.Infinity,
                deltaTime);

            UpdateFlightRotation(frameVelocity.normalized, deltaTime);
        }

        if ((eagleRoot.position - targetWaypoint.position).sqrMagnitude <=
            waypointReachDistance * waypointReachDistance)
        {
            currentWaypointIndex = GetNextValidWaypointIndex(currentWaypointIndex + 1);
        }
    }

    private void ReleaseUlak()
    {
        ulakReleased = true;

        if (ulak == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerEagleDropController)} on '{name}' cannot release the ulak because it is unassigned.",
                this);
        }
        else
        {
            ulak.SetParent(null, true);
        }

        if (ulakCollider != null && enableUlakColliderOnRelease)
        {
            ulakCollider.enabled = true;
        }

        if (ulakRigidbody != null)
        {
            ulakRigidbody.isKinematic = false;
            ulakRigidbody.useGravity = true;
            ulakRigidbody.detectCollisions =
                enableUlakColliderOnRelease || originalUlakRigidbodyDetectCollisions;
            ulakRigidbody.interpolation = originalUlakRigidbodyInterpolation;
            ulakRigidbody.velocity =
                (inheritEagleVelocity ? eagleVelocity : Vector3.zero) + releaseVelocityOffset;
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(TrailerEagleDropController)} on '{name}' has no ulak Rigidbody. " +
                "The ulak was detached but will not receive physics from this controller.",
                this);
        }

        SwitchCamera(ulakDropCamera, ulakDropCameraBlendDuration);
        onUlakReleased?.Invoke();
    }

    private void ApplyCinematicUlakGravity()
    {
        if (!ulakReleased ||
            ulakRigidbody == null ||
            ulakRigidbody.isKinematic ||
            !ulakRigidbody.useGravity)
        {
            return;
        }

        Vector3 gravity = Physics.gravity;
        if (gravity.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        // Rigidbody gravity is already applied by Unity. This opposing acceleration
        // reduces only this ulak's effective gravity to the configured scale.
        if (ulakGravityScale < 1f)
        {
            ulakRigidbody.AddForce(
                -gravity * (1f - ulakGravityScale),
                ForceMode.Acceleration);
        }

        Vector3 gravityDirection = gravity.normalized;
        float downwardSpeed = Vector3.Dot(ulakRigidbody.velocity, gravityDirection);
        if (downwardSpeed > maximumUlakFallSpeed)
        {
            ulakRigidbody.velocity -=
                gravityDirection * (downwardSpeed - maximumUlakFallSpeed);
        }
    }

    private void PrepareUlakForCarry()
    {
        if (ulak != null && ulakCarrySocket != null)
        {
            bool invalidCarrySocket =
                ulakCarrySocket == ulak || ulakCarrySocket.IsChildOf(ulak);

            if (invalidCarrySocket)
            {
                Debug.LogWarning(
                    $"{nameof(TrailerEagleDropController)} on '{name}' has the ulak itself, or one of " +
                    "its children, assigned as Ulak Carry Socket. Assign a separate Transform under " +
                    "the eagle's claw. The ulak pose will not be changed.",
                    this);
            }
            else
            {
                ulak.SetParent(ulakCarrySocket, !snapUlakToCarrySocketOnPlay);

                if (snapUlakToCarrySocketOnPlay)
                {
                    ulak.localPosition = Vector3.zero;
                    ulak.localRotation = Quaternion.identity;
                }
            }
        }

        if (ulakRigidbody != null)
        {
            ulakRigidbody.velocity = Vector3.zero;
            ulakRigidbody.angularVelocity = Vector3.zero;
            ulakRigidbody.useGravity = false;
            ulakRigidbody.isKinematic = true;
            ulakRigidbody.detectCollisions = false;
            ulakRigidbody.interpolation = RigidbodyInterpolation.None;
        }

        if (ulakCollider != null && enableUlakColliderOnRelease)
        {
            ulakCollider.enabled = false;
        }
    }

    private void StoreUlakState()
    {
        hasStoredUlakState = false;

        if (ulak == null)
        {
            return;
        }

        originalUlakParent = ulak.parent;
        originalUlakLocalPosition = ulak.localPosition;
        originalUlakLocalRotation = ulak.localRotation;

        if (ulakRigidbody != null)
        {
            originalUlakRigidbodyIsKinematic = ulakRigidbody.isKinematic;
            originalUlakRigidbodyUseGravity = ulakRigidbody.useGravity;
            originalUlakRigidbodyDetectCollisions = ulakRigidbody.detectCollisions;
            originalUlakRigidbodyInterpolation = ulakRigidbody.interpolation;
        }

        if (ulakCollider != null)
        {
            originalUlakColliderEnabled = ulakCollider.enabled;
        }

        hasStoredUlakState = true;
    }

    private void RestoreUlakState()
    {
        if (!hasStoredUlakState || ulak == null)
        {
            return;
        }

        if (ulakRigidbody != null)
        {
            ulakRigidbody.velocity = Vector3.zero;
            ulakRigidbody.angularVelocity = Vector3.zero;
            ulakRigidbody.isKinematic = true;
            ulakRigidbody.useGravity = false;
        }

        ulak.SetParent(originalUlakParent, false);
        ulak.localPosition = originalUlakLocalPosition;
        ulak.localRotation = originalUlakLocalRotation;

        if (ulakRigidbody != null)
        {
            ulakRigidbody.isKinematic = originalUlakRigidbodyIsKinematic;
            ulakRigidbody.useGravity = originalUlakRigidbodyUseGravity;
            ulakRigidbody.detectCollisions = originalUlakRigidbodyDetectCollisions;
            ulakRigidbody.interpolation = originalUlakRigidbodyInterpolation;
        }

        if (ulakCollider != null)
        {
            ulakCollider.enabled = originalUlakColliderEnabled;
        }

        hasStoredUlakState = false;
        ulakReleased = false;
    }

    private void PrepareAnimatorForScriptedMovement()
    {
        if (eagleAnimator == null)
        {
            return;
        }

        originalAnimatorApplyRootMotion = eagleAnimator.applyRootMotion;
        hasStoredAnimatorRootMotion = true;
        eagleAnimator.applyRootMotion = false;
    }

    private void RestoreAnimatorRootMotion()
    {
        if (hasStoredAnimatorRootMotion && eagleAnimator != null)
        {
            eagleAnimator.applyRootMotion = originalAnimatorApplyRootMotion;
        }

        hasStoredAnimatorRootMotion = false;
    }

    private void PrepareTrackingCamera()
    {
        if (eagleTrackingCamera == null)
        {
            return;
        }

        originalTrackingCameraFollow = eagleTrackingCamera.Follow;
        originalTrackingCameraLookAt = eagleTrackingCamera.LookAt;
        hasStoredTrackingCameraTargets = true;

        if (overrideTrackingCameraTargetsWhilePlaying)
        {
            Transform stableFollowTarget = eagleCameraFollowTarget;
            Transform stableLookAtTarget = eagleCameraLookAtTarget;

            if (stableFollowTarget == null && createStableCameraTargetProxies)
            {
                runtimeCameraFollowProxy = CreateStableCameraTargetProxy(
                    originalTrackingCameraFollow,
                    "EagleCameraFollowProxy");
                stableFollowTarget = runtimeCameraFollowProxy;
            }

            if (stableLookAtTarget == null && createStableCameraTargetProxies)
            {
                runtimeCameraLookAtProxy = CreateStableCameraTargetProxy(
                    originalTrackingCameraLookAt,
                    "EagleCameraLookAtProxy");
                stableLookAtTarget = runtimeCameraLookAtProxy;
            }

            stableFollowTarget = stableFollowTarget != null ? stableFollowTarget : eagleRoot;
            stableLookAtTarget =
                stableLookAtTarget != null ? stableLookAtTarget : stableFollowTarget;

            eagleTrackingCamera.Follow = stableFollowTarget;
            eagleTrackingCamera.LookAt = stableLookAtTarget;
        }

        if (!prewarmEagleTrackingCamera)
        {
            return;
        }

        originalTrackingCameraStandbyMode = eagleTrackingCamera.m_StandbyUpdate;
        hasStoredTrackingCameraStandbyMode = true;
        eagleTrackingCamera.m_StandbyUpdate =
            CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
        eagleTrackingCamera.PreviousStateIsValid = false;
    }

    private void RestoreTrackingCameraRuntimeState(bool restoreTargets)
    {
        if (restoreTargets && hasStoredTrackingCameraTargets && eagleTrackingCamera != null)
        {
            eagleTrackingCamera.Follow = originalTrackingCameraFollow;
            eagleTrackingCamera.LookAt = originalTrackingCameraLookAt;
        }

        if (restoreTargets)
        {
            hasStoredTrackingCameraTargets = false;
            DestroyRuntimeCameraTarget(ref runtimeCameraFollowProxy);
            DestroyRuntimeCameraTarget(ref runtimeCameraLookAtProxy);
        }

        if (hasStoredTrackingCameraStandbyMode && eagleTrackingCamera != null)
        {
            eagleTrackingCamera.m_StandbyUpdate = originalTrackingCameraStandbyMode;
        }

        hasStoredTrackingCameraStandbyMode = false;
    }

    private Transform CreateStableCameraTargetProxy(Transform source, string proxyName)
    {
        if (eagleRoot == null)
        {
            return source;
        }

        GameObject proxyObject = new GameObject(proxyName);
        proxyObject.hideFlags = HideFlags.HideAndDontSave;

        Transform proxy = proxyObject.transform;
        if (source != null)
        {
            proxy.SetPositionAndRotation(source.position, source.rotation);
        }
        else
        {
            proxy.SetPositionAndRotation(eagleRoot.position, eagleRoot.rotation);
        }

        proxy.SetParent(eagleRoot, true);
        return proxy;
    }

    private static void DestroyRuntimeCameraTarget(ref Transform target)
    {
        if (target != null)
        {
            Object.Destroy(target.gameObject);
            target = null;
        }
    }

    private void SwitchCamera(CinemachineVirtualCameraBase targetCamera, float blendDuration)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerEagleDropController)} on '{name}' tried to use an unassigned camera.",
                this);
            return;
        }

        if (openingCameraController == null)
        {
            Debug.LogWarning(
                $"{nameof(TrailerEagleDropController)} needs to be started by " +
                $"{nameof(TrailerOpeningController)} to coordinate camera priorities.",
                this);
            return;
        }

        openingCameraController.SwitchCamera(targetCamera, blendDuration);
    }

    private void BeginTrackingCameraTransition()
    {
        if (trackingCameraTransitionCoroutine != null)
        {
            return;
        }

        if (!fadeToEagleTrackingCamera || openingCameraController == null)
        {
            SwitchCamera(eagleTrackingCamera, eagleTrackingCameraBlendDuration);
            return;
        }

        trackingCameraTransitionCoroutine =
            StartCoroutine(TransitionToEagleTrackingCamera());
    }

    private IEnumerator TransitionToEagleTrackingCamera()
    {
        yield return openingCameraController.FadeThroughBlackToCamera(
            eagleTrackingCamera,
            0f,
            eagleCameraFadeInDuration,
            eagleCameraBlackHoldDuration,
            eagleCameraFadeOutDuration);

        trackingCameraTransitionCoroutine = null;
    }

    private void UpdateFlightRotation(
        Vector3 desiredDirection,
        float deltaTime)
    {
        if (smoothedFlightDirection.sqrMagnitude < 0.000001f)
        {
            smoothedFlightDirection = desiredDirection;
        }

        smoothedFlightDirection = Vector3.SmoothDamp(
            smoothedFlightDirection,
            desiredDirection,
            ref flightDirectionSmoothing,
            rotationDirectionSmoothTime,
            Mathf.Infinity,
            deltaTime).normalized;

        float signedTurnAngle = Vector3.SignedAngle(
            smoothedFlightDirection,
            desiredDirection,
            Vector3.up);
        float desiredBankAngle =
            -Mathf.Clamp(signedTurnAngle / 45f, -1f, 1f) * maximumBankAngle;

        currentBankAngle = Mathf.SmoothDamp(
            currentBankAngle,
            desiredBankAngle,
            ref bankSmoothingVelocity,
            bankSmoothTime,
            Mathf.Infinity,
            deltaTime);

        Quaternion facingRotation =
            Quaternion.LookRotation(smoothedFlightDirection, Vector3.up);
        Quaternion bankRotation =
            Quaternion.AngleAxis(currentBankAngle, Vector3.forward);
        Quaternion desiredRotation = facingRotation * bankRotation;

        float easedRotationAmount =
            1f - Mathf.Exp(-rotationResponsiveness * deltaTime);
        Quaternion easedRotation = Quaternion.Slerp(
            eagleRoot.rotation,
            desiredRotation,
            easedRotationAmount);

        eagleRoot.rotation = Quaternion.RotateTowards(
            eagleRoot.rotation,
            easedRotation,
            rotationSpeed * deltaTime);
    }

    private void SetEagleCamerasInactive()
    {
        if (eagleTrackingCamera != null)
        {
            eagleTrackingCamera.Priority = inactiveCameraPriority;
        }

        if (ulakDropCamera != null)
        {
            ulakDropCamera.Priority = inactiveCameraPriority;
        }
    }

    private void PlayFlightAnimation()
    {
        if (eagleAnimator == null || flightStateHash == 0)
        {
            return;
        }

        if (eagleAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                $"Animator '{eagleAnimator.name}' has no Animator Controller assigned.",
                eagleAnimator);
            return;
        }

        if (!eagleAnimator.HasState(0, flightStateHash))
        {
            Debug.LogWarning(
                $"Animator '{eagleAnimator.name}' does not contain state '{flightStateName}' on layer 0.",
                eagleAnimator);
            return;
        }

        eagleAnimator.CrossFadeInFixedTime(
            flightStateHash,
            animationCrossfadeDuration,
            0,
            0f);
    }

    private void CacheFlightStateHash()
    {
        if (string.IsNullOrWhiteSpace(flightStateName))
        {
            flightStateHash = 0;
            return;
        }

        int configuredHash = Animator.StringToHash(flightStateName);
        if (eagleAnimator == null ||
            eagleAnimator.runtimeAnimatorController == null ||
            eagleAnimator.HasState(0, configuredHash))
        {
            flightStateHash = configuredHash;
            return;
        }

        int fullPathHash = Animator.StringToHash($"{eagleAnimator.GetLayerName(0)}.{flightStateName}");
        flightStateHash = eagleAnimator.HasState(0, fullPathHash)
            ? fullPathHash
            : configuredHash;
    }

    private Transform GetCurrentWaypoint()
    {
        if (flightWaypoints == null ||
            currentWaypointIndex < 0 ||
            currentWaypointIndex >= flightWaypoints.Length)
        {
            return null;
        }

        if (flightWaypoints[currentWaypointIndex] != null)
        {
            return flightWaypoints[currentWaypointIndex];
        }

        currentWaypointIndex = GetNextValidWaypointIndex(currentWaypointIndex + 1);
        return currentWaypointIndex >= 0 && currentWaypointIndex < flightWaypoints.Length
            ? flightWaypoints[currentWaypointIndex]
            : null;
    }

    private int GetNextValidWaypointIndex(int startIndex)
    {
        if (flightWaypoints == null)
        {
            return -1;
        }

        for (int i = Mathf.Max(0, startIndex); i < flightWaypoints.Length; i++)
        {
            if (flightWaypoints[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void OnValidate()
    {
        animationCrossfadeDuration = Mathf.Max(0f, animationCrossfadeDuration);
        flightSpeed = Mathf.Max(0.01f, flightSpeed);
        waypointReachDistance = Mathf.Max(0f, waypointReachDistance);
        movementSmoothTime = Mathf.Max(0.01f, movementSmoothTime);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        rotationDirectionSmoothTime = Mathf.Max(0.01f, rotationDirectionSmoothTime);
        rotationResponsiveness = Mathf.Max(0.01f, rotationResponsiveness);
        bankSmoothTime = Mathf.Max(0.01f, bankSmoothTime);
        ulakReleaseDelay = Mathf.Max(0f, ulakReleaseDelay);
        maximumUlakFallSpeed = Mathf.Max(0.1f, maximumUlakFallSpeed);
        eagleTrackingCameraDelay = Mathf.Max(0f, eagleTrackingCameraDelay);
        eagleTrackingCameraBlendDuration = Mathf.Max(0f, eagleTrackingCameraBlendDuration);
        eagleCameraFadeInDuration = Mathf.Max(0f, eagleCameraFadeInDuration);
        eagleCameraBlackHoldDuration = Mathf.Max(0f, eagleCameraBlackHoldDuration);
        eagleCameraFadeOutDuration = Mathf.Max(0f, eagleCameraFadeOutDuration);
        ulakDropCameraBlendDuration = Mathf.Max(0f, ulakDropCameraBlendDuration);
        postReleaseHoldDuration = Mathf.Max(0f, postReleaseHoldDuration);
    }
}
