using System;
using UnityEngine;


[Serializable]
public class HorseParts
{
    public string name;
    public float life;
    public float defend;
    public float weight;
    public Sprite iconName; // icondi ozi buyerda hamda buni Addressable address bilan ozgartirishimiz kerak
    public int isOpen;
    public int isEquipped;
    public int cost;
}
