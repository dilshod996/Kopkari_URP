using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialTargetHoleRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform passThroughTarget;
    private bool allowTargetInput;

    public void SetTarget(RectTransform target, bool allowInput)
    {
        passThroughTarget = target;
        allowTargetInput = allowInput && target != null;
    }

    public void ClearTarget()
    {
        passThroughTarget = null;
        allowTargetInput = false;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!allowTargetInput || passThroughTarget == null ||
            !passThroughTarget.gameObject.activeInHierarchy)
        {
            return true;
        }

        Canvas targetCanvas = passThroughTarget.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas == null ||
                              targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        return !RectTransformUtility.RectangleContainsScreenPoint(
            passThroughTarget,
            screenPoint,
            targetCamera);
    }
}
