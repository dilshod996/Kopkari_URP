using System.Collections;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.LeanMoveY(closeY, 0);
        OpenCard();
        if (viewButton != null)
        {
            
            viewButton.onClick.AddListener(() => OnClick());
        }
        else
        {
            Debug.LogWarning("View Button is not assigned in PremiumCard.");
        }
        titleText.text = LanguageManager.Instance.GetText(titleID);
        viewText.text = LanguageManager.Instance.GetText(190); 
    }
    private void OnDisable()
    {
        rectTransform.LeanMoveY(closeY, 0);
    }
    private void OpenCard()
    {
        rectTransform.LeanMoveY(openY, startAnimDuration);
    }
    private void OnClick()
    {
        premiumShower.gameObject.SetActive(true);
        premiumShower.OpenStore(premiumCategory);
    }
}
