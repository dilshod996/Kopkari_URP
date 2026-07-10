using UnityEngine;
using UnityEngine.EventSystems;

public class DragScript : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform panel;
    public bool isForOpening = true;
    public bool isOpen = false;
    public DragScriptXPos otherPanel;

    private Vector2 dragStartPos;
    public float closedY = -370f;
    public float openY = 262f;
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
        float dragDelta = eventData.position.y - dragStartPos.y;

        if (isForOpening && dragDelta > threshold)
        {
            OpenPanel();
        }
        else if (!isForOpening && dragDelta < -threshold)
        {
            ClosePanel();
        }
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
        panel.LeanMoveY(openY, duration);
        isOpen = true;
    }

    public void ClosePanel()
    {
        if (panel == null) return;

        CancelPanelTween();
        panel.LeanMoveY(closedY, duration);
        isOpen = false;
    }

    public bool IsOpen() => isOpen;

    private void CancelPanelTween()
    {
        if (panel != null)
            LeanTween.cancel(panel.gameObject);
    }
}
