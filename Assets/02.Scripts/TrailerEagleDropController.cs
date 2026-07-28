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
    [SerializeField, Min(0.01f)] private float movementSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float rotationSpeed = 240f;
    [SerializeField] private bool resetToFirstWaypointOnPlay = true;
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
    [SerializeField] private bool enableUlakColliderOnRelease = true;
    [SerializeField] private bool snapUlakToCarrySocketOnPlay = true;
    [SerializeField] private bool restoreUlakWhenStopped = true;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCameraBase eagleTrackingCamera;
    [SerializeField] private CinemachineVirtualCameraBase ulakDropCamera;
    [Tooltip("Allows the second LookDistance camera to establish the eagle before tracking begins.")]
    [SerializeField, Min(0f)] private float eagleTrackingCameraDelay = 1f;
    [SerializeField, Min(0f)] private float eagleTrackingCameraBlendDuration = 0.35f;
    [SerializeField, Min(0f)] private float ulakDropCameraBlendDuration = 0.15f;
    [SerializeField, Min(0f)] private float postReleaseHoldDuration = 2.5f;

    private Coroutine sequenceCoroutine;
    private TrailerOpeningController openingCameraController;
    private Vector3 eagleVelocity;
    private int currentWaypointIndex;
    private int flightStateHash;
    private bool isPlaying;
    private bool ulakReleased;

    private Transform originalUlakParent;
    private Vector3 originalUlakLocalPosition;
    private Quaternion originalUlakLocalRotation;
    private bool originalUlakRigidbodyIsKinematic;
    private bool originalUlakRigidbodyUseGravity;
    private bool originalUlakColliderEnabled;
    private bool hasStoredUlakState;

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

        CacheFlightStateHash();
    }

    private void OnDisable()
    {
        StopEagleDrop();
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

        openingCameraController = cameraController;
        CacheFlightStateHash();

        if (hasStoredUlakState)
        {
            RestoreUlakState();
        }

        StoreUlakState();
        PrepareUlakForCarry();
        PrepareFlightPath();
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

        if (restoreUlakWhenStopped)
        {
            RestoreUlakState();
        }

        isPlaying = false;
        openingCameraController = null;
        eagleVelocity = Vector3.zero;
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
                SwitchCamera(eagleTrackingCamera, eagleTrackingCameraBlendDuration);
                trackingCameraActivated = true;
            }

            if (elapsed >= ulakReleaseDelay)
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
        openingCameraController = null;
        onEagleSequenceFinished?.Invoke();
    }

    private void PrepareFlightPath()
    {
        eagleVelocity = Vector3.zero;
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
                eagleRoot.SetPositionAndRotation(firstWaypoint.position, firstWaypoint.rotation);
                currentWaypointIndex = GetNextValidWaypointIndex(firstWaypointIndex + 1);
            }
        }
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
        eagleRoot.position = Vector3.SmoothDamp(
            eagleRoot.position,
            targetWaypoint.position,
            ref eagleVelocity,
            movementSmoothTime,
            flightSpeed,
            DeltaTime);

        Vector3 frameMovement = eagleRoot.position - previousPosition;
        if (frameMovement.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(frameMovement.normalized, Vector3.up);
            eagleRoot.rotation = Quaternion.RotateTowards(
                eagleRoot.rotation,
                targetRotation,
                rotationSpeed * DeltaTime);
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

    private void PrepareUlakForCarry()
    {
        if (ulak != null && ulakCarrySocket != null)
        {
            ulak.SetParent(ulakCarrySocket, !snapUlakToCarrySocketOnPlay);

            if (snapUlakToCarrySocketOnPlay)
            {
                ulak.localPosition = Vector3.zero;
                ulak.localRotation = Quaternion.identity;
            }
        }

        if (ulakRigidbody != null)
        {
            ulakRigidbody.velocity = Vector3.zero;
            ulakRigidbody.angularVelocity = Vector3.zero;
            ulakRigidbody.useGravity = false;
            ulakRigidbody.isKinematic = true;
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
        }

        if (ulakCollider != null)
        {
            ulakCollider.enabled = originalUlakColliderEnabled;
        }

        hasStoredUlakState = false;
        ulakReleased = false;
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
        ulakReleaseDelay = Mathf.Max(0f, ulakReleaseDelay);
        eagleTrackingCameraDelay = Mathf.Max(0f, eagleTrackingCameraDelay);
        eagleTrackingCameraBlendDuration = Mathf.Max(0f, eagleTrackingCameraBlendDuration);
        ulakDropCameraBlendDuration = Mathf.Max(0f, ulakDropCameraBlendDuration);
        postReleaseHoldDuration = Mathf.Max(0f, postReleaseHoldDuration);
    }
}
