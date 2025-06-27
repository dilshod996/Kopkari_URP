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
    [Header("Settings")]
    //public CameraTransitionManager cameraMove;
    public CountdownAnimator countdownAnimator;
    [SerializeField] private Button startBtn;
    void Start()
    {
        startBtn.onClick.AddListener(StartGame);
    }

    private void OnEnable()
    {
        Transiliations();
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
        countdownAnimator.gameObject.SetActive(true);
        //cameraMove.OnStartButtonClicked();
        //StartCoroutine(DelayStart());
        gameObject.SetActive(false);
        
    }
    IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(3f);

    }
}
