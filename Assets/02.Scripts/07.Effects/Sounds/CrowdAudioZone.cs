using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class CrowdAudioZone : MonoBehaviour
{
    [Header("Crowd Audio")]
    [SerializeField] private AudioSource crowdSource;
    [SerializeField] private AudioClip crowdClip;
    [SerializeField, Range(0f, 1f)] private float crowdVolume = 0.65f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.6f;

    [Header("Listener Detection")]
    [SerializeField] private string listenerTag = "Player";
    [SerializeField] private bool acceptAudioListener = true;

    [Header("Room Music Ducking")]
    [SerializeField] private bool duckRoomMusic = true;
    [SerializeField, Range(0f, 1f)] private float roomVolumeMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float roomDuckFadeDuration = 0.5f;

    private readonly HashSet<int> _insideColliderIds = new();
    private bool _duckRequested;
    private bool _missingClipWarningShown;

    private void Reset()
    {
        crowdSource = GetComponent<AudioSource>();
        ConfigureSource();
    }

    private void Awake()
    {
        if (crowdSource == null)
            crowdSource = GetComponent<AudioSource>();

        ConfigureSource();
    }

    private void OnEnable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SoundSettingsChanged += HandleSoundSettingsChanged;
    }

    private void OnDisable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SoundSettingsChanged -= HandleSoundSettingsChanged;

        _insideColliderIds.Clear();
        ReleaseRoomDuck();
        StopCrowdImmediately();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsListenerCollider(other)) return;

        if (_insideColliderIds.Add(other.GetInstanceID()) && _insideColliderIds.Count == 1)
            ActivateZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_insideColliderIds.Remove(other.GetInstanceID())) return;

        if (_insideColliderIds.Count == 0)
            DeactivateZone();
    }

    private bool IsListenerCollider(Collider other)
    {
        if (other == null || other.isTrigger) return false;

        if (acceptAudioListener && other.GetComponentInParent<AudioListener>() != null)
            return true;

        if (string.IsNullOrWhiteSpace(listenerTag)) return false;

        Transform body = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        return other.CompareTag(listenerTag) ||
               body.CompareTag(listenerTag) ||
               body.root.CompareTag(listenerTag);
    }

    private void ActivateZone()
    {
        if (!IsSoundEnabled()) return;

        if (FadeCrowdIn())
            RequestRoomDuck();
    }

    private void DeactivateZone()
    {
        ReleaseRoomDuck();
        FadeCrowdOut();
    }

    private void HandleSoundSettingsChanged()
    {
        if (_insideColliderIds.Count == 0)
        {
            DeactivateZone();
            return;
        }

        if (IsSoundEnabled())
        {
            if (FadeCrowdIn())
                RequestRoomDuck();
        }
        else
        {
            ReleaseRoomDuck();
            StopCrowdImmediately();
        }
    }

    private bool FadeCrowdIn()
    {
        if (crowdSource == null || crowdClip == null)
        {
            if (!_missingClipWarningShown)
            {
                Debug.LogWarning($"{nameof(CrowdAudioZone)} on '{name}' needs a crowd AudioClip.", this);
                _missingClipWarningShown = true;
            }
            return false;
        }

        crowdSource.DOKill();
        crowdSource.clip = crowdClip;
        crowdSource.loop = true;

        if (!crowdSource.isPlaying)
        {
            crowdSource.volume = 0f;
            crowdSource.Play();
        }

        crowdSource.DOFade(GetCrowdTargetVolume(), fadeInDuration)
            .SetEase(Ease.OutQuad);

        return true;
    }

    private void FadeCrowdOut()
    {
        if (crowdSource == null || !crowdSource.isPlaying) return;

        crowdSource.DOKill();
        crowdSource.DOFade(0f, fadeOutDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                crowdSource.Stop();
                crowdSource.clip = null;
            });
    }

    private void StopCrowdImmediately()
    {
        if (crowdSource == null) return;

        crowdSource.DOKill();
        crowdSource.Stop();
        crowdSource.clip = null;
        crowdSource.volume = 0f;
    }

    private void RequestRoomDuck()
    {
        if (!duckRoomMusic || _duckRequested || SoundManager.Instance == null) return;

        SoundManager.Instance.RequestRoomDuck(this, roomVolumeMultiplier, roomDuckFadeDuration);
        _duckRequested = true;
    }

    private void ReleaseRoomDuck()
    {
        if (!_duckRequested) return;

        SoundManager.Instance?.ReleaseRoomDuck(this, roomDuckFadeDuration);
        _duckRequested = false;
    }

    private bool IsSoundEnabled()
    {
        return SoundManager.Instance != null
            ? SoundManager.Instance.SoundOn
            : PlayerPrefs.GetInt(SoundManager.PREF_SOUND_STATE, 1) == 1;
    }

    private float GetCrowdTargetVolume()
    {
        float masterVolume = SoundManager.Instance != null
            ? SoundManager.Instance.Volume01
            : PlayerPrefs.GetInt(SoundManager.PREF_SOUND_VOL_100, 100) / 100f;

        return crowdVolume * masterVolume;
    }

    private void ConfigureSource()
    {
        if (crowdSource == null) return;

        crowdSource.playOnAwake = false;
        crowdSource.loop = true;
        crowdSource.spatialBlend = 1f;
        crowdSource.volume = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (crowdSource == null)
            crowdSource = GetComponent<AudioSource>();

        ConfigureSource();
    }
#endif
}
