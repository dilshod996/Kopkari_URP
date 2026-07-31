using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapCard : MonoBehaviour
{
    private const string LastPlayedMapKeyPrefix = "LastPlayedMap_";

    public enum MapType
    {
        Kopkari,
        Racing,
        Archery
    }

    public enum MapWeather
    {
        Clear,
        Sunny,
        Cloudy,
        Rain,
        Dust,
        Lightning,
        Snow
    }

    public struct MapDetailsData
    {
        public MapType MapType;
        public SceneLoadManager.SceneType MovingRoom;
        public Sprite MapSprite;
        public int MapLangCode;
        public int MapInfoCode;
        public string MapKey;
        public int UnlockCost;
        public int PlayCost;
        public int NyufiyAmount;
        public int CoinAmount;
        public int Distance;
        public int RidersAmount;
        public MapWeather Weather;
        public Color BackgroundColor;
        public bool IsUnlocked;
    }

    public static event Action<MapCard, MapDetailsData> OnMapSelected;
    public string MapKey => mapLangName;

    public static void SaveLastPlayedMap(MapType mapType, string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        PlayerPrefs.SetString(GetLastPlayedMapPreferenceKey(mapType), mapKey.Trim());
        PlayerPrefs.Save();
    }

    public static string GetLastPlayedMap(MapType mapType)
    {
        return PlayerPrefs.GetString(GetLastPlayedMapPreferenceKey(mapType), string.Empty);
    }

    private static string GetLastPlayedMapPreferenceKey(MapType mapType)
    {
        return LastPlayedMapKeyPrefix + mapType;
    }

    [Header("Map Data")]
    [SerializeField] private MapType mapType;
    [SerializeField] private SceneLoadManager.SceneType movingRoom;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image mapMainBg;
    [SerializeField] private int mapLangCode = -1;
    [SerializeField] private string mapLangName = "MapName";
    [SerializeField] private int mapInfoCode = -1;
    [SerializeField] private int costMap;
    [SerializeField] private int playCost;
    [SerializeField] private int nyufiyAmount;
    [SerializeField] private int coinAmount;
    [SerializeField] private int distance;
    [SerializeField] private int ridersAmount;
    [SerializeField] private MapWeather weather = MapWeather.Clear;

    [Header("Card UI")]
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private Button playRoomBtn;
    [SerializeField] private GameObject lockObj;

    [Header("Unlock Settings")]
    [SerializeField] private bool isUnlocked = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        DataManager.OnMapUnlocked += HandleMapUnlocked;
        RefreshLockState();
        UpdateCardText();

        if (playRoomBtn != null)
            playRoomBtn.onClick.AddListener(SelectMap);
    }

    private void OnDisable()
    {
        DataManager.OnMapUnlocked -= HandleMapUnlocked;

        if (playRoomBtn != null)
            playRoomBtn.onClick.RemoveListener(SelectMap);
    }

    private void UpdateCardText()
    {
        if (mapNameText != null && LanguageManager.Instance != null)
            mapNameText.text = LanguageManager.Instance.GetText(mapLangCode);
    }

    private void SelectMap()
    {
        if (TryGetDetailsData(out MapDetailsData data))
            OnMapSelected?.Invoke(this, data);
    }

    public bool TryGetDetailsData(out MapDetailsData data)
    {
        RefreshLockState();

        data = new MapDetailsData
        {
            MapType = mapType,
            MovingRoom = movingRoom,
            MapSprite = GetPopupSprite(),
            MapLangCode = mapLangCode,
            MapInfoCode = mapInfoCode,
            MapKey = mapLangName,
            UnlockCost = costMap,
            PlayCost = playCost,
            NyufiyAmount = nyufiyAmount,
            CoinAmount = coinAmount,
            Distance = distance,
            RidersAmount = ridersAmount,
            Weather = weather,
            BackgroundColor = GetPopupBackgroundColor(),
            IsUnlocked = isUnlocked
        };

        return !string.IsNullOrWhiteSpace(mapLangName);
    }

    private Sprite GetPopupSprite()
    {
        return backgroundImage != null ? backgroundImage.sprite : null;
    }

    private Color GetPopupBackgroundColor()
    {
        return mapMainBg != null ? mapMainBg.color : Color.white;
    }

    public void SetScrollAlpha(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    public void SetScrollScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }

    private void RefreshLockState()
    {
        bool mapOpen = IsMapOpen();

        if (lockObj != null)
            lockObj.SetActive(!mapOpen);

        isUnlocked = mapOpen;
    }

    private bool IsMapOpen()
    {
        if (IsAlwaysOpenMap(mapLangName))
            return true;

        if (DataManager.Instance != null)
            return DataManager.Instance.IsMapUnlocked(mapLangName);

        int defaultValue = IsAlwaysOpenMap(mapLangName) ? 1 : 0;
        return PlayerPrefs.GetInt(mapLangName, defaultValue) == 1;
    }

    private static bool IsAlwaysOpenMap(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return false;

        string normalizedKey = mapKey.Trim();

        return normalizedKey == Constants.MapNames.RacingTraining ||
               normalizedKey == Constants.MapNames.Zarafshan ||
               normalizedKey == "TrainingRacing" ||
               normalizedKey == "FirstRacing" ||
               normalizedKey == "Training";
    }

    private void HandleMapUnlocked(string mapKey)
    {
        if (mapKey == mapLangName)
            RefreshLockState();
    }

    public void UnlockCard()
    {
        RefreshLockState();
    }
}
