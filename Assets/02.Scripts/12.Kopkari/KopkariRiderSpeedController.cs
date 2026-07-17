using System;
using System.Collections.Generic;
using MalbersAnimations.Controller;
using UnityEngine;

/// <summary>
/// Resolves the configured Kopkari rider state and applies it to one horse.
/// The JSON is loaded once; state changes are event-driven and do not poll in Update.
/// </summary>
public static class KopkariRiderSpeedController
{
    private const string ResourcePath = "Kopkari/RiderStateSpeed";

    private const string AiCompetitorDefault = "ai_competitor_default";
    private const string AiCompetitorCarrier = "ai_competitor_carrier";
    private const string AiCarrierEscapeBoost = "ai_carrier_escape_boost";
    private const string AiMainRivalDefault = "ai_main_rival_default";
    private const string AiMainRivalCarrier = "ai_main_rival_carrier";
    private const string AiGuard = "ai_guard";
    private const string AiTrapSetter = "ai_trap_setter";
    private const string PlayerDefault = "player_default";
    private const string PlayerCarrier = "player_carrier";
    private const string PlayerSprint = "player_sprint";
    private const string PlayerCarrierSprint = "player_carrier_sprint";

    [Serializable]
    private sealed class SpeedConfig
    {
        public RiderSpeedState[] states;
    }

    [Serializable]
    private sealed class RiderSpeedState
    {
        public string id;
        public int speedIndex;
        public float speedMultiplier = 1f;
    }

    private static readonly Dictionary<string, RiderSpeedState> States =
        new Dictionary<string, RiderSpeedState>(StringComparer.Ordinal);

    private static bool loaded;
    private static bool missingConfigReported;
    private static readonly HashSet<string> MissingStateWarnings = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache()
    {
        States.Clear();
        MissingStateWarnings.Clear();
        loaded = false;
        missingConfigReported = false;
    }

    public static void ApplyAI(
        MAnimal animal,
        AIKopkariRider.UlakRole role,
        bool isMainRival,
        bool isCarrier)
    {
        string stateId;
        switch (role)
        {
            case AIKopkariRider.UlakRole.Guard:
                stateId = AiGuard;
                break;
            case AIKopkariRider.UlakRole.TrapSetter:
                stateId = AiTrapSetter;
                break;
            default:
                stateId = isMainRival
                    ? (isCarrier ? AiMainRivalCarrier : AiMainRivalDefault)
                    : (isCarrier ? AiCompetitorCarrier : AiCompetitorDefault);
                break;
        }

        Apply(animal, stateId, GetFallbackAI(role, isMainRival, isCarrier));
    }

    public static void ApplyPlayer(MAnimal animal, bool isCarrier, bool isSprinting)
    {
        string stateId = isSprinting
            ? (isCarrier ? PlayerCarrierSprint : PlayerSprint)
            : (isCarrier ? PlayerCarrier : PlayerDefault);

        int fallbackIndex = isCarrier
            ? (isSprinting ? 5 : 4)
            : (isSprinting ? 6 : 5);
        Apply(animal, stateId, new RiderSpeedState
        {
            id = stateId,
            speedIndex = fallbackIndex,
            speedMultiplier = 1f
        });
    }

    public static void ApplyAICarrierEscapeBoost(MAnimal animal)
    {
        Apply(animal, AiCarrierEscapeBoost, new RiderSpeedState
        {
            id = AiCarrierEscapeBoost,
            speedIndex = 6,
            speedMultiplier = 1f
        });
    }

    public static void RestoreUnmodifiedSpeed(MAnimal animal)
    {
        animal?.SetKopkariMovementSpeedMultiplier(1f);
    }

    private static RiderSpeedState GetFallbackAI(
        AIKopkariRider.UlakRole role,
        bool isMainRival,
        bool isCarrier)
    {
        int speedIndex;
        if (role == AIKopkariRider.UlakRole.Guard)
            speedIndex = 6;
        else if (role == AIKopkariRider.UlakRole.TrapSetter)
            speedIndex = 7;
        else if (isCarrier)
            speedIndex = 5;
        else if (isMainRival)
            speedIndex = 7;
        else
            speedIndex = 6;

        return new RiderSpeedState
        {
            speedIndex = speedIndex,
            speedMultiplier = 1f
        };
    }

    private static void Apply(MAnimal animal, string stateId, RiderSpeedState fallback)
    {
        if (animal == null)
            return;

        EnsureLoaded();
        RiderSpeedState state;
        if (!States.TryGetValue(stateId, out state) || state == null)
        {
            state = fallback;
            if (MissingStateWarnings.Add(stateId))
                Debug.LogWarning(
                    $"[{nameof(KopkariRiderSpeedController)}] Missing state '{stateId}'. Using built-in defaults.");
        }

        int requestedIndex = Mathf.Max(1, state.speedIndex);
        animal.SetKopkariMovementSpeedMultiplier(state.speedMultiplier);
        animal.Speed_CurrentIndex_Set(requestedIndex);
    }

    private static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            ReportMissingConfig();
            return;
        }

        try
        {
            SpeedConfig config = JsonUtility.FromJson<SpeedConfig>(asset.text);
            if (config?.states == null)
            {
                ReportMissingConfig();
                return;
            }

            for (int i = 0; i < config.states.Length; i++)
            {
                RiderSpeedState state = config.states[i];
                if (state != null && !string.IsNullOrWhiteSpace(state.id))
                    States[state.id] = state;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[{nameof(KopkariRiderSpeedController)}] Could not parse Resources/{ResourcePath}.json. " +
                $"Built-in defaults will be used. {exception.Message}");
        }
    }

    private static void ReportMissingConfig()
    {
        if (missingConfigReported)
            return;

        missingConfigReported = true;
        Debug.LogWarning(
            $"[{nameof(KopkariRiderSpeedController)}] Resources/{ResourcePath}.json was not found or has no states. " +
            "Built-in defaults will be used.");
    }
}
