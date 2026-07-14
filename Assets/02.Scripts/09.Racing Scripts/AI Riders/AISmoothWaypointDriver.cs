using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations;

public class AISmoothWaypointDriver : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] MWayPoint currentWP;

    [Header("Steer & Move")]
    [SerializeField] float turnSpeedDeg = 150f;   // 100–180°/s — tabiiy
    [SerializeField] float maxSpeed = 8f;
    [SerializeField] float accelLerp = 4f;     // tezlik silliqligi

    [Header("LookAhead")]
    [SerializeField] float lookAheadDistAtLow = 2f;
    [SerializeField] float lookAheadDistAtHigh = 6f;
    [SerializeField] float highSpeedThreshold = 7f;

    [Header("Cheap Racing Avoidance")]
    [SerializeField] bool useRacingAvoidance = true;
    [SerializeField] bool avoidLocalPlayer = true;
    [SerializeField] float avoidanceRadius = 3f;
    [SerializeField] float avoidanceStrength = 2f;
    [SerializeField] float maxAvoidanceOffset = 2.5f;
    [SerializeField] float avoidanceScanInterval = 0.18f;
    [SerializeField] float avoidanceSmoothing = 8f;

    Vector3 vel;
    RacingAgent ownAgent;
    Vector3 avoidanceOffset;
    Vector3 targetAvoidanceOffset;
    float nextAvoidanceScanTime;
    float preferredAvoidanceSide = 1f;

    void Awake()
    {
        CacheOwnAgent();
        preferredAvoidanceSide = (GetInstanceID() & 1) == 0 ? 1f : -1f;
    }

    void Update()
    {
        if (!currentWP) return;

        // 1) Waypoint radiuslari
        float stopR = currentWP.StopDistance();
        float slowR = Mathf.Max(currentWP.SlowDistance(), stopR + 0.01f);

        // 2) Keyingi WP ni olamiz (bo‘lmasa o‘zini davom ettiramiz)
        Transform nextT = null;
        List<Transform> nexts = currentWP.NextTargets;
        if (nexts != null && nexts.Count > 0)
        {
            // Masalan, birinchisini oling yoki o‘zingizga mos tanlash
            nextT = nexts[0];
            if (nextT && !nextT.gameObject.activeInHierarchy) nextT = null;
        }

        // 3) Look-ahead nuqta (hozirgi→keyingi segment bo‘ylab oldinga qarash)
        Vector3 steerTarget = currentWP.GetCenterPosition();
        if (nextT)
        {
            Vector3 a = currentWP.GetCenterPosition();
            Vector3 b = nextT.position;
            a.y = b.y = transform.position.y;

            Vector3 seg = (b - a);
            float segLen = seg.magnitude;
            if (segLen > 0.001f)
            {
                Vector3 segN = seg / segLen;
                Vector3 toSelf = (transform.position - a);
                float t = Mathf.Clamp01(Vector3.Dot(toSelf, segN) / segLen);

                // Dinamik lookahead: tezlik oshgan sari uzoqroqqa qarasin
                float speed = vel.magnitude;
                float la = Mathf.Lerp(lookAheadDistAtLow, lookAheadDistAtHigh,
                                      Mathf.InverseLerp(0f, highSpeedThreshold, speed));

                Vector3 along = a + segN * (t * segLen);
                steerTarget = along + segN * la; // oldinga siljitilgan nuqta
            }
        }

        ApplyRacingAvoidance(ref steerTarget);

        // 4) Silliq rotatsiya
        Vector3 dir = (steerTarget - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float newYaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, turnSpeedDeg * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
        }

        // 5) Tezlikni radiuslarga qarab silliq boshqarish
        float distToWP = Vector3.Distance(transform.position, currentWP.GetCenterPosition());
        float desiredSpd = maxSpeed;

        if (distToWP <= slowR) // sekinlashish zonasi
        {
            // stopR da 0 tezlik, slowR’da maxSpeed bo‘lsin
            float k = Mathf.InverseLerp(stopR, slowR, distToWP);
            desiredSpd = Mathf.Lerp(0f, maxSpeed, k);
        }

        Vector3 desiredVel = transform.forward * desiredSpd;
        vel = Vector3.Lerp(vel, desiredVel, accelLerp * Time.deltaTime);

        transform.position += vel * Time.deltaTime;

        // 6) WP ga yetganda keyingisiga o'tish
        if (distToWP <= stopR)
        {
            currentWP.TargetArrived(gameObject);
            var nxt = currentWP.NextTarget(); // Malbers’dagi tayyor funksiya
            if (nxt) currentWP = nxt.GetComponent<MWayPoint>();
        }
    }

    // Tashqi koddan joriy WP ni set qilish uchun
    public void SetWaypoint(MWayPoint wp) => currentWP = wp;

    private void ApplyRacingAvoidance(ref Vector3 steerTarget)
    {
        if (!useRacingAvoidance || avoidanceRadius <= 0f || avoidanceStrength <= 0f)
        {
            avoidanceOffset = Vector3.zero;
            targetAvoidanceOffset = Vector3.zero;
            return;
        }

        if (Time.time >= nextAvoidanceScanTime)
        {
            nextAvoidanceScanTime = Time.time + Mathf.Max(0.05f, avoidanceScanInterval);
            targetAvoidanceOffset = CalculateAvoidanceOffset(steerTarget);
        }

        float lerp = 1f - Mathf.Exp(-avoidanceSmoothing * Time.deltaTime);
        avoidanceOffset = Vector3.Lerp(avoidanceOffset, targetAvoidanceOffset, lerp);

        if (avoidanceOffset.sqrMagnitude > 0.0001f)
            steerTarget += avoidanceOffset;
    }

    private Vector3 CalculateAvoidanceOffset(Vector3 steerTarget)
    {
        RacingController controller = RacingController.Instance;
        if (controller == null)
            return Vector3.zero;

        if (ownAgent == null)
            CacheOwnAgent();

        IReadOnlyList<RacingAgent> agents = controller.AllAgents;
        if (agents == null || agents.Count <= 1)
            return Vector3.zero;

        Vector3 selfPosition = transform.position;
        Vector3 routeForward = steerTarget - selfPosition;
        routeForward.y = 0f;

        if (routeForward.sqrMagnitude < 0.0001f)
        {
            routeForward = transform.forward;
            routeForward.y = 0f;
        }

        if (routeForward.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        routeForward.Normalize();
        Vector3 routeRight = Vector3.Cross(Vector3.up, routeForward);

        float radiusSqr = avoidanceRadius * avoidanceRadius;
        Vector3 separation = Vector3.zero;

        for (int i = 0; i < agents.Count; i++)
        {
            RacingAgent other = agents[i];
            if (!ShouldAvoid(other))
                continue;

            Vector3 delta = selfPosition - other.transform.position;
            delta.y = 0f;

            float distSqr = delta.sqrMagnitude;
            if (distSqr < 0.0001f || distSqr > radiusSqr)
                continue;

            float dist = Mathf.Sqrt(distSqr);
            float weight = 1f - (dist / avoidanceRadius);
            float side = Vector3.Dot(delta, routeRight);

            if (Mathf.Abs(side) < 0.15f)
                side = GetPairAvoidanceSide(other);

            separation += routeRight * Mathf.Sign(side) * weight;
        }

        if (separation.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return Vector3.ClampMagnitude(separation * avoidanceStrength, maxAvoidanceOffset);
    }

    private bool ShouldAvoid(RacingAgent other)
    {
        if (other == null || other.HasFinished || !other.gameObject.activeInHierarchy)
            return false;

        if (ownAgent != null && other == ownAgent)
            return false;

        if (!avoidLocalPlayer && other.isPlayer)
            return false;

        return !other.transform.IsChildOf(transform);
    }

    private float GetPairAvoidanceSide(RacingAgent other)
    {
        if (other == null)
            return preferredAvoidanceSide;

        int selfId = ownAgent != null ? ownAgent.GetInstanceID() : GetInstanceID();
        return selfId < other.GetInstanceID() ? preferredAvoidanceSide : -preferredAvoidanceSide;
    }

    private void CacheOwnAgent()
    {
        ownAgent = GetComponentInParent<RacingAgent>();
        if (ownAgent == null)
            ownAgent = GetComponentInChildren<RacingAgent>();
    }
}
