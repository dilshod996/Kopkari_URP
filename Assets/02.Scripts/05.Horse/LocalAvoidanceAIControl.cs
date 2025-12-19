using UnityEngine;
using MalbersAnimations.Controller.AI;

public class LocalAvoidanceAIControl : MAnimalAIControl
{
    [Header("Local Avoidance")]
    [SerializeField] public bool useLocalAvoidance = true;
    [SerializeField] private float avoidanceRadius = 3f;
    [SerializeField] private float avoidanceStrength = 2f;
    [SerializeField] private LayerMask avoidanceLayers;

    // Inspector¡¯da ishlashini istasang, public property ham berib qo¡®yish mumkin:
    public bool UseLocalAvoidance
    {
        get => useLocalAvoidance;
        set => useLocalAvoidance = value;
    }

    public float AvoidanceRadius
    {
        get => avoidanceRadius;
        set => avoidanceRadius = value;
    }

    public float AvoidanceStrength
    {
        get => avoidanceStrength;
        set => avoidanceStrength = value;
    }

    /// <summary>
    /// Asosiy yerda harakat qilishdan oldin avoidance qo¡®shamiz
    /// </summary>
    public override void Move()
    {
        if (useLocalAvoidance)
            ApplyLocalAvoidance();

        // Bu bazadagi Move, animal.Move(AIDirection * SlowMultiplier) ni qiladi
        base.Move();
    }

    /// <summary>
    /// FreeMove (Fly va h.k) uchun ham avoidance
    /// </summary>
    protected override void FreeMovement()
    {
        if (!HasArrived)
        {
            AIDirection = (DestinationPosition - animal.transform.position);
            SetRemainingDistance(AIDirection.magnitude);

            AIDirection = AIDirection.normalized * SlowMultiplier;

            if (useLocalAvoidance)
                ApplyLocalAvoidance();

            animal.Move(AIDirection);
            Arrive_Destination();
        }
    }

    /// <summary>
    /// Yaqin atrofdagi boshqa riderlardan chetga og¡®ish
    /// </summary>
    private void ApplyLocalAvoidance()
    {
        if (avoidanceRadius <= 0f) return;
        if (AIDirection == Vector3.zero) return;

        Vector3 separation = Vector3.zero;
        int count = 0;

        // Agent markazidan kichik radiusda tekshiramiz
        Collider[] hits = Physics.OverlapSphere(AgentTransform.position, avoidanceRadius, avoidanceLayers);

        foreach (var h in hits)
        {
            // O¡®zimizni e¡¯tiborga olmaymiz
            if (h.transform == this.transform) continue;

            // Faqat boshqa AI riderlar
            var otherAI = h.GetComponentInParent<MAnimalAIControl>();
            if (otherAI == null) continue;

            Vector3 diff = AgentTransform.position - h.transform.position;
            float dist = diff.magnitude;
            if (dist < 0.01f) continue;

            separation += diff.normalized / dist;
            count++;
        }

        if (count > 0)
        {
            separation /= count;
            separation.y = 0f;

            if (separation.sqrMagnitude > 0.0001f)
            {
                Vector3 desired = AIDirection + separation * avoidanceStrength;
                if (desired.sqrMagnitude > 0.0001f)
                    AIDirection = desired.normalized;
            }
        }
    }
}
