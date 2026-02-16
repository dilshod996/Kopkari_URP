using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpecialReachTriggerPoint : MonoBehaviour
{
    [Header("Trigger Filter")]
    [SerializeField] private string requiredTag = "RacingHead";

    [Header("Rules")]
    [SerializeField] private float graceSeconds = 10f;

    [Header("AI Elimination")]
    [SerializeField] private bool disableAI = true; // true => SetActive(false), false => Destroy

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private readonly HashSet<RacingAgent> _passed = new HashSet<RacingAgent>();
    private bool _timerStarted;
    private Coroutine _routine;




    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        _passed.Clear();
        _timerStarted = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        var agent = other.GetComponentInParent<RacingAgent>();
        if (agent == null) return;

        // Bir agent bir marta hisoblanadi
        if (!_passed.Add(agent)) return;

        if (logDebug)
            Debug.Log($"[SpecialReach] Passed: {agent.displayName} (player={agent.isPlayer})");

        // Birinchi o'tgan agent => timer start
        if (!_timerStarted)
        {
            _timerStarted = true;
            _routine = StartCoroutine(GraceCountdown());
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
        var agents = controller.AllAgents;
        if (agents == null || agents.Count == 0)
        {
            UIButtonActions.Instance.HideSpecialTrigger();
            yield break;
        }

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
                EliminateAI(a);
            }
        }

        UIButtonActions.Instance.HideSpecialTrigger();
    }


    private void EliminateAI(RacingAgent agent)
    {
        RacingLeaderboard.Instance?.Unregister(agent);
        RacingController.Instance?.RemoveAgent(agent);
        agent.DisableNavmesh();
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
