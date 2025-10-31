using UnityEngine;
using System;

public class PooledVFX : MonoBehaviour
{
    private Action<GameObject> _onDone;
    private ParticleSystem[] _systems;

    public void Init(Action<GameObject> onDone)
    {
        _onDone = onDone;
        _systems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in _systems)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback; // Destroy emas!
        }
    }

    // Har bir sub-particle tugaganda Unity chaqiradi
    void OnParticleSystemStopped()
    {
        // Barcha sub-systemlar to¡®xtaganini tekshiramiz
        foreach (var ps in _systems)
            if (ps.IsAlive(true)) return;

        _onDone?.Invoke(gameObject); // poolga qaytarish
    }
}
