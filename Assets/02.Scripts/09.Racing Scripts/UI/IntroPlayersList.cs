using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroPlayersList : MonoBehaviour
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
    }

    [Header("Race Agents")]
    [SerializeField] private List<RacingAgent> racingAgents = new List<RacingAgent>();

    [Header("UI Items")]
    [SerializeField] private List<PlayerItemView> itemViews = new List<PlayerItemView>();

    [Header("Local Player Fallback")]
    [SerializeField] private string localPlayerNameFallback = "Player";
    [SerializeField] private string localTeamNameFallback = "Kaja Riders";
    [SerializeField] private int localCountryFallbackIndex;

    [Header("NPC Pools")]
    [SerializeField] private List<string> npcRiderNames = new List<string>
    {
        "Azamat", "Bekzod", "Dilshod", "Jasur", "Kamron", "Murod",
        "Oybek", "Qodir", "Ravshan", "Sardor", "Temur", "Ulugbek"
    };

    [SerializeField] private List<string> npcTeamNames = new List<string>
    {
        "Samarkand Riders", "Bukhara Eagles", "Tashkent Wolves", "Fergana Hawks",
        "Khiva Falcons", "Zarafshan Stars", "Registan Royals", "Karakum Racers"
    };

    [SerializeField] private List<CountryOption> countries = new List<CountryOption>();
    [SerializeField] private bool randomizeOnEveryEnable = true;

    private readonly List<int> riderNamePool = new List<int>();
    private readonly List<int> teamNamePool = new List<int>();
    private readonly List<int> countryPool = new List<int>();
    private readonly HashSet<string> usedRiderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> usedTeamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private int generatedRiderNameCount;
    private int generatedTeamNameCount;
    private bool hasBuiltOnce;

    private void OnEnable()
    {
        if (!randomizeOnEveryEnable && hasBuiltOnce)
            return;

        BuildList();
        hasBuiltOnce = true;
    }

    public void BuildList()
    {
        ResetPools();

        int itemIndex = 0;
        RacingAgent localAgent = GetLocalPlayerAgent();

        PlayerIdentity localIdentity = CreateLocalPlayerIdentity();
        usedRiderNames.Add(localIdentity.RiderName);
        usedTeamNames.Add(localIdentity.TeamName);
        RemoveFromPool(npcRiderNames, riderNamePool, localIdentity.RiderName);
        RemoveFromPool(npcTeamNames, teamNamePool, localIdentity.TeamName);
        ApplyIdentity(localAgent, localIdentity);
        BindItem(itemIndex, localIdentity);
        itemIndex++;

        for (int i = 0; i < racingAgents.Count && itemIndex < itemViews.Count; i++)
        {
            RacingAgent agent = racingAgents[i];
            if (agent == null || agent == localAgent || agent.isPlayer)
                continue;

            PlayerIdentity npcIdentity = CreateNpcIdentity();
            ApplyIdentity(agent, npcIdentity);
            BindItem(itemIndex, npcIdentity);
            itemIndex++;
        }

        HideUnusedItems(itemIndex);
    }

    private RacingAgent GetLocalPlayerAgent()
    {
        for (int i = 0; i < racingAgents.Count; i++)
        {
            RacingAgent agent = racingAgents[i];
            if (agent != null && agent.isPlayer)
                return agent;
        }

        return null;
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
        string teamName = GetRandomUnique(npcTeamNames, teamNamePool, usedTeamNames, "Racing Team");
        CountryOption country = GetRandomCountry();

        return new PlayerIdentity(riderName, teamName, GetCountryName(country), GetCountryFlag(country));
    }

    private void ApplyIdentity(RacingAgent agent, PlayerIdentity identity)
    {
        if (agent == null)
            return;

        agent.displayName = identity.RiderName;
        agent.teamName = identity.TeamName;
        agent.countryName = identity.CountryName;
        agent.flagIcon = identity.FlagSprite;
    }

    private void BindItem(int index, PlayerIdentity identity)
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
        generatedRiderNameCount = 0;
        generatedTeamNameCount = 0;
    }

    private void ResetPool<T>(List<T> source, List<int> pool)
    {
        pool.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
            pool.Add(i);
    }

    private string GetRandomUnique(List<string> source, List<int> pool, HashSet<string> usedValues, string fallback)
    {
        if (source == null || source.Count == 0)
            return GetGeneratedFallbackName(fallback, usedValues);

        if (pool.Count == 0)
            return GetGeneratedFallbackName(fallback, usedValues);

        while (pool.Count > 0)
        {
            int poolIndex = UnityEngine.Random.Range(0, pool.Count);
            int sourceIndex = pool[poolIndex];
            pool.RemoveAt(poolIndex);

            string value = source[sourceIndex];
            if (string.IsNullOrWhiteSpace(value) || usedValues.Contains(value))
                continue;

            usedValues.Add(value);
            return value;
        }

        return GetGeneratedFallbackName(fallback, usedValues);
    }

    private string GetGeneratedFallbackName(string fallback, HashSet<string> usedValues)
    {
        while (true)
        {
            string generatedValue;

            if (fallback == "Rider")
            {
                generatedRiderNameCount++;
                generatedValue = $"{fallback} {generatedRiderNameCount}";
            }
            else
            {
                generatedTeamNameCount++;
                generatedValue = $"{fallback} {generatedTeamNameCount}";
            }

            if (usedValues.Add(generatedValue))
                return generatedValue;
        }
    }

    private void RemoveFromPool(List<string> source, List<int> pool, string usedValue)
    {
        if (source == null || pool == null || string.IsNullOrWhiteSpace(usedValue))
            return;

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            int sourceIndex = pool[i];
            if (sourceIndex < 0 || sourceIndex >= source.Count)
                continue;

            if (string.Equals(source[sourceIndex], usedValue, StringComparison.OrdinalIgnoreCase))
                pool.RemoveAt(i);
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

        int safeIndex = Mathf.Clamp(index, 0, countries.Count - 1);
        return countries[safeIndex];
    }

    private string GetCountryName(CountryOption country)
    {
        if (country == null || string.IsNullOrWhiteSpace(country.countryName))
            return "";

        return country.countryName;
    }

    private Sprite GetCountryFlag(CountryOption country)
    {
        return country != null ? country.flagSprite : null;
    }

    private string GetPlayerPrefsString(string key, string fallback)
    {
        string value = PlayerPrefs.GetString(key, fallback);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
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
}
