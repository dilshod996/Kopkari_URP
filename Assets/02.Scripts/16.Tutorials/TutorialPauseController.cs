using UnityEngine;

public sealed class TutorialPauseController : MonoBehaviour
{
    private static TutorialPauseController instance;
    private int pauseRequests;
    private float previousTimeScale = 1f;
    private bool restorePending;

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
        if (instance != this)
            return;

        if (pauseRequests > 0 && !IsExternalPauseActive())
            RestorePreviousTimeScale();

        instance = null;
    }

    private void Update()
    {
        bool externalPauseActive = IsExternalPauseActive();

        // An application/pause-menu resume can restore its own saved time scale.
        // Re-apply the tutorial pause after that owner has finished.
        if (pauseRequests > 0)
        {
            restorePending = false;
            if (!externalPauseActive && Time.timeScale != 0f)
                Time.timeScale = 0f;
            return;
        }

        // If the tutorial finished while another pause owner was active, defer
        // restoration until that owner has closed instead of resuming the game.
        if (restorePending && !externalPauseActive)
        {
            restorePending = false;
            RestorePreviousTimeScale();
        }
    }

    public static void Apply(TutorialTimeMode timeMode)
    {
        if (timeMode == TutorialTimeMode.PauseGame)
        {
            if (!IsPaused)
                Pause();
        }
        else
            ResumeAll();
    }

    public static void Pause()
    {
        TutorialPauseController controller = GetOrCreate();
        controller.pauseRequests++;

        if (controller.pauseRequests == 1)
        {
            controller.previousTimeScale = Time.timeScale > 0f
                ? Time.timeScale
                : 1f;
            controller.restorePending = false;
            Time.timeScale = 0f;
        }
    }

    public static void Resume()
    {
        if (instance == null || instance.pauseRequests <= 0)
            return;

        instance.pauseRequests--;
        if (instance.pauseRequests == 0)
            instance.ReleasePause();
    }

    public static void ResumeAll()
    {
        if (instance == null)
            return;

        instance.pauseRequests = 0;
        instance.ReleasePause();
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

    private void ReleasePause()
    {
        if (IsExternalPauseActive())
        {
            restorePending = true;
            return;
        }

        restorePending = false;
        RestorePreviousTimeScale();
    }

    private void RestorePreviousTimeScale()
    {
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    private static bool IsExternalPauseActive()
    {
        return KopkariMainUI.IsGameplayPaused;
    }
}
