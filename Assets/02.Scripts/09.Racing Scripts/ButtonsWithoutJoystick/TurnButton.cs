using UnityEngine;
using UnityEngine.EventSystems;

public class TurnButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("-1 = Chap, +1 = O¡®ng")]
    [Range(-1f, 1f)] public float direction = -1f;

    /// <summary>Tugma bosilgan-yo¡®q holati</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Tugma bosilganda signal (masalan, JoystickTurnMixer o¡®qiydi)</summary>
    public float CurrentDir => IsPressed ? direction : 0f;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }

    private void OnDisable()
    {
        // UI o¡®chirilib qolsa ham nolga qaytsin
        IsPressed = false;
    }
}
