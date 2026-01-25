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
    [SerializeField] private TMP_Text soonText1;
    [SerializeField] private TMP_Text soonText2;


    public int soonTextId = -1;

    [Header("UI Buttons")]
    [SerializeField] private Button kopkariLeagueBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button archeryLeagueBtn;
    [SerializeField] private Button racingLeagueBtn;

    [Header("Other Panels")]
    [SerializeField] private GameObject leaguePanel;
    [SerializeField] private GameObject racingMaps;

    private void OnEnable()
    {
        //UITransilations();
        kopkariLeagueBtn.onClick.AddListener(OpenLeaguePanel);
        racingLeagueBtn.onClick.AddListener(OpenRacingMaps);
        closeBtn.onClick.AddListener(CloseGameObject);
    }

    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(306);
        kopkariLeagueText.text = LanguageManager.Instance.GetText(307);
        archeryLeagueText.text = LanguageManager.Instance.GetText(381);
        racingLeagueText.text = LanguageManager.Instance.GetText(382);
        friendsPlayText.text = LanguageManager.Instance.GetText(383);
        soonText1.text = LanguageManager.Instance.GetText(soonTextId);
        soonText2.text = LanguageManager.Instance.GetText(soonTextId);
    }
    private void OpenLeaguePanel()
    {
        HomeMainUI.Instance.ShowUI(leaguePanel);
        this.gameObject.SetActive(false);
    }
    private void OpenRacingMaps()
    {
        HomeMainUI.Instance.ShowUI(racingMaps);
        this.gameObject.SetActive(false);
    }
    public void CloseGameObject()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OnDisable()
    {
        kopkariLeagueBtn.onClick.RemoveListener(OpenLeaguePanel);
        racingLeagueBtn.onClick.RemoveListener(OpenRacingMaps);
        closeBtn.onClick.RemoveListener(CloseGameObject);
    }
}
