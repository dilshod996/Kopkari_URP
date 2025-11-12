using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;

public static class MaterialCacheManager
{
    private static readonly Dictionary<string, Material> materialCache = new();

    // Qo‘shilgan: har bir address uchun reference count
    private static readonly Dictionary<string, int> refCounts = new();

    public static bool TryGet(string address, out Material mat)
    {
        return materialCache.TryGetValue(address, out mat);
    }

    public static void Add(string address, Material mat)
    {
        if (!materialCache.ContainsKey(address))
        {
            materialCache[address] = mat;
            refCounts[address] = 1;
        }
        else
        {
            refCounts[address]++;
        }
    }

    public static void Release(string address)
    {
        if (materialCache.ContainsKey(address))
        {
            refCounts[address]--;

            if (refCounts[address] <= 0)
            {
                Addressables.Release(materialCache[address]);
                materialCache.Remove(address);
                refCounts.Remove(address);
                Debug.Log($"🧹 Released material: {address}");
            }
        }
    }

    public static void ReleaseAll()
    {
        foreach (var kvp in materialCache)
        {
            Addressables.Release(kvp.Value);
        }

        materialCache.Clear();
        refCounts.Clear();
        Debug.Log("🧹 All cached materials released");
    }
}
