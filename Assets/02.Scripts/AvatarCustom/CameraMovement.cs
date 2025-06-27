using UnityEngine;
using UnityEngine.UI; 
public class CameraMovement : MonoBehaviour
{
    public Camera mainCamera; 
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool shouldMove = false;
    private bool shouldMoveBack = false;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button backButton;

    void Start()
    {
        // Save initial camera position and rotation
        initialPosition = mainCamera.transform.position;
        initialRotation = mainCamera.transform.rotation;

        // Example: Get buttons and add listeners
        moveButton.onClick.AddListener(OnMoveButtonClick);
        backButton.onClick.AddListener(OnBackButtonClick);
    }

    void OnMoveButtonClick()
    {
        shouldMove = true;
        shouldMoveBack = false;
    }

    void OnBackButtonClick()
    {
        shouldMoveBack = true;
        shouldMove = false;
    }

    void Update()
    {
        if (shouldMove)
        {

            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, moveSpeed * Time.deltaTime);


            Quaternion targetRot = Quaternion.Euler(targetRotation);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

            // Check if the camera is close enough to stop moving
            if (Vector3.Distance(mainCamera.transform.position, targetPosition) < 0.1f &&
                Quaternion.Angle(mainCamera.transform.rotation, targetRot) < 0.1f)
            {
                shouldMove = false;
            }
        }

        if (shouldMoveBack)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, initialPosition, moveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, initialRotation, rotateSpeed * Time.deltaTime);

            if (Vector3.Distance(mainCamera.transform.position, initialPosition) < 0.1f &&
                Quaternion.Angle(mainCamera.transform.rotation, initialRotation) < 0.1f)
            {
                shouldMoveBack = false;
            }
        }
    }
}
