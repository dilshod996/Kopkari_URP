using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KopkariResultsManager : MonoBehaviour
{
    public static KopkariResultsManager Instance { get; private set; }

    private readonly Dictionary<int, RiderRaceStats> stats = new Dictionary<int, RiderRaceStats>();
    private bool sessionStarted;
    private bool roundStarted;
    private float sessionStartTime;
    private float roundStartTime;
    private float lastRoundDuration;
    private int currentRoundNumber;

    public float RaceDuration { get; private set; }
    public float LastRoundDuration => lastRoundDuration;
    public int CurrentRoundNumber => currentRoundNumber;
    public int WinnerId { get; private set; }
    public string UloqOwner { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        ResetAll();
        Instance = null;
    }

    public void ResetAll()
    {
        stats.Clear();
        sessionStarted = false;
        roundStarted = false;
        sessionStartTime = 0f;
        roundStartTime = 0f;
        lastRoundDuration = 0f;
        currentRoundNumber = 0;
        RaceDuration = 0f;
        WinnerId = 0;
        UloqOwner = string.Empty;
    }

    // Compatibility for older callers.
    public void StartRace()
    {
        StartRound(Mathf.Max(1, currentRoundNumber + 1));
    }

    public void StartRound(int roundNumber)
    {
        if (roundStarted)
            CloseAllActiveHolds();

        if (!sessionStarted)
        {
            sessionStarted = true;
            sessionStartTime = Time.time;
            RaceDuration = 0f;
        }

        currentRoundNumber = Mathf.Max(1, roundNumber);
        roundStartTime = Time.time;
        lastRoundDuration = 0f;
        WinnerId = 0;
        UloqOwner = string.Empty;
        roundStarted = true;

        foreach (RiderRaceStats rider in stats.Values)
            ResetRoundFields(rider);
    }

    public void EndRace()
    {
        CloseAllActiveHolds();
        roundStarted = false;
        if (sessionStarted)
            RaceDuration = Mathf.Max(0f, Time.time - sessionStartTime);
        sessionStarted = false;
    }

    private static void ResetRoundFields(RiderRaceStats rider)
    {
        rider.isHolding = false;
        rider.holdStartTime = 0f;
        rider.finishedWithLamb = false;
        rider.roundPickupTimes = 0;
        rider.roundCarrierTakeovers = 0;
        rider.roundTriggerPoints = 0;
        rider.roundCatchTime = 0f;
        rider.roundCoinPrize = 0;
        rider.roundNyufiyPrize = 0;
        rider.roundComboPrize = 0;
    }

    public void Register(int riderId, string name, string teamName = "Nomads", bool isPlayer = false)
    {
        if (!stats.TryGetValue(riderId, out RiderRaceStats rider))
        {
            rider = new RiderRaceStats { riderId = riderId };
            stats.Add(riderId, rider);
        }

        rider.playerName = string.IsNullOrEmpty(name) ? $"Rider {riderId}" : name;
        rider.isPlayer = isPlayer;
        rider.teamName = teamName;
        if (rider.roundResults == null)
            rider.roundResults = new List<RiderRoundStats>();
    }

    public RiderRaceStats Get(int riderId)
    {
        return stats.TryGetValue(riderId, out RiderRaceStats rider) ? rider : null;
    }

    public void OnLambPicked(int riderId, bool takenFromCarrier = false)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null || rider.isHolding)
            return;

        rider.pickupTimes++;
        rider.roundPickupTimes++;
        if (takenFromCarrier)
        {
            rider.carrierTakeovers++;
            rider.roundCarrierTakeovers++;
        }

        rider.isHolding = true;
        rider.holdStartTime = Time.time;
        UloqOwner = rider.playerName;
    }

    public void OnLambDropped(int riderId)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null || !rider.isHolding)
            return;

        ForceEndHold(rider);
        if (UloqOwner == rider.playerName)
            UloqOwner = string.Empty;
    }

    private static void ForceEndHold(RiderRaceStats rider)
    {
        float held = Mathf.Max(0f, Time.time - rider.holdStartTime);
        rider.totalCatchTime += held;
        rider.roundCatchTime += held;
        rider.isHolding = false;
        rider.holdStartTime = 0f;
    }

    private void CloseAllActiveHolds()
    {
        foreach (RiderRaceStats rider in stats.Values)
        {
            if (rider.isHolding)
                ForceEndHold(rider);
        }
        UloqOwner = string.Empty;
    }

    public void OnTriggerPoint(int riderId)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null)
            return;

        rider.triggerPoints++;
        rider.roundTriggerPoints++;
    }

    public void AwardRoundPrize(int riderId, int coinAmount, int nyufiyAmount)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null)
            return;

        int coins = Mathf.Max(0, coinAmount);
        int nyufiy = Mathf.Max(0, nyufiyAmount);
        rider.coinPrize += coins;
        rider.nyufiyPrize += nyufiy;
        rider.roundCoinPrize += coins;
        rider.roundNyufiyPrize += nyufiy;

        RiderRoundStats snapshot = GetCurrentRoundSnapshot(rider);
        if (snapshot != null)
        {
            snapshot.coinPrize += coins;
            snapshot.nyufiyPrize += nyufiy;
        }
    }

    public void AwardComboPrize(int riderId, int prizeAmount)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null)
            return;

        int prize = Mathf.Max(0, prizeAmount);
        if (prize <= 0)
            return;

        rider.comboPrize += prize;
        rider.comboWins++;
        rider.roundComboPrize += prize;

        RiderRoundStats snapshot = GetCurrentRoundSnapshot(rider);
        if (snapshot != null)
            snapshot.comboPrize += prize;
    }

    private RiderRoundStats GetCurrentRoundSnapshot(RiderRaceStats rider)
    {
        if (rider == null || rider.roundResults == null || rider.roundResults.Count == 0)
            return null;

        RiderRoundStats snapshot = rider.roundResults[rider.roundResults.Count - 1];
        return snapshot != null && snapshot.roundNumber == currentRoundNumber ? snapshot : null;
    }

    public void OnFinish(int riderId)
    {
        if (!roundStarted)
            return;

        RiderRaceStats winner = Get(riderId);
        if (winner == null || !winner.isHolding)
            return;

        float finishTime = Mathf.Max(0f, Time.time - roundStartTime);
        winner.finishedWithLamb = true;
        winner.roundWins++;
        winner.lastRoundFinishTime = finishTime;
        winner.totalWinningTime += finishTime;
        if (winner.bestRoundFinishTime <= 0f || finishTime < winner.bestRoundFinishTime)
            winner.bestRoundFinishTime = finishTime;

        WinnerId = riderId;
        lastRoundDuration = finishTime;
        CloseAllActiveHolds();
        SaveRoundSnapshots(riderId, finishTime);

        roundStarted = false;
        RaceDuration = sessionStarted
            ? Mathf.Max(0f, Time.time - sessionStartTime)
            : finishTime;
    }

    private void SaveRoundSnapshots(int winnerId, float finishTime)
    {
        foreach (RiderRaceStats rider in stats.Values)
        {
            if (rider.roundResults == null)
                rider.roundResults = new List<RiderRoundStats>();

            rider.roundResults.Add(new RiderRoundStats
            {
                roundNumber = currentRoundNumber,
                pickupTimes = rider.roundPickupTimes,
                carrierTakeovers = rider.roundCarrierTakeovers,
                triggerPoints = rider.roundTriggerPoints,
                totalCatchTime = rider.roundCatchTime,
                isWinner = rider.riderId == winnerId,
                finishTime = rider.riderId == winnerId ? finishTime : 0f,
                coinPrize = rider.roundCoinPrize,
                nyufiyPrize = rider.roundNyufiyPrize,
                comboPrize = rider.roundComboPrize
            });
        }
    }

    public List<RiderRaceStats> BuildLeaderboard()
    {
        return stats.Values
            .OrderByDescending(rider => rider.roundWins)
            .ThenByDescending(rider => rider.pickupTimes)
            .ThenByDescending(rider => rider.carrierTakeovers)
            .ThenByDescending(rider => rider.totalCatchTime)
            .ThenByDescending(rider => rider.triggerPoints)
            .ToList();
    }

    public void DebugLogLeaderboard()
    {
        List<RiderRaceStats> leaderboard = BuildLeaderboard();
        if (leaderboard.Count == 0)
        {
            Debug.Log("[Leaderboard] Empty");
            return;
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.AppendLine("===== KOPKARI LEADERBOARD =====");
        for (int i = 0; i < leaderboard.Count; i++)
        {
            RiderRaceStats rider = leaderboard[i];
            text.AppendLine(
                $"#{i + 1} ID:{rider.riderId} {rider.playerName} Team:{rider.teamName} " +
                $"Wins:{rider.roundWins} Pickups:{rider.pickupTimes} " +
                $"Takeovers:{rider.carrierTakeovers} Catch:{rider.totalCatchTime:F2}s " +
                $"LastWin:{rider.lastRoundFinishTime:F2}s BestWin:{rider.bestRoundFinishTime:F2}s " +
                $"Coin:{rider.coinPrize} Nyufiy:{rider.nyufiyPrize} Combo:{rider.comboPrize}");
        }

        Debug.Log(text.ToString());
        Debug.Log($"Overall duration: {RaceDuration:F2}s | Last round: {lastRoundDuration:F2}s");
    }
}
