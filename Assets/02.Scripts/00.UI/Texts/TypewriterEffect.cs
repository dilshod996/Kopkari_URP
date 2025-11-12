using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    public int translationId = -1; // JSON dagi matn ID raqami
    public float delay = 0.05f;

    private string fullText = "";
    private string currentText = "";
    private TextMeshProUGUI textMeshPro;
    private Coroutine typingCoroutine;

    void OnEnable()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();

        // Agar ID berilgan bo¡®lsa, o¡®sha matnni olamiz
        if (translationId > 0)
            SetText(LanguageManager.Instance.GetText(translationId));
    }

    public void SetText(string text)
    {
        fullText = text;
        currentText = "";
        textMeshPro.text = currentText;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(ShowText());
    }

    IEnumerator ShowText()
    {
        for (int i = 0; i <= fullText.Length; i++)
        {
            currentText = fullText.Substring(0, i);
            textMeshPro.text = currentText;
            yield return new WaitForSeconds(delay);
        }
    }
}

