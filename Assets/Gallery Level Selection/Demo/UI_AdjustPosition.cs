#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public enum HorizontalAnchor { left, center, right, ignore }
public enum VerticalAnchor { top, center, bottom, ignore }

[ExecuteInEditMode]
public class UI_AdjustPosition : MonoBehaviour
{
    public HorizontalAnchor horizontalAnchor = HorizontalAnchor.ignore;
    public VerticalAnchor verticalAnchor = VerticalAnchor.ignore;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        AdjustPosition();
    }

    private void Update()
    {
#if UNITY_EDITOR
        AdjustPosition();
#endif
    }

    private void AdjustPosition()
    {
        Vector2 anchorPos = Vector2.zero;

        if (horizontalAnchor == HorizontalAnchor.center) anchorPos.x = 0.5f;
        else if (horizontalAnchor == HorizontalAnchor.right) anchorPos.x = 1.0f;

        if (verticalAnchor == VerticalAnchor.center) anchorPos.y = 0.5f;
        else if (verticalAnchor == VerticalAnchor.top) anchorPos.y = 1.0f;

        // Set the anchors and position
        rectTransform.anchorMin = anchorPos;
        rectTransform.anchorMax = anchorPos;

        // Optionally, adjust the pivot if you want different positioning behavior
        rectTransform.pivot = anchorPos;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}
