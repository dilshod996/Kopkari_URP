using System.Collections.Generic;
using UnityEngine;

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

    static readonly Dictionary<GameObject, Pool> _pools = new();
    static readonly Dictionary<GameObject, Pool> _owner = new();

    static Transform _defaultHolder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _pools.Clear();
        _owner.Clear();
        _defaultHolder = null;
    }

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

    /// <summary>Pool yaratish (bir marta). parent=null bo‘lsa avtomatik "--Pool--" yaratadi</summary>
    public static void CreatePool(GameObject prefab, int prewarm = 8, int maxSize = 30, bool expandable = true, Transform parent = null)
    {
        if (!prefab || _pools.ContainsKey(prefab)) return;

        parent ??= EnsureHolder();

        var p = new Pool
        {
            prefab = prefab,
            parent = parent,
            maxSize = Mathf.Max(1, maxSize),
            expandable = expandable
        };

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

        GameObject go;

        if (p.inactive.Count > 0)
        {
            go = p.inactive.Dequeue();
        }
        else
        {
            if (!p.expandable && p.Count >= p.maxSize) return null;

            go = Object.Instantiate(p.prefab, p.parent);
            _owner[go] = p;
            go.SetActive(false);
        }

        p.active.Add(go);

        var t = go.transform;
        t.SetParent(parent ? parent : p.parent, false);
        t.SetPositionAndRotation(pos, rot);

        // eski lifetime coroutine qolib ketmasin
        var runner = go.GetComponent<_AutoReturn>();
        if (runner != null) runner.StopAllCoroutines();

        go.SetActive(true);

        if (lifeTime > 0f)
        {
            runner ??= go.AddComponent<_AutoReturn>();
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

        // eski lifetime coroutine qolib ketmasin
        var runner = instance.GetComponent<_AutoReturn>();
        if (runner != null) runner.StopAllCoroutines();

        if (p.active.Remove(instance) || !p.inactive.Contains(instance))
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

    // ---------- UI helpers (xohlasang qoldir) ----------
    public static GameObject SpawnUI(GameObject prefab, RectTransform parent, Vector2 anchoredPos, bool worldPositionStays = false)
    {
        if (!prefab)
        {
            Debug.LogWarning("[SimplePool] Prefab null (SpawnUI).");
            return null;
        }

        var go = Spawn(prefab, Vector3.zero, Quaternion.identity, parent, lifeTime: 0f);
        if (!go) return null;

        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.SetParent(parent, worldPositionStays);
            rt.anchoredPosition = anchoredPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
        else
        {
            go.transform.SetParent(parent, worldPositionStays);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        return go;
    }

    public static GameObject SpawnUI(GameObject prefab, RectTransform parent)
        => SpawnUI(prefab, parent, Vector2.zero);

    public static void DespawnUI(GameObject instance) => Despawn(instance);

    // ---------- lifetime helper ----------
    class _AutoReturn : MonoBehaviour
    {
        float _time;
        bool _realtime;
        int _token;

        public void Run(float life)
        {
            _time = life;
            _realtime = false;
            _token++;
            StopAllCoroutines();
            StartCoroutine(Co(_token));
        }

        public void RunRealtime(float life)
        {
            _time = life;
            _realtime = true;
            _token++;
            StopAllCoroutines();
            StartCoroutine(Co(_token));
        }

        System.Collections.IEnumerator Co(int token)
        {
            if (_realtime) yield return new WaitForSecondsRealtime(_time);
            else yield return new WaitForSeconds(_time);

            if (token != _token) yield break;

            SimplePool.Despawn(gameObject);
        }
    }
}
