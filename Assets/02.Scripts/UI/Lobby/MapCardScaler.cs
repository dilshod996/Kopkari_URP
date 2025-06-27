using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapCardScaler : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float lerpSpeed = 5f;

    public MapCard[] mapCards;
    private MapCard targetCard = null;
    private bool isScrollingToCard = false;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;


    void Start()
    {
        //mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();

        foreach (var card in mapCards)
        {
            card.Initialize(this);
        }

        StartCoroutine(CenterCardAfterFrame());
    }

    void Update()
    {
        // New Input System — foydalanuvchi ekranga tegayotganini aniqlash
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed ||
            Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isScrollingToCard = false;
            targetCard = null;
        }

        HandleSmoothScroll();
        UpdateMainCardScalingAndShadow();
    }
    void HandleSmoothScroll()
    {
        if (isScrollingToCard && targetCard != null)
        {
            Vector3 center = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
            Vector3 cardCenter = targetCard.transform.TransformPoint(targetCard.GetComponent<RectTransform>().rect.center);

            float distance = center.x - cardCenter.x;
            Vector2 newPos = scrollRect.content.anchoredPosition + new Vector2(distance, 0);

            scrollRect.content.anchoredPosition = Vector2.Lerp(scrollRect.content.anchoredPosition, newPos, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(distance) < 1f)
            {
                isScrollingToCard = false;
                targetCard = null;
            }
        }
    }

    void UpdateMainCardScalingAndShadow()
    {
        Vector3 center = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        MapCard closestCard = null;
        float closestDistance = Mathf.Infinity;

        foreach (var card in mapCards)
        {
            float distance = Mathf.Abs(card.transform.position.x - center.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCard = card;
            }
        }

        foreach (var card in mapCards)
        {
            card.SetAsMain(card == closestCard);
        }
    }

    public void ScrollToCard(MapCard card)
    {
        targetCard = card;
        isScrollingToCard = true;
    }

    IEnumerator CenterCardAfterFrame()
    {
        yield return null;
        mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();

        if (mapCards.Length >= 2)
        {
            ScrollToCard(mapCards[1]); // center second card
        }
    }

    private void OnEnable()
    {
        titleText.text = LanguageManager.Instance.GetText(46);
        //firstRoomName.text = LanguageManager.Instance.GetText(47);
        //secondRoomName.text = LanguageManager.Instance.GetText(48);
        //thirdRoomName.text = LanguageManager.Instance.GetText(49);
        //fourthRoomName.text = LanguageManager.Instance.GetText(50);
        //fifthRoomName.text = LanguageManager.Instance.GetText(51);
        //sixthRoomName.text = LanguageManager.Instance.GetText(52);
        //seventhRoomName.text = LanguageManager.Instance.GetText(53);
        //eighthRoomName.text = LanguageManager.Instance.GetText(54);
        //ninthRoomName.text = LanguageManager.Instance.GetText(55);
        //tenthRoomName.text = LanguageManager.Instance.GetText(56);
        //eleventhRoomName.text = LanguageManager.Instance.GetText(57);
    }
    void OnDisable()
    {
        mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();
        if (mapCards.Length >= 2)
        {
            float total = mapCards.Length - 1;
            float normalizedPos = Mathf.Clamp01(1f / total);
            scrollRect.horizontalNormalizedPosition = normalizedPos;
        }
    }
}
