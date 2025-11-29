using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Booster : MonoBehaviour
{
    public enum BoosterType { SprintFull, SetSpeedSprint, Defend, WalkZone, TimeBooster, Hit }

    [Header("Booster")]
    public BoosterType boosterType;

    [Header("Refs (optional)")]
    private Collider triggerCol;
    [SerializeField] private GameObject visuals;
    public static Action OnSprintFull;   // Event

    private bool picked;
    private void Reset()
    {
        triggerCol = GetComponent<Collider>();
        triggerCol.isTrigger = true;
    }

    private void OnEnable()
    {
        picked = false;
        if (triggerCol) triggerCol.enabled = true;
        if (visuals) visuals.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;

        bool isPlayer = other.CompareTag("Player");
        bool isNpc = other.CompareTag("NPC");
        if (!isPlayer && !isNpc) return;

        //boostertype qilib bir korib chiqish kerakda delegate action qilib
        var boosters = other.GetComponentInChildren<BoostersContainer>();
        if (boosters == null) return;

        picked = true;

        // 1) Effektni beramiz (Player/NPC farqlab)
        ApplyEffect(boosters, isPlayer);


        // 3) Darhol despawn (Destroy emas)
        if (triggerCol) triggerCol.enabled = false;
        if (visuals) visuals.SetActive(false);
        SimplePool.Despawn(gameObject);
    }

    private void ApplyEffect(BoostersContainer target, bool isPlayer)
    {
        switch (boosterType)
        {
            case BoosterType.SprintFull:
                if (isPlayer) OnSprintFull?.Invoke();   // 🔥 Sprintni to‘liq to‘ldir degan signaltarget.SprintStatFull();
                else target.TriggerBoostSpeed();
                break;

            case BoosterType.SetSpeedSprint:
                if (isPlayer) target.TriggerBoostSpeed();
                else target.TriggerBoostSpeed();
                break;

            case BoosterType.Defend:
                if (isPlayer) target.AddDefend();
                else target.AddDefend();
                break;

            case BoosterType.WalkZone:
                if (isPlayer) target.AddWalkZone(); // NPCga kerak bo‘lmasa qoldiramiz
                break;

            case BoosterType.TimeBooster:
               // if (isPlayer) target.addt(+5f);
                break;

            case BoosterType.Hit:
                if (isPlayer) target.AddHit();
               // else target.NpcMinorBuff(2f);
                break;
        }
    }

}
