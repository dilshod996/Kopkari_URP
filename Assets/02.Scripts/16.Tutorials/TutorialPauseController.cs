using UnityEngine;

public sealed class TutorialPauseController : MonoBehaviour
{
    private static TutorialPauseController instance;
    private int pauseRequests;
    private float previousTimeScale = 1f;

    public static bool IsPaused => instance != null && instance.pauseRequests > 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Apply(TutorialTimeMode timeMode)
    {
        if (timeMode == TutorialTimeMode.PauseGame)
            Pause();
        else
            ResumeAll();
    }

    public static void Pause()
    {
        TutorialPauseController controller = GetOrCreate();
        controller.pauseRequests++;

        if (controller.pauseRequests == 1)
        {
            controller.previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public static void Resume()
    {
        if (instance == null || instance.pauseRequests <= 0)
            return;

        instance.pauseRequests--;
        if (instance.pauseRequests == 0)
            Time.timeScale = instance.previousTimeScale;
    }

    public static void ResumeAll()
    {
        if (instance == null)
            return;

        instance.pauseRequests = 0;
        Time.timeScale = instance.previousTimeScale <= 0f ? 1f : instance.previousTimeScale;
    }

    private static TutorialPauseController GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject go = new GameObject(nameof(TutorialPauseController));
        instance = go.AddComponent<TutorialPauseController>();
        DontDestroyOnLoad(go);
        return instance;
    }
}
