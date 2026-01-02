using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinalCollider : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead")) return;
        var agent = other.GetComponentInParent<RacingAgent>();
        if (agent.isPlayer)
        {
            RacingController.Instance.StopHorseRun();
        }

    }
}
