using System;
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

        // 🔒 Lock
        if (lockImage != null)
            lockImage.SetActive(!isOpen);

        if (setButton != null)
            setButton.interactable = isOpen;

        // ✅ Checkmark
        if (!isOpen)
        {
            if (checkmark != null)
                checkmark.SetActive(false);

            return; // 🔥 shu joy MUHIM
        }

        // faqat OPEN bo‘lsa tekshiriladi
        currentEnv = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment, "");
        bool isSelected = currentEnv== mapKey;
        if (checkmark != null)
            checkmark.SetActive(isSelected);
    }

    private void OnLockClicked()
    {
        Debug.Log("Detias show");
        //if (EnvironmentDetailsPopup.Instance != null)
        //    EnvironmentDetailsPopup.Instance.Show(mapKey);
    }

    private void OnSetClicked()
    {
        if(currentEnv.Equals(mapKey))
            { return; }
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

    private void HandleMapUnlocked(string unlockedMapKey)
    {
        if (unlockedMapKey == mapKey)
            RefreshUI();
    }

}
