using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentCardUI : MonoBehaviour
{
    public enum HomeEnvironment
    {
        Utov,
        Egypt,
        Kansas
    }

    public HomeEnvironment Environment;
    [SerializeField] private string mapKey;        // "zarafmap", "regismap" ...

    [Header("UI")]
    [SerializeField] private GameObject lockImage; // lock + button
    [SerializeField] private Button lockButton;    // details
    [SerializeField] private Button setButton;     // set
    [SerializeField] private GameObject checkmark; // selected icon

    [SerializeField] private EnvironmentChangeUI environmentChangeUI;

    private string currentEnv;
    [SerializeField] private LobbyManager lobbyManager;
    private static readonly Dictionary<string, MapCard> MapCardCache = new();

    public static event Action<string> OnEnvironmentNameChanged;

    private void OnEnable()
    {
        DataManager.OnMapUnlocked += HandleMapUnlocked;
        RefreshUI();

        if (lockButton != null)
            lockButton.onClick.AddListener(OnLockClicked);

        if (setButton != null)
            setButton.onClick.AddListener(OnSetClicked);
    }

    private void OnDisable()
    {
        DataManager.OnMapUnlocked -= HandleMapUnlocked;

        if (lockButton != null)
            lockButton.onClick.RemoveListener(OnLockClicked);

        if (setButton != null)
            setButton.onClick.RemoveListener(OnSetClicked);
    }

    private void RefreshUI()
    {
        bool isOpen = IsMapOpen();

        if (lockImage != null)
            lockImage.SetActive(!isOpen);

        if (setButton != null)
            setButton.interactable = isOpen;

        if (!isOpen)
        {
            if (checkmark != null)
                checkmark.SetActive(false);

            return;
        }

        currentEnv = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment, "");
        bool isSelected = currentEnv == mapKey;

        if (checkmark != null)
            checkmark.SetActive(isSelected);
    }

    private void OnLockClicked()
    {
        if (!TryGetMapDetailsData(out MapCard.MapDetailsData data))
        {
            Debug.LogWarning($"Environment details are missing for map key: {mapKey}", this);
            return;
        }

        MapShowPopup popup = FindObjectOfType<MapShowPopup>(true);
        if (popup == null)
        {
            Debug.LogWarning("MapShowPopup is missing in the scene.", this);
            return;
        }

        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);

        if (HomeMainUI.Instance != null)
            HomeMainUI.Instance.ShowUI(popup);
        else
            popup.gameObject.SetActive(true);

        popup.SetMapData(data);
    }

    private void OnSetClicked()
    {
        if (string.Equals(currentEnv, mapKey, StringComparison.Ordinal))
            return;

        if (!IsMapOpen())
            return;

        PlayerPrefs.SetString(Constants.HomeEnivronments.SelectedEnvironment, mapKey);
        Debug.Log("Selected Map name is " + mapKey);
        lobbyManager.ChangeMap(mapKey);
        OnEnvironmentNameChanged?.Invoke(mapKey);
        PlayerPrefs.Save();
        environmentChangeUI.Hide();
    }

    private bool IsMapOpen()
    {
        if (DataManager.Instance != null)
            return DataManager.Instance.IsMapUnlocked(mapKey);

        int defaultValue = mapKey == Constants.MapNames.RacingTraining || mapKey == Constants.MapNames.Zarafshan ? 1 : 0;
        return PlayerPrefs.GetInt(mapKey, defaultValue) == 1;
    }

    private bool TryGetMapDetailsData(out MapCard.MapDetailsData data)
    {
        data = default(MapCard.MapDetailsData);

        if (string.IsNullOrWhiteSpace(mapKey))
            return false;

        if (MapCardCache.TryGetValue(mapKey, out MapCard cachedCard) &&
            cachedCard != null &&
            cachedCard.TryGetDetailsData(out data))
        {
            return true;
        }

        MapCard[] cards = FindObjectsOfType<MapCard>(true);
        for (int i = 0; i < cards.Length; i++)
        {
            MapCard card = cards[i];
            if (card == null || card.MapKey != mapKey)
                continue;

            MapCardCache[mapKey] = card;
            return card.TryGetDetailsData(out data);
        }

        return false;
    }

    private void HandleMapUnlocked(string unlockedMapKey)
    {
        if (unlockedMapKey == mapKey)
            RefreshUI();
    }
}
