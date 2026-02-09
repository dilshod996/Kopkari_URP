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
    [SerializeField] private Transform winSafeZone;
    [SerializeField] private Transform loseSafeZone;
    [SerializeField] private Transform horseSafeZone;

    public static Action OnShowFinalPage;
    //public MAnimal playerAnimal;
    private async void Start()
    {
        await InitializePlayerAndMountAsync();
    }

    private void OnEnable()
    {
        UIGetLamp.OnPlayerGotLamp += PickupObj;
        RacingController.OnRacingFinished += DismountPlayer;
        //BaseManager.OnGoatPicked += DropState;
    }
    private void OnDisable()
    {
        UIGetLamp.OnPlayerGotLamp -= PickupObj;
        RacingController.OnRacingFinished -= DismountPlayer;
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
            await skinLoader.ApplyAllSkins();
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
    public void PlayFinishCinematic(bool isWin)
    {
        StartCoroutine(FinishCinematic(isWin));
    }
    private IEnumerator FinishCinematic(bool isWin)
    {
        // 1) Blink IN (ko‘z yumish)
        yield return StartCoroutine(UIButtonActions.Instance.FadeBlink(0f, 1f, 1f));
        //horseAnimal.gameObject.SetActive(false);

        // 2) Qorong‘ida — teleport + setup
        if (riderInstance != null)
        {
            riderInstance.ForceDismount();   // yoki DismountAnimal()
            yield return null;

            Transform target = isWin ? winSafeZone : loseSafeZone;

            // rider’ni safe zone ga ko‘chirish
            riderInstance.transform.SetPositionAndRotation(target.position, target.rotation);
            horseAnimal.transform.SetPositionAndRotation(horseSafeZone.position, horseSafeZone.rotation);
            // root motion bo‘lsa, o‘chirib qo‘y (sirpanmasin)
            if (riderInstance.Anim != null) riderInstance.Anim.applyRootMotion = false;

            // ixtiyoriy: rigidbody’ni kinematic qilib qo‘y (barqaror)
            var rb = riderInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }

        // 3) Blink OUT (ko‘z ochish)
        yield return StartCoroutine(UIButtonActions.Instance.FadeBlink(1f, 0f, 1f));
        if (SceneLoadManager.Instance != null)
        {
            int abilityIndex = SceneLoadManager.Instance.CurrentSceneType switch
            {
                SceneLoadManager.SceneType.EgyptRacing => 2,
                SceneLoadManager.SceneType.FirstRacing => 3,
                SceneLoadManager.SceneType.SecondRacing => 1,
                _ => -99
            };

            bool ok = riderAnimal.Mode_TryActivate(18, abilityIndex);
            riderAnimal.Mode_Activate(18, abilityIndex);
        }
        else
        {
            bool ok = riderAnimal.Mode_TryActivate(18, 2);
            riderAnimal.Mode_Activate(18, 2);
        }



        // 5) Anim ko‘rinsin
        yield return new WaitForSecondsRealtime(3.5f);

        // 6) Endi UI
        OnShowFinalPage?.Invoke();
    }
    private void DismountPlayer()
    {
        PlayFinishCinematic(true);
        //StartCoroutine(DismountStructure());
    }
    private IEnumerator DismountStructure()
    {
        if (riderInstance != null)
        {

            riderInstance.ForceDismount();
            yield return null;

            // 1) Horse’dan chetga chiqar (overlap bo'lmasin)
            Transform t = riderInstance.transform;
            Vector3 safePos = horseAnimal.transform.position
                            + horseAnimal.transform.right * 1.0f   // o'ngga 1m
                            + Vector3.up * 0.2f;                   // biroz tepaga

            t.position = safePos;

            // 2) Yerga snap (raycast)
            SnapRiderToGround(t);

            // 3) Endi MAnimal o'chiramiz
            if (riderAnimal != null) riderAnimal.enabled = false;

            // 4) Rigidbody: finish uchun eng stabil variant — kinematic qilib qo'yish
            Rigidbody rb = riderInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true; // 🔥 shu "uchish"ni 100% yo'q qiladi
            }
        }


    }
    private void SnapRiderToGround(Transform t)
    {
        Vector3 origin = t.position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f))
        {
            t.position = hit.point + Vector3.up * 0.02f;
        }
    }
    private IEnumerator MoveSmooth(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = time / duration;
            t.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        t.position = to;
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
