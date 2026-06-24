using UnityEngine;
using Lofelt.NiceVibrations;

public enum HomeHapticId
{
    LowCondition,
    NotEnoughMoney,
    Success,
    Selection,
    ItemPickup,
    BoosterUse,
    PlayerHit,
    RaceFinish
}

public class HomeHapticsManager : MonoBehaviour
{
    public static HomeHapticsManager Instance { get; private set; }

    [Header("Receiver (MainCamera)")]
    [SerializeField] private HapticReceiver receiver;

    [Header("Sources")]
    [SerializeField] private HapticSource lowSource;
    [SerializeField] private HapticSource denySource;
    [SerializeField] private HapticSource successSource;

    [Header("Tuning")]
    [Range(0f, 2f)][SerializeField] private float lowLevel = 0.6f;
    [Range(0f, 2f)][SerializeField] private float denyLevel = 1f;
    [Range(0f, 2f)][SerializeField] private float successLevel = 1.2f;

    [Range(-1f, 1f)][SerializeField] private float lowTone = -0.4f;
    [Range(-1f, 1f)][SerializeField] private float denyTone = -0.1f;
    [Range(-1f, 1f)][SerializeField] private float successTone = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[HomeHaptics] Duplicate manager found.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (receiver == null)
        {
            Debug.LogError("[HomeHaptics] HapticReceiver assign qilinmagan.");
        }
    }

    public void Play(HomeHapticId id)
    {
        if (receiver == null || !receiver.hapticsEnabled)
            return;

        switch (id)
        {
            case HomeHapticId.LowCondition:
                PlaySource(lowSource, lowLevel, lowTone);
                break;

            case HomeHapticId.NotEnoughMoney:
                PlaySource(denySource, denyLevel, denyTone);
                break;

            case HomeHapticId.Success:
                PlaySource(successSource, successLevel, successTone);
                break;

            case HomeHapticId.Selection:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
                break;

            case HomeHapticId.ItemPickup:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
                break;

            case HomeHapticId.BoosterUse:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
                break;

            case HomeHapticId.PlayerHit:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
                break;

            case HomeHapticId.RaceFinish:
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
                break;
        }
    }

    private void PlaySource(HapticSource src, float level, float tone)
    {
        if (src == null) return;

        src.level = level;
        src.frequencyShift = tone;
        src.Play();
    }
}
