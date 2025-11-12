using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleTouchSensor : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask obstacleLayer;  // faqat shu layer bilan urilsa
    [SerializeField] private float globalCooldown = 3f; // necha sekunddan keyin qayta sanasin

    private float lastGlobalHitTime = -999f; // oxirgi hit vaqti

    public Action<GameObject> OnObstacleHit;
    private void OnTriggerEnter(Collider other)
    {
        // faqat obstacle layer bilan urilsa
        if (((1 << other.gameObject.layer) & obstacleLayer.value) == 0) return;

        // global cooldown ishlasin
        if (Time.time - lastGlobalHitTime < globalCooldown)
            return; // hali 3 sekund o‘tmagan — e’tiborga olmaymiz
        lastGlobalHitTime = Time.time; // yangilaymiz
        OnObstacleHit?.Invoke(other.gameObject);
        // Debug.Log($"Obstacle hit: {other.name}");
    }
}
