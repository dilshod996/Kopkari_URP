// RacingLeaderboard.cs (incremental ranking)
using MalbersAnimations.Controller;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RacingLeaderboard : MonoBehaviour
{
    public static RacingLeaderboard Instance { get; private set; }
    public static event System.Action OnLeaderboardShown;
    
    [Header("Refs")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private UIRankingView itemPrefab;
    [SerializeField] private List<RaceCheckpoint> checkpoints = new();
    public int CheckpointCount => checkpoints.Count; // <-- shu orqali hisoblanadi

    [Header("Reveal Animation")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private float showDuration = 0.35f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private float visibleSeconds = 4f;
    [SerializeField] private float hiddenOffsetY = 40f;
    [SerializeField] private float showStartScale = 0.96f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    // Unikal agentlar
    private readonly List<RacingAgent> standings = new();                      // tartiblangan ro‘yxat
    private readonly HashSet<RacingAgent> agentSet = new();                    // dublikatga yo‘l qo‘ymaydi
    private readonly Dictionary<RacingAgent, UIRankingView> rows = new();      // agent -> UI qatori
    public bool RaceStarted { get; private set; }
    public float RaceStartTime { get; private set; }
    public bool RaceFinished { get; private set; }
    private static readonly Color playerTextColor = new Color32(35, 246, 4, 255);
    private static readonly Color playerBgColor = new Color32(255, 251, 0, 255);
    private static readonly Color eliminatedTextColor = new Color32(123, 123, 123, 255);
    private static readonly Color eliminatedBgColor = new Color32(255, 20, 0, 255);
    private Vector2 shownAnchoredPosition;
    private Sequence revealSequence;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (panelRoot != null)
            shownAnchoredPosition = panelRoot.anchoredPosition;

        HideImmediate();
    }

    private void OnDestroy()
    {
        revealSequence?.Kill();

        if (Instance == this)
            Instance = null;
    }

    public void Register(RacingAgent agent)
    {
        if (agent == null || agentSet.Contains(agent)) return;
        if (contentParent == null || itemPrefab == null) return;

        agentSet.Add(agent);
        standings.Add(agent);
        agent.Ranking = standings.Count;

        if (RaceStarted && !agent.HasStarted)
            agent.BeginRace(RaceStartTime);

        var row = Instantiate(itemPrefab, contentParent);
        rows[agent] = row;
        UpdateRow(agent, standings.Count);
    }

    public void Unregister(RacingAgent agent)
    {
        if (agent == null || !agentSet.Remove(agent)) return;

        standings.Remove(agent);
        if (rows.TryGetValue(agent, out var row) && row)
        {
            Destroy(row.gameObject);
        }
        rows.Remove(agent);

        // Pastdagilar rankini yangilash
        for (int i = 0; i < standings.Count; i++)
            UpdateRow(standings[i], i + 1);
    }

    public void ApplyOrderAndShow(IReadOnlyList<RacingAgent> orderedAgents, bool autoHide = true)
    {
        ApplyOrderAndShow(orderedAgents, null, autoHide);
    }

    public void ApplyOrderAndShow(IReadOnlyList<RacingAgent> orderedAgents, IReadOnlyList<RacingAgent> eliminatedAgents, bool autoHide = true)
    {
        if (orderedAgents == null || orderedAgents.Count == 0) return;

        HashSet<RacingAgent> eliminatedSet = new();
        if (eliminatedAgents != null)
        {
            for (int i = 0; i < eliminatedAgents.Count; i++)
            {
                RacingAgent agent = eliminatedAgents[i];
                if (agent != null)
                    eliminatedSet.Add(agent);
            }
        }

        HashSet<RacingAgent> visibleSet = new();
        List<RacingAgent> visibleOrder = new(orderedAgents.Count + eliminatedSet.Count);

        for (int i = 0; i < orderedAgents.Count; i++)
        {
            RacingAgent agent = orderedAgents[i];
            if (agent == null || !visibleSet.Add(agent)) continue;

            Register(agent);
            visibleOrder.Add(agent);
        }

        for (int i = 0; i < standings.Count; i++)
        {
            RacingAgent agent = standings[i];
            if (agent == null || eliminatedSet.Contains(agent) || !agent.gameObject.activeInHierarchy || !visibleSet.Add(agent)) continue;

            visibleOrder.Add(agent);
        }

        if (eliminatedAgents != null)
        {
            for (int i = 0; i < eliminatedAgents.Count; i++)
            {
                RacingAgent agent = eliminatedAgents[i];
                if (agent == null || !visibleSet.Add(agent)) continue;

                Register(agent);
                visibleOrder.Add(agent);
            }
        }

        if (visibleOrder.Count == 0) return;

        standings.Clear();
        standings.AddRange(visibleOrder);

        for (int i = 0; i < standings.Count; i++)
        {
            RacingAgent agent = standings[i];
            if (agent == null) continue;

            agent.Ranking = i + 1;
            bool isEliminated = eliminatedSet.Contains(agent);

            if (rows.TryGetValue(agent, out UIRankingView row) && row != null)
                row.transform.SetSiblingIndex(i);

            UpdateRow(agent, i + 1, isEliminated);
        }

        if (contentParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);

        ShowAnimated(autoHide);
    }

    public void ShowAnimated(bool autoHide = true)
    {
        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        revealSequence?.Kill();
        gameObject.SetActive(true);

        if (panelGroup == null || panelRoot == null)
            return;

        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        panelGroup.alpha = 0f;

        panelRoot.anchoredPosition = shownAnchoredPosition + Vector2.down * hiddenOffsetY;
        panelRoot.localScale = Vector3.one * showStartScale;

        revealSequence = DOTween.Sequence()
            .Join(panelGroup.DOFade(1f, showDuration))
            .Join(panelRoot.DOAnchorPos(shownAnchoredPosition, showDuration).SetEase(showEase))
            .Join(panelRoot.DOScale(1f, showDuration).SetEase(showEase))
            .AppendCallback(() =>
            {
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
                OnLeaderboardShown?.Invoke();
            });

        if (autoHide)
        {
            revealSequence
                .AppendInterval(visibleSeconds)
                .AppendCallback(HideAnimated);
        }
    }

    public void HideAnimated()
    {
        if (panelGroup == null || panelRoot == null)
        {
            HideImmediate();
            return;
        }

        revealSequence?.Kill();

        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        revealSequence = DOTween.Sequence()
            .Join(panelGroup.DOFade(0f, hideDuration))
            .Join(panelRoot.DOAnchorPos(shownAnchoredPosition + Vector2.down * hiddenOffsetY, hideDuration).SetEase(hideEase))
            .Join(panelRoot.DOScale(showStartScale, hideDuration).SetEase(hideEase));
    }

    public void HideImmediate()
    {
        revealSequence?.Kill();

        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        if (panelRoot == null)
            panelRoot = transform as RectTransform;

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.anchoredPosition = shownAnchoredPosition + Vector2.down * hiddenOffsetY;
            panelRoot.localScale = Vector3.one * showStartScale;
        }
    }

    // Global start — bitta vaqtda hamma agentga
    public void StartRace()
    {
        if (RaceStarted) return;
        RaceStarted = true;
        RaceStartTime = Time.time;

        foreach (var a in agentSet)
            if (a != null && !a.HasStarted)
                a.BeginRace(RaceStartTime);
    }

    // Incremental yangilash — BU YERDA BeginRace() ENDI YO'Q!
    public void NotifyCheckpoint(RacingAgent agent)
    {
        if (agent == null) return;
        if (RaceFinished) return; // ✅ lock
        if (!agentSet.Contains(agent)) Register(agent);

        // ⛔ Poyga boshlanmaguncha ranking/log ishlamasin
        if (!RaceStarted) return;

        int oldIndex = standings.IndexOf(agent);
        if (oldIndex < 0) return;

        int i = oldIndex;
        while (i > 0 && IsAhead(standings[i], standings[i - 1]))
        {
            var tmp = standings[i - 1];
            standings[i - 1] = standings[i];
            standings[i] = tmp;
            i--;
        }

        if (i == oldIndex)
        {
            agent.Ranking = oldIndex + 1;
            UpdateRow(agent, oldIndex + 1);
            return;
        }

        int from = Mathf.Min(i, oldIndex);
        int to = Mathf.Max(i, oldIndex);

        for (int k = from; k <= to; k++)
        {
            var ag = standings[k];
            ag.Ranking = k + 1;

            if (rows.TryGetValue(ag, out var row) && row != null)
            {
                row.transform.SetSiblingIndex(k);
                UpdateRow(ag, k + 1);
            }
        }
    }


    private static bool IsAhead(RacingAgent A, RacingAgent B)
    {
        if (A.Passed != B.Passed) return A.Passed > B.Passed;
        if (A.CheckpointIndex != B.CheckpointIndex) return A.CheckpointIndex > B.CheckpointIndex;
        return A.ElapsedTime < B.ElapsedTime; // eng tez — oldinda
    }
    private void UpdateRow(RacingAgent a, int rank, bool eliminated = false)
    {
        if (!rows.TryGetValue(a, out var row) || row == null) return;
        int cpShow = Mathf.Max(0, a.CheckpointIndex);
        string rankText = eliminated ? "X" : $"{rank}.";
        row.SetData(rankText, $"{a.displayName}", $"{a.countryName}", a.flagIcon);/*$"CP {cpShow} • Passed {a.Passed}"*/
        if (eliminated)
        {
            row.SetColor(eliminatedTextColor, eliminatedBgColor);
            return;
        }

        if (a.isPlayer) // yoki o‘zingda qanday belgilang bo‘lsa
        {
            row.SetColor(playerTextColor, playerBgColor);
        }
        //else
        //{
        //    row.SetColor(Color.white, new Color(0.8f, 0.8f, 0.8f)); // boshqalar oq-yumshoq rangda
        //}
    }
    // 🟢 Yangi funksiya: joriy standings ro‘yxatini qaytaradi
    public List<RacingAgent> GetStandings()
    {
        // bu ro‘yxat har doim yangilanib turgan — shunchaki nusxasini qaytaramiz
        return new List<RacingAgent>(standings);
    }

    // 🟢 Agar faqat ism va pozitsiyalar kerak bo‘lsa:
    public List<string> GetStandingsNames()
    {
        List<string> names = new();
        for (int i = 0; i < standings.Count; i++)
        {
            var a = standings[i];
            names.Add($"{i + 1}. {a.displayName}");
        }
        return names;
    }
    // 🟡 Player rankni qaytaradi
    // Agar player topilmasa yoki race boshlanmagan bo‘lsa -1 qaytaradi
    public int PlayerRank()
    {
        if (!RaceStarted) return -1;

        for (int i = 0; i < standings.Count; i++)
        {
            var agent = standings[i];
            if (agent != null && agent.isPlayer)
            {
                return i + 1; // Rank 1 dan boshlanadi
            }
        }

        return -1; // Player yo‘q
    }
    public int GetRank(RacingAgent agent)
    {
        var list = standings; // sening sorted ro'yxating
        int idx = list.IndexOf(agent);
        return idx >= 0 ? idx + 1 : -1;
    }
    /// <summary>
    /// Race yakunlanadi (ranking lock).
    /// Xohlasang UI win/lose screen shu yerdan trigger qilasan.
    /// </summary>
    public void FinishRace()
    {
        if (!RaceStarted || RaceFinished) return;

        RaceFinished = true;

        // Final cleanup: elim bo'lgan / scene'dan chiqib ketgan agentlarni olib tashlash
        UnregisterDroppedAgents();

        // Optional: yakuniy rowlarni bir marta yangilab qo'yamiz
        for (int i = 0; i < standings.Count; i++)
        {
            var a = standings[i];
            if (a == null) continue;
            a.Ranking = i + 1;
            UpdateRow(a, i + 1);
        }

        // Optional: Final standings log
        // Debug.Log("Race Finished. Final standings:\n" + string.Join("\n", GetStandingsNames()));
    }

    /// <summary>
    /// Disable/Destroy bo'lib ketgan agentlarni leaderboarddan olib tashlaydi.
    /// (AI eliminate qilinganda ko'pincha agent.gameObject inactive bo'ladi)
    /// </summary>
    public void UnregisterDroppedAgents()
    {
        // standings ni aylanib, o'chganlarni yig'ib olamiz (modification safe)
        List<RacingAgent> toRemove = null;

        for (int i = 0; i < standings.Count; i++)
        {
            var a = standings[i];

            // Destroy bo'lgan agentlarni olib tashlaymiz. Inactive eliminated agentlar final resultda DNF bo'lib ko'rinishi kerak.
            if (a == null)
            {
                toRemove ??= new List<RacingAgent>(8);
                toRemove.Add(a);
            }
        }

        if (toRemove == null) return;

        for (int i = 0; i < toRemove.Count; i++)
        {
            // a null bo'lsa ham Unregister ichida null check bor
            Unregister(toRemove[i]);
        }
    }

}
