using UnityEngine;
using Cinemachine;
using System.Collections;

public class PooledExplosionFx : MonoBehaviour
{
    [Header("Auto Despawn")]
    [SerializeField] private float fallbackLife = 2f; // safety (agar duration topilmasa)

    [Header("Impulse")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private bool useVelocityAsImpulseDir = false; // xohlasang

    private ParticleSystem[] psList;
    private float maxDuration;
    private Coroutine despawnCo;

    private void Awake()
    {
        // child particle¡¯larni ham olamiz
        psList = GetComponentsInChildren<ParticleSystem>(true);

        // duration hisoblab qo¡¯yamiz (bitta marta)
        maxDuration = 0f;
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            var main = ps.main;

            // duration + lifetime approximate
            float dur = main.duration;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                dur += main.startLifetime.constantMax;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                dur += main.startLifetime.constant;
            else
                dur += 1f; // curve bo¡®lsa taxmin

            if (dur > maxDuration) maxDuration = dur;
        }

        if (maxDuration <= 0.05f) maxDuration = fallbackLife;

        // Optional: inspector¡¯dan topilmasa o¡®zi olsin
        if (!impulseSource) impulseSource = GetComponentInChildren<CinemachineImpulseSource>(true);
    }

    private void OnEnable()
    {
        // oldingi coroutine bo¡®lsa tozalaymiz
        if (despawnCo != null) StopCoroutine(despawnCo);

        // particle¡¯larni qayta start (pool uchun eng muhim)
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        for (int i = 0; i < psList.Length; i++)
        {
            psList[i].Play(true);
        }

        // impulse
        if (impulseSource != null)
        {
            if (useVelocityAsImpulseDir)
            {
                // agar prefab rigidbody/velocity bilan ishlasa (ixtiyoriy)
                var rb = GetComponent<Rigidbody>();
                if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
                    impulseSource.GenerateImpulse(rb.velocity.normalized);
                else
                    impulseSource.GenerateImpulse();
            }
            else
            {
                impulseSource.GenerateImpulse();
            }
        }

        // life tugaganda poolga qaytar
        despawnCo = StartCoroutine(DespawnAfter(maxDuration));
    }

    private IEnumerator DespawnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        ReturnToPool();
    }

    private void OnDisable()
    {
        if (despawnCo != null) StopCoroutine(despawnCo);
        despawnCo = null;
    }

    private void ReturnToPool()
    {
        // SimplePool bo¡®lsa:
        SimplePool.Despawn(gameObject);
        // bo¡®lmasa: gameObject.SetActive(false);
    }
}
