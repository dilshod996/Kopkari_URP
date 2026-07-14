using System;
using System.Collections.Generic;

[Serializable]
public class RiderRoundStats
{
    public int roundNumber;
    public int pickupTimes;
    public int carrierTakeovers;
    public int triggerPoints;
    public float totalCatchTime;
    public bool isWinner;
    public float finishTime;
    public int coinPrize;
    public int nyufiyPrize;
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
    public float totalCatchTime;
    public float lastRoundFinishTime;
    public float bestRoundFinishTime;
    public float totalWinningTime;
    public int coinPrize;
    public int nyufiyPrize;
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
    public int roundComboPrize;
}
