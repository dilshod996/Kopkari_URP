using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Constants;

/// <summary>
/// Bu script Playerning Otiga tegishli bolgan main scripti
/// </summary>
public class HorseMine : MonoBehaviour
{
    public static HorseMine Instance { get; protected set; }

    [Header("Horse ning animation componenti")]
    public MAnimal horseAnimal;

    [Header("Racing ga tegishli bo'lgan componentlar")]
    [SerializeField] private ObstacleTouchSensor obstacleSensor;
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f; // sekund davomida penalty


    [SerializeField] private int hitCount = 0;
    private bool isPenalized = false;
    private Action<GameObject> onHitHandler;

    [Header("Speed Improver")]
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    private void OnEnable()
    {
        if (obstacleSensor == null) return;

        // bir marta yaratamiz
        onHitHandler ??= _ => GetPenalty();

        obstacleSensor.OnObstacleHit += onHitHandler;
    }

    private void OnDisable()
    {
        if (obstacleSensor == null || onHitHandler == null) return;

        obstacleSensor.OnObstacleHit -= onHitHandler;
    }
    #region Penalty Section

    public void GetPenalty()
    {
        hitCount++;

        UIButtonActions.Instance.PlayShock();
        if (hitCount >= maxHits)
        {
            hitCount = 0;
            TriggerPenalty();
        }
    }

    public void TriggerPenalty()
    {
        if (!isPenalized)
        {
            StartCoroutine(ApplyPenalty());
            UIButtonActions.Instance.PlaySlow();
        }
    }

    private IEnumerator ApplyPenalty()
    {
        isPenalized = true;
        hitCount = 0;

        // Eski speed ni saqlaymiz
        horseAnimal.Speed_CurrentIndex_Set(4);

        Debug.Log($"{horseAnimal.name} penalized! Speed reduced to Gallop for {penaltyDuration} seconds.");

        yield return new WaitForSeconds(penaltyDuration);

        // Avvalgi speedni qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(5);
        isPenalized = false;
        Debug.Log($"{horseAnimal.name} recovered from penalty.");
        UIButtonActions.Instance.SliderValueRestore();
    }
    #endregion

    #region Boost Speed

    public void TriggerBoostSpeed()
    {
        if (!maxSpeed)
        {
            StartCoroutine(ImproveSpeed());
        }
    }
    private IEnumerator ImproveSpeed()
    {
        maxSpeed = true;

        // Eski speed ni saqlaymiz
        horseAnimal.Speed_CurrentIndex_Set(6);

        yield return new WaitForSeconds(maxSpeedDuration);

        // Avvalgi speedni qaytaramiz
        horseAnimal.Speed_CurrentIndex_Set(5);
        maxSpeed = false;
        //Debug.Log($"{horseAnimal.name} recovered from penalty.");
    }
    #endregion
}
