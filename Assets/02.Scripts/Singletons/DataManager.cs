using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;


    //Lobby Animation Info
    public bool animPlayed=false;

    public string PlayerName;
    public string HorseName;

    //Player has quy yoki uloqcha

    public bool hasObj = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameObject singletonFolder = GameObject.Find("Singletons");
            if (singletonFolder == null)
            {
                singletonFolder = new GameObject("Singletons");
                DontDestroyOnLoad(singletonFolder);
            }
            transform.SetParent(singletonFolder.transform);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }
    public bool LobbyAnimPlayed()
    {
        return animPlayed;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
