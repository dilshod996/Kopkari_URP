using UnityEngine;
using UnityEngine.EventSystems;

public class HoldInputForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private UIGetLamp target;


    public void OnPointerDown(PointerEventData eventData) => target?.BeginHold(eventData.pointerId);
    public void OnPointerUp(PointerEventData eventData) => target?.EndHold(eventData.pointerId);

    private void OnEnable()
    {
        // Re-entering Malbers pickup focus re-enables this button. UIGetLamp
        // decides whether the original pointer is still physically held.
        target?.FocusReturned();
    }

    private void OnDisable()
    {
        // Malbers hides this button when the player loses pickup focus.
        target?.FocusLost();
    }
}
