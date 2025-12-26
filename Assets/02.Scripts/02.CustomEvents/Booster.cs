using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Booster : MonoBehaviour
{
    public enum BoosterType
    {
        SprintFull,
        SetSpeedSprint,
        Defend,
        WalkZone,
        TimeBooster,
        Hit,
        WebSnare,
        //obstacles
        WallObstacle,
        GetUlak,
        TriggerPoint
    }

    [Header("Booster")]
    public BoosterType boosterType;

    [Header("Refs")]
    [SerializeField] private GameObject visuals;
    private Collider triggerCol;

    [Header("Pickup Feedback (Player Only)")]
    [SerializeField] private GameObject pickupVfxPrefab; // universal VFX
    [SerializeField] private Color pickupColor = Color.white;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float vfxYOffset = 0.15f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.7f;

    [SerializeField] private Sprite boosterIcon;

    public static Action OnSprintFull;

    private bool picked;

    private void Awake()
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

        var boosters = other.GetComponentInChildren<BoostersContainer>();
        if (boosters == null) return;

        picked = true;

        if (isPlayer)
            PlayPickupFeedback();

        ApplyEffect(boosters, isPlayer);

        if (triggerCol) triggerCol.enabled = false;
        if (visuals) visuals.SetActive(false);
        SimplePool.Despawn(gameObject);
    }

    private void PlayPickupFeedback()
    {
        // VFX
        if (pickupVfxPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * vfxYOffset;
            var vfxGo = SimplePool.Spawn(pickupVfxPrefab, pos, Quaternion.identity);

            // VFX prefab ichida PickupVfxColor bo‘lishi kerak
            var colorSetter = vfxGo.GetComponent<PickupVfxColor>();
            if (colorSetter != null)
                colorSetter.SetColor(pickupColor);
        }

        // SFX
        if (pickupSfx != null)
        {
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);
        }
        BoosterUIAnimator.RaiseBoosterPicked(boosterType, boosterIcon);

    }

    private void ApplyEffect(BoostersContainer target, bool isPlayer)
    {
        switch (boosterType)
        {
            case BoosterType.SprintFull:
                if (isPlayer) OnSprintFull?.Invoke();
                else target.TriggerBoostSpeed();
                break;

            case BoosterType.SetSpeedSprint:
                target.TriggerBoostSpeed();
                break;

            case BoosterType.Defend:
                target.AddDefend();
                break;

            case BoosterType.WalkZone:
                if (isPlayer) target.AddWalkZone();
                break;

            case BoosterType.TimeBooster:
                break;

            case BoosterType.Hit:
                if (isPlayer) target.AddHit();
                break;

            case BoosterType.WebSnare:
                target.AddWebSnare();
                break;
        }
    }
}
