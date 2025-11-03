using MalbersAnimations.Controller;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }
    void Start()
    {
        InitLeaderboardPanelHidden();
    }

    // Update is called once per frame
    void Update()
    {
        
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
    }
    public void StopHorseRun()
    {
        StartCoroutine(HorseStop());
    }
    private IEnumerator HorseStop()
    {


        // 3 soniyadan so‘ng to‘xtatamiz misol uchun
        horse.Always_Forward(false);
        horse.Speed_CurrentIndex_Set(2);
        HideLeaderboardPanel();
        mobileCanvasPanel.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);
        horse.StopMoving();
        ShowResultPanel();

        //horse.Move(Vector3.zero);
        //horse.Speed = 0;
    }
    #endregion

    #region Scene Details.
    public void BackLobby()
    {
        SceneLoadManager.Instance.LoadScene(SceneLoadManager.SceneType.Lobby);
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
}
