// RacingAgent.cs
using MalbersAnimations.Controller.AI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
/// <summary>
/// This script located head of horse
/// </summary>
public class RacingAgent : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Rider";
    public string teamName = "Team A";       // 🔹 yangi
    public string countryName = "Uzb";
    public Sprite flagIcon;

    //public Transform pivot;
    public Transform webSnareTarget;
    public Transform shootOriginPoint;
    public bool isPlayer;
    public BoostersContainer boosterContainer;
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
    [SerializeField] private MAnimalAIControl agent;

    // ⬇️ Global start vaqtini berish uchun overload
    public void BeginRace(float globalStartTime)
    {
        if (HasStarted) return;
        HasStarted = true;
        StartTime = globalStartTime;
        FinishTime = 0f;
        HasFinished = false;
        Debug.Log("Race Started");
        RacingController.Instance?.BeginRace();
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
       // if (!pivot) pivot = transform;
    }

    private void OnEnable()
    {
        if (isPlayer)
        {
            displayName = PlayerPrefs.GetString(Constants.Player.UsernameKey);
            teamName = PlayerPrefs.GetString(Constants.Player.TeamName);
        }
    }
    public void DisableNavmesh()
    {
        if (agent == null) return;

        agent.Stop();
        agent.enabled = false;
    }

    public void EnableNavmesh()
    {
        if (agent == null) return;

        agent.enabled = true;
    }

}
