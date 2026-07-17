using System;
using UnityEngine;

public enum UITargetPopupSide
{
    Auto,
    Top,
    Bottom,
    Left,
    Right
}

[Serializable]
public struct UITargetPlacementSettings
{
    public UITargetPopupSide side;
    public Vector2 popupOffset;
    public float targetGap;
    public float canvasMargin;
    public bool rebuildLayoutBeforePlace;

    public static UITargetPlacementSettings Default => new UITargetPlacementSettings
    {
        side = UITargetPopupSide.Auto,
        popupOffset = Vector2.zero,
        targetGap = 36f,
        canvasMargin = 24f,
        rebuildLayoutBeforePlace = false
    };
}
