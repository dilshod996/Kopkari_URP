using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarmUpTutorial : MonoBehaviour
{
    [Header("UI Texts")]
    public GameObject boboyPanel;
    [SerializeField] private TMP_Text detailsText;
    [Header("Buttons")]
    [SerializeField] private Button uloqtimeBtn;
    [SerializeField] private Button backCameraBtn;
    [SerializeField] private Button mainTimeBtn;
    [SerializeField] private Button sprintSliderBtn;
    [SerializeField] private Button joystickBtn;
    [SerializeField] private Button walkZoneBtn;
    [SerializeField] private Button sprintBtn;
    [SerializeField] private Button jumpBtn;
    [SerializeField] private Button defendBtn;
    [SerializeField] private Button getUloqBtn;
    [SerializeField] private Button mainCameraBtn;

    [Header("Settings")]
    [SerializeField] private int mainDeailtsId;
    [SerializeField] private int uloqtimeDetailsId;
    [SerializeField] private int backCameraDetailsId;
    [SerializeField] private int mainTimeDetailsId;
    [SerializeField] private int sprintSliderDetailsId;
    [SerializeField] private int joystickDetailsId;
    [SerializeField] private int walkZoneDetailsId;
    [SerializeField] private int sprintDetailsId;
    [SerializeField] private int jumpDetailsId;
    [SerializeField] private int defendDetailsId;
    [SerializeField] private int getUloqDetailsId;
    [SerializeField] private int mainCameraDetailsId;
    [SerializeField] private int finalID;
    private int clickedCount = 0;
    private int totalButtonCount;


    private Coroutine typingCoroutine;

    public UIGamePlayerList PlayerList;

    [Header("MapTutorial")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private int arrowId;
    [SerializeField] private int uloqCatchTimerId;
    [SerializeField] private int mainTimerAddId;
    [SerializeField] private int walkZoneAddId;
    [SerializeField] private int defendAddId;
    [SerializeField] private int staminaAddId;

    [SerializeField] private TMP_Text arrowText;
    [SerializeField] private TMP_Text uloqCatchTimerText;
    [SerializeField] private TMP_Text mainTimerAddText;
    [SerializeField] private TMP_Text walkZoneAddText;
    [SerializeField] private TMP_Text defendAddText;
    [SerializeField] private TMP_Text staminaAddText;

    [SerializeField] private Button closeButton;
    private void OnEnable()
    {
        clickedCount = 0;
        totalButtonCount = 11; // 11 ta button bor
        string textDetails = LanguageManager.Instance.GetText(mainDeailtsId);
        ShowTextWithTyping(textDetails, 2f); // 2 sekundda to¡®liq yoziladi

        uloqtimeBtn.onClick.AddListener(() => OnClickDetails(uloqtimeDetailsId, uloqtimeBtn));
        backCameraBtn.onClick.AddListener(() => OnClickDetails(backCameraDetailsId, backCameraBtn));
        mainTimeBtn.onClick.AddListener(() => OnClickDetails(mainTimeDetailsId, mainTimeBtn));
        sprintSliderBtn.onClick.AddListener(() => OnClickDetails(sprintSliderDetailsId, sprintSliderBtn));
        joystickBtn.onClick.AddListener(() => OnClickDetails(joystickDetailsId, joystickBtn));
        walkZoneBtn.onClick.AddListener(() => OnClickDetails(walkZoneDetailsId, walkZoneBtn));
        sprintBtn.onClick.AddListener(() => OnClickDetails(sprintDetailsId, sprintBtn));
        jumpBtn.onClick.AddListener(() => OnClickDetails(jumpDetailsId, jumpBtn));
        defendBtn.onClick.AddListener(() => OnClickDetails(defendDetailsId, defendBtn));
        getUloqBtn.onClick.AddListener(() => OnClickDetails(getUloqDetailsId, getUloqBtn));
        mainCameraBtn.onClick.AddListener(() => OnClickDetails(mainCameraDetailsId, mainCameraBtn));
        //closeButton.onClick.AddListener(CloseTutorial);
    }

    private void OnDisable()
    {
        uloqtimeBtn.onClick.RemoveAllListeners();
        backCameraBtn.onClick.RemoveAllListeners();
        mainTimeBtn.onClick.RemoveAllListeners();
        sprintSliderBtn.onClick.RemoveAllListeners();
        joystickBtn.onClick.RemoveAllListeners();
        walkZoneBtn.onClick.RemoveAllListeners();
        sprintBtn.onClick.RemoveAllListeners();
        jumpBtn.onClick.RemoveAllListeners();
        defendBtn.onClick.RemoveAllListeners();
        getUloqBtn.onClick.RemoveAllListeners();
        mainCameraBtn.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
    }

    public void OnClickDetails(int languageId, Button clickedButton)
    {
        clickedButton.gameObject.SetActive(false);
        ShowTextWithTyping(LanguageManager.Instance.GetText(languageId), 2f);

        clickedCount++;

        if (clickedCount >= totalButtonCount)
        {
            StartCoroutine(ShowFinalAndClose());
        }
    }
    private IEnumerator ShowFinalAndClose()
    {
        yield return new WaitForSeconds(4f);
        string textFinal= LanguageManager.Instance.GetText(finalID);
        ShowTextWithTyping(textFinal, 2f);
        yield return new WaitForSeconds(4f);
        SetMapTutorialTexts();

    }


    private void ShowTextWithTyping(string fullText, float duration)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(fullText, duration));
    }

    private IEnumerator TypeText(string fullText, float duration)
    {
        detailsText.text = "";
        int totalChars = fullText.Length;
        float delay = duration / totalChars;

        for (int i = 0; i < totalChars; i++)
        {
            detailsText.text += fullText[i];
            yield return new WaitForSeconds(delay);
        }

        typingCoroutine = null;
    }

    public void SetMapTutorialTexts()
    {
        if (mapPanel != null)
            mapPanel.SetActive(true);
        boboyPanel.SetActive(false);
        arrowText.text = LanguageManager.Instance.GetText(arrowId);
        uloqCatchTimerText.text = LanguageManager.Instance.GetText(uloqCatchTimerId);
        mainTimerAddText.text = LanguageManager.Instance.GetText(mainTimerAddId);
        walkZoneAddText.text = LanguageManager.Instance.GetText(walkZoneAddId);
        defendAddText.text = LanguageManager.Instance.GetText(defendAddId);
        staminaAddText.text = LanguageManager.Instance.GetText(staminaAddId);
        closeButton.gameObject.gameObject.SetActive(true);
        closeButton.onClick.AddListener(CloseTutorial);
    }
    public void CloseTutorial()
    {
        if (PlayerPrefs.GetInt(Constants.Tutorial.GamePlay, 0) == 0)
        {
            PlayerPrefs.SetInt(Constants.Tutorial.GamePlay, 1);
            PlayerPrefs.Save();
            PlayerList.gameObject.SetActive(true);
        }
        gameObject.SetActive(false);
    }
}
