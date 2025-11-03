// RaceCheckpoint.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpoint : MonoBehaviour
{
    [Header("Order")]
    public int index;              // 0..N-1
    public RaceCheckpoint next;    // ixtiyoriy (zanjir uchun)

    // Collider -> RacingAgent cache (GetComponentInParent ni bir marta qilamiz)
    private static readonly Dictionary<Collider, RacingAgent> _cache = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead")) return;

        if (!_cache.TryGetValue(other, out var agent) || agent == null)
        {
            agent = other.GetComponentInParent<RacingAgent>();
            if (agent == null) return;
            _cache[other] = agent;
        }
        if (agent.HasFinished) return;

        var lb = RacingLeaderboard.Instance;
        int total = lb?.CheckpointCount ?? 0;
        if (total <= 0) return;

        int expected = (agent.CheckpointIndex + 1 + total) % total;
        if (index != expected) return;

        agent.PrevCheckpointIndex = agent.CheckpointIndex;
        agent.CheckpointIndex = index;
        agent.Passed++;
        agent.RecordSplit();
        if (index == total - 1)
            agent.EndRace();

        lb?.NotifyCheckpoint(agent);
    }

}
