using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUserInfo : MonoBehaviour
{
    [SerializeField] private Button closeBtn;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] private TMP_Text teamName;
    [SerializeField] private TMP_Text rank;
    [SerializeField] private TMP_Text winningCount;
    void Start()
    {
        closeBtn.onClick.AddListener(CloseAction);
    }

    void CloseAction()
    {
       gameObject.SetActive(false);
    }
}
