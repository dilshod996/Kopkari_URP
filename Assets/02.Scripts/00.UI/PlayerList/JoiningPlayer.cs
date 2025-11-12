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
        playerStatusText.text = LanguageManager.Instance.GetText(304);
        StartCoroutine(ChangeStatusAfterRandomDelay());
    }

    private void GetUserData()
    {
        playerNameText.text = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        horseNameText.text = PlayerPrefs.GetString(Constants.Horse.HorseNameKey);
    }
    private IEnumerator ChangeStatusAfterRandomDelay()
    {
        float delay = Random.Range(2f, 5f);
        yield return new WaitForSeconds(delay);
        playerStatusText.text = LanguageManager.Instance.GetText(275);
    }
}
