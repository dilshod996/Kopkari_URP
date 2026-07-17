using UnityEngine;
using UnityEngine.UI;

public class UITargetRelativePlacer : MonoBehaviour
{
    private static readonly Vector3[] TargetCorners = new Vector3[4];

    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform popup;
    [SerializeField] private UITargetPlacementSettings placement = UITargetPlacementSettings.Default;

    public void Place(RectTransform target)
    {
        Place(popup, target, canvasRect, placement);
    }

    public void Place(RectTransform target, UITargetPopupSide side, Vector2 popupOffset)
    {
        UITargetPlacementSettings runtimePlacement = placement;
        runtimePlacement.side = side;
        runtimePlacement.popupOffset = popupOffset;

        Place(popup, target, canvasRect, runtimePlacement);
    }

    public static bool Place(
        RectTransform popup,
        RectTransform target,
        RectTransform canvasRect,
        UITargetPlacementSettings placement)
    {
        if (!popup || !canvasRect)
            return false;

        if (placement.rebuildLayoutBeforePlace)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
        }

        Vector2 popupHalf = GetRectSize(popup) * 0.5f;
        Rect canvasBounds = canvasRect.rect;
        float margin = Mathf.Max(placement.canvasMargin, 0f);

        float minX = canvasBounds.xMin + popupHalf.x + margin;
        float maxX = canvasBounds.xMax - popupHalf.x - margin;
        float minY = canvasBounds.yMin + popupHalf.y + margin;
        float maxY = canvasBounds.yMax - popupHalf.y - margin;

        popup.anchorMin = new Vector2(0.5f, 0.5f);
        popup.anchorMax = new Vector2(0.5f, 0.5f);
        popup.pivot = new Vector2(0.5f, 0.5f);
        popup.localScale = Vector3.one;

        Vector2 position = target
            ? GetTargetRelativePosition(target, canvasRect, popupHalf, placement)
            : placement.popupOffset;

        popup.anchoredPosition = ClampToCanvas(position, minX, maxX, minY, maxY);
        return true;
    }

    public static Vector2 GetTargetRelativePosition(
        RectTransform target,
        RectTransform canvasRect,
        Vector2 popupHalf,
        UITargetPlacementSettings placement)
    {
        GetTargetBounds(target, canvasRect, out Vector2 targetMin, out Vector2 targetMax);

        Vector2 targetCenter = (targetMin + targetMax) * 0.5f;
        UITargetPopupSide side = placement.side == UITargetPopupSide.Auto
            ? PickAutoSide(targetCenter)
            : placement.side;

        float gap = Mathf.Max(placement.targetGap, 0f);
        Vector2 position = side switch
        {
            UITargetPopupSide.Top => new Vector2(targetCenter.x, targetMax.y + popupHalf.y + gap),
            UITargetPopupSide.Bottom => new Vector2(targetCenter.x, targetMin.y - popupHalf.y - gap),
            UITargetPopupSide.Left => new Vector2(targetMin.x - popupHalf.x - gap, targetCenter.y),
            UITargetPopupSide.Right => new Vector2(targetMax.x + popupHalf.x + gap, targetCenter.y),
            _ => targetCenter
        };

        return position + placement.popupOffset;
    }

    private static UITargetPopupSide PickAutoSide(Vector2 targetCenter)
    {
        if (Mathf.Abs(targetCenter.x) > Mathf.Abs(targetCenter.y))
            return targetCenter.x >= 0f ? UITargetPopupSide.Left : UITargetPopupSide.Right;

        return targetCenter.y >= 0f ? UITargetPopupSide.Bottom : UITargetPopupSide.Top;
    }

    private static void GetTargetBounds(
        RectTransform target,
        RectTransform canvasRect,
        out Vector2 targetMin,
        out Vector2 targetMax)
    {
        target.GetWorldCorners(TargetCorners);

        Camera camera = GetCanvasCamera(canvasRect);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(camera, TargetCorners[0]),
            camera,
            out targetMin);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(camera, TargetCorners[2]),
            camera,
            out targetMax);
    }

    private static Camera GetCanvasCamera(RectTransform canvasRect)
    {
        Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
        if (!canvas || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private static Vector2 GetRectSize(RectTransform rectTransform)
    {
        Vector2 size = rectTransform.rect.size;
        if (size.x <= 1f || size.y <= 1f)
            size = rectTransform.sizeDelta;

        return size;
    }

    private static Vector2 ClampToCanvas(Vector2 position, float minX, float maxX, float minY, float maxY)
    {
        if (minX > maxX)
            position.x = (minX + maxX) * 0.5f;
        else
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY > maxY)
            position.y = (minY + maxY) * 0.5f;
        else
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}
