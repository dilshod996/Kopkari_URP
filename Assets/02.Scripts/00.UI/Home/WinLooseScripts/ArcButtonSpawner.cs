using UnityEngine;
using System.Collections.Generic;

public class ArcButtonAnimator : MonoBehaviour
{
    [Header("Main Button Parent")]
    public Transform mainButton;

    [Header("Buttons (left-right, top-sides, top-center)")]
    public List<GameObject> buttons;

    [Header("Expanded Positions")]
    public List<Vector3> expandedPositions = new List<Vector3>();

    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float delayBetween = 0.1f;

    private bool isExpanded = false;

    private void OnEnable()
    {
        foreach (var btn in buttons)
        {
            btn.SetActive(false);
        }
    }
    public void ToggleButtons()
    {
        if (isExpanded)
        {
            // Collapse
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i; // closure safe
                LeanTween.moveLocal(buttons[i], Vector3.zero, animationDuration)
                         .setDelay(i * delayBetween)
                         .setOnComplete(() => {
                             buttons[index].SetActive(false);
                         });
            }
            isExpanded = false;
        }
        else
        {
            // Expand
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                buttons[i].SetActive(true);
                buttons[i].transform.localPosition = Vector3.zero; // start from main
                LeanTween.moveLocal(buttons[i], expandedPositions[i], animationDuration)
                         .setDelay(i * delayBetween);
            }
            isExpanded = true;
        }
    }
}
