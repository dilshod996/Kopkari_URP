using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeTutorialPresentation : MonoBehaviour
{
    [SerializeField] private GameObject presentationRoot;
    [SerializeField] private Image blocker;
    [SerializeField] private TutorialTargetHoleRaycastFilter raycastFilter;
    [SerializeField] private RectTransform highlight;
    [SerializeField] private RectTransform popup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    private Transform originalHighlightParent;
    private int originalHighlightSiblingIndex;

    public GameObject PresentationRoot => presentationRoot;
    public RectTransform PresentationRect =>
        presentationRoot != null
            ? presentationRoot.transform as RectTransform
            : null;
    public Image Blocker => blocker;
    public TutorialTargetHoleRaycastFilter RaycastFilter => raycastFilter;
    public RectTransform Highlight => highlight;
    public RectTransform Popup => popup;
    public TMP_Text TitleText => titleText;
    public TMP_Text DescriptionText => descriptionText;
    public Button NextButton => nextButton;
    public TMP_Text NextButtonText => nextButtonText;

    public bool HasRequiredReferences =>
        presentationRoot != null &&
        PresentationRect != null &&
        blocker != null &&
        raycastFilter != null &&
        highlight != null &&
        popup != null &&
        titleText != null &&
        descriptionText != null &&
        nextButton != null &&
        nextButtonText != null;

    public void ParkHighlight()
    {
        if (highlight == null)
            return;

        CacheOriginalHighlightParent();
        if (originalHighlightParent == null)
            return;

        highlight.SetParent(originalHighlightParent, false);
        highlight.SetSiblingIndex(
            Mathf.Clamp(
                originalHighlightSiblingIndex,
                0,
                originalHighlightParent.childCount - 1));
        highlight.anchorMin = new Vector2(0.5f, 0.5f);
        highlight.anchorMax = new Vector2(0.5f, 0.5f);
        highlight.pivot = new Vector2(0.5f, 0.5f);
        highlight.anchoredPosition = Vector2.zero;
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = Vector3.one;
        highlight.gameObject.SetActive(false);
    }

    public void KeepPopupOnTop()
    {
        if (popup != null)
            popup.SetAsLastSibling();
    }

    private void CacheOriginalHighlightParent()
    {
        if (originalHighlightParent != null || highlight == null)
            return;

        originalHighlightParent = highlight.parent;
        originalHighlightSiblingIndex = highlight.GetSiblingIndex();
    }
}
