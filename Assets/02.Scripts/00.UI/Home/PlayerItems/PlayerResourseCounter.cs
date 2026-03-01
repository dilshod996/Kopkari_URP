using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourseCounter : MonoBehaviour
{
    public PlayerResourse.Resources resources;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private int languageId;
    private string itemName;
    private int itemAmount;

    private void OnEnable()
    {
        GetData();
        PlayerResourse.OnResourseBought += GetSetResource;
        if(LanguageManager.Instance != null)
        {
            nameText.text =  LanguageManager.Instance.GetText(languageId);
        }
    }
    private void OnDisable()
    {
        PlayerResourse.OnResourseBought -= GetSetResource;
    }
    private void GetSetResource(PlayerResourse.Resources comeResource, int amount)
    {
        if (resources != comeResource)
            return;
        countText.text = $"X{amount}";
    }
    private void GetData()
    {
        itemName = GetItemKey(resources);
        if (string.IsNullOrEmpty(itemName))
            return;
        itemAmount = PlayerPrefs.GetInt(itemName, 0);
        countText.text = $"X{itemAmount}";
    }
    private string GetItemKey(PlayerResourse.Resources resource)
    {
        switch (resource)
        {
            case PlayerResourse.Resources.WalkZone:
                return Constants.PlayerItems.SlowDown;
            case PlayerResourse.Resources.Defender:
                return Constants.PlayerItems.Defense;
            case PlayerResourse.Resources.WebSnare:
                return Constants.PlayerItems.WebSnare;
            case PlayerResourse.Resources.Whiplash:
                return Constants.PlayerItems.Whip;
            case PlayerResourse.Resources.HorseDust:
                return Constants.PlayerItems.Horsedust;
            default:
                return null;
        }
    }
}
