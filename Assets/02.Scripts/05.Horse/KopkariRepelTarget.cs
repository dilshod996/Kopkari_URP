using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KopkariRepelTarget : MonoBehaviour
{
    // Barcha targetlar ro'yxati (bomblar shundan foydalanadi)
    public static readonly List<KopkariRepelTarget> All = new List<KopkariRepelTarget>();

    [Tooltip("Agar y o'qini biroz ko'tarmoqchi bo'lsang (odatda 0).")]
    public float yOffset = 0f;

    void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
        StopAllCoroutines();
    }

    /// <summary>
    /// Bomb markazidan transform-based knockback.
    /// </summary>
    public void ApplyRepel(Vector3 fromPos, float distance, float duration, AnimationCurve curve)
    {
        StopAllCoroutines();
        StartCoroutine(RepelRoutine(fromPos, distance, duration, curve));
    }

    IEnumerator RepelRoutine(Vector3 fromPos, float distance, float duration, AnimationCurve curve)
    {
        Vector3 start = transform.position;

        // Itarish yo'nalishi (faqat XZ)
        Vector3 dir = (start - fromPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.right;   // ustida turgan bo'lsa yon tomonga

        dir.Normalize();

        // Planar (XZ) nuqtalar bilan ishlaymiz
        Vector3 startXZ = new Vector3(start.x, 0f, start.z);
        Vector3 targetXZ = startXZ + dir * distance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = curve.Evaluate(Mathf.Clamp01(t));

            // Lerp faqat XZ uchun
            Vector3 lerpXZ = Vector3.Lerp(startXZ, targetXZ, k);

            // Hozirgi Y¡¯ni olib, faqat XZ ni yangilaymiz
            Vector3 current = transform.position;
            float y = current.y + yOffset;   // yoki faqat current.y; agar offset kerak bo'lmasa

            Vector3 newPos = new Vector3(lerpXZ.x, y, lerpXZ.z);
            transform.position = newPos;

            yield return null;
        }
    }

}
