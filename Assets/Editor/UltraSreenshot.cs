// Assets/Editor/UltraScreenshot.cs
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class UltraScreenshot : EditorWindow
{
    int width = 5906;   // Default 100cm @150DPI
    int height = 11811; // Default 200cm @150DPI

    [MenuItem("Tools/Ultra Screenshot")]
    public static void ShowWindow()
    {
        GetWindow<UltraScreenshot>("Ultra Screenshot");
    }

    void OnGUI()
    {
        GUILayout.Label("High Resolution Screenshot", EditorStyles.boldLabel);
        width = EditorGUILayout.IntField("Width (px)", width);
        height = EditorGUILayout.IntField("Height (px)", height);

        if (GUILayout.Button("Capture GameView"))
        {
            CaptureScreenshot(width, height);
        }
    }

    void CaptureScreenshot(int w, int h)
    {
        string dir = Path.Combine(Application.dataPath, "../Screenshots");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, $"Poster_{w}x{h}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        // Bu joyda CaptureScreenshot UI bilan birga GameView’ni oladi
        ScreenCapture.CaptureScreenshot(filePath, 1);
        Debug.Log("✅ Screenshot saved to: " + filePath);
    }
}
