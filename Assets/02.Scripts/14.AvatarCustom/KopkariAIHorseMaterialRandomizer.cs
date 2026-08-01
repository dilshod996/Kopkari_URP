using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Applies the catalog horse material variation to Registon's separate horse
/// renderers and its combined LOD renderers. All references are serialized on
/// AIKopkariRider; this class never searches the scene or a hierarchy.
/// </summary>
public static class KopkariAIHorseMaterialRandomizer
{
    private const string HorseId = "horse_01";
    private const string BodySlot = "Body";
    private const string ManeSlot = "Mane";
    private const string SaddleSlot = "Saddle";

    // Registon combined-horse material layout:
    // New mane/tail mesh: Tail, Body, Eyes, Saddle, Mane, Reins.
    // Old tail mesh:      Tail, Body, Eyes, Saddle.
    private const int CombinedTailIndex = 0;
    private const int CombinedBodyIndex = 1;
    private const int CombinedEyesIndex = 2;
    private const int CombinedSaddleIndex = 3;
    private const int CombinedManeIndex = 4;
    private const int CombinedReinsIndex = 5;

    private sealed class MaterialPools
    {
        public List<string> body;
        public List<string> mane;
        public List<string> saddle;
    }

    private static readonly object CacheLock = new object();
    private static Task<MaterialPools> poolTask;
    private static readonly Dictionary<string, Task<Material>> MaterialTasks =
        new Dictionary<string, Task<Material>>(StringComparer.OrdinalIgnoreCase);

    public static async Task ApplyRandomAsync(
        int uniqueSeed,
        SkinnedMeshRenderer[] bodyRenderers,
        SkinnedMeshRenderer[] maneRenderers,
        SkinnedMeshRenderer[] tailRenderers,
        SkinnedMeshRenderer[] saddleRenderers,
        SkinnedMeshRenderer[] reinsRenderers,
        SkinnedMeshRenderer[] eyesRenderers,
        SkinnedMeshRenderer[] combinedRenderers)
    {
        if (AddressablesService.Instance == null || PlayerCatalogProvider.Instance == null)
            return;

        await AddressablesService.Instance.EnsureInitializedAsync();
        MaterialPools pools = await GetPoolsAsync();

        var random = new System.Random(uniqueSeed != 0 ? uniqueSeed : Environment.TickCount);
        string bodyKey = Pick(pools.body, random);
        string maneKey = Pick(pools.mane, random);
        string saddleKey = Pick(pools.saddle, random);

        Task<Material> bodyTask = LoadMaterialCachedAsync(bodyKey);
        Task<Material> maneTask = LoadMaterialCachedAsync(maneKey);
        Task<Material> saddleTask = LoadMaterialCachedAsync(saddleKey);
        Task<Material> eyesTask = LoadMaterialCachedAsync(Constants.HorseStaticMaterials.Eyes);
        await Task.WhenAll(bodyTask, maneTask, saddleTask, eyesTask);

        Material body = await bodyTask;
        Material mane = await maneTask;
        Material saddle = await saddleTask;
        Material eyes = await eyesTask;

        ApplySingleMaterial(bodyRenderers, body);
        ApplySingleMaterial(maneRenderers, mane);
        ApplySingleMaterial(tailRenderers, mane);
        ApplySingleMaterial(saddleRenderers, saddle);
        ApplySingleMaterial(reinsRenderers, saddle);
        ApplySingleMaterial(eyesRenderers, eyes);
        ApplyCombinedMaterials(combinedRenderers, body, mane, saddle, eyes);
    }

    private static Task<MaterialPools> GetPoolsAsync()
    {
        lock (CacheLock)
        {
            if (poolTask == null)
                poolTask = LoadPoolsAsync();
            return poolTask;
        }
    }

    private static async Task<MaterialPools> LoadPoolsAsync()
    {
        Task<List<string>> bodyTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(HorseId, BodySlot);
        Task<List<string>> maneTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(HorseId, ManeSlot);
        Task<List<string>> saddleTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(HorseId, SaddleSlot);

        await Task.WhenAll(bodyTask, maneTask, saddleTask);
        return new MaterialPools
        {
            body = await bodyTask,
            mane = await maneTask,
            saddle = await saddleTask
        };
    }

    private static string Pick(List<string> keys, System.Random random)
    {
        return keys == null || keys.Count == 0 ? string.Empty : keys[random.Next(keys.Count)];
    }

    private static void ApplySingleMaterial(SkinnedMeshRenderer[] renderers, Material material)
    {
        if (renderers == null || material == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer != null)
                renderer.sharedMaterial = material;
        }
    }

    private static void ApplyCombinedMaterials(
        SkinnedMeshRenderer[] renderers,
        Material body,
        Material mane,
        Material saddle,
        Material eyes)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            SetMaterial(materials, CombinedTailIndex, mane);
            SetMaterial(materials, CombinedBodyIndex, body);
            SetMaterial(materials, CombinedEyesIndex, eyes);
            SetMaterial(materials, CombinedSaddleIndex, saddle);
            SetMaterial(materials, CombinedManeIndex, mane);
            SetMaterial(materials, CombinedReinsIndex, saddle);
            renderer.sharedMaterials = materials;
        }
    }

    private static void SetMaterial(Material[] materials, int index, Material material)
    {
        if (material != null && materials != null && index >= 0 && index < materials.Length)
            materials[index] = material;
    }

    private static Task<Material> LoadMaterialCachedAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult<Material>(null);

        if (MaterialCacheManager.TryGet(key, out Material cached) && cached != null)
            return Task.FromResult(cached);

        lock (CacheLock)
        {
            if (!MaterialTasks.TryGetValue(key, out Task<Material> task))
            {
                task = LoadAndCacheMaterialAsync(key);
                MaterialTasks.Add(key, task);
            }
            return task;
        }
    }

    private static async Task<Material> LoadAndCacheMaterialAsync(string key)
    {
        try
        {
            Material material = await AddressablesService.Instance.LoadAssetAsync<Material>(key);
            if (material != null)
                MaterialCacheManager.Add(key, material);
            return material;
        }
        finally
        {
            lock (CacheLock)
                MaterialTasks.Remove(key);
        }
    }
}
