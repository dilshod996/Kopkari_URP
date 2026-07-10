using System.Collections;
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
    private Coroutine flipCoroutine;


    private void Awake()
    {
        cardBtn = GetComponent<Button>();
    }

    private void OnEnable()
    {
        ResetCard();

        if (cardBtn != null)
            cardBtn.onClick.AddListener(OnCardClick);
    }

    private void OnDisable()
    {
        if (cardBtn != null)
            cardBtn.onClick.RemoveListener(OnCardClick);

        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        LeanTween.cancel(gameObject);
        isAnimating = false;
    }

    public void Setup(PremiumData premiumData)
    {
        if (premiumData == null) return;

        var language = LanguageManager.Instance;
        if (description != null && language != null)
            description.text = language.GetText(premiumData.descriptionId);
        if (iconImage != null)
            iconImage.sprite = premiumData.icon;
        if (infoText != null && language != null)
            infoText.text = language.GetText(premiumData.infoId);
    }

    #region Flip Animation
    public void OnCardClick()
    {
        if (isAnimating) return;
        flipCoroutine = StartCoroutine(FlipCard());
    }

    private IEnumerator FlipCard()
    {
        isAnimating = true;

        // Step 1: Scale to 0 (invisible horizontally)
        LeanTween.scaleX(gameObject, 0f, flipTime);
        yield return new WaitForSeconds(flipTime);

        // Step 2: Toggle sides
        if (frontSide != null)
            frontSide.SetActive(isFlipped);
        if (backSide != null)
            backSide.SetActive(!isFlipped);
        isFlipped = !isFlipped;

        // Step 3: Scale back to 1 (visible)
        LeanTween.scaleX(gameObject, 1f, flipTime);
        yield return new WaitForSeconds(flipTime);

        isAnimating = false;
        flipCoroutine = null;
    }

    private void ResetCard()
    {
        LeanTween.cancel(gameObject);
        transform.localScale = Vector3.one;
        isFlipped = false;
        isAnimating = false;

        if (frontSide != null)
            frontSide.SetActive(true);
        if (backSide != null)
            backSide.SetActive(false);
    }
    #endregion
}
