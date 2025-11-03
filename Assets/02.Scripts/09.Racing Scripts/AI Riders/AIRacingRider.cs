using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIRacingRider : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    private void Awake()
    {
        DisableNavmesh();
    }


    #region Disable and Enable Rider Navmesh
    public void DisableNavmesh()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }
    public void EnableNavmesh()
    {
        if(agent != null)
        {
            agent.isStopped = false;
        }
    }
    #endregion
}
