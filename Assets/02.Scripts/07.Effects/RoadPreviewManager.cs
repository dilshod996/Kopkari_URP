using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.Curvy;

public class RoadPreviewManager : MonoBehaviour
{
    public static RoadPreviewManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private CurvySpline trackSpline;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private UIPolylineGraphic lineGraphic;

    [Header("Preview")]
    [SerializeField] private float previewDistance = 60f;
    [SerializeField, Range(8, 40)] private int samples = 24;
    [SerializeField] private float thickness = 10f;

    [Header("UI Mapping")]
    [SerializeField] private float padding = 20f;
    [SerializeField] private bool rotateToPlayerForward = true;

    // 🔥 FAKE 3D SETTINGS
    [Header("Fake 3D Perspective")]
    [Tooltip("Yaqindan uzoqqa torayish (0.3–0.6 tavsiya)")]
    [SerializeField, Range(0.25f, 1f)] private float farScale = 0.45f;

    [Tooltip("Oldinga ketayotgandek Y bo'yicha surish")]
    [SerializeField] private float forwardPush = 120f;

    [Tooltip("Perspective egri kuchi (1 = linear)")]
    [SerializeField, Range(0.5f, 2f)] private float curvePower = 1.1f;

    [Header("Optimization")]
    [SerializeField] private float minSecondsBetweenRebuild = 0.12f;

    // Runtime
    private float currentSplineDistance;
    private float lastRebuildTime = -999f;

    private readonly List<Vector3> worldPoints = new();
    private readonly List<Vector2> local2D = new();
    private readonly List<Vector2> uiPoints = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (lineGraphic != null)
            lineGraphic.SetBaseThickness(thickness);

    }
    private void OnEnable()
    {
        HorseDataManager.OnHorseTransorm += SetPlayer;
    }
    private void OnDestroy()
    {
        HorseDataManager.OnHorseTransorm -= SetPlayer;
        if (Instance == this) Instance = null;
    }

    // =========================
    // PUBLIC API
    // =========================

    public void SetPlayer(Transform horse)
    {
        player = horse;
    }

    public void SetPlayerSplineDistance(float distanceMeters, bool rebuildNow = false)
    {
        currentSplineDistance = Mathf.Max(0f, distanceMeters);
        if (rebuildNow)
            RequestRebuild();
    }

    public void RequestRebuild()
    {
        if (!IsReady()) return;

        if (Time.unscaledTime - lastRebuildTime < minSecondsBetweenRebuild)
            return;

        lastRebuildTime = Time.unscaledTime;

        BuildWorldSamples();
        BuildUIPoints_Fake3D();
        lineGraphic.SetPoints(uiPoints);
    }

    // =========================
    // INTERNAL
    // =========================

    private bool IsReady()
    {
        return player != null && trackSpline != null && panelRect != null &&
               lineGraphic != null && samples >= 2;
    }

    private void BuildWorldSamples()
    {
        worldPoints.Clear();

        float splineLen = Mathf.Max(0.01f, trackSpline.Length);
        float step = previewDistance / (samples - 1);

        bool loop = trackSpline.Closed;

        for (int i = 0; i < samples; i++)
        {
            float d = currentSplineDistance + step * i;

            if (loop)
                d = Mathf.Repeat(d, splineLen);
            else
                d = Mathf.Clamp(d, 0f, splineLen);

            Vector3 p = trackSpline.InterpolateByDistance(d);
            worldPoints.Add(p);

            if (!loop && Mathf.Approximately(d, splineLen))
                break;
        }

        if (worldPoints.Count < 2)
        {
            worldPoints.Clear();
            worldPoints.Add(player.position);
            worldPoints.Add(player.position + player.forward * 5f);
        }
    }

    // 🔥 FAKE 3D CORE
    private void BuildUIPoints_Fake3D()
    {
        uiPoints.Clear();
        local2D.Clear();

        if (worldPoints.Count < 2) return;

        Vector2 origin = new Vector2(player.position.x, player.position.z);

        float rotDeg = 0f;
        if (rotateToPlayerForward)
        {
            Vector3 f = player.forward;
            Vector2 f2 = new Vector2(f.x, f.z).normalized;
            rotDeg = Mathf.Atan2(f2.x, f2.y) * Mathf.Rad2Deg;
        }

        // 1️⃣ World → local 2D
        for (int i = 0; i < worldPoints.Count; i++)
        {
            Vector2 p = new Vector2(worldPoints[i].x, worldPoints[i].z) - origin;
            if (rotateToPlayerForward)
                p = Rotate(p, -rotDeg);

            local2D.Add(p);
        }

        // 2️⃣ Normalize to panel
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in local2D)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float w = panelRect.rect.width - padding * 2f;
        float h = panelRect.rect.height - padding * 2f;

        float spanX = Mathf.Max(0.001f, maxX - minX);
        float spanY = Mathf.Max(0.001f, maxY - minY);
        float baseScale = Mathf.Min(w / spanX, h / spanY);

        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;

        // 3️⃣ Fake 3D Perspective
        int count = local2D.Count;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);       // 0 = yaqin, 1 = uzoq
            t = Mathf.Pow(t, curvePower);            // egri kuchi

            float perspective = Mathf.Lerp(1f, farScale, t);
            float push = Mathf.Lerp(0f, forwardPush, t);

            Vector2 p = local2D[i];
            p.x = (p.x - midX) * baseScale * perspective;
            p.y = (p.y - midY) * baseScale * perspective + push;

            uiPoints.Add(p);
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
