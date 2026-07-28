using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One reusable, phase-aware objective marker for Warmup, Uloq and Salym.
/// Objective/distance data is sampled at low frequency; only presentation follows
/// the camera in LateUpdate so the screen-edge indicator remains visually smooth.
/// </summary>
[DisallowMultipleComponent]
public sealed class KopkariObjectiveIndicator : MonoBehaviour
{
    public enum ObjectiveKind
    {
        None,
        Warmup,
        Uloq,
        Target
    }

    [Serializable]
    private sealed class ObjectiveStyle
    {
        public string label = "OBJECTIVE";
        public Sprite icon;
        public Color color = Color.white;
    }

    [Header("Sources")]
    [SerializeField] private KopkariManager manager;
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas screenCanvas;

    [Header("Objective Styles")]
    [SerializeField] private ObjectiveStyle warmupStyle = new ObjectiveStyle
    {
        label = "WARMUP",
        color = new Color(0.2f, 0.65f, 1f, 1f)
    };
    [SerializeField] private ObjectiveStyle uloqStyle = new ObjectiveStyle
    {
        label = "ULOQ",
        color = new Color(1f, 0.72f, 0.12f, 1f)
    };
    [SerializeField] private ObjectiveStyle targetStyle = new ObjectiveStyle
    {
        label = "SALYM",
        color = new Color(0.2f, 1f, 0.45f, 1f)
    };

    [Header("Warmup Particle Beacon")]
    [SerializeField] private ParticleSystem warmupBeacon;
    [SerializeField] private Vector3 beaconOffset = Vector3.zero;
    [SerializeField, Min(0.1f)] private float warmupHideDistance = 2.5f;

