using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundPrizeRecord
{
    public float spentTime;                     // Bu raund qancha vaqt davom etdi
    public int lambCatchCount;                  // Shu raundda necha marta uloq olindi
    public List<Prize> wonPrizes = new(); // Yutgan sovrinlar
}

