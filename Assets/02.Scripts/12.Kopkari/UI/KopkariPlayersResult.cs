using TMPro;
using UnityEngine;

public class KopkariPlayersResult : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text teamNameText;
    [SerializeField] private TMP_Text winsCountText;
    [SerializeField] private TMP_Text pickUpsCountText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text bestWinText;

    [Header("Colors")]
    [SerializeField] private Color playerNameColor = new Color(1f, 0.84f, 0f, 1f); // gold
    [SerializeField] private Color normalNameColor = Color.white;

    public void Configure(
        TMP_Text rank,
        TMP_Text playerName,
        TMP_Text teamName,
        TMP_Text wins,
        TMP_Text pickups,
        TMP_Text totalTime,
        TMP_Text bestWin)
    {
        rankText = rank;
        playerNameText = playerName;
        teamNameText = teamName;
        winsCountText = wins;
        pickUpsCountText = pickups;
        totalTimeText = totalTime;
        bestWinText = bestWin;
    }

    public void BindData(RiderRaceStats stats, int rankIndex)
    {
        if (stats == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);

        if (rankText) rankText.text = (rankIndex + 1).ToString();

        if (playerNameText)
        {
            playerNameText.text = string.IsNullOrEmpty(stats.playerName) ? "Unknown" : stats.playerName;
            playerNameText.color = stats.isPlayer ? playerNameColor : normalNameColor;
        }

        if (teamNameText) teamNameText.text = string.IsNullOrEmpty(stats.teamName) ? "-" : stats.teamName;
        if (pickUpsCountText) pickUpsCountText.text = stats.pickupTimes.ToString();
        if (totalTimeText) totalTimeText.text = FormatTime(stats.totalSpentTime);
        if (winsCountText) winsCountText.text = stats.roundWins.ToString();
        if (bestWinText)
            bestWinText.text = stats.roundWins > 0 ? FormatTime(stats.bestRoundFinishTime) : "--:--";
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
