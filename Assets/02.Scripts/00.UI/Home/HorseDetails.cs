using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HorseDetails : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text title;

    private void OnEnable()
    {
        TextTransilations();
    }

    private void TextTransilations()
    {
        title.text = LanguageManager.Instance.GetText(105);
    }

}
