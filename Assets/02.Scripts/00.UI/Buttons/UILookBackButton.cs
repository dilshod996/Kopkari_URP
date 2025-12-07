using UnityEngine;
using UnityEngine.EventSystems;
using MalbersAnimations;
using System;   // ThirdPersonFollowTarget shu namespaceda

public class UILookBackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    public static Action<bool> OnCameraPressedState;
    public void OnPointerDown(PointerEventData eventData)
    {
        //if (RacingController.Instance == null) return;
        //RacingController.Instance.LookBack();

        OnCameraPressedState?.Invoke(true);

    }

    // Tugma qo¡®yib yuborilganda
    public void OnPointerUp(PointerEventData eventData)
    {
        OnCameraPressedState?.Invoke(false);
        //if (RacingController.Instance == null) return;
        //RacingController.Instance.MainCam();
    }
}
