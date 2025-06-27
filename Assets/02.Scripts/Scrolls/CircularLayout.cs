using UnityEngine;
using UnityEngine.UI;

public class CircularLayout : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform content;
    public float maxScale = 1.2f; // Markazdagi element uchun
    public float minScale = 0.8f; // Yon elementlar uchun
    public float rotationAngle = 15f; // Yon elementlar burilish burchagi
    public float centerThreshold = 50f; // Markaz radiusi

    private void Update()
    {
        UpdateChildTransforms();
    }

    private void UpdateChildTransforms()
    {
        if (content.childCount == 0) return;

        float centerX = scrollRect.viewport.position.x;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            float distanceToCenter = Mathf.Abs(centerX - child.position.x);

            if (distanceToCenter < centerThreshold) // Markazdagi element
            {
                child.localScale = Vector3.one * maxScale;
                child.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else // Yon tarafdagi elementlar
            {
                float scaleFactor = Mathf.Lerp(minScale, maxScale, 1 - distanceToCenter / (Screen.width / 2f));
                float rotation = (child.position.x < centerX) ? rotationAngle : -rotationAngle;

                child.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                child.localRotation = Quaternion.Euler(0, 0, rotation);
            }
        }
    }
}
