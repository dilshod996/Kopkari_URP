using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KopkariIntro : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private KopkariIntroFlowController flowController;

    [Header("Player List")]
    [SerializeField] private GameObject playerListPanel;
    [SerializeField] private KopkariIntroPlayersList playersList;

    [Header("Controls")]
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipButtonText;

    [Header("Gameplay Countdown")]
    [SerializeField] private GameObject countdownBackground;
    [SerializeField] private TMP_Text countdownText;

    private Action completionCallback;

    public KopkariIntroPlayersList PlayersList => playersList;

    private void Awake()
    {
        flowController?.SetIntroPage(this);
    }

    private void OnEnable()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(HandleSkipClicked);
        if(LanguageManager.Instance != null && skipButtonText != null)
            skipButtonText.text = LanguageManager.Instance.GetText(553);
    }

    private void OnDisable()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    public void PrepareHidden()
    {
        SetPlayerListVisible(false);
        SetSkipVisible(false);
        SetCountdownVisible(false);
        gameObject.SetActive(false);
    }

    public void Play(Action onComplete)
    {
        completionCallback = onComplete;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SetPlayerListVisible(false);
        SetSkipVisible(false);
        SetCountdownVisible(false);

        if (flowController == null || !flowController.isActiveAndEnabled)
        {
            CompleteIntro();
            return;
        }

        flowController.SetIntroPage(this);
        flowController.PlayIntro(CompleteIntro);
    }

    public void BuildPlayerList(IReadOnlyList<AIKopkariRider> riders)
    {
        playersList?.BuildList(riders);
    }

    public void RefreshPlayerList()
    {
        playersList?.RefreshReadiness();
    }

    public void SetPlayerListVisible(bool visible)
    {
        if (playerListPanel != null)
            playerListPanel.SetActive(visible);
    }

    public void SetSkipVisible(bool visible)
    {
        if (skipButton == null)
            return;

        skipButton.gameObject.SetActive(visible);
        skipButton.interactable = visible;
    }

    public void SetCountdownVisible(bool visible)
    {
        if (countdownBackground != null)
            countdownBackground.SetActive(visible);
        if (!visible && countdownText != null)
            countdownText.text = string.Empty;
    }

    public void SetCountdownValue(int seconds)
    {
        if (countdownText != null)
            countdownText.text = Mathf.Max(0, seconds).ToString();
    }

    private void HandleSkipClicked()
    {
        flowController?.RequestSkip();
    }

    private void CompleteIntro()
    {
        SetPlayerListVisible(false);
        SetSkipVisible(false);
        SetCountdownVisible(false);

        Action callback = completionCallback;
        completionCallback = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }
}
