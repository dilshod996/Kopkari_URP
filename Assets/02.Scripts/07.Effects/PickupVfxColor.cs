using UnityEngine;

public class PickupVfxColor : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] targets; // xohlasang inspector¡¯dan 2 ta childni drag qil

    public void SetColor(Color c)
    {
        if (targets != null && targets.Length > 0)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (!targets[i]) continue;
                var main = targets[i].main;
                main.startColor = c;
            }
            return;
        }

        // Inspector¡¯dan bermasang, avtomatik topadi:
        var ps = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            var main = ps[i].main;
            main.startColor = c;
        }
    }
}
