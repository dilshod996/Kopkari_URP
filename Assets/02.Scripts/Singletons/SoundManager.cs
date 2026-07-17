using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum UISoundType
{
    Click, Select, Confirm, Success, Error, PopupOpen, PopupClose
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    // PlayerPrefs keys
    public const string PREF_SOUND_STATE = "SoundState";     // 1=on, 0=off
    public const string PREF_SOUND_VOL_100 = "SoundVolume";  // 0..100

    [Header("Sources (2D)")]
    [SerializeField] private AudioSource uiSource;    // one-shots
    [SerializeField] private AudioSource roomSource;  // loop ambience

    // Runtime-loaded clips (Addressables preload -> register here)
    private readonly Dictionary<UISoundType, AudioClip> _uiClips = new();

    // State
    public bool SoundOn { get; private set; } = true;
    public float Volume01 { get; private set; } = 1f; // 0..1
    public event System.Action SoundSettingsChanged;

    private int _sceneSoundRequestVersion;
    private readonly Dictionary<int, float> _roomDuckRequests = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
        LoadFromPrefsAndApply();
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    private void Start()
    {
        RefreshSceneRoomSound(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        _sceneSoundRequestVersion++;
        uiSource?.DOKill();
        roomSource?.DOKill();
        Instance = null;
    }

    private void SetupSources()
    {
        if (uiSource != null)
        {
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
        }

        if (roomSource != null)
        {
            roomSource.playOnAwake = false;
            roomSource.loop = true;
            roomSource.spatialBlend = 0f;
        }
    }

    private void LoadFromPrefsAndApply()
    {
        int state = PlayerPrefs.GetInt(PREF_SOUND_STATE, 1);
        int vol100 = PlayerPrefs.GetInt(PREF_SOUND_VOL_100, 100);

        SoundOn = (state == 1);
        Volume01 = Mathf.Clamp01(vol100 / 100f);

        ApplySoundState();
    }

    private void ApplySoundState()
    {
        if (uiSource != null) uiSource.volume = SoundOn ? Volume01 : 0f;

        if (roomSource != null)
        {
            roomSource.DOKill();
            roomSource.volume = GetRoomTargetVolume();

            // OFF bo'lsa - umuman to'xtasin
            if (!SoundOn)
            {
                roomSource.Stop();
                roomSource.clip = null;
            }
        }
    }

    // ---------------- Settings API ----------------

    public void SetSoundState(bool on)
    {
        SoundOn = on;
        PlayerPrefs.SetInt(PREF_SOUND_STATE, on ? 1 : 0);
        PlayerPrefs.Save();

        ApplySoundState();

        if (on)
            RefreshSceneRoomSound(SceneManager.GetActiveScene());

        SoundSettingsChanged?.Invoke();
    }

    // Slider 0..100
    public void SetVolume100(int value100)
    {
        value100 = Mathf.Clamp(value100, 0, 100);
        Volume01 = value100 / 100f;

        PlayerPrefs.SetInt(PREF_SOUND_VOL_100, value100);
        PlayerPrefs.Save();

        ApplySoundState();
        SoundSettingsChanged?.Invoke();
    }

    // ---------------- Addressables preload -> register ----------------
    // UI clips preload bo'lib kelgach shuni chaqirasiz:
    public void RegisterUIClip(UISoundType type, AudioClip clip)
    {
        if (clip == null) return;
        _uiClips[type] = clip;
    }

    // Agar scene almashganda UI clipsni tozalash kerak bo'lsa:
    public void ClearUIClips()
    {
        _uiClips.Clear();
    }

    // ---------------- UI Play ----------------
    public void PlayUI(UISoundType type)
    {
        if (!SoundOn || uiSource == null) return;

        if (!_uiClips.TryGetValue(type, out var clip) || clip == null)
        {
            // Select has no dedicated Addressable yet, so use Click instead of silence.
            if (type != UISoundType.Select ||
                !_uiClips.TryGetValue(UISoundType.Click, out clip) || clip == null)
                return;
        }

        uiSource.pitch = Random.Range(0.97f, 1.03f); // premium micro-variation
        uiSource.PlayOneShot(clip);
    }

    // Xohlasangiz to'g'ridan-to'g'ri clip bilan ham UI chalish:
    public void PlayUIClip(AudioClip clip)
    {
        if (!SoundOn || uiSource == null || clip == null) return;

        uiSource.pitch = Random.Range(0.97f, 1.03f);
        uiSource.PlayOneShot(clip);
    }

    // ---------------- Scene Room Sound ----------------

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshSceneRoomSound(newScene);
    }

    public void PrepareForSceneChange()
    {
        // Prevent a load requested by the old scene from completing during transition.
        _sceneSoundRequestVersion++;
        StopRoomSmooth(force: true);
    }

    private void RefreshSceneRoomSound(Scene scene)
    {
        int requestVersion = ++_sceneSoundRequestVersion;
        _roomDuckRequests.Clear();

        // A scene with no mapping, or a failed load, must remain silent.
        StopRoom();

        if (!SoundOn || !scene.IsValid()) return;

        string address = GetRoomSoundAddress(scene.name);
        if (string.IsNullOrEmpty(address)) return;

        LoadAndPlaySceneRoomSound(address, scene.name, requestVersion);
    }

    private async void LoadAndPlaySceneRoomSound(string address, string sceneName, int requestVersion)
    {
        // Singleton creation order is not guaranteed in the first scene.
        while (AddressablesService.Instance == null)
        {
            if (this == null || requestVersion != _sceneSoundRequestVersion)
                return;

            await System.Threading.Tasks.Task.Yield();
        }

        AudioClip clip = await AddressablesService.Instance.LoadAssetAsync<AudioClip>(address);

        if (this == null ||
            requestVersion != _sceneSoundRequestVersion ||
            !SoundOn ||
            SceneManager.GetActiveScene().name != sceneName)
            return;

        if (clip == null)
        {
            StopRoom();
            Debug.LogWarning($"No room sound was loaded for scene '{sceneName}' (address: '{address}').");
            return;
        }

        PlayRoom(clip);
    }

    private static string GetRoomSoundAddress(string sceneName)
    {
        switch (sceneName)
        {
            case "Intro":
                return Constants.RoomSound.IntroSound;
            case "Home":
            case "Lobby":
                return Constants.RoomSound.HomeRoomSound;
            case "AvatarCustom":
                return "CustomRoomSound";
            case "FirstRacing":
            case "TrainingRacing":
            case "SecondRacing":
            case "EgyptRacing":
            case "Kansas":
            case "Sibir":
                return Constants.RoomSound.RacingSound;
            default:
                return null;
        }
    }

    // ---------------- Room Play (AudioClip argument) ----------------
    // Addressablesdan load bo'lib kelgan ambience clipni bevosita bering:
    public void PlayRoom(AudioClip clip, bool restartIfSame = false, float fadeInDuration = 0.5f)
    {
        if (roomSource == null) return;
        if (clip == null)
        {
            StopRoom();
            return;
        }
        if (!SoundOn) return;

        if (!restartIfSame && roomSource.clip == clip && roomSource.isPlaying)
            return;

        // Old fade'lar bo'lsa to'xtatamiz (volume tween conflict bo'lmasin)
        roomSource.DOKill();

        roomSource.clip = clip;
        roomSource.loop = true;

        // 0 dan boshlatamiz, keyin sekin ko'taramiz
        roomSource.volume = 0f;
        roomSource.Play();

        roomSource.DOFade(GetRoomTargetVolume(), fadeInDuration)
            .SetEase(Ease.OutQuad);
    }

    // ---------------- Room Ducking ----------------

    public void RequestRoomDuck(Object requester, float volumeMultiplier = 0.45f, float fadeDuration = 0.5f)
    {
        if (requester == null) return;

        _roomDuckRequests[requester.GetInstanceID()] = Mathf.Clamp01(volumeMultiplier);
        RefreshRoomDuck(fadeDuration);
    }

    public void ReleaseRoomDuck(Object requester, float fadeDuration = 0.5f)
    {
        if (requester == null) return;

        _roomDuckRequests.Remove(requester.GetInstanceID());
        RefreshRoomDuck(fadeDuration);
    }

    private void RefreshRoomDuck(float fadeDuration)
    {
        if (roomSource == null || !roomSource.isPlaying) return;

        roomSource.DOKill();
        roomSource.DOFade(GetRoomTargetVolume(), Mathf.Max(0f, fadeDuration))
            .SetEase(Ease.OutQuad);
    }

    private float GetRoomTargetVolume()
    {
        if (!SoundOn) return 0f;

        float multiplier = 1f;
        foreach (float requestedMultiplier in _roomDuckRequests.Values)
            multiplier = Mathf.Min(multiplier, requestedMultiplier);

        return Volume01 * multiplier;
    }

    public void StopRoomSmooth(float fadeDuration = 0.5f, bool force = false)
    {
        if (roomSource == null) return;
        if (!roomSource.isPlaying) return;

        // Generic overlay panels call this method too. Do not let an ordinary
        // popup stop the active scene's mapped background sound.
        string activeSceneSound = GetRoomSoundAddress(SceneManager.GetActiveScene().name);
        bool sceneMoveInProgress = SceneLoadManager.Instance != null && SceneLoadManager.Instance.IsSceneLoading;
        if (!force && !sceneMoveInProgress && !string.IsNullOrEmpty(activeSceneSound))
            return;

        roomSource.DOKill();
        float startVolume = roomSource.volume;

        roomSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                roomSource.Stop();
                roomSource.clip = null;
                roomSource.volume = startVolume; // keyingi play uchun tiklab qo'yamiz
            });
    }
    public void StopRoom()
    {
        if (roomSource == null) return;
        roomSource.DOKill();
        roomSource.Stop();
        roomSource.clip = null;
        roomSource.volume = GetRoomTargetVolume();
    }
}
