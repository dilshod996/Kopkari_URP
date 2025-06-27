using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWinSession : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text ContinueBtnText;
    [SerializeField] private Button continueBtn;
    [SerializeField] private TMP_Text backBtnText;
    [SerializeField] private Button backBtn;

    [Header("Prize Display")]
    [SerializeField] private GameObject prizePrefab;
    [SerializeField] private Transform prizeParentLayout;

    [Header("Session Info")]

    [SerializeField] private TMP_Text sessionTimeTitle;
    [SerializeField] private TMP_Text sessionTimeText;
    [SerializeField] private TMP_Text lambsCaughtTitle;
    [SerializeField] private TMP_Text lambsCaughtText;

    [Header("Best player details")]
    [SerializeField] private TMP_Text bestTitle;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text catchTime;


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
        continueBtn.onClick.AddListener(ContinueGame);
        backBtn.onClick.AddListener(BackLobby);
        Transilation();
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
    #region UI Transilations
    private void Transilation()
    {
        title.text = LanguageManager.Instance.GetText(252);
        ContinueBtnText.text = LanguageManager.Instance.GetText(253);
        backBtnText.text = LanguageManager.Instance.GetText(254);
        sessionTimeTitle.text = LanguageManager.Instance.GetText(255);
        lambsCaughtTitle.text = LanguageManager.Instance.GetText(256);
        bestTitle.text = LanguageManager.Instance.GetText(266);
    }    
    #endregion
    public void DisplayPrizes(PrizeData prize)
    {
        if (prize == null)
        {
            Debug.LogWarning("PrizeData is null");
            return;
        }
            
        float spentTime = prize.roundTime - BaseManager.Instance.mainTime;
        Debug.Log("time spent: " + spentTime);
        int lambsCaught = BaseManager.Instance.catchCounter;
        TimeSpan timeSpan = TimeSpan.FromSeconds(spentTime); //$"{totalTimeSpan.Minutes:D2}:{totalTimeSpan.Seconds:D2}"
        sessionTimeText.text = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        lambsCaughtText.text = lambsCaught.ToString() + " " + LanguageManager.Instance.GetText(277);

        foreach (Transform child in prizeParentLayout)
        {
            Destroy(child.gameObject);
        }

        foreach (var p in prize.winPrizes)
        {
            GameObject prizeGO = Instantiate(prizePrefab, prizeParentLayout);
            PrizeInfo prizeInfo = prizeGO.GetComponent<PrizeInfo>();
            prizeInfo.SetPrize(p);
        }

        ContinueBtnText.text = (prize.prizeLast == 1) ? LanguageManager.Instance.GetText(261) : 
            LanguageManager.Instance.GetText(262);
    }

    private void ContinueGame()
    {
        if (BaseManager.Instance != null)
        {
            BaseManager.Instance.ContinueGame();
            //BaseManager.Instance.currentCondition = BaseManager.PlayerCondition.WinnerSession;
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
