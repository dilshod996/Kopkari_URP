using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AutoScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Scroll Settings")]
    public ScrollRect scrollRect;
    [Tooltip("Time in seconds for a full scroll pass")]
    public float autoScrollTime = 10f;

    private bool isUserScrolling = false;
    private float scrollSpeed;
    private Coroutine resumeScrollCoroutine;

    private void OnEnable()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null) return;

        scrollRect.verticalNormalizedPosition = 1f;
        scrollSpeed = autoScrollTime > 0f ? 1f / autoScrollTime : 0f;
        isUserScrolling = false;
    }

    private void OnDisable()
    {
        StopResumeCoroutine();
    }

    private void Update()
    {
        if (scrollRect == null || isUserScrolling || scrollSpeed <= 0f) return;

        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;

        if (scrollRect.verticalNormalizedPosition <= 0f)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isUserScrolling = true;
        StopResumeCoroutine();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        resumeScrollCoroutine = StartCoroutine(ResumeAfterDelay(3f));
    }

    private IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUserScrolling = false;
        resumeScrollCoroutine = null;
    }

    private void StopResumeCoroutine()
    {
        if (resumeScrollCoroutine == null) return;

        StopCoroutine(resumeScrollCoroutine);
        resumeScrollCoroutine = null;
    }
}
