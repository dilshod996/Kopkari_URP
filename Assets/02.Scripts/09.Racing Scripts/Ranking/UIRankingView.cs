using TMPro;
using UnityEngine;

public class UIRankingView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text ranking;
    [SerializeField] private TMP_Text teamName;

    [Header("Anim Wrapper (child)")]
    [SerializeField] private RectTransform animContainer; // <-- child (AnimContainer)
    [SerializeField] private float moveTime = 0.18f;
    [SerializeField] private float scaleTime = 0.10f;
    [SerializeField] private float upPunch = 1.08f;
    [SerializeField] private float downShrink = 0.97f;

    private int moveTweenId = -1;
    private int scaleTweenId = -1;

    private void Awake()
    {
        if (animContainer == null)
        {
            // fallback: o'zing assign qilganing yaxshi, bu faqat emergency
            animContainer = transform.childCount > 0 ? transform.GetChild(0) as RectTransform : null;
        }
    }

    public void SetData( string rank, string name, string teamname)
    {
        if (nameText) nameText.text = name;
        if (ranking) ranking.text = rank;
        if (teamName) teamName.text = teamname.ToUpper()[..Mathf.Min(3, teamname.Length)];

    }

    public void SetColor(Color nameColor)
    {
        if (nameText) nameText.color = nameColor;
        if (ranking) ranking.color = nameColor;
        if (teamName) teamName.color = nameColor;
    }

    /// <summary>
    /// Layout root joyi o'zgarganda, animContainer'ni offset bilan anim qilamiz.
    /// deltaRank: oldRank - newRank (positive => tepaga chiqdi)
    /// </summary>
    public void AnimateRankDelta(float yDelta, int deltaRank)
    {
        if (animContainer == null) return;

        // cancel
        if (moveTweenId != -1) LeanTween.cancel(moveTweenId);
        if (scaleTweenId != -1) LeanTween.cancel(scaleTweenId);

        // 1) Offset beramiz (go'yo eski joyda turgandek)
        animContainer.anchoredPosition = new Vector2(0f, yDelta);

        // 2) Keyin 0 ga qaytaramiz (layout joyiga "keladi")
        moveTweenId = LeanTween
            .moveY(animContainer, 0f, moveTime)
            .setEase(LeanTweenType.easeOutCubic)
            .id;

        // 3) Scale feedback (up/down)
        float targetScale = 1f;
        if (deltaRank > 0) targetScale = upPunch;
        else if (deltaRank < 0) targetScale = downShrink;

        if (Mathf.Abs(targetScale - 1f) > 0.001f)
        {
            scaleTweenId = LeanTween
                .scale(animContainer, Vector3.one * targetScale, scaleTime)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() =>
                {
                    LeanTween.scale(animContainer, Vector3.one, scaleTime).setEase(LeanTweenType.easeInOutSine);
                })
                .id;
        }
    }
}
