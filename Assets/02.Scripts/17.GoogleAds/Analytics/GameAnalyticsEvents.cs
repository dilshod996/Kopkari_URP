using UnityEngine;
using Firebase.Analytics;

public static class GameAnalyticsEvents
{
    public static void RewardedAdClicked(string placement, string rewardType, int rewardAmount)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("rewarded_ad_clicked",
            new Parameter("placement", placement),
            new Parameter("reward_type", rewardType),
            new Parameter("reward_amount", rewardAmount)
        );

        Debug.Log("Analytics: rewarded_ad_clicked - " + placement);
    }

    public static void RewardedAdCompleted(string placement, string rewardType, int rewardAmount)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("rewarded_ad_completed",
            new Parameter("placement", placement),
            new Parameter("reward_type", rewardType),
            new Parameter("reward_amount", rewardAmount)
        );

        Debug.Log("Analytics: rewarded_ad_completed - " + placement);
    }

    public static void RewardedAdFailed(string placement)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("rewarded_ad_failed",
            new Parameter("placement", placement)
        );

        Debug.Log("Analytics: rewarded_ad_failed - " + placement);
    }

    public static void RaceStarted(string mapName, string modeName)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("race_start",
            new Parameter("map_name", mapName),
            new Parameter("mode_name", modeName)
        );

        Debug.Log("Analytics: race_start - " + mapName);
    }

    public static void RaceFinished(string mapName, string modeName, int rank, int earnedCoins)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("race_finish",
            new Parameter("map_name", mapName),
            new Parameter("mode_name", modeName),
            new Parameter("rank", rank),
            new Parameter("earned_coins", earnedCoins)
        );

        Debug.Log("Analytics: race_finish - " + mapName);
    }

    public static void CoinRewardClaimed(string source, int amount)
    {
        if (!IsFirebaseReady()) return;

        FirebaseAnalytics.LogEvent("coin_reward_claimed",
            new Parameter("source", source),
            new Parameter("amount", amount)
        );

        Debug.Log("Analytics: coin_reward_claimed - " + source);
    }

    private static bool IsFirebaseReady()
    {
        return FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady;
    }
}