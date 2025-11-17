using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragScript : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform panel;       // Panelni o¡®zi
    public bool isForOpening = true;  // Bu tugma ochishmi yoki yopish uchunmi?
    public bool isOpen = false; // Panel ochilganligini tekshirish uchun
    public DragScriptXPos otherPanel;

    private Vector2 dragStartPos;
    public float closedY = -370f;
    public float openY = 262f;
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
        // optional: jonli harakat
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

    public void OnClickClose() // close tugma bosilganda
    {
        ClosePanel();
    }

    public void OpenPanel()
    {
        if (otherPanel != null && otherPanel.IsOpen())
            otherPanel.ClosePanel();
        panel.LeanMoveY(openY, duration);
        isOpen = true; // Panel ochilganligini belgilash
    }
    
    public void ClosePanel()
    {
        panel.LeanMoveY(closedY, duration);
        isOpen = false; // Panel yopilganligini belgilash
    }
    public bool IsOpen() => isOpen;
}
