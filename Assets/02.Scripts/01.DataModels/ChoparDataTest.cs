using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoparDataTest : MonoBehaviour
{
    [SerializeField] private Image choparImage;
    [SerializeField] private string nameChopar;
    [SerializeField] private float catchTime;
    [SerializeField] private int strength;
    [SerializeField] private int power;
    [SerializeField] private int isOpen;
    [SerializeField] private int isEquipped;
    [SerializeField] private int cost;

    [SerializeField] private Button infoButton;
    [SerializeField] private CustomInfoPopup customInfoPopup;
    void Start()
    {
        infoButton.onClick.AddListener(SetChoparData);

    }

    private void SetChoparData()
    {
        ChoparData choparData = new ChoparData
        {
            iconName = choparImage.sprite,
            name = nameChopar,
            catchTime = catchTime,
            strength = strength,
            power = power,
            isOpen = isOpen,
            isEquipped = isEquipped,
            cost = cost,
        };
        customInfoPopup.SetChoparData(choparData);
    }

}
