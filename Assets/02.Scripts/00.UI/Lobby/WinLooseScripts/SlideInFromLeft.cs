using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlideInFromLeft : MonoBehaviour
{
    [Header("Target anchored X position (center of the panel)")]
    public float targetX = 266f;

    [Header("Slide offset (hidden start X)")]
    public float slideOffsetX = -210f;

    [Header("Overshoot (go slightly beyond then come back)")]
    public float overshootX = 20f;

    [Header("Animation settings")]
    public float slideDuration = 0.4f;
    public float fadeDuration = 0.3f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isVisible = false;

    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text percentageAdd;
    [SerializeField] private TMP_Text btnText;
    [SerializeField] private Button getBtn;
    [SerializeField] private Image foodImage;

    [Header("Food Sprites")]
    [SerializeField] private List<Sprite> foodSprites;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        rectTransform.anchoredPosition = new Vector2(slideOffsetX, rectTransform.anchoredPosition.y);
    }

    #region Food Details

    public void FoodInfo(int titleId, FoodCategory category)
    {
        string title = LanguageManager.Instance.GetText(titleId);
        int amount = 0;
        Sprite selectedSprite = null;

        switch (category)
        {
            case FoodCategory.Bugdoy:
                amount = 10;
                selectedSprite = foodSprites.Count > 0 ? foodSprites[0] : null;
                break;
            case FoodCategory.Arpa:
                amount = 15;
                selectedSprite = foodSprites.Count > 1 ? foodSprites[1] : null;
                break;
            case FoodCategory.Water:
                amount = 10;
                selectedSprite = foodSprites.Count > 2 ? foodSprites[2] : null;
                break;
            case FoodCategory.StaminaWater:
                amount = 15;
                selectedSprite = foodSprites.Count > 3 ? foodSprites[3] : null;
                break;
        }

        if (titleText) titleText.text = title;
        if (percentageAdd) percentageAdd.text = amount + "%";
        if (foodImage && selectedSprite) foodImage.sprite = selectedSprite;
        if (btnText) btnText.text = LanguageManager.Instance.GetText(263);
    }

    #endregion

    #region Animation Control

    public void ToggleSlide(int titleId, FoodCategory foodCategory)
    {
        if (isVisible)
        {
            SlideOut();
            getBtn?.onClick.RemoveAllListeners();
        }
        else
        {
            SlideIn();
            FoodInfo(titleId, foodCategory);
            getBtn?.onClick.RemoveAllListeners();
            getBtn?.onClick.AddListener(() => Debug.Log("category: " + foodCategory));
        }
    }

    public void SlideIn()
    {
        isVisible = true;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        LeanTween.alphaCanvas(canvasGroup, 1f, fadeDuration);

        float overshootTarget = targetX + overshootX;
        LeanTween.moveX(rectTransform, overshootTarget, slideDuration * 0.7f)
            .setEase(LeanTweenType.easeOutCubic)
            .setOnComplete(() =>
            {
                LeanTween.moveX(rectTransform, targetX, slideDuration * 0.3f)
                         .setEase(LeanTweenType.easeInCubic);
            });
    }

    public void SlideOut()
    {
        isVisible = false;

        LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration);

        LeanTween.delayedCall(fadeDuration, () =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });

        LeanTween.moveX(rectTransform, slideOffsetX, slideDuration).setEase(LeanTweenType.easeInExpo);
    }

    #endregion
}
