using System.Collections;
using System.Linq;
using UnityEngine;

public class PooledFxController : MonoBehaviour
{
    [Header("Auto")]
    [SerializeField] bool autoDespawnOnEnd = true;
    [SerializeField] float hardMaxLifetime = 0f; // 0=off, >0 bo¡®lsa shu vaqtda majburan Despawn

    ParticleSystem[] ps;
    TrailRenderer[] trails;
    //AudioSource[] audios;
    Coroutine watchCo;

    void Awake()
    {
        ps = GetComponentsInChildren<ParticleSystem>(true);
        trails = GetComponentsInChildren<TrailRenderer>(true);
        //audios = GetComponentsInChildren<AudioSource>(true);

        // Mobil uchun: playOnAwake ni o¡®chirib, boshqaruvni qo¡®lga olamiz
        foreach (var p in ps)
        {
            var m = p.main;
            m.playOnAwake = false;
            // mobil optimizatsiyalarni inspector¡¯da ham bajarish tavsiya:
            // m.simulationSpace = ParticleSystemSimulationSpace.Local;
            // p.collision.enabled = false; p.lights.enabled = false; va h.k.
        }
        //foreach (var a in audios) a.playOnAwake = false;
    }

    void OnEnable()
    {
        // Spawn bo¡®lganda: hammasini tozalab, boshlab beramiz
        foreach (var t in trails) t.Clear();

        foreach (var p in ps)
        {
            p.Clear(true);
            p.Play(true);
        }

        //foreach (var a in audios)
        //{
        //    a.time = 0f;
        //    a.Play();
        //}

        if (watchCo != null) StopCoroutine(watchCo);
        if (autoDespawnOnEnd) watchCo = StartCoroutine(WatchAndDespawn());
    }

    void OnDisable()
    {
        // Despawn bo¡®lganda: to¡®xtatib va tozalab qo¡®yamiz
        foreach (var p in ps)
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        //foreach (var a in audios) a.Stop();
        foreach (var t in trails) t.Clear();

        if (watchCo != null) { StopCoroutine(watchCo); watchCo = null; }
    }

    IEnumerator WatchAndDespawn()
    {
        float timer = 0f;
        // barcha PS o¡®chib bo¡®lguncha kutamiz yoki hardMaxLifetime ga yetguncha
        while (true)
        {
            bool anyAlive = false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i] && ps[i].IsAlive(true)) { anyAlive = true; break; }
            }

            if (!anyAlive) break;

            if (hardMaxLifetime > 0f)
            {
                timer += Time.deltaTime;
                if (timer >= hardMaxLifetime) break;
            }

            yield return null;
        }

        SimplePool.Despawn(gameObject);
    }

    /// <summary>
    /// Agar SimplePool.Spawn(..., lifeTime: X) bermasangiz, inspector¡¯dan autoDespawnOnEnd bilan ishlaydi.
    /// Agar istasangiz, eng uzun PS lifetime¡¯ni hisoblab olishingiz ham mumkin:
    /// </summary>
    public float EstimateMaxDuration()
    {
        float maxDur = 0f;
        foreach (var p in ps)
        {
            var m = p.main;
            var dur = m.duration + (m.loop ? 999f : m.startLifetime.constantMax);
            if (dur > maxDur) maxDur = dur;
        }
        return maxDur;
    }
}
