using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Mission : MonoBehaviour
{
    [Header("UI texts")]
    [SerializeField] private TMP_Text missionTitle;
    [SerializeField] private TMP_Text missionDescription;
    [SerializeField] private TMP_Text missionReward;
    [SerializeField] private Image shadowGet;
    [SerializeField] private Button getButton;
    [SerializeField] private GameObject blockObj;

    [Header("Settings")]
    [SerializeField] private int missionId = 0;
    [SerializeField] private int missionRewardAmount = 0;
    [SerializeField] private int missionTitleLangId = -1;
    [SerializeField] private int missionDescriptionLangId = -1;
    //[SerializeField] private ModalWindowManager modalWindowManager;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        Translations();
    }

    private void Translations()
    {
        missionTitle.text = LanguageManager.Instance.GetText(missionTitleLangId);
        missionDescription.text = LanguageManager.Instance.GetText(missionDescriptionLangId);
        missionReward.text = missionRewardAmount.ToString() + " " + LanguageManager.Instance.GetText(58);
    }
}
