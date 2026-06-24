using UnityEngine;
using Cinemachine;
using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera cam1;
    public CinemachineVirtualCamera cam2;
    public GameObject mainUICanvas;
    public GameObject mobileCanvas;

    private bool isCam1Active = true;
    public bool backFirstCam = false;
    public Vector3 cam1SavedPos;
    public Quaternion cam1SavedRot;

    [SerializeField] private Sprite eagleSprite;
    [SerializeField] private Sprite mainMapSprite;
    [SerializeField] private Button miniMapButton;
    [SerializeField] private RaceWorldMiniMapUI[] miniMapUIs;

    private readonly List<RaceWorldMiniMapUI> cachedMiniMapUIs = new List<RaceWorldMiniMapUI>();

    void Start()
    {
        //cam1.Priority = 20;
        //cam2.Priority = 15;
        if (miniMapButton != null)
            miniMapButton.onClick.AddListener(SwitchCamera);

        CacheMiniMapUIs();
        SetMiniMapUpdatesActive(isCam1Active);
    }

    public void SwitchCamera()
    {
        isCam1Active = !isCam1Active;
        Debug.Log("Change camera");

        if (isCam1Active)
        {
            if (cam1.TryGetComponent<ThirdPersonFollowTarget>(out var cam1Script) && cam1Script.CamPivot != null)
            {
                StartCoroutine(RestoreCam1PivotAfterFrame(cam1Script)); // 👈 Asosiy yechim
            }
            cam1.Priority = 10;
            cam2.Priority = 5;
            if (mobileCanvas != null) mobileCanvas.transform.localScale = Vector3.one;
            if (mainUICanvas != null) mainUICanvas.transform.localScale = Vector3.one;
            SetMiniMapUpdatesActive(true);
            Debug.Log("Back first cam");
            
            if (miniMapButton != null) miniMapButton.image.sprite = eagleSprite;
            backFirstCam = true;
        }
        else
        {
            // cam1 dan cam2 ga o‘tayotganda — cam1 pivotini saqlaymiz
            if (cam1.TryGetComponent<ThirdPersonFollowTarget>(out var cam1Script) && cam1Script.CamPivot != null)
            {
                cam1SavedPos = cam1Script.CamPivot.position;
                cam1SavedRot = cam1Script.CamPivot.rotation;
            }

            cam1.Priority = 5;
            cam2.Priority = 10;
            if (mobileCanvas != null) mobileCanvas.transform.localScale = Vector3.zero;
            if (mainUICanvas != null) mainUICanvas.transform.localScale = Vector3.zero;
            SetMiniMapUpdatesActive(false);
            if (miniMapButton != null) miniMapButton.image.sprite = mainMapSprite;
        }
    }
    private IEnumerator RestoreCam1PivotAfterFrame(ThirdPersonFollowTarget cam1Script)
    {
        yield return new WaitForEndOfFrame(); // LateUpdate tugaganidan so‘ng

        cam1Script.lerpPosition.Value = 0f; // Endi ThirdPersonFollowTarget yozmaydi
        cam1Script.CamPivot.position = cam1SavedPos;
        cam1Script.CamPivot.rotation = cam1SavedRot;
    }

    private void CacheMiniMapUIs()
    {
        cachedMiniMapUIs.Clear();

        AddMiniMapUIs(miniMapUIs);

        if (mainUICanvas != null)
            AddMiniMapUIs(mainUICanvas.GetComponentsInChildren<RaceWorldMiniMapUI>(true));

        if (mobileCanvas != null)
            AddMiniMapUIs(mobileCanvas.GetComponentsInChildren<RaceWorldMiniMapUI>(true));
    }

    private void AddMiniMapUIs(RaceWorldMiniMapUI[] miniMaps)
    {
        if (miniMaps == null) return;

        for (int i = 0; i < miniMaps.Length; i++)
        {
            RaceWorldMiniMapUI miniMap = miniMaps[i];
            if (miniMap == null || cachedMiniMapUIs.Contains(miniMap)) continue;

            cachedMiniMapUIs.Add(miniMap);
        }
    }

    private void SetMiniMapUpdatesActive(bool active)
    {
        if (cachedMiniMapUIs.Count == 0)
            CacheMiniMapUIs();

        for (int i = 0; i < cachedMiniMapUIs.Count; i++)
        {
            RaceWorldMiniMapUI miniMap = cachedMiniMapUIs[i];
            if (miniMap == null) continue;

            miniMap.enabled = active;
        }
    }

}
