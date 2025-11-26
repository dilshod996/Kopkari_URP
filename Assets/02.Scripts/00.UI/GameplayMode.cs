using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMode : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text leagueText;
    [SerializeField] private TMP_Text multiPlayerText;
    [SerializeField] private TMP_Text trainingPlayerText;
    [SerializeField] private TMP_Text friendsPlayText;
    [SerializeField] private TMP_Text nomadGamesText;
    [SerializeField] private TMP_Text soonText1;
    [SerializeField] private TMP_Text soonText2;
    [SerializeField] private TMP_Text soonText3;


    public int soonTextId = -1;

    [Header("UI Buttons")]
    [SerializeField] private Button leagueBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button trainingBtn;
    [SerializeField] private Button racingMapsBtn;

    [Header("Other Panels")]
    [SerializeField] private GameObject leaguePanel;
    [SerializeField] private GameObject racingMaps;

    private void OnEnable()
    {
        //UITransilations();
        leagueBtn.onClick.AddListener(OpenLeaguePanel);
        racingMapsBtn.onClick.AddListener(OpenRacingMaps);
        //closeBtn.onClick.AddListener(CloseGameObject);
    }

    private void UITransilations()
    {
        titleText.text = LanguageManager.Instance.GetText(305);
        leagueText.text = LanguageManager.Instance.GetText(306);
        multiPlayerText.text = LanguageManager.Instance.GetText(243);
        trainingPlayerText.text = LanguageManager.Instance.GetText(307);
        friendsPlayText.text = LanguageManager.Instance.GetText(308);
        nomadGamesText.text = LanguageManager.Instance.GetText(309);
        soonText1.text = LanguageManager.Instance.GetText(soonTextId);
        soonText2.text = LanguageManager.Instance.GetText(soonTextId);
        soonText3.text = LanguageManager.Instance.GetText(soonTextId);
    }
    private void OpenLeaguePanel()
    {
        HomeMainUI.Instance.ShowUI(leaguePanel);
    }
    private void OpenRacingMaps()
    {
        HomeMainUI.Instance.ShowUI(racingMaps);
    }
    public void CloseGameObject()
    {
        HomeMainUI.Instance.HideUI(this);
    }
    private void OnDisable()
    {
        leagueBtn.onClick.RemoveListener(OpenLeaguePanel);
        racingMapsBtn.onClick.RemoveListener(OpenRacingMaps);
        closeBtn.onClick.RemoveListener(CloseGameObject);
    }
}
