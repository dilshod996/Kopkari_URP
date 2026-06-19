using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RaceCheckpoint : MonoBehaviour
{
    [Header("Order")]
    public int index; // 0..N-1

    // Collider -> RacingAgent cache
    private static readonly Dictionary<Collider, RacingAgent> _agentCache = new();

    // Per-agent state (static bo'lgani yaxshi: bir xil agent hamma checkpointlarda ishlaydi)
    private static readonly Dictionary<RacingAgent, int> _lastIndexByAgent = new();
    private static readonly Dictionary<RacingAgent, bool> _reverseByAgent = new();

    [Header("WebSnare (Optional)")]
    public bool canShootWebSnare = false;

    [Range(0f, 1f)] public float shootChance = 0.6f;   // default chance
    public float top3AiChance = 0.5f;                  // AI top-3 uchun

    public float delayMin = 1f;
    public float delayMax = 3f;

    [Tooltip("Agent bu checkpointda ketma-ket otmasligi uchun (sec)")]
    public float shootCooldown = 5f;

    public GameObject webSnarePrefab;
    public float shootSpeed = 20f;

    // Per-agent cooldown (bu checkpoint instance ichida; xohlasang static ham qilsa bo‘ladi)
    private readonly Dictionary<RacingAgent, float> _lastShootTimeByAgent = new();
    private readonly HashSet<RacingAgent> _shootInProgress = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead")) return;

        // Agent cache
        if (!_agentCache.TryGetValue(other, out var agent) || agent == null)
        {

            agent = other.GetComponentInParent<RacingAgent>();
            if (agent == null) return;
            _agentCache[other] = agent;
            RacingController.Instance.RegisterAgent(agent);
        }

        if (agent.HasFinished) return;

        var lb = RacingLeaderboard.Instance;
        int total = lb?.CheckpointCount ?? 0;
        if (total <= 0) return;

        // init last + reverse
        if (!_lastIndexByAgent.TryGetValue(agent, out int lastIndex))
        {
            lastIndex = agent.CheckpointIndex;
            _lastIndexByAgent[agent] = lastIndex;
        }

        if (!_reverseByAgent.TryGetValue(agent, out bool reverseActive))
        {
            reverseActive = false;
            _reverseByAgent[agent] = false;
        }

        // diff: lastIndex -> this.index (circular)
        int diff = (index - lastIndex + total) % total;

        // 1) Reverse start: 1 qadam orqaga ketdi
        if (diff == total - 1 && !reverseActive)
        {
            _reverseByAgent[agent] = true;
            UIButtonActions.Instance?.StartReverse();
        }

        // 2) Reverse clear: reverse holatda 1 qadam oldinga qaytdi
        if (reverseActive && diff == 1)
        {
            _reverseByAgent[agent] = false;
            UIButtonActions.Instance?.ClearReverse();
        }

        // 3) Normal progress faqat diff == 1 bo'lsa
        if (diff == 1)
        {
            agent.PrevCheckpointIndex = agent.CheckpointIndex;
            agent.CheckpointIndex = index;
            agent.Passed++;
            agent.RecordSplit();

            // ✅ WebSnare faqat normal oldinga o'tishda va reverse yo'q bo'lsa
            if (canShootWebSnare && !_reverseByAgent[agent])
            {
                int rank = lb != null ? lb.GetRank(agent) : -1; // 1..N bo'lishi kerak
                TryScheduleWebSnare(agent, rank);
            }

            // Finish
            if (index == total - 1)
            {
                agent.EndRace();
                RacingController.Instance.FinishRace();
                Debug.Log($"Agent {agent.name} finished {agent.ElapsedTime}");
            }
                

            lb?.NotifyCheckpoint(agent);

            // WalkTrap notify
            agent.boosterContainer?.NotifyCheckpointPassed(index, total);
        }

        // last index update (teleport / qayta urish bo'lsa ham)
        _lastIndexByAgent[agent] = index;
        if (agent.isPlayer && index is 5 or 13 or 16)
        {
            int rank = RacingLeaderboard.Instance.PlayerRank();
            Speech(index, rank);
        }

    }

    // -------------------- WEB SNARE --------------------
    private void TryScheduleWebSnare(RacingAgent agent, int rank)
    {
        if(RacingController.Instance.IsRaceOver)  return; 
        if (webSnarePrefab == null) return;
        if (agent == null || agent.HasFinished) return;

        if (_shootInProgress.Contains(agent)) return;

        // Rank rules
        if (rank == 8 || rank == 7 || rank == 9) return; // umuman otmaydi

        bool forceShoot = (rank >= 1 && rank <= 3); // top-3: "auto urish" (player/ai farqsiz)

        float effectiveChance = shootChance;

        // AI top-3: 0.5 chance bo'lsin (sen xohlaganing)
        if (!agent.isPlayer && (rank >= 1 && rank <= 3))
            effectiveChance = top3AiChance;

        // Force bo'lmasa chance ishlaydi
        if (!forceShoot)
        {
            if (Random.value > effectiveChance) return;
        }

        // Cooldown (force bo'lsa ham cooldownni saqlaymiz — spam bo'lmasin)
        if (_lastShootTimeByAgent.TryGetValue(agent, out float lastTime))
        {
            if (Time.time - lastTime < shootCooldown) return;
        }

        float delay = Random.Range(delayMin, delayMax);
        StartCoroutine(WebSnareRoutine(agent, delay));
    }

    private IEnumerator WebSnareRoutine(RacingAgent agent, float delay)
    {
        _shootInProgress.Add(agent);
        yield return new WaitForSeconds(delay);

        // validate
        if (agent == null || agent.HasFinished)
        {
            _shootInProgress.Remove(agent);
            yield break;
        }

        // delay ichida reverse yoqilib qolsa otmaymiz
        if (_reverseByAgent.TryGetValue(agent, out bool rev) && rev)
        {
            _shootInProgress.Remove(agent);
            yield break;
        }

        if (webSnarePrefab == null || agent.webSnareTarget == null || agent.shootOriginPoint == null)
        {
            _shootInProgress.Remove(agent);
            yield break;
        }

        Vector3 origin = agent.shootOriginPoint.position;
        Vector3 target = agent.webSnareTarget.position;

        Vector3 dir = (target - origin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = agent.webSnareTarget.forward;

        dir.Normalize();
        Quaternion rot = Quaternion.LookRotation(dir);

        // ✅ Instantiate o'rniga SimplePool
        var go = SimplePool.Spawn(webSnarePrefab, origin, Quaternion.LookRotation(dir), lifeTime: 4f);
        if (go != null && go.TryGetComponent<WebSnareProjectile>(out var proj))
        {
            proj.LaunchArc(dir, shootSpeed,7f);
        }


        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = dir * shootSpeed;

        _lastShootTimeByAgent[agent] = Time.time;
        _shootInProgress.Remove(agent);
    }

    private void Speech(int index, int rank)
    {
        string name = PlayerPrefs.GetString(Constants.Player.UsernameKey, "Rider");
        string textData = string.Empty;
        string speechText = string.Empty;

        switch (index)
        {
            case 5:
                textData = LanguageManager.Instance?.GetText(373);
                break;

            case 13:
                textData = (rank == 1)
                    ? LanguageManager.Instance?.GetText(370)
                    : LanguageManager.Instance?.GetText(372);
                break;

            case 16:
                textData = LanguageManager.Instance?.GetText(371);
                break;

            default:
                return;
        }

        if (string.IsNullOrEmpty(textData))
            return;

        speechText = string.Format(textData, name);
        Debug.Log("Call 2");
        if (!string.IsNullOrEmpty(speechText))
            UIButtonActions.Instance?.ShowAndHideSpeech(speechText);
    }

}
