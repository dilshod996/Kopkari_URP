using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using Michsky.UI.ModernUIPack;
using System; // For ProgressBar

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance;


    public bool AssetInstantiationFinished { get; set; } = false;
    private static bool _introInitDone;
    public enum SceneType
    {
        None,
        Intro,
        Loading,
        Home,
        Lobby,
        AvatarCustom,
        Beginer,
        Jomboy,
        PastDargom,
        FirstRacing,
        SecondRacing,
        EgyptRacing
    }

    public SceneType CurrentSceneType  = SceneType.None;
    public SceneType PreviousSceneType  = SceneType.None;

    public float loadingTime; // Assign in LoadingScene
    public float fakeDurationIfCached = 5f;
    HashSet<SceneType> assetAlreadyInstantiated = new();

    public Action OnSceneLoaded;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameObject singletonFolder = GameObject.Find("Singletons");
            if (singletonFolder == null)
            {
                singletonFolder = new GameObject("Singletons");
                DontDestroyOnLoad(singletonFolder);
            }
            transform.SetParent(singletonFolder.transform);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void LoadSmartScene(SceneType scene, List<string> preloadKeys)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = scene;
        OnSceneLoaded?.Invoke(); // Notify that the scene has been loaded
        if (!assetAlreadyInstantiated.Contains(scene))
        {
            LoadSceneWithAddressables(scene, preloadKeys);
            assetAlreadyInstantiated.Add(scene);
            Debug.Log($"Scene {scene} loaded with addressables.");
        }
        else
        {
            LoadScene(scene);
            Debug.Log($"Scene {scene} loaded without addressables.");
        }

    }
    public void LoadSmartSceneWithoutAdditive(SceneType scene, List<string> preloadKeys)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = scene;
        OnSceneLoaded?.Invoke(); // Notify that the scene has been loaded
        if (!assetAlreadyInstantiated.Contains(scene))
        {
            LoadSceneWIthAddressableWithoutAdditive(scene, preloadKeys);
            assetAlreadyInstantiated.Add(scene);
        }
        else
        {
            LoadScene(scene);
        }

    }
    public void LoadSceneWithAddressables(SceneType targetScene, List<string> preloadAddresses)
    {
        StartCoroutine(HandleSceneLoad(targetScene, preloadAddresses));
    }
    public void LoadSceneWIthAddressableWithoutAdditive(SceneType targetScene, List<string> preloadAddresses)
    {
        StartCoroutine(HandleSceneLoadWithoutAdditive(targetScene, preloadAddresses));
    }
    private IEnumerator HandleSceneLoad(SceneType targetScene, List<string> preloadAddresses)
    {
        loadingTime = 0f;

        // 0) Loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneType.Loading.ToString());
        while (!asyncLoad.isDone)
            yield return null;

        yield return null;

        // 1) Addressables preload (cached bo‘lsa fake progress)
        bool preloadOK = true;

        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 100f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);
        preloadOK = preloadTask.Result;

        if (!preloadOK)
        {
            // internet yo‘q (download kerak bo‘lsa) yoki download failed
            // shu yerda popup ko‘rsatib qaytib ketasan
            //IntroManager.Instance?.ShowPopup();
            yield break;
        }

        // 2) Target scene ADDITIVE load
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Additive);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 3) Active qilish
        Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        // 4) Instantiation finished kutish
        while (!AssetInstantiationFinished)
            yield return null;

        // 5) Loading scene unload
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(SceneType.Loading.ToString());
        while (!unloadOp.isDone)
            yield return null;

        SetAssetInstantiationFinished(false);
    }

    //private IEnumerator HandleSceneLoad(SceneType targetScene, List<string> preloadAddresses)
    //{

    //    AddressablesManager.Instance.loadingTime = 0f;
    //    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneType.Loading.ToString());
    //    while (!asyncLoad.isDone)
    //        yield return null;

    //    // Scene loaded, wait a frame for safety
    //    yield return null;

    //    // Start actual addressable preload with progress bar
    //    AsyncOperationHandle preloadHandle = default;
    //    Task<AsyncOperationHandle> preloadTask = AddressablesManager.Instance.PreloadWithProgressBarAsync(
    //        preloadAddresses, fakeDurationIfCached);

    //    while (!preloadTask.IsCompleted)
    //        yield return null;

    //    preloadHandle = preloadTask.Result;

    //    // 1. Target sahifani load qil
    //    AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Additive);
    //    while (!sceneLoadOp.isDone)
    //        yield return null;

    //    // 2. Uni active sahifa deb belgilab qo‘y
    //    Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
    //    // Sahifa load bo‘lguncha kuting
    //    while (!loadedScene.isLoaded)
    //        yield return null;

    //    SceneManager.SetActiveScene(loadedScene);

    //    // 3. Assetlar tugaguncha kut
    //    while (!AssetInstantiationFinished)
    //        yield return null;

    //    // 4. Loading sahifani unload qil
    //    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync("Loading");
    //    while (!unloadOp.isDone)
    //        yield return null;

    //    //CurrentSceneType = targetScene;
    //    SetAssetInstantiationFinished(false); // Reset for future scenes


    //}
    #region Introdan Lobby ga
    public void LoadSmartSceneIntro(SceneType scene, List<string> preloadKeys)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = scene;
        OnSceneLoaded?.Invoke(); // Notify that the scene has been loaded
        if (!assetAlreadyInstantiated.Contains(scene))
        {
            LoadSceneWithAddressablesIntro(scene, preloadKeys);
            assetAlreadyInstantiated.Add(scene);
            Debug.Log($"Scene {scene} loaded with addressables.");
        }
        else
        {
            LoadSceneIntro(scene);
            Debug.Log($"Scene {scene} loaded without addressables.");
        }

    }
    public void LoadSceneWithAddressablesIntro(SceneType targetScene, List<string> preloadAddresses)
    {
        StartCoroutine(HandleSceneLoadIntro(targetScene, preloadAddresses));
    }
    private IEnumerator HandleSceneLoadIntro(SceneType targetScene, List<string> preloadAddresses)
    {
        loadingTime = 0f;

        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 100f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);

        if (!preloadTask.Result)
            yield break;

        var uiPairs = new (string address, UISoundType type)[]
          {
            (Constants.UISounds.Click, UISoundType.Click),
            (Constants.UISounds.Confirm, UISoundType.Confirm),
            (Constants.UISounds.Error, UISoundType.Error),
            (Constants.UISounds.Success, UISoundType.Success),
            (Constants.UISounds.PopupOpen, UISoundType.PopupOpen),
            (Constants.UISounds.PopupClose, UISoundType.PopupClose),
          };

        for (int i = 0; i < uiPairs.Length; i++)
        {
            var (address, type) = uiPairs[i];

            var clipTask = AddressablesService.Instance.LoadAssetAsync<AudioClip>(address);
            yield return WaitTask(clipTask);

            AudioClip clip = clipTask.Result;
            if (clip != null && SoundManager.Instance != null)
                SoundManager.Instance.RegisterUIClip(type, clip);
        }


        Scene oldScene = SceneManager.GetActiveScene();

        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
        while (!sceneLoadOp.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);
    }


    //private IEnumerator HandleSceneLoadIntro(SceneType targetScene, List<string> preloadAddresses)
    //{
    //    AddressablesManager.Instance.loadingTime = 0f;

    //    // 1. Avval Addressables preload (progress bar'ni hozirgi sahnadagi UI orqali ko‘rsatishing mumkin)
    //    Task<AsyncOperationHandle> preloadTask =
    //        AddressablesManager.Instance.PreloadWithProgressBarAsync(preloadAddresses, fakeDurationIfCached);

    //    while (!preloadTask.IsCompleted)
    //        yield return null;

    //    AsyncOperationHandle preloadHandle = preloadTask.Result;

    //    // 2. Joriy active sahnani eslab qolamiz
    //    Scene oldScene = SceneManager.GetActiveScene();

    //    // 3. Target sahnani ADDITIVE rejimda load qilamiz
    //    AsyncOperation sceneLoadOp =
    //        SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);

    //    while (!sceneLoadOp.isDone)
    //        yield return null;

    //    // 4. Target sahna to‘liq load bo‘lguncha kutamiz va uni active qilamiz
    //    Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
    //    while (!loadedScene.isLoaded)
    //        yield return null;

    //    SceneManager.SetActiveScene(loadedScene);

    //    // 5. Addressables instantiation tugashini kutamiz
    //    while (!AssetInstantiationFinished)
    //        yield return null;

    //    // 6. Eski sahnani unload qilamiz (Loading emas, balki oldingi active sahna)
    //    if (oldScene.IsValid())
    //    {
    //        AsyncOperation unloadOld = SceneManager.UnloadSceneAsync(oldScene);
    //        while (!unloadOld.isDone)
    //            yield return null;
    //    }

    //    SetAssetInstantiationFinished(false); // keyingi sahnalar uchun reset
    //}
    public void LoadSceneIntro(SceneType newScene)
    {
        StartCoroutine(LoadSceneDirect(newScene));
    }

    private IEnumerator LoadSceneDirect(SceneType newScene)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = newScene;

        // Fake progress bar uchun
        //AddressablesManager.Instance.loadingTime = 0f;
        StartCoroutine(FakeLoadingTimeProgress());

        // Agar fakeDuration ishlatilsa — kutamiz
        yield return new WaitForSeconds(fakeDurationIfCached);

        // To'g'ridan-to'g'ri sahnani yuklaymiz
        SceneManager.LoadScene(newScene.ToString(), LoadSceneMode.Single);
    }

    #endregion
    private IEnumerator HandleSceneLoadWithoutAdditive(SceneType targetScene, List<string> preloadAddresses)
    {
        loadingTime = 0f;
        preloadAddresses ??= new List<string>();

        // 1) Loading scene SINGLE
        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(SceneType.Loading.ToString(), LoadSceneMode.Single);
        while (!loadingOp.isDone)
            yield return null;

        yield return null;

        // 2) Preload env/sound/etc
        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 50f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);

        if (!preloadTask.Result)
            yield break;

        // 2.5) AvatarCustom bo'lsa — active player barcha skin keys preload
        if (targetScene == SceneType.AvatarCustom)
        {
            // catalog ready: eng ishonchli yo'l
            var ensureCatalogTask = PlayerCatalogProvider.Instance.EnsureCatalogAsync();
            yield return WaitTask(ensureCatalogTask);

            string playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");

            var playerPreloadTask = PlayerCatalogProvider.Instance.PreloadAllForPlayerAsync(
                playerId,
                p => loadingTime = 50f + p * 30f,
                includeIcons: true
            );

            yield return WaitTask(playerPreloadTask);
            string horseId = PlayerPrefs.GetString("ActiveHorseId", "horse_01");
            var horsePreloadTask = PlayerCatalogProvider.Instance.PreloadAllForHorseAsync(
                horseId,
                 p => loadingTime = 80f + p * 20f,
                 includeIcons: true);
            yield return WaitTask(horsePreloadTask);

            if (!playerPreloadTask.Result)
                yield break;
        }
        if (targetScene == SceneType.SecondRacing || targetScene == SceneType.EgyptRacing)
        {
            var ensureCatalogTask = PlayerCatalogProvider.Instance.EnsureCatalogAsync();
            yield return WaitTask(ensureCatalogTask);

            // -------- AI HORSE MATERIAL POOL --------
            string horseId = PlayerPrefs.GetString("ActiveHorseId", "horse_01");

            var slotIds = new List<string>
            {
                "Body",
                "Eyes",
                "Mane",
                "Saddle"
            };

            var aiHorsePoolTask = PlayerCatalogProvider.Instance.PreloadMaterialPoolAsync(
                horseId,
                slotIds,
                p => loadingTime = 50f + p * 50f
            );

            yield return WaitTask(aiHorsePoolTask);
            if (!aiHorsePoolTask.Result)
                yield break;
        }


        //// IMPORTANT: reset before target load (agar manager DDOL bo'lsa)
        //AssetInstantiationFinished = false;

        // 3) Target SINGLE
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 4) Active
        Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        // 5) Instantiation finished
        while (!AssetInstantiationFinished)
            yield return null;

        CurrentSceneType = targetScene;
    }






    public void SetAssetInstantiationFinished(bool status)
    {
        AssetInstantiationFinished = status;
    }

    public void LoadScene(SceneType newScene)
    {
        if (newScene == SceneType.Loading) return;
        StartCoroutine(LoadSceneWithTransition(newScene));
    }

    private IEnumerator LoadSceneWithTransition(SceneType newScene)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = newScene;

        loadingTime = 0f;

        // Loading scene
        SceneManager.LoadScene(SceneType.Loading.ToString());

        // Fake progress
        StartCoroutine(FakeLoadingTimeProgress());

        yield return new WaitForSeconds(fakeDurationIfCached);

        SceneManager.LoadScene(newScene.ToString(), LoadSceneMode.Single);
    }

    private IEnumerator FakeLoadingTimeProgress()
    {
        float timer = 0f;
        float duration = fakeDurationIfCached;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            loadingTime = Mathf.Clamp01(timer / duration) * 100f;
            yield return null;
        }

        loadingTime = 100f;
    }

   
    private IEnumerator WaitTask(Task task)
    {
        while (task != null && !task.IsCompleted)
            yield return null;
    }

    private IEnumerator WaitTask<T>(Task<T> task, Action<T> onCompleted)
    {
        while (task != null && !task.IsCompleted)
            yield return null;

        onCompleted?.Invoke(task.Result);
    }
    #region new Inro Load
    public void LoadHomeFromIntro(SceneType homeScene, List<string> preloadKeys)
    {
        StartCoroutine(HandleSceneLoadIntro_ToHome(homeScene, preloadKeys));
    }

    private IEnumerator HandleSceneLoadIntro_ToHome(SceneType targetScene, List<string> preloadAddresses)
    {
        loadingTime = 0f;
        preloadAddresses ??= new List<string>();

        // ✅ 0) Loading panel show (DontDestroy)
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, "Welcome Back", instant: false, exclusive: true);


        // ✅ 1) Preload (intro paytida)
        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 100f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);
        if (!preloadTask.Result)
        {
            UIOverlayRoot.I.HidePanel(UIPanelType.Home, false);
            yield break;
        }

        // ✅ 2) UI Sounds register — faqat 1 marta
        if (!_introInitDone)
        {
            _introInitDone = true;

            var uiPairs = new (string address, UISoundType type)[]
            {
                (Constants.UISounds.Click, UISoundType.Click),
                (Constants.UISounds.Confirm, UISoundType.Confirm),
                (Constants.UISounds.Error, UISoundType.Error),
                (Constants.UISounds.Success, UISoundType.Success),
                (Constants.UISounds.PopupOpen, UISoundType.PopupOpen),
                (Constants.UISounds.PopupClose, UISoundType.PopupClose),
            };

            for (int i = 0; i < uiPairs.Length; i++)
            {
                var (address, type) = uiPairs[i];

                var clipTask = AddressablesService.Instance.LoadAssetAsync<AudioClip>(address);
                yield return WaitTask(clipTask);

                var clip = clipTask.Result;
                if (clip != null && SoundManager.Instance != null)
                    SoundManager.Instance.RegisterUIClip(type, clip);
            }
        }

        // ✅ 3) Home scene load (Single)
        var sceneOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
        while (!sceneOp.isDone)
            yield return null;

        // ✅ 4) Scene ichidagi addressable instantiate'lar tugaguncha kutamiz
        while (!AssetInstantiationFinished)
            yield return null;

        // ✅ 5) Panel hide + flag reset
        //UIOverlayRoot.I.HidePanel(UIPanelType.Home, false);

        SetAssetInstantiationFinished(false);

        // ✅ 6) Endi real loaded bo'ldi deb event chaqirsangiz shu yerda
        OnSceneLoaded?.Invoke();
    }
    #endregion

    #region Loading Scene Removed

    public void LoadSceneNew(SceneType scene, List<string> preloadKeys)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = scene;

        // ❌ bu yerda OnSceneLoaded chaqirilmaydi (hali yuklanmadi)

        if (!assetAlreadyInstantiated.Contains(scene))
        {
            LoadSceneCoroutine(scene, preloadKeys);
            assetAlreadyInstantiated.Add(scene);
        }
        else
        {
            // Agar siz "cached scene" deb o'ylab to'g'ridan-to'g'ri load qilsangiz
            // bu ham Single bo'lgani uchun baribir load bo'ladi.
            StartCoroutine(LoadOnlySceneSingle(scene));
        }
    }

    private IEnumerator LoadOnlySceneSingle(SceneType scene)
    {
        var op = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        OnSceneLoaded?.Invoke();
    }
    private void LoadSceneCoroutine(SceneType targetScene, List<string> preloadAddresses)
    {
        StartCoroutine(HandleScene(targetScene, preloadAddresses));
    }

    private IEnumerator HandleScene(SceneType targetScene, List<string> preloadAddresses)
    {
        loadingTime = 0f;
        preloadAddresses ??= new List<string>();

        // ✅ 0) Loading panel allaqachon Show bo'lgan (buttonda),
        // xohlasangiz bu yerda ham safety uchun:
        //UIOverlayRoot.I.ShowLoading();

        // ✅ 1) Preload env/sound/etc
        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 50f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);
        if (!preloadTask.Result)
        {
           // UIOverlayRoot.I.HideLoading();
            yield break;
        }

        // ✅ 2) Sizdagi extra preloadlar (o'zgarmaydi)
        if (targetScene == SceneType.AvatarCustom)
        {
            var ensureCatalogTask = PlayerCatalogProvider.Instance.EnsureCatalogAsync();
            yield return WaitTask(ensureCatalogTask);

            string playerId = PlayerPrefs.GetString("ActivePlayerId", "player_01");

            var playerPreloadTask = PlayerCatalogProvider.Instance.PreloadAllForPlayerAsync(
                playerId,
                p => loadingTime = 50f + p * 30f,
                includeIcons: true
            );

            yield return WaitTask(playerPreloadTask);

            string horseId = PlayerPrefs.GetString("ActiveHorseId", "horse_01");

            var horsePreloadTask = PlayerCatalogProvider.Instance.PreloadAllForHorseAsync(
                horseId,
                p => loadingTime = 80f + p * 20f,
                includeIcons: true
            );

            yield return WaitTask(horsePreloadTask);

            if (!playerPreloadTask.Result || !horsePreloadTask.Result)
            {
                //UIOverlayRoot.I.HideLoading();
                yield break;
            }
        }

        if (targetScene == SceneType.SecondRacing || targetScene == SceneType.EgyptRacing)
        {
            var ensureCatalogTask = PlayerCatalogProvider.Instance.EnsureCatalogAsync();
            yield return WaitTask(ensureCatalogTask);

            string horseId = PlayerPrefs.GetString("ActiveHorseId", "horse_01");
            var slotIds = new List<string> { "Body", "Eyes", "Mane", "Saddle" };

            var aiHorsePoolTask = PlayerCatalogProvider.Instance.PreloadMaterialPoolAsync(
                horseId,
                slotIds,
                p => loadingTime = 50f + p * 50f
            );

            yield return WaitTask(aiHorsePoolTask);

            if (!aiHorsePoolTask.Result)
            {
                UIOverlayRoot.I.HideLoading();
                yield break;
            }
        }

        // ✅ 3) Endi target scene load (SINGLE)
        var sceneOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
        while (!sceneOp.isDone)
            yield return null;
        // ✅ 3.5) Scene ichidagi async instantiate tugaguncha kutamiz
        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);

        // ✅ 4) Scene REAL loaded
        OnSceneLoaded?.Invoke();

        // ✅ 5) Loading panel yopiladi
        UIOverlayRoot.I.HideLoading();
    }
    #endregion

    #region Back Scene or Scene Reload again
    public void ReloadOrBackScene(SceneType newScene)
    {
        StartCoroutine(LoadSceneWithPanel(newScene));
    }

    private IEnumerator LoadSceneWithPanel(SceneType newScene)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = newScene;

        loadingTime = 0f;

        // ✅ Home'ga qaytayotganda HomePanel ko'rsatamiz (yoki Loading panel)
        //UIOverlayRoot.I.ShowPanel(UIPanelType.Home, instant: false, exclusive: true, message: "Home loading...");

        // ✅ Scene load (Single)
        var op = SceneManager.LoadSceneAsync(newScene.ToString(), LoadSceneMode.Single);
        while (!op.isDone)
            yield return null;

        // ✅ Scene ichidagi async instantiate tugaguncha kutamiz
        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);

        // ✅ Tayyor bo'ldi -> panelni yopasiz (yoki Home UI'ni ko'rsatishga o'tasiz)

        OnSceneLoaded?.Invoke();
    }

    #endregion

}
