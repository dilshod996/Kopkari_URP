using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleTouchSensor : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float globalCooldown = 3f;

    private float lastHitTime = -999f;

    public event Action OnTouched;
    public GameObject defendSphere;

    private void OnTriggerEnter(Collider other)
    {
        if(defendSphere != null && defendSphere.activeSelf)
        {
            return;
        }
        if (((1 << other.gameObject.layer) & obstacleLayer.value) == 0)
            return;

        if (Time.time - lastHitTime < globalCooldown)
            return;

        lastHitTime = Time.time;
        OnTouched?.Invoke();
    }
}

