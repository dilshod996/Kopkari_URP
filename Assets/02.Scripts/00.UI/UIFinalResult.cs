using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFinalResult : MonoBehaviour
{
    [Header("UI Final Result")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text horseName;
    [SerializeField] private TMP_Text backBtnText;
    [SerializeField] private TMP_Text playAgainBtnText;

    [Header("Settings")]
    [SerializeField] private Button backBtn;
    [SerializeField] private Button playAgainBtn;
    [SerializeField] private Transform prizeParentLayout;
    [SerializeField] private GameObject prizePrefab;
    public SceneLoadManager.SceneType sceneType;

    [SerializeField] private PrizeManager prizeManager;

    private void OnEnable()
    {
        Transilitons();
        playAgainBtn.onClick.AddListener(PlayAgain);
        backBtn.onClick.AddListener(BackToLobby);
        DisplayWinInfo();
    }
    private void OnDisable()
    {
        playAgainBtn.onClick.RemoveListener(PlayAgain);
        backBtn.onClick.RemoveListener(BackToLobby);
    }

    private void Transilitons()
    {
        title.text = LanguageManager.Instance.GetText(278);
        string playerNameGet = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        playerName.text = playerNameGet;
        string horseNameGet = PlayerPrefs.GetString(PlayerCatalogProvider.HorseBodyDisplayNamePrefKey);
        horseName.text = horseNameGet;
        backBtnText.text = LanguageManager.Instance.GetText(254);
        playAgainBtnText.text = LanguageManager.Instance.GetText(279);
    }
    private void BackToLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }
    private void PlayAgain()
    {
        SceneLoadManager.Instance.LoadScene(sceneType);
    }
    public void DisplayWinInfo()
    {
        List<RoundPrizeRecord> allRecords = prizeManager.GetAllPrizeHistory();
        List<Prize> allPrizes = new List<Prize>();
        foreach (var record in allRecords)
        {
            allPrizes.AddRange(record.wonPrizes);
        }
        DisplayPrizes(allPrizes);
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
}
