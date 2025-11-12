using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RacingResultPage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentParent;   // VerticalLayoutGroup
    [SerializeField] private UIRacingPlayerFinal itemPrefab;
    [SerializeField] private Button replayButton;            // Yangi qo¡®shilgan Start Button
    [SerializeField] private CanvasGroup startButtonGroup;  // Alpha animatsiya uchun

    private readonly List<UIRacingPlayerFinal> _spawned = new();
    private Coroutine animateRoutine;

    [Header("Options")]
    [SerializeField] private bool sortByRankingAsc = true;  // #1, #2, #3...
    [SerializeField] private float slideOffset = 420f;      // chapdan qanchaga siljib kelsin
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float stagger = 0.06f;         // ketma-ket delay
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutCubic;
    [SerializeField] private bool clearOnBuild = true;
    public SceneLoadManager.SceneType sceneType;

    private void Start()
    {
        // Boshlanishda start tugmasini yashiramiz
        if (startButtonGroup)
        {
            startButtonGroup.alpha = 0f;
            startButtonGroup.interactable = false;
            startButtonGroup.blocksRaycasts = false;
        }
        //BuildList(RacingLeaderboard.Instance.GetStandings());
    }
    private void OnEnable()
    {
        if(replayButton != null)
        {
            replayButton.onClick.AddListener(Replay);
        }
    }
    public void BuildList(List<RacingAgent> entries)
    {
        if (clearOnBuild)
            Clear();

        // Tartiblash
        entries = sortByRankingAsc
            ? entries.OrderBy(e => e.Ranking).ToList()
            : entries.OrderByDescending(e => e.Ranking).ToList();

        // Spawn qilish
        foreach (var e in entries)
        {
            var item = Instantiate(itemPrefab, contentParent);
            item.Bind(e);
            _spawned.Add(item);
        }

        // Animatsiyani ishga tushirish
        animateRoutine = StartCoroutine(AnimateInSequence());
    }


    public void Clear()
    {
        StopAllCoroutines();

        foreach (var it in _spawned)
        {
            if (it)
            {
                LeanTween.cancel(it.gameObject);
                Destroy(it.gameObject);
            }
        }
        _spawned.Clear();

        // Start buttonni ham yashirish
        if (startButtonGroup)
        {
            LeanTween.cancel(startButtonGroup.gameObject);
            startButtonGroup.alpha = 0f;
            startButtonGroup.interactable = false;
            startButtonGroup.blocksRaycasts = false;
        }
    }
    private IEnumerator AnimateInSequence()
    {
        yield return null;

        for (int i = 0; i < _spawned.Count; i++)
        {
            var item = _spawned[i];
            if (!item) continue;

            var rt = item.GetComponent<RectTransform>();
            var cg = item.GetComponent<CanvasGroup>();

            var targetPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(targetPos.x - slideOffset, targetPos.y);

            float delay = i * stagger;

            LeanTween.move(rt, targetPos, slideDuration)
                .setDelay(delay)
                .setEase(easeType);

            if (cg)
            {
                cg.alpha = 0f;
                LeanTween.value(item.gameObject, 0f, 1f, slideDuration * 0.9f)
                    .setDelay(delay + 0.02f)
                    .setOnUpdate((float a) => cg.alpha = a);
            }
        }

        // Barcha itemlar chiqib bo¡®lishini kutish
        float totalDelay = _spawned.Count * stagger + slideDuration;
        yield return new WaitForSeconds(totalDelay + 0.2f);

        // Start buttonni alpha orqali chiqish
        if (startButtonGroup)
        {
            LeanTween.value(startButtonGroup.gameObject, 0f, 1f, 0.6f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnUpdate((float a) =>
                {
                    startButtonGroup.alpha = a;
                })
                .setOnComplete(() =>
                {
                    startButtonGroup.interactable = true;
                    startButtonGroup.blocksRaycasts = true;
                });
        }
    }
    public void Replay()
    {
        SceneLoadManager.Instance.LoadScene(sceneType);
    }

    private void OnDisable()
    {
        Clear();
        replayButton.onClick.RemoveAllListeners();
    }
}
