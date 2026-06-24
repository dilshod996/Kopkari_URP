using System;

[Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string username;
    public int country;
    public string league;
    public string divisionId;
    public int weeklyWinCount;
    public int weeklyRaceCount;
    public int bestTimeMs;
    public int rank;
    public int rewardAmount;
    public bool rewardClaimed;
    public bool isDummy;
}

[Serializable]
public class LeaderboardState
{
    public string mapName;
    public string currentLeague;
    public string currentDivisionId;
    public string lastWeekId;
    public int lastRank;
    public int lastRewardAmount;
    public bool lastRewardClaimed;
}
