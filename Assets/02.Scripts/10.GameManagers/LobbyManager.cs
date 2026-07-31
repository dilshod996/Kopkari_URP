using DG.Tweening;
using GPUInstancerPro.PrefabModule;
using GPUInstancerPro.TerrainModule;
using MalbersAnimations.Controller;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnluckSoftware;
using static Constants;

public class LobbyManager : MonoBehaviour
{

    //public ModalWindowManager detailsPopupManager;
    [Header("Scene Refs")]
    [SerializeField] private Transform environmentRoot;
    private GameObject _currentEnvInstance;
    private string _currentEnvAddress;
    [Header("Tuning")]
    [SerializeField] private float fakeDurationIfCached = 1.2f;
    [SerializeField] private float environmentOverlayCoverSeconds = 0.3f;
    [SerializeField] private float environmentOverlayMinSeconds = 2.5f;


    [Header("Addressable")]
    [SerializeField] private Transform playerSpawnPos;
    [SerializeField] private Transform horseSpawnPos;
    [SerializeField] private GameObject PlayerParent;
    [SerializeField] private GameObject HorseParent;

    [Header("HorseDetails")]
    
    [SerializeField] private ParticleSystem eatParticle;
    [SerializeField] private GameObject foodBowl;
    [SerializeField] private GameObject waterBowl;

    Dictionary<string, Ability> abilityMap = new();
    private Ability eatAbility;
    private Ability drinkAbility;
    private Ability activeAbility;

    [Header("Environment Addressables")]
    [SerializeField] private Transform utovPos;
    private GameObject utov;

    [Header("Player")]
    public GameObject playerPrefab;
    private GameObject playerInstance;
    public MAnimal playerAnimator;

    [Header("Horse")]
    public GameObject horsePrefab;
    private GameObject horseInstance;
    public MAnimal horseAnimator;

    [SerializeField] private GameObject giftPopupPanel;


    [Header("Other room addressables")]
    private List<string> customSceneAddressableAddresses = new List<string>
    {
        "CustomRoomEnvironment",
        "CustomRoomSound",
        "CustomRoomSkybox"
    };

    private List<string> preloadAddresses = new List<string> { };
    private List<string> preloadRacing = new List<string> { Constants.RoomSound.RacingSound};
    private bool isSleeping = false;

    [Header("Legacy Scene GPUI Fallback")]
    [Tooltip("Used only when the instantiated Addressable environment does not contain its own manager.")]
    [SerializeField] private GPUITreeManager treeManager;
    [Tooltip("Used only when the instantiated Addressable environment does not contain its own manager.")]
    [SerializeField] private GPUIDetailManager detailManager;
    [Tooltip("Used only when the instantiated Addressable environment does not contain its own manager.")]
    [SerializeField] private GPUIPrefabManager prefabManager;
    public static event Action<string> OnNameChanged;
    [Header("Lighting")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private StylizedWeatherController weatherController;

    private async void Start()
    {
        SceneLoadManager.Instance?.SetAssetInstantiationFinished(false);

        try
        {
            _currentEnvAddress = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment);
            if (string.IsNullOrEmpty(_currentEnvAddress))
            {
                _currentEnvAddress = Constants.MapNames.Zarafshan;
                PlayerPrefs.SetString(Constants.HomeEnivronments.SelectedEnvironment, _currentEnvAddress);
            }

            if (AddressablesService.Instance == null)
            {
                Debug.LogError("LobbyManager: AddressablesService is missing. Home environment cannot be loaded.");
                SceneLoadManager.Instance?.SetAssetInstantiationFinished(true, succeeded: false);
                return;
            }

            _currentEnvInstance = await AddressablesService.Instance.InstantiateAsync(
                _currentEnvAddress,
                Vector3.zero,
                Quaternion.identity,
                environmentRoot
            );

            if (_currentEnvInstance == null)
            {
                Debug.LogWarning($"LobbyManager: Failed to load home environment '{_currentEnvAddress}'.");
                SceneLoadManager.Instance?.SetAssetInstantiationFinished(true, succeeded: false);
                return;
            }

            RegisterEnvironmentGPUI(_currentEnvInstance.transform);

            // 2) Skybox material load + apply
            await ApplySkyboxByEnvironment(_currentEnvAddress);

            // 3) Directional light color/intensity/rotation apply
            ApplyLightByEnvironment(_currentEnvAddress);
            // 2️⃣ Player spawn
            playerInstance = Instantiate(
                playerPrefab,
                playerSpawnPos.position,
                playerSpawnPos.rotation,
                PlayerParent.transform
            );

            var skinLoader = playerInstance.GetComponentInChildren<PlayerSkinLoader>();
            if (skinLoader != null)
                await skinLoader.ApplyAllSkins();

            // 3️⃣ Horse spawn
            horseInstance = Instantiate(
                horsePrefab,
                horseSpawnPos.position,
                horseSpawnPos.rotation,
                HorseParent.transform
            );

            var horseSkinLoader = horseInstance.GetComponentInChildren<HorseSkinLoader>();
            if (horseSkinLoader != null)
                await horseSkinLoader.ApplyAllSkins();

            // 4️⃣ Scene ready
            SceneLoadManager.Instance?.SetAssetInstantiationFinished(true);

            //HomeMainUI.Instance.RemoveInitialImage();
            HorseAnimGet();
            GetPlayerAnimator();

            UIOverlayRoot.I?.HidePanel(UIPanelType.Home, instant: false);
            SetWeather();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SceneLoadManager.Instance?.SetAssetInstantiationFinished(true, succeeded: false);
        }
    }

