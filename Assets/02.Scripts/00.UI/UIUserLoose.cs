using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUserLoose : MonoBehaviour
{

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text ContinueBtnText;
    [SerializeField] private Button continueBtn;
    [SerializeField] private TMP_Text backBtnText;
    [SerializeField] private Button backBtn;


    [Header("Prize Display")]
    [SerializeField] private GameObject prizePrefab;
    [SerializeField] private Transform prizeParentLayout;


    [SerializeField] private TypewriterEffect npcText;

    [Header("UI FoodButtons")]
    [SerializeField] private Button bugdoyEat;
    [SerializeField] private Button arpaEat;
    [SerializeField] private Button waterDrink;
    [SerializeField] private Button staminaWaterDrink;

    [Header("Food Details")]
    [SerializeField] private SlideInFromLeft foodDetails;
    [Header("Destination X")]
    [SerializeField] private float continueBtnDestX = 100f;
    [SerializeField] private float backBtnDestX = -100f;
    [SerializeField] private float duration = 3f;

    private RectTransform continueRect;
    private RectTransform backRect;

    private Vector2 continueStartPos;
    private Vector2 backStartPos;
    private void Awake()
    {
        continueRect = continueBtn.GetComponent<RectTransform>();
        backRect = backBtn.GetComponent<RectTransform>();

        // Boshlang'ich pozitsiyani saqlab qo'yamiz
        continueStartPos = continueRect.anchoredPosition;
        backStartPos = backRect.anchoredPosition;
    }
    private void OnEnable()
    {
        Transilation();
        continueBtn.onClick.AddListener(ContinueGame);
        backBtn.onClick.AddListener(BackLobby);
        bugdoyEat?.onClick.AddListener(() => foodDetails.ToggleSlide(108, FoodCategory.Bugdoy)); // 100 - bu titleId, o'zgartirishingiz mumkin
        arpaEat?.onClick.AddListener(() => foodDetails.ToggleSlide(109, FoodCategory.Arpa));
        waterDrink?.onClick.AddListener(() => foodDetails.ToggleSlide(111, FoodCategory.Water));
        staminaWaterDrink?.onClick.AddListener(() => foodDetails.ToggleSlide(112, FoodCategory.StaminaWater));

        StartCoroutine(DelayedSlideIn(continueRect, continueBtnDestX, duration, 3f));
        StartCoroutine(DelayedSlideIn(backRect, backBtnDestX, duration, 3f));
    }

    private void OnDisable()
    {
        continueBtn.onClick.RemoveListener(ContinueGame);
        backBtn.onClick.RemoveListener(BackLobby);
        bugdoyEat?.onClick.RemoveAllListeners();
        arpaEat?.onClick.RemoveAllListeners();
        waterDrink?.onClick.RemoveAllListeners();
        staminaWaterDrink?.onClick.RemoveAllListeners();
        continueRect.anchoredPosition = continueStartPos;
        backRect.anchoredPosition = backStartPos;
    }

    #region UI Translations

    private void Transilation()
    {
        title.text = LanguageManager.Instance.GetText(257);
        ContinueBtnText.text = LanguageManager.Instance.GetText(258);
        backBtnText.text = LanguageManager.Instance.GetText(254);
    }
    #endregion
    public void UserLost(PrizeData prize)
    {
        foreach (Transform child in prizeParentLayout)
        {
            Destroy(child.gameObject);
        }

        foreach (var p in prize.losePrizes)
        {
            GameObject prizeGO = Instantiate(prizePrefab, prizeParentLayout);
            PrizeInfo prizeInfo = prizeGO.GetComponent<PrizeInfo>();
            prizeInfo.SetPrize(p);
        }
        npcText.SetText(LanguageManager.Instance.GetText(prize.loseMessageId));
        ContinueBtnText.text = (prize.prizeLast == 1) ? LanguageManager.Instance.GetText(261) : 
            LanguageManager.Instance.GetText(262);

    }
    private void ContinueGame()
    {
        if (KopkariManager.Instance != null)
        {
            KopkariManager.Instance.ContinueGame();
            //BaseManager.Instance.currentCondition = BaseManager.PlayerCondition.LoserSession;
            CloseAction();
        }
        else
        {
            Debug.LogError("BaseManager.Instance not found");
        }
    }

    private void CloseAction()
    {
        gameObject.SetActive(false);
    }
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
    }
    #region Button Animations
    private IEnumerator DelayedSlideIn(RectTransform rect, float destinationX, float duration, float delay)
    {
        yield return new WaitForSeconds(delay); // 🕒 3 sekund kutish

        float elapsed = 0f;
        Vector2 start = rect.anchoredPosition;
        Vector2 end = new Vector2(destinationX, start.y);

        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = end;
    }
    #endregion
}
