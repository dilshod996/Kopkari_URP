using System;
using System.Collections.Generic;
using UnityEngine;

public static class HorsePhysicsRegistry
{
    static readonly List<Rigidbody> _list = new List<Rigidbody>(256);
    static readonly Dictionary<Rigidbody, int> _index = new Dictionary<Rigidbody, int>(256);

    public static int Count => _list.Count;

    public static bool Add(Rigidbody rb)
    {
        if (!rb || _index.ContainsKey(rb)) return false;
        _index[rb] = _list.Count;
        _list.Add(rb);
        return true;
    }

    public static bool Remove(Rigidbody rb)
    {
        if (!rb || !_index.TryGetValue(rb, out int i)) return false;
        int last = _list.Count - 1;
        var lastRB = _list[last];
        _list[i] = lastRB;
        _index[lastRB] = i;
        _list.RemoveAt(last);
        _index.Remove(rb);
        return true;
    }
    public static int CleanupNulls()
    {
        int removed = 0;
        for (int i = _list.Count - 1; i >= 0; i--)
        {
            var rb = _list[i];
            if (rb == null)
            {
                // dict’ni ham sync qilamiz
                _list.RemoveAt(i);
                // dictdan kalitni topib o‘chirish:
                foreach (var kv in _index)
                {
                    if (kv.Key == null || kv.Value == i) { _index.Remove(kv.Key); break; }
                }
                // qolganlarning indeksini yangilash uchun kichik reindex (kamdan-kam bo‘ladi)
                for (int j = i; j < _list.Count; j++) _index[_list[j]] = j;
                removed++;
            }
        }
        return removed;
    }

    // ✅ Ishlayotgan (null bo‘lmagan) sonni bilish
    public static int NonNullCount()
    {
        int c = 0;
        for (int i = 0; i < _list.Count; i++) if (_list[i] != null) c++;
        return c;
    }

    // ✅ Radius filtri yo‘q, shunchaki hammasini aylanib chiqish (diagnostika/pro aylandirish uchun)
    public static void ForEach(System.Action<Rigidbody> action)
    {
        var arr = _list;
        for (int i = 0; i < arr.Count; i++) if (arr[i] != null) action(arr[i]);
    }


    /// center/radius ichidagi RB’lar uchun callback (alloc-free)
    public static void ForEachWithinRadius(Vector3 center, float radius, Rigidbody except, Action<Rigidbody, float> onHit)
    {
        float r2 = radius * radius;
        var arr = _list; // local ref
        for (int i = 0; i < arr.Count; i++)
        {
            var rb = arr[i];
            if (!rb || rb == except) continue;

            Vector3 d = rb.worldCenterOfMass - center;
            float distSqr = d.sqrMagnitude;
            if (distSqr > r2) continue;

            float dist = Mathf.Sqrt(distSqr); // faqat radius ichida
            onHit?.Invoke(rb, dist);
        }
    }
}
