using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HorseRiding : MonoBehaviour, IPointerClickHandler
{
    public LayerMask targetLayer; // 3D obyektlar uchun layer mask

    public void OnPointerClick(PointerEventData eventData)
    {
        // Agar obyekt faqat kerakli layerda bo'lsa, eventni qayta ishlash
        if (((1 << gameObject.layer) & targetLayer) != 0)
        {
            Debug.Log("Cube bosildi!");
        }
    }

}
