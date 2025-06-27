using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HorsePartsTest : MonoBehaviour
{
    [SerializeField] private Image horseDetalSprite;
    [SerializeField] private string nameDetal;
    [SerializeField] private float lifeTime;
    [SerializeField] private float defendPercentage;
    [SerializeField] private float weight;
    [SerializeField] private int isOpen;
    [SerializeField] private int isEquipped;
    [SerializeField] private int cost;

    [SerializeField] private Button infoButton;
    [SerializeField] private CustomInfoPopup customInfoPopup;
    void Start()
    {
        infoButton.onClick.AddListener(SetHorsePartData);
    }

    public void SetHorsePartData()
    {
        HorseParts choparParts = new HorseParts
        {
            iconName = horseDetalSprite.sprite,
            name = nameDetal,
            life = lifeTime,
            defend = defendPercentage,
            weight = weight,
            isOpen = isOpen,
            isEquipped = isEquipped,
            cost = cost,
        };
        customInfoPopup.SetHorseDetails(choparParts);
    }
}
