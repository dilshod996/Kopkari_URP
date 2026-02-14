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
    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f;
    [SerializeField] private BoostersContainer boostersContainer;
    public AIRiderRandomSkin randomSkin;

    private int hitCount;
    private bool isPenalized;
    private void Awake()
    {
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
    public void EnableNavmesh()
    {
        if(agent != null)
        {
            agent.isStopped = false;
        }
    }
    #endregion

    #region Rider Speed Obstacle
    private void OnObstacleTouched()
    {
        boostersContainer.NotifyObstacleTouched_Npc();
    }

    #endregion

    #region Skins

    #endregion
}
