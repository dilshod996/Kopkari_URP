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
    //[SerializeField] private TMP_Text taqaCoinText;
    //[SerializeField] private TMP_Text nyufiyCointText;


    public void Bind(RacingAgent data)
    {
        if (data == null) return;
        //int taqaPrize = 0;
        //int nyufiyPrize = 0;

        // ❗ Finish qilmaganlar uchun prize = 0
        bool dnf = !data.HasFinished;
        bool dnf2 = data.isPlayer;

        //switch (data.Ranking)
        //{
        //    case 1:
        //        taqaPrize = 2;
        //        nyufiyPrize = 1500;
        //        break;

        //    case 2:
        //        taqaPrize = 1;
        //        nyufiyPrize = 1100;
        //        break;

        //    case 3:
        //        taqaPrize = 0;
        //        nyufiyPrize = 700;
        //        break;

        //    default:
        //        taqaPrize = 0;
        //        nyufiyPrize = 300;
        //        break;
        //}

        //if (dnf)
        //{
        //    taqaPrize = 0;
        //    nyufiyPrize = 0;
        //}

        //// 👉 Agent ichida ham yangilab qo‘y (keyinchalik save uchun)
        //data.taqaCoins = taqaPrize;
        //data.nyufiyCoins = nyufiyPrize;

        // UI Fieldlarni to‘ldiramiz
        if (txtRanking) txtRanking.text = $"#{data.Ranking}";
        if (dnf2) txtPlayerName.color = Color.yellow;
        if (txtPlayerName) txtPlayerName.text = data.displayName;
        if (txtTime) txtTime.text = dnf ? "-" : $"{data.LastSplitTime:0.00}s";
        if (txtTeam) txtTeam.text = data.teamName;

        //if (taqaCoinText) taqaCoinText.text = $"+{taqaPrize:N0}";
        //if (nyufiyCointText) nyufiyCointText.text = $"+{nyufiyPrize:N0}";


    }

}
