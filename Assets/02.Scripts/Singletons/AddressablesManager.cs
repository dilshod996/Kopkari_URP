using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using Kopkari;
using Michsky.UI.ModernUIPack;

public class AddressablesManager : MonoBehaviour
{
    public static AddressablesManager Instance { get; private set; }

    private Task initializationTask;

    public float loadingTime; // Assign in LoadingScene
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private async void Start()
    {
        await EnsureInitialized();
    }

    //0.Internetni tekshirish
    public bool HasInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
    public async Task<bool> EnsureInitialized()
    {
        if (initializationTask != null)
        {
            await initializationTask;
            return true;
        }

        if (!HasInternetConnection())
        {
            IntroManager.Instance?.ShowPopup();
            return false;
        }

        initializationTask = Addressables.InitializeAsync().Task;
        await initializationTask;
        Debug.Log("✅ Addressables Initialized");
        return true;
    }


    // ✅ 1. Assetni Address orqali yuklash (masalan, IntroVideo)
    public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        var handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result;

        Debug.LogError("Failed to load: " + address);
        return null;
    }
    public async Task<T> LoadAssetCachedAsync<T>(string address) where T : UnityEngine.Object
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(address).Task;
        if (size > 0)
        {
            Debug.LogWarning($"⚠️ Asset '{address}' is not cached. Skipping load.");
            return null;
        }

        AsyncOperationHandle<T> handle;
        try
        {
            handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Exception while loading asset '{address}': {e.Message}");
            return null;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            return handle.Result;
        }
        else
        {
            Debug.LogError($"❌ Failed to load addressable asset: {address}");
            return null;
        }
    }

    public async Task<T> LoadAssetSmartAsync<T>(string address, Action<float> onProgress = null,float fakeDurationIfCached = 5f) where T : UnityEngine.Object
    {
        //0.Initialize qilish agar bolmagan bolsa
        if (!await EnsureInitialized())
            return null;
        // 1. Download hajmini tekshiramiz
        long size = await Addressables.GetDownloadSizeAsync(address).Task;
        bool isCached = (size == 0);

        if (isCached)
        {
            // 2. Cached bo‘lsa — fake progress (masalan, 5 sekunda 100%)
            float timer = 0f;

            while (timer < fakeDurationIfCached)
            {
                timer += Time.deltaTime;
                onProgress?.Invoke(Mathf.Clamp01(timer / fakeDurationIfCached));
                await Task.Yield();
            }
        }

        // 3. Assetni yuklaymiz (hatto cached bo‘lsa ham)
        var handle = Addressables.LoadAssetAsync<T>(address);

        while (!handle.IsDone)
        {
            if (!isCached) // real progress faqat cache bo‘lmasa
                onProgress?.Invoke(handle.PercentComplete);
            await Task.Yield();
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
            return handle.Result;

        Debug.LogError("Failed to load Addressable: " + address);
        return null;
    }
    /// <summary>
    /// Bu funksiya Addressables orqali assetni yuklaydi va progress barni yangilaydi.Bu asosan single bolgan assetlar uchun.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="address"></param>
    /// <param name="onProgress"></param>
    /// <param name="fakeDurationIfCached"></param>
    /// <returns></returns>
    public async Task<AsyncOperationHandle<T>> LoadAssetWithHandleAsync<T>(
            string address,
            Action<float> onProgress = null,
            float fakeDurationIfCached = 5f
    ) where T : UnityEngine.Object
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(address).Task;
        bool isCached = (size == 0);

        if (isCached)
        {
            Debug.Log("Cached assets found. Showing fake progress. INTRO");
            // 2. Cached bo‘lsa — fake progress (masalan, 5 sekunda 100%)
            float timer = 0f;

            while (timer < fakeDurationIfCached)
            {
                timer += Time.deltaTime;
                onProgress?.Invoke(Mathf.Clamp01(timer / fakeDurationIfCached));
                await Task.Yield();
            }
        }

        var handle = Addressables.LoadAssetAsync<T>(address);

        while (!handle.IsDone)
        {
            if (!isCached)
                onProgress?.Invoke(handle.PercentComplete);
            await Task.Yield();
        }

        return handle; 
    }

    public async Task<List<AsyncOperationHandle<T>>> LoadAssetsWithHandlesAsync<T>(
        List<string> addresses,
        Action<float> onProgress = null,
        float fakeDurationIfCached = 5f
    ) where T : UnityEngine.Object
    {
        if (!await EnsureInitialized())
            return null;

        var handles = new List<AsyncOperationHandle<T>>();
        int count = addresses.Count;

        for (int i = 0; i < count; i++)
        {
            string address = addresses[i];

            long size = await Addressables.GetDownloadSizeAsync(address).Task;
            bool isCached = size == 0;

            if (isCached)
            {
                Debug.Log($"📦 [{address}] is cached. Showing fake progress...");
                float timer = 0f;

                while (timer < fakeDurationIfCached)
                {
                    timer += Time.deltaTime;
                    float progress = Mathf.Clamp01(timer / fakeDurationIfCached);
                    onProgress?.Invoke(((i + progress) / count));
                    await Task.Yield();
                }
            }

            var handle = Addressables.LoadAssetAsync<T>(address);

            while (!handle.IsDone)
            {
                if (!isCached)
                {
                    float progress = handle.PercentComplete;
                    onProgress?.Invoke(((i + progress) / count));
                }
                await Task.Yield();
            }

            handles.Add(handle);
            onProgress?.Invoke((float)(i + 1) / count); // asset to‘liq yuklandi
        }

        return handles;
    }


    /// <summary>
    /// Bu funksiya Addressables orqali ko‘p assetlarni yuklaydi va progress barni yangilaydi.
    /// hamda cached bo‘lsa, fake progress ko‘rsatadi. Bu faqatgina download qiladi , yuklamaydi.
    /// </summary>
    /// <param name="addresses"></param>
    /// <param name="process"></param>
    /// <param name="fakeDurationIfCached"></param>
    /// <returns></returns>
    public async Task<AsyncOperationHandle> PreloadWithProgressBarAsync(
    List<string> addresses,
    float fakeDurationIfCached = 5f)
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(addresses).Task;
        bool isCached = size == 0;

        loadingTime = 0f;

        if (isCached)
        {
            Debug.Log("📦 Cached assets found. Showing fake progress...");

            float timer = 0f;
            while (timer < fakeDurationIfCached)
            {
                timer += 0.05f;
                float percent = Mathf.Clamp01(timer / fakeDurationIfCached) * 100f;
               // Debug.Log($"Fake progress: {percent}%");
                loadingTime = percent;

                await Task.Delay(50);
            }

            //loadingTime = 100f;
            return default;
        }

        AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(addresses, Addressables.MergeMode.Union);

        while (!downloadHandle.IsDone)
        {
            float percent = downloadHandle.PercentComplete * 100f;
            loadingTime = percent;

            await Task.Yield();
        }

        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("✅ Addressables download complete.");
        }
        else
        {
            Debug.LogError("❌ Addressables download failed.");
        }

        if (downloadHandle.IsValid())
        {
            Addressables.Release(downloadHandle);
            Debug.Log("📦 Downloaded bundles released.");
        }

        return downloadHandle;
    }

    //public async Task<AsyncOperationHandle> PreloadWithProgressBarAsync(
    //     List<string> addresses,
    //     float process,
    //     float fakeDurationIfCached = 5f)
    //{
    //    await EnsureInitialized();

    //    //progressBar.isOn = false;

    //    long size = await Addressables.GetDownloadSizeAsync(addresses).Task;
    //    bool isCached = size == 0;

    //    AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(addresses, Addressables.MergeMode.Union);

    //    if (isCached)
    //    {
    //        Debug.Log("Cached assets found. Showing fake progress.LOBBY");
    //        float timer = 0f;
    //        while (timer < fakeDurationIfCached)
    //        {
    //            timer += 0.05f;
    //            float percent = Mathf.Clamp01(timer / fakeDurationIfCached) * 100f;
    //            process = percent; //progressBar.currentPercent = percent;
    //            loadingTime = percent;
    //            //progressBar.currentPercent = percent;
    //            //progressBar.UpdateUI();
    //            await Task.Delay(50);
    //        }

    //        return downloadHandle; // Even if it's cached, we return the handle
    //    }

    //    while (!downloadHandle.IsDone)
    //    {
    //        float percent = downloadHandle.PercentComplete * 100f;
    //        process = percent; //progressBar.currentPercent = percent;
    //        loadingTime = percent;
    //        Debug.Log($"Progress: {loadingTime}%");
    //        //progressBar.currentPercent = percent;
    //        //progressBar.UpdateUI();
    //        await Task.Yield();
    //    }

    //    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
    //    {
    //        Debug.Log("✅ Addressables download complete");
    //    }
    //    else
    //    {
    //        Debug.LogError("❌ Addressables download failed");
    //    }
    //    if (downloadHandle.IsValid())
    //    {
    //        Addressables.Release(downloadHandle);
    //        Debug.Log("📦 Bundles released");
    //    }
    //    return downloadHandle;
    //}

    /// <summary>
    /// Bu funksiya Addressables orqali assetni yuklaydi va uni instantiate qiladi. Bu funksiya player horse uchun qilingan qolgan static assetlar uchun pastdagi...
    /// </summary>
    /// <param name="address"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public async Task<GameObject> LoadAndInstantiateCachedAsync(string address, Vector3? position = null, Quaternion? rotation = null, Transform parent = null)
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(address).Task;
        if (size > 0)
        {
            Debug.LogWarning($"⚠️ Asset '{address}' is not cached. Skipping load and instantiate.");
            return null;
        }

        AsyncOperationHandle<GameObject> loadHandle;

        try
        {
            loadHandle = Addressables.LoadAssetAsync<GameObject>(address);
            await loadHandle.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Exception while loading '{address}': {e.Message}");
            return null;
        }

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"❌ Failed to load addressable: {address}");
            return null;
        }

        Vector3 spawnPos = position ?? Vector3.zero;
        Quaternion spawnRot = rotation ?? Quaternion.identity;

        GameObject instance = Instantiate(loadHandle.Result, spawnPos, spawnRot, parent);

        return instance;
    }

    /// <summary>
    /// Mana bu ammal qiladi static assetlar uchun.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public async Task<T> InstantiateCachedAsync<T>(string address, Vector3? position = null, Quaternion? rotation = null, Transform parent = null) where T : UnityEngine.Object
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(address).Task;

        if (size > 0)
        {
            Debug.LogError($"⚠️ Asset '{address}' is not cached. Skipping.");
            return null;
        }

        // 🧠 Agar bu GameObject bo‘lsa → Instantiate qilamiz
        if (typeof(T) == typeof(GameObject))
        {
            Vector3 spawnPos = position ?? Vector3.zero;
            Quaternion spawnRot = rotation ?? Quaternion.identity;

            var handle = Addressables.InstantiateAsync(address, spawnPos, spawnRot, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result as T;

            Debug.LogError($"❌ Failed to instantiate GameObject: {address}");
            return null;
        }
        else
        {
            // ✅ Boshqa turdagi assetlar uchun — faqat LoadAssetAsync
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            Debug.LogError($"❌ Failed to load asset of type {typeof(T).Name}: {address}");
            return null;
        }
    }


    // ✅ 3. Asset’ni release qilish
    public void ReleaseAsset<T>(T asset) where T : UnityEngine.Object
    {
        Addressables.Release(asset);
    }

    public async Task PrintAssetDebugInfo(string address)
    {
        await EnsureInitialized();

        long size = await Addressables.GetDownloadSizeAsync(address).Task;
        bool isCached = size == 0;

        Debug.Log($"🔍 Addressables Diagnostic:");
        Debug.Log($"- Address: {address}");
        Debug.Log($"- Cached: {(isCached ? "✅ Yes" : "❌ No")}");
        Debug.Log($"- Estimated Download Size: {size} bytes");
    }
}
