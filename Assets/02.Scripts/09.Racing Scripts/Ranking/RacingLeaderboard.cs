// RacingLeaderboard.cs (incremental ranking)
using MalbersAnimations.Controller;
using System.Collections.Generic;
using UnityEngine;

public class RacingLeaderboard : MonoBehaviour
{
    public static RacingLeaderboard Instance { get; private set; }
    
    [Header("Refs")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private UIRankingView itemPrefab;
    [SerializeField] private List<RaceCheckpoint> checkpoints = new();
    public int CheckpointCount => checkpoints.Count; // <-- shu orqali hisoblanadi
    // Unikal agentlar
    private readonly List<RacingAgent> standings = new();                      // tartiblangan ro‘yxat
    private readonly HashSet<RacingAgent> agentSet = new();                    // dublikatga yo‘l qo‘ymaydi
    private readonly Dictionary<RacingAgent, UIRankingView> rows = new();      // agent -> UI qatori
    public bool RaceStarted { get; private set; }
    public float RaceStartTime { get; private set; }

    private void Awake() => Instance = this;

    public void Register(RacingAgent agent)
    {
        if (agent == null || agentSet.Contains(agent)) return;

        agentSet.Add(agent);
        standings.Add(agent);

        if (RaceStarted && !agent.HasStarted)
            agent.BeginRace(RaceStartTime);

        var row = Instantiate(itemPrefab, contentParent);
        rows[agent] = row;
        UpdateRow(agent, standings.Count);
    }

    public void Unregister(RacingAgent agent)
    {
        if (agent == null || !agentSet.Remove(agent)) return;

        standings.Remove(agent);
        if (rows.TryGetValue(agent, out var row) && row)
        {
            Destroy(row.gameObject);
        }
        rows.Remove(agent);

        // Pastdagilar rankini yangilash
        for (int i = 0; i < standings.Count; i++)
            UpdateRow(standings[i], i + 1);
    }
    // Global start — bitta vaqtda hamma agentga
    public void StartRace()
    {
        if (RaceStarted) return;
        RaceStarted = true;
        RaceStartTime = Time.time;

        foreach (var a in agentSet)
            if (a != null && !a.HasStarted)
                a.BeginRace(RaceStartTime);
    }

    // Incremental yangilash — BU YERDA BeginRace() ENDI YO'Q!
    public void NotifyCheckpoint(RacingAgent agent)
    {
        if (agent == null) return;
        if (!agentSet.Contains(agent)) Register(agent);

        // ⛔ Poyga boshlanmaguncha ranking/log ishlamasin
        if (!RaceStarted) return;

        int oldIndex = standings.IndexOf(agent);
        if (oldIndex < 0) return;

        int i = oldIndex;
        while (i > 0 && IsAhead(standings[i], standings[i - 1]))
        {
            var tmp = standings[i - 1];
            standings[i - 1] = standings[i];
            standings[i] = tmp;
            i--;
        }

        int from = Mathf.Min(i, oldIndex);
        int to = Mathf.Max(i, oldIndex);
        for (int k = from; k <= to; k++)
        {
            var ag = standings[k];
            ag.Ranking = k + 1;
            rows[ag].transform.SetSiblingIndex(k);
            UpdateRow(ag, k + 1);
        }

        //Debug(startdan keyin)
        //var sb = new System.Text.StringBuilder("RANK: ");
        //for (int r = 0; r < standings.Count; r++)
        //{
        //    var a2 = standings[r];
        //    sb.Append($"{a2.Ranking}.{a2.displayName}[P={a2.Passed},CP={Mathf.Max(0, a2.CheckpointIndex)},T={a2.LastSplitTime:0.00}s] ");
        //}
        //Debug.Log(sb.ToString());

    }


    private static bool IsAhead(RacingAgent A, RacingAgent B)
    {
        if (A.Passed != B.Passed) return A.Passed > B.Passed;
        if (A.CheckpointIndex != B.CheckpointIndex) return A.CheckpointIndex > B.CheckpointIndex;
        return A.ElapsedTime < B.ElapsedTime; // eng tez — oldinda
    }
    private void UpdateRow(RacingAgent a, int rank)
    {
        if (!rows.TryGetValue(a, out var row) || row == null) return;
        int cpShow = Mathf.Max(0, a.CheckpointIndex);
        row.SetData($"{rank}. {a.displayName}", $"CP {cpShow} • Passed {a.Passed}");
        if (a.isPlayer) // yoki o‘zingda qanday belgilang bo‘lsa
        {
            if (ColorUtility.TryParseHtmlString("#FFBF34", out Color highlightColor))
            {
                row.SetColor(highlightColor);
            }
        }
        //else
        //{
        //    row.SetColor(Color.white, new Color(0.8f, 0.8f, 0.8f)); // boshqalar oq-yumshoq rangda
        //}
    }
    // 🟢 Yangi funksiya: joriy standings ro‘yxatini qaytaradi
    public List<RacingAgent> GetStandings()
    {
        // bu ro‘yxat har doim yangilanib turgan — shunchaki nusxasini qaytaramiz
        return new List<RacingAgent>(standings);
    }

    // 🟢 Agar faqat ism va pozitsiyalar kerak bo‘lsa:
    public List<string> GetStandingsNames()
    {
        List<string> names = new();
        for (int i = 0; i < standings.Count; i++)
        {
            var a = standings[i];
            names.Add($"{i + 1}. {a.displayName}");
        }
        return names;
    }
}
