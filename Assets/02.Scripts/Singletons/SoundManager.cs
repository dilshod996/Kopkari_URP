using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

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

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
        LoadFromPrefsAndApply();
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
            roomSource.volume = SoundOn ? Volume01 : 0f;

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
    }

    // Slider 0..100
    public void SetVolume100(int value100)
    {
        value100 = Mathf.Clamp(value100, 0, 100);
        Volume01 = value100 / 100f;

        PlayerPrefs.SetInt(PREF_SOUND_VOL_100, value100);
        PlayerPrefs.Save();

        ApplySoundState();
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
            return; // preload qilinmagan bo'lsa - jim

        uiSource.pitch = Random.Range(0.97f, 1.03f); // premium micro-variation
        uiSource.PlayOneShot(clip, Volume01);
    }

    // Xohlasangiz to'g'ridan-to'g'ri clip bilan ham UI chalish:
    public void PlayUIClip(AudioClip clip)
    {
        if (!SoundOn || uiSource == null || clip == null) return;

        uiSource.pitch = Random.Range(0.97f, 1.03f);
        uiSource.PlayOneShot(clip, Volume01);
    }

    // ---------------- Room Play (AudioClip argument) ----------------
    // Addressablesdan load bo'lib kelgan ambience clipni bevosita bering:
    public void PlayRoom(AudioClip clip, bool restartIfSame = false, float fadeInDuration = 0.5f)
    {
        if (!SoundOn || roomSource == null || clip == null) return;

        if (!restartIfSame && roomSource.clip == clip && roomSource.isPlaying)
            return;

        // Old fade'lar bo'lsa to'xtatamiz (volume tween conflict bo'lmasin)
        roomSource.DOKill();

        roomSource.clip = clip;
        roomSource.loop = true;

        // 0 dan boshlatamiz, keyin sekin ko'taramiz
        roomSource.volume = 0f;
        roomSource.Play();

        roomSource.DOFade(Volume01, fadeInDuration)
            .SetEase(Ease.OutQuad);
    }
    public void StopRoomSmooth(float fadeDuration = 0.5f)
    {
        if (roomSource == null) return;
        if (!roomSource.isPlaying) return;

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
        roomSource.Stop();
        roomSource.clip = null;
    }
}
