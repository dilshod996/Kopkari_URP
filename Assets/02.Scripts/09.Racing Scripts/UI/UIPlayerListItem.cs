using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerListItem : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text txtPlayerName;
    [SerializeField] private TMP_Text txtHorseName;
    [SerializeField] private TMP_Text txtRanking;
    [SerializeField] private TMP_Text txtTeam;
    [SerializeField] private TMP_Text txtHorsePower;

    [Header("Optional")]
    [SerializeField] private Slider horsePowerBar;   // ixtiyoriy: 0..100
    [SerializeField] private CanvasGroup canvasGroup; // fade-in uchun

    public void Bind(PlayerEntry data)
    {
        if (txtPlayerName) txtPlayerName.text = data.PlayerName;
        if (txtHorseName) txtHorseName.text = data.HorseName;
        if (txtRanking) txtRanking.text = $"#{data.Ranking}";
        if (txtTeam) txtTeam.text = data.Team;
        if (txtHorsePower) txtHorsePower.text = $"{data.HorsePower}";

        if (horsePowerBar)
        {
            horsePowerBar.minValue = 0;
            horsePowerBar.maxValue = 100;
            horsePowerBar.value = Mathf.Clamp(data.HorsePower, 0, 100);
        }

        if (canvasGroup) canvasGroup.alpha = 0f; // animatsiya oldidan
    }
}
