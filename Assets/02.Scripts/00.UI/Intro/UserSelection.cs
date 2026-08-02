using Kopkari;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class UserSelection : MonoBehaviour
{
    [SerializeField] private GameObject horseSelectionPanel;
    [SerializeField] private GameObject riderSelectionPanel;
    [SerializeField] private GameObject nameInputPage;

    [SerializeField] private ToggleGroup riderToggleGroup;
    [SerializeField] private ToggleGroup horseToggleGroup;

    [SerializeField] private ButtonManagerBasicIcon horseSelectPageBtn;
    [SerializeField] private ButtonManagerBasicIcon backLogoPageBtn;
    [SerializeField] private ButtonManagerBasicIcon nameInputPageBtn;
    [SerializeField] private ButtonManagerBasicIcon backToRiderSelectBtn;
    [SerializeField] private ButtonManagerBasicWithIcon lobbyMoveBtn;

    public string RiderName=string.Empty;
    public string HorseName = string.Empty;
    //[SerializeField] private AnimatorController RiderPageAC;
    //[SerializeField] private AnimatorController HorsePageAC;
    //private bool isRiderAnim=false;
    //private bool isHorseAnim=false;

    [SerializeField] private TMP_InputField horseInput;
    [SerializeField] private TMP_InputField riderInput;

    void Start()
    {
        horseSelectPageBtn.clickEvent.AddListener(() => BackToRiderPage(true));
        backToRiderSelectBtn.clickEvent.AddListener(()=>BackToRiderPage(false));
        lobbyMoveBtn.clickEvent.AddListener(GoToLobby);
        //backLogoPageBtn.clickEvent.AddListener(BackToLogAction);
        nameInputPageBtn.clickEvent.AddListener(GoToNamePage);

        // Only the rider has a personal name. Horse names come from the selected body catalog entry.
        riderInput.onValueChanged.AddListener(delegate { CheckInputFields(); });
    }
    private void OnEnable()
    {
    }
    //private void BackToLogAction()
    //{
    //    Screen.orientation = ScreenOrientation.Portrait;
    //}
    private void BackToRiderPage(bool state)
    {
        if(state==true)
            RiderName = RiderHorseSelectedToggleName(riderToggleGroup);
        horseSelectionPanel.SetActive(state);
        riderSelectionPanel.SetActive(!state);
    }
    private void GoToNamePage()
    {
        HorseName = RiderHorseSelectedToggleName(horseToggleGroup);
        if (HorseName != string.Empty)
        {
            nameInputPage.SetActive(true);
            horseSelectionPanel.SetActive(false);
        }
    }
    public Toggle GetSelectedToggle(ToggleGroup toggleGroup)
    {
        return toggleGroup.ActiveToggles().FirstOrDefault();
    }

    public string RiderHorseSelectedToggleName(ToggleGroup toggleGroup)
    {
        Toggle selected = GetSelectedToggle(toggleGroup);
        if (selected != null)
        {
            Debug.Log("Tanlangan rider: " + selected.name);
            //RiderName = selected.name;
        }
        else
        {          
            Debug.Log("Hech qanday toggle tanlanmagan.");
            
        }
        return selected.name;
    }
    void CheckInputFields()
    {
        bool isInput1Valid = !string.IsNullOrWhiteSpace(riderInput.text);

        lobbyMoveBtn.buttonVar.interactable = isInput1Valid;
    }
    private void GoToLobby()
    {
        if (!PlayerPrefs.HasKey("username"))
        {
            PlayerPrefs.SetString("username", riderInput.text);
            PlayerPrefs.Save();
            Debug.Log("Foydalanuvchi ro'yxatdan o'tdi!");
        }
        else
        {
            Debug.Log("Foydalanuvchi allaqachon mavjud!");
        }
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }
}
