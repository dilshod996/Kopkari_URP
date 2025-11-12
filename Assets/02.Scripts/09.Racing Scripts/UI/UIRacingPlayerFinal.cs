using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIRacingPlayerFinal : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text txtRanking;
    [SerializeField] private TMP_Text txtPlayerName;
    [SerializeField] private TMP_Text txtTime;
    [SerializeField] private TMP_Text txtTeam;
    [SerializeField] private TMP_Text txtEarnings;
    [SerializeField] private CanvasGroup canvasGroup; // fade-in uchun


    public void Bind(RacingAgent data)
    {
        if (data == null) return;

        float prize = 0f;
        switch (data.Ranking)
        {
            case 1: prize = 5f; break;
            case 2: prize = 3f; break;
            case 3: prize = 1f; break;
            default: prize = 0f; break;
        }

        // Agentning earnings qiymatini ham yangilab qo‘yish (keyinchalik saqlash uchun)
        // ⛳ Finish qilmaganlarga vaqt "-" va prize = 0 (xohlasang qoldir)
        bool dnf = !data.HasFinished;

        data.earnings = dnf ? 0f : prize;

        if (txtRanking) txtRanking.text = $"#{data.Ranking}";
        if (txtPlayerName) txtPlayerName.text = data.displayName;
        if (txtTime) txtTime.text = dnf ? "-" : $"{data.LastSplitTime:0.00}s";
        if (txtTeam) txtTeam.text = data.teamName;
        if (txtEarnings) txtEarnings.text = $"${data.earnings:0}";

        if (canvasGroup) canvasGroup.alpha = 0f; // fade anim oldidan 0
    }
}
