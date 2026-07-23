using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KopkariResultsManager : MonoBehaviour
{
    public const int NoWinnerId = -1;

    public static KopkariResultsManager Instance { get; private set; }

    private readonly Dictionary<int, RiderRaceStats> stats = new Dictionary<int, RiderRaceStats>();
    private bool sessionStarted;
    private bool roundStarted;
    private float sessionStartTime;
    private float roundStartTime;
    private float lastRoundDuration;
    private float completedRoundDuration;
    private int currentRoundNumber;
    private KopkariMatchRewardSummary playerRewardSummary;
    private bool playerRewardGranted;
    private bool horseConditionApplied;
    private HorseConditionStats finalHorseCondition;

    public float RaceDuration { get; private set; }
    public float LastRoundDuration => lastRoundDuration;
    public int CurrentRoundNumber => currentRoundNumber;
    public int WinnerId { get; private set; } = NoWinnerId;
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
        completedRoundDuration = 0f;
        currentRoundNumber = 0;
        RaceDuration = 0f;
        WinnerId = NoWinnerId;
        UloqOwner = string.Empty;
        playerRewardSummary = null;
        playerRewardGranted = false;
        horseConditionApplied = false;
        finalHorseCondition = default;
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
            completedRoundDuration = 0f;
            RaceDuration = 0f;
        }

        currentRoundNumber = Mathf.Max(1, roundNumber);
        roundStartTime = Time.time;
        lastRoundDuration = 0f;
        WinnerId = NoWinnerId;
        UloqOwner = string.Empty;
        roundStarted = true;

        foreach (RiderRaceStats rider in stats.Values)
            ResetRoundFields(rider);
    }

    public void EndRace()
    {
        if (roundStarted)
        {
            lastRoundDuration = Mathf.Max(0f, Time.time - roundStartTime);
            completedRoundDuration += lastRoundDuration;
            CloseAllActiveHolds();
            SaveRoundSnapshots(NoWinnerId, 0f);
        }
        else
        {
            CloseAllActiveHolds();
        }
        roundStarted = false;
        RaceDuration = completedRoundDuration;
        sessionStarted = false;
    }

    public void EndRoundWithoutWinner()
    {
        if (!roundStarted)
            return;

        CloseAllActiveHolds();
        lastRoundDuration = Mathf.Max(0f, Time.time - roundStartTime);
        completedRoundDuration += lastRoundDuration;
        SaveRoundSnapshots(NoWinnerId, 0f);
        roundStarted = false;
        RaceDuration = completedRoundDuration;
    }

    public int SimulateRemainingRounds(int winnerId, IReadOnlyList<float> roundTimeLimits)
    {
        if (roundTimeLimits == null || roundTimeLimits.Count == 0)
            return 0;

        if (roundStarted || playerRewardSummary != null || playerRewardGranted)
        {
            Debug.LogWarning("[KopkariResultsManager] Remaining rounds cannot be simulated after results are finalized or while a round is active.");
            return 0;
        }

        RiderRaceStats winner = Get(winnerId);
        if (winner == null || winner.isPlayer)
        {
            Debug.LogWarning($"[KopkariResultsManager] Simulated winner ID {winnerId} is not a registered AI rider.");
            return 0;
        }

        int simulatedRoundCount = 0;
        for (int i = 0; i < roundTimeLimits.Count; i++)
        {
            float roundTimeLimit = Mathf.Max(1f, roundTimeLimits[i]);
            int roundNumber = currentRoundNumber + 1;
            float finishTime = GetSimulatedFinishTime(winnerId, roundNumber, roundTimeLimit);
            float winnerCatchTime = GetSimulatedWinnerCatchTime(winnerId, roundNumber, finishTime);
            float nonWinnerCatchPool = finishTime * 0.35f;
            float nonWinnerWeightTotal = 0f;

            foreach (RiderRaceStats rider in stats.Values)
            {
                if (!rider.isPlayer && rider.riderId != winnerId)
                    nonWinnerWeightTotal += GetSimulatedActivityWeight(rider.riderId, roundNumber);
            }

            currentRoundNumber = roundNumber;
            lastRoundDuration = finishTime;
            completedRoundDuration += finishTime;

            winner.roundWins++;
            winner.lastRoundFinishTime = finishTime;
            winner.totalWinningTime += finishTime;
            if (winner.bestRoundFinishTime <= 0f || finishTime < winner.bestRoundFinishTime)
                winner.bestRoundFinishTime = finishTime;

            foreach (RiderRaceStats rider in stats.Values)
            {
                if (rider.roundResults == null)
                    rider.roundResults = new List<RiderRoundStats>();

                bool isWinner = rider.riderId == winnerId;
                uint activityHash = GetSimulatedActivityHash(rider.riderId, roundNumber);
                int pickups = rider.isPlayer ? 0 : 1 + (activityHash % 5u == 0u ? 1 : 0);
                int takeovers = rider.isPlayer ? 0 : (activityHash % 3u == 0u ? 1 : 0);
                int triggerPoints = isWinner ? 1 : 0;
                float catchTime = rider.isPlayer
                    ? 0f
                    : isWinner
                        ? winnerCatchTime
                        : nonWinnerWeightTotal > 0f
                            ? nonWinnerCatchPool *
                              (GetSimulatedActivityWeight(rider.riderId, roundNumber) / nonWinnerWeightTotal)
                            : 0f;

                rider.pickupTimes += pickups;
                rider.carrierTakeovers += takeovers;
                rider.triggerPoints += triggerPoints;
                rider.totalCatchTime += catchTime;
                if (!rider.isPlayer)
                    rider.totalSpentTime += finishTime;
                rider.roundResults.Add(new RiderRoundStats
                {
                    roundNumber = roundNumber,
                    roundDuration = finishTime,
                    pickupTimes = pickups,
                    carrierTakeovers = takeovers,
                    triggerPoints = triggerPoints,
                    totalCatchTime = catchTime,
                    isWinner = isWinner,
                    finishTime = isWinner ? finishTime : 0f
                });
            }

            simulatedRoundCount++;
        }

        WinnerId = winnerId;
        UloqOwner = string.Empty;
        RaceDuration = completedRoundDuration;
        roundStarted = false;
        sessionStarted = false;
        return simulatedRoundCount;
    }

    private static float GetSimulatedFinishTime(int winnerId, int roundNumber, float roundTimeLimit)
    {
        uint hash = unchecked((uint)(winnerId * 397) ^ (uint)(roundNumber * 7919));
        float variation = (hash % 1000u) / 999f;
        float finishTime = Mathf.Lerp(roundTimeLimit * 0.55f, roundTimeLimit * 0.85f, variation);
        return Mathf.Clamp(finishTime, Mathf.Min(1f, roundTimeLimit), roundTimeLimit);
    }

    private static float GetSimulatedWinnerCatchTime(int winnerId, int roundNumber, float finishTime)
    {
        uint hash = unchecked((uint)(winnerId * 613) ^ (uint)(roundNumber * 3571));
        float variation = (hash % 1000u) / 999f;
        return finishTime * Mathf.Lerp(0.3f, 0.45f, variation);
    }

    private static uint GetSimulatedActivityHash(int riderId, int roundNumber)
    {
        return unchecked((uint)(riderId * 941) ^ (uint)(roundNumber * 5233));
    }

    private static float GetSimulatedActivityWeight(int riderId, int roundNumber)
    {
        uint hash = GetSimulatedActivityHash(riderId, roundNumber);
        return 0.75f + (hash % 501u) / 1000f;
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
        rider.roundXpPrize = 0;
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

    public RiderRaceStats GetPlayerStats()
    {
        return stats.Values.FirstOrDefault(rider => rider != null && rider.isPlayer);
    }

    public RiderRoundStats GetLatestPlayerRound()
    {
        RiderRaceStats player = GetPlayerStats();
        if (player == null || player.roundResults == null || player.roundResults.Count == 0)
            return null;

        return player.roundResults[player.roundResults.Count - 1];
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

    public void AwardRoundPrize(int riderId, int coinAmount, int nyufiyAmount, int xpAmount)
    {
        RiderRaceStats rider = Get(riderId);
        if (rider == null)
            return;

        int coins = Mathf.Max(0, coinAmount);
        int nyufiy = Mathf.Max(0, nyufiyAmount);
        int xp = Mathf.Max(0, xpAmount);
        rider.coinPrize += coins;
        rider.nyufiyPrize += nyufiy;
        rider.xpPrize += xp;
        rider.roundCoinPrize += coins;
        rider.roundNyufiyPrize += nyufiy;
        rider.roundXpPrize += xp;

        RiderRoundStats snapshot = GetCurrentRoundSnapshot(rider);
        if (snapshot != null)
        {
            snapshot.coinPrize += coins;
            snapshot.nyufiyPrize += nyufiy;
            snapshot.xpPrize += xp;
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
        completedRoundDuration += finishTime;
        CloseAllActiveHolds();
        SaveRoundSnapshots(riderId, finishTime);

        roundStarted = false;
        RaceDuration = completedRoundDuration;
    }

    private void SaveRoundSnapshots(int winnerId, float finishTime)
    {
        float roundDuration = Mathf.Max(0f, lastRoundDuration);
        foreach (RiderRaceStats rider in stats.Values)
        {
            if (rider.roundResults == null)
                rider.roundResults = new List<RiderRoundStats>();

            rider.totalSpentTime += roundDuration;
            rider.roundResults.Add(new RiderRoundStats
            {
                roundNumber = currentRoundNumber,
                roundDuration = roundDuration,
                pickupTimes = rider.roundPickupTimes,
                carrierTakeovers = rider.roundCarrierTakeovers,
                triggerPoints = rider.roundTriggerPoints,
                totalCatchTime = rider.roundCatchTime,
                isWinner = rider.riderId == winnerId,
                finishTime = rider.riderId == winnerId ? finishTime : 0f,
                coinPrize = rider.roundCoinPrize,
                nyufiyPrize = rider.roundNyufiyPrize,
                xpPrize = rider.roundXpPrize,
                comboPrize = rider.roundComboPrize
            });
        }
    }

    public List<RiderRaceStats> BuildLeaderboard()
    {
        return stats.Values
            .OrderByDescending(rider => rider.roundWins)
            .ThenByDescending(rider => rider.pickupTimes)
            .ThenBy(rider => rider.riderId)
            .ToList();
    }

    public KopkariMatchRewardSummary GetOrGrantPlayerMatchReward()
    {
        if (playerRewardSummary != null)
        {
            if (!playerRewardGranted)
            {
                RiderRaceStats cachedPlayer = stats.Values.FirstOrDefault(rider => rider != null && rider.isPlayer);
                GrantPlayerMatchReward(cachedPlayer, playerRewardSummary);
            }
            return playerRewardSummary;
        }

        List<RiderRaceStats> leaderboard = BuildLeaderboard();
        int playerIndex = leaderboard.FindIndex(rider => rider != null && rider.isPlayer);
        if (playerIndex < 0)
            return null;

        RiderRaceStats player = leaderboard[playerIndex];
        int rank = playerIndex + 1;
        int pickupBonus = GetPickupBonus(player.pickupTimes);
        playerRewardSummary = new KopkariMatchRewardSummary
        {
            playerRank = rank,
            rankNyufiy = GetNyufiyByRank(rank),
            roundNyufiy = player.nyufiyPrize,
            comboNyufiy = player.comboPrize,
            pickupBonus = pickupBonus,
            rankCoin = GetCoinByRank(rank),
            roundCoin = player.coinPrize,
            rankXp = GetXpByRank(rank),
            roundXp = player.xpPrize
        };
        playerRewardSummary.totalNyufiy = playerRewardSummary.rankNyufiy +
                                          playerRewardSummary.roundNyufiy +
                                          playerRewardSummary.comboNyufiy +
                                          playerRewardSummary.pickupBonus;
        playerRewardSummary.totalCoin = playerRewardSummary.rankCoin + playerRewardSummary.roundCoin;
        playerRewardSummary.xp = playerRewardSummary.rankXp + playerRewardSummary.roundXp;

        GrantPlayerMatchReward(player, playerRewardSummary);
        return playerRewardSummary;
    }

    private void GrantPlayerMatchReward(RiderRaceStats player, KopkariMatchRewardSummary reward)
    {
        if (playerRewardGranted || player == null || reward == null)
            return;

        if (CurrencyManager.Instance == null || DataManager.Instance == null)
        {
            Debug.LogWarning("[KopkariResultsManager] Reward services are not ready.");
            return;
        }

        CurrencyManager.Instance.AddNyufiy(reward.totalNyufiy, true);
        CurrencyManager.Instance.AddCoin(reward.totalCoin, true);
        DataManager.Instance.AddLevelPoint(reward.xp, true);

        float savedPossession = DataManager.Instance.GetBestRecord(Constants.MapNames.Registan);
        if (player.totalCatchTime > savedPossession)
            DataManager.Instance.SaveBestRecord(Constants.MapNames.Registan, player.totalCatchTime);

        DataManager.Instance.SaveRaceResult(
            Constants.MapNames.Registan,
            reward.playerRank == 1,
            Mathf.RoundToInt(RaceDuration));
        playerRewardGranted = true;
    }

    public HorseConditionStats GetOrApplyHorseCondition()
    {
        if (horseConditionApplied)
            return finalHorseCondition;

        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        RiderRaceStats player = stats.Values.FirstOrDefault(rider => rider != null && rider.isPlayer);
        float possession = player != null ? player.totalCatchTime : 0f;

        finalHorseCondition = new HorseConditionStats(
            Mathf.Max(0f, Mathf.Round(current.Power - RaceDuration * 0.2f)),
            Mathf.Max(0f, Mathf.Round(current.Cooling - RaceDuration * 0.05f - possession * 0.4f)),
            Mathf.Max(0f, Mathf.Round(current.Stamina - RaceDuration * 0.3f)));
        HorseConditionStatsService.SaveCurrent(finalHorseCondition);
        horseConditionApplied = true;
        return finalHorseCondition;
    }

    private static int GetNyufiyByRank(int rank)
    {
        switch (rank)
        {
            case 1: return 500;
            case 2: return 400;
            case 3: return 300;
            default: return 200;
        }
    }

    private static int GetCoinByRank(int rank)
    {
        switch (rank)
        {
            case 1: return 2;
            case 2: return 1;
            default: return 0;
        }
    }

    private static int GetXpByRank(int rank)
    {
        switch (rank)
        {
            case 1: return 7;
            case 2: return 5;
            case 3: return 3;
            default: return 3;
        }
    }

    private static int GetPickupBonus(int pickupTimes)
    {
        return Mathf.Max(0, pickupTimes) * 100;
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
                $"Coin:{rider.coinPrize} Nyufiy:{rider.nyufiyPrize} " +
                $"XP:{rider.xpPrize + GetXpByRank(i + 1)} " +
                $"(Round:{rider.xpPrize} Rank:{GetXpByRank(i + 1)}) Combo:{rider.comboPrize}");
        }

        Debug.Log(text.ToString());
        Debug.Log($"Overall duration: {RaceDuration:F2}s | Last round: {lastRoundDuration:F2}s");
    }
}
