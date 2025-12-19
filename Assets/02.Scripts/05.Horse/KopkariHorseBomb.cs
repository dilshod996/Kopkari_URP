using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-50)]
public class KopkariHorseBomb : MonoBehaviour
{
    [Header("Logical Repel Settings")]
    [Tooltip("Otlarni itarish radiusi (XZ bo'yicha).")]
    public float radius = 20f;

    [Tooltip("Har bir ot necha birlik masofaga suriladi.")]
    public float logicalPushDistance = 5f;

    [Tooltip("Itarish animatsiyasi qancha davom etadi (sekund).")]
    public float repelDuration = 0.35f;

    [Tooltip("Tugma bosgan otni o'zini ignore qilish.")]
    public bool ignoreSelf = true;

    [Tooltip("Planar (faqat XZ) masofa hisoblash.")]
    public bool horizontalOnly = true;

    [Tooltip("0..1 bo'yicha vaqtga qarab itarish kuchi.")]
    public AnimationCurve repelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Center Override")]
    [Tooltip("Agar bomb markazini boshqa objectdan olmoqchi bo'lsang.")]
    public Transform centerOverride;

    [Header("Cooldown")]
    public float cooldown = 2f;

    [Header("FX")]
    public VFXPool explosionPool;
    public AudioClip sfxClip;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    public float dTime = 2f;
    public float dScale = 2.3f;

    [Header("AI Auto Use (ixtiyoriy)")]
    public bool aiAutoUse = false;
    public float aiFirstDelay = 2f;
    public Vector2 aiRepeatDelay = new Vector2(6f, 10f);

    Transform _root;
    bool _onCD;
    [Header("FX Offset")]
    [SerializeField] private float vfxHeightOffset = 0f; // ↑ shu orqali tepaga ko'tar
    [SerializeField] private float repelStartDelay = 0.7f;
    void Awake()
    {
        _root = transform.root;
    }

    void Start()
    {
        if (explosionPool == null && BaseManager.Instance != null)
            explosionPool = BaseManager.Instance.pool;
    }

    void OnEnable()
    {
        if (aiAutoUse)
            StartCoroutine(AutoLoop());
        KopkariMainUI.OnHorsePushEffect += ActivateHere;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        KopkariMainUI.OnHorsePushEffect -= ActivateHere;
    }

    // UI button
    public void ActivateHere()
    {
        Vector3 pos = GetBombCenter();
        TryExplode(pos);
    }

    public void ActivateAt(Vector3 worldPos)
    {
        TryExplode(worldPos);
    }

    Vector3 GetBombCenter()
    {
        if (centerOverride) return centerOverride.position;
        return transform.position;
    }

    IEnumerator AutoLoop()
    {
        if (aiFirstDelay > 0f)
            yield return new WaitForSeconds(aiFirstDelay);

        while (aiAutoUse)
        {
            ActivateHere();
            float wait = Mathf.Max(0.1f, Random.Range(aiRepeatDelay.x, aiRepeatDelay.y));
            yield return new WaitForSeconds(wait);
        }
    }

    void TryExplode(Vector3 pos)
    {
        if (_onCD) return;
        Debug.Log("run");
        // Hech bo'lmasa bitta target bo'lsin
        if (KopkariRepelTarget.All == null || KopkariRepelTarget.All.Count == 0)
            return;

        StartCoroutine(DoLogicalRepel(pos));
    }

    IEnumerator DoLogicalRepel(Vector3 pos)
    {
        _onCD = true;

        Vector3 vfxPos = pos + Vector3.up * vfxHeightOffset;

        if (explosionPool != null)
            explosionPool.PlayAt(vfxPos, Quaternion.identity, dScale, dTime);

        //if (sfxClip != null)
        //    AudioSource.PlayClipAtPoint(sfxPos, vfxPos, sfxVolume);

        if (repelStartDelay > 0f)
            yield return new WaitForSeconds(repelStartDelay);
        // Barcha otlar bo'yicha yuramiz
        for (int i = 0; i < KopkariRepelTarget.All.Count; i++)
        {
            var target = KopkariRepelTarget.All[i];
            if (!target) continue;

            Transform t = target.transform;

            // O'z rootni ignore qilish
            if (ignoreSelf && t.root == _root)
                continue;

            Vector3 d = t.position - pos;
            if (horizontalOnly) d.y = 0f;

            float dist = d.magnitude;
            if (dist > radius || dist < 0.001f)
                continue;
            target.ApplyRepel(pos, logicalPushDistance, repelDuration, repelCurve);
        }

        yield return new WaitForSeconds(cooldown);
        _onCD = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 p = centerOverride ? centerOverride.position : transform.position;
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
