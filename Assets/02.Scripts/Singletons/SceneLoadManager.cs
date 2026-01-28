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
        {
           // IntroManager.Instance?.ShowPopup();
            yield break;
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

        // Single bo‘lgani uchun old scene unload shart emas (u allaqachon ketgan bo‘ladi),
        // lekin qoldirsang ham zarar qilmaydi. Xohlasang olib tashla.
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

        // 1) Loading scene SINGLE
        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(SceneType.Loading.ToString(), LoadSceneMode.Single);
        while (!loadingOp.isDone)
            yield return null;

        yield return null;

        // 2) Preload
        var preloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
            preloadAddresses,
            p => loadingTime = p * 100f,
            fakeDurationIfCached
        );

        yield return WaitTask(preloadTask);

        if (!preloadTask.Result)
        {
            //IntroManager.Instance?.ShowPopup();
            yield break;
        }

        // 3) Target SINGLE (Loading sahnasini almashtiradi)
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

    //private IEnumerator HandleSceneLoadWithoutAdditive(SceneType targetScene, List<string> preloadAddresses)
    //{
    //    // 1. Oldingi sahifani saqlaymiz
    //    //PreviousSceneType = CurrentSceneType;
    //    //CurrentSceneType = SceneType.Loading;
    //    AddressablesManager.Instance.loadingTime = 0f;
    //    // 2. Loading sahifani to‘liq yuklaymiz (SceneMode.Single)
    //    AsyncOperation loadingOp = SceneManager.LoadSceneAsync(SceneType.Loading.ToString(), LoadSceneMode.Single);
    //    while (!loadingOp.isDone)
    //        yield return null;

    //    // 3. Bir frame kutamiz
    //    yield return null;

    //    // 4. Addressable preloadni boshlaymiz

    //    Task<AsyncOperationHandle> preloadTask = AddressablesManager.Instance.PreloadWithProgressBarAsync(
    //        preloadAddresses, fakeDurationIfCached);

    //    while (!preloadTask.IsCompleted)
    //        yield return null;

    //    var preloadHandle = preloadTask.Result;

    //    // 5. Target sahifani yuklaymiz (Single: Loading sahifasini o‘chiradi)
    //    AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
    //    while (!sceneLoadOp.isDone)
    //        yield return null;

    //    // 6. Sahifa yuklanguncha kutamiz
    //    Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
    //    while (!loadedScene.isLoaded)
    //        yield return null;

    //    // 7. Uni active sahifa deb belgilaymiz
    //    SceneManager.SetActiveScene(loadedScene);

    //    // 8. Agar Player va Horse instantiate qilinishi kutilayotgan bo‘lsa
    //    while (!AssetInstantiationFinished)
    //        yield return null;

    //    // 9. Yangi sahifaga o‘tamiz
    //    CurrentSceneType = targetScene;

    //    // 10. Keyingi sahifalar uchun flagni reset qilish mumkin (agar kerak bo‘lsa)
    //    // SetAssetInstantiationFinished(false);
    //}



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

    ///test
    //public void LoadScene(SceneType newScene)
    //{
    //    if (newScene == SceneType.Loading)
    //        return;

    //    StartCoroutine(LoadSceneWithTransition(newScene));
    //}

    //private IEnumerator LoadSceneWithTransition(SceneType newScene)
    //{
    //    PreviousSceneType = CurrentSceneType;
    //    CurrentSceneType = newScene;
    //    SceneManager.LoadScene(SceneType.Loading.ToString());
    //    AddressablesManager.Instance.loadingTime = 0f; // 💡 boshlanishida 0
    //    StartCoroutine(FakeLoadingTimeProgress());     // 💡 loadingTime ni sekin ko‘taradi
    //    // SoundManager.Instance.StopMusicEvent();
    //    yield return new WaitForSeconds(fakeDurationIfCached);

    //    SceneManager.LoadScene(newScene.ToString());

    //}
    //IEnumerator FakeLoadingTimeProgress()
    //{
    //    float timer = 0f;
    //    float duration = fakeDurationIfCached;

    //    while (timer < duration)
    //    {
    //        timer += Time.deltaTime;
    //        AddressablesManager.Instance.loadingTime = Mathf.Clamp01(timer / duration) * 100f;
    //        yield return null;
    //    }

    //    AddressablesManager.Instance.loadingTime = 100f;
    //}
    //Single scene load uchun ishlaydi lekin instantiate objectlar borligi uchun ux ga tasiri juda katta hisoblanadi

    //private IEnumerator HandleSceneLoad(SceneType targetScene, List<string> preloadAddresses)
    //{
    //    PreviousSceneType = CurrentSceneType;
    //    CurrentSceneType = SceneType.Loading;

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

    //    // Load the target scene
    //    AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString());
    //    while (!sceneLoadOp.isDone)
    //        yield return null;

    //    CurrentSceneType = targetScene;

    //}
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

}
