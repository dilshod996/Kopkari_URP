using MalbersAnimations;
using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RacingController : MonoBehaviour
{
    public static RacingController Instance { get; protected set; }
    public MAnimal horse;
    [SerializeField] private List<AIRacingRider> aiRiders;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private float countdownDelay = 1f; // har bosqich oralig‘i
    [SerializeField] private float startTextDuration = 0.3f; // "Start" yozuvi qancha turadi



    [Header("Leaderboard Fade-In")]
    [SerializeField] private RacingLeaderboard leaderboard;
    [SerializeField] private CanvasGroup leaderboardGroup;        // leaderboard panel (CanvasGroup kerak)
    [SerializeField] private RectTransform leaderboardRoot;       // ixtiyoriy: scale pop anim uchun
    [SerializeField] private float leaderboardFadeDuration = 0.35f;
    [SerializeField] private LeanTweenType leaderboardEase = LeanTweenType.easeOutCubic;
    [SerializeField] private float leaderboardPopScale = 1.03f;   // 1.0 = popsiz

    [Header("ResultPage Fade-In")]
    [SerializeField] private RacingResultPage resultPage;
    [SerializeField] private CanvasGroup resultboardGroup;        // leaderboard panel (CanvasGroup kerak)
    [SerializeField] private RectTransform resultboardRoot;
    [SerializeField] private float resultFadeDuration = 0.01f;  // fast rejimda fade


    [SerializeField] private GameObject mobileCanvasPanel;


    [Header("Sprint UI Effect")]
    [SerializeField] private Image sprintImg;

    [Header("Reverse UI")]
    [SerializeField] private RectTransform reversePanel;     // boshida SetActive(false)
    [SerializeField] private TextMeshProUGUI reverseTimeText;
    [SerializeField] private float slideDuration = 0.25f;    // anim vaqti
    [SerializeField] private float panelShownY = -165f;         // ko‘rinadigan y
    [SerializeField] private float panelHiddenY = 150f;      // yuqoriga yashirin y (anchored)
    [SerializeField] private float reverseGraceTime = 5f;    // sekund
    [SerializeField] private float uiTick = 0.2f;            // progress text yangilash
    private Coroutine reverseCo;
    private bool reverseActive;
    private float tLeft;

    [Header("Popup Data")]
    [SerializeField] UISpeechBuble speechBubble;

    [Header("Game Over")]
    [SerializeField] GameOver gameOverPanel;
    [Header("Walk Zone Prefab")]
    public GameObject walkZonePrefab;
    [Header("Camera Details")]
    [SerializeField] private ThirdPersonFollowTarget mainCam;
    [SerializeField] private ThirdPersonFollowTarget backCam;
    [SerializeField] private ThirdPersonFollowTarget finishCam;
    private float _savedMainYaw;
    private float _savedMainPitch;

    public float cameraDistance = 4.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }
    void Start()
    {
        InitLeaderboardPanelHidden();
        SimplePool.CreatePool(walkZonePrefab, prewarm: 10, maxSize: 40, expandable: true);
        //GetSetAnimal(HorseMine.Instance.horseAnimal);
    }
    private void OnDestroy()
    {
        // poolingdagi barcha aktiv WalkZone obyektlarni qaytaradi
        SimplePool.ClearAll();
    }





    public void OnStartButtonPressed()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        countText.gameObject.SetActive(true);

        for (int i = 3; i >= 1; i--)
        {
            countText.text = i.ToString();
            yield return new WaitForSeconds(countdownDelay);
        }

        // "Start" yozuvi
        countText.text = "Start!";
        yield return new WaitForSeconds(startTextDuration);

        //countText.gameObject.SetActive(false);

        // endi poyga boshlanadi
        StartRacing();
    }
    public void StartRacing()
    {
        EnableNavMesh();
        StartRun();
        ShowLeaderboardPanel();
        RacingLeaderboard.Instance.StartRace();
    }

    public void StopRacing()
    {
        DisableNavmesh();
        //StopAlwaysForward();
    }
    #region LeaderBoard
    private void InitLeaderboardPanelHidden()
    {
        if (!leaderboardGroup) return;

        leaderboardGroup.gameObject.SetActive(true); // alpha 0: ko‘rinmaydi, lekin mavjud
        leaderboardGroup.alpha = 0f;
        leaderboardGroup.interactable = false;
        leaderboardGroup.blocksRaycasts = false;

        if (leaderboardRoot)
        {
            leaderboardRoot.localScale = Vector3.one; // boshlang‘ich holat
        }
    }

    private void ShowLeaderboardPanel()
    {
        if (!leaderboardGroup) return;

        // avvalgi tweennni to‘xtatib qo‘yish xavfsiz
        LeanTween.cancel(leaderboardGroup.gameObject);

        // ixtiyoriy: kichik pop effekt
        if (leaderboardRoot && leaderboardPopScale > 1f)
        {
            leaderboardRoot.localScale = Vector3.one * leaderboardPopScale;
            LeanTween.scale(leaderboardRoot, Vector3.one, leaderboardFadeDuration)
                     .setEase(leaderboardEase);
        }

        // alpha fade-in
        leaderboardGroup.alpha = 0f;
        LeanTween.alphaCanvas(leaderboardGroup, 1f, leaderboardFadeDuration)
                 .setEase(leaderboardEase)
                 .setOnComplete(() =>
                 {
                     leaderboardGroup.interactable = true;
                     leaderboardGroup.blocksRaycasts = true;
                 });
    }
    private void HideLeaderboardPanel()
    {
        if (!leaderboardGroup) return;

        leaderboardGroup.interactable = false;
        leaderboardGroup.blocksRaycasts = false;

        LeanTween.cancel(leaderboardGroup.gameObject);
        LeanTween.alphaCanvas(leaderboardGroup, 0f, leaderboardFadeDuration)
                 .setEase(leaderboardEase);
    }
    #endregion

    #region AI Horses
    public void EnableNavMesh()
    {
        for(int i = 0; i < aiRiders.Count; i++)
        {
            aiRiders[i].EnableNavmesh();
        }
    }
    public void DisableNavmesh()
    {
        for(int i = 0;i < aiRiders.Count; i++)
        {
            aiRiders[i].DisableNavmesh();
        }
    }
    #endregion

    #region Horse Manage
    public void StartRun()
    {
        StartHorseRun(horse);
    }
    public void GetSetAnimal(MAnimal mAnimal)
    {
        horse = mAnimal;
    }
    public void StartHorseRun(MAnimal mAnimal)
    {
        StartCoroutine(HorseRunStarter(mAnimal));
    }

    private IEnumerator HorseRunStarter(MAnimal mAnimal)
    {
        horse = mAnimal;

        // horse null bo‘lmaguncha kutadi
        yield return new WaitUntil(() => horse != null);

        horse.Always_Forward(true);
        mobileCanvasPanel.gameObject.SetActive(true);
    }
    public void StopHorseRun()
    {
        StartCoroutine(HorseStopAction(ShowResultPanel));
    }
    private IEnumerator HorseStopAction(Action action)
    {
        // 3 soniyadan so‘ng to‘xtatamiz misol uchun
        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);
        CameraPostionCheck();
        HideLeaderboardPanel();
        mobileCanvasPanel.gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        horse.StopMoving();
        action?.Invoke();
    }
    #endregion

    #region Scene Details.
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Home);
    }
    #endregion

    #region Final Page
   
    /// <summary>
    /// Final sahifani animatsiya bilan ko‘rsatadi va anim tugagach ro‘yxatni quradi.
    /// </summary>
    public void ShowResultPanel()
    {
        if (!resultboardGroup || !resultPage) return;

        // Tweenga toza holat
        LeanTween.cancel(resultboardGroup.gameObject);
        if (resultboardRoot) LeanTween.cancel(resultboardRoot.gameObject);

        // Ko‘rinadigan, lekin yashirin (alpha=0)
        resultboardGroup.gameObject.SetActive(true);
        resultboardGroup.alpha = 0f;
        resultboardGroup.interactable = false;
        resultboardGroup.blocksRaycasts = false;

        if (resultboardRoot)
            resultboardRoot.localScale = (leaderboardPopScale > 1f) ? Vector3.one * leaderboardPopScale : Vector3.one;

        // STANDINGS ni anim tugagach olish — shunda snapshot “final” bo‘ladi
        // (agar hohlasang oldindan ham olishing mumkin)
        var lb = RacingLeaderboard.Instance;

        // Sequence: (pop -> fade) -> BuildList
        var seq = LeanTween.sequence();

        // Pop (agar kerak bo‘lsa)
        if (resultboardRoot && leaderboardPopScale > 1f)
        {
            seq.append(LeanTween.scale(resultboardRoot, Vector3.one, resultFadeDuration * 0.55f).setEase(leaderboardEase));
        }

        // Fade-in
        seq.append(LeanTween.alphaCanvas(resultboardGroup, 1f, resultFadeDuration).setEase(leaderboardEase));

        // Anim tugagach: interaktivni yoqamiz va BuildList chaqiramiz
        seq.append(() =>
        {
            resultboardGroup.interactable = true;
            resultboardGroup.blocksRaycasts = true;

            var standings = lb?.GetStandings();  // List<RacingAgent>
            if (standings != null)
            {
                // 🔥 Endi final listni quramiz
                resultPage.BuildList(standings);
            }
        });
    }
    #endregion

    #region Horse Back Running
    public void StartReverse()
    {
        // Timer reset (agar allaqachon aktiv bo‘lsa ham yangilaymiz)
        tLeft = reverseGraceTime;
        SpeechBubbleEnable("Ogohlantirish orqaga yugurayapsan!");
        if (!reverseActive)
        {
            reverseActive = true;
            ShowPanel(); // SetActive(true) + slide in

            if (reverseCo != null) StopCoroutine(reverseCo);
            reverseCo = StartCoroutine(ReverseCountdown());
        }
        // reverseActive bo‘lsa ham faqat tLeft yangilandi (UI shu korutinada yangilanadi)
    }

    public void ClearReverse()
    {
        if (!reverseActive) return;
        SpeechBubbleDisable();
        reverseActive = false;
        if (reverseCo != null) { StopCoroutine(reverseCo); reverseCo = null; }
        HidePanel(); // slide out + SetActive(false)

    }

    // ===== UI Anim (LeanTween) =====
    private void ShowPanel()
    {
        if (!reversePanel) return;

        reversePanel.gameObject.SetActive(true); // LT uchun active bo‘lishi shart
        LeanTween.cancel(reversePanel);

        // start pozitsiya: hiddenY
        var ap = reversePanel.anchoredPosition;
        reversePanel.anchoredPosition = new Vector2(ap.x, panelHiddenY);

        LeanTween.value(reversePanel.gameObject, panelHiddenY, panelShownY, slideDuration)
                 .setEaseOutCubic()
                 .setOnUpdate((float y) =>
                 {
                     var p = reversePanel.anchoredPosition;
                     reversePanel.anchoredPosition = new Vector2(p.x, y);
                 });
    }

    private void HidePanel()
    {
        if (!reversePanel) return;

        LeanTween.cancel(reversePanel);
        var ap = reversePanel.anchoredPosition;

        LeanTween.value(reversePanel.gameObject, ap.y, panelHiddenY, slideDuration)
                 .setEaseInCubic()
                 .setOnUpdate((float y) =>
                 {
                     var p = reversePanel.anchoredPosition;
                     reversePanel.anchoredPosition = new Vector2(p.x, y);
                 })
                 .setOnComplete(() =>
                 {
                     if (reverseTimeText) reverseTimeText.text = "";
                     reversePanel.gameObject.SetActive(false); // qayta inactive
                 });
    }

    // ===== Countdown (Update yo‘q) =====
    private IEnumerator ReverseCountdown()
    {
        var wait = new WaitForSecondsRealtime(uiTick);

        while (reverseActive && tLeft > 0f)
        {
            if (reverseTimeText) reverseTimeText.text = $"{tLeft:0}";
            tLeft -= uiTick;
            yield return wait;
        }

        reverseCo = null;

        if (reverseActive)
        {
            // Timeout → DQ
            reverseActive = false;
            HidePanel();
            Disqualify();
        }
    }

    // ===== DQ logika =====
    private void Disqualify()
    {
        // Bu yerda sizning o‘yindagi jazo:
        // - Malbers: hayvonni bloklash
        // - Result page/popup
        // - Leaderboardga signal
        // Masalan:
        // var animal = FindObjectOfType<MalbersAnimations.Controller.MAnimal>();
        // if (animal) animal.Lock(true);
        StartCoroutine(HorseStopAction(GameOverPanel));
        SpeechBubbleDisable();
        Debug.Log("[RacingController] Reverse timeout -> DQ");
    }

    private void GameOverPanel()
    {
        gameOverPanel.gameObject.SetActive(true);
    }
    #endregion

    #region Popup Speech Bubble
    public void ShowAndHideSpeech(string speech)
    {
        StartCoroutine(SpeechCoroutine(speech));
    }
    private IEnumerator SpeechCoroutine(string text)
    {
        SpeechBubbleEnable(text);
        yield return new WaitForSeconds(3.5f);
        SpeechBubbleDisable();
    }
    public void SpeechBubbleEnable(string text)
    {
       // speechBubble.gameObject.SetActive(true);
        speechBubble.Show(text);
    }
    public void SpeechBubbleDisable()
    {
        speechBubble.Hide();
    }
    #endregion

    #region Camera Details
    public void CameraPostionCheck()
    {
        horse.UseCameraInput = false;
        finishCam.SetFinishViewSmooth(
            cameraDistance,  // masofa
            -33f,             // yaw -> Inspector’da 18 bo'lishi uchun
            6.5f,             // pitch (istaganingcha o'zgartirishing mumkin)
            0.13f,  // pastga/pasga offset kerak bo'lsa
            1f               // 1 soniyada aylanib borsin, xohlasang 0.5 / 2f qil
        );
    }
    public void LookBack()
    {
        horse.UseCameraInput = false;
        CacheMainCamView();
        // masalan lookBackCam – bu LookBack virtual kamera ichidagi ThirdPersonFollowTarget
        backCam.SetBackViewInstant(
            distance: 3f,       // yoki o'zing xohlagan masofa

            verticalOffset: 0.4f   // agar ekstra tushirmoqchi bo'lsang, yoki 0f qoldirsa ham bo'ladi
        );

    }
    public void CacheMainCamView()
    {
        _savedMainYaw = mainCam._cinemachineTargetYaw;
        _savedMainPitch = mainCam._cinemachineTargetPitch;
    }
    public void MainCam()
    {
        StartCoroutine(ReturnToMainCamRoutine());
    }

    private IEnumerator ReturnToMainCamRoutine()
    {


        // Kamera view'ni birdan eski holatga qaytaramiz
        mainCam.SetViewInstant(
           distance: 6f,
           targetYaw: _savedMainYaw,      // 157 o'rniga oldingi qiymat
           targetPitch: _savedMainPitch,  // 13 o'rniga oldingi qiymat
           verticalOffset: 0
       );

        // Bitta frame yoki kichik delay kutamiz – SetViewInstant ichidagi coroutine ishini tugatib olsin
        yield return new WaitForSeconds(0.2f);               // xohlasang yield return new WaitForSeconds(0.1f);



        // Endi otni inputga qaytadan bog'laymiz
        horse.UseCameraInput = true;
    }

    #endregion

}
