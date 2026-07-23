using System;
using System.Collections.Generic;

[Serializable]
public class RiderRoundStats
{
    public int roundNumber;
    public float roundDuration;
    public int pickupTimes;
    public int carrierTakeovers;
    public int triggerPoints;
    public float totalCatchTime;
    public bool isWinner;
    public float finishTime;
    public int coinPrize;
    public int nyufiyPrize;
    public int xpPrize;
    public int comboPrize;
}

[Serializable]
public class RiderRaceStats
{
    public int riderId;
    public string playerName;
    public string teamName;
    public bool isPlayer;

    // Whole-match totals.
    public int pickupTimes;
    public int carrierTakeovers;
    public int triggerPoints;
    public int roundWins;
    public float totalSpentTime;
    public float totalCatchTime;
    public float lastRoundFinishTime;
    public float bestRoundFinishTime;
    public float totalWinningTime;
    public int coinPrize;
    public int nyufiyPrize;
    public int xpPrize;
    public int comboPrize;
    public int comboWins;
    public List<RiderRoundStats> roundResults = new List<RiderRoundStats>();

    // Current-round runtime values.
    public bool isHolding;
    public float holdStartTime;
    public bool finishedWithLamb;
    public int roundPickupTimes;
    public int roundCarrierTakeovers;
    public int roundTriggerPoints;
    public float roundCatchTime;
    public int roundCoinPrize;
    public int roundNyufiyPrize;
    public int roundXpPrize;
    public int roundComboPrize;
}

[Serializable]
public class KopkariMatchRewardSummary
{
    public int playerRank;
    public int rankNyufiy;
    public int roundNyufiy;
    public int comboNyufiy;
    public int pickupBonus;
    public int totalNyufiy;
    public int rankCoin;
    public int roundCoin;
    public int totalCoin;
    public int rankXp;
    public int roundXp;
    public int xp;
}
