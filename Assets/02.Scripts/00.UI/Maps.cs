using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Maps : MonoBehaviour
{
    [SerializeField] private Button closeBtn;
    [SerializeField] GalleryLevelSelectionManager scrollMenu;
    public TMP_Text infoText;        // Info matni
    public float scrollSpeed = 100f; // Harakat tezligi
    private float startPosX = 1484f; // Boshlanish X pozitsiyasi
    private float endPosX = -1484f;  // Tugash X pozitsiyasi
    private RectTransform rectTransform;

    Coroutine textCoroutine;

    private List<string> loadingMessages = new List<string>
    {
        "Chavandoz tayyorlaning! Maydonga chiqish vaqti keldi!",
        "Otni mahkam tuting, uloq uchun kurash boshlanmoqda!",
        "Faqat eng chaqqon va epchil chavandozlar g¡®alaba qozonadi!",
        "Chang-to¡®zon ichida haqiqiy qahramon aniq bo¡®ladi!",
        "Ko¡®pkari maydonida faqat mard va kuchli chavandozlar qoladi!",
        "Otingizga ishonchingiz komilmi? Ko¡®pkari maydonida sinov kutmoqda!",
        "Uloqni qo¡®lga kiritib, g¡®alabani tantana qilish vaqti keldi!",
        "Otlarning dupuri va kurash shovqini sizni kutmoqda!",
        "Faqat eng mahoratli chavandoz g¡®olib bo¡®ladi!",
        "Jangga shay turing! Ko¡®pkari maydoni sizni kutmoqda!",
        "Otingiz shamoldan tez yugura oladimi? Ko¡®ramiz!",
        "Bugun Ko'pkari maydonida tarix yoziladi!",
        "Raqiblaringiz ham shay, lekin siz eng yaxshisisiz!",
        "Otin mahkam ushla, uloq osonlikcha qo¡®lga tushmaydi!",
        "Asl chavandoz mag¡®lubiyatni tan olmaydi!"
    };
    void Start()
    {
        closeBtn.onClick.AddListener(ClosePanel);
        
    }

    void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        //scrollMenu.start_level = 1;
        if (rectTransform == null)
        {
            rectTransform = infoText.GetComponent<RectTransform>();
        }
        textCoroutine = StartCoroutine(ScrollText());
    }
    private void OnDisable()
    {
        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
        }
    }
    public void Check()
    {
        Debug.Log("Check");
    }

    private IEnumerator ScrollText()
    {
        while (true)
        {
            // Tasodifiy matnni tanlash
            infoText.text = loadingMessages[Random.Range(0, loadingMessages.Count)];

            // Boshlanish X pozitsiyasiga o'rnating
            rectTransform.anchoredPosition = new Vector2(startPosX, rectTransform.anchoredPosition.y);

            // Harakatlanish jarayoni
            float elapsedTime = 0f;
            while (elapsedTime < Mathf.Abs(startPosX - endPosX) / scrollSpeed)
            {
                // Vaqtga qarab X pozitsiyasini yangilash
                float newXPos = Mathf.Lerp(startPosX, endPosX, elapsedTime * scrollSpeed / Mathf.Abs(startPosX - endPosX));
                rectTransform.anchoredPosition = new Vector2(newXPos, rectTransform.anchoredPosition.y);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}
