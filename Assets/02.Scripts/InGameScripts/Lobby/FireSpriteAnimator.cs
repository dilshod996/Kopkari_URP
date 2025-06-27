using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FireSpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;            // Sprite sheet dan ajratilgan frame'lar
    public float frameRate = 0.1f;     // Har bir frame o¡®rtasidagi vaqt

    private Image imageComponent;
    private Coroutine animationCoroutine;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    void OnEnable()
    {
        animationCoroutine = StartCoroutine(PlayAnimation());
    }

    void OnDisable()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }

    IEnumerator PlayAnimation()
    {
        int currentFrame = 0;

        while (true)
        {
            imageComponent.sprite = frames[currentFrame];
            currentFrame = (currentFrame + 1) % frames.Length;
            yield return new WaitForSeconds(frameRate);
        }
    }
}
