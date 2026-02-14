using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.ModernUIPack;


public class MapCard : MonoBehaviour
{
    public enum MapType
    {
        Kopkari,
        Racing,
        Archery
    }
    public MapType mapType;
    [SerializeField] private Sprite backgroundSprite;
    public Sprite BackgroundSprite => backgroundSprite;
    [Header("UI Elements")]
 
    public Image shadowImage;
    public TMP_Text mapNameText;
    public Button cardScrollBtn;
    public Button blockBtn;
    public Image mapImage;

    [Header("Unlock Settings")]
    public bool isUnlocked = true;

    private Vector3 selectedScale = Vector3.one * 1.04f;
    private Vector3 normalScale = Vector3.one * 0.94f;

    private float currentAlpha = 0.85f;
    private float targetAlpha = 0.85f;
    private Vector3 targetScale;

    [Header("Popup")]
    [SerializeField] private MapShowPopup popupMapInfo;


    [Header("Language")]
    [SerializeField] private int mapLangCode = -1;
    [SerializeField] private string mapLangName = "MapName";

    [SerializeField] private int mapInfoCode = -1;
    [SerializeField] private int costMap;
    [SerializeField] private int playCost;
    private MapCardScaler manager;

    //[Header("Moving Scene Details")]
    //[SerializeField] private SceneLoadManager.SceneType selectedScene;
    //[SerializeField] private Button selectBtn;



    private void OnEnable()
    {
        LockeMap();
        mapNameText.text = LanguageManager.Instance.GetText(mapLangCode);
        blockBtn.onClick.AddListener(MapDetails);

    }

    private void MapDetails()
    {
        //string cost  = costMap.ToString() /*+ LanguageManager.Instance.GetText(58)*/;
        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
        HomeMainUI.Instance.ShowUI(popupMapInfo);
        popupMapInfo.SetMapData(LanguageManager.Instance.GetText(mapLangCode), mapImage.sprite, costMap, LanguageManager.Instance.GetText(mapInfoCode), mapLangName, mapType);
    }
    public void Initialize(MapCardScaler cardManager)
    {
        manager = cardManager;

        if (cardScrollBtn != null)
            cardScrollBtn.onClick.AddListener(() => manager.ScrollToCard(this));

        //if (chooseButton != null)
        //    chooseButton.onClick.AddListener(OnChooseClicked);
    }

    public void SetAsMain(bool isMain)
    {
        targetScale = isMain ? selectedScale : normalScale;

        if (isUnlocked)
        {
            targetAlpha = isMain ? 0f : 0.85f;

            if (shadowImage != null)
                shadowImage.gameObject.SetActive(!isMain);
            Debug.Log("Card is main?" + gameObject.name);
        }
        else
        {
            targetAlpha = 0.85f;;
            //Debug.Log("Card is locked, setting alpha to 0.85");
        }
    }
    private void LockeMap()
    {
        int mapOpen = PlayerPrefs.GetInt(mapLangName, 0);
        if (mapOpen != 0)
        {
            blockBtn.gameObject.SetActive(false);
            isUnlocked = true;
        }
        else
        {
            blockBtn.gameObject.SetActive(true);
            isUnlocked= false;
        }
        //if (!isUnlocked)
        //{
        //    blockBtn.gameObject.SetActive(true);
        //}
        //else
        //{
        //    blockBtn.gameObject.SetActive(false);
        //}
    }

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 2.5f);
    }

    public void SetMapInfo(string name, string info, string unlockInfo, bool unlocked)
    {
        if (mapNameText != null) mapNameText.text = name;
        //if (mapInfoText != null) mapInfoText.text = info;

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

   
}

