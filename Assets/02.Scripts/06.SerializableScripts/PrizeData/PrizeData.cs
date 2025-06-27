using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrizeData
{
    [Header("Victory Prizes")]
    public List<Prize> winPrizes = new ();

    [Header("Lose Prizes")]
    public List<Prize> losePrizes = new ();

    [Header("Messages and Time")]
    public int loseMessageId;
    public int prizeLast = 0;
    public float roundTime = 150f;
}
