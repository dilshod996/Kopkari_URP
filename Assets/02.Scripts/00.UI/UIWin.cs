using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class UIWin : MonoBehaviour
{
    [Header("Titles")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text prizesText;

    [Header("Best Result")]
    [SerializeField] private TMP_Text bestResultTimeValue;
    [SerializeField] private TMP_Text catchLambTryValue;

    [Header("Current Result")]
    [SerializeField] private TMP_Text currentResultTimeValue;
    [SerializeField] private TMP_Text currentCatchLambTryValue;

    [Header("Prize Display")]
    [SerializeField] private GameObject prizePrefab;
    [SerializeField] private Transform prizeParentLayout;

    [Header("Bottom Details")]
    [SerializeField] private Button backBtn;
    [SerializeField] private Button playAgainBtn;
    [SerializeField] private GameObject RecordObj;

    [SerializeField] private PrizeManager prizeManager;

    private void OnEnable()
    {
        DisplayWinInfo();

        backBtn.onClick.AddListener(BackToLobby);
        playAgainBtn.onClick.AddListener(PlayAgain);
    }

    private void OnDisable()
    {
        backBtn.onClick.RemoveListener(BackToLobby);
        playAgainBtn.onClick.RemoveListener(PlayAgain);
    }

    private void DisplayWinInfo()
    {
        List<RoundPrizeRecord> allRecords = prizeManager.GetAllPrizeHistory();
        if (allRecords == null || allRecords.Count == 0) return;

        string roomName = SceneManager.GetActiveScene().name;

        // 1. Umumiy vaqt va uloqlar hisoblash (current & best uchun)
        float totalTime = 0f;
        int totalLambs = 0;
        foreach (var record in allRecords)
        {
            totalTime += record.spentTime;
            totalLambs += record.lambCatchCount;
        }

        // 2. Save if this total is better than previous
        SaveBestResultIfNeeded(roomName, totalTime, totalLambs);
        LoadBestResult(roomName, out float bestTime, out int bestLambs);

        // 3. Format mm:ss
        TimeSpan totalTimeSpan = TimeSpan.FromSeconds(totalTime);
        currentResultTimeValue.text = $"{totalTimeSpan.Minutes:D2}:{totalTimeSpan.Seconds:D2}";
        currentCatchLambTryValue.text = totalLambs.ToString();

        TimeSpan bestTimeSpan = TimeSpan.FromSeconds(bestTime);
        bestResultTimeValue.text = $"{bestTimeSpan.Minutes:D2}:{bestTimeSpan.Seconds:D2}";
        catchLambTryValue.text = bestLambs.ToString();

        // 4. All prizes from all rounds
        List<Prize> allPrizes = new List<Prize>();
        foreach (var record in allRecords)
        {
            allPrizes.AddRange(record.wonPrizes);
        }

        DisplayPrizes(allPrizes);
        prizesText.text = $"Sovrinlaringiz {allPrizes.Count} ta!";
    }


    private void SaveBestResultIfNeeded(string roomName, float newTime, int newLambs)
    {
        string timeKey = $"BestTime_{roomName}";
        string lambKey = $"BestLamb_{roomName}";

        float bestTime = PlayerPrefs.GetFloat(timeKey, float.MaxValue);
        int bestLambs = PlayerPrefs.GetInt(lambKey, 0);

        bool isNewRecord = false;

        if (newLambs > bestLambs || (newLambs == bestLambs && newTime < bestTime))
        {
            isNewRecord = true;
            PlayerPrefs.SetFloat(timeKey, newTime);
            PlayerPrefs.SetInt(lambKey, newLambs);
            PlayerPrefs.Save();
        }

        if (RecordObj != null)
            RecordObj.SetActive(isNewRecord);
    }


    private void LoadBestResult(string roomName, out float bestTime, out int bestLambs)
    {
        string timeKey = $"BestTime_{roomName}";
        string lambKey = $"BestLamb_{roomName}";

        bestTime = PlayerPrefs.GetFloat(timeKey, 0f);
        bestLambs = PlayerPrefs.GetInt(lambKey, 0);
    }

    private void DisplayPrizes(List<Prize> prizes)
    {
        foreach (Transform child in prizeParentLayout)
        {
            Destroy(child.gameObject);
        }

        foreach (var prize in prizes)
        {
            GameObject prizeGO = Instantiate(prizePrefab, prizeParentLayout);
            PrizeInfo prizeInfo = prizeGO.GetComponent<PrizeInfo>();
            prizeInfo.SetPrize(prize);
        }
    }

    private void BackToLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }

    private void PlayAgain()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Registan);
    }
}
