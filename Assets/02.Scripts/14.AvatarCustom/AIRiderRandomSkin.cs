using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AIRiderRandomSkin : MonoBehaviour
{
    [Header("LOD Group (recommended)")]
    [SerializeField] private LODGroup lodGroup;

    //[Header("Fallback (if no LODGroup)")]
    //[SerializeField] private SkinnedMeshRenderer combinedSMR;

    [Header("Catalog Avatar Id (horse id)")]
    [SerializeField] private string avatarId = "horse_01";

    [Header("Catalog Slot Ids")]
    [SerializeField] private string bodySlotId = "Body";
    [SerializeField] private string maneSlotId = "Mane";      // tail ham shu
    [SerializeField] private string saddleSlotId = "Saddle";  // reins ham shu

    // ✅ Sening inspector mapping (5 materials):
    // 0 ManeA, 1 Body, 2 Eyes, 3 Saddle, 4 ManeB
    private const int ManeIndexA = 0;
    private const int BodyIndex = 1;
    private const int EyesIndex = 2;
    private const int SaddleIndex = 3;
    private const int ManeIndexB = 4;

    // Pools (catalogdan)
    private List<string> _bodyKeys, _maneKeys, _saddleKeys;
    private bool _poolsReady;

    private sealed class SkinPools
    {
        public List<string> body;
        public List<string> mane;
        public List<string> saddle;
    }

    private static readonly object CacheLock = new object();
    private static readonly Dictionary<string, Task<SkinPools>> PoolTasks =
        new Dictionary<string, Task<SkinPools>>();
    private static readonly Dictionary<string, Task<Material>> MaterialTasks =
        new Dictionary<string, Task<Material>>();

    // LOD renderers cache
    private readonly List<SkinnedMeshRenderer> _lodSMRs = new();

    private void Awake()
    {
        CacheLODRenderers();
    }

    private void CacheLODRenderers()
    {
        _lodSMRs.Clear();

        if (lodGroup != null)
        {
            var lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                var rs = lods[i].renderers;
                for (int j = 0; j < rs.Length; j++)
                {
                    if (rs[j] is SkinnedMeshRenderer smr && smr != null)
                        _lodSMRs.Add(smr);
                }
            }
        }

        // fallback
        //if (_lodSMRs.Count == 0 && combinedSMR != null)
        //    _lodSMRs.Add(combinedSMR);
    }

    public void SetAvatarId(string id) => avatarId = id;

    public async Task ApplyRandomAsync(int uniqueSeed = 0, bool useSharedLoading = false)
    {
        // LOD rendererlar yo'q bo'lsa qayta cache qilib ko'ramiz
        if (_lodSMRs.Count == 0)
            CacheLODRenderers();

        if (_lodSMRs.Count == 0)
            return;

        await AddressablesService.Instance.EnsureInitializedAsync();

        // 1) Poollarni 1 marta olish
        if (!_poolsReady)
        {
            if (useSharedLoading)
            {
                SkinPools pools = await GetSharedPoolsAsync();
                _bodyKeys = pools.body;
                _maneKeys = pools.mane;
                _saddleKeys = pools.saddle;
            }
            else
            {
                _bodyKeys = await PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, bodySlotId);
                _maneKeys = await PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, maneSlotId);
                _saddleKeys = await PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, saddleSlotId);
            }
            _poolsReady = true;
        }

        int seed = (uniqueSeed != 0) ? uniqueSeed : (Environment.TickCount ^ GetInstanceID());
        var rng = new System.Random(seed);

        string bodyKey = Pick(_bodyKeys, rng);
        string maneKey = Pick(_maneKeys, rng);
        string saddleKey = Pick(_saddleKeys, rng);

        var bodyMat = await LoadMatCached(bodyKey);
        var maneMat = await LoadMatCached(maneKey);
        var saddleMat = await LoadMatCached(saddleKey);

        // ✅ Eyes doim default (senda shunday kerak edi)
        var eyesMat = await LoadMatCached(Constants.HorseStaticMaterials.Eyes);

        // 2) LOD0 dan material layout (length) olamiz (5 ta bo'lishi kerak)
        var baseMats = _lodSMRs[0].sharedMaterials;
        if (baseMats == null || baseMats.Length < 5)
            return;

        // 3) Yangi material array tayyorlaymiz
        var newMats = (Material[])baseMats.Clone();

        SetMat(newMats, BodyIndex, bodyMat);
        SetMat(newMats, SaddleIndex, saddleMat);
        SetMat(newMats, ManeIndexA, maneMat);
        SetMat(newMats, ManeIndexB, maneMat);
        SetMat(newMats, EyesIndex, eyesMat);

        // 4) Hammasiga apply (LOD0/LOD1/LOD2)
        for (int i = 0; i < _lodSMRs.Count; i++)
        {
            var smr = _lodSMRs[i];
            if (smr == null) continue;

            // Ba'zan LOD1/2 material length boshqacha bo'lishi mumkin:
            // shunda biz safe qilib, copydan clone qilib set qilamiz
            var mats = smr.sharedMaterials;
            if (mats == null || mats.Length == 0)
                continue;

            if (mats.Length == newMats.Length)
            {
                smr.sharedMaterials = (Material[])newMats.Clone();
            }
            else
            {
                // length farq bo'lsa: mavjud length bo'yicha mos indexlarni set qilamiz
                var fixedMats = (Material[])mats.Clone();
                SafeSet(fixedMats, BodyIndex, bodyMat);
                SafeSet(fixedMats, EyesIndex, eyesMat);
                SafeSet(fixedMats, SaddleIndex, saddleMat);
                SafeSet(fixedMats, ManeIndexA, maneMat);
                SafeSet(fixedMats, ManeIndexB, maneMat);
                smr.sharedMaterials = fixedMats;
            }
        }
    }

    private static string Pick(List<string> keys, System.Random rng)
    {
        if (keys == null || keys.Count == 0) return "";
        return keys[rng.Next(0, keys.Count)];
    }

    private static void SetMat(Material[] mats, int idx, Material mat)
    {
        if (mat == null) return;
        mats[idx] = mat;
    }

    private static void SafeSet(Material[] mats, int idx, Material mat)
    {
        if (mat == null) return;
        if (idx < 0 || idx >= mats.Length) return;
        mats[idx] = mat;
    }

    private static async Task<Material> LoadMatCached(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (MaterialCacheManager.TryGet(key, out var cached) && cached != null)
            return cached;

        Task<Material> loadTask;
        lock (CacheLock)
        {
            if (!MaterialTasks.TryGetValue(key, out loadTask))
            {
                loadTask = LoadAndCacheMaterial(key);
                MaterialTasks.Add(key, loadTask);
            }
        }

        return await loadTask;
    }

    private Task<SkinPools> GetSharedPoolsAsync()
    {
        string cacheKey = $"{avatarId}|{bodySlotId}|{maneSlotId}|{saddleSlotId}";
        lock (CacheLock)
        {
            if (!PoolTasks.TryGetValue(cacheKey, out Task<SkinPools> task))
            {
                task = LoadPoolsAsync();
                PoolTasks.Add(cacheKey, task);
            }

            return task;
        }
    }

    private async Task<SkinPools> LoadPoolsAsync()
    {
        Task<List<string>> bodyTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, bodySlotId);
        Task<List<string>> maneTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, maneSlotId);
        Task<List<string>> saddleTask =
            PlayerCatalogProvider.Instance.GetMaterialKeysAsync(avatarId, saddleSlotId);

        await Task.WhenAll(bodyTask, maneTask, saddleTask);
        return new SkinPools
        {
            body = await bodyTask,
            mane = await maneTask,
            saddle = await saddleTask
        };
    }

    private static async Task<Material> LoadAndCacheMaterial(string key)
    {
        try
        {
            var mat = await AddressablesService.Instance.LoadAssetAsync<Material>(key);
            if (mat != null) MaterialCacheManager.Add(key, mat);
            return mat;
        }
        finally
        {
            lock (CacheLock)
                MaterialTasks.Remove(key);
        }
    }
}
