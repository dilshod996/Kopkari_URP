using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseGame : MonoBehaviour
{
    [SerializeField] private Button ResumeBtn;
    [SerializeField] private Button LobbyBackBtn;
    [SerializeField] private Button howToPlay;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text titlePause;
    [SerializeField] private TMP_Text resumeText;
    [SerializeField] private TMP_Text backLobbyText;
    [SerializeField] private TMP_Text howToPlayText;
    private bool _paused;

    [Header("Pages")]
    [SerializeField] private HowToPlay howToPlayPage;

    private float overAllTime = 0f;
    private float overAllBoostTime = 0f;
    private float overAllPenaltyTime = 0f;
    private void Awake()
    {
        ResumeBtn.onClick.AddListener(ResumeGame);
        LobbyBackBtn.onClick.AddListener(MoveLobby);
        howToPlay.onClick.AddListener(EnableHowToPlay);
    }

    private void OnEnable()
    {
        UpdateTexts();
        if (!_paused)
            StartCoroutine(PauseNextFrame());  
        //Debug.Log("PauseTime " + RacingController.Instance.ElapsedTime);
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        _paused = false;
        if(KopkariMainUI.Instance != null)
        {
            KopkariMainUI.Instance.HideUI(this);
        }
        else
        {
            UIButtonActions.Instance.HideUI(this);
            RacingController.Instance.ResumeRaceTime();
        }
        
    }

    private void MoveLobby()
    {
        UIOverlayRoot.I.Confirm(429, 430,1,2, BackLobby, null);
    }
    void BackLobby()
    {
        Time.timeScale = 1f;
        HorseStats();
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }

    private void UpdateTexts()
    {
        if(LanguageManager.Instance != null)
        {
            titlePause.text = LanguageManager.Instance.GetText(301);
            resumeText.text = LanguageManager.Instance.GetText(253);
            backLobbyText.text = LanguageManager.Instance.GetText(302);
            howToPlayText.text = LanguageManager.Instance.GetText(195);
        }
    }
    private IEnumerator PauseNextFrame()
    {
        yield return new WaitForSecondsRealtime(0.45f);
        Time.timeScale = 0f;
        _paused = true;
    }

    private void EnableHowToPlay()
    {
        if (howToPlayPage != null)
        {
            howToPlayPage.gameObject.SetActive(true);
        }
    }

    #region If Destroy Set Data
    private void GetGameFinishedTime()
    {
        if (RacingController.Instance != null)
            overAllTime = RacingController.Instance.ElapsedTime;
    }
    private void GetOverallPenaltyTimeAndBoost()
    {
        if (UIButtonActions.Instance != null)
        {
            overAllPenaltyTime = UIButtonActions.Instance.GetTotalWebSnareTime();
            overAllBoostTime = UIButtonActions.Instance.GetTotalHoldTime();
        }
    }
    private void HorseStats()
    {
        // --- Load ---
        float horsePowerMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Power);
        float horseCoolingMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling);
        float horseStaminaMain = PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina);
        Debug.Log($"[Game Over] Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime}");
        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - overAllPenaltyTime;     // penalty bo¡®lmagan vaqt

        float newPower = horsePowerMain - (overAllBoostTime * 0.4f + basicTime * 0.3f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.1f);

        newPower = Mathf.Max(0, newPower);
        newStamina = Mathf.Max(0, newStamina);//Hard coded now
        newCooling = Mathf.Max(0, newCooling);


        float rPower = Mathf.Round(newPower);          // butun son (masalan: 83)
        float rStamina = Mathf.Round(newStamina);
        float rCooling = Mathf.Round(newCooling);
        // Progress Bar Updatelar

        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, rPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, rStamina);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, rCooling);

        PlayerPrefs.Save();

        Debug.Log($"Horse Stats Updated ¡æ Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }
    #endregion
}
