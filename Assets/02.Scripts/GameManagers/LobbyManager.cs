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
using static UnityEngine.Rendering.VolumeComponent;

public class LobbyManager : MonoBehaviour
{
    public NotificationManager NotificationManager;
    public ModalWindowManager closePopupManager;

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
   // private Ability playerActiveAbility;

    [Header("Horse")]
    public GameObject horsePrefab;
    private GameObject horseInstance;
    public MAnimal horseAnimator;

    [Header("First Prefs")]
    [SerializeField] private NPCStarter npcSelectPanel;
    [SerializeField] private GameObject giftPopupPanel;

    [Header("Other room addressables")]
    private List<string> customSceneAddressableAddresses = new List<string> { "Chopar"};

    public enum YesBtnActions
    {
        None, 
        Quit,
        Back
    }

    public YesBtnActions BtnClicked = YesBtnActions.None;

    [Header("Main Texts")]
    [SerializeField] private TMP_Text customText;
    [SerializeField] private TMP_Text prizeText;
    [SerializeField] private TMP_Text playText;
    [SerializeField] private TMP_Text tournamentText;
    [SerializeField] private TMP_Text storeText;
    [SerializeField] private TMP_Text settingsText;
    //[SerializeField] private TMP_Text newsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text lobbyName;
    private const string NPCStartTimeKey = "NPCStartTime";
    [SerializeField] private TMP_Text hourNPC;
    private TimeSpan npcDuration = TimeSpan.FromHours(24); // 24 soat

    [Header("NPC Selection")]
    [SerializeField] private Image npcImage;
    [SerializeField] private Sprite npcFirstSprite;
    [SerializeField] private Sprite npcSecondSprite;
    [Header("PlayerPrefs Texts")]
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text horseName;
    [SerializeField] private PlayerPrefsData playerPrefsPanel;
    [SerializeField] private FoodRemoveMotion foodMotionObj;
    [SerializeField] private HorseDetails horseFoodsPanel;
    private readonly string[] prefsToCheck = { "GiftGiven", "SelectedNPC", "username", "horsedata" };
    private List<string> preloadAddresses;
    private bool isSleeping = false;

