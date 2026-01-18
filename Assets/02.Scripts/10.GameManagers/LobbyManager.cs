using MalbersAnimations.Controller;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{

    public ModalWindowManager detailsPopupManager;

    [SerializeField] private AudioClip lobbySound;
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
    private List<string> customSceneAddressableAddresses = new List<string> { "Chopar"};

    private List<string> preloadAddresses;
    private bool isSleeping = false;


    private async void Start()
    {
        SceneLoadManager.Instance.SetAssetInstantiationFinished(false);
        playerInstance = Instantiate(playerPrefab, playerSpawnPos.position, playerSpawnPos.rotation, PlayerParent.transform);

        // 2. Ichidan PlayerSkinLoader scriptni topamiz
        PlayerSkinLoader skinLoader = playerInstance.GetComponentInChildren<PlayerSkinLoader>();
        // 3. Agar mavjud bo‘lsa — Addressable materiallarni qo‘llaymiz
        if (skinLoader != null)
        {
            // await skinLoader.ApplyMaterials();
            await skinLoader.ApplySkins();
        }
        else
        {
            Debug.Log("❌ PlayerSkinLoader component not found on instantiated player.");
        }
        horseInstance = Instantiate(horsePrefab, horseSpawnPos.position, horseSpawnPos.rotation, HorseParent.transform);

        // 4. Ichidan HorseSkinLoader scriptni topamiz
        HorseSkinLoader horseSkinLoader = horseInstance.GetComponentInChildren<HorseSkinLoader>();
        if(horseSkinLoader != null)
        {
            await horseSkinLoader.ApplySkins();
        }
        else
        {
            Debug.Log("❌ HorseSkinLoader component not found on instantiated horse.");
        }

        utov = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
            Constants.Environment.Utov,
            position: utovPos.position,
            rotation: utovPos.rotation,
            parent: null
        );
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);
        //Removing Boshidagi FadeImgae Intro Scene dan kelayotganda
        HomeMainUI.Instance.RemoveInitialImage();
        //Horse Animator Details
        HorseAnimGet();
        GetPlayerAnimator();
        if (SoundManager.Instance != null)
             SoundManager.Instance.PlayMusic(lobbySound);
        preloadAddresses = GetPreloadMaterialAddresses();

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
        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.Jomboy, preloadAddresses);
    }
    public void PastDargom()
    {
        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.PastDargom, preloadAddresses);
    }
    public void AvatarCustom()
    {
        SceneLoadManager.Instance.LoadSmartScene(SceneLoadManager.SceneType.AvatarCustom, customSceneAddressableAddresses);
    }
    public void TrainingRoom()
    {
        
        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.Beginer, preloadAddresses);
    }
    #endregion

    #region Racing Rooms
    public void FirstRoom()
    {
        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.FirstRacing, preloadAddresses);
    }
    public void BaxmalRacing()
    {
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        int langId=-1;
        if (currentPower < 30)
            langId = 334;

        if (currentCooling < 30)
            langId = 335;

        if (currentStamina < 30)
            langId = 336;
        

        if (currentPower < 30 || currentCooling < 30 || currentStamina < 30)
        {
            HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            return;  // Racing boshlanmaydi
        }
           

        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.SecondRacing, preloadAddresses);
    }
    public void EgyptRacing()
    {
        float currentPower = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float currentCooling = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float currentStamina = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        int langId = -1;
        if (currentPower < 30)
            langId = 334;

        if (currentCooling < 30)
            langId = 335;

        if (currentStamina < 30)
            langId = 336;


        if (currentPower < 30 || currentCooling < 30 || currentStamina < 30)
        {
            HomeMainUI.Instance?.HorseResourceFinishPopup(LanguageManager.Instance.GetText(langId));
            return;  // Racing boshlanmaydi
        }


        SceneLoadManager.Instance.LoadSmartSceneWithoutAdditive(SceneLoadManager.SceneType.EgyptRacing, preloadAddresses);
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

    private void OnDestroy()
    {
        //if (player != null) Addressables.ReleaseInstance(player);
       // if (horse != null) Addressables.ReleaseInstance(horse);
        if (utov != null) Addressables.ReleaseInstance(utov);
    }
}
