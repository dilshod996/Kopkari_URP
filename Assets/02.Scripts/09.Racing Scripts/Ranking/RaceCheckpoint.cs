// RaceCheckpoint.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpoint : MonoBehaviour
{
    [Header("Order")]
    public int index;              // 0..N-1
    public RaceCheckpoint next;    // ixtiyoriy (zanjir uchun)

    // Collider -> RacingAgent cache
    private static readonly Dictionary<Collider, RacingAgent> _agentCache = new();

    // Agentga bog'liq holatlar (GLOBAL EMAS)
    private static readonly Dictionary<RacingAgent, int> _lastIndexByAgent = new();
    private static readonly Dictionary<RacingAgent, bool> _reverseByAgent = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead")) return;
        
        // Cache
        if (!_agentCache.TryGetValue(other, out var agent) || agent == null)
        {
            agent = other.GetComponentInParent<RacingAgent>();
            if (agent == null) return;
            _agentCache[other] = agent;
        }
        if (agent.HasFinished) return;
        if(index == 17 && agent.isPlayer && RacingController.Instance != null)
        {
            RacingController.Instance.ShowAndHideSpeech("Faster " + agent.displayName + "! You are near finish!");
        }
        var lb = RacingLeaderboard.Instance;
        int total = lb?.CheckpointCount ?? 0;
        if (total <= 0) return;

        // Agent uchun last index va reverse flagini init qilamiz
        if (!_lastIndexByAgent.TryGetValue(agent, out var last))
        {
            // agentning hozirgi CheckpointIndex'ini boshlang'ich sifatida olamiz
            last = agent.CheckpointIndex;
            _lastIndexByAgent[agent] = last;
        }
        if (!_reverseByAgent.TryGetValue(agent, out var reverseActive))
        {
            reverseActive = false;
            _reverseByAgent[agent] = false;
        }

        // Yo'nalishni aniqlash: last -> index
        int diff = (index - last + total) % total;

        // diff == 0  -> shu checkpointni qayta urish (e’tibor bermasa ham bo‘ladi)
        // diff == 1  -> oldinga 1 qadam
        // diff == total-1 -> orqaga 1 qadam
        // boshqa diff -> sakrash (2+ checkpoint sakrab o’tish) — odatda e’tiborsiz qoldiriladi

        // 🔻 Orqaga ketdi
        if (diff == total - 1 && !reverseActive)
        {
            _reverseByAgent[agent] = true;
            RacingController.Instance?.StartReverse();
        }

        // 🔺 Oldinga qaytdi — reverse’ni o‘chirish sharti:
        // faqat diff == 1 bo‘lganda (ya’ni 1 qadam oldinga)
        if (reverseActive && diff == 1)
        {
            _reverseByAgent[agent] = false;
            RacingController.Instance?.ClearReverse();
        }

        // 🔹 Normal progress faqat diff == 1 holatda
        if (diff == 1)
        {
            agent.PrevCheckpointIndex = agent.CheckpointIndex;
            agent.CheckpointIndex = index;
            agent.Passed++;
            agent.RecordSplit();

            // Agar finish oxirgi checkpoint bo'lsa
            if (index == total - 1)
                agent.EndRace();

            lb?.NotifyCheckpoint(agent);
            // ✅ WalkTrap: faqat normal oldinga qadamda xabar beramiz
            agent.boosterContainer?.NotifyCheckpointPassed(index, total);
        }

        // Teleport yoki diff == 0 bo‘lsa — progress bermaymiz, faqat last’ni yangilaymiz
        _lastIndexByAgent[agent] = index;


    }
}
