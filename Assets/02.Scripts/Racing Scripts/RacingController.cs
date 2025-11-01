using MalbersAnimations.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RacingController : MonoBehaviour
{
    public static RacingController Instance { get; protected set; }
    public MAnimal horse;
    [SerializeField] private List<AIRacingRider> aiRiders;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartRacing()
    {
        EnableNavMesh();
        StartRun();
    }

    public void StopRacing()
    {
        DisableNavmesh();
        StopAlwaysForward();
    }
    #region AI Horses
    public void EnableNavMesh()
    {
        for(int i = 0; i < aiRiders.Count; i++)
        {
            aiRiders[i].EnableNavmesh();
        }
    }
    public void DisableNavmesh()
    {
        for(int i = 0;i < aiRiders.Count; i++)
        {
            aiRiders[i].DisableNavmesh();
        }
    }
    #endregion
    #region Horse Manage
    //public void HorseRunStarter(MAnimal mAnimal)
    //{
    //    horse = mAnimal;
    //    if(!horse)
    //    {
    //        horse.Always_Forward(true);
    //        Debug.Log("Forward is " + horse.AlwaysForward);
    //    }
    //    else
    //    {
    //        Debug.Log("Horse Forward null");
    //    }
    //}
    public void StartRun()
    {
        StartHorseRun(horse);
    }
    public void GetSetAnimal(MAnimal mAnimal)
    {
        horse = mAnimal;
    }
    public void StartHorseRun(MAnimal mAnimal)
    {
        StartCoroutine(HorseRunStarter(mAnimal));
    }

    private IEnumerator HorseRunStarter(MAnimal mAnimal)
    {
        horse = mAnimal;

        // horse null bo¡®lmaguncha kutadi
        yield return new WaitUntil(() => horse != null);

        horse.Always_Forward(true);
    }
    private void StopAlwaysForward()
    {
        horse.Always_Forward(false);
    }
    #endregion

    #region Scene Details.
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }
    #endregion
}
