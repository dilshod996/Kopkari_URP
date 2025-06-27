using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadMangager : MonoBehaviour
{
    public static SceneLoadMangager Instance;
    public enum SceneType
    {
        None,
        Intro,
        Loading,
        Lobby,
        PracticeRoom,
        AvatarCustom, 
        Beginer,
        TrainingRoom,
        TrainingRoom2,
        Jomboy
    }

    public SceneType CurrentSceneType { get; private set; } = SceneType.None;
    public SceneType PreviousSceneType { get; private set; } = SceneType.None;
    public float loadingTime = 7f;

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

    public void LoadScene(SceneType newScene)
    {
        if (newScene == SceneType.Loading)
            return;

        StartCoroutine(LoadSceneWithTransition(newScene));
    }

    private IEnumerator LoadSceneWithTransition(SceneType newScene)
    {
        PreviousSceneType = CurrentSceneType;
        CurrentSceneType = SceneType.Loading;
        SceneManager.LoadScene(SceneType.Loading.ToString());
       // SoundManager.Instance.StopMusicEvent();
        yield return new WaitForSeconds(loadingTime);

        SceneManager.LoadScene(newScene.ToString());
        CurrentSceneType = newScene;
    }
}
