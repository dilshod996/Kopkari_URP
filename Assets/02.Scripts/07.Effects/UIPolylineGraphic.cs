using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIPolylineGraphic : Graphic
{
    [Header("Thickness")]
    [SerializeField] private float baseThickness = 7f;
    [SerializeField] private float farThicknessMultiplier = 1.8f;
    [SerializeField] private float thicknessCurvePower = 1.2f;

    [Header("Alpha (Depth + Fade)")]
    [SerializeField, Range(0f, 1f)] private float nearAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float farAlpha = 0.15f;
    [SerializeField] private float alphaCurvePower = 1.1f;

    [Header("Caps")]
    [SerializeField] private bool roundCaps = true;
    [SerializeField] private int capSegments = 8;

    private readonly List<Vector2> points = new();

    public void SetPoints(IList<Vector2> newPoints)
    {
        points.Clear();
        if (newPoints != null)
            points.AddRange(newPoints);

        SetVerticesDirty();
    }

    public void SetBaseThickness(float t)
    {
        baseThickness = Mathf.Max(1f, t);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        int count = points.Count;

        for (int i = 0; i < count - 1; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[i + 1];

            Vector2 dir = p1 - p0;
            if (dir.sqrMagnitude < 0.0001f) continue;
            dir.Normalize();

            float t = i / (float)(count - 1);
            t = Mathf.Pow(t, thicknessCurvePower);

            // 🔥 Thickness depth
            float thickness = baseThickness * Mathf.Lerp(1f, farThicknessMultiplier, t);
            float half = thickness * 0.5f;

            // 🔥 Alpha depth + fade
            float a = Mathf.Lerp(nearAlpha, farAlpha, Mathf.Pow(t, alphaCurvePower));
            Color c = color;
            c.a *= a;

            Vector2 n = new Vector2(-dir.y, dir.x) * half;

            Vector2 v0 = p0 - n;
            Vector2 v1 = p0 + n;
            Vector2 v2 = p1 + n;
            Vector2 v3 = p1 - n;

            int idx = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = c;

            vert.position = v0; vh.AddVert(vert);
            vert.position = v1; vh.AddVert(vert);
            vert.position = v2; vh.AddVert(vert);
            vert.position = v3; vh.AddVert(vert);

            vh.AddTriangle(idx + 0, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx + 0);
        }

        if (roundCaps)
        {
            AddCap(vh, points[0], points[1],
                   baseThickness * 0.5f, color, true);

            AddCap(vh, points[^1], points[^2],
                   baseThickness * farThicknessMultiplier * 0.5f,
                   new Color(color.r, color.g, color.b, color.a * farAlpha),
                   false);
        }
    }

    private void AddCap(VertexHelper vh, Vector2 center, Vector2 next,
                        float radius, Color c, bool start)
    {
        Vector2 dir = (next - center);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector2 forward = start ? -dir : dir;
        float baseAngle = Mathf.Atan2(forward.y, forward.x);

        int seg = Mathf.Max(3, capSegments);
        int baseIndex = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = c;
        v.position = center;
        vh.AddVert(v);

        for (int i = 0; i <= seg; i++)
        {
            float a = baseAngle + Mathf.PI * ((float)i / seg - 0.5f);
            Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

            v.position = p;
            vh.AddVert(v);

            if (i > 0)
                vh.AddTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
        }
    }
}
