using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PremiumItem : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text infoText;
    [Header("UI Anim details")]
    [SerializeField] GameObject frontSide;
    [SerializeField] GameObject backSide;
    private bool isFlipped = false;
    private bool isAnimating = false;
    public float flipTime = 0.2f;
    private Button cardBtn;


    private void Start()
    {
        cardBtn = GetComponent<Button>();
        cardBtn.onClick.AddListener(OnCardClick);
    }
    public void Setup(PremiumData premiumData)
    {
        description.text = LanguageManager.Instance.GetText(premiumData.descriptionId);
        iconImage.sprite = premiumData.icon;
        infoText.text = LanguageManager.Instance.GetText(premiumData.infoId);
    }

    #region Flip Animation
    public void OnCardClick()
    {
        if (isAnimating) return;
        StartCoroutine(FlipCard());
    }

    private IEnumerator FlipCard()
    {
        isAnimating = true;

        // Step 1: Scale to 0 (invisible horizontally)
        LeanTween.scaleX(gameObject, 0f, flipTime);
        yield return new WaitForSeconds(flipTime);

        // Step 2: Toggle sides
        frontSide.SetActive(isFlipped);
        backSide.SetActive(!isFlipped);
        isFlipped = !isFlipped;

        // Step 3: Scale back to 1 (visible)
        LeanTween.scaleX(gameObject, 1f, flipTime);
        yield return new WaitForSeconds(flipTime);

        isAnimating = false;
    }
    #endregion
}
