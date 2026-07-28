using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RacingTutorialPresentation : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private GameObject presentationRoot;
    [SerializeField] private Image blocker;
    [SerializeField] private RectTransform highlight;
    [SerializeField] private RectTransform popup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    public GameObject PresentationRoot => presentationRoot;
    public Image Blocker => blocker;
    public RectTransform Highlight => highlight;
    public RectTransform Popup => popup;
    public TMP_Text TitleText => titleText;
    public TMP_Text DescriptionText => descriptionText;
    public Button NextButton => nextButton;
    public TMP_Text NextButtonText => nextButtonText;

    public bool HasRequiredReferences =>
        presentationRoot != null &&
        blocker != null &&
        highlight != null &&
        popup != null &&
        titleText != null &&
        descriptionText != null &&
        nextButton != null &&
        nextButtonText != null;
}
