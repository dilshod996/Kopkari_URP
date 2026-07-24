using Michsky.UI.ModernUIPack;
using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ProgressBar progressBar;
    [SerializeField] private TMP_Text randomText;

    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject baxmalRacingPanel;
    [SerializeField] private GameObject jomboyKopkariPanel;
    [SerializeField] private GameObject egyptPanel;

    private Coroutine progressRoutine;
    private Coroutine textRoutine;

    private void OnEnable()
    {
        //SoundManager.Instance?.StopMusicEvent();
        SoundManager.Instance?.StopRoomSmooth();
        ApplyScenePanel(SceneLoadManager.Instance != null
            ? SceneLoadManager.Instance.CurrentSceneType
            : SceneLoadManager.SceneType.Home);
    }

    private void OnDisable()
    {
        StopLoadingRoutines();
    }

    private void ApplyScenePanel(SceneLoadManager.SceneType sceneType)
    {
        // 1) hammasini o'chiramiz (clean slate)
        SetAllPanels(false);

        // 2) oldingi coroutine'larni to'xtatamiz
        StopLoadingRoutines();

        // 3) kerakli panel + kerakli rutinalar
        switch (sceneType)
        {
            case SceneLoadManager.SceneType.Home:
                homePanel.SetActive(true);
                StartLoadingRoutines();
                break;

            case SceneLoadManager.SceneType.SecondRacing:
                baxmalRacingPanel.SetActive(true);
                break;

            case SceneLoadManager.SceneType.Beginer:
            case SceneLoadManager.SceneType.Registan:
                jomboyKopkariPanel.SetActive(true);
                break;

            case SceneLoadManager.SceneType.EgyptRacing:
                egyptPanel.SetActive(true);
                break;

            default:
                homePanel.SetActive(true);
                StartLoadingRoutines();
                break;
        }
    }

    private void SetAllPanels(bool state)
    {
        if (homePanel != null) homePanel.SetActive(state);
        if (baxmalRacingPanel != null) baxmalRacingPanel.SetActive(state);
        if (jomboyKopkariPanel != null) jomboyKopkariPanel.SetActive(state);
        if (egyptPanel != null) egyptPanel.SetActive(state);
    }

    private void StartLoadingRoutines()
    {
        if (progressBar != null)
        {
            progressRoutine = StartCoroutine(ProgressbarRoutine());
        }

        if (randomText != null)
        {
            textRoutine = StartCoroutine(ChangeTextRoutine());
        }
    }

    private void StopLoadingRoutines()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }

        if (textRoutine != null)
        {
            StopCoroutine(textRoutine);
            textRoutine = null;
        }
    }

    private IEnumerator ProgressbarRoutine()
    {
        while (true)
        {
            float current = SceneLoadManager.Instance != null
                ? SceneLoadManager.Instance.loadingTime
                : 0f;

            current = Mathf.Clamp(current, 0f, 100f);

            progressBar.currentPercent = current;
            progressBar.UpdateUI();

            if (current >= 100f)
                yield break;

            yield return null;
        }
    }


    private IEnumerator ChangeTextRoutine()
    {
        // safety: manager yo'q bo'lsa loopni to'xtatamiz
        while (LanguageManager.Instance != null)
        {
            int randomIndex = Random.Range(6, 20);
            randomText.text = LanguageManager.Instance.GetText(randomIndex);
            yield return new WaitForSeconds(3f);
        }
    }
}
