using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoosterUIAnimator : MonoBehaviour
{
    // ====== STATIC EVENT ======
    public static event Action<Booster.BoosterType, Sprite> OnBoosterPicked;

    public static void RaiseBoosterPicked(Booster.BoosterType type, Sprite icon)
        => OnBoosterPicked?.Invoke(type, icon);

    // ====== UI REFS ======
    [Header("Canvas & Fly Layer")]
    [SerializeField] private Canvas canvas;                  // Screen Space Overlay bo'lsa cam=null
    [SerializeField] private RectTransform flyLayer;         // stretch fullscreen layer
    [SerializeField] private GameObject flyIconPrefabGO;     // !!! Prefab GameObject (ichida Image bor)

    [Header("Targets")]
    [SerializeField] private RectTransform hitBtn;
    [SerializeField] private RectTransform defendBtn;
    [SerializeField] private RectTransform walkZoneBtn;
    [SerializeField] private RectTransform timeBoosterBtn;
    [SerializeField] private RectTransform webSnareBtn;
    [SerializeField] private RectTransform setSpeedSprintBtn;
    [SerializeField] private RectTransform sprintFullBtn;
    [SerializeField] private RectTransform hitCounterSliderRect;
    [SerializeField] private RectTransform uloqGetSliderRect;
    [SerializeField] private RectTransform uloqTriggerPassRect;
    [SerializeField] private RectTransform speedStateIconRect;

    [Header("Text BG (optional)")]
    [SerializeField] private RectTransform hitTextBg;
    [SerializeField] private RectTransform defendTextBg;
    [SerializeField] private RectTransform walkZoneTextBg;
    [SerializeField] private RectTransform timeBoosterTextBg;
    [SerializeField] private RectTransform webSnareTextBg;
    [SerializeField] private RectTransform setSpeedSprintTextBg;
    [SerializeField] private RectTransform sprintFullTextBg;
    [SerializeField] private RectTransform hitCounterSliderTextBg;
    [SerializeField] private RectTransform uloqGetSliderTextBg;
    [SerializeField] private RectTransform uloqTriggerPassTextBg;
    [SerializeField] private RectTransform speedStateTextBg;

    [Header("Fly Start Offset (from center)")]
    [SerializeField] private Vector2 startOffset = new Vector2(0f, 120f);

    // ====== Per-Type ======
    [Header("Per-Type Fly Settings")]
    [SerializeField] private float hitDur = 0.40f;
    [SerializeField] private float hitArc = 90f;

    [SerializeField] private float defendDur = 0.50f;
    [SerializeField] private float defendArc = 140f;

    [SerializeField] private float walkZoneDur = 0.60f;
    [SerializeField] private float walkZoneArc = 170f;

    [SerializeField] private float timeBoosterDur = 0.55f;
    [SerializeField] private float timeBoosterArc = 200f;

    [SerializeField] private float webSnareDur = 0.65f;
    [SerializeField] private float webSnareArc = 230f;

    [SerializeField] private float sprintFullDur = 0.45f;
    [SerializeField] private float sprintFullArc = 120f;

    [SerializeField] private float setSpeedSprintDur = 0.42f;
    [SerializeField] private float setSpeedSprintArc = 110f;

    [SerializeField] private float sprintStartScale = 1.35f;
    [SerializeField] private float sprintEndScale = 0.75f;

    // ====== Defaults ======
    [Header("Fly Defaults")]
    [SerializeField] private float flyDuration = 0.55f;
    [SerializeField] private float arcHeight = 140f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float startScale = 1.2f;
    [SerializeField] private float endScale = 0.6f;

    [Header("Punch / Bulb")]
    [SerializeField] private float btnPunchScale = 1.12f;
    [SerializeField] private float btnPunchUpTime = 0.08f;
    [SerializeField] private float btnPunchDownTime = 0.10f;

    [SerializeField] private float bulbScale = 1.5f;
    [SerializeField] private float bulbUpTime = 0.09f;
    [SerializeField] private float bulbDownTime = 0.12f;

    // ====== Queue ======
    [Header("Queue")]
    [SerializeField] private float betweenDelay = 0.06f;
    [SerializeField] private int maxQueue = 20;

    // ====== Internal caches ======
    private Dictionary<Booster.BoosterType, RectTransform> _btnMap;
    private Dictionary<Booster.BoosterType, RectTransform> _bgMap;

    private struct FlyReq
    {
        public Booster.BoosterType type;
        public Sprite icon;
    }

    private readonly Queue<FlyReq> _queue = new(32);
    private Coroutine _queueRoutine;

    // cache (alloc yo'q)
    private WaitForSecondsRealtime _waitBetween;
    private Camera _cam;

    // fly icon cache: instance -> (RectTransform, Image)
    private readonly Dictionary<GameObject, IconRefs> _iconCache = new(16);

    private struct IconRefs
    {
        public RectTransform rt;
        public Image img;
    }

    private void Awake()
    {
        _btnMap = new Dictionary<Booster.BoosterType, RectTransform>(8)
        {
            { Booster.BoosterType.Hit, hitBtn },
            { Booster.BoosterType.Defend, defendBtn },
            { Booster.BoosterType.WalkZone, walkZoneBtn },
            { Booster.BoosterType.TimeBooster, timeBoosterBtn },
            { Booster.BoosterType.WebSnare, webSnareBtn },
            { Booster.BoosterType.SetSpeedSprint, setSpeedSprintBtn },
            { Booster.BoosterType.SprintFull, sprintFullBtn },
            { Booster.BoosterType.WallObstacle, hitCounterSliderRect },
            { Booster.BoosterType.GetUlak, uloqGetSliderRect },
            { Booster.BoosterType.TriggerPoint, uloqTriggerPassRect },
            { Booster.BoosterType.SpeedState, speedStateIconRect },
        };

        _bgMap = new Dictionary<Booster.BoosterType, RectTransform>(8)
        {
            { Booster.BoosterType.Hit, hitTextBg },
            { Booster.BoosterType.Defend, defendTextBg },
            { Booster.BoosterType.WalkZone, walkZoneTextBg },
            { Booster.BoosterType.TimeBooster, timeBoosterTextBg },
            { Booster.BoosterType.WebSnare, webSnareTextBg },
            { Booster.BoosterType.SetSpeedSprint, setSpeedSprintTextBg },
            { Booster.BoosterType.SprintFull, sprintFullTextBg },
            { Booster.BoosterType.WallObstacle, hitCounterSliderTextBg },
            { Booster.BoosterType.GetUlak, uloqGetSliderTextBg },
            { Booster.BoosterType.TriggerPoint, uloqTriggerPassTextBg },
            { Booster.BoosterType.SpeedState, speedStateTextBg },
        };

        _waitBetween = betweenDelay > 0f ? new WaitForSecondsRealtime(betweenDelay) : null;
        _cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // Poolni oldindan isitib qo'yish (GC spike bo'lmasin)
        if (flyIconPrefabGO && flyLayer)
            SimplePool.CreatePool(flyIconPrefabGO, prewarm: 6, maxSize: 20, expandable: true, parent: flyLayer);
    }

    private void OnValidate()
    {
        // Play mode¡¯da betweenDelay o¡¯zgarsa wait cache yangilansin
        if (Application.isPlaying)
            _waitBetween = betweenDelay > 0f ? new WaitForSecondsRealtime(betweenDelay) : null;
    }

    private void OnEnable()
    {
        OnBoosterPicked -= EnqueuePicked;
        OnBoosterPicked += EnqueuePicked;
    }

    private void OnDisable()
    {
        OnBoosterPicked -= EnqueuePicked;
        Cleanup();
    }

    private void OnDestroy()
    {
        OnBoosterPicked -= EnqueuePicked;
        Cleanup();
    }

    // ====== EVENT -> QUEUE ======
    private void EnqueuePicked(Booster.BoosterType type, Sprite icon)
    {
        if (!isActiveAndEnabled || icon == null) return;
        if (_queue.Count >= maxQueue) return;

        _queue.Enqueue(new FlyReq { type = type, icon = icon });

        if (_queueRoutine == null)
            _queueRoutine = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0 && isActiveAndEnabled)
        {
            var req = _queue.Dequeue();

            if (!_btnMap.TryGetValue(req.type, out var targetBtn) || targetBtn == null)
            {
                yield return null;
                continue;
            }

            GetTypeSettings(req.type, out float dur, out float arc, out float sStart, out float sEnd);

            bool done = false;

            PlayFly(req.type, req.icon, targetBtn, dur, arc, sStart, sEnd, () => done = true);

            while (!done && isActiveAndEnabled)
                yield return null;

            if (_waitBetween != null)
                yield return _waitBetween;
        }

        _queueRoutine = null;
    }

    private void GetTypeSettings(Booster.BoosterType type, out float dur, out float arc, out float sStart, out float sEnd)
    {
        dur = flyDuration;
        arc = arcHeight;
        sStart = startScale;
        sEnd = endScale;

        switch (type)
        {
            case Booster.BoosterType.Hit: dur = hitDur; arc = hitArc; break;
            case Booster.BoosterType.Defend: dur = defendDur; arc = defendArc; break;
            case Booster.BoosterType.WalkZone: dur = walkZoneDur; arc = walkZoneArc; break;
            case Booster.BoosterType.TimeBooster: dur = timeBoosterDur; arc = timeBoosterArc; break;
            case Booster.BoosterType.WebSnare: dur = webSnareDur; arc = webSnareArc; break;

            case Booster.BoosterType.SprintFull:
                dur = sprintFullDur; arc = sprintFullArc;
                sStart = sprintStartScale; sEnd = sprintEndScale;
                break;

            case Booster.BoosterType.SetSpeedSprint:
                dur = setSpeedSprintDur; arc = setSpeedSprintArc;
                break;
        }
    }

    // ====== FLY (POOL) ======
    private void PlayFly(Booster.BoosterType type, Sprite icon, RectTransform target, float dur, float arc, float sStart, float sEnd, Action onDone)
    {
        if (!flyIconPrefabGO || !flyLayer) { onDone?.Invoke(); return; }

        // Pooldan olamiz (Instantiate/Destroy yo'q)
        GameObject iconGo = SimplePool.SpawnUI(flyIconPrefabGO, flyLayer, startOffset);
        if (!iconGo) { onDone?.Invoke(); return; }

        if (!_iconCache.TryGetValue(iconGo, out var refs))
        {
            refs.rt = iconGo.transform as RectTransform;
            refs.img = iconGo.GetComponent<Image>(); // 1 marta cache
            _iconCache[iconGo] = refs;
        }

        if (refs.rt == null || refs.img == null)
        {
            SimplePool.DespawnUI(iconGo);
            onDone?.Invoke();
            return;
        }

        refs.img.raycastTarget = false;
        refs.img.sprite = icon;

        refs.rt.localScale = Vector3.one * sStart;

        Vector2 startPos = startOffset;
        Vector2 endPos = GetAnchoredPos(target);

        refs.rt.anchoredPosition = startPos;

        Vector2 mid = (startPos + endPos) * 0.5f;
        Vector2 control = mid + Vector2.up * arc;

        StartCoroutine(FlyRoutine(type, refs.rt, refs.img, iconGo, startPos, control, endPos, dur, sStart, sEnd, onDone));
    }

    private IEnumerator FlyRoutine(
        Booster.BoosterType type,
        RectTransform rt, Image img, GameObject iconGo,
        Vector2 start, Vector2 control, Vector2 end,
        float dur, float sStart, float sEnd,
        Action onDone)
    {
        float t = 0f;

        while (t < 1f)
        {
            if (!isActiveAndEnabled || iconGo == null || rt == null)
                yield break;

            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);

            float k = Mathf.Clamp01(t);
            float e = ease.Evaluate(k);

            Vector2 p1 = Vector2.Lerp(start, control, e);
            Vector2 p2 = Vector2.Lerp(control, end, e);
            rt.anchoredPosition = Vector2.Lerp(p1, p2, e);

            rt.localScale = Vector3.one * Mathf.Lerp(sStart, sEnd, e);

            yield return null;
        }

        // arrive effects
        if (_btnMap.TryGetValue(type, out var targetBtn) && targetBtn != null) Punch(targetBtn);
        if (_bgMap.TryGetValue(type, out var bg) && bg != null) Bulb(bg);

        // return to pool (Destroy yo'q)
        if (img != null) img.sprite = null;
        SimplePool.DespawnUI(iconGo);

        onDone?.Invoke();
    }

    private Vector2 GetAnchoredPos(RectTransform anyRect)
    {
        Vector3 worldCenter = anyRect.TransformPoint(anyRect.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_cam, worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            flyLayer, screen, _cam, out Vector2 localPoint);

        return localPoint;
    }

    // ====== EFFECTS ======
    private void Punch(RectTransform target)
    {
        if (target == null) return;

        LeanTween.cancel(target);
        target.localScale = Vector3.one;

        LeanTween.scale(target, Vector3.one * btnPunchScale, btnPunchUpTime)
            .setIgnoreTimeScale(true)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                if (target == null) return;

                LeanTween.scale(target, Vector3.one, btnPunchDownTime)
                    .setIgnoreTimeScale(true)
                    .setEase(LeanTweenType.easeOutQuad);
            });
    }

    private void Bulb(RectTransform bg)
    {
        if (bg == null) return;

        LeanTween.cancel(bg);
        bg.localScale = Vector3.one;

        LeanTween.scale(bg, Vector3.one * bulbScale, bulbUpTime)
            .setIgnoreTimeScale(true)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                if (bg == null) return;

                LeanTween.scale(bg, Vector3.one, bulbDownTime)
                    .setIgnoreTimeScale(true)
                    .setEase(LeanTweenType.easeOutQuad);
            });
    }

    // ====== CLEANUP ======
    private void Cleanup()
    {
        StopAllCoroutines();
        _queue.Clear();
        _queueRoutine = null;

        // Tweens cancel + scale reset (faqat shu elementlar)
        ResetScaleSafe(hitBtn);
        ResetScaleSafe(defendBtn);
        ResetScaleSafe(walkZoneBtn);
        ResetScaleSafe(timeBoosterBtn);
        ResetScaleSafe(webSnareBtn);
        ResetScaleSafe(setSpeedSprintBtn);
        ResetScaleSafe(sprintFullBtn);

        ResetScaleSafe(hitTextBg);
        ResetScaleSafe(defendTextBg);
        ResetScaleSafe(walkZoneTextBg);
        ResetScaleSafe(timeBoosterTextBg);
        ResetScaleSafe(webSnareTextBg);
        ResetScaleSafe(setSpeedSprintTextBg);
        ResetScaleSafe(sprintFullTextBg);

        // Agar ichkarida pooldan olingan iconlar qolib ketgan bo'lsa:
        if (flyIconPrefabGO) SimplePool.DespawnAll(flyIconPrefabGO);
    }

    private void ResetScaleSafe(RectTransform rt)
    {
        if (rt == null) return;
        LeanTween.cancel(rt);
        rt.localScale = Vector3.one;
    }
}
