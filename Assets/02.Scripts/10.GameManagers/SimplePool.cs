using System.Collections.Generic;
using UnityEngine;
// --- NEW: kerak emas bo'lsa ham qoldirsa bo'ladi (atributlar UnityEngine'da)
// using UnityEngine.SceneManagement;

public static class SimplePool
{
    class Pool
    {
        public GameObject prefab;
        public Transform parent;
        public readonly Queue<GameObject> inactive = new();
        public readonly HashSet<GameObject> active = new();
        public int maxSize;
        public bool expandable;

        public int Count => inactive.Count + active.Count;
    }

    // prefab -> pool
    static readonly Dictionary<GameObject, Pool> _pools = new();
    // instance -> pool
    static readonly Dictionary<GameObject, Pool> _owner = new();

    // --- NEW: default holder kesh (destroy bo'lsa qayta tiklash uchun)
    static Transform _defaultHolder;

    // --- NEW: domain/scene reloadda statiklarni tozalash
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _pools.Clear();
        _owner.Clear();
        _defaultHolder = null;
    }

    // --- NEW: holderni kafolatlash (agar yo'q bo'lsa yaratadi)
    static Transform EnsureHolder()
    {
        if (_defaultHolder == null)
        {
            var go = GameObject.Find("--Pool--");
            if (!go) go = new GameObject("--Pool--");
            _defaultHolder = go.transform;
        }
        return _defaultHolder;
    }

    /// <summary>Pool yaratish (bir marta). parent=null bo‘lsa avtomatik "—Pool—" yaratadi</summary>
    public static void CreatePool(GameObject prefab, int prewarm = 8, int maxSize = 30, bool expandable = true, Transform parent = null)
    {
        if (!prefab || _pools.ContainsKey(prefab)) return;

        if (!parent)
        {
            // OLD:
            // var holder = GameObject.Find("--Pool--") ?? new GameObject("--Pool--");
            // parent = holder.transform;

            // --- NEW: holderni kesh orqali kafolatlaymiz
            parent = EnsureHolder();
        }

        var p = new Pool { prefab = prefab, parent = parent, maxSize = Mathf.Max(1, maxSize), expandable = expandable };
        _pools[prefab] = p;

        for (int i = 0; i < Mathf.Max(0, prewarm); i++)
        {
            var go = Object.Instantiate(prefab, parent);
            go.SetActive(false);
            _owner[go] = p;
            p.inactive.Enqueue(go);
        }
    }

    /// <summary>Spawn; lifeTime>0 bo‘lsa avtomatik qaytaradi</summary>
    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null, float lifeTime = 0f)
    {
        if (!prefab)
        {
            Debug.LogWarning("[SimplePool] Prefab null.");
            return null;
        }

        if (!_pools.TryGetValue(prefab, out var p))
        {
            CreatePool(prefab, prewarm: 0);
            p = _pools[prefab];
        }

        GameObject go = null;
        if (p.inactive.Count > 0)
        {
            go = p.inactive.Dequeue();
        }
        else
        {
            if (p.expandable || p.Count < p.maxSize)
            {
                go = Object.Instantiate(p.prefab, p.parent);
                _owner[go] = p;
                go.SetActive(false);
            }
            else
            {
                // limit to‘ldi
                return null;
            }
        }

        p.active.Add(go);

        var t = go.transform;
        t.SetParent(parent ? parent : p.parent, false);
        t.SetPositionAndRotation(pos, rot);

        go.SetActive(true);

        if (lifeTime > 0f)
        {
            // Auto-despawn korutini
            var runner = go.GetComponent<_AutoReturn>() ?? go.AddComponent<_AutoReturn>();
            runner.Run(lifeTime);
        }

        return go;
    }

    public static void Despawn(GameObject instance)
    {
        if (!instance) return;
        if (!_owner.TryGetValue(instance, out var p))
        {
            Object.Destroy(instance);
            return;
        }

        if (p.active.Remove(instance))
        {
            instance.SetActive(false);
            instance.transform.SetParent(p.parent, false);
            p.inactive.Enqueue(instance);
        }
        else if (!p.inactive.Contains(instance))
        {
            instance.SetActive(false);
            instance.transform.SetParent(p.parent, false);
            p.inactive.Enqueue(instance);
        }
    }

    public static void DespawnAll(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var p)) return;
        var copy = new List<GameObject>(p.active);
        foreach (var go in copy) Despawn(go);
    }

    // --- NEW: sahna almashtirishda yoki restartda hamma narsani tozalash uchun
    public static void ClearAll(bool destroyInstances = true)
    {
        if (destroyInstances)
        {
            foreach (var kv in _pools)
            {
                var p = kv.Value;
                foreach (var a in p.active) if (a) Object.Destroy(a);
                foreach (var i in p.inactive) if (i) Object.Destroy(i);
            }
        }

        _pools.Clear();
        _owner.Clear();

        if (_defaultHolder)
        {
            Object.Destroy(_defaultHolder.gameObject);
            _defaultHolder = null;
        }
    }

    // Kichik ichki helper komponent (faqat bitta skript ichida)
    class _AutoReturn : MonoBehaviour
    {
        float _time;
        bool _running;

        public void Run(float life)
        {
            _time = life;
            if (!_running) StartCoroutine(Co());
        }

        System.Collections.IEnumerator Co()
        {
            _running = true;
            yield return new WaitForSeconds(_time);
            _running = false;
            SimplePool.Despawn(gameObject);
        }
    }
}
