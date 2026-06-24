using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardsPanel : MonoBehaviour
{
    [Header("Map Tabs")]
    [SerializeField] private LeaderboardMapTabUI[] mapTabs;
    [SerializeField] private string[] mapKeys =
    {
        Constants.MapNames.Zarafshan,
        Constants.MapNames.Egypt,
        Constants.MapNames.Registan,
        Constants.MapNames.Kansas,
    };

    [Header("State UI")]
    [SerializeField] private GameObject lockedStateObject;
    [SerializeField] private TMP_Text lockedStateText;
    [SerializeField] private GameObject loadingStateObject;
    [SerializeField] private TMP_Text emptyStateText;
    [SerializeField] private TMP_Text currentLeagueText;
    [SerializeField] private TMP_Text currentDivisionText;
    [SerializeField] private TMP_Text weekText;
    [SerializeField] private TMP_Text promotionInfoText;
    [SerializeField] private TMP_Text rewardInfoText;
    [SerializeField] private TMP_Text errorText;

    [Header("Entries")]
    [SerializeField] private Transform leaderboardContentParent;
    [SerializeField] private LeaderboardEntryUI leaderboardEntryPrefab;
    [SerializeField] private bool fillMissingRowsWithDummyEntries = true;
    [SerializeField, Range(1, 20)] private int minimumDisplayEntries = 20;

    private readonly List<LeaderboardEntryUI> spawnedEntries = new List<LeaderboardEntryUI>();
    private static readonly string[][] DummyPlayerNamesByCountry =
    {
        new[] { "Azim", "Bekzod", "Sardor", "Jasur", "Diyor", "Nodir" },
        new[] { "Alikhan", "Nursultan", "Erlan", "Dauren", "Ayan", "Bekzat" },
        new[] { "Ivan", "Dmitry", "Sergey", "Nikita", "Mikhail", "Pavel" },
        new[] { "Oliver", "Harry", "George", "Jack", "Arthur", "Thomas" },
        new[] { "Adilet", "Bakyt", "Emir", "Ruslan", "Azamat", "Mirlan" },
        new[] { "Myrat", "Serdar", "Batyr", "Dovran", "Maksat", "Yusup" },
        new[] { "Farhod", "Dilshod", "Jamshid", "Behruz", "Suhrob", "Parviz" },
        new[] { "Abdulla", "Yusuf", "Erkin", "Tursun", "Osman", "Ilyas" },
        new[] { "Ahmad", "Farid", "Hamid", "Omar", "Zahir", "Karim" },
        new[] { "Emre", "Mert", "Burak", "Yusuf", "Kerem", "Arda" },
        new[] { "James", "Michael", "Daniel", "Logan", "Ethan", "Ryan" },
        new[] { "Omar", "Youssef", "Karim", "Mostafa", "Hassan", "Tarek" },
        new[] { "Rashad", "Emin", "Tural", "Orkhan", "Elvin", "Kamran" },
        new[] { "Fahad", "Saud", "Khalid", "Nasser", "Majed", "Sultan" }
    };

    private string selectedMapKey;
    private int loadRequestVersion;

    private void OnEnable()
    {
        DataManager.OnMapUnlocked += HandleMapUnlocked;
        OpenPanel();
    }

    private void OnDisable()
    {
        DataManager.OnMapUnlocked -= HandleMapUnlocked;
    }

    public void OpenPanel()
    {
        RefreshStaticInfo();
        RefreshMapTabs();
        SelectDefaultMap();
    }

    public void SelectMap(string mapKey)
    {
        selectedMapKey = mapKey;
        SetSelectedTab(mapKey);
        RefreshStaticInfo();
        ClearEntries();
        SetError("");
        SetLoading(false);

        if (DataManager.Instance == null)
        {
            ShowEmpty("Leaderboard is not ready yet.");
            return;
        }

        if (!DataManager.Instance.CanOpenLeaderboardForMap(mapKey))
        {
            ShowLocked();
            return;
        }

        HideLocked();
        SetLoading(true);
        ShowEmpty("");

        int requestVersion = ++loadRequestVersion;

        DataManager.Instance.LoadLeaderboardState(mapKey, state =>
        {
            if (requestVersion != loadRequestVersion || selectedMapKey != mapKey)
                return;

            ApplyStateHeader(state);
            LoadEntries(mapKey, state.currentLeague, state.currentDivisionId, requestVersion);
        },
        error =>
        {
            if (requestVersion != loadRequestVersion)
                return;

            SetLoading(false);
            SetError(error);
            ShowEmpty("Could not load leaderboard state.");
        });
    }

    private void LoadEntries(string mapKey, string league, string divisionId, int requestVersion)
    {
        DataManager.Instance.LoadWeeklyLeaderboard(mapKey, league, divisionId, entries =>
        {
            if (requestVersion != loadRequestVersion || selectedMapKey != mapKey)
                return;

            SetLoading(false);
            PopulateEntries(entries);
        },
        error =>
        {
            if (requestVersion != loadRequestVersion)
                return;

            SetLoading(false);
            SetError(error);
            ShowEmpty("Could not load leaderboard.");
        });
    }

    private void PopulateEntries(List<LeaderboardEntry> entries)
    {
        ClearEntries();
        List<LeaderboardEntry> displayEntries = BuildDisplayEntries(entries);

        if (displayEntries.Count == 0)
        {
            ShowEmpty("No leaderboard entries this week.");
            return;
        }

        ShowEmpty("");
        string currentUid = FirebaseManager.Instance != null ? FirebaseManager.Instance.UserId : "";

        foreach (LeaderboardEntry entry in displayEntries)
        {
            if (leaderboardEntryPrefab == null || leaderboardContentParent == null)
                break;

            LeaderboardEntryUI row = Instantiate(leaderboardEntryPrefab, leaderboardContentParent);
            row.gameObject.SetActive(true);
            row.Bind(entry, !entry.isDummy && !string.IsNullOrEmpty(currentUid) && entry.uid == currentUid);
            spawnedEntries.Add(row);
        }
    }

    private List<LeaderboardEntry> BuildDisplayEntries(List<LeaderboardEntry> realEntries)
    {
        List<LeaderboardEntry> displayEntries = realEntries != null
            ? new List<LeaderboardEntry>(realEntries)
            : new List<LeaderboardEntry>();

        if (!fillMissingRowsWithDummyEntries)
            return displayEntries;

        int targetCount = Mathf.Clamp(minimumDisplayEntries, 1, 20);
        int missingCount = targetCount - displayEntries.Count;

        if (missingCount <= 0)
            return displayEntries;

        int highestRealWins = 0;

        foreach (LeaderboardEntry entry in displayEntries)
        {
            if (entry != null && entry.weeklyWinCount > highestRealWins)
                highestRealWins = entry.weeklyWinCount;
        }

        List<LeaderboardEntry> dummyEntries = new List<LeaderboardEntry>();

        for (int i = 0; i < missingCount; i++)
            dummyEntries.Add(CreateDummyEntry(displayEntries.Count + i + 1, highestRealWins));

        displayEntries.AddRange(dummyEntries);
        displayEntries.Sort(CompareLeaderboardEntries);

        for (int i = 0; i < displayEntries.Count; i++)
            displayEntries[i].rank = i + 1;

        return displayEntries;
    }

    private int CompareLeaderboardEntries(LeaderboardEntry left, LeaderboardEntry right)
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

        return string.Compare(left.uid, right.uid, System.StringComparison.Ordinal);
    }

    private LeaderboardEntry CreateDummyEntry(int rank, int highestRealWins)
    {
        int maxWins = highestRealWins > 0 ? Mathf.Clamp(highestRealWins + Random.Range(0, 2), 1, 8) : Random.Range(2, 8);
        int wins = maxWins > 0 ? Random.Range(0, maxWins + 1) : 0;
        int races = wins + Random.Range(0, 5);
        int bestTimeMs = Random.Range(78000, 156000);
        int country = Random.Range(0, DummyPlayerNamesByCountry.Length);
        string[] countryNames = DummyPlayerNamesByCountry[country];

        return new LeaderboardEntry
        {
            uid = "dummy_" + rank + "_" + Random.Range(1000, 9999),
            username = countryNames[Random.Range(0, countryNames.Length)],
            country = country,
            league = currentLeagueText != null ? currentLeagueText.text : "",
            divisionId = currentDivisionText != null ? currentDivisionText.text : "",
            weeklyWinCount = wins,
            weeklyRaceCount = races,
            bestTimeMs = bestTimeMs,
            rank = rank,
            rewardAmount = 0,
            rewardClaimed = false,
            isDummy = true
        };
    }

    private void RefreshMapTabs()
    {
        if (mapTabs == null)
            return;

        for (int i = 0; i < mapTabs.Length; i++)
        {
            LeaderboardMapTabUI tab = mapTabs[i];
            if (tab == null)
                continue;

            string key = !string.IsNullOrEmpty(tab.MapKey) ? tab.MapKey : GetMapKeyByIndex(i);
            bool unlocked = DataManager.Instance != null && DataManager.Instance.CanOpenLeaderboardForMap(key);
            tab.Bind(key, unlocked, SelectMap);
            tab.SetSelected(key == selectedMapKey);
        }
    }

    private void SelectDefaultMap()
    {
        string preferredMap = Constants.MapNames.Zarafshan;

        if (DataManager.Instance != null && DataManager.Instance.CanOpenLeaderboardForMap(preferredMap))
        {
            SelectMap(preferredMap);
            return;
        }

        foreach (string mapKey in mapKeys)
        {
            if (DataManager.Instance != null && DataManager.Instance.CanOpenLeaderboardForMap(mapKey))
            {
                SelectMap(mapKey);
                return;
            }
        }

        if (mapKeys != null && mapKeys.Length > 0)
            SelectMap(mapKeys[0]);
    }

    private void SetSelectedTab(string mapKey)
    {
        if (mapTabs == null)
            return;

        foreach (LeaderboardMapTabUI tab in mapTabs)
        {
            if (tab != null)
                tab.SetSelected(tab.MapKey == mapKey);
        }
    }

    private string GetMapKeyByIndex(int index)
    {
        if (mapKeys == null || index < 0 || index >= mapKeys.Length)
            return "";

        return mapKeys[index];
    }

    private void ApplyStateHeader(LeaderboardState state)
    {
        if (currentLeagueText != null)
            currentLeagueText.text = state != null ? state.currentLeague : "bronze";

        if (currentDivisionText != null)
            currentDivisionText.text = state != null ? state.currentDivisionId : "001";
    }

    private void RefreshStaticInfo()
    {
        if (weekText != null && DataManager.Instance != null)
            weekText.text = DataManager.Instance.GetCurrentWeekId();

        if (promotionInfoText != null)
            promotionInfoText.text = "Top 3 promote. Bottom 3 demote.";

        if (rewardInfoText != null)
            rewardInfoText.text = GetRewardInfoText(selectedMapKey);
    }

    private string GetRewardInfoText(string mapKey)
    {
        int firstPlace = 5000;
        int secondPlace = 3000;
        int thirdPlace = 1500;
        int topTen = 500;

        switch (mapKey)
        {
            case Constants.MapNames.Egypt:
                firstPlace = 7000;
                secondPlace = 5000;
                thirdPlace = 3000;
                topTen = 1000;
                break;
        }

        return $"Rewards: 1st {firstPlace:N0} | 2nd {secondPlace:N0} | 3rd {thirdPlace:N0} | 4-10 {topTen:N0} Nyufiy";
    }

    private void ShowLocked()
    {
        SetLoading(false);
        ClearEntries();

        if (lockedStateObject != null)
            lockedStateObject.SetActive(true);

        if (lockedStateText != null)
            lockedStateText.text = "Unlock this map to join the leaderboard.";

        if (currentLeagueText != null)
            currentLeagueText.text = "-";

        if (currentDivisionText != null)
            currentDivisionText.text = "-";

        ShowEmpty("");
    }

    private void HideLocked()
    {
        if (lockedStateObject != null)
            lockedStateObject.SetActive(false);
    }

    private void ShowEmpty(string message)
    {
        if (emptyStateText == null)
            return;

        emptyStateText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        emptyStateText.text = message;
    }

    private void SetLoading(bool loading)
    {
        if (loadingStateObject != null)
            loadingStateObject.SetActive(loading);
    }

    private void SetError(string message)
    {
        if (errorText == null)
            return;

        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        errorText.text = message;
    }

    private void ClearEntries()
    {
        foreach (LeaderboardEntryUI row in spawnedEntries)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedEntries.Clear();
    }

    private void HandleMapUnlocked(string mapKey)
    {
        RefreshMapTabs();

        if (mapKey == selectedMapKey)
            SelectMap(mapKey);
    }
}
