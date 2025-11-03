// UIPlayerListItem.cs
using TMPro;
using UnityEngine;

public class UIRankingView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text metaText; // masalan Lap/CP/progress

    public void SetData(string name, string meta)
    {
        if (nameText) nameText.text = name;
        if (metaText) metaText.text = meta;
    }
}
