using UnityEngine;
using UnityEngine.EventSystems;

public class HoldInputForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private UIGetLamp target;


    public void OnPointerDown(PointerEventData eventData) => target?.BeginHold();
    public void OnPointerUp(PointerEventData eventData) => target?.EndHold();
    private void OnDisable()
    {
        // Tugma o¡®chganida (SetActive(false)) holatni to¡®xtatamiz
        target?.EndHold();
    }
}
