using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

public class ReverseWarningUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform reversePanel;
    [SerializeField] private TMP_Text reverseTimeText;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float panelShownY = -165f;
    [SerializeField] private float panelHiddenY = 150f;

    [Header("Reverse Settings")]
    [SerializeField] private float reverseGraceTime = 5f;
    [SerializeField] private float uiTick = 0.2f;

    public Action OnHidePanelAndDisqualify;
    public Action OnReverseStarted;
    public Action OnReverseCleared;

    private Coroutine reverseCo;
    private float tLeft;
    private bool reverseActive;
    private Tween panelTween;
    private bool isActive=false;

    public static event Action OnTimerShowed;

    // ✅ NEW: External UI-only method
    // Har safar chaqirilsa: hammasini stop qilib qaytadan show qiladi
    public void ShowPanel(float timer)
    {
        if (!reversePanel) return;
        if (reverseTimeText)
            reverseTimeText.text = $"{timer:0}";
        if (isActive) { return; }
        isActive = true;

        // hamma eski jarayonlarni to‘xtatamiz (tween + coroutine)
        StopAllReverseUIProcesses();

        // panelni qaytadan ko‘rsatamiz
        reversePanel.gameObject.SetActive(true);

        // start pos: hidden
        reversePanel.anchoredPosition =
            new Vector2(reversePanel.anchoredPosition.x, panelHiddenY);

        // text
        if (reverseTimeText)
            reverseTimeText.text = $"{timer:0}";

        // slide in
        panelTween = reversePanel
            .DOAnchorPosY(panelShownY, slideDuration)
            .SetEase(Ease.OutCubic).SetUpdate(true);
        Debug.Log("Showed " + timer);
        OnTimerShowed?.Invoke();
    }
    public void HidePanelNotTimeBased()
    {
        isActive = false;
        StopAllReverseUIProcesses();
        HidePanel();
    }

    // ✅ Helper: coroutine+tween stop
    private void StopAllReverseUIProcesses()
    {
        // kill tween
        panelTween?.Kill();
        panelTween = null;

        // stop coroutine
        if (reverseCo != null)
        {
            StopCoroutine(reverseCo);
            reverseCo = null;
        }
    }

    #region Existing Reverse Logic (optional)

    public void StartReverse()
    {
        tLeft = reverseGraceTime;

        if (!reverseActive)
        {
            reverseActive = true;
            OnReverseStarted?.Invoke();

            // shu yerda ham restart show ishlatamiz
            ShowPanel(tLeft);

            if (reverseCo != null)
                StopCoroutine(reverseCo);

            reverseCo = StartCoroutine(ReverseCountdown());
        }
        else
        {
            // aktiv bo‘lsa ham UI restart + text refresh
            ShowPanel(tLeft);
        }
    }

    public void ClearReverse()
    {
        if (!reverseActive) return;

        reverseActive = false;
        OnReverseCleared?.Invoke();

        StopAllReverseUIProcesses();
        HidePanel();
    }

    public void HidePanelAndDisqualify()
    {
        StopAllReverseUIProcesses();
        reverseActive = false;

        HidePanel(() =>
        {
            OnHidePanelAndDisqualify?.Invoke();
        });
    }

    private void HidePanel(Action onComplete = null)
    {
        if (!reversePanel) return;

        panelTween?.Kill();

        panelTween = reversePanel
            .DOAnchorPosY(panelHiddenY, slideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                if (reverseTimeText) reverseTimeText.text = "";
                reversePanel.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    private IEnumerator ReverseCountdown()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(uiTick);

        while (reverseActive && tLeft > 0f)
        {
            // UI ni ham ShowPanel orqali yangilab turamiz (restart emas, faqat text kerak bo‘lsa alohida method ham qilamiz)
            if (reverseTimeText) reverseTimeText.text = $"{Mathf.CeilToInt(tLeft)}";
            tLeft -= uiTick;
            yield return wait;
        }

        reverseCo = null;

        if (reverseActive)
            HidePanelAndDisqualify();
    }

    #endregion
}
