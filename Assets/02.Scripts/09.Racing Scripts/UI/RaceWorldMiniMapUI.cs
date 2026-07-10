using System.Collections.Generic;
using UnityEngine;

public class RaceWorldMiniMapUI : MonoBehaviour
{
    [System.Serializable]
    public class AiRiderMiniMapIcon
    {
        public Transform aiRider;
        public RectTransform aiIcon;
        public bool rotateIcon;

        [System.NonSerialized] public Vector2 targetPosition;
        [System.NonSerialized] public float targetAngle;
        [System.NonSerialized] public bool targetInitialized;
    }

    [Header("World References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform mapBottomLeft;
    [SerializeField] private Transform mapTopRight;

    [Header("Static World Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform finishPoint;
    [SerializeField] private List<Transform> checkpointPoints = new List<Transform>();

    [Header("UI References")]
    [SerializeField] private RectTransform miniMapRect;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform startIcon;
    [SerializeField] private RectTransform finishIcon;
    [SerializeField] private List<RectTransform> checkpointIcons = new List<RectTransform>();

    [Header("AI Rider Icons")]
    [SerializeField] private List<AiRiderMiniMapIcon> aiRiderIcons = new List<AiRiderMiniMapIcon>();

    [Header("Player Icon")]
    [SerializeField] private bool rotatePlayerIcon = true;

    [Header("Performance")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.6f;
    [SerializeField, Min(0f)] private float positionPixelThreshold = 1f;
    [SerializeField, Min(0f)] private float rotationDegreeThreshold = 3f;

    [Header("Smoothing")]
    [SerializeField] private bool smoothIconMovement = true;
    [SerializeField, Min(1f)] private float positionSmoothSpeed = 4f;
    [SerializeField, Min(1f)] private float rotationSmoothSpeed = 6f;

    private bool initialized;
    private float nextUpdateTime;
    private Vector2 playerTargetPosition;
    private float playerTargetAngle;
    private bool playerTargetInitialized;
    private float mapMinX;
    private float mapMinZ;
    private float invMapWidth;
    private float invMapDepth;
    private float rectXMin;
    private float rectYMin;
    private float rectWidth;
    private float rectHeight;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!initialized)
            Initialize();
    }

    private void OnDisable()
    {
        initialized = false;
        playerTargetInitialized = false;

        for (int i = 0; i < aiRiderIcons.Count; i++)
        {
            if (aiRiderIcons[i] != null)
                aiRiderIcons[i].targetInitialized = false;
        }
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        if (Time.unscaledTime >= nextUpdateTime)
        {
            nextUpdateTime = Time.unscaledTime + updateInterval;
            UpdatePlayerIcon();
            UpdateAiRiderIcons();
        }

        if (smoothIconMovement)
        {
            SmoothPlayerIcon();
            SmoothAiRiderIcons();
        }
    }

    public void Initialize()
    {
        initialized = false;

        if (player == null)
        {
            if (RacingController.Instance == null || RacingController.Instance.horse == null)
                return;

            player = RacingController.Instance.horse.transform;
            //Debug.Log("RaceWorldMiniMapUI: Player reference yo'q.");
            //return;
        }

        if (mapBottomLeft == null || mapTopRight == null)
        {
            Debug.Log("RaceWorldMiniMapUI: MapBottomLeft yoki MapTopRight yo'q.");
            return;
        }

        if (miniMapRect == null || playerIcon == null)
        {
            Debug.Log("RaceWorldMiniMapUI: MiniMapRect yoki PlayerIcon yo'q.");
            return;
        }

        if (!CacheMapData()) return;

        PlaceStaticIcons();

        initialized = true;
        nextUpdateTime = Time.unscaledTime + updateInterval;
        UpdatePlayerIcon();
        UpdateAiRiderIcons();
    }

    private void UpdatePlayerIcon()
    {
        if (player == null || playerIcon == null)
        {
            initialized = false;
            return;
        }

        Vector2 uiPosition = WorldToMiniMapPosition(player.position);
        playerTargetPosition = uiPosition;

        if (!smoothIconMovement || !playerTargetInitialized)
        {
            SetAnchoredPositionIfChanged(playerIcon, uiPosition);
            playerTargetInitialized = true;
        }

        if (rotatePlayerIcon)
        {
            UpdatePlayerIconTargetRotation();
        }
    }

    private void UpdateAiRiderIcons()
    {
        for (int i = 0; i < aiRiderIcons.Count; i++)
        {
            AiRiderMiniMapIcon riderIcon = aiRiderIcons[i];

            if (riderIcon == null) continue;
            if (riderIcon.aiRider == null || riderIcon.aiIcon == null) continue;

            Vector2 uiPosition = WorldToMiniMapPosition(riderIcon.aiRider.position);
            riderIcon.targetPosition = uiPosition;

            if (!smoothIconMovement || !riderIcon.targetInitialized)
            {
                SetAnchoredPositionIfChanged(riderIcon.aiIcon, uiPosition);
                riderIcon.targetInitialized = true;
            }

            if (riderIcon.rotateIcon)
            {
                UpdateAiIconTargetRotation(riderIcon);
            }
        }
    }

    private void PlaceStaticIcons()
    {
        if (startPoint != null && startIcon != null)
            startIcon.anchoredPosition = WorldToMiniMapPosition(startPoint.position);

        if (finishPoint != null && finishIcon != null)
            finishIcon.anchoredPosition = WorldToMiniMapPosition(finishPoint.position);

        int count = Mathf.Min(checkpointPoints.Count, checkpointIcons.Count);

        for (int i = 0; i < count; i++)
        {
            if (checkpointPoints[i] == null || checkpointIcons[i] == null) continue;

            checkpointIcons[i].anchoredPosition = WorldToMiniMapPosition(checkpointPoints[i].position);
        }
    }

    private Vector2 WorldToMiniMapPosition(Vector3 worldPosition)
    {
        float normalizedX = Mathf.Clamp01((worldPosition.x - mapMinX) * invMapWidth);
        float normalizedY = Mathf.Clamp01((worldPosition.z - mapMinZ) * invMapDepth);

        float uiX = rectXMin + rectWidth * normalizedX;
        float uiY = rectYMin + rectHeight * normalizedY;

        return new Vector2(uiX, uiY);
    }

    private void UpdatePlayerIconTargetRotation()
    {
        Vector3 forward = player.forward;

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        // Agar arrow icon tepaga qaragan bo'lsa shu to'g'ri ishlaydi.
        playerTargetAngle = -angle;

        if (!smoothIconMovement)
            SetRotationIfChanged(playerIcon, playerTargetAngle);
    }

    private void UpdateAiIconTargetRotation(AiRiderMiniMapIcon riderIcon)
    {
        Vector3 forward = riderIcon.aiRider.forward;

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        riderIcon.targetAngle = -angle;

        if (!smoothIconMovement)
            SetRotationIfChanged(riderIcon.aiIcon, riderIcon.targetAngle);
    }

    private bool CacheMapData()
    {
        if (mapBottomLeft == null || mapTopRight == null || miniMapRect == null)
        {
            initialized = false;
            return false;
        }

        float mapWidth = mapTopRight.position.x - mapBottomLeft.position.x;
        float mapDepth = mapTopRight.position.z - mapBottomLeft.position.z;

        if (Mathf.Approximately(mapWidth, 0f) || Mathf.Approximately(mapDepth, 0f))
        {
            Debug.Log("RaceWorldMiniMapUI: Map bounds size is zero.");
            return false;
        }

        mapMinX = mapBottomLeft.position.x;
        mapMinZ = mapBottomLeft.position.z;
        invMapWidth = 1f / mapWidth;
        invMapDepth = 1f / mapDepth;

        Rect rect = miniMapRect.rect;
        rectXMin = rect.xMin;
        rectYMin = rect.yMin;
        rectWidth = rect.width;
        rectHeight = rect.height;

        return true;
    }

    private void SetAnchoredPositionIfChanged(RectTransform icon, Vector2 position)
    {
        if ((icon.anchoredPosition - position).sqrMagnitude < positionPixelThreshold * positionPixelThreshold)
            return;

        icon.anchoredPosition = position;
    }

    private void SetRotationIfChanged(RectTransform icon, float zAngle)
    {
        float currentZ = icon.localEulerAngles.z;
        if (Mathf.Abs(Mathf.DeltaAngle(currentZ, zAngle)) < rotationDegreeThreshold)
            return;

        icon.localEulerAngles = new Vector3(0f, 0f, zAngle);
    }

    private void SmoothPlayerIcon()
    {
        if (!playerTargetInitialized) return;
        if (playerIcon == null)
        {
            initialized = false;
            return;
        }

        float positionT = 1f - Mathf.Exp(-positionSmoothSpeed * Time.unscaledDeltaTime);
        Vector2 position = Vector2.Lerp(playerIcon.anchoredPosition, playerTargetPosition, positionT);
        SetSmoothedAnchoredPosition(playerIcon, position, playerTargetPosition);

        if (!rotatePlayerIcon) return;

        float rotationT = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.unscaledDeltaTime);
        float angle = Mathf.LerpAngle(playerIcon.localEulerAngles.z, playerTargetAngle, rotationT);
        SetSmoothedRotation(playerIcon, angle, playerTargetAngle);
    }

    private void SmoothAiRiderIcons()
    {
        float positionT = 1f - Mathf.Exp(-positionSmoothSpeed * Time.unscaledDeltaTime);
        float rotationT = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.unscaledDeltaTime);

        for (int i = 0; i < aiRiderIcons.Count; i++)
        {
            AiRiderMiniMapIcon riderIcon = aiRiderIcons[i];

            if (riderIcon == null || !riderIcon.targetInitialized) continue;
            if (riderIcon.aiIcon == null) continue;

            Vector2 position = Vector2.Lerp(riderIcon.aiIcon.anchoredPosition, riderIcon.targetPosition, positionT);
            SetSmoothedAnchoredPosition(riderIcon.aiIcon, position, riderIcon.targetPosition);

            if (!riderIcon.rotateIcon) continue;

            float angle = Mathf.LerpAngle(riderIcon.aiIcon.localEulerAngles.z, riderIcon.targetAngle, rotationT);
            SetSmoothedRotation(riderIcon.aiIcon, angle, riderIcon.targetAngle);
        }
    }

    private void SetSmoothedAnchoredPosition(RectTransform icon, Vector2 position, Vector2 targetPosition)
    {
        if ((icon.anchoredPosition - targetPosition).sqrMagnitude < 0.01f)
            return;

        icon.anchoredPosition = position;
    }

    private void SetSmoothedRotation(RectTransform icon, float zAngle, float targetAngle)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(icon.localEulerAngles.z, targetAngle)) < 0.1f)
            return;

        icon.localEulerAngles = new Vector3(0f, 0f, zAngle);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!initialized || !isActiveAndEnabled || miniMapRect == null) return;
        if (!CacheMapData()) return;

        PlaceStaticIcons();
    }
}
