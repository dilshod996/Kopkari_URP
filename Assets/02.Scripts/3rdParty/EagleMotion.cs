using UnityEngine;

public class EagleMotion : MonoBehaviour
{
    public Transform centerPoint; // Aylanish markazi
    public float radius = 45f;     // Doira radiusi
    public float speed = 0.05f;      // Aylanish tezligi
    public float rotationSmoothTime = 0.1f; // Silliqlashtirish uchun vaqt

    private float angle = 0f;
    private Quaternion targetRotation;

    void FixedUpdate()
    {
        // Burchakni oshirish
        angle += speed * Time.deltaTime;

        // Yangi pozitsiyani hisoblash (x-z tekislik)
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        Vector3 newPosition = new Vector3(centerPoint.position.x + x, transform.position.y, centerPoint.position.z + z);
        transform.position = newPosition;

        // Tangensial yo'nalishni hisoblash
        Vector3 direction = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }

        // Silliq burilish uchun Quaternion.Slerp
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
    }
}
