using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoScene : MonoBehaviour
{
    public Animator taqaAC;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("videoOt"))
        {
            taqaAC.SetTrigger("playTaqa");
            Debug.Log("videoOt");
            //taqaAC
        }
    }


}
