using System;
using UnityEngine;

public class ObstacleTouchSensor : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float globalCooldown = 3f;

    private float lastHitTime = -999f;

    public event Action OnTouched;
    public GameObject defendSphere;
    public Vector3 LastHitPosition { get; private set; }
    public Quaternion LastHitRotation { get; private set; } = Quaternion.identity;

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
        LastHitPosition = other.ClosestPoint(transform.position);

        Vector3 impactDirection = transform.position - LastHitPosition;
        if (impactDirection.sqrMagnitude > 0.0001f)
            LastHitRotation = Quaternion.LookRotation(impactDirection.normalized, transform.up);
        else
            LastHitRotation = transform.rotation;

        OnTouched?.Invoke();
    }
}

