using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDisablerRacing : MonoBehaviour
{
    public enum PlayerTuto
    {
        Stop,
        Obstacle
    }
    public PlayerTuto tuto = PlayerTuto.Stop;
    public static event Action OnHorseStopped;
    private bool isTutorialNot=false;
    private void OnEnable()
    {
        RacingTutorials.OnDontShowTutorial += ShowTutorial;
    }
    private void OnDisable()
    {
        RacingTutorials.OnDontShowTutorial -= ShowTutorial;
    }
    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isTutorialNot) return;
        if (!other.CompareTag("RacingHead")) return;
        var agent = other.GetComponentInParent<RacingAgent>();
        if(tuto == PlayerTuto.Stop)
        {
            if (agent.isPlayer)
            {
                RacingController.Instance.StopHorseImmideate();
                OnHorseStopped?.Invoke();
            }
        }
        else
        {
            if (agent.isPlayer)
            {
                HorseMine.OnObstacleTouchedEvent?.Invoke();
                RacingTutorials.OnWallObstacleTutorial?.Invoke();
            }
        }

    }
    private void ShowTutorial(bool sow)
    {
        isTutorialNot = sow;
    }

}
