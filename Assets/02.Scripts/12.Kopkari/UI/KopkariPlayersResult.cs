using TMPro;
using UnityEngine;

public class KopkariPlayersResult : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text teamNameText;
    [SerializeField] private TMP_Text catchCountText;
    [SerializeField] private TMP_Text catchTimingText;
    [SerializeField] private TMP_Text triggerCountText;

    [Header("Colors")]
    [SerializeField] private Color playerNameColor = new Color(1f, 0.84f, 0f, 1f); // gold
    [SerializeField] private Color normalNameColor = Color.white;

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
        if (catchCountText) catchCountText.text = stats.pickupTimes.ToString();
        if (triggerCountText) triggerCountText.text = stats.triggerPoints.ToString();
        if (catchTimingText) catchTimingText.text = FormatTime(stats.totalCatchTime);
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
