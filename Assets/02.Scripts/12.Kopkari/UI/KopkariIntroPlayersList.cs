using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KopkariIntroPlayersList : MonoBehaviour
{
    [Serializable]
    private class CountryOption
    {
        public string countryName;
        public Sprite flagSprite;
    }

    [Serializable]
    private class PlayerItemView
    {
        public GameObject root;
        public TMP_Text riderNameText;
        public TMP_Text teamNameText;
        public TMP_Text countryNameText;
        public Image flagImage;
        public GameObject readyIndicator;
        public TMP_Text readyText;
    }

    private readonly struct PlayerIdentity
    {
        public PlayerIdentity(string riderName, string teamName, string countryName, Sprite flagSprite)
        {
            RiderName = riderName;
            TeamName = teamName;
            CountryName = countryName;
            FlagSprite = flagSprite;
        }

        public string RiderName { get; }
        public string TeamName { get; }
        public string CountryName { get; }
        public Sprite FlagSprite { get; }
    }

    [Header("UI Items")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private int titleLanguageId = -1;
    [SerializeField] private List<PlayerItemView> itemViews = new List<PlayerItemView>();

    [Header("Local Player Fallback")]
    [SerializeField] private string localPlayerNameFallback = "Player";
    [SerializeField] private string localTeamNameFallback = "Kaja Riders";
    [SerializeField] private int localCountryFallbackIndex;

    [Header("NPC Identity Pools")]
    [SerializeField] private List<string> npcRiderNames = new List<string>
    {
        "Azamat", "Bekzod", "Dilshod", "Jasur", "Kamron", "Murod",
        "Oybek", "Qodir", "Ravshan", "Sardor", "Temur", "Ulugbek"
    };

    [SerializeField] private List<string> npcTeamNames = new List<string>
    {
        "Samarkand Riders", "Bukhara Eagles", "Tashkent Wolves", "Fergana Hawks",
        "Khiva Falcons", "Zarafshan Stars", "Registan Royals", "Karakum Riders"
    };
    [SerializeField] private List<string> npcHorseNames = new List<string>
    {
        "Tulpor", "Qorabayir", "Shabdez", "Boz", "Jiyron", "Samandar",
        "Yulduz", "Lochin", "Dovul", "Sarbaz", "Olmos", "Qalqon"
    };
    [SerializeField] private Vector2Int npcWinningsRange = new Vector2Int(3, 45);

    [SerializeField] private List<CountryOption> countries = new List<CountryOption>();
    [Header("Status Language IDs")]
    [SerializeField] private int readyLabelLanguageId = -1;
    [SerializeField] private int movingLabelLanguageId = -1;

    private readonly List<int> riderNamePool = new List<int>();
    private readonly List<int> teamNamePool = new List<int>();
    private readonly List<int> countryPool = new List<int>();
    private readonly HashSet<string> usedRiderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> usedTeamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AIKopkariRider, PlayerItemView> riderViews =
        new Dictionary<AIKopkariRider, PlayerItemView>();

    private int generatedRiderCount;
    private int generatedTeamCount;

    public string LocalRiderName { get; private set; }
    public string LocalTeamName { get; private set; }
    public string LocalCountryName { get; private set; }
    public Sprite LocalFlagIcon { get; private set; }

    public void BuildList(IReadOnlyList<AIKopkariRider> riders)
    {
        SetLocalizedText(titleText, titleLanguageId);
        ResetPools();
        riderViews.Clear();

        int itemIndex = 0;
        PlayerIdentity localIdentity = CreateLocalPlayerIdentity();
        LocalRiderName = localIdentity.RiderName;
        LocalTeamName = localIdentity.TeamName;
        LocalCountryName = localIdentity.CountryName;
        LocalFlagIcon = localIdentity.FlagSprite;
        ReserveIdentity(localIdentity);
        BindItem(itemIndex++, localIdentity, null);

        if (riders != null)
        {
            for (int i = 0; i < riders.Count; i++)
            {
                AIKopkariRider rider = riders[i];
                if (rider == null)
                    continue;

                PlayerIdentity identity = CreateNpcIdentity();
                rider.SetIdentity(
                    identity.RiderName,
                    identity.TeamName,
                    identity.CountryName,
                    identity.FlagSprite,
                    GetRandomHorseName(),
                    GetRandomWinnings());

                if (itemIndex < itemViews.Count)
                    BindItem(itemIndex++, identity, rider);
            }
        }

        HideUnusedItems(itemIndex);
        RefreshReadiness();
    }

    public void RefreshReadiness()
    {
        SetLocalizedText(titleText, titleLanguageId);
        string readyLabel = GetLocalizedText(readyLabelLanguageId);
        string movingLabel = GetLocalizedText(movingLabelLanguageId);

        if (itemViews.Count > 0 && itemViews[0] != null)
        {
            PlayerItemView localPlayerView = itemViews[0];
            if (localPlayerView.readyIndicator != null)
                localPlayerView.readyIndicator.SetActive(true);
            if (localPlayerView.readyText != null)
                localPlayerView.readyText.text = readyLabel;
        }

        foreach (KeyValuePair<AIKopkariRider, PlayerItemView> entry in riderViews)
        {
            AIKopkariRider rider = entry.Key;
            PlayerItemView view = entry.Value;
            bool ready = rider != null && rider.IsReadyAtStart;

            if (view.readyIndicator != null)
                view.readyIndicator.SetActive(ready);
            if (view.readyText != null)
                view.readyText.text = ready ? readyLabel : movingLabel;
        }
    }

    private PlayerIdentity CreateLocalPlayerIdentity()
    {
        string riderName = GetPlayerPrefsString(Constants.Player.UsernameKey, localPlayerNameFallback);
        string teamName = GetPlayerPrefsString(Constants.Player.TeamName, localTeamNameFallback);
        int countryIndex = PlayerPrefs.GetInt(Constants.Player.CountryName, localCountryFallbackIndex);
        CountryOption country = GetCountryByIndex(countryIndex);
        return new PlayerIdentity(riderName, teamName, GetCountryName(country), GetCountryFlag(country));
    }

    private PlayerIdentity CreateNpcIdentity()
    {
        string riderName = GetRandomUnique(npcRiderNames, riderNamePool, usedRiderNames, "Rider");
        string teamName = GetRandomUnique(npcTeamNames, teamNamePool, usedTeamNames, "Kopkari Team");
        CountryOption country = GetRandomCountry();
        return new PlayerIdentity(riderName, teamName, GetCountryName(country), GetCountryFlag(country));
    }

    private string GetRandomHorseName()
    {
        if (npcHorseNames == null || npcHorseNames.Count == 0)
            return "Horse";
        return npcHorseNames[UnityEngine.Random.Range(0, npcHorseNames.Count)];
    }

    private int GetRandomWinnings()
    {
        int min = Mathf.Max(0, Mathf.Min(npcWinningsRange.x, npcWinningsRange.y));
        int max = Mathf.Max(min, Mathf.Max(npcWinningsRange.x, npcWinningsRange.y));
        return UnityEngine.Random.Range(min, max + 1);
    }

    private void BindItem(int index, PlayerIdentity identity, AIKopkariRider rider)
    {
        if (index < 0 || index >= itemViews.Count)
            return;

        PlayerItemView view = itemViews[index];
        if (view == null)
            return;

        if (view.root != null)
            view.root.SetActive(true);
        if (view.riderNameText != null)
            view.riderNameText.text = identity.RiderName;
        if (view.teamNameText != null)
            view.teamNameText.text = identity.TeamName;
        if (view.countryNameText != null)
            view.countryNameText.text = identity.CountryName;
        if (view.flagImage != null)
        {
            view.flagImage.sprite = identity.FlagSprite;
            view.flagImage.enabled = identity.FlagSprite != null;
        }

        if (rider != null)
            riderViews[rider] = view;
        else
        {
            if (view.readyIndicator != null)
                view.readyIndicator.SetActive(true);
            if (view.readyText != null)
                view.readyText.text = GetLocalizedText(readyLabelLanguageId);
        }
    }

    private void HideUnusedItems(int firstUnusedIndex)
    {
        for (int i = firstUnusedIndex; i < itemViews.Count; i++)
        {
            PlayerItemView view = itemViews[i];
            if (view != null && view.root != null)
                view.root.SetActive(false);
        }
    }

    private void ResetPools()
    {
        ResetPool(npcRiderNames, riderNamePool);
        ResetPool(npcTeamNames, teamNamePool);
        ResetPool(countries, countryPool);
        usedRiderNames.Clear();
        usedTeamNames.Clear();
        generatedRiderCount = 0;
        generatedTeamCount = 0;
    }

    private static void ResetPool<T>(List<T> source, List<int> pool)
    {
        pool.Clear();
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
            pool.Add(i);
    }

    private void ReserveIdentity(PlayerIdentity identity)
    {
        usedRiderNames.Add(identity.RiderName);
        usedTeamNames.Add(identity.TeamName);
        RemoveFromPool(npcRiderNames, riderNamePool, identity.RiderName);
        RemoveFromPool(npcTeamNames, teamNamePool, identity.TeamName);
    }

    private string GetRandomUnique(
        List<string> source,
        List<int> pool,
        HashSet<string> usedValues,
        string fallback)
    {
        while (source != null && pool.Count > 0)
        {
            int poolIndex = UnityEngine.Random.Range(0, pool.Count);
            int sourceIndex = pool[poolIndex];
            pool.RemoveAt(poolIndex);

            string value = source[sourceIndex];
            if (!string.IsNullOrWhiteSpace(value) && usedValues.Add(value))
                return value;
        }

        while (true)
        {
            int number = fallback == "Rider" ? ++generatedRiderCount : ++generatedTeamCount;
            string value = $"{fallback} {number}";
            if (usedValues.Add(value))
                return value;
        }
    }

    private static void RemoveFromPool(List<string> source, List<int> pool, string usedValue)
    {
        if (source == null || string.IsNullOrWhiteSpace(usedValue))
            return;

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            int sourceIndex = pool[i];
            if (sourceIndex >= 0 &&
                sourceIndex < source.Count &&
                string.Equals(source[sourceIndex], usedValue, StringComparison.OrdinalIgnoreCase))
            {
                pool.RemoveAt(i);
            }
        }
    }

    private CountryOption GetRandomCountry()
    {
        if (countries == null || countries.Count == 0)
            return null;
        if (countryPool.Count == 0)
            ResetPool(countries, countryPool);

        int poolIndex = UnityEngine.Random.Range(0, countryPool.Count);
        int countryIndex = countryPool[poolIndex];
        countryPool.RemoveAt(poolIndex);
        return GetCountryByIndex(countryIndex);
    }

    private CountryOption GetCountryByIndex(int index)
    {
        if (countries == null || countries.Count == 0)
            return null;
        return countries[Mathf.Clamp(index, 0, countries.Count - 1)];
    }

    private static string GetCountryName(CountryOption country)
    {
        return country == null || string.IsNullOrWhiteSpace(country.countryName)
            ? string.Empty
            : country.countryName;
    }

    private static Sprite GetCountryFlag(CountryOption country)
    {
        return country != null ? country.flagSprite : null;
    }

    private static string GetPlayerPrefsString(string key, string fallback)
    {
        string value = PlayerPrefs.GetString(key, fallback);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
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
}
