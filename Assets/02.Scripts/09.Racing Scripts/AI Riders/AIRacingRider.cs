using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class AIRacingRider : MonoBehaviour
{
    [Header("Horse animal details")]
    [SerializeField] private MAnimal aiHorse;
    [SerializeField] private NavMeshAgent agent;
    [Header("Obstacle Details")]
    [SerializeField] private ObstacleTouchSensor sensor;
    [Header("Navigation Avoidance")]
    [SerializeField] private ObstacleAvoidanceType avoidanceQuality = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    [SerializeField, Range(0, 99)] private int avoidancePriorityMin = 40;
    [SerializeField, Range(0, 99)] private int avoidancePriorityMax = 75;
    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f;
    [SerializeField] private BoostersContainer boostersContainer;
    public AIRiderRandomSkin randomSkin;

    private int hitCount;
    private bool isPenalized;
    private void Awake()
    {
        ConfigureAvoidancePriority();
        DisableNavmesh();

    }
    private void OnEnable()
    {
        if (sensor != null)
            sensor.OnTouched += OnObstacleTouched;
    }

    private void OnDisable()
    {
        if (sensor != null)
            sensor.OnTouched -= OnObstacleTouched;

        StopAllCoroutines();

        // safe reset (disable bo'lib qolsa speed 4 da qolmasin)
        isPenalized = false;
        hitCount = 0;

        if (aiHorse != null)
            aiHorse.Speed_CurrentIndex_Set(5);
    }


    #region Disable and Enable Rider Navmesh
    public void DisableNavmesh()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }
    public void DisableSpeed()
    {
        if(aiHorse != null)
        {
            aiHorse.Speed_CurrentIndex_Set(0);
        }
    }
    public void EnableNavmesh()
    {
        if(agent != null)
        {
            ConfigureAvoidancePriority();
            agent.isStopped = false;
            if (RacingController.Instance.mapType == RacingController.RacingType.Training)
            {
                aiHorse.Speed_CurrentIndex_Set(5);
            }
        }
    }
    #endregion

    #region Rider Speed Obstacle
    private void ConfigureAvoidancePriority()
    {
        if (agent == null) return;

        int minimum = Mathf.Min(avoidancePriorityMin, avoidancePriorityMax);
        int maximum = Mathf.Max(avoidancePriorityMin, avoidancePriorityMax);
        int range = maximum - minimum + 1;

        agent.obstacleAvoidanceType = avoidanceQuality;
        agent.avoidancePriority = minimum + Mathf.Abs(GetInstanceID() % range);
    }

    private void OnObstacleTouched()
    {
        boostersContainer.NotifyObstacleTouched_Npc();
    }

    #endregion

    #region Skins

    #endregion
}
