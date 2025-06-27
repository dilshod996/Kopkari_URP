using TMPro;
using UnityEngine;
using UnityEngine.UI; // Include for Text (use TMPro if you're using TextMeshPro)

public class FPSDisplay : MonoBehaviour
{
    public TMP_Text fpsText; // Reference to the Text component
    // If using TextMeshPro, use this instead:
    // public TMP_Text fpsText; 

    private float deltaTime = 0.0f;

    void Update()
    {
        // Calculate the FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        // Update the UI Text with the FPS value
        fpsText.text = "FPS: " + Mathf.Ceil(fps).ToString();
    }
}
