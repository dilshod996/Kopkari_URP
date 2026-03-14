using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEnablerRacing : MonoBehaviour
{
    public enum AIState
    {
        Run,
        Stop
    }
    public AIState state= AIState.Run;
    private bool isTutorialNot = false;

    private void OnEnable()
    {
        RacingTutorials.OnDontShowTutorial += ShowTutorial;
    }
    private void OnDisable()
    {
        RacingTutorials.OnDontShowTutorial -= ShowTutorial;
    }
    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead")) return;
        var agent = other.GetComponentInParent<RacingAgent>();
        if (state == AIState.Run && agent.isPlayer)
        {
            RacingController.Instance.EnableNavMesh();
        }
        else
        {
            if (isTutorialNot) return;
            RacingController.Instance.DisableNavmesh();
        }

    }
    private void ShowTutorial(bool sow)
    {
        isTutorialNot = sow;
    }
}
