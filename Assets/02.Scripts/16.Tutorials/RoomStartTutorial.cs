using System.Collections;
using TMPro;
using UnityEngine;

public class RoomStartTutorial : MonoBehaviour
{
    public GameObject[] pages; // 3 ta sahifani saqlaydi
    public UIGamePlayerList playerListTable;
    public WarmUpTutorial warmUpTutorial;

    [SerializeField] private TMP_Text firstPageText;
    [SerializeField] private TMP_Text secondPageText;
    [SerializeField] private TMP_Text thirdPageText;

    public int firstPageTextId = 1;
    public int secondPageTextId = 2;
    public int thirdPageTextId = 3;

    private int currentPageIndex = 0;

    private void Start()
    {
        StartCoroutine(ShowPagesAuto());
    }

    private IEnumerator ShowPagesAuto()
    {
        while (currentPageIndex < pages.Length)
        {
            ShowPage(currentPageIndex); // Sahifani ko¡®rsatamiz
            SetPageText(currentPageIndex); // Sahifa matnini o¡®rnatamiz

            yield return new WaitForSeconds(2.5f); // 2 sekund kutamiz

            // Sahifani yo'q qilish animatsiyasi
            yield return StartCoroutine(SwitchPage(currentPageIndex, currentPageIndex + 1));

            currentPageIndex++;
        }

        // Oxirgi sahifadan keyin
        if (PlayerPrefs.GetInt(Constants.Tutorial.GamePlay) != 1)
        {
            warmUpTutorial.gameObject.SetActive(true);
            yield return StartCoroutine(DisableAfterLastPage());
        }
        else
        {
            if (playerListTable != null)
            {
                playerListTable.gameObject.SetActive(true);
                yield return StartCoroutine(DisableAfterLastPage());
            }
        }

    }

    private void SetPageText(int index)
    {
        switch (index)
        {
            case 0:
                if (firstPageText != null)
                    firstPageText.text = LanguageManager.Instance.GetText(firstPageTextId);
                break;
            case 1:
                if (secondPageText != null)
                    secondPageText.text = LanguageManager.Instance.GetText(secondPageTextId);
                break;
            case 2:
                if (thirdPageText != null)
                    thirdPageText.text = LanguageManager.Instance.GetText(thirdPageTextId);
                break;
        }
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
            pages[i].transform.localScale = (i == index) ? Vector3.one : Vector3.zero;
        }
    }

    private IEnumerator SwitchPage(int hideIndex, int showIndex)
    {
        if (hideIndex < pages.Length && pages[hideIndex] != null)
        {
            yield return StartCoroutine(ScalePage(pages[hideIndex], 1f, 0f, 0.5f));
            pages[hideIndex].SetActive(false);
        }

        if (showIndex < pages.Length && pages[showIndex] != null)
        {
            pages[showIndex].SetActive(true);
            yield return StartCoroutine(ScalePage(pages[showIndex], 0f, 1f, 0.5f));
        }
    }

    private IEnumerator ScalePage(GameObject page, float startScale, float endScale, float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.one * startScale;
        Vector3 targetScale = Vector3.one * endScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            page.transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }

        page.transform.localScale = targetScale;
    }

    private IEnumerator DisableAfterLastPage()
    {
        if (pages[currentPageIndex - 1] != null)
        {
            yield return StartCoroutine(ScalePage(pages[currentPageIndex - 1], 1f, 0f, 0.5f));
            pages[currentPageIndex - 1].SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}
