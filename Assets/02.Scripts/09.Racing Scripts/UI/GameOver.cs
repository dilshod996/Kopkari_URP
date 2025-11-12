using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button playAgain;
    [SerializeField] private Button backLobby;

    [Header("Anim Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutBack;
    public SceneLoadManager.SceneType sceneType;

    private void Start()
    {
        playAgain.onClick.AddListener(PlayAgainAction);
    }
    private void OnEnable()
    {
        ShowAnimation();
    }

    private void OnDisable()
    {
        // anim reset bo¡®lishi uchun qayta chaqirilganda toza holat
        canvasGroup.alpha = 0;
        transform.localScale = Vector3.one * 0.8f;
    }

    private void ShowAnimation()
    {
        // Dastlabki holat
        canvasGroup.alpha = 0;
        transform.localScale = Vector3.one * 0.8f;

        // Alpha (fade in)
        LeanTween.value(gameObject, 0f, 1f, fadeDuration)
            .setOnUpdate((float val) => canvasGroup.alpha = val);

        // Scale (zoom in)
        LeanTween.scale(gameObject, Vector3.one, scaleDuration)
            .setEase(easeType)
            .setDelay(0.05f); // biroz kechikish bilan chiroyli effekt
    }
    public void PlayAgainAction()
    {
        SceneLoadManager.Instance.LoadScene(sceneType);
    }
}
