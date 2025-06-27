using MalbersAnimations.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkZone : MonoBehaviour
{
    public float lifetime = 5f;
    public Collider walkZoneCollider;
    private void Start()
    {
        //Destroy(gameObject, lifetime);
        StartCoroutine(AutoDestroy());
    }
    public void OnGameObjectEnter(GameObject obj)
    {
        // 1. Qobiq yoqilgan bo‘lsa, hech narsa qilmaymiz
        var player = obj.GetComponentInChildren<AttackDefendManager>();
        if (player != null && player.defendQobiq.activeSelf)
        {
            Debug.Log(" Qobiq yoqilgan — WalkZone ishlamaydi");
            return;
        }
        if (player.defendCount > 0 && player.isNpc)
        {
            player.DefendPlayerNpc();
            return;
        }
        Debug.Log("Qobiq yoqilmagan — WalkZone ishlaydi");
        var animal = obj.GetComponentInChildren<MAnimal>();
        if (animal != null)
        {
            animal.CurrentSpeedSet.LockSpeed = true;
            //animal.CurrentSpeedSet.CurrentIndex = 1;
        }


    }
    public void OnGameObjectExit(GameObject obj)
    {
        var animal = obj.GetComponentInChildren<MAnimal>();
        if (animal != null && animal.CurrentSpeedSet != null)
        {
            animal.CurrentSpeedSet.LockSpeed = false;

            
        }
    }
    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime);

        if (walkZoneCollider != null)
        {
            transform.position += Vector3.down * 15f;
            Debug.Log("📉 WalkZone yer ostiga tushirildi - TriggerExit ishga tushadi");
        }

        yield return new WaitForSeconds(0.1f); // optional delay for safety
        Destroy(gameObject);
    }
    public void TestCollider()
    {
        Debug.Log("Collider stopped");
    }
}
