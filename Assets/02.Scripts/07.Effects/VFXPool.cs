using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class VFXPool : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 50;

    private ObjectPool<GameObject> pool;

    void Awake()
    {
        pool = new ObjectPool<GameObject>(
            CreatePooled,
            OnGet,
            OnRelease,
            OnDestroyPooled,
            collectionCheck: false,
            defaultCapacity,
            maxSize
        );
    }

    GameObject CreatePooled()
    {
        var go = Instantiate(explosionPrefab);
        go.SetActive(false);

        // VFX tugaganda poolga qaytarish uchun “Callback” ulaymiz
        var cb = go.GetComponent<PooledVFX>();
        if(cb == null)
        {
            cb = go.AddComponent<PooledVFX>();
        }
        cb.Init(ReturnToPool);

        return go;
    }

    void OnGet(GameObject go) => go.SetActive(true);
    void OnRelease(GameObject go) => go.SetActive(false);
    void OnDestroyPooled(GameObject go) => Destroy(go);

    public void PlayAt(Vector3 pos, Quaternion rot)
    {
        var go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);

        // ichidagi barcha particle’larni ishga tushiramiz
        var ps = go.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null) ps.Play(true);
    }
    public void PlayAt(Vector3 pos, Quaternion rot, float desiredScale = 2f, float scaleDuration = 1f)
    {
        var go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);

        // 1) Scale start holat
        go.transform.localScale = Vector3.one;

        // 2) Particle’larni ishga tushirish
        var ps = go.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null) ps.Play(true);

        // 3) Scale-ni vaqt bo‘yicha 1 -> desiredScale oshirish va tugaganda 1 ga qaytarish
        StartCoroutine(ScaleThenReset(go.transform, desiredScale, scaleDuration, ps));
    }

    private IEnumerator ScaleThenReset(Transform root, float desiredScale, float duration, ParticleSystem ps)
    {
        // 1→desiredScale
        float t = 0f;
        Vector3 from = Vector3.one;
        Vector3 to = Vector3.one * desiredScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            root.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }
        root.localScale = to;

        // Particle tugashini kutish (childlar bilan birga)
        if (ps != null)
        {
            while (ps.IsAlive(true))
                yield return null;
        }

        // Reset: qayta ishlatilganda doim 1x dan boshlaydi
        root.localScale = Vector3.one;

        // ⬇️ Agar PooledVFX o‘zi Release qilsa, bu qator kerak emas.
        // Agar poolingni shu klass boshqarsa, shu yerda qaytaring:
        // pool.Release(root.gameObject);
    }

    void ReturnToPool(GameObject go) => pool.Release(go);
}
