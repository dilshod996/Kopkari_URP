using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button playAgain;
    [SerializeField] private Button backLobby;
    [SerializeField] private Button support;
    [SerializeField] private TMP_Text replayText;
    [SerializeField] private TMP_Text backText;
    [SerializeField] private TMP_Text supportText;

    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameFood foodPage;
    [SerializeField] private ConditionCheck conditionCheck;

    public SceneLoadManager.SceneType sceneType;

    private float overAllTime = 0f;
    private float overAllBoostTime = 0f;
    private float overAllPenaltyTime = 0f;

    // Horse Last stats

    private float lastPower = 0f;
    private float lastCooling = 0f;
    private float lastStamina = 0f;

    private void OnEnable()
    {
        // UIButtonActions.OnSprintHold += GetOverallBoostTime;
        //RacingController.OnOverallBoostTime += GetOverallBoostTime;
        GetOverallPenaltyTime();
        GetBoostTime();
        backLobby.onClick.AddListener(BackHome);
        playAgain.onClick.AddListener(PlayAgainAction);
        support.onClick.AddListener(OpenSuppliesPage);
        UITransilations();
        overAllTime = RacingController.Instance.RaceEndTime;
        HorseStats();
        
    }

    private void OnDisable()
    {
        playAgain.onClick.RemoveAllListeners();
        backLobby.onClick.RemoveAllListeners();
        support.onClick.RemoveAllListeners();
        //UIButtonActions.OnSprintHold -= GetOverallBoostTime;
        //RacingController.OnOverallPenaltyTime -= GetOverallPenaltyTime;
        //RacingController.OnOverallBoostTime -= GetOverallBoostTime;
    }
    private void UITransilations()
    {
        if(LanguageManager.Instance != null)
        {
            gameOverTitle.text = LanguageManager.Instance.GetText(196);
            backText.text = LanguageManager.Instance.GetText(302);
            replayText.text = LanguageManager.Instance.GetText(197);
            supportText.text = LanguageManager.Instance.GetText(198);
            infoText.text = LanguageManager.Instance.GetText(194);
        }
    }
    public void PlayAgainAction()
    {
        if (lastPower < 20 || lastCooling < 10 || lastStamina < 30)
        {
            UIButtonActions.Instance?.ShowUI(conditionCheck);
            this.gameObject.SetActive(false);
            return;  // Racing davom etmaydi
        }
        PlayAgainText();
        SceneLoadManager.Instance.ReloadOrBackScene(sceneType);
    }
    public void PlayAgainText()
    {
        switch(sceneType)
        {
            case SceneLoadManager.SceneType.SecondRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Zarafshan, "This time is your win!");
                break;
            case SceneLoadManager.SceneType.EgyptRacing:
                UIOverlayRoot.I.ShowPanel(UIPanelType.Egypt, "Egypt People waiting you race");
                break;
        }
    }
    private void BackHome()
    {
        UIOverlayRoot.I.ShowPanel(UIPanelType.Home, LanguageManager.Instance.GetText(191));
        SceneLoadManager.Instance.ReloadOrBackScene(SceneLoadManager.SceneType.Home);
    }
    private void OpenSuppliesPage()
    {
        this.gameObject.SetActive(false);
        UIButtonActions.Instance?.ShowUI(foodPage);
    }

    #region Horse Statistics
    private void GetOverallPenaltyTime()
    {
        overAllPenaltyTime = UIButtonActions.Instance?.GetTotalWebSnareTime() ?? 0f;
    }

    private void GetBoostTime()
    {
        overAllBoostTime = UIButtonActions.Instance?.GetTotalHoldTime() ?? 0f;
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

        float newPower = horsePowerMain - (overAllBoostTime * 0.5f + basicTime * 0.2f);
        float newStamina = horseStaminaMain - (overAllTime * 0.3f);
        float newCooling = horseCoolingMain - (overAllPenaltyTime * 0.5f + nonPenaltyTime * 0.05f);

        newPower = Mathf.Max(0, newPower);
        newStamina = Mathf.Max(0, newStamina);//Hard coded now
        newCooling = Mathf.Max(0, newCooling);


        float rPower = Mathf.Round(newPower);          // butun son (masalan: 83)
        float rStamina = Mathf.Round(newStamina);
        float rCooling = Mathf.Round(newCooling);
        lastPower = rPower;
        lastCooling = rCooling;
        lastStamina = rStamina;
        // Progress Bar Updatelar
     
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, rPower);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, rStamina);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, rCooling);

        PlayerPrefs.Save();

        Debug.Log($"Horse Stats Updated ¡æ Power:{rPower}, Stamina:{rStamina}, Cooling:{rCooling}");
    }
    #endregion
}
