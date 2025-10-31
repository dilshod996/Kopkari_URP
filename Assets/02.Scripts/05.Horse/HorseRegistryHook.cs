using UnityEngine;

[DefaultExecutionOrder(-200)] // Bomb'dan oldin ro'yxatdan o'tsin
[RequireComponent(typeof(Rigidbody))]
public class HorseRegistryHook : MonoBehaviour
{
    Rigidbody _rb;
    void Awake() { _rb = GetComponent<Rigidbody>(); }
    void OnEnable() { HorsePhysicsRegistry.Add(_rb); }
    void OnDisable() { HorsePhysicsRegistry.Remove(_rb); }
}
