using UnityEngine;
using UnityEngine.InputSystem;

public class HighResScreenshot : MonoBehaviour
{
    [Header("Output Settings")]
    public string screenshotFileName = "PosterCapture_UI.png";
    public int resolutionMultiplier = 4; // 1 = asl o‘lcham, 2/3/4 = yuqori sifat

    [Header("Input Settings")]
    public Key screenshotKey = Key.P;

    void Update()
    {
        if (Keyboard.current[screenshotKey].wasPressedThisFrame)
        {
            TakeFullScreenshotWithUI();
        }
    }

    void TakeFullScreenshotWithUI()
    {
        string fullPath = System.IO.Path.Combine(Application.dataPath, screenshotFileName);

        // Ushbu metod UI (Canvas) bilan to‘liq skrin qiladi
        ScreenCapture.CaptureScreenshot(fullPath, resolutionMultiplier);

        Debug.Log($"✅ Full screenshot with UI saved: {fullPath}");
    }
}
