using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardPopup : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    public void SetData(Sprite sprite, string amount)
    {
        icon.sprite = sprite;
        amountText.text = amount;
        Debug.Log(amount + "PPP");
    }
    public void OnClose()
    {
        HomeMainUI.Instance.HideUI(this);
    }

}
