using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public sealed class AddressablesService : MonoBehaviour
{
    public enum DependencyDownloadState
    {
        NotDownloaded,
        Downloading,
        Ready,
        Failed
    }

    private sealed class DependencyDownloadOperation
    {
        public readonly TaskCompletionSource<bool> Completion = new();
        public readonly List<Action<float>> ProgressListeners = new();
        public float Progress;
    }

    public static AddressablesService Instance { get; private set; }
    public event Action<string, DependencyDownloadState, float> DependencyDownloadStateChanged;

    private Task _initTask;
    private bool _initialized;
    private AsyncOperationHandle<IResourceLocator> _initHandle;
    private bool _hasInitHandle;

    // Asset handle cache (LoadAssetAsync uchun)
    private readonly Dictionary<string, AsyncOperationHandle> _assetHandles = new();
    private readonly Dictionary<string, object> _assetListCache = new();
    private readonly Dictionary<string, Task<UnityEngine.Object>> _assetLoadTasks = new();
    private readonly Dictionary<string, DependencyDownloadOperation> _dependencyDownloadOperations = new();
    private readonly HashSet<string> _readyDependencyKeys = new();
    private readonly HashSet<string> _failedDependencyKeys = new();

    // Instance handle cache (InstantiateAsync uchun) - key: instanceId
    private readonly Dictionary<int, AsyncOperationHandle<GameObject>> _instanceHandles = new();

    // Optional: Environment instance ref
    public GameObject CurrentEnvironment { get; private set; }
    private AsyncOperationHandle<GameObject>? _currentEnvHandle;

    [Header("Network")]
    [SerializeField] private bool requireInternetWhenDownloadNeeded = true;

    [Header("Error Popup")]
    [SerializeField] private bool showPopupOnErrors = true;
    [SerializeField] private float popupCooldownSeconds = 2f;
    [SerializeField] private int popupTitleTextId = 520;
    [SerializeField] private int popupDescriptionTextId = 521;
    [SerializeField] private int popupButtonTextId = 428;

    private string _lastPopupMessage;
    private float _lastPopupTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (_hasInitHandle && _initHandle.IsValid())
            Addressables.Release(_initHandle);
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
        try
        {
            _initHandle = Addressables.InitializeAsync(autoReleaseHandle: false);
            _hasInitHandle = _initHandle.IsValid();

            if (!_hasInitHandle)
                throw new InvalidOperationException("Addressables initialization returned an invalid operation handle.");

            await _initHandle.Task;

            if (!IsSucceeded(_initHandle))
            {
                throw new InvalidOperationException(
                    BuildHandleError("Addressables initialization failed.", _initHandle),
                    GetOperationException(_initHandle));
            }

            _initialized = true;
            Debug.Log("Addressables initialized safely");
        }
        catch (Exception ex)
        {
            _initialized = false;
            _initTask = null;

            if (_hasInitHandle && _initHandle.IsValid())
            {
                Addressables.Release(_initHandle);
                _hasInitHandle = false;
            }

            ReportAddressablesError("Addressables initialization failed.", ex);

            throw;
        }
    }

    private bool HasInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    // ----------------------------
    // 1) PRELOAD / DOWNLOAD (dependencies)
    // ----------------------------
    /// <summary>
    /// Keys bu: address, label, yoki IResourceLocation bo'lishi mumkin.
    /// Biz soddaroq: string key (address/label) bilan ishlaymiz.
    /// </summary>
    public async Task<bool> PreloadDependenciesAsync(
        IList<string> keys,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.5f,
        bool showErrorPopup = true)
    {
        if (keys == null || keys.Count == 0) { onProgress?.Invoke(1f); return true; }

        AsyncOperationHandle<long> sizeHandle = default;
        AsyncOperationHandle downloadHandle = default;
        string keyText = FormatKeys(keys);

        try
        {
            await EnsureInitializedAsync();

            List<string> missingKeys = await FindMissingKeysAsync(keys);
            if (missingKeys.Count > 0)
            {
                ReportAddressablesError(
                    $"Required Addressables keys were not found: {FormatKeys(missingKeys)}",
                    showPopup: showErrorPopup);
                onProgress?.Invoke(0f);
                return false;
            }

            sizeHandle = Addressables.GetDownloadSizeAsync(keys);
            long size = await sizeHandle.Task;

            if (!IsSucceeded(sizeHandle))
            {
                ReportAddressablesError(BuildHandleError($"Failed to check download size for: {keyText}", sizeHandle),
                    GetOperationException(sizeHandle),
                    showErrorPopup);
                onProgress?.Invoke(0f);
                return false;
            }

            bool isCached = (size == 0);

            if (!isCached && requireInternetWhenDownloadNeeded && !HasInternetConnection())
            {
                ReportAddressablesError(
                    $"Internet connection is required to download Addressables content: {keyText}",
                    showPopup: showErrorPopup);
                onProgress?.Invoke(0f);
                return false;
            }

            if (isCached)
            {
                await FakeProgressAsync(onProgress, fakeDurationIfCached);
                onProgress?.Invoke(1f);
                return true;
            }

            downloadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);

            while (!downloadHandle.IsDone)
            {
                onProgress?.Invoke(downloadHandle.PercentComplete);
                await Task.Yield();
            }

            bool ok = IsSucceeded(downloadHandle);

            if (!ok)
            {
                ReportAddressablesError(BuildHandleError($"Addressables download failed for: {keyText}", downloadHandle),
                    GetOperationException(downloadHandle),
                    showErrorPopup);
            }

            onProgress?.Invoke(ok ? 1f : 0f);
            return ok;
        }
        catch (Exception ex)
        {
            if (!WasInitializationFailureAlreadyReported())
                ReportAddressablesError($"Addressables preload failed for: {keyText}", ex, showErrorPopup);

            onProgress?.Invoke(0f);
            return false;
        }
        finally
        {
            if (sizeHandle.IsValid())
                Addressables.Release(sizeHandle);

            if (downloadHandle.IsValid())
                Addressables.Release(downloadHandle);
        }
    }

    private async Task<List<string>> FindMissingKeysAsync(IList<string> keys)
    {
        var missingKeys = new List<string>();

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (string.IsNullOrWhiteSpace(key))
            {
                missingKeys.Add("<empty>");
                continue;
            }

            AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = default;
            try
            {
                locationsHandle = Addressables.LoadResourceLocationsAsync(key);
                IList<IResourceLocation> locations = await locationsHandle.Task;

                if (!IsSucceeded(locationsHandle) || locations == null || locations.Count == 0)
                    missingKeys.Add(key);
            }
            catch
            {
                missingKeys.Add(key);
            }
            finally
            {
                if (locationsHandle.IsValid())
                    Addressables.Release(locationsHandle);
            }
        }

        return missingKeys;
    }

    public async Task<bool> AddressExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = default;

        try
        {
            await EnsureInitializedAsync();
            locationsHandle = Addressables.LoadResourceLocationsAsync(key.Trim());
            IList<IResourceLocation> locations = await locationsHandle.Task;

            return IsSucceeded(locationsHandle) &&
                   locations != null &&
                   locations.Count > 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not check Addressables address '{key}': {ex.Message}");
            return false;
        }
        finally
        {
            if (locationsHandle.IsValid())
                Addressables.Release(locationsHandle);
        }
    }

    /// <summary>
    /// Bitta address/label uchun preload.
    /// </summary>
    public async Task<bool> PreloadDependenciesAsync(
        string key,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.2f,
        bool showErrorPopup = true)
    {
        return await PreloadDependenciesAsync(new List<string> { key }, onProgress, fakeDurationIfCached, showErrorPopup);
    }

    /// <summary>
    /// Ensures that one Addressables key and all of its dependencies are cached.
    /// Concurrent callers for the same key share one download operation.
    /// </summary>
    public async Task<bool> EnsureDependenciesDownloadedAsync(
        string key,
        Action<float> onProgress = null,
        bool showErrorPopup = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            onProgress?.Invoke(0f);
            ReportAddressablesError(
                "Cannot download Addressables dependencies for an empty key.",
                showPopup: showErrorPopup);
            return false;
        }

        string normalizedKey = key.Trim();

        if (_dependencyDownloadOperations.TryGetValue(normalizedKey, out DependencyDownloadOperation existing))
        {
            AddProgressListener(existing, onProgress);
            return await existing.Completion.Task;
        }

        var operation = new DependencyDownloadOperation();
        AddProgressListener(operation, onProgress);
        _dependencyDownloadOperations[normalizedKey] = operation;
        SetDependencyDownloadState(normalizedKey, DependencyDownloadState.Downloading, 0f);
        _ = RunDependencyDownloadAsync(normalizedKey, operation, showErrorPopup);

        return await operation.Completion.Task;
    }

    public DependencyDownloadState GetDependencyDownloadState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return DependencyDownloadState.NotDownloaded;

        string normalizedKey = key.Trim();
        if (_dependencyDownloadOperations.ContainsKey(normalizedKey))
            return DependencyDownloadState.Downloading;
        if (_readyDependencyKeys.Contains(normalizedKey))
            return DependencyDownloadState.Ready;
        if (_failedDependencyKeys.Contains(normalizedKey))
            return DependencyDownloadState.Failed;

        return DependencyDownloadState.NotDownloaded;
    }

    private async Task RunDependencyDownloadAsync(
        string key,
        DependencyDownloadOperation operation,
        bool showErrorPopup)
    {
        bool succeeded = false;

        try
        {
            succeeded = await PreloadDependenciesAsync(
                new List<string> { key },
                progress => NotifyDependencyDownloadProgress(key, operation, progress),
                fakeDurationIfCached: 0f,
                showErrorPopup: showErrorPopup);
        }
        catch (Exception ex)
        {
            ReportAddressablesError(
                $"Addressables dependency download failed for: {key}",
                ex,
                showErrorPopup);
        }
        finally
        {
            _dependencyDownloadOperations.Remove(key);

            if (succeeded)
            {
                _failedDependencyKeys.Remove(key);
                _readyDependencyKeys.Add(key);
                NotifyDependencyDownloadProgress(key, operation, 1f);
                SetDependencyDownloadState(key, DependencyDownloadState.Ready, 1f);
            }
            else
            {
                _readyDependencyKeys.Remove(key);
                _failedDependencyKeys.Add(key);
                SetDependencyDownloadState(key, DependencyDownloadState.Failed, operation.Progress);
            }

            operation.Completion.TrySetResult(succeeded);
        }
    }

    private static void AddProgressListener(
        DependencyDownloadOperation operation,
        Action<float> listener)
    {
        if (listener == null)
            return;

        operation.ProgressListeners.Add(listener);
        try
        {
            listener(operation.Progress);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void NotifyDependencyDownloadProgress(
        string key,
        DependencyDownloadOperation operation,
        float progress)
    {
        operation.Progress = Mathf.Clamp01(progress);

        for (int i = 0; i < operation.ProgressListeners.Count; i++)
        {
            try
            {
                operation.ProgressListeners[i]?.Invoke(operation.Progress);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        SetDependencyDownloadState(key, DependencyDownloadState.Downloading, operation.Progress);
    }

    private void SetDependencyDownloadState(
        string key,
        DependencyDownloadState state,
        float progress)
    {
        try
        {
            DependencyDownloadStateChanged?.Invoke(key, state, Mathf.Clamp01(progress));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
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
    public async Task<T> LoadAssetAsync<T>(string addressOrLabel, bool showErrorPopup = true) where T : UnityEngine.Object
    {
        try
        {
            await EnsureInitializedAsync();

            // Cache bo'lsa, handle'dan qaytaramiz
            if (_assetHandles.TryGetValue(addressOrLabel, out var cachedHandle) && cachedHandle.IsValid())
            {
                return cachedHandle.Result as T;
            }

            // Share one operation when multiple startup systems ask for the same address concurrently.
            if (_assetLoadTasks.TryGetValue(addressOrLabel, out Task<UnityEngine.Object> existingLoadTask))
                return await existingLoadTask as T;

            Task<UnityEngine.Object> loadTask = LoadAssetInternalAsync<T>(addressOrLabel);
            _assetLoadTasks[addressOrLabel] = loadTask;

            try
            {
                return await loadTask as T;
            }
            finally
            {
                if (_assetLoadTasks.TryGetValue(addressOrLabel, out Task<UnityEngine.Object> currentTask)
                    && ReferenceEquals(currentTask, loadTask))
                {
                    _assetLoadTasks.Remove(addressOrLabel);
                }
            }
        }
        catch (Exception ex)
        {
            if (!WasInitializationFailureAlreadyReported())
                ReportAddressablesError($"Exception while loading Addressables asset: {addressOrLabel}", ex, showErrorPopup);

            return null;
        }
    }

    private async Task<UnityEngine.Object> LoadAssetInternalAsync<T>(string addressOrLabel) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(addressOrLabel);
        try
        {
            await handle.Task;

            if (!IsSucceeded(handle))
            {
                string error = BuildHandleError($"Failed to load Addressables asset: {addressOrLabel}", handle);
                Exception operationException = GetOperationException(handle);
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw new InvalidOperationException(error, operationException);
            }

            _assetHandles[addressOrLabel] = handle;
            return handle.Result;
        }
        catch
        {
            if (handle.IsValid() && !_assetHandles.ContainsKey(addressOrLabel))
                Addressables.Release(handle);

            throw;
        }
    }

    public async Task<IList<T>> LoadAssetsAsync<T>(string addressOrLabel) where T : UnityEngine.Object
    {
        try
        {
            await EnsureInitializedAsync();

            if (_assetHandles.TryGetValue(addressOrLabel, out var cachedHandle) && cachedHandle.IsValid())
            {
                if (_assetListCache.TryGetValue(addressOrLabel, out object cachedList))
                    return cachedList as IList<T>;
            }

            var handle = Addressables.LoadAssetsAsync<T>(addressOrLabel, (Action<T>)null);
            await handle.Task;

            if (!IsSucceeded(handle))
            {
                ReportAddressablesError(BuildHandleError($"Failed to load Addressables assets: {addressOrLabel}", handle),
                    GetOperationException(handle));

                if (handle.IsValid())
                    Addressables.Release(handle);

                return Array.Empty<T>();
            }

            _assetHandles[addressOrLabel] = handle;
            _assetListCache[addressOrLabel] = handle.Result;
            return handle.Result;
        }
        catch (Exception ex)
        {
            if (!WasInitializationFailureAlreadyReported())
                ReportAddressablesError($"Exception while loading Addressables assets: {addressOrLabel}", ex);

            return Array.Empty<T>();
        }
    }

    public void ReleaseLoadedAsset(string addressOrLabel)
    {
        if (_assetHandles.TryGetValue(addressOrLabel, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
            _assetHandles.Remove(addressOrLabel);
            _assetListCache.Remove(addressOrLabel);
        }
    }

    public void ReleaseAllLoadedAssets()
    {
        foreach (var kv in _assetHandles)
        {
            if (kv.Value.IsValid()) Addressables.Release(kv.Value);
        }
        _assetHandles.Clear();
        _assetListCache.Clear();
    }

    // ----------------------------
    // 3) INSTANTIATE (GameObject) + TRACK INSTANCE HANDLES
    // ----------------------------
    public async Task<GameObject> InstantiateAsync(
        string address,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null,
        bool showErrorPopup = true)
    {
        try
        {
            await EnsureInitializedAsync();

            var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
            await handle.Task;

            if (!IsSucceeded(handle))
            {
                ReportAddressablesError(BuildHandleError($"Failed to instantiate Addressables asset: {address}", handle),
                    GetOperationException(handle),
                    showErrorPopup);

                if (handle.IsValid())
                    Addressables.Release(handle);

                return null;
            }

            var go = handle.Result;
            _instanceHandles[go.GetInstanceID()] = handle;
            return go;
        }
        catch (Exception ex)
        {
            if (!WasInitializationFailureAlreadyReported())
                ReportAddressablesError(
                    $"Exception while instantiating Addressables asset: {address}",
                    ex,
                    showErrorPopup);

            return null;
        }
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;

        int id = instance.GetInstanceID();
        if (ReleaseInstance(id))
            return;

        // Agar handle topilmasa, fallback:
        Destroy(instance);
    }

    public bool ReleaseInstance(int instanceId)
    {
        if (_instanceHandles.TryGetValue(instanceId, out var handle) && handle.IsValid())
        {
            Addressables.ReleaseInstance(handle);
            _instanceHandles.Remove(instanceId);
            return true;
        }

        _instanceHandles.Remove(instanceId);
        return false;
    }

    public void ReleaseAllInstances()
    {
        List<int> ids = new List<int>(_instanceHandles.Count);
        foreach (int id in _instanceHandles.Keys)
            ids.Add(id);

        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            var h = _instanceHandles[id];
            if (h.IsValid()) Addressables.ReleaseInstance(h);
        }
        _instanceHandles.Clear();
    }

    // ----------------------------
    // 4) ENVIRONMENT METHODS (swap)
    // ----------------------------
    /// <summary>
    /// Environment'ni loading panel ortida preload + instantiate qiladi.
    /// Old env bo'lsa - release qiladi.
    /// </summary>
    public async Task<GameObject> LoadEnvironmentAsync(
        string envAddress,
        Transform parent,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 1.5f,
        Vector3? position = null,
        Quaternion? rotation = null)
    {
        // 1) Preload dependencies (cached bo'lsa fake progress)
        bool ok = await PreloadDependenciesAsync(envAddress, onProgress, fakeDurationIfCached);
        if (!ok) return null;

        // 2) Old env'ni unload
        await UnloadCurrentEnvironmentAsync();

        // 3) Instantiate new env
        Vector3 pos = position ?? Vector3.zero;
        Quaternion rot = rotation ?? Quaternion.identity;

        return await InstantiateEnvironmentInternal(envAddress, pos, rot, parent);
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

        return await InstantiateEnvironmentInternal(envAddress, pos, rot, parent);
    }

    private async Task<GameObject> InstantiateEnvironmentInternal(
        string envAddress,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        try
        {
            var handle = Addressables.InstantiateAsync(envAddress, position, rotation, parent);
            await handle.Task;

            if (!IsSucceeded(handle))
            {
                ReportAddressablesError(BuildHandleError($"Failed to load environment: {envAddress}", handle),
                    GetOperationException(handle));

                if (handle.IsValid())
                    Addressables.Release(handle);

                return null;
            }

            CurrentEnvironment = handle.Result;
            _currentEnvHandle = handle;
            _instanceHandles[CurrentEnvironment.GetInstanceID()] = handle;

            return CurrentEnvironment;
        }
        catch (Exception ex)
        {
            if (!WasInitializationFailureAlreadyReported())
                ReportAddressablesError($"Exception while loading environment: {envAddress}", ex);

            return null;
        }
    }

    public Task UnloadCurrentEnvironmentAsync()
    {
        if (CurrentEnvironment == null) return Task.CompletedTask;

        ReleaseInstance(CurrentEnvironment);
        CurrentEnvironment = null;
        _currentEnvHandle = null;

        return Task.CompletedTask;
    }

    private void ReportAddressablesError(string message, Exception exception = null, bool showPopup = true)
    {
        string fullMessage = exception == null || string.IsNullOrWhiteSpace(exception.Message)
            ? message
            : $"{message}\n{exception.Message}";

        if (exception != null)
            Debug.LogException(exception);

        Debug.LogError(fullMessage);

        if (showPopup)
            ShowAddressablesErrorPopup();
    }

    private bool WasInitializationFailureAlreadyReported()
    {
        return !_initialized && _initTask == null;
    }

    private void ShowAddressablesErrorPopup()
    {
        if (!showPopupOnErrors || UIOverlayRoot.I == null)
            return;

        string popupKey = $"{popupTitleTextId}:{popupDescriptionTextId}:{popupButtonTextId}";
        if (_lastPopupMessage == popupKey && Time.unscaledTime - _lastPopupTime < popupCooldownSeconds)
            return;


        _lastPopupMessage = popupKey;
        _lastPopupTime = Time.unscaledTime;

        if (LanguageManager.Instance == null || !LanguageManager.Instance.IsReady)
        {
            UIOverlayRoot.I.Done(
                "Download failed",
                "Could not download required game content. Please check your internet connection and try again.",
                "OK",
                null
            );
            return;
        }

        UIOverlayRoot.I.Done(popupTitleTextId, popupDescriptionTextId, popupButtonTextId, null);
    }

    private static string BuildHandleError(string prefix, AsyncOperationHandle handle)
    {
        if (!handle.IsValid())
            return $"{prefix}\nInvalid Addressables operation handle.";

        Exception operationException = GetOperationException(handle);
        string operationError = operationException != null
            ? operationException.Message
            : $"Status: {handle.Status}";

        return $"{prefix}\n{operationError}";
    }

    private static string BuildHandleError<T>(string prefix, AsyncOperationHandle<T> handle)
    {
        if (!handle.IsValid())
            return $"{prefix}\nInvalid Addressables operation handle.";

        Exception operationException = GetOperationException(handle);
        string operationError = operationException != null
            ? operationException.Message
            : $"Status: {handle.Status}";

        return $"{prefix}\n{operationError}";
    }

    private static bool IsSucceeded(AsyncOperationHandle handle)
    {
        return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
    }

    private static bool IsSucceeded<T>(AsyncOperationHandle<T> handle)
    {
        return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
    }

    private static Exception GetOperationException(AsyncOperationHandle handle)
    {
        return handle.IsValid() ? handle.OperationException : null;
    }

    private static Exception GetOperationException<T>(AsyncOperationHandle<T> handle)
    {
        return handle.IsValid() ? handle.OperationException : null;
    }

    private static string FormatKeys(IList<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return "(empty)";

        System.Text.StringBuilder builder = null;
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (builder == null)
                builder = new System.Text.StringBuilder(key);
            else
                builder.Append(", ").Append(key);
        }

        return builder == null ? "(empty)" : builder.ToString();
    }
}
