using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using MalbersAnimations.Events;
using System.Collections;
using TMPro;
using UnityEngine;
using VLB;
using static BaseManager;

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

    public bool TestMode = false; // Optional for debugging

    [SerializeField] private TMP_Text nameText;
    //[SerializeField] private TMP_Text walkZoneText;
    //[SerializeField] private TMP_Text defendText;

    [Header("Effect To Other Players")]


    public AttackDefendManager attackDefendManager; // Reference to AttackDefendManager for dropping traps
    
    private void OnDisable()
    {
        StopAllCoroutines();
        //if (attackDefendManager != null)
        //{
        //    attackDefendManager.OnWalkZoneAdded -= UpdateWalkZone;
        //    attackDefendManager.OnWalkZoneRemoved -= UpdateWalkZone;
        //    attackDefendManager.OnDefendAdded -= UpdateDefend;
        //    attackDefendManager.OnDefendRemoved -= UpdateDefend;
        //}
    }
    private void OnEnable()
    {
        if (nameText != null)
        {
            nameText.text = gameObject.name;
        }
        //if(attackDefendManager != null)
        //{
        //    //walkZoneText.text = attackDefendManager.walkZoneCount.ToString();
        //    //defendText.text = attackDefendManager.defendCount.ToString();
        //    //attackDefendManager.OnWalkZoneAdded += UpdateWalkZone;
        //    //attackDefendManager.OnWalkZoneRemoved += UpdateWalkZone;
        //    //attackDefendManager.OnDefendAdded += UpdateDefend;
        //    //attackDefendManager.OnDefendRemoved += UpdateDefend;
        //}

    }
    //public void UpdateWalkZone()
    //{
    //    walkZoneText.text = attackDefendManager.walkZoneCount.ToString();
    //}
    //public void UpdateDefend()
    //{
    //    defendText.text = attackDefendManager.defendCount.ToString();
    //}
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
        TestMode = true;

        yield return new WaitForSeconds(waitToPickUp);

        if (pickUp != null && !pickUp.Has_Item && pickUp.FocusedItem != null)
        {
            pickUp.PickUpItem();
            if (lambParentObj.transform.childCount > 0)
            {
                BaseManager.Instance.LambOwner = gameObject.name;
                BaseManager.Instance.CurrentCondition = PlayerCondition.TakenTargetOthers;

                isItemPicked = true;
                currentItemTime = itemPickedDuration;

                goToFinishState?.Play(brain);

#if UNITY_EDITOR
                Debug.Log($"[NPCGetLamb] Picked up item: {gameObject.name}");
#endif

                StartItemTimer();
            }
            else
            {
                Debug.LogWarning("[NPCGetLamb] No lambs available to pick up.");
            }

        }

        waitCoroutine = null;
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
                DropWalkTrap();               // ✅ 1 marta trap tashlaydi
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
        if(BaseManager.Instance.currentCondition == PlayerCondition.TakenTargetOthers)
        {
            pickUp?.DropItem();
        }
        if(BaseManager.Instance.currentCondition != PlayerCondition.LoserSession)
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

    #region Effect To Other Players
    public void DropWalkTrap()
    {
        attackDefendManager.DropWalkTrapNpc();
    }
    #endregion

    #region Defend

    #endregion
}
