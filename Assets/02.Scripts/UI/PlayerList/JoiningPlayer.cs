using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;

public class JoiningPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerStatusText;
    [SerializeField] private TMP_Text horseNameText;
    public string playerName;
    public string horseName;
    public bool isAIPlayer = true;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (!isAIPlayer)
        {
            GetUserData();
        }
        else
        {
            playerNameText.text = playerName;
            horseNameText.text = horseName;
        }
        playerStatusText.text = LanguageManager.Instance.GetText(275);
    }

    private void GetUserData()
    {
        playerNameText.text = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        horseNameText.text = PlayerPrefs.GetString(Constants.Horse.HorseNameKey);
    }
}
