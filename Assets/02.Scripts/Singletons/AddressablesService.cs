using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressablesService : MonoBehaviour
{
    public static AddressablesService Instance { get; private set; }

    private Task _initTask;
    private bool _initialized;

    // Asset handle cache (LoadAssetAsync uchun)
    private readonly Dictionary<string, AsyncOperationHandle> _assetHandles = new();

    // Instance handle cache (InstantiateAsync uchun) - key: instanceId
    private readonly Dictionary<int, AsyncOperationHandle<GameObject>> _instanceHandles = new();

    // Optional: Environment instance ref
    public GameObject CurrentEnvironment { get; private set; }
    private AsyncOperationHandle<GameObject>? _currentEnvHandle;

    [Header("Network")]
    [SerializeField] private bool requireInternetWhenDownloadNeeded = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----------------------------
    // 0) INIT
    // ----------------------------
    public Task EnsureInitializedAsync()
    {
        if (_initialized)
            return Task.CompletedTask;

        if (_initTask != null)
            return _initTask;

        _initTask = InitInternalAsync();
        return _initTask;
    }

    private async Task InitInternalAsync()
    {
        // ❗ MUHIM: hech qanday Status / IsValid YO‘Q
        await UnityEngine.AddressableAssets.Addressables.InitializeAsync().Task;

        _initialized = true;
        Debug.Log("✅ Addressables initialized safely");
    }


    private bool HasInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    // ----------------------------
    // 1) PRELOAD / DOWNLOAD (dependencies)
    // ----------------------------
    /// <summary>
    /// Keys bu: address, label, yoki IResourceLocation bo‘lishi mumkin.
    /// Biz soddaroq: string key (address/label) bilan ishlaymiz.
    /// </summary>
    public async Task<bool> PreloadDependenciesAsync(
        IList<string> keys,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.5f)
    {
        if (keys == null || keys.Count == 0) { onProgress?.Invoke(1f); return true; }

        try
        {
            await EnsureInitializedAsync();
        }
        catch
        {
            return false;
        }
        var sizeHandle = Addressables.GetDownloadSizeAsync(keys);
        long size = await sizeHandle.Task;
        if (sizeHandle.IsValid())
            Addressables.Release(sizeHandle);
        bool isCached = (size == 0);

        if (!isCached && requireInternetWhenDownloadNeeded && !HasInternetConnection())
            return false;

        if (isCached)
        {
            await FakeProgressAsync(onProgress, fakeDurationIfCached);
            onProgress?.Invoke(1f);
            return true;
        }

        var downloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);

        while (!downloadHandle.IsDone)
        {
            onProgress?.Invoke(downloadHandle.PercentComplete);
            await Task.Yield();
        }

        bool ok = downloadHandle.IsValid() && downloadHandle.Status == AsyncOperationStatus.Succeeded;

        if (downloadHandle.IsValid())
            Addressables.Release(downloadHandle);

        onProgress?.Invoke(ok ? 1f : 0f);
        return ok;
    }


    /// <summary>
    /// Bitta address/label uchun preload.
    /// </summary>
    public async Task<bool> PreloadDependenciesAsync(
        string key,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.2f)
    {
        return await PreloadDependenciesAsync(new List<string> { key }, onProgress, fakeDurationIfCached);
    }

    private async Task FakeProgressAsync(Action<float> onProgress, float duration)
    {
        if (onProgress == null || duration <= 0f) return;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            onProgress(Mathf.Clamp01(t / duration));
            await Task.Yield();
        }
    }

    // ----------------------------
    // 2) LOAD ASSET (no instantiate) + CACHE
    // ----------------------------
    public async Task<T> LoadAssetAsync<T>(string addressOrLabel) where T : UnityEngine.Object
    {
        await EnsureInitializedAsync();

        // Cache bo‘lsa, handle’dan qaytaramiz
        if (_assetHandles.TryGetValue(addressOrLabel, out var cachedHandle) && cachedHandle.IsValid())
        {
            return (cachedHandle.Result as T);
        }

        var handle = Addressables.LoadAssetAsync<T>(addressOrLabel);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
            return null;

        _assetHandles[addressOrLabel] = handle;
        return handle.Result;
    }

    public void ReleaseLoadedAsset(string addressOrLabel)
    {
        if (_assetHandles.TryGetValue(addressOrLabel, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
            _assetHandles.Remove(addressOrLabel);
        }
    }

    public void ReleaseAllLoadedAssets()
    {
        foreach (var kv in _assetHandles)
        {
            if (kv.Value.IsValid()) Addressables.Release(kv.Value);
        }
        _assetHandles.Clear();
    }

    // ----------------------------
    // 3) INSTANTIATE (GameObject) + TRACK INSTANCE HANDLES
    // ----------------------------
    public async Task<GameObject> InstantiateAsync(
        string address,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null)
    {
        await EnsureInitializedAsync();

        var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
            return null;

        var go = handle.Result;
        _instanceHandles[go.GetInstanceID()] = handle;
        return go;
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;

        int id = instance.GetInstanceID();
        if (_instanceHandles.TryGetValue(id, out var handle) && handle.IsValid())
        {
            Addressables.ReleaseInstance(handle);
            _instanceHandles.Remove(id);
        }
        else
        {
            // Agar handle topilmasa, fallback:
            Destroy(instance);
        }
    }

    public void ReleaseAllInstances()
    {
        var ids = _instanceHandles.Keys.ToList();
        foreach (var id in ids)
        {
            var h = _instanceHandles[id];
            if (h.IsValid()) Addressables.ReleaseInstance(h);
        }
        _instanceHandles.Clear();
    }

    // ----------------------------
    // 4) ENVIRONMENT METHODS (swap)
    // ----------------------------
    /// <summary>
    /// Environment’ni loading panel ortida preload + instantiate qiladi.
    /// Old env bo‘lsa - release qiladi.
    /// </summary>
    public async Task<GameObject> LoadEnvironmentAsync(
        string envAddress,
        Transform parent,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.5f,
        Vector3? position = null,
        Quaternion? rotation = null)
    {
        // 1) Preload dependencies (cached bo‘lsa fake progress)
        bool ok = await PreloadDependenciesAsync(envAddress, onProgress, fakeDurationIfCached);
        if (!ok) return null;

        // 2) Old env’ni unload
        await UnloadCurrentEnvironmentAsync();

        // 3) Instantiate new env
        Vector3 pos = position ?? Vector3.zero;
        Quaternion rot = rotation ?? Quaternion.identity;

        var handle = Addressables.InstantiateAsync(envAddress, pos, rot, parent);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
            return null;

        CurrentEnvironment = handle.Result;
        _currentEnvHandle = handle;

        // Track instance handle
        _instanceHandles[CurrentEnvironment.GetInstanceID()] = handle;

        return CurrentEnvironment;
    }
    public async Task<GameObject> LoadEnvironmentAsync(
    IList<string> preloadKeys,
    string envAddress,
    Transform parent,
    Action<float> onProgress = null,
    float fakeDurationIfCached = 1.5f,
    Vector3? position = null,
    Quaternion? rotation = null)
    {
        bool ok = await PreloadDependenciesAsync(preloadKeys, onProgress, fakeDurationIfCached);
        if (!ok) return null;

        await UnloadCurrentEnvironmentAsync();

        Vector3 pos = position ?? Vector3.zero;
        Quaternion rot = rotation ?? Quaternion.identity;

        var handle = Addressables.InstantiateAsync(envAddress, pos, rot, parent);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
            return null;

        CurrentEnvironment = handle.Result;
        _currentEnvHandle = handle;
        _instanceHandles[CurrentEnvironment.GetInstanceID()] = handle;

        return CurrentEnvironment;
    }


    public Task UnloadCurrentEnvironmentAsync()
    {
        if (CurrentEnvironment == null) return Task.CompletedTask;

        ReleaseInstance(CurrentEnvironment);
        CurrentEnvironment = null;
        _currentEnvHandle = null;

        return Task.CompletedTask;
    }
}
