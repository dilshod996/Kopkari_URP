using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentLoadingUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text mapNameText;

    [SerializeField] private Sprite utovEnvIcon;
    [SerializeField] private Sprite egyptEnvIcon;
    private void OnEnable()
    {
        EnvironmentCardUI.OnEnvironmentNameChanged += SetMapData;
    }
    private void OnDisable()
    {
        EnvironmentCardUI.OnEnvironmentNameChanged -= SetMapData;
    }
    public void SetMapData(string mapName)
    {
        Debug.Log("Map changing to " + mapName);
        switch(mapName)
        {
            case Constants.MapNames.Zarafshan:
                mapNameText.text = LanguageManager.Instance.GetText(27);
                iconImage.sprite = utovEnvIcon;
                break;
            case Constants.MapNames.Egypt:
                mapNameText.text = LanguageManager.Instance.GetText(410);
                iconImage.sprite = egyptEnvIcon;
                break;
        }
    }
}
