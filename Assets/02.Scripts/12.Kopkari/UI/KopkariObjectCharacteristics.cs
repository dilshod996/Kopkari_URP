using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class KopkariObjectCharacteristics : MonoBehaviour
{
    [Header("Map Details")]
    [SerializeField] private GameObject mapDetailsBackground;
    [FormerlySerializedAs("mapNameText")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private string mapDisplayName = "REGISTAN";
    [SerializeField] private int ulakInfoLanguageId = -1;
    [SerializeField] private int mainRivalInfoLanguageId = -1;
    [SerializeField] private int localPlayerInfoLanguageId = -1;
    [SerializeField] private int riderInfoLanguageId = -1;

    [Header("Localized Labels")]
    [SerializeField] private TMP_Text nameLabelText;
    [SerializeField] private int nameLabelLanguageId = -1;
    [SerializeField] private TMP_Text horseNameLabelText;
    [SerializeField] private int horseNameLabelLanguageId = -1;
    [SerializeField] private TMP_Text countryLabelText;
    [SerializeField] private int countryLabelLanguageId = -1;
    [SerializeField] private TMP_Text teamNameLabelText;
    [SerializeField] private int teamNameLabelLanguageId = -1;
    [SerializeField] private TMP_Text winningsLabelText;
    [SerializeField] private int winningsLabelLanguageId = -1;

    [Header("Location Description")]
    [SerializeField] private GameObject locationDescriptionBackground;
    [SerializeField] private TMP_Text locationDescriptionText;
    [SerializeField] private List<int> randomLocationDescriptionLanguageIds = new List<int> { 342 };
    [SerializeField] private int playerListDescriptionLanguageId = -1;
    [SerializeField] private int gateDescriptionLanguageId = -1;
    [SerializeField] private int ulakDescriptionLanguageId = -1;
    [SerializeField] private int mainRivalDescriptionLanguageId = -1;
    [SerializeField] private int localPlayerDescriptionLanguageId = -1;
    [SerializeField] private int riderDescriptionLanguageId = -1;

    [Header("Full Characteristics Background (Main Rival + Local Player)")]
    [SerializeField] private GameObject fullBackground;
    [SerializeField] private TMP_Text fullTitleText;
    [SerializeField] private int mainRivalTitleLanguageId = -1;
    [SerializeField] private int localPlayerTitleLanguageId = -1;
    [SerializeField] private TMP_Text fullRiderNameText;
    [SerializeField] private TMP_Text fullHorseNameText;
    [SerializeField] private TMP_Text fullCountryNameText;
    [SerializeField] private Image fullFlagImage;
    [SerializeField] private TMP_Text fullTeamNameText;
    [SerializeField] private TMP_Text fullWinningsText;

    [Header("Standard AI Rider Background")]
    [SerializeField] private GameObject riderBackground;
    [SerializeField] private TMP_Text riderNameText;
    [SerializeField] private TMP_Text riderCountryNameText;
    [SerializeField] private Image riderFlagImage;

    [Header("Fallback")]
    [SerializeField] private string playerHorseFallback = "Horse";

    private void Awake()
    {
        ApplyLocalizedLabels();
        HideAll();
    }

    public void ShowGateMap()
    {
        ShowMapOnly(mapDisplayName);
        ShowDescription(gateDescriptionLanguageId);
    }

    public void ShowUlak()
    {
        ShowMapOnly(GetLocalizedText(ulakInfoLanguageId));
        ShowDescription(ulakDescriptionLanguageId);
    }

    public void ShowRandomLocationDescription()
    {
        SetActive(mapDetailsBackground, false);
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
        ShowDescription(GetRandomLanguageId(randomLocationDescriptionLanguageIds));
    }

    public void ShowLocalPlayerDescription()
    {
        SetActive(mapDetailsBackground, false);
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
        ShowDescription(localPlayerDescriptionLanguageId);
    }

    public void ShowPlayerListDescription()
    {
        SetActive(mapDetailsBackground, false);
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
        ShowDescription(playerListDescriptionLanguageId);
    }

    public void ShowMainRival(AIKopkariRider rider)
    {
        if (rider == null)
        {
            HideAll();
            return;
        }

        ApplyLocalizedLabels();
        ShowMapTitle(GetLocalizedText(mainRivalInfoLanguageId));
        SetLocalizedText(fullTitleText, mainRivalTitleLanguageId);
        ShowFull(
            rider.RiderName,
            rider.HorseName,
            rider.CountryName,
            rider.FlagIcon,
            rider.TeamName,
            rider.Winnings);
        ShowDescription(mainRivalDescriptionLanguageId);
    }

    public void ShowLocalPlayer(KopkariIntroPlayersList playersList)
    {
        string riderName = playersList != null
            ? playersList.LocalRiderName
            : PlayerPrefs.GetString(Constants.Player.UsernameKey, "Player");
        string teamName = playersList != null
            ? playersList.LocalTeamName
            : PlayerPrefs.GetString(Constants.Player.TeamName, "Kaja Riders");
        string countryName = playersList != null ? playersList.LocalCountryName : string.Empty;
        Sprite flag = playersList != null ? playersList.LocalFlagIcon : null;
        string horseName = PlayerPrefs.GetString(Constants.Horse.HorseNameKey, playerHorseFallback);
        int wins = PlayerPrefs.GetInt(Constants.RacingData.TotalWins, 0);

        ApplyLocalizedLabels();
        ShowMapTitle(GetLocalizedText(localPlayerInfoLanguageId));
        SetLocalizedText(fullTitleText, localPlayerTitleLanguageId);
        ShowFull(riderName, horseName, countryName, flag, teamName, wins);
        ShowDescription(localPlayerDescriptionLanguageId);
    }

    public void ShowRider(AIKopkariRider rider)
    {
        if (rider == null)
        {
            HideAll();
            return;
        }

        ApplyLocalizedLabels();
        ShowMapTitle(GetLocalizedText(riderInfoLanguageId));
        SetActive(fullBackground, false);
        SetActive(riderBackground, true);
        SetText(riderNameText, rider.RiderName);
        SetText(riderCountryNameText, rider.TeamName);
        SetFlag(riderFlagImage, rider.FlagIcon);
        ShowDescription(riderDescriptionLanguageId);
    }

    public void HideAll()
    {
        SetActive(mapDetailsBackground, false);
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
        SetActive(locationDescriptionBackground, false);
        if (locationDescriptionText != null)
            locationDescriptionText.text = string.Empty;
    }

    private void ShowMapOnly(string title)
    {
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
        ShowMapTitle(title);
    }

    private void ShowMapTitle(string title)
    {
        SetActive(mapDetailsBackground, true);
        SetText(infoText, title);
    }

    private void ApplyLocalizedLabels()
    {
        SetLocalizedLabel(nameLabelText, nameLabelLanguageId);
        SetLocalizedLabel(horseNameLabelText, horseNameLabelLanguageId);
        SetLocalizedLabel(countryLabelText, countryLabelLanguageId);
        SetLocalizedLabel(teamNameLabelText, teamNameLabelLanguageId);
        SetLocalizedLabel(winningsLabelText, winningsLabelLanguageId);
    }

    private void ShowDescription(int languageId)
    {
        SetActive(locationDescriptionBackground, true);
        if (locationDescriptionText == null)
            return;

        locationDescriptionText.text = GetLocalizedText(languageId);
    }

    private static int GetRandomLanguageId(List<int> languageIds)
    {
        if (languageIds == null || languageIds.Count == 0)
            return -1;

        return languageIds[Random.Range(0, languageIds.Count)];
    }

    private static void SetLocalizedLabel(TMP_Text target, int languageId)
    {
        if (target == null)
            return;

        string localizedText = GetLocalizedText(languageId);
        target.text = string.IsNullOrWhiteSpace(localizedText)
            ? string.Empty
            : localizedText.TrimEnd() + ":";
    }

    private static void SetLocalizedText(TMP_Text target, int languageId)
    {
        if (target != null)
            target.text = GetLocalizedText(languageId);
    }

    private static string GetLocalizedText(int languageId)
    {
        return languageId >= 0 && LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(languageId)
            : string.Empty;
    }

    private void ShowFull(
        string riderName,
        string horseName,
        string countryName,
        Sprite flag,
        string teamName,
        int wins)
    {
        SetActive(riderBackground, false);
        SetActive(fullBackground, true);
        SetText(fullRiderNameText, riderName);
        SetText(fullHorseNameText, horseName);
        SetText(fullCountryNameText, countryName);
        SetFlag(fullFlagImage, flag);
        SetText(fullTeamNameText, teamName);
        SetText(fullWinningsText, Mathf.Max(0, wins).ToString());
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static void SetFlag(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
