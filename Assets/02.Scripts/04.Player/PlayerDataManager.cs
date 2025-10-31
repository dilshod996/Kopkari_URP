using UnityEngine;
using System.Threading.Tasks;
using MalbersAnimations.HAP;
using MalbersAnimations.Utilities;
using MalbersAnimations.Controller;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;
using System.Collections;

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

    public Button walkZoneBtn;
    public Button defendBtn;

    private MAnimal horseAnimal;

    [Header("Player Attack and DefendDetails")]
    [SerializeField] private TMP_Text walkZoneText;
    [SerializeField] private TMP_Text defendText;
    AttackDefendManager attackDefendController;
    public ProgressBar progressBar;

    //public MAnimal playerAnimal;
    private async void Start()
    {
        await InitializePlayerAndMountAsync();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("InitialDoor"))
        {
            Debug.Log("Entered Initial Door");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("InitialDoor"))
        {
            isWater = false;
        }
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
        if (BaseManager.Instance != null)
        {
            BaseManager.Instance.horseAnimal = horseAnimal;
        }
        if(RacingController.Instance != null)
        {
            RacingController.Instance.GetSetAnimal(horseAnimal);
        }
       // playerAnimal = riderInstance.RiderAnimal;
        attackDefendController = playerInstance.GetComponentInChildren<AttackDefendManager>();
        if (attackDefendController != null && walkZoneBtn != null)
        {
            // Avval eski listenerlarni tozalaymiz, keyin yangi funksiya bog‘laymiz
            walkZoneText.text = attackDefendController.walkZoneCount.ToString();
            attackDefendController.OnWalkZoneAdded += UpdateWalkZoneText;
            attackDefendController.OnWalkZoneRemoved += UpdateWalkZoneText;
            defendText.text = attackDefendController.defendCount.ToString();
            attackDefendController.OnDefendAdded += UpdateDefendText;
            attackDefendController.OnDefendRemoved += UpdateDefendText;
            walkZoneBtn.onClick.RemoveAllListeners();
            walkZoneBtn.onClick.AddListener(attackDefendController.DropWalkTrap);
            defendBtn.onClick.RemoveAllListeners();
            defendBtn.onClick.AddListener(() => CheckItLocked(attackDefendController));
        }
        else
        {
            Debug.LogError("Dropper yoki Button topilmadi!");
        }
        //SceneLoadManager.Instance.SetAssetInstantiationFinished(true);
    }
    private void OnDisable()
    {
        if (attackDefendController != null)
        {
            // Listenerlarni tozalaymiz
            attackDefendController.OnWalkZoneAdded -= UpdateWalkZoneText;
            attackDefendController.OnWalkZoneRemoved -= UpdateWalkZoneText;
            attackDefendController.OnDefendAdded -= UpdateDefendText;
            attackDefendController.OnDefendRemoved -= UpdateDefendText;
        }
    }
    private void UpdateWalkZoneText()
    {
        walkZoneText.text = attackDefendController.walkZoneCount.ToString();
    }
    private void UpdateDefendText()
    {
        defendText.text = attackDefendController.defendCount.ToString();
    }

    public async void MountPlayer()
    {
        riderInstance.Anim.speed = 0.5f;
        riderInstance.MountAnimal();
        await Task.Delay(2000);
        riderInstance.Anim.speed = 1f;

        // Notify game manager
        BeginerRoomManager.Instance.GameStartedAction(true);
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
    private IEnumerator RunDefendProgress()
    {
        int maxValue = 6;
        int currentValue = maxValue;

        progressBar.gameObject.SetActive(true);
        progressBar.currentPercent = 6f;
        progressBar.UpdateUI();
        progressBar.textPercent.text = currentValue.ToString();

        while (currentValue > 0)
        {
            yield return new WaitForSeconds(1f);

            currentValue--;
            float percent = (float)currentValue / maxValue;
            int percentValue = Mathf.RoundToInt(percent * 100f);
            progressBar.currentPercent = percentValue;
            progressBar.UpdateUI();
            progressBar.textPercent.text = currentValue.ToString(); // text ni yangilaymiz
        }

        progressBar.currentPercent = 0f;
        progressBar.UpdateUI();
        progressBar.textPercent.text = "0";
        progressBar.gameObject.SetActive(false);
    }
    #region PickUp And Drop
    public void PickupObj()
    {
        pickableObj?.TryPickUp();
    }
    
    public void DropObject()
    {
        
        if (pickableObj != null && pickableObj.Has_Item)
        {
            pickableObj.TryDrop();
        }
        else
        {
            Debug.Log("No Item to Drop");
        }
    }

    public void DisablePicBtn()
    {
        // For UI integration
    }
    #endregion
}
