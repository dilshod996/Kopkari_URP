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
    [Header("Refs")]
    [SerializeField] private MAnimal horseAnimal;
    [SerializeField] private ObstacleTouchSensor obstacleSensor;
    [SerializeField] private BoostersContainer playerBooster;

    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f;

    private int hitCount;
    private bool isPenalized;

    [Header("Speed Improver")]
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;
    [Header("Majburiy start nuqtasi")]
    public Transform targetPoint;
    public float requiredRadius = 2f;
    public float maxTime = 0f;

    private bool reached = false;
    private bool eliminated = false;
    private Coroutine checkCoroutine;

    public static Action OnReachedStartTarget;
    public static Action OnObstacleTouchedEvent;
    private void OnEnable()
    {
        if (obstacleSensor != null)
            obstacleSensor.OnTouched += OnObstacleTouched;

    }

    private void OnDisable()
    {
        if (obstacleSensor != null)
            obstacleSensor.OnTouched -= OnObstacleTouched;
    }

    #region Penalty Section

    private void OnObstacleTouched()
    {
        if (isPenalized) return;

        OnObstacleTouchedEvent?.Invoke();
        //UIButtonActions.Instance.PlayShock();

        if (playerBooster != null)
            playerBooster.NotifyObstacleTouched();
    }


    #endregion

    #region Legacy Starting Point
    public void SetLegacyStartPoint(Transform point, float time)
    {
        targetPoint = point;
        maxTime = time;
    }

    public void BeginCheck()
    {
        if (checkCoroutine != null)
            StopCoroutine(checkCoroutine);

        reached = false;
        eliminated = false;
        checkCoroutine = StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        float timeLeft = maxTime;
        Transform riderTransform = transform;

        while (timeLeft > 0f && !reached)
        {
            timeLeft -= Time.deltaTime;

            if (targetPoint != null &&
                Vector3.Distance(riderTransform.position, targetPoint.position) <= requiredRadius)
            {
                reached = true;
                OnReachedStartTarget?.Invoke();
                yield break;
            }

            yield return null;
        }

        if (!reached)
            eliminated = true;
    }
    #endregion
}
