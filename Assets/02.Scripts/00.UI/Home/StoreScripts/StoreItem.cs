using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StoreItem : MonoBehaviour
{
    public CategoryType categoryType;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text btnText;
    [SerializeField] private int titleId;
    [SerializeField] private Button viewBtn;

    public ProductContainer productContainer;
    void Start()
    {
        viewBtn.onClick.AddListener(() => OpenProductPanel());

    }

    private void OpenProductPanel()
    {
        productContainer.gameObject.SetActive(true);
        productContainer.OpenStore(categoryType);
    }
    private void OnEnable()
    {
        UITransilitions();
    }
    
    private void UITransilitions()
    {
        titleText.text = LanguageManager.Instance.GetText(titleId);
        btnText.text = LanguageManager.Instance.GetText(190);
    }
}
