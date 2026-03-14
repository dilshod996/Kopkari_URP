using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReinZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
{
    public enum Side { Left, Right }
    [SerializeField] private Side side;

    [Header("Pull Settings")]
    [Tooltip("Necha pixel tortilsa 1.0 bo'ladi")]
    [SerializeField] private float maxPullPixels = 180f;

    [Tooltip("Odatda pastga tortish qulay (deltaY pastga = pull)")]
    [SerializeField] private bool pullDown = true;

    public float Pull01 { get; private set; }   // 0..1
    public bool IsHeld { get; private set; }

    private int _pointerId = int.MinValue;
    private Vector2 _startPos;
    public static event Action OnRightReinUsed;
    public static event Action OnLeftReinUsed;
    private bool _dragTriggered;
    [SerializeField] private float tutorialTriggerThreshold = 0.25f;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsHeld) return;

        IsHeld = true;
        _pointerId = eventData.pointerId;
        _startPos = eventData.position;
        Pull01 = 0f;
        _dragTriggered = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsHeld || eventData.pointerId != _pointerId) return;

        Vector2 delta = eventData.position - _startPos;

        float pullPixels = 0f;
        if (pullDown)
        {
            pullPixels = Mathf.Max(0f, -delta.y);
        }
        else
        {
            pullPixels = (side == Side.Left) ? Mathf.Max(0f, -delta.x) : Mathf.Max(0f, delta.x);
        }

        Pull01 = Mathf.Clamp01(pullPixels / Mathf.Max(1f, maxPullPixels));

        // 🔥 tutorial uchun bir marta event yuboramiz
        if (!_dragTriggered && Pull01 >= tutorialTriggerThreshold)
        {
            _dragTriggered = true;

            if (side == Side.Right)
                OnRightReinUsed?.Invoke();
            else
                OnLeftReinUsed?.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _pointerId) return;
        ResetState();
    }

    public void OnCancel(BaseEventData eventData)
    {
        ResetState();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void ResetState()
    {
        IsHeld = false;
        _pointerId = int.MinValue;
        Pull01 = 0f;
        _dragTriggered = false;
    }
}
