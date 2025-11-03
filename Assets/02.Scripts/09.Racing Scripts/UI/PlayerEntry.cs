using System;

[Serializable]
public class PlayerEntry
{
    public string PlayerName;
    public string HorseName;
    public int Ranking;      // kichik — yuqori o‘rin
    public string Team;
    public int HorsePower;   // 0..100

    public PlayerEntry(string playerName, string horseName, int ranking, string team, int horsePower)
    {
        PlayerName = playerName;
        HorseName = horseName;
        Ranking = ranking;
        Team = team;
        HorsePower = horsePower;
    }
}
