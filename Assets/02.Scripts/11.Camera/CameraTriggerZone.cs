using UnityEngine;
using Cinemachine;
using MalbersAnimations;

public class CameraTriggerZone : MonoBehaviour
{
    public CinemachineVirtualCamera cam1;
    public CinemachineVirtualCamera cam2;
    public float xPos = 0;
    public float yPos = 90f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(BaseManager.Instance.currentCondition == BaseManager.PlayerCondition.GotTarget)
            {
                return;
            }
            var thirdPerson = cam2.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            //var thirdPersonLookCam = cam2.GetComponent<ThirdPersonFollowTarget>();
            if (thirdPerson != null)
            {
                if (BaseManager.Instance.currentCondition == BaseManager.PlayerCondition.TakenTargetOthers)
                {
                    thirdPerson.ShoulderOffset = new Vector3(-0.9f, 0.3f, 0f); // Chapdan qarasin
                    
                }
                else
                {
                    thirdPerson.ShoulderOffset = new Vector3(0.9f, 0.3f, 0f); // O'ngdan qarasin
                    //thirdPersonLookCam.SetRotation(xPos, yPos);
                }
            }
            cam1.Priority = 10;
            cam2.Priority = 15;
            //Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("NameUI")); // yashirish
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cam1.Priority = 15;
            cam2.Priority = 10;
            //Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("NameUI")); // ko¡®rsatish
        }
    }
}
