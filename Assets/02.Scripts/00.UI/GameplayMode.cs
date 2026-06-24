using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMode : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text kopkariLeagueText;
    [SerializeField] private TMP_Text racingLeagueText;
    [SerializeField] private TMP_Text archeryLeagueText;
    [SerializeField] private TMP_Text friendsPlayText;
    [SerializeField] private TMP_Text kopkariBtnText, racingBtnText, archeryBtnText, localTeamBtnText;

    [Header("UI Buttons")]
    [SerializeField] private Button kopkariLeagueBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button archeryLeagueBtn;
    [SerializeField] private Button racingLeagueBtn;




    private void OnEnable()
    {
        UITransilations();
        kopkariLeagueBtn.onClick.AddListener(OpenLeaguePanel);
        racingLeagueBtn.onClick.AddListener(OpenRacingMaps);
        closeBtn.onClick.AddListener(CloseGameObject);
    }

    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(474);
        kopkariLeagueText.text = LanguageManager.Instance.GetText(306);
        archeryLeagueText.text = LanguageManager.Instance.GetText(381);
        racingLeagueText.text = LanguageManager.Instance.GetText(382);
        friendsPlayText.text = LanguageManager.Instance.GetText(383);
        kopkariBtnText.text = LanguageManager.Instance.GetText(4);
        racingBtnText.text = LanguageManager.Instance.GetText(4);
        archeryBtnText.text = LanguageManager.Instance.GetText(125);
        localTeamBtnText.text = LanguageManager.Instance.GetText(125);
    }
    private void OpenLeaguePanel()
    {
        HomeMainUI.Instance.OpenKopkariMaps();
        this.gameObject.SetActive(false);
    }
    private void OpenRacingMaps()
    {
        HomeMainUI.Instance.OpenRacingMaps();
        this.gameObject.SetActive(false);
    }
    public void CloseGameObject()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OnDisable()
    {
        kopkariLeagueBtn.onClick.RemoveAllListeners();
        racingLeagueBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.RemoveAllListeners();
    }
}
