using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackViewCam : MonoBehaviour
{
    [Header("References")]
    public Camera backViewCamera;
    public RectTransform parentRawImage;
    public RawImage backViewUI;
    public RenderTexture renderTexture;
    public Transform mainCamera;
    public float duration = 1f;

    [Header("Offset Settings")]
    public float distance = 2f;
    public float height = 1.5f;

    private Coroutine uiSlideCoroutine;

    void Start()
    {
        if (renderTexture != null && backViewCamera != null)
        {
            backViewCamera.targetTexture = renderTexture;
        }

        SetBackViewState(false); // Boshlanishda yashiringan
    }

    private void LateUpdate()
    {
        if (backViewCamera != null && mainCamera != null)
        {
            UpdateBackCameraPosition();
        }
    }

    public void UpdateBackCameraPosition()
    {
        Vector3 offset = -mainCamera.forward * distance + Vector3.up * height;
        transform.position = mainCamera.position + offset;
        transform.LookAt(mainCamera.position + Vector3.up * height);
    }

    public void SetBackViewState(bool isActive)
    {
        if (backViewCamera != null)
            backViewCamera.enabled = isActive;

        if (backViewUI.gameObject.activeSelf != isActive)
            backViewUI.gameObject.SetActive(isActive);

        if (uiSlideCoroutine != null)
            StopCoroutine(uiSlideCoroutine);

        if (isActive)
        {
            uiSlideCoroutine = StartCoroutine(SmoothMove(271f, -71f, duration)); // pastga tush
        }
        else
        {
            uiSlideCoroutine = StartCoroutine(SmoothMove(-71f, 271f, duration)); // yuqoriga qayt
        }
    }

    IEnumerator SmoothMove(float fromY, float toY, float duration)
    {
        float elapsed = 0f;
        Vector2 startPos = new Vector2(parentRawImage.anchoredPosition.x, fromY);
        Vector2 endPos = new Vector2(parentRawImage.anchoredPosition.x, toY);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            parentRawImage.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        parentRawImage.anchoredPosition = endPos;
    }
}
