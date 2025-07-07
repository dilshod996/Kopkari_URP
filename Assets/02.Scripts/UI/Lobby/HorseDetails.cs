using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HorseDetails : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text selectBtnText;
    [SerializeField] private int descriptionId=-1;
    [SerializeField] private int descriptionErrorId=-1;
    [SerializeField] private int notEnoughFoodId=-1;
    [Header("UI Settings")]
    [SerializeField] private Button selectBtn;
    //[SerializeField] private ToggleGroup foodToggleGroup;
    //[SerializeField] private ToggleGroup waterToggleGroup;

    [Header("Toggles")]
    [SerializeField] private List<Toggle> foodToggles;
    [SerializeField] private List<Toggle> waterToggles;
    private const string FoodPrefsKey = "foodToggle";
    private const string WaterPrefsKey = "waterToggle";


    
    private bool isFoodSelected = false;
    private bool isWaterSelected = false;
    void Start()
    {
        selectBtn.onClick.AddListener(SelectedOnClick);
    }

    private void OnEnable()
    {
        TextTransilations();
        SelectedFoodEnable();
        SelectedWaterEnable();
    }

    private void TextTransilations()
    {
        title.text = LanguageManager.Instance.GetText(105);
        description.color = new Color32(16, 7, 2, 255);
        description.text = LanguageManager.Instance.GetText(descriptionId);
        selectBtnText.text = LanguageManager.Instance.GetText(95);
    }


    #region Food Selection
    private void SelectedFoodEnable()
    {
        string savedToggleName = PlayerPrefs.GetString(FoodPrefsKey, "");
        Debug.Log("Saved Food Toggle: " + savedToggleName);

        if (!string.IsNullOrEmpty(savedToggleName))
        {
            foreach (var toggle in foodToggles)
            {
                if (toggle.name == savedToggleName)
                {
                    toggle.isOn = true;
                    break;
                }
            }
        }
    }
    public void FoodSelected()
    {
        Toggle selectedToggle = foodToggles.FirstOrDefault(t => t.isOn);

        if (selectedToggle != null)
        {
            Debug.Log("Tanlangan Toggle: " + selectedToggle.name);
            isFoodSelected = true;
            PlayerPrefs.SetString(FoodPrefsKey, selectedToggle.name);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("Hech qanday toggle tanlanmagan.");
            isFoodSelected = false;
        }
    }
    public void OnFoodToggleChanged(Toggle changedToggle)
    {
        if (changedToggle.isOn)
        {
            foreach (var toggle in foodToggles)
            {
                if (toggle != changedToggle)
                {
                    toggle.isOn = false;
                }
            }
        }
    }
    #endregion

    #region Water Selection
    private void SelectedWaterEnable()
    {
        string savedToggleName = PlayerPrefs.GetString(WaterPrefsKey, "");
        Debug.Log("Saved Water Toggle: " + savedToggleName);

        if (!string.IsNullOrEmpty(savedToggleName))
        {
            foreach (var toggle in waterToggles)
            {
                if (toggle.name == savedToggleName)
                {
                    toggle.isOn = true;
                    break;
                }
            }
        }
    }
    public void WaterSelected()
    {
        Toggle selectedToggle = waterToggles.FirstOrDefault(t => t.isOn);

        if (selectedToggle != null)
        {
            Debug.Log("Tanlangan Toggle: " + selectedToggle.name);
            PlayerPrefs.SetString(WaterPrefsKey, selectedToggle.name);
            isWaterSelected = true;
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("Hech qanday toggle tanlanmagan.");

            isWaterSelected = false;
        }
    }
    public void OnWaterToggleChanged(Toggle changedToggle)
    {
        if (changedToggle.isOn)
        {
            foreach (var toggle in waterToggles)
            {
                if (toggle != changedToggle)
                {
                    toggle.isOn = false;
                }
            }
        }
    }
    #endregion


    #region Food Error or Success Message
    public void SelectedOnClick()
    {
        FoodSelected();
        WaterSelected();
        if (isFoodSelected && isWaterSelected)
        {
            //Debug.Log("Food and Water selected successfully.");
            gameObject.SetActive(false);
            if (!PlayerPrefs.HasKey("horseData"))
            {
                PlayerPrefs.SetInt("horseData", 1);
            }
        }
        else
        {
            description.color = new Color32(125, 6, 1, 255);
            description.text = LanguageManager.Instance.GetText(descriptionErrorId);

        }
        //gameObject.SetActive(false);
    }
    public void FinishedFoodMessage()
    {
        description.color = new Color32(125, 6, 1, 255);
        description.text = LanguageManager.Instance.GetText(notEnoughFoodId);
    }
    #endregion
}
