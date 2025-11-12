using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodDetails : MonoBehaviour
{
    public FoodCategory foodCategory;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private int titleId = -1;
    [SerializeField] private TMP_Text percentageAdd;
    [SerializeField] private int percentageAmout = 10;
    [SerializeField] private TMP_Text btnText;

    [Header("UI Settings")]
    [SerializeField] private Button getBtn;

    [SerializeField] private List<Sprite> foodSprites;
    void Start()
    {
        getBtn.onClick.AddListener(() =>
        {
            // Handle button click logic here
            Debug.Log("Get Button Clicked for " + foodCategory);
        });
    }
    private void OnEnable()
    {
        FoodInfo();
    }
    private void FoodInfo()
    {
        titleText.text = LanguageManager.Instance.GetText(titleId);
        percentageAdd.text = percentageAmout.ToString() + "%";
        btnText.text = LanguageManager.Instance.GetText(263);
        //switch (foodCategory)
        //{
        //    case FoodCategory.Bugdoy:
        //        titleText.text = LanguageManager.Instance.GetText(titleId);
        //        percentageAdd.text = percentageAmout.ToString() + "%";
        //        break;
        //    case FoodCategory.Arpa:
        //        titleText.text = LanguageManager.Instance.GetText(titleId);
        //        percentageAdd.text = percentageAmout.ToString() + "%";
        //        break;
        //}
    }

}
