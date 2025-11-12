using UnityEngine;

public class StartButtonAnim : MonoBehaviour
{
    public float speed = 2f;         // Tezligi (yurak urish tezligi)
    public float scaleAmount = 1.1f; // Qancha kattalashadi

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale; // Dastlabki o¡®lchamni saqlab olamiz
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * (scaleAmount - 1);
        transform.localScale = initialScale * scale;
    }
}
