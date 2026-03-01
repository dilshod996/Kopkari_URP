using DG.Tweening;
using GPUInstancerPro.PrefabModule;
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

public class LobbyManager : MonoBehaviour
{

    public ModalWindowManager detailsPopupManager;
    [Header("Scene Refs")]
    [SerializeField] private Transform environmentRoot;
    private GameObject _currentEnvInstance;
    private string _currentEnvAddress;
    [Header("Tuning")]
    [SerializeField] private float fakeDurationIfCached = 1.2f;
    [SerializeField] private RectTransform loadingRT;
    [SerializeField] private float wipeDuration = 1f;
    [SerializeField] private Ease wipeEase = Ease.OutExpo;

    private Tween loadingTween;
    private bool isLoadingVisible;


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
    private List<string> customSceneAddressableAddresses = new List<string> { "CustomRoomEnvironment", "CustomRoomSound" };

    private List<string> preloadAddresses = new List<string> { };
    private List<string> preloadRacing = new List<string> { Constants.RoomSound.RacingSound};
    private bool isSleeping = false;

    [Header("GPUI managers")]
    [SerializeField] GPUInstancerPro.TerrainModule.GPUITreeManager treeManager;
    [SerializeField] GPUInstancerPro.TerrainModule.GPUIDetailManager detailManager;
    [SerializeField] private GPUIPrefabManager prefabManager;
    public static event Action<string> OnNameChanged;

    private async void Start()
    {
        SceneLoadManager.Instance.SetAssetInstantiationFinished(false);
        _currentEnvAddress = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment);
        _currentEnvInstance = await AddressablesService.Instance.InstantiateAsync(
            _currentEnvAddress,
            Vector3.zero,
            Quaternion.identity,
            environmentRoot
        );

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
        RegisterEnvPrefabs(_currentEnvInstance.transform);

        // 4️⃣ Scene ready
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);

        //HomeMainUI.Instance.RemoveInitialImage();
        HorseAnimGet();
        GetPlayerAnimator();

        RoomSound();
        UIOverlayRoot.I.HidePanel(UIPanelType.Home, instant: false);
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

    #region Popup va Infolar

    public void InfoPopup()
    {
        detailsPopupManager.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(44),
            LanguageManager.Instance.GetText(45),
            LanguageManager.Instance.GetText(1),
            LanguageManager.Instance.GetText(2));
    }

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
        
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.Beginer, preloadAddresses);
    }
    #endregion

    #region Racing Rooms
    public void FirstRoom()
    {
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.FirstRacing, preloadAddresses);
    }
    public void BaxmalRacing()
    {
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        //int langId=-1;
        //if (currentPower < 30)
        //    langId = 334;

        //if (currentCooling < 30)
        //    langId = 335;

        //if (currentStamina < 30)
        //    langId = 336;
        

        if (currentPower < Constants.HorseConditionNum.Power || currentCooling < Constants.HorseConditionNum.Cool || currentStamina < Constants.HorseConditionNum.Stamina)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            HomeMainUI.Instance?.SHowFoodPanel();
            return;  // Racing boshlanmaydi
        }
        // ;
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, LanguageManager.Instance.GetText(209), instant: false);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.SecondRacing, preloadRacing);
    }
    public void EgyptRacing()
    {
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        //int langId = -1;
        //if (currentPower < 20)
        //    langId = 334;

        //if (currentCooling < 10)
        //    langId = 335;

        //if (currentStamina < 30)
        //    langId = 336;


        if (currentPower < 20 || currentCooling < 10 || currentStamina < 30)
        {
            //HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            HomeMainUI.Instance?.SHowFoodPanel();
            HomeHapticsManager.Instance.Play(HomeHapticId.LowCondition);
            return;  // Racing boshlanmaydi
        }
        HomeHapticsManager.Instance.Play(HomeHapticId.Success);
        UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, LanguageManager.Instance.GetText(210), instant: false);
        SceneLoadManager.Instance.LoadSceneNew(SceneLoadManager.SceneType.EgyptRacing, preloadRacing);
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
        await SetLoading(true);

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
            await SetLoading(false);
            Debug.LogWarning("❌ Environment instantiate failed.");
            return;
        }

        StartCoroutine(EnableManagersNextFrame());

        Teleport(playerInstance.transform, playerSpawnPos);
        Teleport(horseInstance.transform, horseSpawnPos);

        OnNameChanged?.Invoke(envAddress);
        _currentEnvAddress = envAddress;
        RegisterEnvPrefabs(_currentEnvInstance.transform);
        await SetLoading(false);
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


    public async Task SetLoading(bool show)
    {
        if (isLoadingVisible == show)
            return;

        isLoadingVisible = show;

        loadingTween?.Kill();

        float screenW = Screen.width;

        if (show)
        {
            // 🔒 Input block
            loadingRT.gameObject.SetActive(true);
            // Start: right side (fast entry)
            loadingRT.anchoredPosition = new Vector2(screenW, 0f);

            loadingTween = loadingRT
                .DOAnchorPosX(0f, wipeDuration)
                .SetEase(wipeEase);

            await loadingTween.AsyncWaitForCompletion();
        }
        else
        {
            // Exit: left side (speed out)
            loadingTween = loadingRT
                .DOAnchorPosX(-screenW, wipeDuration * 0.85f)
                .SetEase(Ease.InExpo);

            await loadingTween.AsyncWaitForCompletion();

            loadingRT.gameObject.SetActive(false);
        }
    }


    private IEnumerator EnableManagersNextFrame()
    {
        // 1) Hard reset
        if (treeManager != null) treeManager.Dispose();
        if (detailManager != null) detailManager.Dispose();

        yield return null; // terrain fully registered bo‘lsin

        if (treeManager != null) treeManager.Initialize();
        if (detailManager != null) detailManager.Initialize();

        // 2) Force update
        if (treeManager != null) treeManager.RequireUpdate(true);
        if (detailManager != null) detailManager.RequireUpdate(true);
    }
    public void RegisterEnvPrefabs(Transform envRoot)
    {
        if (prefabManager == null || envRoot == null) return;

        var instances = envRoot.GetComponentsInChildren<GPUIPrefab>(true);

        // 0 ID bo'lganlarini filtr qilamiz (aks holda error spam)
        var valid = instances.Where(p => p != null && p.GetPrefabID() != 0 && !p.IsInstanced).ToArray();

        // Queue'ga qo'shadi, manager LateUpdate'da instancelaydi
        GPUIPrefabManager.AddPrefabInstances(valid);

        // Agar Transform updates o'chirilgan bo'lsa, bir marta update talab qilsa bo'ladi
        prefabManager.RequireTransformUpdate();
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

    #region Room Sound
    private async void RoomSound()
    {
        var clip = await AddressablesService.Instance.LoadAssetAsync<AudioClip>(Constants.RoomSound.HomeRoomSound);
        if(clip != null && SoundManager.Instance !=null)
        {
            SoundManager.Instance.PlayRoom(clip);
        }
    }
    #endregion
    private void OnDestroy()
    {
        if (_currentEnvInstance != null)
        {
            AddressablesService.Instance.ReleaseInstance(_currentEnvInstance);
        }
    }
}
