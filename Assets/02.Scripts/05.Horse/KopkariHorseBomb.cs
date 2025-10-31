using UnityEngine;
using System.Collections;

public enum BombForceMode { Impulse, VelocityChange }

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Rigidbody))]
public class KopkariHorseBomb : MonoBehaviour
{
    [Header("Explosion")]
    public float radius = 25f;
    public float force = 50f;
    [Tooltip("Planar rejimda e'tiborsiz. Faqat 3D portlashda ishlatiladi.")]
    public float upwardsModifier = 0.0f;
    public BombForceMode forceMode = BombForceMode.VelocityChange;
    public int sustainFrames = 3;
    public float maxSpeedCap = 0f; // 0 = cheklama yo‘q

    [Header("Behaviour")]
    public bool ignoreSelf = true;
    public bool horizontalOnly = true;        // ✅ planar (XZ) rejim
    public bool useCOMForTargets = false;     // rb.position / worldCenterOfMass
    public float cooldown = 2.0f;

    [Header("Center Override")]
    public Transform centerOverride;          // bomb markazi
    public bool useCOMForSelf = true;

    [Header("AI Auto Use")]
    public bool aiAutoUse = false;
    public float aiFirstDelay = 2f;
    public Vector2 aiRepeatDelay = new Vector2(6f, 10f);

    [Header("FX")]
    public VFXPool explosionPool;
    public AudioClip sfxClip;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    [Header("VFX Scale and Time")]
    public float dTime = 2f;
    public float dScale = 2.3f;

    [Header("Debug")]
    public bool debugLog = true;
    public bool debugDraw = true;
    public float debugDrawTime = 0.6f;

    Rigidbody _selfRB;
    Transform _root;
    bool _onCD;

    [Header("Repel Timing")]
    [Tooltip("Necha soniya davomida itarish bo'lib boradi")]
    public float repelDuration = 1.0f;              // 👈 1s davomida
    [Tooltip("Kuchni vaqt bo'yicha taqsimlash og'irligi. 1->0 ease-out default.")]
    public AnimationCurve repelCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // Batch uchun kichik konteyner (alloclarni kamaytirish)
    struct Hit { public Rigidbody rb; public Vector3 baseVel; }
    readonly System.Collections.Generic.List<Hit> _hitBatch = new System.Collections.Generic.List<Hit>(64);


    void Awake()
    {
        _selfRB = GetComponent<Rigidbody>();
        _root = transform.root;
        
    }
    private void Start()
    {
        if (explosionPool == null && BaseManager.Instance != null)
        {
            explosionPool = BaseManager.Instance.pool;
        }
    }
    void OnEnable()
    {
        if (aiAutoUse) StartCoroutine(AutoLoop());
    }

    void OnDisable() { StopAllCoroutines(); }

    // UI button
    public void ActivateHere()
    {
        Vector3 pos = GetBombCenter();
        TryExplode(pos);
    }
    public void ActivateAt(Vector3 worldPos) => TryExplode(worldPos);

    Vector3 GetBombCenter()
    {
        if (centerOverride) return centerOverride.position;
        if (useCOMForSelf && _selfRB) return _selfRB.worldCenterOfMass;
        return transform.position; // fallback
    }

    IEnumerator AutoLoop()
    {
        if (aiFirstDelay > 0f) yield return new WaitForSeconds(aiFirstDelay);
        while (aiAutoUse)
        {
            ActivateHere();
            float wait = Mathf.Max(0.1f, Random.Range(aiRepeatDelay.x, aiRepeatDelay.y));
            yield return new WaitForSeconds(wait);
        }
    }

    void TryExplode(Vector3 pos)
    {
        int purged = HorsePhysicsRegistry.CleanupNulls();
        int alive = HorsePhysicsRegistry.NonNullCount();
        if (debugLog) Debug.Log($"[Bomb] Center={pos} | Registry: Count={HorsePhysicsRegistry.Count}, NonNull={alive}, Purged={purged}");

        if (_onCD || alive == 0) return;
        StartCoroutine(DoExplosion(pos));
    }

