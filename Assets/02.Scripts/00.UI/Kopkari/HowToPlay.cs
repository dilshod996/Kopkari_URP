using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlay : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text backBtnText;

    [Header("Buttons")]
    [SerializeField] private Button topUloqDistance;
    [SerializeField] private Button topTimerDistance;
    [SerializeField] private Button topLeftUloqCheckPoint;
    [SerializeField] private Button rightSprint;
    [SerializeField] private Button rightHitCounter;
    [SerializeField] private Button rightUloqTimer;
    [SerializeField] private Button moveJoystick;
    [SerializeField] private Button moveCamera;
    [SerializeField] private Button sprint;
    [SerializeField] private Button cameraBack;
    [SerializeField] private Button jump;
    [SerializeField] private Button defense;
    [SerializeField] private Button walkZone;
    [SerializeField] private Button webSnare;
    [SerializeField] private Button qamchi;
    [SerializeField] private Button pushRiders;

    [Header("Pulse Animation")]
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;
    [SerializeField] private float pulseSpeed = 5f; // katta bo'lsa tezroq "pulse"

    private Button _currentBtn;
    private RectTransform _currentRT;
    private Coroutine _pulseCo;

    private void OnEnable()
    {
        // default text (markazda)
        description.text = LanguageManager.Instance?.GetText(345);
        backBtnText.text = LanguageManager.Instance?.GetText(362);

        // Listenerlar (har bir button uchun descId berasan)
        // ⚠️ descId larni sen o'zingning LanguageManager table'ingga moslab qo'yasan
        Register(topUloqDistance, 346);
        Register(topTimerDistance, 347);
        Register(topLeftUloqCheckPoint, 348);
        Register(rightSprint, 349);
        Register(rightHitCounter, 350);
        Register(rightUloqTimer, 351);
        Register(moveJoystick, 352);
        Register(moveCamera, 361);
        Register(sprint, 353);
        Register(cameraBack, 354);
        Register(jump, 355);
        Register(defense, 356);
        Register(walkZone, 357);
        Register(webSnare, 358);
        Register(qamchi, 359);
        Register(pushRiders, 360);
    }

    private void OnDisable()
    {
        // Tozalash (takror OnEnable bo‘lganda listenerlar ko‘payib ketmasin)
        UnregisterAll();
        StopPulseAndReset();
    }

    private void Register(Button btn, int descId)
    {
        if (!btn) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnButtonClicked(btn, descId));
    }

    private void OnButtonClicked(Button btn, int descId)
    {
        // Text update
        description.text = LanguageManager.Instance?.GetText(descId) ?? "";

        // Oldingini reset qilamiz
        StopPulseAndReset();

        // Yangisini pulse qilamiz
        _currentBtn = btn;
        _currentRT = btn.transform as RectTransform;
        if (_currentRT != null)
            _pulseCo = StartCoroutine(Pulse(_currentRT));
    }

    private IEnumerator Pulse(RectTransform rt)
    {
        // PingPong orqali 0.9 - 1.1 oralig'ida scale
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * pulseSpeed;
            float s = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(t, 1f));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private void StopPulseAndReset()
    {
        if (_pulseCo != null)
        {
            StopCoroutine(_pulseCo);
            _pulseCo = null;
        }

        if (_currentRT != null)
            _currentRT.localScale = Vector3.one;

        _currentBtn = null;
        _currentRT = null;
    }

    private void UnregisterAll()
    {
        SafeClear(topUloqDistance);
        SafeClear(topTimerDistance);
        SafeClear(topLeftUloqCheckPoint);
        SafeClear(rightSprint);
        SafeClear(rightHitCounter);
        SafeClear(rightUloqTimer);
        SafeClear(moveJoystick);
        SafeClear(sprint);
        SafeClear(cameraBack);
        SafeClear(jump);
        SafeClear(defense);
        SafeClear(walkZone);
        SafeClear(webSnare);
        SafeClear(qamchi);
        SafeClear(pushRiders);
        SafeClear(moveCamera);
    }

    private void SafeClear(Button btn)
    {
        if (btn) btn.onClick.RemoveAllListeners();
    }
}
