using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationUI : MonoBehaviour
{
    [Header("Notification UI Settings")]
    [Tooltip("The duration for which the notification will be displayed in seconds.")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text closeText;
    [SerializeField] private TMP_Text contentText;
    void Start()
    {
        
    }
    private void OnEnable()
    {
        closeBtn.onClick.AddListener(CloseNotification);
        Transilitions();
    }
    private void OnDisable()
    {
        closeBtn.onClick.RemoveListener(CloseNotification);
    }
    private void CloseNotification()
    {
        gameObject.SetActive(false);
    }
    private void Transilitions()
    {
        titleText.text = LanguageManager.Instance.GetText(136);
        contentText.text = LanguageManager.Instance.GetText(138);
        closeText.text = LanguageManager.Instance.GetText(137);
    }
    private void SetNotification(string title, string content, string closeText)
    {
        titleText.text = title;
        contentText.text = content;
        this.closeText.text = closeText;
    }
}
