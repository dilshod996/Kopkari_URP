using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;


public class MapCard : MonoBehaviour
{
    [Header("UI Elements")]
 
    public Image shadowImage;
    public TMP_Text mapNameText;
    public TMP_Text mapInfoText;
    public Button chooseButton;
    public Button cardClickButton;
    public Button infoButton;
    public Image lockImage;

    [Header("Unlock Settings")]
    public bool isUnlocked = true;

    private Vector3 selectedScale = Vector3.one * 1.04f;
    private Vector3 normalScale = Vector3.one * 0.94f;

    private float currentAlpha = 0.85f;
    private float targetAlpha = 0.85f;
    private Vector3 targetScale;
    [SerializeField] private ModalWindowManager modalWindowManager;
    [SerializeField] private NotificationManager notificationManager;


    [Header("Language")]
    [SerializeField] private int mapLangCode = -1;
    [SerializeField] private string mapLangName = "MapName";

    [SerializeField] private int mapInfoCode = -1;
    [SerializeField] private int costMap;
    private MapCardScaler manager;

    



    private void OnEnable()
    {
        LockeMap();
        mapNameText.text = LanguageManager.Instance.GetText(mapLangCode);
        infoButton.onClick.AddListener(MapDetails);
        modalWindowManager.confirmButton.onClick.AddListener(BuyButtonOption);
        chooseButton.GetComponentInChildren<TMP_Text>().text = LanguageManager.Instance.GetText(68);
        if(infoButton!=null&&infoButton.gameObject.activeSelf)
            infoButton.GetComponentInChildren<TMP_Text>().text = LanguageManager.Instance.GetText(71);
        
    }

    private void MapDetails()
    {
        string cost  = costMap.ToString() + LanguageManager.Instance.GetText(58);
        modalWindowManager.UpdateUICustomWithButtons(mapNameText.text,
            LanguageManager.Instance.GetText(mapInfoCode), cost, LanguageManager.Instance.GetText(2));
    }
    public void Initialize(MapCardScaler cardManager)
    {
        manager = cardManager;

        if (cardClickButton != null)
            cardClickButton.onClick.AddListener(() => manager.ScrollToCard(this));

        if (chooseButton != null)
            chooseButton.onClick.AddListener(OnChooseClicked);

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoClicked);
    }

    public void SetAsMain(bool isMain)
    {
        targetScale = isMain ? selectedScale : normalScale;
        if (isUnlocked)
        {
            targetAlpha = isMain ? 0f : 0.85f;

            if (shadowImage != null)
                shadowImage.gameObject.SetActive(!isMain);
            //Debug.Log("Card is main?" + gameObject.name);
        }
        else
        {
            targetAlpha = 0.85f;
            if(infoButton != null)
                infoButton.interactable = isMain;
            //Debug.Log("Card is locked, setting alpha to 0.85");
        }
    }
    private void LockeMap()
    {
        if (!isUnlocked)
        {
            lockImage.gameObject.SetActive(true);
            chooseButton.gameObject.SetActive(false);
            infoButton.gameObject.SetActive(true);
        }
        else
        {
            lockImage.gameObject.SetActive(false);
            chooseButton.gameObject.SetActive(true);
            infoButton.gameObject.SetActive(false);
        }
    }

    void BuyButtonOption()
    {
        notificationManager.CustomeUpdate(LanguageManager.Instance.GetText(70), LanguageManager.Instance.GetText(69));
    }
    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 2.5f);

        // Smooth shadow fade
        //if (shadowImage != null && isUnlocked)
        //{
        //    currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 2.5f);
        //    Color color = shadowImage.color;
        //    color.a = currentAlpha;
        //    shadowImage.color = color;

        //    if (cardClickButton != null)
        //        cardClickButton.interactable = (currentAlpha > 0.05f);
        //}
    }

    public void SetMapInfo(string name, string info, string unlockInfo, bool unlocked)
    {
        if (mapNameText != null) mapNameText.text = name;
        if (mapInfoText != null) mapInfoText.text = info;

        isUnlocked = unlocked;

       // UpdateChooseButtonText();
    }

    private void OnChooseClicked()
    {
        if (isUnlocked)
        {
            Debug.Log($"Map chosen: {mapNameText.text}");
            // Bu yerda tanlangan kartani ishlatish mumkin
        }
        else
        {
            // Agar locked bo‘lsa, popup orqali ko‘rsat
            //InfoPopupManager.Instance.ShowPopup(mapNameText.text, unlockCondition, false, this);
        }
    }

    private void OnInfoClicked()
    {
        //InfoPopupManager.Instance.ShowPopup(mapNameText.text, unlockCondition, isUnlocked, this);
    }

    public void UnlockCard()
    {
        isUnlocked = true;
        //UpdateChooseButtonText();
    }

    private void UpdateChooseButtonText()
    {
        if (chooseButton != null)
        {
            TMP_Text btnText = chooseButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = isUnlocked ? "Choose" : "Unlock";
            }
        }
    }
}

