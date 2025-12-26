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

    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float penaltyDuration = 10f;

    private int hitCount;
    private bool isPenalized;

    [Header("Speed Improver")]
    [SerializeField] private bool maxSpeed = false;
    [SerializeField] private float maxSpeedDuration = 5f;
    [Header("Majburiy start nuqtasi")]
    public Transform targetPoint;      // Rider borishi kerak bo'lgan joy
    public float requiredRadius = 2f;  // Qancha yaqin bo'lsa "bordim" deb hisoblanadi
    public float maxTime = 0f;        // Necha sekund ichida yetib borishi shart

    private bool reached = false;
    private bool eliminated = false;
    private Coroutine checkCoroutine;

    public static Action OnReachedStartTarget;
    private void OnEnable()
    {
        if (obstacleSensor != null)
            obstacleSensor.OnTouched += OnObstacleTouched;

        BaseManager.OnGameStarted += BeginCheck;
        BaseManager.OnStartPoint += StartPoint;
        if (BaseManager.CurrentStartPoint != null)
        {
            StartPoint(BaseManager.CurrentStartPoint, BaseManager.CurrentWarmupTime);
        }
    }

    private void OnDisable()
    {
        if (obstacleSensor != null)
            obstacleSensor.OnTouched -= OnObstacleTouched;
        BaseManager.OnGameStarted -= BeginCheck;
        BaseManager.OnStartPoint -= StartPoint;
    }

    #region Penalty Section

    private void OnObstacleTouched()
    {
        if (isPenalized) return;

        hitCount++;
        Sprite sprite;
        if(UIButtonActions.Instance!=null)
            sprite = UIButtonActions.Instance.obstacleHitSprite;
        else
            sprite = KopkariMainUI.Instance.obstacleSprite;
        // 🎯 UI BOOSTER ANIM TRIGGER
        BoosterUIAnimator.RaiseBoosterPicked(
            Booster.BoosterType.WallObstacle,
            sprite // icon sprite
        );

        UIButtonActions.Instance.PlayShock();

        if (hitCount >= maxHits)
        {
            hitCount = 0;
            StartCoroutine(ApplyPenalty());
        }
    }

    private IEnumerator ApplyPenalty()
    {
        isPenalized = true;

        horseAnimal.Speed_CurrentIndex_Set(4);
        UIButtonActions.Instance.PlaySlow();

        yield return new WaitForSeconds(penaltyDuration);

        horseAnimal.Speed_CurrentIndex_Set(5);
        isPenalized = false;
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

    #region Starting Point
    private void StartPoint(Transform point, float time)
    {
        targetPoint = point;
        maxTime = time;

        Debug.Log($"{name} start point oldi: {point.position}, warmup: {time}");

        // Shu yerda warmup logikasini boshlasang bo‘ladi:
        // masalan:
        // StartCoroutine(WarmUpRoutine());
    }
    public void BeginCheck()
    {
        // Har ehtimolga qarshi eski coroutine bo‘lsa – to‘xtatamiz
        if (checkCoroutine != null)
            StopCoroutine(checkCoroutine);

        reached = false;
        eliminated = false;

        checkCoroutine = StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        float timeLeft = maxTime;

        // Riderning o‘zi (odatda root transform)
        Transform riderTransform = transform;

        while (timeLeft > 0f && !reached)
        {
            timeLeft -= Time.deltaTime;

            if (targetPoint != null)
            {
                float dist = Vector3.Distance(riderTransform.position, targetPoint.position);

                // Yetarlicha yaqinlashgan bo‘lsa – success
                if (dist <= requiredRadius)
                {
                    reached = true;
                    OnReachedStartPoint();
                    yield break;
                }
            }

            yield return null;
        }

        // Vaqt tugab bo‘ldi, lekin yetib bormagan bo‘lsa → o‘yindan chetlatamiz
        if (!reached)
        {
            EliminateRider();
        }
    }

    private void OnReachedStartPoint()
    {
        OnReachedStartTarget?.Invoke();
        Debug.Log($"{name} start nuqtasiga yetib bordi ✅");
        // Hohlasang shu yerda:
        // - AIga "normal race logic"ni yoqasan
        // - Idle/ready animatsiyasini qo'yasan
    }

    private void EliminateRider()
    {
        if (eliminated) return;
        eliminated = true;

        Debug.Log($"{name} start nuqtasiga yetib bormadi, o‘yindan chetlatildi ❌");

        // Bu yerda riderni o‘chirib tashlaysan yoki DQ qilasan:
        // 1) Player bo'lsa – controlni o'chirish:
        //    playerController.enabled = false;
        // 2) AI bo'lsa – AIAgent/MAnimal harakatini o'chirish:
        //    ai.enabled = false;  yoki  animal.LockMovement(true);
        // 3) Hohlasang "DQ" popap, effekt, text va hokazo
    }
    #endregion
}