    private void OnEnable()
    {
        if (eatAbility != null)
        {
            eatAbility.OnEnter.AddListener(EatAction);
            eatAbility.OnExit.AddListener(StopEat);
        }
        if(drinkAbility != null)
        {
            drinkAbility.OnEnter.AddListener(DrinkAction);
            drinkAbility.OnExit.AddListener(StopDrink);
        }
        FoodShowerPopup.OnWaterDrink += PlayDrink;
        FoodShowerPopup.OnFoodEat += PlayEat;
        
    }
    private void OnDisable()
    {
        FoodShowerPopup.OnWaterDrink -= PlayDrink;
        FoodShowerPopup.OnFoodEat -= PlayEat;

    }

    #region Lobby Environment and Skybox
    private async Task ApplySkyboxByEnvironment(string selectedEnv)
    {
        string skyboxAddress = GetSkyboxAddress(selectedEnv);

        if (string.IsNullOrEmpty(skyboxAddress))
        {
            Debug.LogWarning("Skybox address not found for env: " + selectedEnv);
            return;
        }

        Material skyboxMaterial =
            await AddressablesService.Instance.LoadAssetAsync<Material>(skyboxAddress);

        if (skyboxMaterial == null)
        {
            Debug.LogWarning("Skybox material failed to load: " + skyboxAddress);
            return;
        }

        RenderSettings.skybox = skyboxMaterial;

        // Skybox / reflection / ambient update
        DynamicGI.UpdateEnvironment();
    }

    private string GetSkyboxAddress(string selectedEnv)
    {
        switch (selectedEnv)
        {
            case Constants.MapNames.Zarafshan:
                return Constants.SkyBoxes.ZarafshanSkybox;

            case Constants.MapNames.Registan:
                return Constants.SkyBoxes.RegistanSkybox;

            case Constants.MapNames.Egypt:
                return Constants.SkyBoxes.EgyptSkybox;

            case Constants.MapNames.Kansas:
                return Constants.SkyBoxes.KansasSkybox;

            default:
                Debug.LogWarning("Unknown environment skybox: " + selectedEnv);
                return Constants.SkyBoxes.ZarafshanSkybox;
        }
    }
    private void ApplyLightByEnvironment(string selectedEnv)
    {
        if (directionalLight == null) return;

        switch (selectedEnv)
        {
            case Constants.MapNames.Zarafshan:
                directionalLight.color = new Color32(215, 230, 219, 255); // #D7E6DB
                directionalLight.intensity = 1.1f;
                directionalLight.transform.rotation = Quaternion.Euler(66f, 97f, 0f);
                break;

            case Constants.MapNames.Registan:
                directionalLight.color = new Color(1f, 0.86f, 0.65f);
                directionalLight.intensity = 1.25f;
                directionalLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                break;

            case Constants.MapNames.Egypt:
                directionalLight.color = new Color32(254, 180, 32, 255); // #FEB420
                directionalLight.intensity = 3.5f;
                directionalLight.transform.rotation = Quaternion.Euler(132f, -78.5f, 0f); 
                break;

            case Constants.MapNames.Kansas:
                directionalLight.color = new Color32(254, 255, 138, 255);
                directionalLight.intensity = 1.9f;
                directionalLight.transform.rotation = Quaternion.Euler(132.3f, -71f, 0f);
                break;
        }

        RenderSettings.sun = directionalLight;
    }
    #endregion

