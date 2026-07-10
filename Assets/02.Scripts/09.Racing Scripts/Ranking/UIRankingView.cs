using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRankingView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image bg;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text ranking;
    [SerializeField] private TMP_Text teamName;
    [SerializeField] private Image flagIcon;

    public void SetData( string rank, string name, string teamname, Sprite flag)
    {
        if (nameText) nameText.text = name;
        if (ranking) ranking.text = rank;
        if (teamName)
        {
            string safeTeamName = string.IsNullOrEmpty(teamname) ? "---" : teamname;
            teamName.text = safeTeamName.ToUpper()[..Mathf.Min(3, safeTeamName.Length)];
        }
        if(flagIcon!=null) flagIcon.sprite = flag;
    }

    public void SetColor(Color nameColor, Color bgColor)
    {
        if (nameText) nameText.color = nameColor;
        if (ranking) ranking.color = nameColor;
        if (teamName) teamName.color = nameColor;
        if(bg)bg.color = bgColor;
    }
}
