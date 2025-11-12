using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodRemoveMotion : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 originalPosition;

    [SerializeField] private float duration = 0.5f; // Inspector orqali sozlanadigan vaqt
    [Header("Main Details")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text percentageAddedText;
    [SerializeField] private TMP_Text amountDecreaseText;

    [Header("Food Sprites")]
    [SerializeField] private Sprite bugdoySprite;
    [SerializeField] private Sprite arpaSprite;
    [SerializeField] private Sprite olmaSprite;
    [SerializeField] private Sprite suvSprite;
    [SerializeField] private Sprite chidamliSuvSprite;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }
    public void SetFoodDetails(string foodName, string percentageAdded, string amountDecrease)
    {
        Sprite icon = null; // Default value in case no match is found
        switch(foodName)
        {
            case Constants.Prizes.Bugdoy:
                icon = bugdoySprite;
                break;
            case Constants.Prizes.Arpa:
                icon = arpaSprite;
                break;
            case Constants.Prizes.Apple:
                icon = olmaSprite;
                break;
            case Constants.Prizes.Water:
                icon = suvSprite;
                break;
            case Constants.Prizes.StaminWater:
                icon = chidamliSuvSprite;
                break;
            default:
                Debug.Log("Unknown food name: " + foodName);
                break;
        }
        iconImage.sprite = icon;
        percentageAddedText.text = percentageAdded;
        amountDecreaseText.text = amountDecrease;
        MoveItemMotion();
    }
    public void MoveItemMotion()
    {
        StartCoroutine(MoveAndReset());
    }

    private IEnumerator MoveAndReset()
    {
        Vector2 startPos = originalPosition;
        Vector2 endPos = new Vector2(originalPosition.x, -1000f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = endPos;

        yield return new WaitForSeconds(0.5f);

        rectTransform.anchoredPosition = originalPosition;
    }
}
