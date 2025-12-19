using System;
using UnityEngine;

[Serializable]
public class RiderRaceStats
{
    public int riderId;          // 0 = Player, 1.. = NPC
    public string playerName;
    public string teamName;

    public bool isPlayer;

    public int pickupTimes;
    public int triggerPoints;

    public float totalCatchTime; // pickup ¡æ drop umumiy vaqt

    // runtime
    public bool isHolding;
    public float holdStartTime;

    public bool finishedWithLamb; // winner flag
}
