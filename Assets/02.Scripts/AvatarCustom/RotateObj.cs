using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // 👈 bu kerak

public class RotateObj : MonoBehaviour
{
    public float rotationSpeed = 0.2f;

    private Vector2 lastTouchPos;
    private bool isDragging = false;

    void Update()
    {
        if (Touchscreen.current == null || Touchscreen.current.primaryTouch.press.isPressed == false)
            return;

        // 👉 Touch ID orqali tekshir: UI ustida bosilganmi
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue()))
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            lastTouchPos = touch.position.ReadValue();
            isDragging = true;
        }
        else if (touch.press.isPressed)
        {
            if (isDragging)
            {
                Vector2 currentPos = touch.position.ReadValue();
                Vector2 delta = currentPos - lastTouchPos;

                float yRotation = -delta.x * rotationSpeed;
                transform.Rotate(0, yRotation, 0, Space.World);

                lastTouchPos = currentPos;
            }
        }
        else if (touch.press.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}
