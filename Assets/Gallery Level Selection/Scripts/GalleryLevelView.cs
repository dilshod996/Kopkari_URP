using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GalleryLevelView : MonoBehaviour
{
    public GalleryLevelSelectionManager manager;
    public Image image;
    public TMP_Text text;
    public string levelName;
    public bool colorEffectOnText = false;

    [HideInInspector] public float index;
    public float progress;

    public RectTransform image1;
    public RectTransform image2;

    public float changeAction;

    public Image hiddenWall;
    public virtual void UpdateProgress(float value, float linearValue)
    {

        if (value != progress)
        {
            if (manager && manager.hasColorTransition)
            {
                if (image != null)
                {
                    changeAction = manager.linearColor ? linearValue : value;
                    if (image) image.color = GetFadedColor(manager.transitionColorStart, manager.transitionColorEnd,changeAction);
                }
                if (text != null)
                {
                    if (text && colorEffectOnText) text.color = GetFadedColor(manager.transitionColorStart, manager.transitionColorEnd, manager.linearColor ? linearValue : value);
                }
            }

            progress = value;
        }

        bool isTarget = this.transform.localScale.x > 0.99;
        if (image1 != null && image2 != null)
        {
            
            float targetPosY = isTarget? 215f : 45f;
            float negativeTargetPosY = isTarget ? -215f : -45f;
            image.gameObject.SetActive(!isTarget);

            image1.anchoredPosition = new Vector2(image1.anchoredPosition.x, targetPosY);
            image2.anchoredPosition = new Vector2(image2.anchoredPosition.x, negativeTargetPosY);
        }
        if (hiddenWall != null)
        {
            hiddenWall.gameObject.SetActive(!isTarget);
        }

    }

    public static Color GetFadedColor(Color start, Color end, float ratio)
    {
        return new Color((start.r + (ratio * (end.r - start.r))), (start.g + (ratio * (end.g - start.g))), (start.b + (ratio * (end.b - start.b))), (start.a + (ratio * (end.a - start.a))));
    }
}
