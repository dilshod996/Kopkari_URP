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
    public bool AssetInstantiationSucceeded { get; private set; }
    private static bool _introInitDone;
    private const float HomeInstantiationTimeoutSeconds = 45f;
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
        TrainingRacing,
        SecondRacing, //Zarafshan
        EgyptRacing,
        Kansas,
        Sibir,
        Registan
    }

    public SceneType CurrentSceneType  = SceneType.None;
    public SceneType PreviousSceneType  = SceneType.None;
    private const string RegistanSceneName = "Registon";

    public float loadingTime; // Assign in LoadingScene
    public float fakeDurationIfCached = 5f;
    public bool IsSceneLoading { get; private set; }
    public float LastSceneMoveTime { get; private set; }
    public float CurrentSceneMoveTime => IsSceneLoading
        ? Time.realtimeSinceStartup - sceneMoveStartRealtime
        : LastSceneMoveTime;

    private float sceneMoveStartRealtime;
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

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SyncCurrentSceneType(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        SyncCurrentSceneType(newScene);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncCurrentSceneType(scene);
    }

    private void SyncCurrentSceneType(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.name))
            return;

        if (!TryGetSceneType(scene.name, out SceneType sceneType))
            return;

        if (CurrentSceneType == sceneType)
            return;

        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = sceneType;
    }

    private static string GetUnitySceneName(SceneType sceneType)
    {
        // Keep Beginer as a legacy alias for existing serialized references.
        return sceneType == SceneType.Registan || sceneType == SceneType.Beginer
            ? RegistanSceneName
            : sceneType.ToString();
    }

    private static bool TryGetSceneType(string sceneName, out SceneType sceneType)
    {
        if (sceneName == RegistanSceneName || sceneName == "Registan")
        {
            sceneType = SceneType.Registan;
            return true;
        }

        return Enum.TryParse(sceneName, out sceneType);
    }

    public void LoadSmartScene(SceneType scene, List<string> preloadKeys)
    {
        if (scene == SceneType.Loading || IsSceneLoading)
            return;

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
        if (scene == SceneType.Loading || IsSceneLoading)
            return;

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
        if (!TryBeginSceneMove(targetScene))
            return;

        StartCoroutine(HandleSceneLoad(targetScene, preloadAddresses));
    }
    public void LoadSceneWIthAddressableWithoutAdditive(SceneType targetScene, List<string> preloadAddresses)
    {
        if (!TryBeginSceneMove(targetScene))
            return;

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
        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 100f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);

        if (!IsSuccessful(preloadTask))
        {
            // internet yo‘q (download kerak bo‘lsa) yoki download failed
            // shu yerda popup ko‘rsatib qaytib ketasan
            //IntroManager.Instance?.ShowPopup();
            CancelSceneMove();
            yield break;
        }

        // 2) Target scene ADDITIVE load
        string targetSceneName = GetUnitySceneName(targetScene);
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 3) Active qilish
        Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
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
        CompleteSceneMove();
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
        if (scene == SceneType.Loading || IsSceneLoading)
            return;
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
        if (!TryBeginSceneMove(targetScene))
            return;

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

        if (!IsSuccessful(preloadTask))
        {
            CancelSceneMove();
            yield break;
        }

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

        string targetSceneName = GetUnitySceneName(targetScene);
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        while (!sceneLoadOp.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);
        CompleteSceneMove();
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
        if (!TryBeginSceneMove(newScene))
            return;

        StartCoroutine(LoadSceneDirect(newScene));
    }

    private IEnumerator LoadSceneDirect(SceneType newScene)
    {
        // Fake progress bar uchun
        //AddressablesManager.Instance.loadingTime = 0f;
        StartCoroutine(FakeLoadingTimeProgress());

        // Agar fakeDuration ishlatilsa — kutamiz
        yield return new WaitForSeconds(fakeDurationIfCached);

        // To'g'ridan-to'g'ri sahnani yuklaymiz
        SceneManager.LoadScene(GetUnitySceneName(newScene), LoadSceneMode.Single);
        CompleteSceneMove();
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

        if (!IsSuccessful(preloadTask))
        {
            CancelSceneMove();
            yield break;
        }

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

            if (!IsSuccessful(playerPreloadTask) || !IsSuccessful(horsePreloadTask))
            {
                CancelSceneMove();
                yield break;
            }
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
            if (!IsSuccessful(aiHorsePoolTask))
            {
                CancelSceneMove();
                yield break;
            }
        }


        //// IMPORTANT: reset before target load (agar manager DDOL bo'lsa)
        //AssetInstantiationFinished = false;

        // 3) Target SINGLE
        string targetSceneName = GetUnitySceneName(targetScene);
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 4) Active
        Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        // 5) Instantiation finished
        while (!AssetInstantiationFinished)
            yield return null;

        CurrentSceneType = targetScene;
        SetAssetInstantiationFinished(false);
        CompleteSceneMove();
    }






    public void SetAssetInstantiationFinished(bool status, bool succeeded = true)
    {
        AssetInstantiationFinished = status;
        AssetInstantiationSucceeded = status && succeeded;
    }

    public void LoadScene(SceneType newScene)
    {
        if (!TryBeginSceneMove(newScene))
            return;

        StartCoroutine(LoadSceneWithTransition(newScene));
    }

    private IEnumerator LoadSceneWithTransition(SceneType newScene)
    {
        loadingTime = 0f;

        // Loading scene
        SceneManager.LoadScene(SceneType.Loading.ToString());

        // Fake progress
        StartCoroutine(FakeLoadingTimeProgress());

        yield return new WaitForSeconds(fakeDurationIfCached);

        SceneManager.LoadScene(GetUnitySceneName(newScene), LoadSceneMode.Single);
        CompleteSceneMove();
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

    private bool IsSuccessful(Task<bool> task)
    {
        return task != null
            && task.IsCompleted
            && !task.IsCanceled
            && !task.IsFaulted
            && task.Result;
    }

    private void BeginSceneMove()
    {
        loadingTime = 0f;
        IsSceneLoading = true;
        sceneMoveStartRealtime = Time.realtimeSinceStartup;
        SoundManager.Instance?.PrepareForSceneChange();
    }

    private bool TryBeginSceneMove(SceneType targetScene)
    {
        if (targetScene == SceneType.Loading || IsSceneLoading)
        {
            if (IsSceneLoading)
                Debug.LogWarning($"Ignored scene load request for {targetScene}: another scene is already loading.");

            return false;
        }

        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = targetScene;
        BeginSceneMove();
        return true;
    }

    private void CompleteSceneMove()
    {
        LastSceneMoveTime = CurrentSceneMoveTime;
        IsSceneLoading = false;
        loadingTime = 100f;
        OnSceneLoaded?.Invoke();
    }

    private void CancelSceneMove()
    {
        IsSceneLoading = false;
    }

    #region new Inro Load
    public bool LoadHomeFromIntro(SceneType homeScene, List<string> preloadKeys, Task<bool> existingPreloadTask = null)
    {
        if (!TryBeginSceneMove(homeScene))
            return false;

        StartCoroutine(HandleSceneLoadIntro_ToHome(homeScene, preloadKeys, existingPreloadTask));
        return true;
    }

    private IEnumerator HandleSceneLoadIntro_ToHome(SceneType targetScene, List<string> preloadAddresses, Task<bool> existingPreloadTask = null)
    {
        loadingTime = 0f;
        preloadAddresses ??= new List<string>();

        // ✅ 0) Loading panel show (DontDestroy)
        string loadingMessage = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(192)
            : "Loading Home...";
        UIOverlayRoot.I?.ShowPanel(UIPanelType.Home, loadingMessage, instant: false, exclusive: true);

        if (AddressablesService.Instance == null)
        {
            CancelSceneMove();
            Kopkari.IntroManager.Instance?.HandleHomeLoadFailed("AddressablesService is unavailable.");
            yield break;
        }


        // ✅ 1) Preload (intro paytida)
        Task<bool> preloadTask = existingPreloadTask;
        if (preloadTask == null)
        {
            preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
                preloadAddresses,
                p => loadingTime = p * 100f,
                fakeDurationIfCached,
                showErrorPopup: false
            );
        }

        yield return WaitTask(preloadTask);
        if (!IsSuccessful(preloadTask))
        {
            UIOverlayRoot.I?.HidePanel(UIPanelType.Home, false);
            CancelSceneMove();
            Kopkari.IntroManager.Instance?.HandleHomeLoadFailed(
                "One or more required Home Addressables could not be loaded.");
            yield break;
        }

        // ✅ 2) UI Sounds register — faqat 1 marta
        if (!_introInitDone)
        {
            var uiPairs = new (string address, UISoundType type)[]
            {
                (Constants.UISounds.Click, UISoundType.Click),
                (Constants.UISounds.Confirm, UISoundType.Confirm),
                (Constants.UISounds.Error, UISoundType.Error),
                (Constants.UISounds.Success, UISoundType.Success),
                (Constants.UISounds.PopupOpen, UISoundType.PopupOpen),
                (Constants.UISounds.PopupClose, UISoundType.PopupClose),
            };

            bool allUiSoundsLoaded = SoundManager.Instance != null;
            for (int i = 0; i < uiPairs.Length; i++)
            {
                var (address, type) = uiPairs[i];

                var clipTask = AddressablesService.Instance.LoadAssetAsync<AudioClip>(address);
                yield return WaitTask(clipTask);

                var clip = clipTask.Result;
                if (clip != null && SoundManager.Instance != null)
                    SoundManager.Instance.RegisterUIClip(type, clip);
                else
                    allUiSoundsLoaded = false;
            }

            _introInitDone = allUiSoundsLoaded;
        }

        // ✅ 3) Home scene load (Single)
        SetAssetInstantiationFinished(false);
        var sceneOp = SceneManager.LoadSceneAsync(GetUnitySceneName(targetScene), LoadSceneMode.Single);
        if (sceneOp == null)
        {
            CancelSceneMove();
            Kopkari.IntroManager.Instance?.HandleHomeLoadFailed("The Home scene could not be opened.");
            yield break;
        }

        while (!sceneOp.isDone)
            yield return null;

        // ✅ 4) Scene ichidagi addressable instantiate'lar tugaguncha kutamiz
        float homeReadyDeadline = Time.realtimeSinceStartup + HomeInstantiationTimeoutSeconds;
        while (!AssetInstantiationFinished && Time.realtimeSinceStartup < homeReadyDeadline)
            yield return null;

        if (!AssetInstantiationFinished || !AssetInstantiationSucceeded)
        {
            string reason = !AssetInstantiationFinished
                ? $"Home initialization timed out after {HomeInstantiationTimeoutSeconds:0} seconds."
                : "Home reported that required content failed to initialize.";

            Debug.LogError(reason);
            UIOverlayRoot.I?.HidePanel(UIPanelType.Home, false);
            CancelSceneMove();
            SetAssetInstantiationFinished(false);

            // Intro was unloaded by the Single-mode Home load, so return to a known recoverable scene.
            var recoveryOp = SceneManager.LoadSceneAsync(SceneType.Intro.ToString(), LoadSceneMode.Single);
            if (recoveryOp != null)
            {
                while (!recoveryOp.isDone)
                    yield return null;
            }

            yield break;
        }

        // ✅ 5) Panel hide + flag reset
        //UIOverlayRoot.I.HidePanel(UIPanelType.Home, false);

        SetAssetInstantiationFinished(false);

        // ✅ 6) Endi real loaded bo'ldi
        CompleteSceneMove();
    }
    #endregion

    #region Loading Scene Removed

    public void LoadSceneNew(SceneType scene, List<string> preloadKeys)
    {
        if (!TryBeginSceneMove(scene))
            return;

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
        var op = SceneManager.LoadSceneAsync(GetUnitySceneName(scene), LoadSceneMode.Single);
        while (!op.isDone)
        {
            loadingTime = Mathf.Clamp01(op.progress / 0.9f) * 95f;
            yield return null;
        }

        CompleteSceneMove();
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
        if (!IsSuccessful(preloadTask))
        {
           // UIOverlayRoot.I.HideLoading();
            CancelSceneMove();
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
                p => loadingTime = 80f + p * 10f,
                includeIcons: true
            );

            yield return WaitTask(horsePreloadTask);

            if (!IsSuccessful(playerPreloadTask) || !IsSuccessful(horsePreloadTask))
            {
                //UIOverlayRoot.I.HideLoading();
                CancelSceneMove();
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
                p => loadingTime = 50f + p * 40f
            );

            yield return WaitTask(aiHorsePoolTask);

            if (!IsSuccessful(aiHorsePoolTask))
            {
                CancelSceneMove();
                UIOverlayRoot.I.HideLoading();
                yield break;
            }
        }

        // ✅ 3) Endi target scene load (SINGLE)
        var sceneOp = SceneManager.LoadSceneAsync(GetUnitySceneName(targetScene), LoadSceneMode.Single);
        while (!sceneOp.isDone)
        {
            loadingTime = Mathf.Max(loadingTime, 50f + Mathf.Clamp01(sceneOp.progress / 0.9f) * 45f);
            yield return null;
        }
        // ✅ 3.5) Scene ichidagi async instantiate tugaguncha kutamiz
        loadingTime = Mathf.Max(loadingTime, 95f);
        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);

        // ✅ 4) Scene REAL loaded
        CompleteSceneMove();

        // ✅ 5) Loading panel yopiladi
        UIOverlayRoot.I.HideLoading();
    }
    #endregion

    #region Back Scene or Scene Reload again
    public void ReloadOrBackScene(SceneType newScene)
    {
        if (!TryBeginSceneMove(newScene))
            return;

        StartCoroutine(LoadSceneWithPanel(newScene));
    }

    private IEnumerator LoadSceneWithPanel(SceneType newScene)
    {
        // ✅ Home'ga qaytayotganda HomePanel ko'rsatamiz (yoki Loading panel)
        //UIOverlayRoot.I.ShowPanel(UIPanelType.Home, instant: false, exclusive: true, message: "Home loading...");

        // ✅ Scene load (Single)
        var op = SceneManager.LoadSceneAsync(GetUnitySceneName(newScene), LoadSceneMode.Single);
        while (!op.isDone)
        {
            loadingTime = Mathf.Clamp01(op.progress / 0.9f) * 95f;
            yield return null;
        }

        // ✅ Scene ichidagi async instantiate tugaguncha kutamiz
        loadingTime = Mathf.Max(loadingTime, 95f);
        while (!AssetInstantiationFinished)
            yield return null;

        SetAssetInstantiationFinished(false);

        // ✅ Tayyor bo'ldi -> panelni yopasiz (yoki Home UI'ni ko'rsatishga o'tasiz)
        CompleteSceneMove();
    }

    #endregion

}
