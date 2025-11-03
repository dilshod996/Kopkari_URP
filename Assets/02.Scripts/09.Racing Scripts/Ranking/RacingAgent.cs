// RacingAgent.cs
using UnityEngine;

public class RacingAgent : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Rider";
    public string teamName = "Team A";       // 🔹 yangi
    public float earnings = 0f;              // 🔹 yangi
    public Transform pivot;

    public int CheckpointIndex { get; set; } = -1;    // hali o‘tmagan
    public int PrevCheckpointIndex { get; set; } = -1;
    public int Ranking { get; set; } = 0;     // 🔹 leaderboard set qiladi
    // Yangi: lap o‘rniga monoton progress
    public int Passed { get; set; } = 0;
    // 🕒 Yangi: startdan finishgacha bo‘lgan vaqt
    public bool HasStarted { get; private set; }
    public bool HasFinished { get; private set; }
    public float StartTime { get; private set; }
    public float FinishTime { get; private set; }

    // 🔧 start berilmaguncha 0 qaytar
    public float ElapsedTime => HasStarted ? ((HasFinished ? FinishTime : Time.time) - StartTime) : 0f;

    // 🧭 Oxirgi checkpointdan o‘tgan ON (split) vaqti
    public float LastSplitTime { get; private set; } = 0f;

    public void BeginRace()
    {
        if (HasStarted) return;
        HasStarted = true;
        StartTime = Time.time;
        FinishTime = 0f;
        HasFinished = false;
    }

    // ⬇️ Global start vaqtini berish uchun overload
    public void BeginRace(float globalStartTime)
    {
        if (HasStarted) return;
        HasStarted = true;
        StartTime = globalStartTime;
        FinishTime = 0f;
        HasFinished = false;
    }

    public void EndRace()
    {
        if (HasFinished) return;
        HasFinished = true;
        FinishTime = Time.time;
    }
    // ✅ Checkpoint kesilganda chaqirasiz: splitni “muzlatib” yozib qo‘yadi
    public void RecordSplit()
    {
        LastSplitTime = ElapsedTime;
    }
    private void Awake()
    {
        if (!pivot) pivot = transform;
    }
    
}
