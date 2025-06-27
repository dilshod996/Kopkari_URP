using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoparPartsTest : MonoBehaviour
{
    [SerializeField] private Image choparPartIcon;
    [SerializeField] private string namePart;
    [SerializeField] private float life;
    [SerializeField] private float defend;
    [SerializeField] private float height;
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
        ChoparParts choparParts = new ChoparParts
        {
            iconName = choparPartIcon.sprite,
            name = namePart,
            life = life,
            defend = defend,
            height = height,
            isOpen = isOpen,
            isEquipped = isEquipped,
            cost = cost,
        };
        customInfoPopup.SetChoparDetails(choparParts);
    }
}
