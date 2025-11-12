using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AutoScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Scroll Sozlamalari")]
    public ScrollRect scrollRect;
    [Tooltip("Scrollni to¡®liq yurish vaqti (sekundlarda)")]
    public float autoScrollTime = 10f;

    private bool isUserScrolling = false;
    private float scrollSpeed;

    private Coroutine resumeScrollCoroutine;

    private void OnEnable()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        scrollRect.verticalNormalizedPosition = 1f;

        scrollSpeed = 1f / autoScrollTime;
        isUserScrolling = false;
    }

    private void Update()
    {
        if (!isUserScrolling)
        {
            scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;

            if (scrollRect.verticalNormalizedPosition <= 0f)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isUserScrolling = true;

        // Agar drag vaqtida eski coroutine ishlayotgan bo¡®lsa, to¡®xtatamiz
        if (resumeScrollCoroutine != null)
        {
            StopCoroutine(resumeScrollCoroutine);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 3 sekunddan keyin auto scrollni qayta yoqamiz
        resumeScrollCoroutine = StartCoroutine(ResumeAfterDelay(3f));
    }

    private IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUserScrolling = false;
    }
}
