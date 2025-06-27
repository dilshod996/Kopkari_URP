using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIStore : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
    private void OnEnable()
    {
        titleText.text = LanguageManager.Instance.GetText(25);
    }
}
