using UnityEngine;

public class SplinePointsProvider : MonoBehaviour
{
    [SerializeField] private Transform[] bakedPoints; // spline spawnerdan bake qilingan pointlar

    public bool HasPoints => bakedPoints != null && bakedPoints.Length >= 2;
    public int PointCount => bakedPoints?.Length ?? 0;

    // t [0..1] => position + forward + right
    public void Evaluate(float t, out Vector3 pos, out Vector3 forward, out Vector3 right)
    {
        if (!HasPoints)
        {
            pos = transform.position;
            forward = transform.forward;
            right = transform.right;
            return;
        }

        int last = bakedPoints.Length - 1;
        float ft = Mathf.Clamp01(t) * last;
        int a = Mathf.FloorToInt(ft);
        int b = Mathf.Min(a + 1, last);
        float lerp = ft - a;

        Vector3 p0 = bakedPoints[a].position;
        Vector3 p1 = bakedPoints[b].position;

        pos = Vector3.Lerp(p0, p1, lerp);

        forward = (p1 - p0);
        forward.y = 0f;
        forward = forward.sqrMagnitude < 0.0001f ? transform.forward : forward.normalized;

        right = Vector3.Cross(Vector3.up, forward).normalized;
    }
}