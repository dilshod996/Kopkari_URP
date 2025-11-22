using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapShowPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text mapName;
    [SerializeField] private Image mapImage;
    [SerializeField] private TMP_Text costMap;
    [SerializeField] private TMP_Text mapDetails;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Transilation Texts")]
    [SerializeField] private TMP_Text butBtnText;
    [SerializeField] private TMP_Text cancelBtnText;
    

    public void SetMapData(string mapname, Sprite mapSprite, string cost, string mapdetails)
    {
        mapName.text = mapname;
        mapImage.sprite = mapSprite;
        costMap.text = cost;
        mapDetails.text= mapdetails;
    }
    public void ClosePopup()
    {
        HomeMainUI.Instance.HideUI(this);
    }
}
