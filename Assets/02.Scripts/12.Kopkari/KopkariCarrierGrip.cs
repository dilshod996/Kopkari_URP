using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KopkariCarrierGrip : MonoBehaviour
{
    public enum DamageSource
    {
        WalkTrap,
        GuardRiderMelee,
        GuardHorseAttack,
        GuardContact,
        MainRivalSideAttack,
        TrapSetterContact,
        PlayerTouch,
        OtherRiderContact
    }

    [Header("Grip")]
    [SerializeField, Min(1f)] private float maximumGrip = 100f;
    [SerializeField, Min(0f)] private float pickupProtectionDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float damageCooldown = 1f;

    [Header("Damage Amounts")]
    [SerializeField, Min(0f)] private float walkTrapDamage = 50f;
    [SerializeField, Min(0f)] private float guardRiderMeleeDamage = 35f;
    [SerializeField, Min(0f)] private float guardHorseAttackDamage = 10f;
    [SerializeField, Min(0f)] private float guardContactDamage = 20f;
    [SerializeField, Min(0f)] private float mainRivalSideAttackDamage = 20f;
    [SerializeField, Min(0f)] private float trapSetterContactDamage = 20f;
    [SerializeField, Min(0f)] private float playerTouchDamage = 20f;
    [SerializeField, Min(0f)] private float otherRiderContactDamage = 20f;

    private readonly Dictionary<int, float> nextAllowedDamageTimes = new Dictionary<int, float>(12);
    private float protectionEndTime;
    private bool depletionRaised;

    public event Action<float, float> GripChanged;
    public event Action GripDepleted;

    public float CurrentGrip { get; private set; }
    public float MaximumGrip => Mathf.Max(1f, maximumGrip);
    public float NormalizedGrip => Mathf.Clamp01(CurrentGrip / MaximumGrip);
    public bool IsHolding { get; private set; }
    public bool IsProtected => IsHolding && Time.time < protectionEndTime;
    public GameObject OwnerRoot { get; private set; }

    public void BeginHold(GameObject ownerRoot)
    {
        OwnerRoot = ownerRoot != null ? ownerRoot.transform.root.gameObject : transform.root.gameObject;
        CurrentGrip = MaximumGrip;
        protectionEndTime = Time.time + Mathf.Max(0f, pickupProtectionDuration);
        depletionRaised = false;
        IsHolding = true;
        nextAllowedDamageTimes.Clear();
        GripChanged?.Invoke(CurrentGrip, MaximumGrip);
    }

    public void EndHold()
    {
        IsHolding = false;
        OwnerRoot = null;
        protectionEndTime = 0f;
        depletionRaised = false;
        nextAllowedDamageTimes.Clear();
    }

    public bool ApplyDamage(DamageSource source, GameObject attacker = null)
    {
        if (!IsHolding || depletionRaised || IsProtected)
            return false;

        float damage = GetDamage(source);
        if (damage <= 0f)
            return false;

        int cooldownKey = GetCooldownKey(source, attacker);
        if (nextAllowedDamageTimes.TryGetValue(cooldownKey, out float nextAllowedTime) &&
            Time.time < nextAllowedTime)
        {
            return false;
        }

        nextAllowedDamageTimes[cooldownKey] = Time.time + Mathf.Max(0.1f, damageCooldown);
        CurrentGrip = Mathf.Max(0f, CurrentGrip - damage);
        GripChanged?.Invoke(CurrentGrip, MaximumGrip);

        if (CurrentGrip <= 0f)
        {
            depletionRaised = true;
            IsHolding = false;
            GripDepleted?.Invoke();
        }

        return true;
    }

    private float GetDamage(DamageSource source)
    {
        switch (source)
        {
            case DamageSource.WalkTrap:
                return walkTrapDamage;
            case DamageSource.GuardRiderMelee:
                return guardRiderMeleeDamage;
            case DamageSource.GuardHorseAttack:
                return guardHorseAttackDamage;
            case DamageSource.GuardContact:
                return guardContactDamage;
            case DamageSource.MainRivalSideAttack:
                return mainRivalSideAttackDamage;
            case DamageSource.TrapSetterContact:
                return trapSetterContactDamage;
            case DamageSource.PlayerTouch:
                return playerTouchDamage;
            case DamageSource.OtherRiderContact:
                return otherRiderContactDamage;
            default:
                return 0f;
        }
    }

    private static int GetCooldownKey(DamageSource source, GameObject attacker)
    {
        int attackerId = attacker != null ? attacker.transform.root.gameObject.GetInstanceID() : 0;
        unchecked
        {
            return (attackerId * 397) ^ (int)source;
        }
    }
}
