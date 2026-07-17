using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class UIGetLamp : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage; // Image: Filled Vertical, origin Bottom
    [SerializeField] private GameObject fillImgBg;

    [Header("Pickup Settings")]
    [SerializeField] private float holdTime = 5f;   // 0→100% to‘lish vaqti
    [SerializeField] private float decayTime = 3f;  // 100→0% tushish vaqti
    [SerializeField] private bool resetAfterPerform = true;

    [Header("Competitive Pickup (Player Only)")]
    [SerializeField] private bool useCompetitivePickup = true;
    [SerializeField, Min(0.1f)] private float nearbyRiderRadius = 6f;
    [SerializeField, Min(0.1f)] private float nearbyRiderCheckInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float uncontestedHoldTime = 1.5f;
    [SerializeField, Min(0.1f)] private float lightlyContestedHoldTime = 2.5f;
    [SerializeField, Min(0.1f)] private float contestedHoldTime = 3.5f;
    [SerializeField, Min(0.1f)] private float heavilyContestedHoldTime = 4.5f;
    [SerializeField, Min(0.1f)] private float focusLostResetTime = 2f;

    private bool isHolding;
    private float progress01;              // 0..1
    private Coroutine runningCR;
    private float buildRate;               // 1/holdTime (precomputed)
    private float decayRate;               // 1/decayTime (precomputed)
    private float currentCompetitiveHoldTime;
    private float nextNearbyRiderCheckTime;
    private int trackedPointerId = int.MinValue;
    private bool isTrackingPointer;
    [SerializeField] private Sprite uloqHeadSprite;


    public static Action OnPlayerGotLamp;
    private void OnEnable()
    {
        buildRate = (holdTime > 0f) ? 1f / holdTime : 999f;
        decayRate = (decayTime > 0f) ? 1f / decayTime : 0f;
    }


    private void Update()
    {
        // The pickup button is disabled when Malbers loses focus, so it cannot
        // receive PointerUp. Keep tracking the original pointer here and forget
        // it as soon as the player actually releases it.
        if (isTrackingPointer && !isHolding && !IsTrackedPointerHeld())
            ClearTrackedPointer();
    }

    public void BeginHold()
    {
        BeginHold(-1);
    }

    public void BeginHold(int pointerId)
    {
        trackedPointerId = pointerId;
        isTrackingPointer = true;
        BeginOrResumeHold();
    }

    private void BeginOrResumeHold()
    {
        KopkariMainUI.Instance?.DisableWebSnare();
        isHolding = true;
        RefreshCompetitiveHoldTime(true);

        if (fillImage)
        {
            // agar ilgari 0 bo‘lgan bo‘lsa, yangi jarayonni 0 dan boshlatamiz
            if (progress01 <= 0.0001f)
                progress01 = 0f;

            fillImage.fillAmount = progress01;
            fillImage.gameObject.SetActive(true);
            fillImgBg.SetActive(true);
        }

        StopRunning();
        runningCR = StartCoroutine(HoldRoutine());

        //BaseManager.Instance.CurrentCondition = BaseManager.PlayerCondition.GettingTarget;
    }


    public void EndHold()
    {
        EndHold(trackedPointerId);
    }

    public void EndHold(int pointerId)
    {
        if (isTrackingPointer && pointerId != trackedPointerId)
            return;

        ClearTrackedPointer();
        isHolding = false;
        StopRunning();

        if (decayRate > 0f && progress01 > 0f)
            runningCR = StartCoroutine(DecayRoutine());
        else
            TryHideWhenEmpty();
    }

    /// <summary>
    /// Called when the Malbers focus event hides the pickup button. Unlike a
    /// normal pointer release, focus loss clears stored progress in two seconds.
    /// </summary>
    public void FocusLost()
    {
        isHolding = false;
        StopRunning();

        if (progress01 > 0f)
            runningCR = StartCoroutine(FocusLostResetRoutine());
        else
            TryHideWhenEmpty();
    }

    /// <summary>
    /// Called when the Malbers focus event shows the pickup button again. If the
    /// same physical pointer is still held, stop the focus-loss decay and resume
    /// from the remaining progress without requiring another PointerDown.
    /// </summary>
    public void FocusReturned()
    {
        if (!isTrackingPointer)
            return;

        if (!IsTrackedPointerHeld())
        {
            ClearTrackedPointer();
            return;
        }

        BeginOrResumeHold();
    }

    /// <summary>
    /// Immediately clears pickup input and progress when gameplay makes the
    /// Ulak unavailable (round finish, timeout, warmup or result page).
    /// </summary>
    public void CancelImmediately()
    {
        isHolding = false;
        ClearTrackedPointer();
        StopRunning();
        progress01 = 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.gameObject.SetActive(false);
        }

        if (fillImgBg != null)
            fillImgBg.SetActive(false);
    }

    private IEnumerator HoldRoutine()
    {
        while (isHolding && progress01 < 1f)
        {
            RefreshCompetitiveHoldTime(false);
            float activeBuildRate = useCompetitivePickup
                ? 1f / Mathf.Max(0.1f, currentCompetitiveHoldTime)
                : buildRate;
            progress01 += activeBuildRate * Time.deltaTime;
            if (fillImage) fillImage.fillAmount = progress01;
            yield return null;
        }

        if (progress01 >= 1f)
        {
            PerformAction();

            if (resetAfterPerform)
            {
                progress01 = 0f;
                if (fillImage)
                {
                    fillImage.fillAmount = 0f;
                    fillImage.gameObject.SetActive(false);
                    fillImgBg.SetActive(false);
                }
                isHolding = false;
            }
        }

        runningCR = null;
    }

    private IEnumerator DecayRoutine()
    {
        while (!isHolding && progress01 > 0f)
        {
            progress01 -= decayRate * Time.deltaTime;
            if (fillImage) fillImage.fillAmount = progress01;
            yield return null;
        }

        TryHideWhenEmpty();
        runningCR = null;
    }

    private IEnumerator FocusLostResetRoutine()
    {
        float startProgress = progress01;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, focusLostResetTime);

        while (!isHolding && elapsed < duration && progress01 > 0f)
        {
            elapsed += Time.deltaTime;
            progress01 = Mathf.Lerp(startProgress, 0f, Mathf.Clamp01(elapsed / duration));
            if (fillImage) fillImage.fillAmount = progress01;
            yield return null;
        }

        if (!isHolding)
        {
            progress01 = 0f;
            if (fillImage) fillImage.fillAmount = 0f;
            TryHideWhenEmpty();
        }

        runningCR = null;
    }

    private void RefreshCompetitiveHoldTime(bool force)
    {
        if (!useCompetitivePickup)
            return;

        if (!force && Time.unscaledTime < nextNearbyRiderCheckTime)
            return;

        nextNearbyRiderCheckTime = Time.unscaledTime + Mathf.Max(0.1f, nearbyRiderCheckInterval);
        KopkariManager manager = KopkariManager.Instance;
        Vector3 pickupPosition = manager != null && manager.pickableObj != null
            ? manager.pickableObj.transform.position
            : transform.position;
        int nearbyRiders = AIKopkariRider.CountActiveRidersNear(pickupPosition, nearbyRiderRadius);

        if (nearbyRiders <= 0)
            currentCompetitiveHoldTime = uncontestedHoldTime;
        else if (nearbyRiders <= 2)
            currentCompetitiveHoldTime = lightlyContestedHoldTime;
        else if (nearbyRiders <= 5)
            currentCompetitiveHoldTime = contestedHoldTime;
        else
            currentCompetitiveHoldTime = heavilyContestedHoldTime;
    }

    private void TryHideWhenEmpty()
    {
        if (fillImage && progress01 <= 0.0001f)
        {
            fillImage.gameObject.SetActive(false);
            fillImgBg.SetActive(false);
        }
            
    }

    private void PerformAction()
    {
        KopkariManager.Instance.LambOwner = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        BoosterUIAnimator.RaiseBoosterPicked(
            Booster.BoosterType.GetUlak,
            uloqHeadSprite // icon sprite
        );
        OnPlayerGotLamp?.Invoke();
        Debug.Log("✅ Uloq olindi!");
    }

    private void StopRunning()
    {
        if (runningCR != null)
        {
            StopCoroutine(runningCR);
            runningCR = null;
        }
    }

    private bool IsTrackedPointerHeld()
    {
        if (!isTrackingPointer)
            return false;

        // Pointer.current covers the primary touchscreen contact as well as the
        // mouse under the project's Input System UI module.
        return Pointer.current != null && Pointer.current.press.isPressed;
    }

    private void ClearTrackedPointer()
    {
        isTrackingPointer = false;
        trackedPointerId = int.MinValue;
    }

    private void OnDisable()
    {
        isHolding = false;
        ClearTrackedPointer();
        StopRunning();
    }
}
