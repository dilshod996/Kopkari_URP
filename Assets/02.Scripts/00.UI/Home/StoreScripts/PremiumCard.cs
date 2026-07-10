using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PremiumCard : MonoBehaviour
{
    [Header("Start Anim Details")]
    private RectTransform rectTransform;
    [SerializeField] private float startAnimDuration = 0.3f;
    [SerializeField] private float closeY = 0f;
    [SerializeField] private float openY = 100f;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text viewText;

    [SerializeField] private int titleID = -1;
    [Header("UI Buttons")]
    public PremiumCategoryType premiumCategory;
    [SerializeField] private Button viewButton;
    public PremiumShower premiumShower;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            LeanTween.cancel(rectTransform.gameObject);
            rectTransform.LeanMoveY(closeY, 0);
        }

        OpenCard();
        if (viewButton != null)
        {
            viewButton.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning("View Button is not assigned in PremiumCard.");
        }

        var language = LanguageManager.Instance;
        if (language == null) return;

        if (titleText != null)
            titleText.text = language.GetText(titleID);
        if (viewText != null)
            viewText.text = language.GetText(190); 
    }
    private void OnDisable()
    {
        if (viewButton != null)
            viewButton.onClick.RemoveListener(OnClick);

        if (rectTransform == null) return;

        LeanTween.cancel(rectTransform.gameObject);
        rectTransform.LeanMoveY(closeY, 0);
    }
    private void OpenCard()
    {
        if (rectTransform != null)
            rectTransform.LeanMoveY(openY, startAnimDuration);
    }
    private void OnClick()
    {
        if (premiumShower == null) return;

        premiumShower.gameObject.SetActive(true);
        premiumShower.OpenStore(premiumCategory);
    }
}
