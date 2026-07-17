using DG.Tweening;
using GPUInstancerPro.PrefabModule;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class AvatarCustomManager : MonoBehaviour
{
    [SerializeField] private ModalWindowManager Popup;
    [SerializeField] private AvatarCustomUIManager uiManager;

    #region Player and Horse Data
    [Header("Player and Horse Prefab")]
    public GameObject playerPrefab;
    private GameObject playerInstance;

    public GameObject horsePrefab;
    private GameObject horseInstance;

    // public GameObject environmentObj;
    [SerializeField] private string envAddressAddressablesName = "CustomRoomEnvironment";
    private GameObject _currentEnvInstance;
    [Header("Spawn Positions")]
    [SerializeField] private Transform playerSpawnPos;
    [SerializeField] private Transform horseSpawnPos;
    #endregion

    #region Camera Movemenet Parametrs
    private AvatarCustomTypes.CamSpot _currentSpot = AvatarCustomTypes.CamSpot.Start;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform cameraPivot;

    [Header("Targets (Camera Positions)")]
    [SerializeField] private Transform playerCamPos;
    [SerializeField] private Transform horseCamPos;
    [SerializeField] private Transform headCameraPos;
    [SerializeField] private Transform upperBodyCameraPos;
    [SerializeField] private Transform camStartPos;

    [Header("Raycast")]
    [SerializeField] private LayerMask clickableMask;
    [SerializeField] private float maxDistance = 200f;

    [Header("Animation (DOTween)")]
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private float rotateDuration = 0.7f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Click Identification")]
    [SerializeField] private string playerTag = "PlayerSpot";
    [SerializeField] private string horseTag = "HorseSpot";

    private InputAction _pressAction;
    private InputAction _posAction;

    private Tween _moveTween;
    private Tween _rotTween;
    #endregion

    #region Buttons

    #endregion

    [SerializeField] private GPUIPrefabManager prefabManager;
    public static PlayerSkinLoader CurrentPlayerSkinLoader { get; private set; }
    public static event Action<PlayerSkinLoader> OnPlayerSkinLoad;
    public static event Action<HorseSkinLoader> OnHorseSkinLoad;

    public static event Action OnAllSet;
    private void Reset()
    {
        cam = Camera.main;
        if (cam != null) cameraPivot = cam.transform;
    }

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cameraPivot == null && cam != null) cameraPivot = cam.transform;

        _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        _posAction = new InputAction("Position", InputActionType.Value, "<Pointer>/position");
    }

    private void OnEnable()
    {
        _pressAction.Enable();
        _posAction.Enable();

        _pressAction.performed += OnPressed;
        // ✅ UI button listenerlarni UI manager o'rnatadi
        uiManager?.Bind(this);
    }

    private void OnDisable()
    {
        _pressAction.performed -= OnPressed;

        _pressAction.Disable();
        _posAction.Disable();
    }

    private async void Start()
    {
        _currentEnvInstance = await AddressablesService.Instance.InstantiateAsync(
            envAddressAddressablesName,
            Vector3.zero,
            Quaternion.identity
        );
        // eski Start'ingni saqlab qoldim (commentlar ham)
        await InitializePlayerAndHorse();
        RegisterEnvPrefabs(_currentEnvInstance.transform);
        //Popup.confirmButton.onClick.AddListener(LoadLobbyScene);
    }


    #region Player And Horse Initialization
    private async Task InitializePlayerAndHorse()
    {
        playerInstance = Instantiate(playerPrefab, playerSpawnPos.position, playerSpawnPos.rotation);

        PlayerSkinLoader skinLoader = playerInstance.GetComponentInChildren<PlayerSkinLoader>();
        if (skinLoader != null)
            await skinLoader.ApplyAllSkins();
        else
            Debug.Log("❌ PlayerSkinLoader component not found on instantiated player.");
        horseInstance = Instantiate(horsePrefab, horseSpawnPos.position, horseSpawnPos.rotation /*HorseParent.transform*/);

        HorseSkinLoader horseSkinLoader = horseInstance.GetComponentInChildren<HorseSkinLoader>();
        if (horseSkinLoader != null)
            await horseSkinLoader.ApplyAllSkins();
        else
            Debug.Log("❌ HorseSkinLoader component not found on instantiated horse.");
        OnAllSet?.Invoke();
        SceneLoadManager.Instance.SetAssetInstantiationFinished(true);
        UIOverlayRoot.I.HidePanel(UIPanelType.Custom);
    }
    public static void RaisePlayerSkinLoad(PlayerSkinLoader loader)
    {
        CurrentPlayerSkinLoader = loader;
        OnPlayerSkinLoad?.Invoke(loader);
    }
    public static void RaiseHorseSkinLoad(HorseSkinLoader loader)
    {
        //CurrentPlayerSkinLoader = loader;
        OnHorseSkinLoad?.Invoke(loader);
    }
    #endregion

    #region Camera Movements + Input
    private void OnPressed(InputAction.CallbackContext ctx)
    {
        if (cam == null || cameraPivot == null) return;

        if (IsPointerOverUI())
            return;

        Vector2 screenPos = _posAction.ReadValue<Vector2>();

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag(playerTag))
                GoToSpot(AvatarCustomTypes.CamSpot.Player);
            else if (hit.collider.CompareTag(horseTag))
                GoToSpot(AvatarCustomTypes.CamSpot.Horse);
        }
    }

    // ✅ sen ishlatayotgan “UI ustida bo‘lsa blok” varianti (o‘zgartirmadim)
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Pointer.current != null)
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Pointer.current.position.ReadValue()
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results != null && results.Count > 0;
        }

        return false;
    }

    public void GoToSpot(AvatarCustomTypes.CamSpot spot)
    {
        Transform target = null;

        switch (spot)
        {
            case AvatarCustomTypes.CamSpot.Start: target = camStartPos; break;
            case AvatarCustomTypes.CamSpot.HeadPlayer: target = headCameraPos; break;
            case AvatarCustomTypes.CamSpot.UpperBodyPlayer: target = upperBodyCameraPos; break;
            case AvatarCustomTypes.CamSpot.Player: target = playerCamPos; break;
            case AvatarCustomTypes.CamSpot.Horse: target = horseCamPos; break;
        }

        GoTo(target, spot);
        _currentSpot = spot;
    }

    private void GoTo(Transform target, AvatarCustomTypes.CamSpot spot)
    {
        if (target == null || cameraPivot == null) return;

        _moveTween?.Kill();
        _rotTween?.Kill();

        bool moveDone = false;
        bool rotDone = false;

        void CompleteIfReady()
        {
            if (!moveDone || !rotDone) return;

            switch (spot)
            {
                case AvatarCustomTypes.CamSpot.Player:
                    uiManager?.OnPlayerArrived(); // ✅ UI chiqishi
                    break;

                case AvatarCustomTypes.CamSpot.Horse:
                    uiManager?.OnHorseArrived();  // ✅ UI chiqishi
                    break;

                case AvatarCustomTypes.CamSpot.Start:
                    uiManager?.CloseRightPanel(); // ✅ UI yopilishi
                    break;
            }
        }

        _moveTween = cameraPivot
            .DOMove(target.position, moveDuration)
            .SetEase(ease)
            .OnComplete(() => { moveDone = true; CompleteIfReady(); });

        _rotTween = cameraPivot
            .DORotateQuaternion(target.rotation, rotateDuration)
            .SetEase(ease)
            .OnComplete(() => { rotDone = true; CompleteIfReady(); });
    }
    #endregion

    #region Back and Save Actions
    public void BackPublic() => Back();

    private void Back()
    {
        if (_currentSpot != AvatarCustomTypes.CamSpot.Start)
        {
            GoToSpot(AvatarCustomTypes.CamSpot.Start);
        }
        else
        {
            ConfirmPopup();
        }
    }
    private void ConfirmPopup()
    {
        UIOverlayRoot.I.Confirm(434, 435, 1, 2, YesAction, null);
    }
    private void YesAction()
    {
        AvatarCustomUIManager.RevertPendingPreviews();
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191), instant: false, true);
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }

    public void OpenPopup()
    {
        Popup.UpdateUICustom("Buncha tez?", "Tezlik bilan o'yindi boshlaymizmi?");
    }

    public void LoadLobbyScene()
    {
        //SceneLoadManager.Instance.LoadSmartScene(SceneLoadManager.SceneType.Lobby, lobbySceneAddressableAddresses);
    }
    #endregion

    public void RegisterEnvPrefabs(Transform envRoot)
    {
        if (prefabManager == null || envRoot == null) return;

        var instances = envRoot.GetComponentsInChildren<GPUIPrefab>(true);

        // 0 ID bo'lganlarini filtr qilamiz (aks holda error spam)
        List<GPUIPrefab> valid = new List<GPUIPrefab>(instances.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            GPUIPrefab instance = instances[i];
            if (instance != null && instance.GetPrefabID() != 0 && !instance.IsInstanced)
                valid.Add(instance);
        }

        // Queue'ga qo'shadi, manager LateUpdate'da instancelaydi
        GPUIPrefabManager.AddPrefabInstances(valid);

        // Agar Transform updates o'chirilgan bo'lsa, bir marta update talab qilsa bo'ladi
        prefabManager.RequireTransformUpdate();
    }
}
