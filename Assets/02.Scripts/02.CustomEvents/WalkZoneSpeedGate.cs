using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Controller;

public class WalkZoneSpeedGate : MonoBehaviour
{
    

    public void OnGameObjectEnter(GameObject obj)
    {
        // 1. Qobiq yoqilgan bo‘lsa, hech narsa qilmaymiz
        var player = obj.GetComponentInChildren<AttackDefendManager>();
        if (player != null && player.defendQobiq.activeSelf)
        {
            Debug.Log("🛡️ Qobiq yoqilgan — WalkZone ishlamaydi");
            return;
        }

        Debug.Log("Qobiq yoqilmagan — WalkZone ishlaydi");
        var animal = obj.GetComponentInChildren<MAnimal>();
        animal.CurrentSpeedSet.LockSpeed = true;
        animal.CurrentSpeedSet.CurrentIndex = 1;
    }
}
