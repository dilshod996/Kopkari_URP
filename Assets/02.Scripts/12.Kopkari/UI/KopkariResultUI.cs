using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KopkariResultUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text matchDurationText;

    [Header("Best Player")]
    [SerializeField] private TMP_Text bestPlayerNameText;
    [SerializeField] private TMP_Text bestPlayerTeamText;
    [SerializeField] private TMP_Text bestPlayerWinsText;

    [Header("Leaderboard")]
    [SerializeField] private RectTransform leaderboardContent;
    [SerializeField] private KopkariPlayersResult leaderboardRowTemplate;

    [Header("Your Match")]
    [SerializeField] private TMP_Text playerRankText;
    [SerializeField] private TMP_Text playerWinsText;
    [SerializeField] private TMP_Text playerPickupsText;
    [SerializeField] private TMP_Text playerTakeoversText;
    [SerializeField] private TMP_Text playerPossessionText;
    [SerializeField] private TMP_Text playerComboText;
    [SerializeField] private TMP_Text playerBestWinText;
    [SerializeField] private TMP_Text playerLastWinText;

    [Header("Earnings")]
    [SerializeField] private TMP_Text rankNyufiyText;
    [SerializeField] private TMP_Text roundNyufiyText;
    [SerializeField] private TMP_Text comboNyufiyText;
    [SerializeField] private TMP_Text pickupBonusText;
    [SerializeField] private TMP_Text totalNyufiyText;
    [SerializeField] private TMP_Text rankCoinText;
    [SerializeField] private TMP_Text roundCoinText;
    [SerializeField] private TMP_Text totalCoinText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text currentNyufiyText;
    [SerializeField] private TMP_Text currentCoinText;

    [Header("Horse Condition")]
    [SerializeField] private TMP_Text horsePowerText;
    [SerializeField] private TMP_Text horseCoolingText;
    [SerializeField] private TMP_Text horseStaminaText;

    [Header("Actions")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button goHomeButton;

    private readonly List<KopkariPlayersResult> spawnedRows = new List<KopkariPlayersResult>();

    private void Awake()
    {
        if (playerTakeoversText != null && playerTakeoversText.transform.parent != null)
            playerTakeoversText.transform.parent.gameObject.SetActive(false);

        if (headerText == null)
            BuildRuntimeFallback();
    }

    private void OnEnable()
    {
        replayButton?.onClick.AddListener(Replay);
        goHomeButton?.onClick.AddListener(BackLobby);
        RefreshUI();
    }

    private void OnDisable()
    {
        replayButton?.onClick.RemoveListener(Replay);
        goHomeButton?.onClick.RemoveListener(BackLobby);
        ClearLeaderboard();
    }

    public void RefreshFromResults()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        KopkariResultsManager manager = KopkariResultsManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[KopkariResultUI] Results manager is missing.");
            return;
        }

        if (headerText)
        {
            headerText.text = "KOPKARI RESULTS";
            headerText.fontStyle |= FontStyles.Bold;
        }

        SetText(matchDurationText, FormatTime(manager.RaceDuration));

        List<RiderRaceStats> leaderboard = manager.BuildLeaderboard();
        BuildLeaderboard(leaderboard);
        FillBestPlayer(leaderboard);
        FillPlayerResult(manager, leaderboard);
        FillHorseCondition(manager);
    }

    private void FillBestPlayer(List<RiderRaceStats> leaderboard)
    {
        RiderRaceStats best = leaderboard != null && leaderboard.Count > 0 ? leaderboard[0] : null;
        SetText(bestPlayerNameText, best != null ? SafeName(best.playerName) : "-");
        SetText(bestPlayerTeamText, best != null ? SafeTeam(best.teamName) : "-");
        SetText(bestPlayerWinsText, best != null ? best.roundWins.ToString() : "0");
    }

    private void BuildLeaderboard(List<RiderRaceStats> leaderboard)
    {
        ClearLeaderboard();
        if (leaderboardContent == null || leaderboardRowTemplate == null || leaderboard == null)
            return;

        leaderboardRowTemplate.gameObject.SetActive(false);
        for (int i = 0; i < leaderboard.Count; i++)
        {
            KopkariPlayersResult row = Instantiate(leaderboardRowTemplate, leaderboardContent);
            row.gameObject.name = $"PlayerRow_{i + 1:00}";
            row.BindData(leaderboard[i], i);
            spawnedRows.Add(row);
        }
    }

    private void ClearLeaderboard()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            if (spawnedRows[i] != null)
                Destroy(spawnedRows[i].gameObject);
        }
        spawnedRows.Clear();
    }

    private void FillPlayerResult(KopkariResultsManager manager, List<RiderRaceStats> leaderboard)
    {
        int playerIndex = leaderboard.FindIndex(rider => rider != null && rider.isPlayer);
        RiderRaceStats player = playerIndex >= 0 ? leaderboard[playerIndex] : null;
        if (player == null)
        {
            ClearPlayerResult();
            return;
        }

        SetText(playerRankText, $"#{playerIndex + 1}");
        SetText(playerWinsText, player.roundWins.ToString());
        SetText(playerPickupsText, player.pickupTimes.ToString());
        SetText(playerPossessionText, FormatTime(player.totalSpentTime));
        SetText(playerComboText, player.comboWins.ToString());
        SetText(playerBestWinText, player.roundWins > 0 ? FormatTime(player.bestRoundFinishTime) : "--:--");
        SetText(playerLastWinText, player.roundWins > 0 ? FormatTime(player.lastRoundFinishTime) : "--:--");

        KopkariMatchRewardSummary reward = manager.GetOrGrantPlayerMatchReward();
        if (reward == null)
            return;

        SetText(rankNyufiyText, reward.rankNyufiy.ToString("N0"));
        SetText(roundNyufiyText, reward.roundNyufiy.ToString("N0"));
        SetText(comboNyufiyText, reward.comboNyufiy.ToString("N0"));
        SetText(pickupBonusText, reward.pickupBonus.ToString("N0"));
        SetText(totalNyufiyText, reward.totalNyufiy.ToString("N0"));
        SetText(rankCoinText, reward.rankCoin.ToString("N0"));
        SetText(roundCoinText, reward.roundCoin.ToString("N0"));
        SetText(totalCoinText, reward.totalCoin.ToString("N0"));
        SetText(xpText, reward.xp.ToString("N0"));

        if (CurrencyManager.Instance != null)
        {
            SetText(currentNyufiyText, CurrencyManager.Instance.Nyufiy.ToString("N0"));
            SetText(currentCoinText, CurrencyManager.Instance.Coin.ToString("N0"));
        }
    }

    private void FillHorseCondition(KopkariResultsManager manager)
    {
        HorseConditionStats condition = manager.GetOrApplyHorseCondition();
        SetText(horsePowerText, Mathf.RoundToInt(condition.Power).ToString());
        SetText(horseCoolingText, Mathf.RoundToInt(condition.Cooling).ToString());
        SetText(horseStaminaText, Mathf.RoundToInt(condition.Stamina).ToString());
    }

    private void ClearPlayerResult()
    {
        TMP_Text[] texts =
        {
            playerRankText, playerWinsText, playerPickupsText, playerTakeoversText,
            playerPossessionText, playerComboText, playerBestWinText, playerLastWinText
        };
        for (int i = 0; i < texts.Length; i++)
            SetText(texts[i], "-");
    }

    public void Replay()
    {
        if (SceneLoadManager.Instance == null)
            return;

        UIOverlayRoot.I?.ShowMovementPanelForScene(SceneLoadManager.SceneType.Beginer);
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Beginer);
    }

    public void BackLobby()
    {
        if (SceneLoadManager.Instance == null)
            return;

        UIOverlayRoot.I?.ShowPanel(
            UIPanelType.Home,
            LanguageManager.Instance != null ? LanguageManager.Instance.GetText(191) : string.Empty);
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }

    private static string SafeName(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    private static string SafeTeam(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void BuildRuntimeFallback()
    {
        gameObject.name = "KopkariResulPageNew";
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        Image rootImage = GetComponent<Image>();
        if (rootImage == null)
            rootImage = gameObject.AddComponent<Image>();
        rootImage.color = new Color(.035f, .055f, .075f, .985f);

        GameObject layout = UIObject("NewResultLayout", transform);
        Stretch(layout.GetComponent<RectTransform>(), 18f);
        VerticalLayoutGroup rootLayout = layout.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = 10f;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandHeight = false;

        headerText = Text("Header_Bold", layout.transform, "KOPKARI RESULTS", 40, FontStyles.Bold);
        headerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        matchDurationText = Text("MatchDuration", layout.transform, "00:00", 21, FontStyles.Bold);
        matchDurationText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        GameObject body = UIObject("ResultContent", layout.transform);
        body.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childForceExpandWidth = true;

        Transform left = RuntimePanel("Left_PlayerSummary", body.transform, .75f);
        AddSectionTitle(left, "BEST PLAYER");
        bestPlayerNameText = AddValue(left, "Player", 24);
        bestPlayerTeamText = AddValue(left, "Team", 18);
        bestPlayerWinsText = AddRuntimeStat(left, "Round Wins", "0");
        AddSectionTitle(left, "YOUR MATCH");
        playerRankText = AddRuntimeStat(left, "Rank", "-");
        playerWinsText = AddRuntimeStat(left, "Round Wins", "0");
        playerPickupsText = AddRuntimeStat(left, "Ulak Pickups", "0");
        playerTakeoversText = null;
        playerPossessionText = AddRuntimeStat(left, "Total Time", "00:00");
        playerComboText = AddRuntimeStat(left, "Combos", "0");
        playerBestWinText = AddRuntimeStat(left, "Best Win", "--:--");
        playerLastWinText = AddRuntimeStat(left, "Last Win", "--:--");
        AddSectionTitle(left, "HORSE CONDITION");
        horsePowerText = AddRuntimeStat(left, "Power", "0");
        horseCoolingText = AddRuntimeStat(left, "Cooling", "0");
        horseStaminaText = AddRuntimeStat(left, "Stamina", "0");

        Transform center = RuntimePanel("Center_Leaderboard", body.transform, 1.65f);
        AddSectionTitle(center, "LEADERBOARD");
        CreateRuntimeLeaderboardHeader(center);
        GameObject scrollObject = UIObject("Leaderboard_Scroll", center);
        scrollObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        GameObject viewport = UIObject("Viewport", scrollObject.transform);
        Stretch(viewport.GetComponent<RectTransform>(), 0f);
        viewport.AddComponent<RectMask2D>();
        GameObject content = UIObject("Players_Content", viewport.transform);
        leaderboardContent = content.GetComponent<RectTransform>();
        leaderboardContent.anchorMin = new Vector2(0f, 1f);
        leaderboardContent.anchorMax = new Vector2(1f, 1f);
        leaderboardContent.pivot = new Vector2(.5f, 1f);
        VerticalLayoutGroup listLayout = content.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 4f;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = leaderboardContent;
        leaderboardRowTemplate = CreateRuntimeLeaderboardRow("PlayerRow_Template", content.transform, false);
        leaderboardRowTemplate.gameObject.SetActive(false);

        Transform right = RuntimePanel("Right_Earnings", body.transform, .9f);
        AddSectionTitle(right, "YOUR EARNINGS");
        rankNyufiyText = AddRuntimeStat(right, "Rank Nyufiy", "0");
        roundNyufiyText = AddRuntimeStat(right, "Round Nyufiy", "0");
        comboNyufiyText = AddRuntimeStat(right, "Combo Nyufiy", "0");
        pickupBonusText = AddRuntimeStat(right, "Pickup Bonus", "0");
        totalNyufiyText = AddRuntimeStat(right, "Total Nyufiy", "0");
        rankCoinText = AddRuntimeStat(right, "Rank Coins", "0");
        roundCoinText = AddRuntimeStat(right, "Round Coins", "0");
        totalCoinText = AddRuntimeStat(right, "Total Coins", "0");
        xpText = AddRuntimeStat(right, "XP", "0");
        AddSectionTitle(right, "CURRENT BALANCE");
        currentNyufiyText = AddRuntimeStat(right, "Nyufiy", "0");
        currentCoinText = AddRuntimeStat(right, "Coins", "0");

        GameObject actions = UIObject("Actions", layout.transform);
        actions.AddComponent<LayoutElement>().preferredHeight = 70f;
        HorizontalLayoutGroup actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 16f;
        actionLayout.padding = new RectOffset(420, 420, 0, 0);
        actionLayout.childControlWidth = true;
        actionLayout.childForceExpandWidth = true;
        replayButton = RuntimeButton("Replay", actions.transform, "REPLAY");
        goHomeButton = RuntimeButton("GoHome", actions.transform, "GO HOME");
    }

    private static Transform RuntimePanel(string name, Transform parent, float width)
    {
        GameObject panel = UIObject(name, parent);
        panel.AddComponent<Image>().color = new Color(.075f, .11f, .14f, 1f);
        panel.AddComponent<LayoutElement>().flexibleWidth = width;
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 3f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        return panel.transform;
    }

    private static void AddSectionTitle(Transform parent, string title)
    {
        TMP_Text text = Text(title.Replace(" ", "") + "_Header", parent, title, 21, FontStyles.Bold);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 35f;
    }

    private static TMP_Text AddValue(Transform parent, string value, float size)
    {
        TMP_Text text = Text(value + "_Value", parent, value, size, FontStyles.Bold);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 31f;
        return text;
    }

    private static TMP_Text AddRuntimeStat(Transform parent, string label, string value)
    {
        GameObject row = UIObject(label.Replace(" ", "") + "_Row", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 29f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        TMP_Text labelText = Text("Label", row.transform, label, 16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        TMP_Text valueText = Text("Value", row.transform, value, 17, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        valueText.gameObject.AddComponent<LayoutElement>().flexibleWidth = .7f;
        return valueText;
    }

    private static void CreateRuntimeLeaderboardHeader(Transform parent)
    {
        KopkariPlayersResult unused = CreateRuntimeLeaderboardRow("ColumnHeaders", parent, true);
        string[] titles = { "#", "PLAYER", "TEAM", "WINS", "PICKUPS", "TOTAL TIME", "BEST WIN" };
        for (int i = 0; i < titles.Length; i++)
            unused.transform.GetChild(i).GetComponent<TMP_Text>().text = titles[i];
        Object.Destroy(unused);
    }

    private static KopkariPlayersResult CreateRuntimeLeaderboardRow(string name, Transform parent, bool header)
    {
        GameObject row = UIObject(name, parent);
        row.AddComponent<LayoutElement>().preferredHeight = header ? 38f : 48f;
        row.AddComponent<Image>().color = new Color(.1f, .15f, .18f, .8f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.spacing = 2f;
        float[] widths = { .35f, 1.35f, 1f, .55f, .7f, .9f, .8f };
        TMP_Text[] fields = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            fields[i] = Text("Column_" + i, row.transform, "-", 14, header ? FontStyles.Bold : FontStyles.Normal);
            fields[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = widths[i];
        }
        KopkariPlayersResult result = row.AddComponent<KopkariPlayersResult>();
        result.Configure(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6]);
        return result;
    }

    private static Button RuntimeButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = UIObject(name, parent);
        buttonObject.AddComponent<Image>().color = new Color(.13f, .48f, .64f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        TMP_Text text = Text("Text", buttonObject.transform, label, 23, FontStyles.Bold);
        Stretch(text.rectTransform, 0f);
        return button;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = UIObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        return text;
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