    [Header("----------- Horse Action Buttons ------------")]
    [SerializeField] private Button eatBtn;
    [SerializeField] private Button drinkButton;
    private async void Start()
    {
        SceneLoadManager.Instance.SetAssetInstantiationFinished(false);
        //player = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
        //    "Chopar",
        //    position: playerSpawnPos.position,
        //    rotation: playerSpawnPos.rotation,
        //    parent: PlayerParent.transform
        //);
        // 1. Player'ni sahnaga qo‘yamiz
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

        //horse = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
        //    "Horse",
        //    position: horseSpawnPos.position,
        //    rotation: horseSpawnPos.rotation,
        //    parent: HorseParent.transform
        //);
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
            "Utov",
            position: utovPos.position,
            rotation: utovPos.rotation,
            parent: null
        );
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);

        //Horse Animator Details
        HorseAnimGet();
        GetPlayerAnimator();
        StartCoroutine(NotiPopup());
        closePopupManager.cancelButton.onClick.AddListener(CancelBtnEvent);
        closePopupManager.confirmButton.onClick.AddListener(ConfirmBtnEvent);
        if(SoundManager.Instance != null)
             SoundManager.Instance.PlayMusic(lobbySound);
        CheckNPCSelectionDads();
        InvokeRepeating(nameof(UpdateTimeRemaining), 0f, 1f);
        if(PlayerPrefs.HasKey(Constants.Player.UsernameKey))
        {
            playerName.text = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        }
        if(PlayerPrefs.HasKey(Constants.Horse.HorseNameKey))
        {
            horseName.text = PlayerPrefs.GetString(Constants.Horse.HorseNameKey);
        }
        preloadAddresses = GetPreloadMaterialAddresses();

        if(eatBtn != null)
        {
            eatBtn.onClick.AddListener(PlayEat);
        }
        if(drinkButton != null)
        {
            drinkButton.onClick.AddListener(PlayDrink);
        }
    }
    private void OnEnable()
    {
        MainLobbyText();
        foreach (var key in prefsToCheck)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SelectedPrefsCheck(key);
                break;
            }
        }
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
    }


    IEnumerator NotiPopup()
    {
        if (NotificationManager!=null)
        {
            yield return new WaitForSeconds(3f);
            NotificationManager.timer = 7f;
            NotificationManager.CustomeUpdate(LanguageManager.Instance.GetText(40), 
                LanguageManager.Instance.GetText(41));
        }
        yield return null;
    }
    void ConfirmBtnEvent()
    {
        AvatarCustom();
    }
    void CancelBtnEvent()
    {
        BtnClicked = YesBtnActions.None;
    }
    #region Popup va Infolar
    public void CustomPopup()
    {

        BtnClicked = YesBtnActions.Quit;
        closePopupManager.UpdateUICustomWithButtons(LanguageManager.Instance.GetText(42),
            LanguageManager.Instance.GetText(43),
            LanguageManager.Instance.GetText(1),
            LanguageManager.Instance.GetText(2));
    }

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
    #endregion

    #region Player Starting Prefs
    private List<string> GetPreloadMaterialAddresses()
    {
        List<string> preload = new List<string>();

        //PlayerPrefs dan material addresslarini olish
        string face = PlayerPrefs.GetString(Constants.Player.PlayerFaceKey);
        string upper = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey);
        string lower = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey);
        string helmet = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey);

        //PlayerPrefs dan ot material addresslarini olish

        string horseBody = PlayerPrefs.GetString(Constants.Horse.HorseBodyKey);
        string horseEyes = PlayerPrefs.GetString(Constants.Horse.HorseEyesKey);
        string horseMane = PlayerPrefs.GetString(Constants.Horse.HorseManeKey);
        string horseTail = PlayerPrefs.GetString(Constants.Horse.HorseTailKey);
        string horseReins = PlayerPrefs.GetString(Constants.Horse.HorseReinsKey);
        string horseSaddle = PlayerPrefs.GetString(Constants.Horse.HorseSaddleKey);
        string horseReinsHead = PlayerPrefs.GetString(Constants.Horse.HorseReinsHeadKey);

        preload.Add(face);
        preload.Add(upper);
        preload.Add(lower);
        preload.Add(helmet);
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
    public void PlayerNameCheck()
    {
        if (!PlayerPrefs.HasKey("username")){
            if( playerPrefsPanel != null)
            {
                playerPrefsPanel.gameObject.SetActive(true);
                playerPrefsPanel.UserDataCheck();
            }
        }
    }
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
            SelectedPrefsCheck("SelectedNPC");
        }
    }
    //Boboylar Npc
    private void CheckNPCSelectionDads()
    {
        string selectedNPC = PlayerPrefs.GetString("SelectedNPC", string.Empty);

        if (!string.IsNullOrEmpty(selectedNPC))
        {
            npcSelectPanel.EnableBg(); // panelni faollashtirish

            switch (selectedNPC)
            {
                case "ShomurodOta":
                    npcImage.sprite = npcFirstSprite;
                    break;

                case "NurmamatOta":
                    npcImage.sprite = npcSecondSprite;
                    break;

                default:
                    npcImage.sprite = npcFirstSprite; // Default sprite
                    break;
            }
        }
        else
        {
            // hech narsa tanlanmagan bo‘lsa panelni o‘chirsa ham bo‘ladi (ixtiyoriy)
            npcImage.sprite = npcFirstSprite; // yoki null, agar bo‘sh bo‘lishi kerak bo‘lsa
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

        // Endi NPC tanlashni tekshiramiz
        //CheckNPCSelection();
        SelectedPrefsCheck("SelectedNPC");
    }

    private void CheckNPCSelection()
    {
        if (!PlayerPrefs.HasKey("SelectedNPC"))
        {
            // Agar hali NPC tanlanmagan bo'lsa
            if (npcSelectPanel != null)
                npcSelectPanel.gameObject.SetActive(true);
        }
    }

    //SHu yerdan boshlab har bitta prefs save bolganligini korish kerak
    public void SelectedPrefsCheck(string nameofPrefs)
    {
        switch (nameofPrefs)
        {
            case "GiftGiven":
                CheckFirstTimeGift();
                break;
            case "SelectedNPC":
                CheckNPCSelection();
                break;
            case Constants.Player.UsernameKey:
                PlayerNameCheck();
                break;
            case "horsedata":
                playerPrefsPanel.HorseDataCheck();
                break;
            default:break;
        }
    }

        #endregion

    #region Text Languages

    public void MainLobbyText()
    {

        customText.text = LanguageManager.Instance.GetText(21);
        prizeText.text = LanguageManager.Instance.GetText(22);
        playText.text = LanguageManager.Instance.GetText(23);
        tournamentText.text = LanguageManager.Instance.GetText(24);
        storeText.text = LanguageManager.Instance.GetText(25);
        settingsText.text = LanguageManager.Instance.GetText(26);
       // newsText.text = LanguageManager.Instance.GetText(28);
        //timeText.text = LanguageManager.Instance.GetText(21);
        lobbyName.text = LanguageManager.Instance.GetText(27);

    }
    private void UpdateTimeRemaining()
    {
        if (!PlayerPrefs.HasKey(NPCStartTimeKey))
        {
            hourNPC.text = "Time out";
            CancelInvoke(nameof(UpdateTimeRemaining));
            return;
        }

        DateTime startTime = DateTime.Parse(PlayerPrefs.GetString(NPCStartTimeKey));
        TimeSpan elapsed = DateTime.Now - startTime; // Local vaqt farqi
        TimeSpan remaining = npcDuration - elapsed;


        if (remaining.TotalSeconds <= 0)
        {
            hourNPC.text = LanguageManager.Instance.GetText(260);
            CancelInvoke(nameof(UpdateTimeRemaining));
        }
        else
        {
            hourNPC.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }
    #endregion
    #region Player Actions
    private void GetPlayerAnimator()
    {
        playerAnimator = playerInstance.GetComponent<MAnimal>();
        Mode actionModePlayer = playerAnimator.Mode_Get(4); // Action Mode
        if (actionModePlayer != null)
        {
            Debug.Log("✅ Action mode found for player");
        }
        else
        {
            Debug.LogError("❌ Action Mode not found for player");
        }
    }
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
            Debug.Log("✅ Action mode found");
            eatAbility = actionMode.Abilities.Find(a => a.Name == "Eat");
            Debug.Log($"Eat Ability: {eatAbility?.Name}");
            drinkAbility = actionMode.Abilities.Find(a => a.Name == "Drink");
            Debug.Log($"Drink Ability: {drinkAbility?.Name}");
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
        string foodName = PlayerPrefs.GetString("foodToggle");
        float amountFood = PlayerPrefs.GetFloat(foodName);
        if (amountFood <= 0)
        {
            Debug.Log("Food amount is zero or less. Cannot eat.");
            horseFoodsPanel.gameObject.SetActive(true);
            horseFoodsPanel.FinishedFoodMessage();
            //return;
        }
        else
        {
            // Food amount is sufficient, proceed with eating
            float decreaseAmount = 0;
            float percentageAdded = 0;
            switch (foodName)
            {
                case Constants.Prizes.Bugdoy:
                    decreaseAmount = 0.5f;
                    percentageAdded = 10f;
                    break;
                case Constants.Prizes.Arpa:
                    decreaseAmount = 0.5f;
                    percentageAdded = 15f;
                    break;
                case Constants.Prizes.Apple:
                    decreaseAmount = 0.5f;
                    percentageAdded = 20f;
                    break;
                default:
                    Debug.Log("Food not found or not set in PlayerPrefs.");
                    return;
            }
            foodMotionObj.SetFoodDetails(foodName, ("+" + percentageAdded.ToString()+"%"), (decreaseAmount.ToString() + " " + LanguageManager.Instance.GetText(106)));
            float sum = amountFood - decreaseAmount;
            PlayerPrefs.SetFloat(foodName, sum);
            PlayerPrefs.Save(); // Save the updated food amount
            if (horseAnimator != null && eatAbility != null)
            {
                horseAnimator.Mode_Activate(4, eatAbility.Index);
                PlayerIdleLook();
            }
        }
        

    }
    public void EatAction()
    {
        Debug.Log("Eating action started");
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
        Debug.Log("Drinking action started");
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
                Debug.Log("🍽 EAT STARTED");
                eatParticle.Play();
                foodBowl.SetActive(true);
                eatBtn.interactable = false;
                drinkButton.interactable = false; 
                break;

            case "Drink":
                Debug.Log("🥤 DRINK STARTED");
                eatParticle.Play();
                waterBowl.SetActive(true);
                eatBtn.interactable = false;
                drinkButton.interactable = false;
                break;
        }
    }

    private void OnExitAction()
    {
        if (activeAbility == null) return;

        switch (activeAbility.Name)
        {
            case "Eat":
                Debug.Log("✅ EAT ENDED");
                eatParticle.Stop();
                foodBowl.SetActive(false);
                eatBtn.interactable = true;
                drinkButton.interactable = true;
                break;

            case "Drink":
                Debug.Log("✅ DRINK ENDED");
                eatParticle.Stop();
                waterBowl.SetActive(false);
                drinkButton.interactable = true;
                eatBtn.interactable = true;
                break;
        }

        activeAbility = null; // reset
    }
    public void PlayDrink()
    {
        string waterName = PlayerPrefs.GetString("waterToggle");
        float amountWater = PlayerPrefs.GetFloat(waterName);
        if (amountWater <= 0)
        {
            Debug.Log("Water amount is zero or less. Cannot drink.");
            horseFoodsPanel.gameObject.SetActive(true);
            horseFoodsPanel.FinishedFoodMessage();
            //return;
        }
        else
        {
            float decreaseAmount = 0;
            float percentageAdded = 0;
            switch (waterName)
            {
                case Constants.Prizes.Water:
                    decreaseAmount = 1f;
                    percentageAdded = 10f;
                    break;
                case Constants.Prizes.StaminWater:
                    decreaseAmount = 1f;
                    percentageAdded = 15f;
                    break;
                default:
                    Debug.Log("Food not found or not set in PlayerPrefs.");
                    return;
            }
            foodMotionObj.SetFoodDetails(waterName, ("+" + percentageAdded.ToString() + "%"), (decreaseAmount.ToString() + " " + LanguageManager.Instance.GetText(107)));
            float sum = amountWater - decreaseAmount;
            PlayerPrefs.SetFloat(waterName, sum);
            PlayerPrefs.Save(); // Save the updated food amount
            if(horseAnimator != null && drinkAbility != null)
            {
                // Drink actionni boshlash (Mode 4, Ability 7 - Drink)
                horseAnimator.Mode_Activate(4, drinkAbility.Index);
                PlayerIdleLook();
            }
            
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

    private void OnDestroy()
    {
        //if (player != null) Addressables.ReleaseInstance(player);
       // if (horse != null) Addressables.ReleaseInstance(horse);
        if (utov != null) Addressables.ReleaseInstance(utov);
    }
}
