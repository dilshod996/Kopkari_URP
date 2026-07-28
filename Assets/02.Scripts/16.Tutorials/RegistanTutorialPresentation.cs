using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RegistanTutorialPresentation : MonoBehaviour
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

    [Header("Stable Objective Preview")]
    [SerializeField] private GameObject objectivePreviewRoot;
    [SerializeField] private RectTransform objectivePreviewTarget;
    [SerializeField] private Image objectivePreviewIcon;
    [SerializeField] private TMP_Text objectivePreviewLabel;
    [SerializeField] private TMP_Text objectivePreviewDistance;

    public GameObject PresentationRoot => presentationRoot;
    public Image Blocker => blocker;
    public RectTransform Highlight => highlight;
    public RectTransform Popup => popup;
    public TMP_Text TitleText => titleText;
    public TMP_Text DescriptionText => descriptionText;
    public Button NextButton => nextButton;
    public TMP_Text NextButtonText => nextButtonText;
    public RectTransform ObjectivePreviewTarget => objectivePreviewTarget;

    public bool HasRequiredReferences =>
        presentationRoot != null &&
        blocker != null &&
        highlight != null &&
        popup != null &&
        titleText != null &&
        descriptionText != null &&
        nextButton != null &&
        nextButtonText != null &&
        objectivePreviewRoot != null &&
        objectivePreviewTarget != null &&
        objectivePreviewIcon != null &&
        objectivePreviewLabel != null &&
        objectivePreviewDistance != null;

    public void SetObjectivePreview(
        bool visible,
        string label = "",
        string distance = "",
        Sprite icon = null,
        Color? color = null)
    {
        if (objectivePreviewRoot == null)
            return;

        objectivePreviewRoot.SetActive(visible);
        if (!visible)
            return;

        Color resolvedColor = color ?? Color.white;
        objectivePreviewLabel.text = label;
        objectivePreviewLabel.color = resolvedColor;
        objectivePreviewDistance.text = distance;

        objectivePreviewIcon.sprite = icon;
        objectivePreviewIcon.color = resolvedColor;
        objectivePreviewIcon.enabled = icon != null;
    }
}
