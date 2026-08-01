// Assets/Editor/UltraScreenshot.cs
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class UltraScreenshot : EditorWindow
{
    private const int Width = 1920;
    private const int Height = 1080;

    [MenuItem("Tools/Ultra Screenshot")]
    public static void ShowWindow()
    {
        GetWindow<UltraScreenshot>("Ultra Screenshot");
    }

    private void OnGUI()
    {
        GUILayout.Label("Google Play Phone Screenshot", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Game View resolutionini 1920 x 1080 qilib tanlang.",
            MessageType.Info
        );

        EditorGUILayout.LabelField("Output", $"{Width} × {Height}");

        if (GUILayout.Button("Capture Phone Screenshot"))
        {
            CaptureScreenshot();
        }
    }

    private void CaptureScreenshot()
    {
        string directory = Path.Combine(
            Application.dataPath,
            "../Screenshots"
        );

        Directory.CreateDirectory(directory);

        string filePath = Path.Combine(
            directory,
            $"PhoneScreenshot_{Width}x{Height}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        );

        // Game View 1920×1080 bo‘lsa, natija ham 1920×1080 chiqadi.
        ScreenCapture.CaptureScreenshot(filePath, 1);

        Debug.Log($"✅ Screenshot saved: {filePath}");
    }
}