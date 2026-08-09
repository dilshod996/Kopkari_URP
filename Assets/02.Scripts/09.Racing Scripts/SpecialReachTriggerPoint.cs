using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpecialReachTriggerPoint : MonoBehaviour
{
    public static event Action OnFirstAIRiderEntered;
    public static event Action OnPlayerEntered;

    [Header("Trigger Filter")]
    [SerializeField] private string requiredTag = "RacingHead";

    [Header("Rules")]
    [SerializeField] private float graceSeconds = 10f;

    [Header("Optimization")]
    [SerializeField] private bool optimizeMarkerParticles;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private readonly HashSet<RacingAgent> _passed = new HashSet<RacingAgent>();
    private readonly List<RacingAgent> _passedOrder = new List<RacingAgent>();
    private bool _timerStarted;
    private bool _aiTutorialSignalSent;
    private Coroutine _routine;
    private bool _playerCompleted;
    private ParticleSystem[] _markerParticles;
    private static readonly List<SpecialReachTriggerPoint> ActivePoints =
        new List<SpecialReachTriggerPoint>();




    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        _passed.Clear();
        _passedOrder.Clear();
        _timerStarted = false;
        _aiTutorialSignalSent = false;
        _playerCompleted = false;
        if (optimizeMarkerParticles)
        {
            CacheMarkerParticles();

            if (!ActivePoints.Contains(this))
                ActivePoints.Add(this);
            RefreshMarkerVisuals();
        }

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (optimizeMarkerParticles)
        {
            ActivePoints.Remove(this);
            RefreshMarkerVisuals();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        var agent = other.GetComponentInParent<RacingAgent>();
        if (agent == null) return;

        var controller = RacingController.Instance;
        if (controller == null) return;

        controller.RegisterAgent(agent);

        // Bir agent bir marta hisoblanadi
        if (!_passed.Add(agent)) return;
        _passedOrder.Add(agent);

        if (logDebug)
            Debug.Log($"[SpecialReach] Passed: {agent.displayName} (player={agent.isPlayer})");

        if (agent.isPlayer)
        {
            _playerCompleted = true;
            if (optimizeMarkerParticles)
                RefreshMarkerVisuals();
            OnPlayerEntered?.Invoke();
        }

        // Birinchi o'tgan agent => timer start
        if (!_timerStarted && !controller.IsRaceOver)
        {
            bool notifyTutorial = !agent.isPlayer && !_aiTutorialSignalSent;
            _timerStarted = true;
            _routine = StartCoroutine(GraceCountdown());

            // StartCoroutine runs through ShowSpecialTrigger before its first
            // yield, so listeners can safely highlight the visible panel.
            if (notifyTutorial)
            {
                _aiTutorialSignalSent = true;
                OnFirstAIRiderEntered?.Invoke();
            }
        }
    }

    private static void RefreshMarkerVisuals()
    {
        ActivePoints.RemoveAll(point => point == null);
        SpecialReachTriggerPoint next = ActivePoints
            .Where(point => point.isActiveAndEnabled && !point._playerCompleted)
            .OrderBy(point => point.GetCheckpointOrder())
            .FirstOrDefault();

        for (int i = 0; i < ActivePoints.Count; i++)
            ActivePoints[i].SetMarkerVisible(ActivePoints[i] == next);
    }

    private int GetCheckpointOrder()
    {
        if (TryGetTrailingNumber(gameObject.name, out int numberedOrder))
            return numberedOrder;

        if (transform.parent != null &&
            TryGetUnityCopyOrder(transform.parent.name, out int copyOrder))
            return copyOrder;

        return transform.parent != null
            ? transform.parent.GetSiblingIndex()
            : transform.GetSiblingIndex();
    }

    private void CacheMarkerParticles()
    {
        _markerParticles = GetComponentsInChildren<ParticleSystem>(true);
        if (_markerParticles.Length > 0 || transform.parent == null)
            return;

        Transform closestMarkerRoot = null;
        float closestDistance = float.PositiveInfinity;
        Transform groupRoot = transform.parent;

        for (int i = 0; i < groupRoot.childCount; i++)
        {
            Transform candidate = groupRoot.GetChild(i);
            if (candidate == transform) continue;
            if (candidate.GetComponentInChildren<ParticleSystem>(true) == null) continue;

            float distance = (candidate.position - transform.position).sqrMagnitude;
            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestMarkerRoot = candidate;
        }

        _markerParticles = closestMarkerRoot != null
            ? closestMarkerRoot.GetComponentsInChildren<ParticleSystem>(true)
            : Array.Empty<ParticleSystem>();
    }

    private static bool TryGetTrailingNumber(string value, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(value)) return false;

        int end = value.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(value[end])) end--;
        int start = end;
        while (start >= 0 && char.IsDigit(value[start])) start--;

        return start < end && int.TryParse(value.Substring(start + 1, end - start), out number);
    }

    private static bool TryGetUnityCopyOrder(string value, out int order)
    {
        order = 0;
        if (string.IsNullOrEmpty(value) ||
            !value.StartsWith("TimeCheckTrigger", StringComparison.Ordinal))
            return false;

        int openParenthesis = value.LastIndexOf('(');
        int closeParenthesis = value.LastIndexOf(')');
        if (openParenthesis < 0 || closeParenthesis <= openParenthesis)
            return true;

        if (!int.TryParse(
                value.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1),
                out int copyIndex))
            return false;

        order = copyIndex + 1;
        return true;
    }

    private void SetMarkerVisible(bool visible)
    {
        if (_markerParticles == null) return;

        for (int i = 0; i < _markerParticles.Length; i++)
        {
            ParticleSystem marker = _markerParticles[i];
            if (marker == null) continue;

            if (visible)
            {
                if (!marker.isPlaying)
                    marker.Play(true);
            }
            else
            {
                marker.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private IEnumerator GraceCountdown()
    {
        if (logDebug)
            Debug.Log($"[SpecialReach] Timer started: {graceSeconds}s");

        var controller = RacingController.Instance;
        if (controller == null) yield break;

        float timeLeft = graceSeconds;

        bool playerPassed = false;

        // UI ochiladi
        UIButtonActions.Instance.ShowSpecialTrigger((int)timeLeft);

        while (timeLeft > 0f)
        {
            // ✅ PLAYER o'tib ketsa coroutine to'xtamasin
            if (!playerPassed && HasPlayerPassed())
            {
                playerPassed = true;
                UIButtonActions.Instance.HideSpecialTrigger(); // UI yop
                                                               // yield break ❌ yo'q
            }

            yield return new WaitForSeconds(1f);

            timeLeft -= 1f;

            // UI faqat player o'tmagan bo'lsa update qilamiz (xohlasang doim update ham qilsa bo'ladi)
            if (!playerPassed)
                UIButtonActions.Instance.ShowSpecialTrigger((int)timeLeft);
        }

        // ⛔ Time tugadi – endi kimlar o‘tmaganini tekshiramiz
        var agents = new List<RacingAgent>(controller.AllAgents);
        if (agents == null || agents.Count == 0)
        {
            UIButtonActions.Instance.HideSpecialTrigger();
            yield break;
        }

        List<RacingAgent> failedAgents = new List<RacingAgent>();

        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null) continue;

            if (_passed.Contains(a)) continue;

            if (a.isPlayer)
            {
                if (logDebug) Debug.Log("[SpecialReach] PLAYER failed => GameOver");
                controller.PlayerFailedSpecialReach(a, this);
            }
            else
            {
                if (logDebug) Debug.Log($"[SpecialReach] AI failed => eliminated: {a.displayName}");
                failedAgents.Add(a);
                EliminateAI(a);
            }
        }

        UIButtonActions.Instance.HideSpecialTrigger();

        if (HasPlayerPassed() && !controller.IsRaceOver)
            RacingLeaderboard.Instance?.ApplyOrderAndShow(_passedOrder, failedAgents);
    }


    private void EliminateAI(RacingAgent agent)
    {
        RacingLeaderboard.Instance?.Unregister(agent);
        RacingController.Instance?.RemoveAgent(agent);
        agent.DisableNavmesh();

        var aiRider = agent.GetComponentInParent<AIRacingRider>();
        if (aiRider != null)
            aiRider.gameObject.SetActive(false);
        else
            agent.gameObject.SetActive(false);

        UIButtonActions.Instance.EliminitedRider(agent.displayName, agent.flagIcon,false);
    }

    private bool HasPlayerPassed()
    {
        foreach (var a in _passed)
        {
            if (a != null && a.isPlayer)
                return true;
        }
        return false;
    }

  

}
