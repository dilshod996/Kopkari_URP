using System;
using UnityEngine;


[Serializable]
public class ChoparParts
{
    public string name;
    public Sprite iconName; // icondi ozi buyerda hamda buni Addressable address bilan ozgartirishimiz kerak
    public float life;
    public float defend;
    public float height;
    public int isOpen;
    public int isEquipped;
    public int cost;
}