using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGamePlayerList : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text imageTitle;
    [SerializeField] private TMP_Text playerNameTitle;
    [SerializeField] private TMP_Text horseNameTitle;
    [SerializeField] private TMP_Text powerTitle;
    [SerializeField] private TMP_Text readyTitle;
    [SerializeField] private TMP_Text startBtnText;

    [Header("Time Info")]
    [SerializeField] private GameObject panelToHide;
    [SerializeField] private TMP_Text timeText;
    private int currentTime = 5;
    [Header("Settings")]
    public CameraTransitionManager cameraMove;
    public CountdownAnimator countdownAnimator;
    [SerializeField] private Button startBtn;
    void Start()
    {
        
        UpdateText();                   // Boshlanishda vaqtni ko¡®rsatish
        StartCoroutine(Countdown());   // Taymerni boshlash
    }

    private void OnEnable()
    {
        Transiliations();
        startBtn.onClick.AddListener(StartGame);
    }
    private void OnDisable()
    {
        startBtn.onClick.RemoveListener(StartGame);
    }

    private void Transiliations()
    {
        title.text = LanguageManager.Instance.GetText(273);
        imageTitle.text = LanguageManager.Instance.GetText(272);
        playerNameTitle.text = LanguageManager.Instance.GetText(100);
        horseNameTitle.text = LanguageManager.Instance.GetText(89);
        powerTitle.text = LanguageManager.Instance.GetText(274);
        readyTitle.text = LanguageManager.Instance.GetText(276);
        startBtnText.text = LanguageManager.Instance.GetText(4);
    }

    private void StartGame()
    {
        cameraMove.OnStartButtonClicked(); 
        //countdownAnimator.gameObject.SetActive(true);
        gameObject.SetActive(false);
        
    }
    IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(3f);

    }

    #region Time Live Panel
    IEnumerator Countdown()
    {
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1f);
            currentTime--;
            UpdateText();
        }

        // Vaqt tugadi
        panelToHide.SetActive(false);
        startBtn.gameObject.SetActive(true);
    }

    void UpdateText()
    {
        timeText.text = $"00:0{currentTime}";
    }
    #endregion
}