    IEnumerator DoExplosion(Vector3 pos)
    {
        _onCD = true;

        Explode(pos);
        if (sfxClip) AudioSource.PlayClipAtPoint(sfxClip, pos, sfxVolume);

        //int candidates = 0, hitCount = 0;

        //HorsePhysicsRegistry.ForEach((rb) =>
        //{
        //    if (!rb) return;
        //    if (ignoreSelf && rb == _selfRB) return;

        //    // Target markazi
        //    Vector3 center = useCOMForTargets ? rb.worldCenterOfMass : rb.position;

        //    // ✅ masofa faqat XZ bo'yicha
        //    Vector3 d = center - pos;
        //    if (horizontalOnly) d.y = 0f;
        //    float dist = d.magnitude;
        //    candidates++;
        //    if (dist > radius) return;

        //    // ✅ yo'nalish faqat XZ (vertikal qo'shilmaydi)
        //    Vector3 dir = dist > 0.0001f ? (d / dist) : Vector3.zero;

        //    // falloff
        //    float t = Mathf.Clamp01(1f - (dist / radius));
        //    float scaled = Mathf.Lerp(force * 0.5f, force, t);

        //    if (forceMode == BombForceMode.VelocityChange)
        //    {
        //        // ✅ massdan mustaqil, planar velocity-change
        //        Vector3 perFrameVel = dir * (scaled / Mathf.Max(1, sustainFrames));
        //        StartCoroutine(ApplyVelChangeOverFrames(rb, perFrameVel, sustainFrames, maxSpeedCap));
        //    }
        //    else // BombForceMode.Impulse
        //    {
        //        if (horizontalOnly)
        //        {
        //            // ✅ radial yo'nalishni tekislash: markazning Y'i target bilan bir xil
        //            Vector3 posPlanar = new Vector3(pos.x, center.y, pos.z);
        //            rb.AddExplosionForce(scaled, posPlanar, radius, 0f, ForceMode.Impulse);
        //        }
        //        else
        //        {
        //            rb.AddExplosionForce(scaled, pos, radius, upwardsModifier, ForceMode.Impulse);
        //        }
        //    }

        //    if (debugDraw) Debug.DrawLine(pos, center, Color.cyan, debugDrawTime);
        //    if (debugLog) Debug.Log($"[Bomb] HIT {rb.name} distXZ={dist:0.0} scaled={scaled:0.0}");
        //    hitCount++;
        //});

        //if (debugLog) Debug.Log($"[Bomb] Candidates {candidates}, Hit {hitCount} / {HorsePhysicsRegistry.NonNullCount()} (radius={radius})");

        //if (cooldown > 0f) yield return new WaitForSeconds(cooldown);
        int candidates = 0, hitCount = 0;
        _hitBatch.Clear();

        HorsePhysicsRegistry.ForEach((rb) =>
        {
            if (!rb) return;
            if (ignoreSelf && rb == _selfRB) return;

            Vector3 center = useCOMForTargets ? rb.worldCenterOfMass : rb.position;

            // ✅ masofa faqat XZ
            Vector3 d = center - pos;
            if (horizontalOnly) d.y = 0f;
            float dist = d.magnitude;
            candidates++;
            if (dist > radius) return;

            // ✅ yo'nalish faqat XZ
            Vector3 dir = dist > 0.0001f ? (d / dist) : Vector3.zero;

            // falloff
            float t = Mathf.Clamp01(1f - (dist / radius));
            float scaled = Mathf.Lerp(force * 0.5f, force, t);

            if (forceMode == BombForceMode.VelocityChange)
            {
                // Bir kadrda emas, vaqtga taqsimlaymiz:
                Vector3 baseVel = dir * scaled;     // umumiy berilishi kerak bo'lgan Δv vektori (planar)
                _hitBatch.Add(new Hit { rb = rb, baseVel = baseVel });

                if (debugDraw) Debug.DrawLine(pos, center, Color.cyan, debugDrawTime);
                if (debugLog) Debug.Log($"[Bomb] HIT {rb.name} distXZ={dist:0.0} scaled={scaled:0.0}");
                hitCount++;
            }
            else // Impulse
            {
                if (horizontalOnly)
                {
                    Vector3 posPlanar = new Vector3(pos.x, center.y, pos.z);
                    rb.AddExplosionForce(scaled, posPlanar, radius, 0f, ForceMode.Impulse);
                }
                else
                {
                    rb.AddExplosionForce(scaled, pos, radius, upwardsModifier, ForceMode.Impulse);
                }

                if (debugDraw) Debug.DrawLine(pos, center, Color.cyan, debugDrawTime);
                if (debugLog) Debug.Log($"[Bomb] HIT {rb.name} distXZ={dist:0.0} scaled={scaled:0.0}");
                hitCount++;
            }
        });

        if (debugLog) Debug.Log($"[Bomb] Candidates {candidates}, Hit {hitCount} / {HorsePhysicsRegistry.NonNullCount()} (radius={radius})");

        // 🔥 VelocityChange bo'lsa — bitta batch coroutine 1s davomida qo'llaydi
        if (forceMode == BombForceMode.VelocityChange && _hitBatch.Count > 0)
        {
            yield return StartCoroutine(ApplyBatchVelOverTime(_hitBatch, repelDuration, repelCurve, maxSpeedCap));
        }
        _onCD = false;
    }

