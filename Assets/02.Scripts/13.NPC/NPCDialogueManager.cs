using UnityEngine;
using TMPro;
using System.Collections;
using System;
using UnityEngine.UI;
using static BaseManager;
using System.Collections.Generic;

public class NPCDialogueManager : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public GameObject dialoguePanel;
    public float delay = 0.04f;

    private string fullText;
    private string currentText;
    private Coroutine typingCoroutine;
    private Coroutine autoCloseCoroutine;

    private const string SelectedNPCKey = "SelectedNPC";
    private const string NPCStartTimeKey = "NPCStartTime";
    private TimeSpan npcDuration = TimeSpan.FromHours(24);

    [Header("NPC Sprites")]
    public Sprite shomurodOtaSprite;
    public Sprite nurmamatOtaSprite;

    [SerializeField] private Image npcImage;

    private List<string> uloqMessages = new List<string>
    {
        "Uloqni mahkam ushla, qo‘lingdan chiqib ketmasin!",
        "Sizning ot kuchingiz yetarlimi? Hali ko‘ramiz!",
        "Uloqni mahkam tort, raqiblar senga qarab yugurmoqda!",
        "Faqat eng mard chavandoz uloqni ko‘tara oladi!",
        "Hamma ko‘zlar sizda! G‘alaba sizga bog‘liq!",
        "Uloqni olib chiqish oson emas! Kuching yetadimi?",
        "Qattiq tur! Uloq hozircha sening qo‘lingda!",
        "G‘alabaga bir qadam qoldi! Bardam bo‘ling!",
        "Bu sening imkoniyating! Uloqni ko‘tar va maydondan olib chiq!",
        "Hamma kuchini ishga sol! Raqiblar juda yaqinlashdi!",
        "Boshqalardan oldin uloqni olib, o‘zingni ko‘rsat!",
        "O‘zingni yo‘qotma, uzoqni o‘yla va harakatni to‘g‘ri tanla!"
    };

    private void Start()
    {
        //dialoguePanel.SetActive(false);
    }

    #region ActiveNPC check va NPC Sprite olish
    public bool IsNPCActive()
    {
        if (!PlayerPrefs.HasKey(NPCStartTimeKey))
            return false;

        DateTime startTime = DateTime.Parse(PlayerPrefs.GetString(NPCStartTimeKey));
        TimeSpan elapsed = DateTime.Now - startTime;
        TimeSpan remaining = npcDuration - elapsed;

        return remaining.TotalSeconds > 0;
    }

    public Sprite GetNPCSprite()
    {
        if (!PlayerPrefs.HasKey(SelectedNPCKey))
            return null;

        string npcName = PlayerPrefs.GetString(SelectedNPCKey);

        switch (npcName)
        {
            case "ShomurodOta":
                return shomurodOtaSprite;
            case "NurmamatOta":
                return nurmamatOtaSprite;
            default:
                return null;
        }
    }
    #endregion

    #region Popup boshqaruv
    public void OpenPopup(string dialogueText, float popupShowTime = 2f)
    {
        // Avval eski popup va coroutine'larni stop qilamiz
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        // Agar popup allaqachon ochiq bo'lsa, toza qilib olamiz
        if (dialoguePanel.activeSelf)
        {
            ClosePopup(); // typing va autoclose coroutinelarini stop qiladi va panelni tozalaydi
        }

        // Endi yangi popup ochamiz
        if (IsNPCActive())
        {
            Sprite npcSprite = GetNPCSprite();
            if (npcSprite != null)
            {
                npcImage.sprite = npcSprite;
                npcImage.gameObject.SetActive(true);
            }
            else
            {
                npcImage.gameObject.SetActive(false);
            }

            fullText = dialogueText;
            currentText = "";
            textMeshPro.text = currentText;

            dialoguePanel.SetActive(true); // endi yangisini ochamiz

            typingCoroutine = StartCoroutine(ShowText(popupShowTime));
        }
        else
        {
            Debug.Log("No Time");
            dialoguePanel.SetActive(false);
        }
    }

    private IEnumerator ShowText(float popupShowTime=2f)
    {
        for (int i = 0; i <= fullText.Length; i++)
        {
            currentText = fullText.Substring(0, i);
            textMeshPro.text = currentText;
            yield return new WaitForSeconds(delay);
        }

        // Text yozish tugagandan keyin 2 sekund kutib avtomatik yopiladi
        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay(popupShowTime));
    }

    private IEnumerator AutoCloseAfterDelay(float popupShowTime=2f)
    {
        yield return new WaitForSeconds(popupShowTime);
        ClosePopup();
    }

    public void ClosePopup()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        dialoguePanel.SetActive(false);
    }
    #endregion


    #region PopupOchish states
    public void OpenNPCPanel(PlayerCondition state)
    {
        switch (state)
        {
            case PlayerCondition.Start:
                OpenPopup("Qani polvon boshladik, Senga ko'rsatilayotgan >>> ortidan borgin!", 5f);
                break;
            case PlayerCondition.GettingTarget:
                OpenPopup(GetRandomUloqMessage());
                break;
            case PlayerCondition.GotTarget:
                OpenPopup("Hayda polvon haydaa! Uloqni oldingiz!");
                break;
            case PlayerCondition.NearTarget:
                OpenPopup("Finishga yaqin keldingiz! Tezroq!");
                break;
            case PlayerCondition.AwayTarget:
                OpenPopup("Uloqdan uzoqlashdingiz! Yordam kerak!");
                break;
            case PlayerCondition.CatchLimit:
                OpenPopup("Uloqni ushlab turish vaqti tugamoqda ! 3 soniya", 3f);
                break;
            case PlayerCondition.TakenTargetOthers:
                OpenPopup("Uloqni " + KopkariManager.Instance.LambOwner + " polvon olib ketdi! Tezroq qayt!", 3f);
                break;
            // va hokazo...
            default:
                break;
        }
    }
    public string GetRandomUloqMessage()
    {
        if (uloqMessages == null || uloqMessages.Count == 0)
            return string.Empty;

        int randomIndex = UnityEngine.Random.Range(0, uloqMessages.Count);
        return uloqMessages[randomIndex];
    }

    #endregion
}
