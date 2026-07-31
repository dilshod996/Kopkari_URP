using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapCardScaler : MonoBehaviour
{
    public enum MapScalerType
    {
        Racing,
        Kopkari,
        Archery
    }

    [Header("Map Type")]
    [SerializeField] private MapScalerType mapType = MapScalerType.Racing;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private MapCard[] mapCards;
    [SerializeField] private float centeredAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0.45f;
    [SerializeField] private float alphaFadeDistance = 600f;
    [SerializeField] private float centeredScale = 1.04f;
    [SerializeField] private float hiddenScale = 0.9f;
    [SerializeField] private float scaleDistance = 600f;
    [SerializeField] private float centerScrollSpeed = 9f;
    [SerializeField] private float centerScrollMaxDuration = 0.55f;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TMP_Text closeText;

    [Header("Popup")]
    [SerializeField] private MapShowPopup popupMapInfo;

    private MapCard currentMainCard;
    private MapCard targetCard;
    private Coroutine centerRoutine;

    private void Start()
    {
        RefreshCards();
        UpdateCardVisuals();
    }

    private void OnEnable()
    {
        MapCard.OnMapSelected += HandleMapSelected;

        UITransilitions();

        if (closeBtn != null)
            closeBtn.onClick.AddListener(ClosePage);

        RefreshCards();
        RestorePreferredMainCard();
    }

    private void OnDisable()
    {
        MapCard.OnMapSelected -= HandleMapSelected;

        if (closeBtn != null)
            closeBtn.onClick.RemoveListener(ClosePage);

        StopCenterRoutine();
    }

    private void Update()
    {
        UpdateCardVisuals();
    }

    private void RefreshCards()
    {
        if (scrollRect != null && scrollRect.content != null)
            mapCards = scrollRect.content.GetComponentsInChildren<MapCard>();
    }

    private void UpdateCardVisuals()
    {
        if (scrollRect == null || scrollRect.viewport == null || mapCards == null || mapCards.Length == 0)
            return;

        Vector3 center = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        float fadeDistance = alphaFadeDistance > 0f ? alphaFadeDistance : scrollRect.viewport.rect.width * 0.5f;
        MapCard closestCard = null;
        float closestDistance = Mathf.Infinity;

        foreach (MapCard card in mapCards)
        {
            if (card == null)
                continue;

            float distance = Mathf.Abs(card.transform.position.x - center.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCard = card;
            }

            float alphaT = Mathf.Clamp01(distance / fadeDistance);
            float scaleT = Mathf.Clamp01(distance / GetScaleDistance());

            float alpha = Mathf.Lerp(centeredAlpha, hiddenAlpha, alphaT);
            float scale = Mathf.Lerp(centeredScale, hiddenScale, scaleT);

            card.SetScrollAlpha(alpha);
            card.SetScrollScale(scale);
        }

        if (targetCard == null && closestCard != null)
            currentMainCard = closestCard;
    }

    private float GetScaleDistance()
    {
        if (scaleDistance > 0f)
            return scaleDistance;

        return scrollRect.viewport.rect.width * 0.5f;
    }

    private void HandleMapSelected(MapCard selectedCard, MapCard.MapDetailsData data)
    {
        if (!MatchesScalerType(data.MapType))
            return;

        RefreshCards();
        UpdateCardVisuals();

        if (selectedCard == null)
            return;

        if (targetCard != null || selectedCard != currentMainCard)
        {
            CenterCard(selectedCard, true);
            return;
        }

        ShowMapDetails(data);
    }

    private void ShowMapDetails(MapCard.MapDetailsData data)
    {
        if (popupMapInfo == null)
            popupMapInfo = FindObjectOfType<MapShowPopup>(true);

        if (popupMapInfo == null)
            return;

        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
        HomeMainUI.Instance.ShowUI(popupMapInfo);
        popupMapInfo.SetMapData(data);
    }

    private void RestorePreferredMainCard()
    {
        StopCenterRoutine();

        if (!isActiveAndEnabled)
            return;

        centerRoutine = StartCoroutine(CenterPreferredMainCardNextFrame());
    }

    private IEnumerator CenterPreferredMainCardNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        RefreshCards();

        MapCard preferredCard = GetPreferredMainCard();
        if (preferredCard != null && scrollRect != null && scrollRect.content != null && scrollRect.viewport != null)
        {
            scrollRect.content.anchoredPosition = GetCenteredContentPosition(preferredCard);
            currentMainCard = preferredCard;
            targetCard = null;
            UpdateCardVisuals();
        }

        centerRoutine = null;
    }

    private MapCard GetPreferredMainCard()
    {
        if (mapCards == null || mapCards.Length == 0)
            return null;

        MapCard.MapType cardType = GetCardType();
        string preferredMapKey = MapCard.GetLastPlayedMap(cardType);

        MapCard preferredCard = FindCardByKey(preferredMapKey);
        if (preferredCard != null)
            return preferredCard;

        if (cardType == MapCard.MapType.Racing)
        {
            MapCard zarafshanCard = FindCardByKey(Constants.MapNames.Zarafshan);
            if (zarafshanCard != null)
                return zarafshanCard;
        }

        return mapCards[0];
    }

    private MapCard FindCardByKey(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey) || mapCards == null)
            return null;

        foreach (MapCard card in mapCards)
        {
            if (card != null &&
                string.Equals(card.MapKey, mapKey, System.StringComparison.Ordinal))
                return card;
        }

        return null;
    }

    private MapCard.MapType GetCardType()
    {
        switch (mapType)
        {
            case MapScalerType.Kopkari:
                return MapCard.MapType.Kopkari;
            case MapScalerType.Archery:
                return MapCard.MapType.Archery;
            default:
                return MapCard.MapType.Racing;
        }
    }

    private void CenterCard(MapCard card, bool animated)
    {
        if (card == null || scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            return;

        StopCenterRoutine();

        if (!animated)
        {
            scrollRect.content.anchoredPosition = GetCenteredContentPosition(card);
            currentMainCard = card;
            targetCard = null;
            UpdateCardVisuals();
            return;
        }

        centerRoutine = StartCoroutine(CenterCardRoutine(card));
    }

    private IEnumerator CenterCardRoutine(MapCard card)
    {
        targetCard = card;
        float elapsed = 0f;
        float previousDistance = Mathf.Infinity;
        int stuckFrames = 0;

        scrollRect.StopMovement();

        while (card != null && scrollRect != null && scrollRect.content != null && scrollRect.viewport != null)
        {
            float distance = GetDistanceFromCenter(card);
            float absDistance = Mathf.Abs(distance);
            Vector2 targetPosition = scrollRect.content.anchoredPosition + new Vector2(distance, 0f);

            float speed = Mathf.Max(0.01f, centerScrollSpeed);
            scrollRect.content.anchoredPosition = Vector2.Lerp(
                scrollRect.content.anchoredPosition,
                targetPosition,
                Time.unscaledDeltaTime * speed);

            if (absDistance < 1f)
                break;

            if (Mathf.Abs(previousDistance - absDistance) < 0.05f)
                stuckFrames++;
            else
                stuckFrames = 0;

            previousDistance = absDistance;
            elapsed += Time.unscaledDeltaTime;

            if (stuckFrames > 8 || elapsed >= centerScrollMaxDuration)
                break;

            yield return null;
        }

        if (card != null && scrollRect != null && scrollRect.content != null && scrollRect.viewport != null)
        {
            if (Mathf.Abs(GetDistanceFromCenter(card)) < 6f)
                scrollRect.content.anchoredPosition = GetCenteredContentPosition(card);

            scrollRect.StopMovement();
            currentMainCard = card;
        }

        UpdateCardVisuals();
        targetCard = null;
        centerRoutine = null;
    }

    private Vector2 GetCenteredContentPosition(MapCard card)
    {
        return scrollRect.content.anchoredPosition + new Vector2(GetDistanceFromCenter(card), 0f);
    }

    private float GetDistanceFromCenter(MapCard card)
    {
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Vector3 viewportCenter = scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);
        Vector3 cardCenter = cardRect != null
            ? card.transform.TransformPoint(cardRect.rect.center)
            : card.transform.position;

        return viewportCenter.x - cardCenter.x;
    }

    private void StopCenterRoutine()
    {
        if (centerRoutine != null)
        {
            StopCoroutine(centerRoutine);
            centerRoutine = null;
        }

        targetCard = null;
    }

    private bool MatchesScalerType(MapCard.MapType selectedMapType)
    {
        return (mapType == MapScalerType.Racing && selectedMapType == MapCard.MapType.Racing) ||
               (mapType == MapScalerType.Kopkari && selectedMapType == MapCard.MapType.Kopkari) ||
               (mapType == MapScalerType.Archery && selectedMapType == MapCard.MapType.Archery);
    }

    private void UITransilitions()
    {
        if (LanguageManager.Instance == null)
            return;

        if (titleText != null)
        {
            if (mapType == MapScalerType.Racing)
                titleText.text = LanguageManager.Instance.GetText(375);
            else if (mapType == MapScalerType.Kopkari)
                titleText.text = LanguageManager.Instance.GetText(482);
        }

        if (closeText != null)
            closeText.text = LanguageManager.Instance.GetText(362);
    }

    private void ClosePage()
    {
        gameObject.SetActive(false);
        HomeMainUI.Instance.OpenGameMainPanel();
    }
}
