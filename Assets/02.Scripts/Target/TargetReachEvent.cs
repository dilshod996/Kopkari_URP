using MalbersAnimations.Controller;
using System;
using UnityEngine;

public class TargetReachEvent : MonoBehaviour
{
    [SerializeField] private Pickable lambObject;
    public static event Action<int, bool> OnReachedTargetWithLamb; // winner-only actions
    public static event Action OnRoundEnded;
    private bool triggerLocked;
    [SerializeField] private string requiredChildTag = "RacingHead";

    private void OnEnable()
    {
        BaseManager.OnResetTarget += ResetTrigger;
    }

    private void OnDisable()
    {
        BaseManager.OnResetTarget -= ResetTrigger;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerLocked) return;
        if (!other.CompareTag(requiredChildTag)) return;
        // Rider rootni topamiz (player ham, npc ham)
        var riderRoot = other.transform.root;
        if (riderRoot == null) return;
        bool isPlayer = riderRoot.CompareTag("Player");
        bool isNpc = riderRoot.CompareTag("NPC");
        Debug.Log($"Entered to final pos and is NPC {isNpc} or is Player {isPlayer}");
        if (!isPlayer && !isNpc) return;
        var bm = KopkariManager.Instance;
        if (bm == null) return;
        if (bm.currentGoatOwner == null) return;
        if (bm.currentGoatOwner != riderRoot.gameObject) return;
        int riderid = 0;
        // Rider ID aniqlash
        if (isNpc)
        {
            riderid = riderRoot.GetComponent<NPCGetLamb_CodeAI>().GetId();
        }
        else
        {
            riderid = PlayerPrefs.GetInt(Constants.Player.Userid);
        }


        // 2) RaceResultsManager ga finish deb aytamiz
        KopkariResultsManager.Instance?.OnFinish(riderid);
        KopkariResultsManager.Instance.DebugLogLeaderboard();
        // 1) Winner o‘z drop/win logicini qiladi
        OnReachedTargetWithLamb?.Invoke(riderid, isPlayer);

        // 2) Round tugadi → hamma warm pointga ketadi
        OnRoundEnded?.Invoke();

        // 3) Player bo‘lsa BaseManager flow
        if (isPlayer)
        {
            KopkariManager.Instance?.MarkPlayerReachedTarget();
        }

        triggerLocked = true;
    }

    public void ResetTrigger()
    {
        triggerLocked = false;
    }
}
