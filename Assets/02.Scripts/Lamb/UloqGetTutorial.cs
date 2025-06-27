using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UloqGetTutorial : MonoBehaviour
{
    [SerializeField] private TutorialScript tutorialScript;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (PracticeRoomManager.Instance != null && !PracticeRoomManager.Instance.IsCatched)
            {
                tutorialScript.SHowBoboginamNearUloq("Uloqqa yaqinroq keling polvon, yana ham yaqinroq");
            }

            
        }
    }
}
