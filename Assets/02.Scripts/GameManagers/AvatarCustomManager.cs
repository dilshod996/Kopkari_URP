using MalbersAnimations.Controller;
using MalbersAnimations.HAP;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AvatarCustomManager : MonoBehaviour
{
    [SerializeField] private ModalWindowManager Popup;

    private bool isSaved;

    //[SerializeField] private GameObject InfoPanel;
    //[SerializeField] private Button CustomTestBtn;

    [Header("RoomEvents")]
    [SerializeField] private Button backLobby;
    [SerializeField] private Button SaveBtn;

    [Header("Player and Horse Prefab")]

    public GameObject playerPrefab;
    private GameObject playerInstance;
    public GameObject horsePrefab;
    private GameObject horseInstance;

    [Header("Addressable")]
    [SerializeField] private Transform playerSpawnPos;
    [SerializeField] private Transform horseSpawnPos;
    [SerializeField] private GameObject PlayerParent;
    [SerializeField] private GameObject HorseParent;
    private List<string> lobbySceneAddressableAddresses = new List<string> { "Chopar", "Horse", "Utov"};
    private GameObject player;
    private GameObject horse;
    [SerializeField] AudioClip roomSound;
    private async void Start()
    {
        SoundManager.Instance.PlayMusic(roomSound);
        await InitializePlayerAndHorse();
        //player = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
        //    "Chopar",
        //    position: playerSpawnPos.position,
        //    rotation: playerSpawnPos.rotation,
        //    parent: PlayerParent.transform
        //);
        //horse = await AddressablesManager.Instance.LoadAndInstantiateCachedAsync(
        //    "Horse",
        //    position: horseSpawnPos.position,
        //    rotation: horseSpawnPos.rotation,
        //    parent: HorseParent.transform
        //);
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);
        Popup.confirmButton.onClick.AddListener(LoadLobbyScene);
       // CustomTestBtn.onClick.AddListener(()=>StartCoroutine(InfoPanelDelay()));
    }
    public void OpenPopup()
    {
        Popup.UpdateUICustom("Buncha tez?" , "Tezlik bilan o'yindi boshlaymizmi?");
    }
    public void LoadLobbyScene()
    {
        SceneLoadManager.Instance.LoadSmartScene(SceneLoadManager.SceneType.Lobby, lobbySceneAddressableAddresses);
    }

    #region Player And Horse Initialization
    private async Task InitializePlayerAndHorse()
    {

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
        if (horseSkinLoader != null)
        {
            await horseSkinLoader.ApplySkins();
        }
        else
        {
            Debug.Log("❌ HorseSkinLoader component not found on instantiated horse.");
        }

    }
    #endregion
}
