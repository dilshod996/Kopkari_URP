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
        SecondRacing
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

        AddressablesManager.Instance.loadingTime = 0f;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneType.Loading.ToString());
        while (!asyncLoad.isDone)
            yield return null;

        // Scene loaded, wait a frame for safety
        yield return null;

        // Start actual addressable preload with progress bar
        AsyncOperationHandle preloadHandle = default;
        Task<AsyncOperationHandle> preloadTask = AddressablesManager.Instance.PreloadWithProgressBarAsync(
            preloadAddresses, fakeDurationIfCached);

        while (!preloadTask.IsCompleted)
            yield return null;

        preloadHandle = preloadTask.Result;

        // 1. Target sahifani load qil
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Additive);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 2. Uni active sahifa deb belgilab qo‘y
        Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        // Sahifa load bo‘lguncha kuting
        while (!loadedScene.isLoaded)
            yield return null;

        SceneManager.SetActiveScene(loadedScene);

        // 3. Assetlar tugaguncha kut
        while (!AssetInstantiationFinished)
            yield return null;

        // 4. Loading sahifani unload qil
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync("Loading");
        while (!unloadOp.isDone)
            yield return null;

        //CurrentSceneType = targetScene;
        SetAssetInstantiationFinished(false); // Reset for future scenes


    }
    private IEnumerator HandleSceneLoadWithoutAdditive(SceneType targetScene, List<string> preloadAddresses)
    {
        // 1. Oldingi sahifani saqlaymiz
        //PreviousSceneType = CurrentSceneType;
        //CurrentSceneType = SceneType.Loading;
        AddressablesManager.Instance.loadingTime = 0f;
        // 2. Loading sahifani to‘liq yuklaymiz (SceneMode.Single)
        AsyncOperation loadingOp = SceneManager.LoadSceneAsync(SceneType.Loading.ToString(), LoadSceneMode.Single);
        while (!loadingOp.isDone)
            yield return null;

        // 3. Bir frame kutamiz
        yield return null;

        // 4. Addressable preloadni boshlaymiz

        Task<AsyncOperationHandle> preloadTask = AddressablesManager.Instance.PreloadWithProgressBarAsync(
            preloadAddresses, fakeDurationIfCached);

        while (!preloadTask.IsCompleted)
            yield return null;

        var preloadHandle = preloadTask.Result;

        // 5. Target sahifani yuklaymiz (Single: Loading sahifasini o‘chiradi)
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);
        while (!sceneLoadOp.isDone)
            yield return null;

        // 6. Sahifa yuklanguncha kutamiz
        Scene loadedScene = SceneManager.GetSceneByName(targetScene.ToString());
        while (!loadedScene.isLoaded)
            yield return null;

        // 7. Uni active sahifa deb belgilaymiz
        SceneManager.SetActiveScene(loadedScene);

        // 8. Agar Player va Horse instantiate qilinishi kutilayotgan bo‘lsa
        while (!AssetInstantiationFinished)
            yield return null;

        // 9. Yangi sahifaga o‘tamiz
        CurrentSceneType = targetScene;

        // 10. Keyingi sahifalar uchun flagni reset qilish mumkin (agar kerak bo‘lsa)
        // SetAssetInstantiationFinished(false);
    }



    public void SetAssetInstantiationFinished(bool status)
    {
        AssetInstantiationFinished = status;
    }



    ///test
    public void LoadScene(SceneType newScene)
    {
        if (newScene == SceneType.Loading)
            return;

        StartCoroutine(LoadSceneWithTransition(newScene));
    }

    private IEnumerator LoadSceneWithTransition(SceneType newScene)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = SceneType.Loading;
        SceneManager.LoadScene(SceneType.Loading.ToString());
        AddressablesManager.Instance.loadingTime = 0f; // 💡 boshlanishida 0
        StartCoroutine(FakeLoadingTimeProgress());     // 💡 loadingTime ni sekin ko‘taradi
        // SoundManager.Instance.StopMusicEvent();
        yield return new WaitForSeconds(fakeDurationIfCached);

        SceneManager.LoadScene(newScene.ToString());
        CurrentSceneType = newScene;
    }
    IEnumerator FakeLoadingTimeProgress()
    {
        float timer = 0f;
        float duration = fakeDurationIfCached;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            AddressablesManager.Instance.loadingTime = Mathf.Clamp01(timer / duration) * 100f;
            yield return null;
        }

        AddressablesManager.Instance.loadingTime = 100f;
    }
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
}
