using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class KopkariRoundChangePopup : MonoBehaviour
{
    [Header("Round Change")]
    [SerializeField] private GameObject roundChangePanel;
    [SerializeField] private Button nextRoundButton;
    [SerializeField] private Button finishHereButton;

    [Header("Warmup Countdown")]
    [SerializeField] private GameObject warmupBackground;
    [SerializeField] private TMP_Text warmupText;

    private void Awake()
    {
        HideAll();
    }

    private void OnEnable()
    {
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(HandleNextRoundClicked);
        if (finishHereButton != null)
            finishHereButton.onClick.AddListener(HandleFinishHereClicked);
    }

    private void OnDisable()
    {
        if (nextRoundButton != null)
            nextRoundButton.onClick.RemoveListener(HandleNextRoundClicked);
        if (finishHereButton != null)
            finishHereButton.onClick.RemoveListener(HandleFinishHereClicked);
    }

    public void ShowRoundChange(bool canStartNextRound)
    {
        HideWarmupCountdown();

        if (roundChangePanel != null)
            roundChangePanel.SetActive(true);

        SetButtonState(nextRoundButton, canStartNextRound);
        SetButtonState(finishHereButton, true);
    }

    public void HideRoundChange()
    {
        if (roundChangePanel != null)
            roundChangePanel.SetActive(false);

        SetButtonState(nextRoundButton, false);
        SetButtonState(finishHereButton, false);
    }

    public void ShowWarmupCountdown(int seconds)
    {
        HideRoundChange();

        if (warmupBackground != null)
            warmupBackground.SetActive(true);
        if (warmupText != null)
            warmupText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideWarmupCountdown()
    {
        if (warmupBackground != null)
            warmupBackground.SetActive(false);
        if (warmupText != null)
            warmupText.text = string.Empty;
    }

    public void HideAll()
    {
        HideRoundChange();
        HideWarmupCountdown();
    }

    private void HandleNextRoundClicked()
    {
        HideRoundChange();
        KopkariManager.Instance?.BeginNextRoundWarmup();
    }

    private void HandleFinishHereClicked()
    {
        HideAll();
        KopkariMainUI.Instance?.ShowResult();
    }

    private static void SetButtonState(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = visible;
    }
}
