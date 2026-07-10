using UnityEngine;
using UnityEngine.EventSystems;

public class DragScriptXPos : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform panel;
    public bool isForOpening = true;
    public bool isOpen = false;
    public DragScript otherPanel;

    private Vector2 dragStartPos;
    private float closedX = -290f;
    private float openX = 180f;
    private float threshold = 80f;
    private float duration = 0.3f;

    private void OnEnable()
    {
        ClosePanel();
    }

    private void OnDisable()
    {
        CancelPanelTween();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float dragDelta = eventData.position.x - dragStartPos.x;

        if (isForOpening && dragDelta > threshold)
            OpenPanel();
        else if (!isForOpening && dragDelta < -threshold)
            ClosePanel();
    }

    public void OnClickClose()
    {
        ClosePanel();
    }

    public void OpenPanel()
    {
        if (otherPanel != null && otherPanel.IsOpen())
            otherPanel.ClosePanel();
        if (panel == null) return;

        CancelPanelTween();
        panel.LeanMoveX(openX, duration);
        isOpen = true;
    }

    public void ClosePanel()
    {
        if (panel == null) return;

        CancelPanelTween();
        panel.LeanMoveX(closedX, duration);
        isOpen = false;
    }

    public bool IsOpen() => isOpen;

    private void CancelPanelTween()
    {
        if (panel != null)
            LeanTween.cancel(panel.gameObject);
    }
}
