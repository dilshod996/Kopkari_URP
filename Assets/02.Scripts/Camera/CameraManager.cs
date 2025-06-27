using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera mountCamera;
    public CinemachineVirtualCamera mainCamera;
    public CinemachineVirtualCamera dollyCamera;

    private enum CameraState { Mount, Main, Dolly }
    [SerializeField] private CameraState currentState = CameraState.Dolly;

    private bool hasMounted = false;

    [Header("DollySettings")]
    [SerializeField] private CinemachineSmoothPath dollyTrack; // CinemachineSmoothPath ishlatilmoqda
    [SerializeField] private float moveSpeed = 2.0f;
    public Transform mainCameraTransform;

    private float pathPosition = 0f;
    private bool isMoving = true;
    private CinemachineTrackedDolly dollyComponent;

    //private Vector3 fixedPosition = new Vector3(367.523468f, 3.00912809f, 180.335632f);
    //private Quaternion fixedRotation = Quaternion.Euler(0.470444053f, 32.5044556f, -2.66813238e-08f);

    private void Awake()
    {
        dollyComponent = dollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        dollyComponent.m_PathPosition = 0f;
        //SwitchToDollyCamera(0f);
    }
    void Update()
    {
        if (currentState==CameraState.Dolly)
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
    public void UseMountCamera()
    {
        if (hasMounted) return;

        mountCamera.Priority = 20;
        mainCamera.Priority = 10;
        dollyCamera.Priority = 5;

        currentState = CameraState.Mount;
    }
    public void OnMountComplete()
    {
        hasMounted = true;
        mountCamera.Priority = 10;
        mainCamera.Priority = 20;
        currentState = CameraState.Main;
    }

    public void SwitchToMainCamera()
    {
        if (currentState == CameraState.Main) return;

        mainCamera.Priority = 20;
        dollyCamera.Priority = 15;
        currentState = CameraState.Main;
    }

    public void SwitchToDollyCamera(float cameraStartPos)
    {
        if (currentState == CameraState.Dolly) return;

        dollyCamera.Priority = 20;
        mainCamera.Priority = 15;

        currentState = CameraState.Dolly;
        pathPosition = cameraStartPos; // Kamera holatini qayta boshlash
        dollyComponent.m_PathPosition = pathPosition;
    }
}
