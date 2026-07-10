using UnityEngine;

public class ArrowDirection : MonoBehaviour
{
    public Transform target;
    public Transform cameraTransform;
    public Transform finishTarget;

    public Vector3 correctionEuler = new Vector3(0, 0, 0);
    public float forwardOffset = 1f; // Player oldida masofa
    public float upwardOffset = 1f;  // Player ustida balandlik
    public float fixedY = 1.5f;

    void Update()
    {
        if (target != null)
        {
            // Playerning x va z koordinatalarini olish, y ni esa fixedY ga belgilash:
            Vector3 offset = cameraTransform.forward * forwardOffset + cameraTransform.up * upwardOffset;
            Vector3 arrowPos = cameraTransform.position + offset;
            transform.position = arrowPos;
            if (AIGameRoom.Instance != null)
            {
                TargetState(AIGameRoom.Instance.IsCatched());
            }
            else if (PracticeRoomManager.Instance != null)
            {
                TargetState(PracticeRoomManager.Instance.IsCatched);
            }
            else if(KopkariManager.Instance != null)
            {
                TargetState(KopkariManager.Instance.IsCatched);
            }
            else
            {
                Debug.LogError("ArrowDirection: TargetState not found");
            }
           
            
        }
    }

    private void TargetState(bool isCachted)
    {
        //AIGameRoom.Instance.IsCatched()
        if (isCachted)
        {
            Vector3 direction = finishTarget.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Agar kerak bo¡®lsa, qo¡®shimcha burilishni qo¡®shish
            Quaternion correction = Quaternion.Euler(correctionEuler);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * correction, Time.deltaTime * 5f);
        }
        else
        {
            Vector3 direction = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Agar kerak bo¡®lsa, qo¡®shimcha burilishni qo¡®shish
            Quaternion correction = Quaternion.Euler(correctionEuler);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * correction, Time.deltaTime * 5f);
        }
    }
}
