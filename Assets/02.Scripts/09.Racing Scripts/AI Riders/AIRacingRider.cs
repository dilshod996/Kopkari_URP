using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIRacingRider : MonoBehaviour
{
    [Header("Horse animal details")]
    [SerializeField] private MAnimal aiHorse;
    [SerializeField] private NavMeshAgent agent;
    [Header("Obstacle Details")]
    [SerializeField] private ObstacleTouchSensor sensor;
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f; // sekund davomida penalty


    [SerializeField] private int hitCount = 0;
    private bool isPenalized = false;
    private Action<GameObject> onHitHandler;
    private void Awake()
    {
        DisableNavmesh();
    }
    private void OnEnable()
    {
        if (sensor == null) return;

        // bir marta yaratamiz
        onHitHandler ??= _ => GetPenalty();

        sensor.OnObstacleHit += onHitHandler;
    }

    private void OnDisable()
    {
        if (sensor == null || onHitHandler == null) return;

        sensor.OnObstacleHit -= onHitHandler;
        StopAllCoroutines();
    }

    #region Disable and Enable Rider Navmesh
    public void DisableNavmesh()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }
    public void EnableNavmesh()
    {
        if(agent != null)
        {
            agent.isStopped = false;
        }
    }
    #endregion

    #region Rider Speed Obstacle
    public void GetPenalty()
    {
        hitCount++;
        if (hitCount >= maxHits)
        {
            hitCount = 0;
            TriggerPenalty();
        }
    }

    public void TriggerPenalty()
    {
        if (!isPenalized) StartCoroutine(ApplyPenalty());
    }

    private IEnumerator ApplyPenalty()
    {
        isPenalized = true;
        hitCount = 0;

        // Eski speed ni saqlaymiz
        aiHorse.Speed_CurrentIndex_Set(4);

        yield return new WaitForSeconds(penaltyDuration);

        // Avvalgi speedni qaytaramiz
        aiHorse.Speed_CurrentIndex_Set(5);
        isPenalized = false;
        Debug.Log($"{aiHorse.name} recovered from penalty.");
    }
    #endregion
}
