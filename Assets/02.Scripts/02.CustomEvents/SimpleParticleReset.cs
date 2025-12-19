using UnityEngine;

public class SimpleParticleReset : MonoBehaviour
{
    private ParticleSystem[] ps;
    private TrailRenderer[] trails;
    //private AudioSource[] audios;

    void Awake()
    {
        ps = GetComponentsInChildren<ParticleSystem>(true);
        trails = GetComponentsInChildren<TrailRenderer>(true);
        //audios = GetComponentsInChildren<AudioSource>(true);

        // playOnAwake'ni o¡®chirib qo¡®yamiz
        foreach (var p in ps)
        {
            var m = p.main;
            m.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        // Trail¡¯larni tozalash
        foreach (var t in trails)
            t.Clear();

        // Particle¡¯larni reset + play
        foreach (var p in ps)
        {
            p.Clear(true);          // avvalgi frame'lardan qolganlarni o¡®chiradi
            p.Simulate(0, true, true); // to¡®liq reset
            p.Play(true);           // boshlash
        }

        //foreach (var a in audios)
        //{
        //    a.time = 0f;
        //    a.Play();
        //}
    }

    void OnDisable()
    {
        // Disable bo¡®lganda hammasini to¡®xtatamiz
        foreach (var p in ps)
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        foreach (var t in trails)
            t.Clear();

        //foreach (var a in audios)
        //{
        //    a.Stop();
        //}
    }
}
