using UnityEngine;
using Cinemachine;
using MalbersAnimations;

public class DollyCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera thirdPersonCamera;
    [SerializeField] private CinemachineVirtualCamera dollyCamera;
    [SerializeField] private CinemachineSmoothPath dollyTrack; // CinemachineSmoothPath ishlatilmoqda
    [SerializeField] private float moveSpeed = 2.0f;
    public Transform mainCameraTransform;

    private float pathPosition = 0f;
    private bool isMoving = true;
    private CinemachineTrackedDolly dollyComponent;

    private Vector3 fixedPosition = new Vector3(367.523468f, 3.00912809f, 180.335632f);
    private Quaternion fixedRotation = Quaternion.Euler(0.470444053f, 32.5044556f, -2.66813238e-08f);

    public ThirdPersonFollowTarget camPos;


    private void Awake()
    {
        dollyComponent = dollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        dollyComponent.m_PathPosition = 0f;
    }
    private void Start()
    {
        if (camPos != null && camPos.CamPivot != null)
        {
            camPos.CamPivot.position = new Vector3(370, 3, 185);
            camPos.CamPivot.rotation = Quaternion.Euler(10, 30, 0);
        }
    }
    void Update()
    {
        if (isMoving)
        {
            // Waypointga qarab harakat qilish
            pathPosition += Time.deltaTime * moveSpeed;

            // Agar pathPosition oxirgi waypointga yetgan bo¡®lsa, uni qaytadan boshlash
            if (pathPosition >= dollyTrack.PathLength)
            {
                pathPosition = 0f; // Yangi boshlangan holat
            }

            // Kameraning dolly yo¡®li bo¡®ylab harakatini yangilash
            dollyComponent.m_PathPosition = pathPosition;
        }
    }

    // Start tugmasi bosilganda 3rd person kameraga o'tish
    public void StartGame()
    {
        mainCameraTransform.position = fixedPosition;
        mainCameraTransform.rotation = fixedRotation;
        isMoving = false; // Harakatni to¡®xtatish
        dollyCamera.Priority = 5;  // Dolly kamerani pasaytirish
        thirdPersonCamera.Priority = 10;  // 3rd person kamerani faollashtirish
    }

    // Dolly kameraga qaytish
    public void BackToDolly(float cameraStartPos)
    {
        isMoving = true;  // Yana harakat qilishni boshlash
        dollyCamera.Priority = 10; // Dolly kamerani ustuvor qilish
        thirdPersonCamera.Priority = 5; // 3rd person kamerani pastroq qilish
        pathPosition = cameraStartPos; // Kamera holatini qayta boshlash
        dollyComponent.m_PathPosition = pathPosition;
    }


}
