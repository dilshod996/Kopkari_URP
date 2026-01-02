using UnityEngine;

public class RoadSegmentTrigger : MonoBehaviour
{

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("RacingHead")) return;

        var agent = other.GetComponentInParent<RacingAgent>();
        if (agent == null || !agent.isPlayer) return;

        triggered = true;
        RoadPreviewManager.Instance?.RequestRebuild();
    }
}
