using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Globalization;
using System.Threading.Tasks;
public partial class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    public static event Action<string> OnMapUnlocked;
    public static event Action OnPlayerDataLoaded;

    private FirebaseFirestore db;
    private string uid;
    private const string TutorialDoneField = "tutorialDone";
    private static readonly string[] AllMapKeys =
    {
        Constants.MapNames.RacingTraining,
        Constants.MapNames.Zarafshan,
        Constants.MapNames.Registan,
        Constants.MapNames.Egypt,
        Constants.MapNames.Japan,
        Constants.MapNames.PastDargom,
        Constants.MapNames.Chiroqchi,
        Constants.MapNames.Kansas
    };

    private static readonly string[] DefaultUnlockedMapKeys =
    {
        Constants.MapNames.RacingTraining,
        Constants.MapNames.Zarafshan
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            FirebaseManager.Instance != null &&
            !string.IsNullOrEmpty(FirebaseManager.Instance.UserId)
        );

        db = FirebaseFirestore.DefaultInstance;
        uid = FirebaseManager.Instance.UserId;

        CreatePlayerDataIfNeeded();
        LoadLocalData();
    }

    private void CreatePlayerDataIfNeeded()
    {
        DocumentReference userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to check player data: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                Debug.Log("Player data already exists: " + uid);
                LoadPlayerDataFromSnapshot(snapshot);
                return;
            }

            string localUsername = PlayerPrefs.GetString(Constants.Player.UsernameKey, "");
            int localCountry = PlayerPrefs.GetInt(Constants.Player.CountryName, 0);

            Dictionary<string, object> newUser = new Dictionary<string, object>
            {
                { Constants.Player.UID, uid },
                { Constants.Player.UsernameKey, localUsername },
                { Constants.Player.CountryName, localCountry },
                { Constants.Coins.Nyufiy, 3000 },
                { Constants.Coins.Coin, 15 },
                { Constants.Level.LevelAmount, 1 },
                { Constants.Level.XP, 0 },
                { Constants.RacingData.TotalRaces, 0 },
                { Constants.RacingData.TotalWins, 0 },
                { Constants.PlayerItems.Defense, 3 },
                { Constants.PlayerItems.SlowDown, 3 },
                { Constants.PlayerItems.WebSnare, 3 },
                { Constants.PlayerItems.Whip, 0 },
                { Constants.PlayerItems.Horsedust, 0 },
                { Constants.PlayerItems.FakeUlak, 3 },
                { TutorialDoneField, PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay) ? 1 : 0 },
                { Constants.MapNames.RacingTraining, 1 },
                { Constants.MapNames.Zarafshan, 1 },
                { Constants.MapNames.Registan, 0 },
                { Constants.MapNames.Egypt, 0 },
                { Constants.MapNames.Japan, 0 },
                { Constants.MapNames.PastDargom, 0 },
                { Constants.MapNames.Chiroqchi, 0 },
                { Constants.MapNames.Kansas, 0 },
                { Constants.Others.CreatedAt, FieldValue.ServerTimestamp },
                { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
            };

            userRef.SetAsync(newUser).ContinueWithOnMainThread(setTask =>
            {
                if (setTask.IsFaulted || setTask.IsCanceled)
                {
                    Debug.LogError("Failed to create player data: " + setTask.Exception);
                    return;
                }

                Debug.Log("New player data created: " + uid);
                ApplyDefaultPlayerDataToLocal();
                OnPlayerDataLoaded?.Invoke();
            });
        });
    }
    private void LoadLocalData()
    {
        LevelAmount = PlayerPrefs.GetInt(Constants.Level.LevelAmount, 1);
        XP = PlayerPrefs.GetInt(Constants.Level.XP, 0);
        LevelUpPending = PlayerPrefs.GetInt(Constants.Level.LevelUpPending, 0);

        TotalRaces = PlayerPrefs.GetInt(Constants.RacingData.TotalRaces, 0);
        TotalWins = PlayerPrefs.GetInt(Constants.RacingData.TotalWins, 0);
        Defense = PlayerPrefs.GetInt(Constants.PlayerItems.Defense, 3);
        SlowDown = PlayerPrefs.GetInt(Constants.PlayerItems.SlowDown, 3);
        WebSnare = PlayerPrefs.GetInt(Constants.PlayerItems.WebSnare, 3);
        Whip = PlayerPrefs.GetInt(Constants.PlayerItems.Whip, 0);
        Horsedust = PlayerPrefs.GetInt(Constants.PlayerItems.Horsedust, 0);
        FakeUlak = PlayerPrefs.GetInt(Constants.PlayerItems.FakeUlak, 3);
        LoadMapUnlocksFromLocal();
    }
    private void ApplyDefaultPlayerDataToLocal()
    {
        int nyufiy = 3000;
        int coin = 15;

        LevelAmount = 1;
        XP = 0;
        LevelUpPending = 0;

        TotalRaces = 0;
        TotalWins = 0;

        Defense = 3;
        SlowDown = 3;
        WebSnare = 3;
        Whip = 0;
        Horsedust = 0;
        FakeUlak = 3;

        PlayerPrefs.SetInt(Constants.Level.LevelAmount, LevelAmount);
        PlayerPrefs.SetInt(Constants.Level.XP, XP);
        PlayerPrefs.SetInt(Constants.Level.LevelUpPending, LevelUpPending);

        PlayerPrefs.SetInt(Constants.RacingData.TotalRaces, TotalRaces);
        PlayerPrefs.SetInt(Constants.RacingData.TotalWins, TotalWins);

        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiy);
        PlayerPrefs.SetInt(Constants.Coins.Coin, coin);
        PlayerPrefs.SetInt(Constants.PlayerItems.Defense, Defense);
        PlayerPrefs.SetInt(Constants.PlayerItems.SlowDown, SlowDown);
        PlayerPrefs.SetInt(Constants.PlayerItems.WebSnare, WebSnare);
        PlayerPrefs.SetInt(Constants.PlayerItems.Whip, Whip);
        PlayerPrefs.SetInt(Constants.PlayerItems.Horsedust, Horsedust);
        PlayerPrefs.SetInt(Constants.PlayerItems.FakeUlak, FakeUlak);
        ApplyDefaultMapUnlocksToLocal();

        PlayerPrefs.Save();

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.SetCurrencyFromServer(nyufiy, coin);
    }
    #region LEVEL UP
    public int LevelAmount { get; private set; }
    public int XP { get; private set; }
    public int LevelUpPending { get; private set; }

    public void AddLevelPoint(int earnedXp, bool syncNow = false)
    {
        if (earnedXp <= 0)
            return;

        LevelAmount += 0; // faqat field borligini ko¡®rsatish uchun
        XP += earnedXp;

        while (XP >= 100)
        {
            XP -= 100;
            LevelAmount++;
            LevelUpPending++;
        }

        PlayerPrefs.SetInt(Constants.Level.LevelAmount, LevelAmount);
        PlayerPrefs.SetInt(Constants.Level.XP, XP);
        PlayerPrefs.SetInt(Constants.Level.LevelUpPending, LevelUpPending);
        PlayerPrefs.Save();

        if (syncNow)
            SyncLevelToFirestore();
    }
    public void ConsumeLevelUpPending()
    {
        if (LevelUpPending <= 0)
            return;

        LevelUpPending--;

        PlayerPrefs.SetInt(Constants.Level.LevelUpPending, LevelUpPending);
        PlayerPrefs.Save();

        SyncLevelToFirestore();
    }
    private void SyncLevelToFirestore()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> updateData = new Dictionary<string, object>
    {
        { Constants.Level.LevelAmount, LevelAmount },
        { Constants.Level.XP, XP },
        { Constants.Level.LevelUpPending, LevelUpPending },
        { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
    };

        db.Collection("users").Document(uid).SetAsync(updateData, SetOptions.MergeAll);
    }
    #endregion

    #region RACE DATA
    public int TotalRaces { get; private set; }
    public int TotalWins { get; private set; }
    public void SaveRaceResult(string mapName, bool isWin, int playTimeSeconds)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        TotalRaces++;

        if (isWin)
            TotalWins++;

        PlayerPrefs.SetInt(Constants.RacingData.TotalRaces, TotalRaces);
        PlayerPrefs.SetInt(Constants.RacingData.TotalWins, TotalWins);
        PlayerPrefs.Save();

        SyncRaceResultToFirestore(mapName, isWin, playTimeSeconds);
        SubmitWeeklyLeaderboardResult(mapName, isWin, playTimeSeconds)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("Weekly leaderboard submit failed: " + task.Exception);
            });
    }
    private void SyncRaceResultToFirestore(string mapName, bool isWin, int playTimeSeconds)
    {
        if (db == null || string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("Race result sync failed: Firebase not ready");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);
        DocumentReference mapRef = userRef.Collection("map_stats").Document(mapName);

        WriteBatch batch = db.StartBatch();

        Dictionary<string, object> userUpdate = new Dictionary<string, object>
    {
        { Constants.RacingData.TotalRaces, TotalRaces },
        { Constants.RacingData.TotalWins, TotalWins },
        { "last_played_map", mapName },
        { "last_race_is_win", isWin },
        { "updated_at", FieldValue.ServerTimestamp }
    };

        Dictionary<string, object> mapUpdate = new Dictionary<string, object>
    {
        { "map_name", mapName },
        { "play_count", FieldValue.Increment(1) },
        { "win_count", FieldValue.Increment(isWin ? 1 : 0) },
        { "loss_count", FieldValue.Increment(isWin ? 0 : 1) },
        { "total_play_time_seconds", FieldValue.Increment(playTimeSeconds) },
        { "last_play_time_seconds", playTimeSeconds },
        { "last_result", isWin ? "win" : "lose" },
        { "updated_at", FieldValue.ServerTimestamp }
    };

        batch.Set(userRef, userUpdate, SetOptions.MergeAll);
        batch.Set(mapRef, mapUpdate, SetOptions.MergeAll);

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Race result sync failed: " + task.Exception);
                return;
            }

            Debug.Log("Race result synced: " + mapName);
        });
    }
    #endregion

    #region BEST RECORDS
    private const string BestRecordPrefix = "best_record_";

    private string GetBestRecordPrefsKey(string mapName)
    {
        return BestRecordPrefix + NormalizeLeaderboardMapName(mapName);
    }

    public float GetBestRecord(string mapName)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        string key = GetBestRecordPrefsKey(mapName);
        float savedTime = PlayerPrefs.GetFloat(key, 0);

        if (savedTime == 0 && !IsKnownMap(mapName))
            savedTime = PlayerPrefs.GetFloat(mapName, 0);

        return savedTime;
    }

    public void SaveBestRecord(string mapName, float newTime)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        string key = GetBestRecordPrefsKey(mapName);

        float savedTime = GetBestRecord(mapName);

        if (savedTime != 0 && savedTime <= newTime)
            return;

        PlayerPrefs.SetFloat(key, newTime);
        PlayerPrefs.Save();

        SyncBestRecordToFirestore(mapName, newTime);
    }
    private void SyncBestRecordToFirestore(string mapName, float bestTime)
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        DocumentReference recordRef = db
            .Collection("users")
            .Document(uid)
            .Collection("records")
            .Document(mapName);

        Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "map_name", mapName },
        { "best_time", bestTime },
        { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
    };

        recordRef.SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Best record sync failed: " + task.Exception);
                    return;
                }

                Debug.Log("Best record synced: " + mapName);
            });
    }
    #endregion

    #region WEEKLY LEADERBOARD
    private const string LeagueBronze = "bronze";
    private const string LeagueSilver = "silver";
    private const string LeagueGold = "gold";
    private const string LeaguePlatinum = "platinum";
    private const string LeagueDiamond = "diamond";
    private const string DefaultDivisionId = "001";
    private const int LeaderboardDivisionLimit = 20;

    public string GetCurrentWeekId()
    {
        DateTime utcNow = DateTime.UtcNow;
        Calendar calendar = CultureInfo.InvariantCulture.Calendar;
        int week = calendar.GetWeekOfYear(utcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        int year = utcNow.Year;

        if (utcNow.Month == 1 && week >= 52)
            year--;
        else if (utcNow.Month == 12 && week == 1)
            year++;

        return $"{year}-W{week:00}";
    }

    public string GetNextLeague(string league)
    {
        switch (league)
        {
            case LeagueBronze:
                return LeagueSilver;
            case LeagueSilver:
                return LeagueGold;
            case LeagueGold:
                return LeaguePlatinum;
            case LeaguePlatinum:
                return LeagueDiamond;
            case LeagueDiamond:
                return LeagueDiamond;
            default:
                return LeagueBronze;
        }
    }

    public string GetPreviousLeague(string league)
    {
        switch (league)
        {
            case LeagueDiamond:
                return LeaguePlatinum;
            case LeaguePlatinum:
                return LeagueGold;
            case LeagueGold:
                return LeagueSilver;
            case LeagueSilver:
                return LeagueBronze;
            case LeagueBronze:
                return LeagueBronze;
            default:
                return LeagueBronze;
        }
    }

    private string NormalizeLeaderboardMapName(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            return "";

        switch (mapName)
        {
            case "SecondRacing":
            case "Zarafshan":
            case "zarafshan":
                return Constants.MapNames.Zarafshan;
            case "EgyptRacing":
            case "Egypt":
            case "egypt":
                return Constants.MapNames.Egypt;
            case "Kansas":
            case "kansas":
                return Constants.MapNames.Kansas;
            case "TrainingRacing":
            case "FirstRacing":
            case "Training":
                return Constants.MapNames.RacingTraining;
            case "Registan":
            case "registan":
                return Constants.MapNames.Registan;
            case "PastDargom":
            case "pastdargom":
                return Constants.MapNames.PastDargom;
            case "Chiroqchi":
            case "chiroqchi":
                return Constants.MapNames.Chiroqchi;
            default:
                return mapName;
        }
    }

    private List<string> GetLeaderboardReadMapNames(string mapName)
    {
        List<string> mapNames = new List<string>();
        AddUniqueMapName(mapNames, NormalizeLeaderboardMapName(mapName));
        return mapNames;
    }
    private void AddUniqueMapName(List<string> mapNames, string mapName)
    {
        if (!string.IsNullOrEmpty(mapName) && !mapNames.Contains(mapName))
            mapNames.Add(mapName);
    }
    private string NormalizeLeaderboardLeague(string league)
    {
        if (string.IsNullOrEmpty(league))
            return LeagueBronze;

        switch (league.Trim().ToLowerInvariant())
        {
            case LeagueBronze:
                return LeagueBronze;
            case LeagueSilver:
                return LeagueSilver;
            case LeagueGold:
                return LeagueGold;
            case LeaguePlatinum:
                return LeaguePlatinum;
            case LeagueDiamond:
                return LeagueDiamond;
            default:
                return LeagueBronze;
        }
    }

    private string NormalizeLeaderboardDivisionId(string divisionId)
    {
        return string.IsNullOrEmpty(divisionId) ? DefaultDivisionId : divisionId.Trim();
    }
    public async Task SubmitWeeklyLeaderboardResult(string mapName, bool isWin, float raceTimeSeconds)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        if (db == null || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(mapName))
            return;

        string weekId = GetCurrentWeekId();
        string currentUid = uid;
        string username = PlayerPrefs.GetString(Constants.Player.UsernameKey, "");
        int country = PlayerPrefs.GetInt(Constants.Player.CountryName, 0);

        DocumentReference stateRef = db.Collection("users")
            .Document(currentUid)
            .Collection("leaderboard_state")
            .Document(mapName);

        DocumentSnapshot stateSnapshot = await stateRef.GetSnapshotAsync();
        string league = LeagueBronze;
        string divisionId = DefaultDivisionId;

        if (stateSnapshot.Exists)
        {
            league = GetString(stateSnapshot, "current_league", LeagueBronze);
            divisionId = GetString(stateSnapshot, "current_division_id", DefaultDivisionId);
        }

        league = NormalizeLeaderboardLeague(league);
        divisionId = NormalizeLeaderboardDivisionId(divisionId);

        // TODO Cloud Function: assign players to divisions and enforce the max 20 players per division.
        // The client defaults new players to bronze/001, but server code should be the final authority.
        Dictionary<string, object> stateData = new Dictionary<string, object>
        {
            { "map_name", mapName },
            { "current_league", league },
            { "current_division_id", divisionId },
            { "last_week_id", weekId },
            { "updated_at", FieldValue.ServerTimestamp }
        };

        if (!stateSnapshot.Exists)
        {
            stateData["last_rank"] = 0;
            stateData["last_reward_amount"] = 0;
            stateData["last_reward_claimed"] = false;
        }

        await stateRef.SetAsync(stateData, SetOptions.MergeAll);

        DocumentReference scoreRef = GetWeeklyScoreReference(mapName, weekId, league, divisionId, currentUid);
        DocumentSnapshot scoreSnapshot = await scoreRef.GetSnapshotAsync();
        int raceTimeMs = Mathf.Max(0, Mathf.RoundToInt(raceTimeSeconds * 1000f));
        int savedBestTimeMs = scoreSnapshot.Exists ? GetInt(scoreSnapshot, "best_time_ms", 0) : 0;
        bool shouldUpdateBestTime = savedBestTimeMs <= 0 || raceTimeMs < savedBestTimeMs;

        Dictionary<string, object> scoreData = new Dictionary<string, object>
        {
            { "uid", currentUid },
            { "username", username },
            { "country", country },
            { "map_name", mapName },
            { "week_id", weekId },
            { "league", league },
            { "division_id", divisionId },
            { "weekly_race_count", FieldValue.Increment(1) },
            { "weekly_win_count", FieldValue.Increment(isWin ? 1 : 0) },
            { "last_play_time_seconds", raceTimeSeconds },
            { "last_result", isWin ? "win" : "lose" },
            { "updated_at", FieldValue.ServerTimestamp }
        };

        if (!scoreSnapshot.Exists)
        {
            scoreData["rank"] = 0;
            scoreData["reward_amount"] = 0;
            scoreData["reward_claimed"] = false;
            scoreData["created_at"] = FieldValue.ServerTimestamp;
        }

        if (shouldUpdateBestTime)
            scoreData["best_time_ms"] = raceTimeMs;

        await scoreRef.SetAsync(scoreData, SetOptions.MergeAll);

        // TODO Cloud Function: weekly settlement should calculate ranks from weekly_win_count desc,
        // best_time_ms asc, weekly_race_count asc, then write rank/reward_amount.
        // TODO Cloud Function: promote ranks 1-3, demote ranks 18-20, and keep league bounds server-side.
    }

    public async Task<List<LeaderboardEntry>> LoadWeeklyLeaderboard(string mapName, string league, string divisionId, int limit = 20)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        league = NormalizeLeaderboardLeague(league);
        divisionId = NormalizeLeaderboardDivisionId(divisionId);
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        if (db == null || string.IsNullOrEmpty(mapName))
            return entries;

        string weekId = GetCurrentWeekId();
        int queryLimit = Mathf.Clamp(limit, 1, LeaderboardDivisionLimit);

        List<string> mapReadNames = GetLeaderboardReadMapNames(mapName);

        foreach (string readMapName in mapReadNames)
        {
            CollectionReference scoresRef = db.Collection("leaderboards")
                .Document(readMapName)
                .Collection("weeks")
                .Document(weekId)
                .Collection("leagues")
                .Document(league)
                .Collection("divisions")
                .Document(divisionId)
                .Collection("scores");

            QuerySnapshot snapshot = await scoresRef.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                LeaderboardEntry entry = new LeaderboardEntry
                {
                    uid = GetString(document, "uid", document.Id),
                    username = GetString(document, "username", ""),
                    country = GetInt(document, "country", 0),
                    league = GetString(document, "league", league),
                    divisionId = GetString(document, "division_id", divisionId),
                    weeklyWinCount = GetInt(document, "weekly_win_count", 0),
                    weeklyRaceCount = GetInt(document, "weekly_race_count", 0),
                    bestTimeMs = GetInt(document, "best_time_ms", 0),
                    rank = GetInt(document, "rank", 0),
                    rewardAmount = GetInt(document, "reward_amount", 0),
                    rewardClaimed = GetBool(document, "reward_claimed", false)
                };

                entries.Add(entry);
            }

            if (entries.Count > 0)
                break;
        }
        entries.Sort((left, right) =>
        {
            int result = right.weeklyWinCount.CompareTo(left.weeklyWinCount);
            if (result != 0)
                return result;

            int leftBestTime = left.bestTimeMs > 0 ? left.bestTimeMs : int.MaxValue;
            int rightBestTime = right.bestTimeMs > 0 ? right.bestTimeMs : int.MaxValue;
            result = leftBestTime.CompareTo(rightBestTime);
            if (result != 0)
                return result;

            result = left.weeklyRaceCount.CompareTo(right.weeklyRaceCount);
            if (result != 0)
                return result;

            return string.Compare(left.uid, right.uid, StringComparison.Ordinal);
        });

        int displayCount = Mathf.Min(queryLimit, entries.Count);
        List<LeaderboardEntry> limitedEntries = entries.GetRange(0, displayCount);

        for (int i = 0; i < limitedEntries.Count; i++)
        {
            if (limitedEntries[i].rank <= 0)
                limitedEntries[i].rank = i + 1;
        }

        // TODO Cloud Function: official rank calculation should be written to each score document.
        // The client-side sort above is for display only and must not be trusted for rewards.
        return limitedEntries;
    }

    public async Task<bool> ClaimWeeklyLeaderboardReward(string mapName)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        if (db == null || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(mapName))
            return false;

        DocumentReference stateRef = db.Collection("users")
            .Document(uid)
            .Collection("leaderboard_state")
            .Document(mapName);

        DocumentSnapshot stateSnapshot = await stateRef.GetSnapshotAsync();

        if (!stateSnapshot.Exists)
            return false;

        string weekId = GetString(stateSnapshot, "last_week_id", "");
        string league = NormalizeLeaderboardLeague(GetString(stateSnapshot, "current_league", LeagueBronze));
        string divisionId = NormalizeLeaderboardDivisionId(GetString(stateSnapshot, "current_division_id", DefaultDivisionId));
        int rewardAmount = GetInt(stateSnapshot, "last_reward_amount", 0);
        bool rewardClaimed = GetBool(stateSnapshot, "last_reward_claimed", false);

        if (string.IsNullOrEmpty(weekId) || rewardAmount <= 0 || rewardClaimed)
            return false;

        // TODO Cloud Function: replace this placeholder with a callable function that validates
        // the settled rank/reward, grants Nyufiy atomically, and sets claim flags server-side.
        // The Unity client must not grant leaderboard rewards directly.
        Dictionary<string, object> stateUpdate = new Dictionary<string, object>
        {
            { "last_reward_claimed", true },
            { "updated_at", FieldValue.ServerTimestamp }
        };

        Dictionary<string, object> scoreUpdate = new Dictionary<string, object>
        {
            { "reward_claimed", true },
            { "updated_at", FieldValue.ServerTimestamp }
        };

        await stateRef.SetAsync(stateUpdate, SetOptions.MergeAll);
        await GetWeeklyScoreReference(mapName, weekId, league, divisionId, uid).SetAsync(scoreUpdate, SetOptions.MergeAll);

        return true;
    }

    private DocumentReference GetWeeklyScoreReference(string mapName, string weekId, string league, string divisionId, string playerUid)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        league = NormalizeLeaderboardLeague(league);
        divisionId = NormalizeLeaderboardDivisionId(divisionId);
        return db.Collection("leaderboards")
            .Document(mapName)
            .Collection("weeks")
            .Document(weekId)
            .Collection("leagues")
            .Document(league)
            .Collection("divisions")
            .Document(divisionId)
            .Collection("scores")
            .Document(playerUid);
    }
    #endregion

    #region WEEKLY LEADERBOARD READ API
    public bool CanOpenLeaderboardForMap(string mapName)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        return !string.IsNullOrEmpty(mapName) && IsMapUnlocked(mapName);
    }

    public void LoadLeaderboardState(string mapName, Action<LeaderboardState> onLoaded, Action<string> onError = null)
    {
        mapName = NormalizeLeaderboardMapName(mapName);
        if (db == null || string.IsNullOrEmpty(uid))
        {
            onError?.Invoke("Leaderboard is not ready.");
            return;
        }

        if (string.IsNullOrEmpty(mapName))
        {
            onError?.Invoke("Map name is empty.");
            return;
        }

        DocumentReference stateRef = db.Collection("users")
            .Document(uid)
            .Collection("leaderboard_state")
            .Document(mapName);

        stateRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke("Could not load leaderboard state.");
                Debug.LogError("Leaderboard state load failed: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                onLoaded?.Invoke(CreateDefaultLeaderboardState(mapName));
                return;
            }

            LeaderboardState state = new LeaderboardState
            {
                mapName = GetString(snapshot, "map_name", mapName),
                currentLeague = GetString(snapshot, "current_league", LeagueBronze),
                currentDivisionId = GetString(snapshot, "current_division_id", DefaultDivisionId),
                lastWeekId = GetString(snapshot, "last_week_id", ""),
                lastRank = GetInt(snapshot, "last_rank", 0),
                lastRewardAmount = GetInt(snapshot, "last_reward_amount", 0),
                lastRewardClaimed = GetBool(snapshot, "last_reward_claimed", false)
            };

            state.currentLeague = NormalizeLeaderboardLeague(state.currentLeague);
            state.currentDivisionId = NormalizeLeaderboardDivisionId(state.currentDivisionId);

            onLoaded?.Invoke(state);
        });
    }

    public void LoadWeeklyLeaderboard(string mapName, string league, string divisionId, Action<List<LeaderboardEntry>> onLoaded, Action<string> onError = null, int limit = 20)
    {
        if (!CanOpenLeaderboardForMap(mapName))
        {
            onError?.Invoke("Unlock this map to join the leaderboard.");
            return;
        }

        LoadWeeklyLeaderboard(mapName, league, divisionId, limit).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke("Could not load leaderboard.");
                Debug.LogError("Weekly leaderboard load failed: " + task.Exception);
                return;
            }

            onLoaded?.Invoke(task.Result);
        });
    }

    private LeaderboardState CreateDefaultLeaderboardState(string mapName)
    {
        return new LeaderboardState
        {
            mapName = mapName,
            currentLeague = LeagueBronze,
            currentDivisionId = DefaultDivisionId,
            lastWeekId = GetCurrentWeekId(),
            lastRank = 0,
            lastRewardAmount = 0,
            lastRewardClaimed = false
        };
    }

    // TODO Cloud Function: weekly rank settlement, promotion, demotion, and reward creation must be server-authoritative.
    // TODO Cloud Function: Unity must only read settled rank/reward info and submit race results.
    #endregion
    #region RESTORE DATA
    private void LoadPlayerDataFromSnapshot(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> snapshotData = snapshot.ToDictionary();
        int nyufiy = GetInt(snapshot, Constants.Coins.Nyufiy, 3000);
        int coin = GetInt(snapshot, Constants.Coins.Coin, 15);
        string username = GetString(snapshot, Constants.Player.UsernameKey, PlayerPrefs.GetString(Constants.Player.UsernameKey, ""));
        int country = GetInt(snapshot, Constants.Player.CountryName, PlayerPrefs.GetInt(Constants.Player.CountryName, 0));

        LevelAmount = GetInt(snapshot, Constants.Level.LevelAmount, 1);
        XP = GetInt(snapshot, Constants.Level.XP, 0);
        LevelUpPending = GetInt(snapshot, Constants.Level.LevelUpPending, 0);

        TotalRaces = GetInt(snapshot, Constants.RacingData.TotalRaces, 0);
        TotalWins = GetInt(snapshot, Constants.RacingData.TotalWins, 0);

        Defense = GetInt(snapshot, Constants.PlayerItems.Defense, 3);
        SlowDown = GetInt(snapshot, Constants.PlayerItems.SlowDown, 3);
        WebSnare = GetInt(snapshot, Constants.PlayerItems.WebSnare, 3);
        Whip = GetInt(snapshot, Constants.PlayerItems.Whip, 0);
        Horsedust = GetInt(snapshot, Constants.PlayerItems.Horsedust, 0);
        FakeUlak = GetInt(
            snapshot,
            Constants.PlayerItems.FakeUlak,
            PlayerPrefs.GetInt(Constants.PlayerItems.FakeUlak, 3));

        PlayerPrefs.SetInt(Constants.Level.LevelAmount, LevelAmount);
        PlayerPrefs.SetInt(Constants.Level.XP, XP);
        PlayerPrefs.SetInt(Constants.Level.LevelUpPending, LevelUpPending);

        PlayerPrefs.SetInt(Constants.RacingData.TotalRaces, TotalRaces);
        PlayerPrefs.SetInt(Constants.RacingData.TotalWins, TotalWins);

        if (!string.IsNullOrEmpty(username))
            PlayerPrefs.SetString(Constants.Player.UsernameKey, username);

        PlayerPrefs.SetInt(Constants.Player.CountryName, country);
        PlayerPrefs.SetInt(Constants.Coins.Nyufiy, nyufiy);
        PlayerPrefs.SetInt(Constants.Coins.Coin, coin);
        PlayerPrefs.SetInt(Constants.PlayerItems.Defense, Defense);
        PlayerPrefs.SetInt(Constants.PlayerItems.SlowDown, SlowDown);
        PlayerPrefs.SetInt(Constants.PlayerItems.WebSnare, WebSnare);
        PlayerPrefs.SetInt(Constants.PlayerItems.Whip, Whip);
        PlayerPrefs.SetInt(Constants.PlayerItems.Horsedust, Horsedust);
        PlayerPrefs.SetInt(Constants.PlayerItems.FakeUlak, FakeUlak);
        LoadMapUnlocksFromSnapshot(snapshot);
        LoadTutorialStateFromSnapshot(snapshot);

        PlayerPrefs.Save();

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetCurrencyFromServer(nyufiy, coin);
        }

        if (!snapshotData.ContainsKey(Constants.Player.UsernameKey) || !snapshotData.ContainsKey(Constants.Player.CountryName))
            SyncPlayerProfileToFirestore(username, country);

        if (!snapshotData.ContainsKey(TutorialDoneField))
            BackfillTutorialDoneToFirestore();

        Debug.Log($"Player data loaded from Firestore: Level {LevelAmount}, XP {XP}, Nyufiy {nyufiy}, Coin {coin}");
        OnPlayerDataLoaded?.Invoke();
    }

    public void SavePlayerProfile(string username, int countryIndex, bool syncNow = true)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        username = username.Trim();

        PlayerPrefs.SetString(Constants.Player.UsernameKey, username);
        PlayerPrefs.SetInt(Constants.Player.CountryName, countryIndex);
        PlayerPrefs.Save();

        if (syncNow)
            SyncPlayerProfileToFirestore(username, countryIndex);
    }

    private void SyncPlayerProfileToFirestore(string username, int countryIndex)
    {
        if (db == null || string.IsNullOrEmpty(uid) || string.IsNullOrWhiteSpace(username))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { Constants.Player.UsernameKey, username.Trim() },
            { Constants.Player.CountryName, countryIndex },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Player profile sync failed: " + task.Exception);
                    return;
                }

                Debug.Log("Player profile synced to Firestore");
            });
    }

    private int GetInt(DocumentSnapshot snapshot, string fieldName, int defaultValue)
    {
        Dictionary<string, object> data = snapshot.ToDictionary();

        if (!data.ContainsKey(fieldName) || data[fieldName] == null)
            return defaultValue;

        try
        {
            return Convert.ToInt32(data[fieldName]);
        }
        catch
        {
            return defaultValue;
        }
    }
    private bool GetBool(DocumentSnapshot snapshot, string fieldName, bool defaultValue)
    {
        Dictionary<string, object> data = snapshot.ToDictionary();

        if (!data.ContainsKey(fieldName) || data[fieldName] == null)
            return defaultValue;

        try
        {
            return Convert.ToBoolean(data[fieldName]);
        }
        catch
        {
            return defaultValue;
        }
    }

    private string GetString(DocumentSnapshot snapshot, string fieldName, string defaultValue)
    {
        Dictionary<string, object> data = snapshot.ToDictionary();

        if (!data.ContainsKey(fieldName) || data[fieldName] == null)
            return defaultValue;

        return data[fieldName].ToString();
    }
    private void LoadTutorialStateFromSnapshot(DocumentSnapshot snapshot)
    {
        int tutorialDone = GetInt(snapshot, TutorialDoneField, 0);

        if (tutorialDone != 1)
            return;

        ApplyTutorialDoneToLocal();
    }

    private void ApplyTutorialDoneToLocal()
    {
        PlayerPrefs.SetInt(Constants.Tutorial.Settings, 1);
        PlayerPrefs.SetInt(Constants.Tutorial.Name, 1);
        PlayerPrefs.SetInt(Constants.Tutorial.OptionalTutorial, 1);
        PlayerPrefs.SetInt(Constants.Tutorial.TutorialPlay, 1);
        PlayerPrefs.SetInt(Constants.Tutorial.TutorialReward, 1);
    }

    public void SetTutorialDone(bool syncNow = true)
    {
        ApplyTutorialDoneToLocal();
        PlayerPrefs.Save();

        if (syncNow)
            SyncTutorialDoneToFirestore(1);
    }

    private void BackfillTutorialDoneToFirestore()
    {
        int localTutorialDone = PlayerPrefs.HasKey(Constants.Tutorial.TutorialPlay) ? 1 : 0;
        SyncTutorialDoneToFirestore(localTutorialDone);
    }

    private void SyncTutorialDoneToFirestore(int value)
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { TutorialDoneField, value == 1 ? 1 : 0 },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Tutorial state sync failed: " + task.Exception);
                    return;
                }

                Debug.Log($"Tutorial state synced: {TutorialDoneField} = {data[TutorialDoneField]}");
            });
    }
    #endregion

    #region MAP UNLOCK DATA
    public bool IsMapUnlocked(string mapKey)
    {
        if (string.IsNullOrEmpty(mapKey))
            return false;

        int defaultValue = IsDefaultUnlockedMap(mapKey) ? 1 : 0;
        return PlayerPrefs.GetInt(mapKey, defaultValue) == 1;
    }

    public void UnlockMap(string mapKey, bool syncNow = false)
    {
        if (string.IsNullOrEmpty(mapKey) || !IsKnownMap(mapKey))
            return;

        PlayerPrefs.SetInt(mapKey, 1);
        PlayerPrefs.Save();
        OnMapUnlocked?.Invoke(mapKey);

        if (syncNow)
            SyncMapUnlockToFirestore(mapKey);
    }

    private void LoadMapUnlocksFromLocal()
    {
        foreach (string mapKey in AllMapKeys)
        {
            if (IsDefaultUnlockedMap(mapKey) && PlayerPrefs.GetInt(mapKey, 0) == 0)
                PlayerPrefs.SetInt(mapKey, 1);
        }

        PlayerPrefs.Save();
    }

    private void ApplyDefaultMapUnlocksToLocal()
    {
        foreach (string mapKey in AllMapKeys)
        {
            int value = IsDefaultUnlockedMap(mapKey) ? 1 : 0;
            PlayerPrefs.SetInt(mapKey, value);

            OnMapUnlocked?.Invoke(mapKey);
        }
    }

    private void LoadMapUnlocksFromSnapshot(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> data = snapshot.ToDictionary();
        bool shouldBackfill = false;

        foreach (string mapKey in AllMapKeys)
        {
            int defaultValue = IsDefaultUnlockedMap(mapKey) ? 1 : 0;
            int localValue = PlayerPrefs.GetInt(mapKey, defaultValue) == 1 ? 1 : 0;
            bool hasLegacyBestRecord = PlayerPrefs.GetFloat(mapKey, 0f) > 0f;
            int value = defaultValue;

            if (data.ContainsKey(mapKey) && data[mapKey] != null)
            {
                try
                {
                    value = Convert.ToInt32(data[mapKey]);
                }
                catch
                {
                    value = defaultValue;
                }
            }
            else
            {
                shouldBackfill = true;
            }

            value = value == 1 ? 1 : 0;

            if ((localValue == 1 || hasLegacyBestRecord) && value == 0)
            {
                value = 1;
                shouldBackfill = true;
            }

            PlayerPrefs.SetInt(mapKey, value);

            OnMapUnlocked?.Invoke(mapKey);
        }

        if (shouldBackfill)
            SyncMapUnlocksToFirestore();
    }

    private void SyncMapUnlockToFirestore(string mapKey)
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { mapKey, 1 },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Map unlock sync failed: " + task.Exception);
                    return;
                }

                Debug.Log("Map unlock synced: " + mapKey);
            });
    }

    private void SyncMapUnlocksToFirestore()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        foreach (string mapKey in AllMapKeys)
        {
            int defaultValue = IsDefaultUnlockedMap(mapKey) ? 1 : 0;
            data[mapKey] = PlayerPrefs.GetInt(mapKey, defaultValue) == 1 ? 1 : 0;
        }

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Map unlocks sync failed: " + task.Exception);
                    return;
                }

                Debug.Log("Map unlocks synced to Firestore");
            });
    }

    private bool IsDefaultUnlockedMap(string mapKey)
    {
        foreach (string defaultMapKey in DefaultUnlockedMapKeys)
        {
            if (defaultMapKey == mapKey)
                return true;
        }

        return false;
    }

    private bool IsKnownMap(string mapKey)
    {
        foreach (string knownMapKey in AllMapKeys)
        {
            if (knownMapKey == mapKey)
                return true;
        }

        return false;
    }
    #endregion
    #region INVENTORY DATA
    public int Defense { get; private set; }
    public int SlowDown { get; private set; }
    public int WebSnare { get; private set; }
    public int Whip { get; private set; }
    public int Horsedust { get; private set; }
    public int FakeUlak { get; private set; }
    public int GetItemAmount(string itemKey)
    {
        if (itemKey == Constants.PlayerItems.Defense)
            return Defense;

        if (itemKey == Constants.PlayerItems.SlowDown)
            return SlowDown;

        if (itemKey == Constants.PlayerItems.WebSnare)
            return WebSnare;

        if (itemKey == Constants.PlayerItems.Whip)
            return Whip;

        if (itemKey == Constants.PlayerItems.Horsedust)
            return Horsedust;

        if (itemKey == Constants.PlayerItems.FakeUlak)
            return FakeUlak;

        return 0;
    }

    public void AddItem(string itemKey, int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return;

        int currentAmount = GetItemAmount(itemKey);
        SetItemAmount(itemKey, currentAmount + amount);

        if (syncNow)
            SyncItemToFirestore(itemKey, GetItemAmount(itemKey));
    }

    public bool SpendItem(string itemKey, int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return false;

        int currentAmount = GetItemAmount(itemKey);

        if (currentAmount < amount)
            return false;

        SetItemAmount(itemKey, currentAmount - amount);

        if (syncNow)
            SyncItemToFirestore(itemKey, GetItemAmount(itemKey));

        return true;
    }

    private void SetItemAmount(string itemKey, int value)
    {
        if (itemKey == Constants.PlayerItems.Defense)
            Defense = value;
        else if (itemKey == Constants.PlayerItems.SlowDown)
            SlowDown = value;
        else if (itemKey == Constants.PlayerItems.WebSnare)
            WebSnare = value;
        else if (itemKey == Constants.PlayerItems.Whip)
            Whip = value;
        else if (itemKey == Constants.PlayerItems.Horsedust)
            Horsedust = value;
        else if (itemKey == Constants.PlayerItems.FakeUlak)
            FakeUlak = value;
        else
            return;

        PlayerPrefs.SetInt(itemKey, value);
        PlayerPrefs.Save();
    }
    public void SetItemAmountFromGame(string itemKey, int value, bool syncNow = false)
    {
        SetItemAmount(itemKey, value);

        if (syncNow)
            SyncItemToFirestore(itemKey, value);
    }
    private void SyncItemToFirestore(string itemKey, int value)
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
    {
        { itemKey, value },
        { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
    };

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Item sync failed: " + task.Exception);
                    return;
                }

                Debug.Log($"Item synced: {itemKey} = {value}");
            });
    }

    private void SyncInventoryToFirestore()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { Constants.PlayerItems.Defense, Defense },
            { Constants.PlayerItems.SlowDown, SlowDown },
            { Constants.PlayerItems.WebSnare, WebSnare },
            { Constants.PlayerItems.Whip, Whip },
            { Constants.PlayerItems.Horsedust, Horsedust },
            { Constants.PlayerItems.FakeUlak, FakeUlak },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        db.Collection("users").Document(uid)
            .SetAsync(data, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Inventory sync failed: " + task.Exception);
                    return;
                }

                Debug.Log("Inventory synced to Firestore");
            });
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SyncInventoryToFirestore();
            SyncMapUnlocksToFirestore();
        }
    }

    private void OnApplicationQuit()
    {
        SyncInventoryToFirestore();
        SyncMapUnlocksToFirestore();
    }

    #endregion
}
