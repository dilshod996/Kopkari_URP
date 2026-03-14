using UnityEngine;

public class MoveObject : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float duration = 10f;  // 💡 Bu yerda 10 soniya

    private float elapsedTime = 0f;
    private bool isMoving = true;

    void Start()
    {
        transform.position = startPoint.position;
    }

    void Update()
    {
        if (!isMoving) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);
        transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);

        if (t >= 1f)
        {
            isMoving = false;
            Debug.Log("Yetib keldi!");
        }
    }
}