    #region Popup va Infolar

    //public void InfoPopup()
    //{
    //    detailsPopupManager.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(44),
    //        LanguageManager.Instance.GetText(45),
    //        LanguageManager.Instance.GetText(1),
    //        LanguageManager.Instance.GetText(2));
    //}

    #endregion

    #region Scene Movements

    public void Jomboy()
    {
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.Jomboy, preloadAddresses);
    }
    public void PastDargom()
    {
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.PastDargom, preloadAddresses);
    }

    public void TrainingRoom()
    {
        
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.Registan, preloadAddresses);
    }
    #endregion

    #region Racing Rooms
    public void FirstRoom()
    {
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.FirstRacing, preloadAddresses);
    }
    public void TrainRacing()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.RacingTutorial, "Welcome to the Game", instant: false, trackSceneProgress: true);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.TrainingRacing, preloadAddresses);
    }
    public void BaxmalRacing()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float currentPower = current.Power;
        float currentCooling = current.Cooling;
        float currentStamina = current.Stamina;
        

        if (currentPower < Constants.HorseConditionNum.Power || currentCooling < Constants.HorseConditionNum.Cool || currentStamina < Constants.HorseConditionNum.Stamina)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            HomeMainUI.Instance?.SHowFoodPanel();
            return;  // Racing boshlanmaydi
        }
        // ;
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, LanguageManager.Instance.GetText(209), instant: false, trackSceneProgress: true);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.SecondRacing, preloadRacing);
    }
    public void EgyptRacing()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float currentPower = current.Power;
        float currentCooling = current.Cooling;
        float currentStamina = current.Stamina;

        if (currentPower < 20 || currentCooling < 10 || currentStamina < 30)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeMainUI.Instance?.SHowFoodPanel();
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            return;  // Racing boshlanmaydi
        }
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, LanguageManager.Instance.GetText(210), instant: false, trackSceneProgress: true);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.EgyptRacing, preloadRacing);
    }
    public void TexasRacing()
    {
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float currentPower = current.Power;
        float currentCooling = current.Cooling;
        float currentStamina = current.Stamina;

        if (currentPower < 20 || currentCooling < 10 || currentStamina < 30)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeMainUI.Instance?.SHowFoodPanel();
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            return;  // Racing boshlanmaydi
        }
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, LanguageManager.Instance.GetText(210), instant: false, trackSceneProgress: true);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.Kansas, preloadRacing);
    }
    #endregion

    #region Player Starting Prefs
    private List<string> GetPreloadMaterialAddresses()
    {
        List<string> preload = new List<string>();

        //PlayerPrefs dan material addresslarini olish
        string helmet = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey);
        string head = PlayerPrefs.GetString(Constants.Player.PlayerHeadKey);
        string faceHair = PlayerPrefs.GetString(Constants.Player.PlayerFaceHairKey);
        string hand = PlayerPrefs.GetString(Constants.Player.PlayerHand);
        string upper = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey);
        string lower = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey);

        //PlayerPrefs dan ot material addresslarini olish

        string horseBody = PlayerPrefs.GetString(Constants.Horse.HorseBodyKey);
        string horseEyes = PlayerPrefs.GetString(Constants.Horse.HorseEyesKey);
        string horseMane = PlayerPrefs.GetString(Constants.Horse.HorseManeKey);
        string horseTail = PlayerPrefs.GetString(Constants.Horse.HorseTailKey);
        string horseReins = PlayerPrefs.GetString(Constants.Horse.HorseReinsKey);
        string horseSaddle = PlayerPrefs.GetString(Constants.Horse.HorseSaddleKey);
        string horseReinsHead = PlayerPrefs.GetString(Constants.Horse.HorseReinsHeadKey);

        preload.Add(head);
        preload.Add(hand);
        preload.Add(faceHair);
        preload.Add(upper);
        preload.Add(lower);
        preload.Add(helmet); ;
        preload.Add(horseBody);
        preload.Add(horseEyes);
        preload.Add(horseMane);
        preload.Add(horseTail);
        preload.Add(horseReins);
        preload.Add(horseSaddle);
        preload.Add(horseReinsHead);
        // Boshqa material addresslarini qo‘shish

        return preload;
    }
    //public void PlayerNameCheck()
    //{
    //    if (!PlayerPrefs.HasKey("username")){
    //        if( playerPrefsPanel != null)
    //        {
    //            playerPrefsPanel.gameObject.SetActive(true);
    //            playerPrefsPanel.UserDataCheck();
    //        }
    //    }
    //}
    private void CheckFirstTimeGift()
    {
        if (!PlayerPrefs.HasKey("GiftGiven"))
        {
            // Sovga berilmagan => faqat Gift Popup ochamiz
            if (giftPopupPanel != null)
                giftPopupPanel.SetActive(true);
        }
        else
        {
            // Sovga allaqachon berilgan => to'g'ridan-to'g'ri NPC selection ni tekshiramiz
            //CheckNPCSelection();
        }
    }

    // GET button bosilganda chaqiriladi
    public void OnGiftCollected()
    {
        // Gift olish logikasi (sovga berish)
        PlayerPrefs.SetInt("GiftGiven", 1);
        PlayerPrefs.Save();

        if (giftPopupPanel != null)
            giftPopupPanel.SetActive(false);

    }

    //SHu yerdan boshlab har bitta prefs save bolganligini korish kerak
    //public void SelectedPrefsCheck(string nameofPrefs)
    //{
    //    switch (nameofPrefs)
    //    {
    //        case "GiftGiven":
    //            CheckFirstTimeGift();
    //            break;
    //        case Constants.Player.UsernameKey:
    //            PlayerNameCheck();
    //            break;
    //        case "horsedata":
    //            playerPrefsPanel.HorseDataCheck();
    //            break;
    //        default:break;
    //    }
    //}

        #endregion


    #region Player Actions
    private void PlayerIdleLook()
    {
        playerAnimator.Mode_Activate(4, 55);
    }
    #endregion

    #region Horse Actions

    private void HorseAnimGet()
    {
        horseAnimator = horseInstance.GetComponent<MAnimal>();
        Mode actionMode = horseAnimator.Mode_Get(4); // Action Mode
        if (actionMode != null)
        {
            eatAbility = actionMode.Abilities.Find(a => a.Name == "Eat");
            drinkAbility = actionMode.Abilities.Find(a => a.Name == "Drink");
            actionMode.OnEnterMode.AddListener(OnEnterAction);
            actionMode.OnExitMode.AddListener(OnExitAction);
        }
        else
        {
            Debug.LogError("❌ Action Mode not found");
        }
    }

    public void PlayEat()
    {
        /// Mana shu yerda food resursini tekshirish kerak
        if (horseAnimator != null && eatAbility != null)
        {
            horseAnimator.Mode_Activate(4, eatAbility.Index);
            PlayerIdleLook();
        }       

    }
    public void EatAction()
    {
        if (eatParticle != null)
            eatParticle.Play();

        if (foodBowl != null)
            foodBowl.SetActive(true);
    }
    public void StopEat()
    {
        if (eatParticle != null)
            eatParticle.Stop();

        if (foodBowl != null)
            foodBowl.SetActive(false);
    }
    public void DrinkAction()
    {
        if (eatParticle != null)
            eatParticle.Play();

        if (waterBowl != null)
            waterBowl.SetActive(true);
    }
    public void StopDrink()
    {
        if (eatParticle != null)
            eatParticle.Stop();

        if (foodBowl != null)
            waterBowl.SetActive(false);
       
    }

    private void OnEnterAction()
    {
        var actionMode = horseAnimator.Mode_Get(4);
        activeAbility = actionMode?.ActiveAbility;

        if (activeAbility == null) return;

        switch (activeAbility.Name)
        {
            case "Eat":
                eatParticle.Play();
                foodBowl.SetActive(true);
                break;

            case "Drink":
                eatParticle.Play();
                waterBowl.SetActive(true);
                break;
        }
    }

    private void OnExitAction()
    {
        if (activeAbility == null) return;

        switch (activeAbility.Name)
        {
            case "Eat":
                eatParticle.Stop();
                foodBowl.SetActive(false);
                HomeMainUI.Instance?.MainUIState(true);
                break;

            case "Drink":
                Debug.Log("✅ DRINK ENDED");
                waterBowl.SetActive(false);
                HomeMainUI.Instance?.MainUIState(true);
                break;
        }

        activeAbility = null; // reset
    }
    public void PlayDrink()
    {
        if (horseAnimator != null && drinkAbility != null)
        {
            // Drink actionni boshlash (Mode 4, Ability 7 - Drink)
            horseAnimator.Mode_Activate(4, drinkAbility.Index);
            PlayerIdleLook();
        }
    }
    public void SleepAction()
    {
        if (!isSleeping)
        {
            // Sleep actionni boshlash (Mode 4, Ability 6 - Sleep)
            horseAnimator.Mode_Activate(4, 6);
            isSleeping = true;
        }
        else
        {
            // Wake up - Mode’ni to‘xtatish
            horseAnimator.Mode_Stop();
            isSleeping = false;
        }
    }
    #endregion

    private void GetPlayerAnimator()
    {
        playerAnimator = playerInstance.GetComponent<MAnimal>();
    }

    #region Changing Environment
    public void ChangeMap(string mapKey)
    {
         SwitchEnvironment(mapKey);
    }

    public async void SwitchEnvironment(string envAddress)
    {
        await SwitchEnvironmentAsync(envAddress);
    }

    public async Task SwitchEnvironmentAsync(string envAddress)
    {
        float overlayStartTime = Time.unscaledTime;
        ShowHomeOverlay();

        try
        {
            await WaitUnscaledSeconds(environmentOverlayCoverSeconds);

            // ✅ 0) Old envni aniq unload qil
            if (_currentEnvInstance != null)
            {
                AddressablesService.Instance.ReleaseInstance(_currentEnvInstance);
                _currentEnvInstance = null;
            }


            // ✅ 1) Yangi envni preload+load
            _currentEnvInstance = await AddressablesService.Instance.LoadEnvironmentAsync(
                envAddress,
                environmentRoot,
                onProgress: null,
                fakeDurationIfCached: fakeDurationIfCached
            );

            if (_currentEnvInstance == null)
            {
                Debug.LogWarning("❌ Environment instantiate failed.");
                return;
            }

            RegisterEnvironmentGPUI(_currentEnvInstance.transform);

            await ApplySkyboxByEnvironment(envAddress);
            ApplyLightByEnvironment(envAddress);

            Teleport(playerInstance.transform, playerSpawnPos);
            Teleport(horseInstance.transform, horseSpawnPos);

            OnNameChanged?.Invoke(envAddress);
            _currentEnvAddress = envAddress;
            ChangeWeather(envAddress);

            await Task.Yield();
        }
        finally
        {
            await WaitForMinimumOverlayTime(overlayStartTime);
            HideHomeOverlay();
        }
    }


    public void TeleportPlayer()
    {
        Teleport(playerInstance.transform, playerSpawnPos);
    }
    public void TeleportHorse()
    {
        Teleport(horseInstance.transform, horseSpawnPos);
    }
    private void Teleport(Transform target, Transform spawn)
    {
        if (target == null || spawn == null) return;
        target.SetPositionAndRotation(spawn.position, spawn.rotation);
    }


    private void ShowHomeOverlay()
    {
        if (UIOverlayRoot.I == null)
            return;

        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, GetHomeOverlayMessage(), instant: false, exclusive: true);
    }

    private void HideHomeOverlay()
    {
        UIOverlayRoot.I?.HidePanel(UIPanelType.Home, instant: false);
    }

    private async Task WaitForMinimumOverlayTime(float overlayStartTime)
    {
        while (Time.unscaledTime - overlayStartTime < environmentOverlayMinSeconds)
            await Task.Yield();
    }

    private async Task WaitUnscaledSeconds(float seconds)
    {
        if (seconds <= 0f)
            return;

        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < seconds)
            await Task.Yield();
    }

    private string GetHomeOverlayMessage()
    {
        if (LanguageManager.Instance != null)
            return LanguageManager.Instance.GetText(192);

        return "Home loading...";
    }


    private void RegisterEnvironmentGPUI(Transform envRoot)
    {
        if (envRoot == null)
            return;

        RegisterEnvironmentGPUIPrefabs(envRoot);
        RegisterEnvironmentGPUITerrains(envRoot);
    }

    private void RegisterEnvironmentGPUIPrefabs(Transform envRoot)
    {
        GPUIPrefab[] instances = envRoot.GetComponentsInChildren<GPUIPrefab>(true);
        GPUIPrefab[] validInstances = instances
            .Where(instance =>
                instance != null &&
                instance.GetPrefabID() != 0 &&
                !instance.IsInstanced)
            .ToArray();

        if (validInstances.Length == 0)
            return;

        GPUIPrefabManager environmentManager =
            envRoot.GetComponentInChildren<GPUIPrefabManager>(true);
        GPUIPrefabManager activeManager =
            environmentManager != null ? environmentManager : prefabManager;

        WarnIfDuplicateManagers(environmentManager, prefabManager, "GPUIPrefabManager");

        if (activeManager == null)
        {
            Debug.LogWarning(
                "LobbyManager: The Addressable environment has GPUI prefab instances but no GPUIPrefabManager.");
            return;
        }

        if (!activeManager.isActiveAndEnabled)
        {
            Debug.LogWarning(
                "LobbyManager: The selected GPUIPrefabManager is inactive. Runtime prefab registration was skipped.");
            return;
        }

        Dictionary<int, int> prototypeIndexByPrefabId = new Dictionary<int, int>();
        Dictionary<int, List<GPUIPrefab>> instancesByPrototypeIndex =
            new Dictionary<int, List<GPUIPrefab>>();

        for (int prototypeIndex = 0;
             prototypeIndex < activeManager.GetPrototypeCount();
             prototypeIndex++)
        {
            prototypeIndexByPrefabId[activeManager.GetPrefabID(prototypeIndex)] = prototypeIndex;
        }

        int unmatchedInstanceCount = 0;
        for (int instanceIndex = 0; instanceIndex < validInstances.Length; instanceIndex++)
        {
            GPUIPrefab instance = validInstances[instanceIndex];
            if (!prototypeIndexByPrefabId.TryGetValue(
                    instance.GetPrefabID(),
                    out int matchingPrototypeIndex))
            {
                unmatchedInstanceCount++;
                continue;
            }

            if (!instancesByPrototypeIndex.TryGetValue(
                    matchingPrototypeIndex,
                    out List<GPUIPrefab> prototypeInstances))
            {
                prototypeInstances = new List<GPUIPrefab>();
                instancesByPrototypeIndex.Add(matchingPrototypeIndex, prototypeInstances);
            }

            prototypeInstances.Add(instance);
        }

        foreach (KeyValuePair<int, List<GPUIPrefab>> pair in instancesByPrototypeIndex)
            activeManager.AddPrefabInstances(pair.Value, pair.Key);

        if (unmatchedInstanceCount > 0)
        {
            Debug.LogWarning(
                $"LobbyManager: {unmatchedInstanceCount} runtime GPUI prefab instance(s) do not have a matching " +
                "prototype on the Addressable environment's GPUIPrefabManager.");
        }

        activeManager.RequireTransformUpdate();
    }

    private void RegisterEnvironmentGPUITerrains(Transform envRoot)
    {
        GPUITerrain[] terrains = envRoot.GetComponentsInChildren<GPUITerrain>(true);
        if (terrains.Length == 0)
            return;

        GPUITreeManager environmentTreeManager =
            envRoot.GetComponentInChildren<GPUITreeManager>(true);
        GPUITreeManager activeTreeManager =
            environmentTreeManager != null ? environmentTreeManager : treeManager;

        WarnIfDuplicateManagers(environmentTreeManager, treeManager, "GPUITreeManager");

        if (activeTreeManager != null && activeTreeManager.isActiveAndEnabled)
        {
            activeTreeManager.AddTerrains(terrains);
            activeTreeManager.RequireUpdate(true);
        }
        else if (environmentTreeManager != null || treeManager != null)
        {
            Debug.LogWarning(
                "LobbyManager: The selected GPUITreeManager is inactive. Runtime tree registration was skipped.");
        }

        GPUIDetailManager environmentDetailManager =
            envRoot.GetComponentInChildren<GPUIDetailManager>(true);
        GPUIDetailManager activeDetailManager =
            environmentDetailManager != null ? environmentDetailManager : detailManager;

        WarnIfDuplicateManagers(environmentDetailManager, detailManager, "GPUIDetailManager");

        if (activeDetailManager != null && activeDetailManager.isActiveAndEnabled)
        {
            activeDetailManager.AddTerrains(terrains);
            activeDetailManager.RequireUpdate(true);
        }
        else if (environmentDetailManager != null || detailManager != null)
        {
            Debug.LogWarning(
                "LobbyManager: The selected GPUIDetailManager is inactive. Runtime detail registration was skipped.");
        }
    }

    private static void WarnIfDuplicateManagers(
        Component environmentManager,
        Component sceneManager,
        string managerName)
    {
        if (environmentManager == null || sceneManager == null || environmentManager == sceneManager)
            return;

        Debug.LogWarning(
            $"LobbyManager: Both the Addressable environment and the Home scene contain a {managerName}. " +
            "The Addressable manager will be used; remove the scene manager after migration to avoid duplicate GPUI processing.");
    }
    #endregion

    #region Avatar Custom Scene ga o'tish
    public void MoveCustomRoom()
    {
        UIOverlayRoot.I.Confirm(222, 221, 93, 94, AvatarCustom, null);
    }
    public void AvatarCustom()
    {
        string message = $"{LanguageManager.Instance.GetText(193)}..."; 
        UIOverlayRoot.I.ShowPanel(UIPanelType.Custom, message, instant: false, exclusive: true);
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.AvatarCustom, customSceneAddressableAddresses);
    }
    #endregion

    #region Wheater Controller
    private void SetWeather()
    {
        if (weatherController == null)
            return;

        string mapname = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment);
        if (mapname == Constants.MapNames.Zarafshan)
        {
            weatherController.ChangeWeather("Lightning");
        }
        else if (mapname == Constants.MapNames.Egypt)
        {
            weatherController.ChangeWeather("Dust");
        }
        else if (mapname == Constants.MapNames.Kansas)
        {
            weatherController.ChangeWeather("Calm");
        }
    }
    public void ChangeWeather(string mapname)
    {
        if (weatherController == null)
            return;

        if (mapname == Constants.MapNames.Zarafshan)
        {
            weatherController.ChangeWeather("Lightning");
        }
        else if (mapname == Constants.MapNames.Egypt)
        {
            weatherController.ChangeWeather("Dust");
        }
        else if (mapname == Constants.MapNames.Kansas)
        {
            weatherController.ChangeWeather("Calm");
        }
    }
    #endregion

    public void DeleteRegistanTutorialProgressForTesting()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteKopkariTutorialProgressForTesting();
            return;
        }

        KopkariTutorialProgress.DeleteAllLocalProgress();
        Debug.LogWarning(
            "Registan tutorial PlayerPrefs were deleted, but DataManager was unavailable for Firebase deletion.");
    }

    public void DeleteRacingTutorialProgressForTesting()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteRacingTutorialProgressForTesting();
            return;
        }

        RacingTutorialProgress.DeleteAllLocalProgress();
        Debug.LogWarning(
            "Racing tutorial PlayerPrefs were deleted, but DataManager was unavailable for Firebase deletion.");
    }

    public void DeleteHomeTutorialProgressForTesting()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteHomeTutorialProgressForTesting();
            return;
        }

        HomeTutorialProgress.DeleteAllLocalProgress();
        Debug.LogWarning(
            "Home tutorial PlayerPrefs were deleted, but DataManager was unavailable for Firebase deletion.");
    }

    private void OnDestroy()
    {
        if (_currentEnvInstance != null)
        {
            AddressablesService.Instance.ReleaseInstance(_currentEnvInstance);
        }
    }
}
