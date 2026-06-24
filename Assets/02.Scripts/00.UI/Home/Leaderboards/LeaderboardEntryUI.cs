using TMPro;
using UnityEngine;

public class LeaderboardEntryUI : MonoBehaviour
{
    private static readonly string[] CountryNames =
    {
        "Uzbekistan",
        "Kazakhstan",
        "Russia",
        "England",
        "Kyrgyzstan",
        "Turkmenistan",
        "Tajikistan",
        "East Turkestan",
        "Afghanistan",
        "Turkey",
        "USA",
        "Egypt",
        "Azerbaijan",
        "Saudi Arabia"
    };

    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text countryText;
    [SerializeField] private TMP_Text winCountText;
    [SerializeField] private TMP_Text raceCountText;
    [SerializeField] private TMP_Text bestTimeText;
    [SerializeField] private GameObject currentUserHighlight;

    private void Awake()
    {
        AutoWire();
    }

    public void Bind(LeaderboardEntry entry, bool isCurrentUser)
    {
        AutoWire();

        if (entry == null)
            return;

        if (rankText != null)
            rankText.text = entry.rank > 0 ? entry.rank.ToString() : "-";

        if (usernameText != null)
            usernameText.text = string.IsNullOrEmpty(entry.username) ? "Player" : entry.username;

        if (countryText != null)
            countryText.text = GetCountryName(entry.country);

        if (winCountText != null)
            winCountText.text = entry.weeklyWinCount.ToString();

        if (raceCountText != null)
            raceCountText.text = entry.weeklyRaceCount.ToString();

        if (bestTimeText != null)
            bestTimeText.text = FormatBestTime(entry.bestTimeMs);

        if (currentUserHighlight != null)
            currentUserHighlight.SetActive(isCurrentUser);
    }

    private string FormatBestTime(int bestTimeMs)
    {
        if (bestTimeMs <= 0)
            return "-";

        float seconds = bestTimeMs / 1000f;
        return $"{seconds:0.00}s";
    }

    private string GetCountryName(int country)
    {
        if (country < 0 || country >= CountryNames.Length)
            return "-";

        return CountryNames[country];
    }

    private void AutoWire()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            switch (text.gameObject.name)
            {
                case "RankText":
                    rankText = rankText != null ? rankText : text;
                    break;
                case "UsernameText":
                    usernameText = usernameText != null ? usernameText : text;
                    break;
                case "CountryText":
                    countryText = countryText != null ? countryText : text;
                    break;
                case "WinCountText":
                    winCountText = winCountText != null ? winCountText : text;
                    break;
                case "RaceCountText":
                    raceCountText = raceCountText != null ? raceCountText : text;
                    break;
                case "BestTimeText":
                    bestTimeText = bestTimeText != null ? bestTimeText : text;
                    break;
            }
        }

        Transform highlight = transform.Find("Highlight");
        if (currentUserHighlight == null && highlight != null)
            currentUserHighlight = highlight.gameObject;
    }
}
