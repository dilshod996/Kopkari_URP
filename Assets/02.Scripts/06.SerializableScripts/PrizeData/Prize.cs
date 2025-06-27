using UnityEngine;

[System.Serializable]
public class Prize 
{
    public PrizeType prizeType; // Enum to define the type of prize
    public int prizeTextId;
    public Sprite prizeSprite;
    public float rewardAmount;

}
