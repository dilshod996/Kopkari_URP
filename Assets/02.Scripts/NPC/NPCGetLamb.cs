using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using MalbersAnimations.Events;
using System.Collections;
using TMPro;
using UnityEngine;
using VLB;

public class NPCGetLamb : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MPickUp pickUp;
    [SerializeField] private MAIState goToFinishState;
    [SerializeField] private MAIState goToLambState;
    [SerializeField] private MAIState reachedTargetState;
    [SerializeField] private MAnimalBrain brain;

    [Header("Settings")]
    [SerializeField] private float waitToPickUp = 10f;
    [SerializeField] private float itemPickedDuration = 20f;

    [Header("LambParenObj")]
    [SerializeField] private GameObject lambParentObj; // Parent object for the lamb

    private Coroutine waitCoroutine;
    private Coroutine itemTimerCoroutine;

    private bool isItemPicked = false;
    private float currentItemTime;

    [Header("Checkpoints Route")]
    [SerializeField] private CheckpointTrigger[] checkpointsRoute;   // AI shu tartibda boradi
    [SerializeField] private Transform finishTarget;                 // yakuniy finish nuqta

    private int currentCheckpointIndex = -1;


    private void OnDisable()
    {
        StopAllCoroutines();
    }
    private void OnEnable()
    {

    }
    public void OnEnterEvent()
    {
        if (pickUp != null && pickUp.FocusedItem != null && !pickUp.Has_Item && waitCoroutine == null)
        {
            waitCoroutine = StartCoroutine(WaitToPickUpLamb());
        }
    }

    public void OnExitEvent()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }

    private IEnumerator WaitToPickUpLamb()
    {

        yield return new WaitForSeconds(waitToPickUp);

        if (pickUp != null && !pickUp.Has_Item && pickUp.FocusedItem != null)
        {
            pickUp.PickUpItem();
            if (lambParentObj.transform.childCount > 0)
            {
                BaseManager.Instance.LambOwner = gameObject.name;
                BaseManager.Instance.CurrentCondition = BaseManager.PlayerCondition.TakenTargetOthers;

                isItemPicked = true;
                currentItemTime = itemPickedDuration;

                // ✅ AI ham uloq egasi sifatida ro‘yxatdan o‘tadi
                if (BaseManager.Instance != null)
                {
                    BaseManager.Instance.NotifyGoatOwner(transform.root.gameObject, true);
                }

#if UNITY_EDITOR
                Debug.Log($"[NPCGetLamb] Picked up item: {gameObject.name}");
#endif

                // Oldingi: to‘g‘ri finishga
                // goToFinishState?.Play(brain);

                // Yangi: checkpointlardan keyin finishga
                StartRouteToCheckpoints();

                StartItemTimer();
            }
            else
            {
                Debug.LogWarning("[NPCGetLamb] No lambs available to pick up.");
            }
        }


        waitCoroutine = null;
    }
    private void StartRouteToCheckpoints()
    {
        if (brain == null)
            return;

        currentCheckpointIndex = 0;
        MoveToNextPoint();
    }

    private void MoveToNextPoint()
    {
        if (brain == null)
            return;

        // Hali checkpointlar qolganmi?
        if (checkpointsRoute != null && currentCheckpointIndex >= 0 && currentCheckpointIndex < checkpointsRoute.Length)
        {
            var cp = checkpointsRoute[currentCheckpointIndex];
            if (cp != null)
            {
                // Malbers Brain targetini shu checkpointga o‘rnatamiz
               // brain.SetTarget(cp.transform);
                goToFinishState?.Play(brain); // Bu state "borish" state bo‘lsa bas
            }
        }
        else
        {
            // Barcha checkpoint tugadi → endi finishga
            if (finishTarget != null)
            {
                //brain.SetTarget(finishTarget);
                goToFinishState?.Play(brain);
            }
        }
    }

    private void StartItemTimer()
    {
        if (itemTimerCoroutine == null)
        {
            itemTimerCoroutine = StartCoroutine(ItemPickedCountdown());
        }
    }

    private void StopItemTimer()
    {
        if (itemTimerCoroutine != null)
        {
            StopCoroutine(itemTimerCoroutine);
            itemTimerCoroutine = null;
        }
    }

    private IEnumerator ItemPickedCountdown()
    {
        bool walkZoneDropped = false; // har coroutine uchun yangi flag
        while (currentItemTime > 0 && isItemPicked)
        {
            yield return new WaitForSeconds(1f);
            currentItemTime -= 1f;
            if (!walkZoneDropped && currentItemTime <= itemPickedDuration - 4f)
            {
                walkZoneDropped = true;     // faqat shu coroutine ichida 1 marta
            }
        }

        if (currentItemTime <= 0f && isItemPicked)
        {
            DropItemAndRetry();
        }

        itemTimerCoroutine = null;
    }

    private void DropItemAndRetry()
    {
        isItemPicked = false;
        if(BaseManager.Instance.currentCondition == BaseManager.PlayerCondition.TakenTargetOthers)
        {
            pickUp?.DropItem();
        }
        if(BaseManager.Instance.currentCondition != BaseManager.PlayerCondition.LoserSession)
        {
            BaseManager.Instance.currentCondition = BaseManager.PlayerCondition.None;
        }
        

#if UNITY_EDITOR
        Debug.Log("[NPCGetLamb] Item dropped due to timeout!");
#endif

        goToLambState?.Play(brain);
        StopItemTimer();
    }

    public void ReachTarget()
    {
        reachedTargetState?.Play(brain);
    }
}
