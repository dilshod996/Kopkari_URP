using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCStarter : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject npcPanel;
    [SerializeField] private GameObject bgMain;
    [SerializeField] private TMP_Text hourNPC;
    [SerializeField] private Image lobbyNpcImage;

    [SerializeField] private PlayerPrefsData playerPrefsData;

    [Header("NPC Details")]
    [SerializeField] private Toggle npcFirst;
    [SerializeField] private Toggle npcSecond;

    [SerializeField] private Sprite npcFirstSprite;
    [SerializeField] private Sprite npcSecondSprite;

    private const string SelectedNPCKey = "SelectedNPC";
    private const string NPCStartTimeKey = "NPCStartTime";
    private TimeSpan npcDuration = TimeSpan.FromHours(24); // 24 soat


    [Header("UI Texts")]
    [SerializeField] private TMP_Text titlePage;
    [SerializeField] private TMP_Text descriptionPage;
    [SerializeField] private TMP_Text npcFirstName;
    [SerializeField] private TMP_Text npcSecondName;
    [SerializeField] private TMP_Text closeBtnText;
    

    private void Start()
    {
        //startButton.onClick.AddListener(OpenNPCPanel);
        closeButton.onClick.AddListener(CloseNPCPanel);

        npcFirst.onValueChanged.AddListener((isOn) => { if (isOn) SelectNPC("ShomurodOta", npcFirstSprite); });
        npcSecond.onValueChanged.AddListener((isOn) => { if (isOn) SelectNPC("NurmamatOta", npcSecondSprite); });

        // Avval tanlangan NPC¡¯ni yuklaymiz
        if (PlayerPrefs.HasKey(SelectedNPCKey))
        {
            string npcName = PlayerPrefs.GetString(SelectedNPCKey);
            Sprite npcSprite = npcName == "ShomurodOta" ? npcFirstSprite : npcSecondSprite;
            lobbyNpcImage.sprite = npcSprite;
        }
        else
        {
            bgMain.SetActive(false);
        }

        InvokeRepeating(nameof(UpdateTimeRemaining), 0f, 1f);
    }
    private void OnEnable()
    {
        OpenNPCPanel();
        titlePage.text = LanguageManager.Instance.GetText(91); // "NPC Starter"
        descriptionPage.text = LanguageManager.Instance.GetText(92); // "NPC Starter Description"
        npcFirstName.text = LanguageManager.Instance.GetText(93); // "Shomurod Ota"
        npcSecondName.text = LanguageManager.Instance.GetText(94); // "Nurmamat Ota"
        closeBtnText.text = LanguageManager.Instance.GetText(95); // "Close NPC Panel" "Choose"

    }

    private void OpenNPCPanel()
    {
        //npcPanel.SetActive(true);

        if (PlayerPrefs.HasKey(SelectedNPCKey))
        {
            string npcName = PlayerPrefs.GetString(SelectedNPCKey);

            if (npcName == "ShomurodOta")
            {
                npcFirst.isOn = true;
                npcSecond.isOn = false;
            }
            else if (npcName == "NurmamatOta")
            {
                npcFirst.isOn = false;
                npcSecond.isOn = true;
            }
            closeButton.gameObject.SetActive(true);
        }
        else
        {
            npcFirst.isOn = false;
            npcSecond.isOn = false;
            closeButton.gameObject.SetActive(false);
        }
    }

    private void CloseNPCPanel()
    {
        npcPanel.SetActive(false);
        if (!bgMain.activeSelf)
        {
            bgMain.SetActive(true);
        }
        if (!PlayerPrefs.HasKey("username"))
        {
            playerPrefsData.gameObject.SetActive(true);
            playerPrefsData.UserDataCheck();
        }
    }
    public void EnableBg()
    {
        if (!bgMain.activeSelf)
            bgMain.SetActive(true);
    }

    private void SelectNPC(string npcName, Sprite npcSprite)
    {
        // Faqat bir marta NPC tanlanganda vaqt yoziladi
        PlayerPrefs.SetString(SelectedNPCKey, npcName);
        PlayerPrefs.SetString(NPCStartTimeKey, DateTime.Now.ToString("o")); // Local vaqt
        PlayerPrefs.Save();

        lobbyNpcImage.sprite = npcSprite;
        if(!closeButton.gameObject.activeSelf)
            closeButton.gameObject.SetActive(true);
        Debug.Log($"NPC {npcName} tanlandi va 24 soat boshlandi (local time).");
    }

    private void UpdateTimeRemaining()
    {
        if (!PlayerPrefs.HasKey(NPCStartTimeKey))
        {
            hourNPC.text = "Vaqt yo'q";
            return;
        }

        DateTime startTime = DateTime.Parse(PlayerPrefs.GetString(NPCStartTimeKey));
        TimeSpan elapsed = DateTime.Now - startTime; // Local vaqt farqi
        TimeSpan remaining = npcDuration - elapsed;

        if (remaining.TotalSeconds <= 0)
        {
            hourNPC.text = "NPC muddati tugadi";
        }
        else
        {
            hourNPC.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }

    private void UpdateUI()
    {

    }
}
