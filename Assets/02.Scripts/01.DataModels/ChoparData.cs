using System;
using UnityEngine;

[Serializable]
public class ChoparData
{
    public string name;
    public float catchTime;
    public int strength;
    public int power;
    public Sprite iconName; // icondi ozi buyerda hamda buni Addressable address bilan ozgartirishimiz kerak
    public int isOpen;
    public int isEquipped;
    public int cost;

    // Customization bog¡®lovchilari
    public string headsetId;
    public string upperBodyId;
    public string lowerBodyId;
    public string horseArmorId;
}
