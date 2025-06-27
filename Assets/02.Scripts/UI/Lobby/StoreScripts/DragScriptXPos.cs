using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragScriptXPos : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform panel;
    public bool isForOpening = true;
    public bool isOpen = false; // optional: panel ochilganligini tekshirish uchun
    public DragScript otherPanel;
    private Vector2 dragStartPos;
    private float closedX = -290f;
    private float openX = 180f;
    private float threshold = 80f;
    private float duration = 0.3f;

    private void OnEnable()
    {
        ClosePanel(); // Panelni boshlanishida yopish
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // optional: jonli preview
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
        panel.LeanMoveX(openX, duration);
        isOpen = true; // optional: panel ochilganligini belgilash
    }

    public void ClosePanel()
    {
        panel.LeanMoveX(closedX, duration);
        isOpen = false; // optional: panel yopilganligini belgilash
    }
    public bool IsOpen() => isOpen;
}
