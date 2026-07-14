using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KopkariObjectCharacteristics : MonoBehaviour
{
    [Header("Map Details")]
    [SerializeField] private GameObject mapDetailsBackground;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private string mapDisplayName = "REGISTAN";
    [SerializeField] private string ulakDisplayName = "THE ULAK";
    [SerializeField] private string mainRivalDisplayName = "MAIN RIVAL";
    [SerializeField] private string localPlayerDisplayName = "YOU";
    [SerializeField] private string riderDisplayName = "RIVAL";

    [Header("Full Characteristics Background (Main Rival + Local Player)")]
    [SerializeField] private GameObject fullBackground;
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

    [Header("Formatting")]
    [SerializeField] private string winningsFormat = "{0} Wins";
    [SerializeField] private string playerHorseFallback = "Horse";

    private void Awake()
    {
        HideAll();
    }

    public void ShowGateMap()
    {
        ShowMapOnly(mapDisplayName);
    }

    public void ShowUlak()
    {
        ShowMapOnly(ulakDisplayName);
    }

    public void ShowMainRival(AIKopkariRider rider)
    {
        if (rider == null)
        {
            HideAll();
            return;
        }

        ShowMapTitle(mainRivalDisplayName);
        ShowFull(
            rider.RiderName,
            rider.HorseName,
            rider.CountryName,
            rider.FlagIcon,
            rider.TeamName,
            rider.Winnings);
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

        ShowMapTitle(localPlayerDisplayName);
        ShowFull(riderName, horseName, countryName, flag, teamName, wins);
    }

    public void ShowRider(AIKopkariRider rider)
    {
        if (rider == null)
        {
            HideAll();
            return;
        }

        ShowMapTitle(riderDisplayName);
        SetActive(fullBackground, false);
        SetActive(riderBackground, true);
        SetText(riderNameText, rider.RiderName);
        SetText(riderCountryNameText, rider.TeamName);
        SetFlag(riderFlagImage, rider.FlagIcon);
    }

    public void HideAll()
    {
        SetActive(mapDetailsBackground, false);
        SetActive(fullBackground, false);
        SetActive(riderBackground, false);
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
        SetText(mapNameText, title);
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
        SetText(fullWinningsText, string.Format(winningsFormat, Mathf.Max(0, wins)));
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
