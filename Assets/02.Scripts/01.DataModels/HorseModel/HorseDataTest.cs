using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HorseDataTest : MonoBehaviour
{
    [SerializeField] private Image horseImage;
    [SerializeField] private string nameHorse;
    [SerializeField] private float boostSpeed;
    [SerializeField] private int speed;
    [SerializeField] private int energy;
    [SerializeField] private int isOpen;
    [SerializeField] private int isEquipped;
    [SerializeField] private int cost;

    [SerializeField] private Button infoButton;
    [SerializeField] private CustomInfoPopup customInfoPopup;
    void Start()
    {
        infoButton.onClick.AddListener(SetChoparPartData);
    }

    public void SetChoparPartData()
    {
        HorseData horseData = new HorseData
        {
            iconName = horseImage.sprite,
            name = nameHorse,
            boostSpeedTime = boostSpeed,
            speed = this.speed,
            energy = this.energy,
            isOpen = isOpen,
            isEquipped = isEquipped,
            cost = cost,
        };
        customInfoPopup.SetHorseData(horseData);
    }
}
