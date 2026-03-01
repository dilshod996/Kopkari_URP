using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MalbersAnimations;
using MalbersAnimations.Controller; // MAnimal

public class GoatDistanceUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform root;   // panel (icon + text)
    [SerializeField] private Image goatIcon;
    [SerializeField] private TMP_Text metersText;

    [Header("Targets")]
    [SerializeField] private Transform goatTransform; // uloq (serializefield)

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private bool horizontalOnly = true;
    [SerializeField] private float nearMeters = 15f;
    [SerializeField] private float hideWhenVeryClose = 3f;

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private float pulseScaleMax = 1.08f;
    [SerializeField] private float pulseTime = 0.6f;

    [SerializeField] Transform _player;   // rider transform (eventdan keladi)
    bool _visible;
    float _nextUpdate;

    int _showTweenId = -1;
    int _pulseTweenId = -1;

    CanvasGroup _cg;
    [Header("Distance Rules")]
    [SerializeField] private float warnMeters = 80f;
    [SerializeField] private float gameOverMeters = 100f;
    [SerializeField] private float warnResetMeters = 75f;

    bool warnShown;
    bool gameOverTriggered;


    void Awake()
    {
        if (root == null) root = GetComponent<RectTransform>();

        _cg = root.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = root.gameObject.AddComponent<CanvasGroup>();

        // start hidden
        root.localScale = Vector3.one * 0.85f;
        _cg.alpha = 0f;
    }

    void OnEnable()
    {
        // SENING event: 1=horse, 2=player(rider)
        KopkariManager.OnHorseTransform += OnSpawnedRiderAndHorse; // BaseManager nomi o'zingda qanaqa bo'lsa o'sha
    }

    void OnDisable()
    {
        KopkariManager.OnHorseTransform -= OnSpawnedRiderAndHorse;
    }

    private void OnSpawnedRiderAndHorse(Transform playerTransform)
    {
        _player = playerTransform;

        // Ikkalasi ham bor bo‘lsa — ko‘rsat
        if (_player != null && goatTransform != null)
            Show();
        else
            HideImmediate();
    }


    void Update()
    {
        if (!_visible) return;
        if (_player == null || goatTransform == null) { Hide(); return; }

        if (Time.unscaledTime < _nextUpdate) return;
        _nextUpdate = Time.unscaledTime + updateInterval;

        float dist = GetDistanceMeters(_player.position, goatTransform.position);
        int meters = Mathf.Max(0, Mathf.RoundToInt(dist));
        metersText.text = meters + " m";

        if (dist <= nearMeters) StartPulse();
        else StopPulse();
        // OUT OF KOPKARI flag
        // WARNING
        if (dist >= warnMeters && dist < gameOverMeters)
        {
            if (!warnShown)
            {
                warnShown = true;
                ShowWarning();
            }
        }
        else if (dist <= warnResetMeters)
        {
            warnShown = false;
        }

        // GAME OVER
        if (dist >= gameOverMeters && !gameOverTriggered)
        {
            gameOverTriggered = true;
            TriggerGameOver();
        }


    }
    void ShowWarning()
    {
        KopkariManager.Instance?.speechBubble.ShowPopup("You are out of Kopkari! Return to the goat!");
        // UI popup yoki HUD text
        //Debug.Log("You are out of Kopkari! Return to the goat!");
    }

    void TriggerGameOver()
    {
        Debug.Log("GAME OVER: Out of Kopkari");

        // O'yinni to'xtatish game over page di chaqirish shu yerda

    }

    float GetDistanceMeters(Vector3 a, Vector3 b)
    {
        if (horizontalOnly)
        {
            a.y = 0f;
            b.y = 0f;
        }
        return Vector3.Distance(a, b);
    }

    // -------------------------
    // Public API: o'yin logikangdan chaqirasan
    // -------------------------
    public void SetGoat(Transform goat)
    {
        goatTransform = goat;
    }
    public void SHowHide(bool state)
    {
        if (state)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    public void Show()
    {
        Debug.Log("show it");
        if (_visible) return;

        if (_player == null || goatTransform == null)
            return;

        _visible = true;
        Debug.Log("show it 2");
        root.gameObject.SetActive(true);

        KillTween(ref _showTweenId);

        root.localScale = Vector3.one * 0.85f;
        _cg.alpha = 0f;

        _showTweenId = LeanTween.scale(root, Vector3.one, showDuration)
            .setEaseOutBack()
            .setIgnoreTimeScale(true)
            .id;

        LeanTween.value(root.gameObject, 0f, 1f, showDuration)
            .setIgnoreTimeScale(true)
            .setOnUpdate(a => _cg.alpha = a);
    }

    public void Hide()
    {
        if (!_visible) return;
        _visible = false;

        StopPulse();
        KillTween(ref _showTweenId);

        LeanTween.scale(root, Vector3.one * 0.9f, hideDuration)
            .setEaseInBack()
            .setIgnoreTimeScale(true);

        LeanTween.value(root.gameObject, _cg.alpha, 0f, hideDuration)
            .setIgnoreTimeScale(true)
            .setOnUpdate(a => _cg.alpha = a)
            .setOnComplete(() =>
            {
                if (!_visible) root.gameObject.SetActive(false);
            });
    }

    void HideImmediate()
    {
        _visible = false;
        StopPulse();
        KillTween(ref _showTweenId);
        root.gameObject.SetActive(false);
        root.localScale = Vector3.one;
        _cg.alpha = 0f;
    }

    void StartPulse()
    {
        if (_pulseTweenId != -1) return;

        _pulseTweenId = LeanTween.scale(root, Vector3.one * pulseScaleMax, pulseTime * 0.5f)
            .setEaseInOutSine()
            .setLoopPingPong()
            .setIgnoreTimeScale(true)
            .id;
    }

    void StopPulse()
    {
        KillTween(ref _pulseTweenId);
        root.localScale = Vector3.one;
    }

    void KillTween(ref int id)
    {
        if (id == -1) return;
        LeanTween.cancel(id);
        id = -1;
    }
    public void ForceHide()
    {
        // Barcha flaglarni reset
        _visible = false;
        warnShown = false;
        gameOverTriggered = false;

        // Tweenlarni to‘liq o‘ldiramiz
        StopPulse();
        KillTween(ref _showTweenId);
        KillTween(ref _pulseTweenId);

        // LeanTween bilan bog‘liq bo‘lgan hammasini bekor qilish (root uchun)
        LeanTween.cancel(root.gameObject);

        // UI ni darhol yashirish
        root.localScale = Vector3.one;
        _cg.alpha = 0f;
        root.gameObject.SetActive(false);
    }

}
