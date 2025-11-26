using UnityEngine;
using UnityEngine.EventSystems;
using MalbersAnimations;   // ThirdPersonFollowTarget shu namespaceda

public class UILookBackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    //[Header("Cameras (ThirdPersonFollowTarget)")]
    //[SerializeField] private ThirdPersonFollowTarget mainCam;      // Asosiy kamera
    //[SerializeField] private ThirdPersonFollowTarget lookBackCam;  // Look back kamera

    //[Header("LookBack preset")]
    //[SerializeField] private float distance = 3f;
    //[SerializeField] private float targetYaw = -20f;
    //[SerializeField] private float targetPitch = -13f;
    //[SerializeField] private float verticalOffset = -0.4f; // kerak bo'lsa

    // Tugma bosilganda (bosib turish boshi)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (RacingController.Instance == null) return;

        // Kamerani darhol look back holatiga o¡®tkazamiz
        RacingController.Instance.LookBack();
    }

    // Tugma qo¡®yib yuborilganda
    public void OnPointerUp(PointerEventData eventData)
    {
        if (RacingController.Instance == null) return;

        // Yana asosiy kamerani Aktiv qilamiz
        RacingController.Instance.MainCam();
    }
}
