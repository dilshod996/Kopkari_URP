using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardMapTabUI : MonoBehaviour
{
    [SerializeField] private string mapKey;
    [SerializeField] private TMP_Text mapTitleText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject selectedStateObject;
    [SerializeField] private Button button;

    private Action<string> onClicked;
    public string MapKey => mapKey;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        AutoWire();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(string key, bool unlocked, Action<string> clicked)
    {
        AutoWire();

        mapKey = key;
        onClicked = clicked;

        if (mapTitleText != null)
            mapTitleText.text = GetDisplayName(key);

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);
    }

    public void SetSelected(bool selected)
    {
        if (selectedStateObject != null)
            selectedStateObject.SetActive(selected);
    }

    private void HandleClicked()
    {
        if (!string.IsNullOrEmpty(mapKey))
            onClicked?.Invoke(mapKey);
    }

    private string GetDisplayName(string key)
    {
        switch (key)
        {
            case Constants.MapNames.RacingTraining:
                return "Training";
            case Constants.MapNames.Zarafshan:
                return "Zarafshan";
            case Constants.MapNames.Registan:
                return "Registan";
            case Constants.MapNames.Egypt:
                return "Egypt";
            case Constants.MapNames.Japan:
                return "Japan";
            case Constants.MapNames.Kansas:
                return "Kansas";
            case Constants.MapNames.PastDargom:
                return "Past Dargom";
            case Constants.MapNames.Chiroqchi:
                return "Chiroqchi";
            default:
                return key;
        }
    }

    private void AutoWire()
    {
        if (mapTitleText == null)
            mapTitleText = GetComponentInChildren<TMP_Text>(true);

        if (lockIcon == null)
        {
            Transform lockTransform = transform.Find("LockIcon");
            if (lockTransform != null)
                lockIcon = lockTransform.gameObject;
        }

        if (selectedStateObject == null)
        {
            Transform selectedTransform = transform.Find("SelectedState");
            if (selectedTransform != null)
                selectedStateObject = selectedTransform.gameObject;
        }
    }
}
