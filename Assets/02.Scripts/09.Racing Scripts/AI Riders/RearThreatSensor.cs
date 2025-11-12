using System;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class RearThreatSensor : MonoBehaviour
{
    public event Action ThreatBegan;      // birinchi raqib kirganda
    public event Action ThreatEnded;      // oxirgi raqib chiqqanda

    [SerializeField] private Transform ownerRoot; // otning transformi (forward uchun)
    [SerializeField] private LayerMask enemyMask; // Rider/Horse qatlam(lar)i
    [SerializeField] private float behindDotMax = -0.05f; // < 0 ¡æ orqa sektor

    private readonly HashSet<Transform> _inside = new();
    private Collider _col;

    private void Reset()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
    }

    private void Awake()
    {
        if (!ownerRoot) ownerRoot = transform.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other)) return;
        if (!IsBehind(other.transform)) return;

        if (_inside.Add(other.transform) && _inside.Count == 1)
            ThreatBegan?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("State of enemy: " + IsEnemy(other));
        if (!IsEnemy(other)) return;

        if (_inside.Remove(other.transform) && _inside.Count == 0)
            ThreatEnded?.Invoke();
    }

    private bool IsEnemy(Collider c)
    {
        return ((1 << c.gameObject.layer) & enemyMask) != 0;
    }

    private bool IsBehind(Transform t)
    {
        var fwd = ownerRoot ? ownerRoot.forward : Vector3.forward;
        var dir = (t.position - ownerRoot.position).normalized;
        return Vector3.Dot(fwd, dir) <= behindDotMax; // manfiy ¡æ orqada
    }

    public bool HasThreat => _inside.Count > 0;
}
