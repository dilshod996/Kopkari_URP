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
    [SerializeField] private Button settingsBtn;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text titlePause;
    [SerializeField] private TMP_Text resumeText;
    [SerializeField] private TMP_Text backLobbyText;
    [SerializeField] private TMP_Text settingsText;
    [SerializeField] private int racingSettingsTextId = 26;
    [SerializeField] private int kopkariHowToPlayTextId = -1;
    [SerializeField] private GameObject detailsBg;
    [SerializeField] private GameObject countBg;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private int resumeCountdownSeconds = 3;
    private bool _paused;
    private Coroutine resumeCountdownRoutine;

    private float overAllTime = 0f;
    private float overAllBoostTime = 0f;
    private float overAllPenaltyTime = 0f;
    private void Awake()
    {
        if (ResumeBtn != null)
            ResumeBtn.onClick.AddListener(ResumeGame);

        if (LobbyBackBtn != null)
            LobbyBackBtn.onClick.AddListener(MoveLobby);
        if(settingsBtn != null)
            settingsBtn.onClick.AddListener(OpenSettingsPanel);
    }

    private void OnDestroy()
    {
        StopResumeCountdown();

        if (ResumeBtn != null)
            ResumeBtn.onClick.RemoveListener(ResumeGame);

        if (LobbyBackBtn != null)
            LobbyBackBtn.onClick.RemoveListener(MoveLobby);
        if (settingsBtn != null)
            settingsBtn.onClick.RemoveListener(OpenSettingsPanel);

    }

    private void OnEnable()
    {
        UpdateTexts();
        SetPauseDetailsVisible(true);
        SetCountdownVisible(false);

        if (ResumeBtn != null)
            ResumeBtn.interactable = true;

        if (!_paused)
            StartCoroutine(PauseNextFrame());  
        //Debug.Log("PauseTime " + RacingController.Instance.ElapsedTime);
    }

    private void OnDisable()
    {
        StopResumeCountdown();
    }

    public void ApplyImmediatePause()
    {
        StopResumeCountdown();
        SetPauseDetailsVisible(true);
        SetCountdownVisible(false);

        if (ResumeBtn != null)
            ResumeBtn.interactable = true;

        Time.timeScale = 0f;
        _paused = true;
    }

    public void ResumeGame()
    {
        if (resumeCountdownRoutine != null)
            return;

        resumeCountdownRoutine = StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        if (ResumeBtn != null)
            ResumeBtn.interactable = false;

        SetPauseDetailsVisible(false);
        SetCountdownVisible(true);

        int startCount = Mathf.Max(1, resumeCountdownSeconds);
        for (int i = startCount; i > 0; i--)
        {
            if (countText != null)
                countText.text = i.ToString();

            yield return new WaitForSecondsRealtime(1f);
        }

        SetCountdownVisible(false);
        _paused = false;

        if(KopkariMainUI.Instance != null)
        {
            KopkariMainUI.Instance.ResumeFromPause(this);
        }
        else
        {
            Time.timeScale = 1f;
            UIButtonActions.Instance?.HideUI(this);
            RacingController.Instance?.ResumeRaceTime();
            UIButtonActions.Instance?.NotifyRaceResumedFromPause();
        }

        resumeCountdownRoutine = null;
    }

    private void StopResumeCountdown()
    {
        if (resumeCountdownRoutine == null)
            return;

        StopCoroutine(resumeCountdownRoutine);
        resumeCountdownRoutine = null;

        if (ResumeBtn != null)
            ResumeBtn.interactable = true;
    }

    private void SetPauseDetailsVisible(bool visible)
    {
        if (detailsBg != null)
            detailsBg.SetActive(visible);
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countBg != null)
            countBg.SetActive(visible);

        if (!visible && countText != null)
            countText.text = string.Empty;
    }

    private void MoveLobby()
    {
        UIOverlayRoot.I.Confirm(429, 430,1,2, BackLobby, null);
    }
    void BackLobby()
    {
        if (KopkariManager.Instance != null)
        {
            KopkariMainUI.Instance?.ReleasePauseForSceneExit();
            ApplyKopkariHorseConditionBeforeLeaving();
        }
        else
        {
            RacingController.Instance?.RecordAbandonedRace();
            Time.timeScale = 1f;
            GetGameFinishedTime();
            GetOverallPenaltyTimeAndBoost();
            HorseStats();
        }

        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }

    private void ApplyKopkariHorseConditionBeforeLeaving()
    {
        KopkariManager.Instance.FinishMatch();

        KopkariResultsManager results = KopkariResultsManager.Instance;
        if (results == null)
        {
            Debug.LogWarning("[UIPauseGame] Kopkari results manager is missing; horse condition was not applied.");
            return;
        }

        results.GetOrApplyHorseCondition();
    }

    private void UpdateTexts()
    {
        if(LanguageManager.Instance != null)
        {
            titlePause.text = LanguageManager.Instance.GetText(301);
            resumeText.text = LanguageManager.Instance.GetText(253);
            backLobbyText.text = LanguageManager.Instance.GetText(302);
            if (settingsText != null)
            {
                int textId = KopkariMainUI.Instance != null
                    ? kopkariHowToPlayTextId
                    : racingSettingsTextId;
                if (textId >= 0)
                    settingsText.text = LanguageManager.Instance.GetText(textId);
            }
        }
    }
    private IEnumerator PauseNextFrame()
    {
        yield return new WaitForSecondsRealtime(0.45f);
        Time.timeScale = 0f;
        _paused = true;
    }
    private void OpenSettingsPanel()
    {
        if (KopkariMainUI.Instance != null)
        {
            KopkariMainUI.Instance.ShowHowToPlayPage();
            return;
        }

        if (UIButtonActions.Instance != null)
            UIButtonActions.Instance.OpenInGameSettingsPanel();
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
        HorseConditionStats current = HorseConditionStatsService.GetCurrentOrInitialize(
            HorseConditionStatsService.GetCachedMaxOrDefault());
        float horsePowerMain = current.Power;
        float horseCoolingMain = current.Cooling;
        float horseStaminaMain = current.Stamina;
        Debug.Log($"[Game Over] Overall Time {overAllTime} penalytTime {overAllPenaltyTime} over all boost time {overAllBoostTime}");
        // --- Calc ---
        float basicTime = overAllTime - overAllBoostTime;       // oddiy yugurish vaqti
        float nonPenaltyTime = overAllTime - overAllPenaltyTime;     // Non-penalty time.

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

        HorseConditionStatsService.SaveCurrent(new HorseConditionStats(rPower, rCooling, rStamina));

        Debug.Log($"Horse Stats Updated -> Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }
    #endregion
}