    IEnumerator ApplyVelChangeOverFrames(Rigidbody rb, Vector3 velChangePerFrame, int frames, float cap)
    {
        // ✅ Y komponent umuman berilmasin
        velChangePerFrame.y = 0f;

        for (int i = 0; i < frames; i++)
        {
            rb.AddForce(velChangePerFrame, ForceMode.VelocityChange);

            if (cap > 0f && rb.velocity.sqrMagnitude > cap * cap)
                rb.velocity = rb.velocity.normalized * cap;

            yield return new WaitForFixedUpdate();
        }
    }
    IEnumerator ApplyBatchVelOverTime(System.Collections.Generic.List<Hit> hits, float duration, AnimationCurve curve, float cap)
    {
        // FixedUpdate'lar soni: 1s / fixedDeltaTime
        int frames = Mathf.Max(2, Mathf.CeilToInt(duration / Time.fixedDeltaTime));

        // Oldindan vaznlar yig'indisini hisoblaymiz, shunda ∑step = baseVel
        float sum = 0f;
        float[] weights = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = (i + 0.5f) / frames;      // 0..1 (o‘rtadagi sample)
            float w = Mathf.Max(0f, curve.Evaluate(t));
            weights[i] = w;
            sum += w;
        }
        if (sum <= 0f) sum = frames; // agar curve 0 bo‘lsa, tekis taqsimlaymiz (fallback)

        for (int f = 0; f < frames; f++)
        {
            float wNorm = weights[f] / sum;

            for (int i = 0; i < hits.Count; i++)
            {
                var h = hits[i];
                if (!h.rb) continue;

                // Har frame'dagi Δv (planar).
                Vector3 dv = h.baseVel * wNorm;
                dv.y = 0f; // ✅ Y komponent yo‘q

                h.rb.AddForce(dv, ForceMode.VelocityChange);

                if (cap > 0f && h.rb.velocity.sqrMagnitude > cap * cap)
                    h.rb.velocity = h.rb.velocity.normalized * cap;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void Explode(Vector3 position)
    {
        // Instantiate+Destroy o‘rniga:
        //explosionPool.PlayAt(position, Quaternion.identity);
        explosionPool.PlayAt(position, Quaternion.identity, dScale, dTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 p = centerOverride ? centerOverride.position : transform.position;
        // Planar halqa
        const int steps = 48;
        Vector3 prev = p + new Vector3(radius, 0, 0);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.95f);
        for (int i = 1; i <= steps; i++)
        {
            float ang = (i / (float)steps) * Mathf.PI * 2f;
            Vector3 q = p + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, q);
            prev = q;
        }
    }
#endif
}
