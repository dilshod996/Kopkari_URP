using System.Collections;
using UnityEngine;

public class IceZone : MonoBehaviour
{
    public float lifetime = 5f;          // Trap qancha vaqt qoladi
    public float slipForce = 10f;        // Sirpanish kuchi
    public float slipDuration = 1.5f;    // Qancha vaqt sirpanadi

    private void Start()
    {
        //Destroy(gameObject, lifetime); // Vaqt o‘tgach yo‘qoladi
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("✅ Triggered with: " + other.name); // tekshirish

        Rigidbody rb = other.GetComponent<Rigidbody>() ?? other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            StartCoroutine(Slip(rb));
        }
    }

    private IEnumerator Slip(Rigidbody rb)
    {
        float timer = 0f;
        Vector3 direction = rb.velocity.normalized;

        while (timer < slipDuration)
        {
            rb.AddForce(direction * slipForce * Time.deltaTime, ForceMode.VelocityChange);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
