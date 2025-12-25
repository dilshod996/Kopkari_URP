using UnityEngine;
using System.Threading.Tasks;
using MalbersAnimations.HAP;
using MalbersAnimations.Utilities;
using MalbersAnimations.Controller;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System;

public class PlayerDataManager : MonoBehaviour
{
    public bool isWater = false;
    [SerializeField] private MPickUp pickableObj;

    //public MRider riderPrefab;
    public GameObject playerPrefab;
    private GameObject playerInstance;
    public Transform riderSpawnPoint;

    private MRider riderInstance;
    public HorseDataManager horseManager;
    private MAnimal riderAnimal;

    private MAnimal horseAnimal;
    



    public static Action<MAnimal, MAnimal> OnRiderAndHorse;
    public static Action OnDropObjectEvent;
    public static Action OnPickObjectEventl;
    public static Action<GameObject> OnLocalPlayerObject;

    //public MAnimal playerAnimal;
    private async void Start()
    {
        await InitializePlayerAndMountAsync();
    }

    private void OnEnable()
    {
        UIGetLamp.OnPlayerGotLamp += PickupObj;
        //BaseManager.OnGoatPicked += DropState;
    }
    private void OnDisable()
    {
        UIGetLamp.OnPlayerGotLamp -= PickupObj;
        //BaseManager.OnGoatPicked -= DropState;
    }
    private async Task InitializePlayerAndMountAsync()
    {

        playerInstance = Instantiate(playerPrefab, riderSpawnPoint.position, riderSpawnPoint.rotation, riderSpawnPoint.transform);

        string username = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        playerInstance.name = username;
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

        // 2. Component sifatida MRider ni olamiz
        riderInstance = playerInstance.GetComponent<MRider>();

        Mount horseMount = await horseManager.SpawnHorseAsync();

        // ✅ Mount holatini darhol qilish
        horseMount.InstantMount = true;   // scene da agar rider yaxshi korinmasa comment qilish mumkin

        // Wait for MountPoint is ready
        while (horseMount.MountPoint == null)
        {
            await Task.Yield();
        }

        // Trigger enter (optional)
        riderInstance.MountTriggerEnter(horseMount, horseMount.GetComponentInChildren<MountTriggers>());

        // Sekin animatsiya
        riderInstance.Anim.speed = 0.5f;

        // ✅ Darhol mount bo‘lish (parent bo‘ladi)
        riderInstance.MountAnimal();
        pickableObj = riderInstance.GetComponentInChildren<MPickUp>();
        riderInstance.gameObject.name = PlayerPrefs.GetString(Constants.Player.UsernameKey);
        await Task.Delay(1000);
        riderInstance.Anim.speed = 1f;
        horseAnimal = horseManager.CurrentAnimal;
        riderAnimal = riderInstance.RiderAnimal;
        OnRiderAndHorse?.Invoke(horseAnimal, riderAnimal);
        OnLocalPlayerObject?.Invoke(playerInstance.transform.root.gameObject);

    }


    public void CustomizeRider(MaterialChanger materialChanger, int index)
    {
        materialChanger?.SetAllMaterials(index);
    }
    public void CheckItLocked(AttackDefendManager attackDefendController)
    {
        Debug.Log("Check it is locked" + horseAnimal.CurrentSpeedSet.LockSpeed);
        attackDefendController.DefendPlayer();
        //StartCoroutine(RunDefendProgress());
        if (horseAnimal.CurrentSpeedSet.LockSpeed)
        {
            horseAnimal.CurrentSpeedSet.LockSpeed = false; // Lockni ochamiz
            //horseAnimal.CurrentSpeedSet.LockIndex = 2;
        }
    }
    
    #region PickUp And Drop
    public void PickupObj()
    {
        pickableObj?.PickUpItem();
    }
    public void DropState(bool state)
    {
        if (!state) DropObject();
    }
    public void DropObject()
    {
        
        if (pickableObj != null && pickableObj.Has_Item)
        {
            //pickableObj.TryDrop();
            pickableObj.DropItem();
            Debug.Log("Droppedddd");
        }
        else
        {
            Debug.Log("No Item to Drop");
        }
    }

    #endregion
}
