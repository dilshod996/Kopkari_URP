using MalbersAnimations;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public MobileJoystickX joystick;
    public bool isRight;

    public void OnPointerDown(PointerEventData e)
    {
        if (!joystick) return;
        if (isRight) joystick.TurnRightDown(); else joystick.TurnLeftDown();
        Debug.Log($"{name} DOWN");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (joystick) joystick.TurnButtonUp();
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (joystick) joystick.TurnButtonUp(); // barmoq panjara tashqarisiga chiqsa ham stop
    }

    void OnDisable()
    {
        if (joystick) joystick.TurnButtonUp();
    }
}
