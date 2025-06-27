using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Booster : MonoBehaviour
{
    public enum BoosterType
    {
        SprintFull,
        SprintByTime,
        Defend,
        WalkZone,
        TimeBooster
    }

    public BoosterType boosterType;
    public UIFadeInOut boosterUX;
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Booster Triggered: " + boosterType + " by " + other.tag);
        if(other.CompareTag("Player"))
        {
            AttackDefendManager player = other.GetComponentInChildren<AttackDefendManager>();
            if (player != null)
            {
                Debug.Log("player not null");
                switch (boosterType)
                {
                    case BoosterType.SprintFull:
                        Color32 sprintColorFull = new Color32(247, 220, 89, 255);
                        boosterUX.BoosterUI(sprintColorFull);
                        BaseManager.Instance.SpeedBoosterGet(2);
                        break;
                    case BoosterType.SprintByTime:
                        Color32 sprintColorTime = new Color32(64, 236, 0, 62);
                        boosterUX.BoosterUI(sprintColorTime);
                        BaseManager.Instance.SpeedBoosterGet(5);
                       // player.SprintByTime();
                        break;
                    case BoosterType.Defend:
                        Color32 defendColor = new Color32(185, 242, 255, 255);
                        boosterUX.BoosterUI(defendColor);
                        player.AddDefend();
                        break;
                    case BoosterType.WalkZone:
                        player.AddWalkZone();
                        Color32 walkZoneColor = new Color32(225, 164, 56, 255);
                        boosterUX.BoosterUI(walkZoneColor);
                        break;
                    case BoosterType.TimeBooster:
                        Color32 timeBoosterColor = new Color32(28, 140, 165, 255);
                        boosterUX.BoosterUI(timeBoosterColor);
                        BaseManager.Instance.mainTime += 5;
                       // player.TimeBooster();
                        break;
                }
                Destroy(gameObject);
            }
            
        }
        else if (other.CompareTag("NPC"))
        {
            AttackDefendManager npc = other.GetComponentInChildren<AttackDefendManager>();
            if (npc != null)
            {
                switch (boosterType)
                {
                    case BoosterType.SprintFull:
                        //npc.SprintFull();
                        break;
                    case BoosterType.SprintByTime:
                        //npc.SprintByTime();
                        break;
                    case BoosterType.Defend:
                        npc.AddDefend();
                        break;
                    case BoosterType.WalkZone:
                        npc.AddWalkZone();
                        break;
                    case BoosterType.TimeBooster:
                        //BaseManager.Instance.mainTime += 5;
                        //npc.TimeBooster();
                        break;
                }
            }
            Destroy(gameObject);
        }
    }
    

}
