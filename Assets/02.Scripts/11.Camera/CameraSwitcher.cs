using UnityEngine;
using Cinemachine;
using MalbersAnimations;
using System.Collections;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera cam1;
    public CinemachineVirtualCamera cam2;
    public GameObject mainUICanvas;
    public GameObject mobileCanvas;

    private bool isCam1Active = true;
    public bool backFirstCam = false;
    public Vector3 cam1SavedPos;
    public Quaternion cam1SavedRot;

    [SerializeField] private Sprite eagleSprite;
    [SerializeField] private Sprite mainMapSprite;
    [SerializeField] private Button miniMapButton;
    void Start()
    {
        //cam1.Priority = 20;
        //cam2.Priority = 15;
        miniMapButton.onClick.AddListener(SwitchCamera);
    }

    public void SwitchCamera()
    {
        isCam1Active = !isCam1Active;
        Debug.Log("Change camera");

        if (isCam1Active)
        {
            if (cam1.TryGetComponent<ThirdPersonFollowTarget>(out var cam1Script) && cam1Script.CamPivot != null)
            {
                StartCoroutine(RestoreCam1PivotAfterFrame(cam1Script)); // 👈 Asosiy yechim
            }
            cam1.Priority = 10;
            cam2.Priority = 5;
            mobileCanvas.transform.localScale = Vector3.one;
            mainUICanvas.transform.localScale = Vector3.one;
            Debug.Log("Back first cam");
            
            miniMapButton.image.sprite = eagleSprite;
            backFirstCam = true;
        }
        else
        {
            // cam1 dan cam2 ga o‘tayotganda — cam1 pivotini saqlaymiz
            if (cam1.TryGetComponent<ThirdPersonFollowTarget>(out var cam1Script) && cam1Script.CamPivot != null)
            {
                cam1SavedPos = cam1Script.CamPivot.position;
                cam1SavedRot = cam1Script.CamPivot.rotation;
            }

            cam1.Priority = 5;
            cam2.Priority = 10;
            mobileCanvas.transform.localScale = Vector3.zero;
            mainUICanvas.transform.localScale = Vector3.zero;
            miniMapButton.image.sprite = mainMapSprite;
        }
    }
    private IEnumerator RestoreCam1PivotAfterFrame(ThirdPersonFollowTarget cam1Script)
    {
        yield return new WaitForEndOfFrame(); // LateUpdate tugaganidan so‘ng

        cam1Script.lerpPosition.Value = 0f; // Endi ThirdPersonFollowTarget yozmaydi
        cam1Script.CamPivot.position = cam1SavedPos;
        cam1Script.CamPivot.rotation = cam1SavedRot;
    }

}
