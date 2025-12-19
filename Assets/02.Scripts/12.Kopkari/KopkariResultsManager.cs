using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KopkariResultsManager : MonoBehaviour
{
    public static KopkariResultsManager Instance { get; private set; }

    private readonly Dictionary<int, RiderRaceStats> _stats = new();


    private bool _raceStarted;
    private float _raceStartTime;
    public float RaceDuration { get; private set; }
    public string UloqOwner;

    // Optional: kim birinchi lamb bilan finishga kirdi (winner id)
    public int WinnerId { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Manager yo‘q bo‘lsa ham data qolib ketmasin
        if (Instance == this)
        {
            ResetAll();
            Instance = null;
        }
    }

    // =========================
    // Life-cycle controls
    // =========================

    public void ResetAll()
    {
        _stats.Clear();

        _raceStarted = false;
        _raceStartTime = 0f;

        WinnerId = 0;
    }

    public void StartRace()
    {
        _raceStarted = true;
        _raceStartTime = Time.time;
        WinnerId = 0;
        RaceDuration = 0f;
        // Agar oldindan register bo‘lganlar bo‘lsa — finished/holding reset qilamiz
        foreach (var s in _stats.Values)
            ResetRuntimeFields(s);
    }

    public void EndRace()
    {
        _raceStarted = false;

        // Kimdir uloqni ushlab turgan bo‘lsa ham yopib qo‘yamiz (optional)
        foreach (var s in _stats.Values)
        {
            if (s.isHolding)
                ForceEndHold(s);
        }
    }

    private void ResetRuntimeFields(RiderRaceStats s)
    {
        s.isHolding = false;
        s.holdStartTime = 0f;
;
        s.finishedWithLamb = false;

        s.totalCatchTime = 0f;

        s.pickupTimes = 0;
        s.triggerPoints = 0;
    }

    // =========================
    // Register / Get
    // =========================

    public void Register(int riderId, string name, string teamName = "Nomads", bool isPlayer = false)
    {
        if (!_stats.TryGetValue(riderId, out var s))
        {
            s = new RiderRaceStats { riderId = riderId };
            _stats.Add(riderId, s);
        }

        s.playerName = string.IsNullOrEmpty(name) ? $"Rider {riderId}" : name;
        s.isPlayer = isPlayer;
        s.teamName = teamName;
    }


    public RiderRaceStats Get(int riderId)
    {
        return _stats.TryGetValue(riderId, out var s) ? s : null;
    }


    // =========================
    // Gameplay events
    // =========================

    public void OnLambPicked(int riderId)
    {
        var s = Get(riderId);
        if (s == null) return;

        s.pickupTimes++;

        if (!s.isHolding)
        {
            s.isHolding = true;
            s.holdStartTime = Time.time;
        }
        UloqOwner = s.playerName;
    }

    public void OnLambDropped(int riderId)
    {
        var s = Get(riderId);
        if (s == null) return;

        if (!s.isHolding) return;
        ForceEndHold(s);
        UloqOwner = string.Empty;
    }

    private void ForceEndHold(RiderRaceStats s)
    {
        float held = Mathf.Max(0f, Time.time - s.holdStartTime);
        s.totalCatchTime += held;

        s.isHolding = false;
        s.holdStartTime = 0f;
    }

    public void OnTriggerPoint(int riderId)
    {
        var s = Get(riderId);
        if (s == null) return;

        s.triggerPoints++;
    }

    public void OnFinish(int riderId)
    {
        var s = Get(riderId);
        if (s == null) return;
        if (s.finishedWithLamb) return; // allaqachon winner bor
        // Finish paytida uloq qo‘lida bo‘lsa
        s.finishedWithLamb = s.isHolding;
        // Winner: birinchi bo‘lib lamb bilan finishga kirgan
        if (s.finishedWithLamb && WinnerId==0)
            WinnerId = s.riderId;
        if (_raceStarted && RaceDuration <= 0f)
            RaceDuration = Mathf.Max(0f, Time.time - _raceStartTime);
        // Finishda hold yopilsin (overall catch time to‘liq bo‘lsin)
        if (s.isHolding)
            ForceEndHold(s);
        _raceStarted = false;
    }

    // =========================
    // Leaderboard
    // =========================

    public List<RiderRaceStats> BuildLeaderboard()
    {
        return _stats.Values
            // 1) winner har doim tepada
            .OrderByDescending(s => s.finishedWithLamb)

            // 2) pickup ko‘p bo‘lsa yuqori
            .ThenByDescending(s => s.pickupTimes)

            // 3) lambni ko‘p ushlab turgan yuqori
            .ThenByDescending(s => s.totalCatchTime)

            // 4) trigger ko‘p bo‘lsa yuqori
            .ThenByDescending(s => s.triggerPoints)
            .ToList();
    }

    public void DebugLogLeaderboard()
    {
        var list = BuildLeaderboard();

        if (list == null || list.Count == 0)
        {
            Debug.Log("[Leaderboard] Empty");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("===== LEADERBOARD =====");

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];

            sb.AppendLine(
                $"#{i + 1} | " +
                $"ID:{s.riderId} | " +
                $"{s.playerName} | " +
                $"Team:{s.teamName} | " +
                $"Pickup:{s.pickupTimes} | " +
                $"Catch:{s.totalCatchTime:F2}s | " +
                $"Triggers:{s.triggerPoints} | " +
                $"Winner:{s.finishedWithLamb}"
            );
        }

        Debug.Log(sb.ToString());
        Debug.Log($"RaceDuration: {RaceDuration:F2}s");

    }


}
