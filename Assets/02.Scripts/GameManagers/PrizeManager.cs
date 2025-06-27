using System.Collections.Generic;
using UnityEngine;

public class PrizeManager : MonoBehaviour
{
    [SerializeField] public PrizeConfig prizeConfig; //change it
    private int currentPrizeIndex = 0;

    public List<RoundPrizeRecord> allRoundPrizes = new();
    public PrizeData GetCurrentPrize()
    {
        if (currentPrizeIndex < prizeConfig.prizes.Count)
            return prizeConfig.prizes[currentPrizeIndex];
        return null;
    }

    public PrizeData GetPreviousPrize()
    {
        if (currentPrizeIndex - 1 >= 0 && currentPrizeIndex - 1 < prizeConfig.prizes.Count)
            return prizeConfig.prizes[currentPrizeIndex - 1];
        return null;
    }

    public bool HasMorePrizes()
    {
        return currentPrizeIndex < prizeConfig.prizes.Count;
    }

    public void MoveToNextPrize()
    {
        currentPrizeIndex++; // Har doim oshsin

        if (currentPrizeIndex > prizeConfig.prizes.Count)
            currentPrizeIndex = prizeConfig.prizes.Count;
    }



    public int CurrentPrizeIndex => currentPrizeIndex;

    #region All prizes
    public void SaveRoundPrize(List<Prize> prizes, float spentTime, int lambCatchCount)
    {
        RoundPrizeRecord record = new RoundPrizeRecord
        {
            spentTime = spentTime,
            lambCatchCount = lambCatchCount,
            wonPrizes = new List<Prize>(prizes)
        };

        allRoundPrizes.Add(record);
    }

    public List<RoundPrizeRecord> GetAllPrizeHistory()
    {
        return allRoundPrizes;
    }

    public void ClearAllPrizes()
    {
        allRoundPrizes.Clear();
    }
    #endregion
}
