using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class HoldInputForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static event Action<bool> OnPickupFocusChanged;

    [SerializeField] private UIGetLamp target;


    public void OnPointerDown(PointerEventData eventData) => target?.BeginHold(eventData.pointerId);
    public void OnPointerUp(PointerEventData eventData) => target?.EndHold(eventData.pointerId);

    private void OnEnable()
    {
        // Re-entering Malbers pickup focus re-enables this button. UIGetLamp
        // decides whether the original pointer is still physically held.
        target?.FocusReturned();
        OnPickupFocusChanged?.Invoke(true);
    }

    private void OnDisable()
    {
        // Malbers hides this button when the player loses pickup focus.
        target?.FocusLost();
        OnPickupFocusChanged?.Invoke(false);
    }
}
