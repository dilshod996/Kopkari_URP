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

    Vector3 vel;

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
}
