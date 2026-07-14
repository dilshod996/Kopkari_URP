using System;
using UnityEngine;

public class TargetReachEvent : MonoBehaviour
{
    public static event Action<int, bool> OnReachedTargetWithLamb;
    public static event Action OnRoundEnded;

    [SerializeField] private string requiredChildTag = "RacingHead";
    private bool triggerLocked;

    private void OnEnable()
    {
        // This component lives on the target object, which is disabled between
        // rounds. It cannot receive OnResetTarget while inactive, so every
        // activation must begin unlocked for the newly prepared round.
        ResetTrigger();
        KopkariManager.OnResetTarget += ResetTrigger;
    }

    private void OnDisable()
    {
        KopkariManager.OnResetTarget -= ResetTrigger;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerLocked || !other.CompareTag(requiredChildTag))
            return;

        Transform riderRoot = other.transform.root;
        if (riderRoot == null)
            return;

        KopkariManager manager = KopkariManager.Instance;
        if (manager == null || manager.roomState != KopkariManager.RoomState.GameStarted)
            return;

        bool isPlayer = manager.IsLocalRiderTransform(other.transform) ||
                        manager.IsLocalRiderTransform(riderRoot);
        AIKopkariRider aiRider = isPlayer
            ? null
            : (other.GetComponentInParent<AIKopkariRider>() ??
               riderRoot.GetComponentInChildren<AIKopkariRider>(true));
        if (!isPlayer && aiRider == null)
            return;
        if (!manager.IsCurrentGoatOwnerTransform(isPlayer ? other.transform : aiRider.transform))
            return;

        int riderId = isPlayer
            ? PlayerPrefs.GetInt(Constants.Player.Userid, 0)
            : aiRider.GetId();

        // Lock before callbacks so physics cannot report the same winner twice.
        triggerLocked = true;
        KopkariResultsManager.Instance?.OnFinish(riderId);
        KopkariResultsManager.Instance?.DebugLogLeaderboard();

        // Complete the AI carrier before KopkariManager handles the winner event.
        // Otherwise the manager clears ownership first and the AI's later event
        // callback sees hasLamb == false, skipping its physical Ulak drop.
        if (!isPlayer)
            aiRider.CompleteRoundAtTarget();

        OnReachedTargetWithLamb?.Invoke(riderId, isPlayer);
        OnRoundEnded?.Invoke();
    }

    public void ResetTrigger()
    {
        triggerLocked = false;
    }
}