    [Header("Screen Edge UI (child of Screen Canvas)")]
    [Tooltip("Assign a child object, not the GameObject containing this script.")]
    [SerializeField] private GameObject screenIndicatorRoot;
    [SerializeField] private RectTransform screenIndicator;
    [SerializeField] private Image screenIcon;
    [SerializeField] private TMP_Text screenLabel;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField, Min(0f)] private float screenEdgePadding = 90f;
    [SerializeField, Range(0f, 0.25f)] private float viewportMargin = 0.04f;
    [Tooltip("Height used when projecting the objective onto the screen.")]
    [SerializeField] private Vector3 screenTargetOffset = new Vector3(0f, 2f, 0f);

    [Header("Mobile Sampling")]
    [SerializeField, Range(0.1f, 1f)] private float objectiveRefreshInterval = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float distanceRefreshInterval = 0.25f;
    [SerializeField] private bool horizontalDistanceOnly = true;

    private RectTransform canvasRect;
    private ObjectiveKind currentKind;
    private Transform currentTarget;
    private float nextObjectiveRefresh;
    private float nextDistanceRefresh;
    private float currentDistance;
    private bool visualsVisible;
    private bool tutorialPreviewActive;

    public ObjectiveKind CurrentKind => currentKind;
    public Transform CurrentTarget => currentTarget;

    public bool TryGetTutorialSnapshot(
        out ObjectiveKind kind,
        out string label,
        out Sprite icon,
        out Color color,
        out int distanceMeters)
    {
        KopkariManager activeManager = manager != null ? manager : KopkariManager.Instance;
        Transform snapshotTarget = null;
        kind = ObjectiveKind.None;
        label = string.Empty;
        icon = null;
        color = Color.white;
        distanceMeters = 0;

        if (activeManager != null)
        {
            if (activeManager.roomState == KopkariManager.RoomState.GameStarted)
            {
                bool localPlayerOwnsUloq = activeManager.currentGoatOwner != null &&
                                           activeManager.IsLocalRiderTransform(
                                               activeManager.currentGoatOwner.transform);
                if (localPlayerOwnsUloq && activeManager.CurrentTargetPosition != null)
                {
                    kind = ObjectiveKind.Target;
                    snapshotTarget = activeManager.CurrentTargetPosition;
                }
                else if (activeManager.UlakTransform != null)
                {
                    kind = ObjectiveKind.Uloq;
                    snapshotTarget = activeManager.UlakTransform;
                }
            }
            else if (activeManager.IsRoundWarmupActive &&
                     activeManager.CurrentWarmupPosition != null)
            {
                kind = ObjectiveKind.Warmup;
                snapshotTarget = activeManager.CurrentWarmupPosition;
            }
        }

        if (kind == ObjectiveKind.None || snapshotTarget == null)
            return false;

        ObjectiveStyle style = GetStyle(kind);
        if (style != null)
        {
            label = style.label;
            icon = style.icon;
            color = style.color;
        }

        Transform snapshotPlayer = player;
        if (snapshotPlayer == null && activeManager != null)
        {
            if (activeManager.horseAnimal != null)
                snapshotPlayer = activeManager.horseAnimal.transform;
            else if (activeManager.LocalRiderAnimal != null)
                snapshotPlayer = activeManager.LocalRiderAnimal.transform;
        }

        if (snapshotPlayer != null)
        {
            Vector3 from = snapshotPlayer.position;
            Vector3 to = snapshotTarget.position;
            if (horizontalDistanceOnly)
            {
                from.y = 0f;
                to.y = 0f;
            }

            distanceMeters = Mathf.Max(0, Mathf.RoundToInt(Vector3.Distance(from, to)));
        }

        return true;
    }

    private void Awake()
    {
        ResolveReferences();
        HideVisuals();
    }

    private void OnEnable()
    {
        KopkariManager.OnHorseTransform += HandlePlayerTransform;
        KopkariManager.OnGoatOwnerChanged += HandleOwnerChanged;
        KopkariManager.OnGameStarted += HandleGameStateChanged;
        KopkariManager.OnGameStartFinishState += HandleGameStartFinishState;
        RefreshObjective(true);
    }

    private void OnDisable()
    {
        KopkariManager.OnHorseTransform -= HandlePlayerTransform;
        KopkariManager.OnGoatOwnerChanged -= HandleOwnerChanged;
        KopkariManager.OnGameStarted -= HandleGameStateChanged;
        KopkariManager.OnGameStartFinishState -= HandleGameStartFinishState;
        StopWarmupBeacon();
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextObjectiveRefresh)
        {
            nextObjectiveRefresh = now + Mathf.Max(0.1f, objectiveRefreshInterval);
            RefreshObjective(false);
        }

        if (visualsVisible && now >= nextDistanceRefresh)
        {
            nextDistanceRefresh = now + Mathf.Max(0.1f, distanceRefreshInterval);
            RefreshDistance();
        }
    }

    private void LateUpdate()
    {
        if (!visualsVisible || currentTarget == null)
            return;

        ResolveCameraIfNeeded();
        UpdateScreenPresentation(tutorialPreviewActive);
        UpdateWarmupBeaconVisibility();
    }

    public void RefreshNow()
    {
        RefreshObjective(true);
    }

    public void SetTutorialPreview(bool active)
    {
        tutorialPreviewActive = active;
        if (active)
        {
            // Preview only overrides visibility. It must keep the real objective,
            // distance and camera-relative positioning used by normal gameplay.
            RefreshObjective(true);
            if (currentKind != ObjectiveKind.None && currentTarget != null)
                SetScreenIndicatorActive(true);
            else
                SetScreenIndicatorActive(false);
            return;
        }

        RefreshObjective(true);
    }

    private void ResolveReferences()
    {
        if (manager == null)
            manager = KopkariManager.Instance;
        if (screenCanvas == null)
            screenCanvas = GetComponentInParent<Canvas>();
        canvasRect = screenCanvas != null ? screenCanvas.transform as RectTransform : null;
        ResolvePlayerIfNeeded();
        ResolveCameraIfNeeded();
    }

    private void ResolvePlayerIfNeeded()
    {
        if (player != null)
            return;

        KopkariManager activeManager = manager != null ? manager : KopkariManager.Instance;
        if (activeManager != null)
        {
            if (activeManager.horseAnimal != null)
                player = activeManager.horseAnimal.transform;
            else if (activeManager.LocalRiderAnimal != null)
                player = activeManager.LocalRiderAnimal.transform;
        }
    }

    private void ResolveCameraIfNeeded()
    {
        if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            worldCamera = Camera.main;
    }

    private void HandlePlayerTransform(Transform playerTransform)
    {
        player = playerTransform;
        RefreshObjective(true);
    }

    private void HandleOwnerChanged(GameObject ownerRoot)
    {
        RefreshObjective(true);
    }

    private void HandleGameStateChanged()
    {
        RefreshObjective(true);
    }

    private void HandleGameStartFinishState(bool state)
    {
        RefreshObjective(true);
    }

    private void RefreshObjective(bool forcePresentation)
    {
        if (manager == null)
            manager = KopkariManager.Instance;
        ResolvePlayerIfNeeded();

        ObjectiveKind resolvedKind = ObjectiveKind.None;
        Transform resolvedTarget = null;
        if (manager != null)
        {
            if (manager.roomState == KopkariManager.RoomState.GameStarted)
            {
                bool localPlayerOwnsUloq = manager.currentGoatOwner != null &&
                                           manager.IsLocalRiderTransform(manager.currentGoatOwner.transform);
                if (localPlayerOwnsUloq && manager.CurrentTargetPosition != null)
                {
                    resolvedKind = ObjectiveKind.Target;
                    resolvedTarget = manager.CurrentTargetPosition;
                }
                else if (manager.UlakTransform != null)
                {
                    resolvedKind = ObjectiveKind.Uloq;
                    resolvedTarget = manager.UlakTransform;
                }
            }
            else if (manager.IsRoundWarmupActive && manager.CurrentWarmupPosition != null)
            {
                resolvedKind = ObjectiveKind.Warmup;
                resolvedTarget = manager.CurrentWarmupPosition;
            }
        }

        bool changed = resolvedKind != currentKind || resolvedTarget != currentTarget;
        currentKind = resolvedKind;
        currentTarget = resolvedTarget;

        if (currentKind == ObjectiveKind.None || currentTarget == null)
        {
            HideVisuals();
            return;
        }

        if (changed || forcePresentation || !visualsVisible)
        {
            ApplyStyle(GetStyle(currentKind));
            ShowVisuals();
            RefreshDistance();
            PositionWarmupBeacon();
        }
    }

    private ObjectiveStyle GetStyle(ObjectiveKind kind)
    {
        switch (kind)
        {
            case ObjectiveKind.Warmup:
                return warmupStyle;
            case ObjectiveKind.Target:
                return targetStyle;
            default:
                return uloqStyle;
        }
    }

    private void ApplyStyle(ObjectiveStyle style)
    {
        if (style == null)
            return;

        if (screenIcon != null)
        {
            screenIcon.sprite = style.icon;
            screenIcon.color = style.color;
            screenIcon.enabled = style.icon != null;
        }
        if (screenLabel != null)
        {
            screenLabel.text = style.label;
            screenLabel.color = style.color;
        }
    }

    private void RefreshDistance()
    {
        if (player == null || currentTarget == null)
            return;

        Vector3 from = player.position;
        Vector3 to = currentTarget.position;
        if (horizontalDistanceOnly)
        {
            from.y = 0f;
            to.y = 0f;
        }

        currentDistance = Vector3.Distance(from, to);
        if (distanceText != null)
            distanceText.text = Mathf.Max(0, Mathf.RoundToInt(currentDistance)) + " m";
    }

    private void UpdateScreenPresentation(bool forceVisible)
    {
        RectTransform boundaryRect = screenIndicator != null
            ? screenIndicator.parent as RectTransform
            : null;
        if (screenIndicatorRoot == null || screenIndicator == null || boundaryRect == null ||
            worldCamera == null || currentTarget == null)
        {
            SetScreenIndicatorActive(false);
            return;
        }

        Vector3 viewport = worldCamera.WorldToViewportPoint(currentTarget.position + screenTargetOffset);
        float margin = Mathf.Clamp(viewportMargin, 0f, 0.25f);
        bool onScreen = viewport.z > 0f && viewport.x >= margin && viewport.x <= 1f - margin &&
                        viewport.y >= margin && viewport.y <= 1f - margin;
        if (onScreen && !forceVisible)
        {
            SetScreenIndicatorActive(false);
            return;
        }

        SetScreenIndicatorActive(true);
        Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        if (viewport.z < 0f)
            direction = -direction;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;
        direction.Normalize();

        Rect parentRect = boundaryRect.rect;
        Rect indicatorRect = screenIndicator.rect;
        Vector2 indicatorScale = new Vector2(
            Mathf.Abs(screenIndicator.localScale.x),
            Mathf.Abs(screenIndicator.localScale.y));
        Vector2 anchorReferenceNormalized = new Vector2(
            Mathf.Lerp(screenIndicator.anchorMin.x, screenIndicator.anchorMax.x, screenIndicator.pivot.x),
            Mathf.Lerp(screenIndicator.anchorMin.y, screenIndicator.anchorMax.y, screenIndicator.pivot.y));
        Vector2 anchorReference = new Vector2(
            Mathf.Lerp(parentRect.xMin, parentRect.xMax, anchorReferenceNormalized.x),
            Mathf.Lerp(parentRect.yMin, parentRect.yMax, anchorReferenceNormalized.y));

        float minX = parentRect.xMin + screenEdgePadding - indicatorRect.xMin * indicatorScale.x - anchorReference.x;
        float maxX = parentRect.xMax - screenEdgePadding - indicatorRect.xMax * indicatorScale.x - anchorReference.x;
        float minY = parentRect.yMin + screenEdgePadding - indicatorRect.yMin * indicatorScale.y - anchorReference.y;
        float maxY = parentRect.yMax - screenEdgePadding - indicatorRect.yMax * indicatorScale.y - anchorReference.y;

        // If the parent is smaller than the indicator plus padding, keep the
        // indicator centered on that axis instead of allowing it outside.
        if (minX > maxX)
            minX = maxX = (minX + maxX) * 0.5f;
        if (minY > maxY)
            minY = maxY = (minY + maxY) * 0.5f;

        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 halfSize = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
        float xScale = Mathf.Abs(direction.x) > 0.0001f
            ? halfSize.x / Mathf.Abs(direction.x)
            : float.PositiveInfinity;
        float yScale = Mathf.Abs(direction.y) > 0.0001f
            ? halfSize.y / Mathf.Abs(direction.y)
            : float.PositiveInfinity;
        screenIndicator.anchoredPosition = center + direction * Mathf.Min(xScale, yScale);

    }

    private void PositionWarmupBeacon()
    {
        if (warmupBeacon == null)
            return;

        bool shouldPlay = currentKind == ObjectiveKind.Warmup && currentTarget != null;
        warmupBeacon.transform.position = shouldPlay
            ? currentTarget.position + beaconOffset
            : warmupBeacon.transform.position;

        if (shouldPlay)
        {
            if (!warmupBeacon.gameObject.activeSelf)
                warmupBeacon.gameObject.SetActive(true);
            if (!warmupBeacon.isPlaying)
                warmupBeacon.Play(true);
        }
        else
        {
            StopWarmupBeacon();
        }
    }

    private void UpdateWarmupBeaconVisibility()
    {
        if (warmupBeacon == null || currentKind != ObjectiveKind.Warmup)
            return;

        warmupBeacon.transform.position = currentTarget.position + beaconOffset;
        bool shouldShow = player == null || currentDistance > Mathf.Max(0.1f, warmupHideDistance);
        if (shouldShow && !warmupBeacon.isPlaying)
            warmupBeacon.Play(true);
        else if (!shouldShow && warmupBeacon.isPlaying)
            warmupBeacon.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void ShowVisuals()
    {
        visualsVisible = true;
        PositionWarmupBeacon();
    }

    private void HideVisuals()
    {
        visualsVisible = false;
        currentKind = ObjectiveKind.None;
        currentTarget = null;
        SetScreenIndicatorActive(false);
        StopWarmupBeacon();
    }

    private void SetScreenIndicatorActive(bool active)
    {
        if (screenIndicatorRoot != null && screenIndicatorRoot != gameObject &&
            screenIndicatorRoot.activeSelf != active)
        {
            screenIndicatorRoot.SetActive(active);
        }
    }

    private void StopWarmupBeacon()
    {
        if (warmupBeacon == null)
            return;

        if (warmupBeacon.isPlaying)
            warmupBeacon.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
