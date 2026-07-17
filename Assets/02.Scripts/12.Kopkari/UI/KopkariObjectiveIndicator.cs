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

    public ObjectiveKind CurrentKind => currentKind;
    public Transform CurrentTarget => currentTarget;

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
        UpdateScreenPresentation();
        UpdateWarmupBeaconVisibility();
    }

    public void RefreshNow()
    {
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

    private void UpdateScreenPresentation()
    {
        if (screenIndicatorRoot == null || screenIndicator == null || canvasRect == null ||
            worldCamera == null || currentTarget == null)
        {
            SetScreenIndicatorActive(false);
            return;
        }

        Vector3 viewport = worldCamera.WorldToViewportPoint(currentTarget.position + screenTargetOffset);
        float margin = Mathf.Clamp(viewportMargin, 0f, 0.25f);
        bool onScreen = viewport.z > 0f && viewport.x >= margin && viewport.x <= 1f - margin &&
                        viewport.y >= margin && viewport.y <= 1f - margin;
        if (onScreen)
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

        Vector2 halfSize = canvasRect.rect.size * 0.5f;
        halfSize.x = Mathf.Max(1f, halfSize.x - screenEdgePadding);
        halfSize.y = Mathf.Max(1f, halfSize.y - screenEdgePadding);
        float xScale = Mathf.Abs(direction.x) > 0.0001f
            ? halfSize.x / Mathf.Abs(direction.x)
            : float.PositiveInfinity;
        float yScale = Mathf.Abs(direction.y) > 0.0001f
            ? halfSize.y / Mathf.Abs(direction.y)
            : float.PositiveInfinity;
        screenIndicator.anchoredPosition = direction * Mathf.Min(xScale, yScale);

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
