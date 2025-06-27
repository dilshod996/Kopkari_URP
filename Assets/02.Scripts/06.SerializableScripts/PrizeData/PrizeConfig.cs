using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Prize", menuName = "Ko'pkari/Prizes", order = 1)]
public class PrizeConfig : ScriptableObject
{
    public List<PrizeData> prizes;
}
