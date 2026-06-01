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

    private bool initialized;

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (!initialized) return;

        UpdatePlayerIcon();
        UpdateAiRiderIcons();
    }

    public void Initialize()
    {
        if (player == null)
        {
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

        PlaceStaticIcons();

        initialized = true;
        UpdatePlayerIcon();
        UpdateAiRiderIcons();
    }

    private void UpdatePlayerIcon()
    {
        Vector2 uiPosition = WorldToMiniMapPosition(player.position);
        playerIcon.anchoredPosition = uiPosition;

        if (rotatePlayerIcon)
        {
            RotatePlayerIcon();
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
            riderIcon.aiIcon.anchoredPosition = uiPosition;

            if (riderIcon.rotateIcon)
            {
                RotateAiIcon(riderIcon.aiRider, riderIcon.aiIcon);
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
        float minX = mapBottomLeft.position.x;
        float maxX = mapTopRight.position.x;

        float minZ = mapBottomLeft.position.z;
        float maxZ = mapTopRight.position.z;

        float normalizedX = Mathf.InverseLerp(minX, maxX, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(minZ, maxZ, worldPosition.z);

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        Rect rect = miniMapRect.rect;

        float uiX = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
        float uiY = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);

        return new Vector2(uiX, uiY);
    }

    private void RotatePlayerIcon()
    {
        Vector3 forward = player.forward;

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        // Agar arrow icon tepaga qaragan bo'lsa shu to'g'ri ishlaydi.
        playerIcon.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    private void RotateAiIcon(Transform aiRider, RectTransform aiIcon)
    {
        Vector3 forward = aiRider.forward;

        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        aiIcon.localEulerAngles = new Vector3(0f, 0f, -angle);
    }
}