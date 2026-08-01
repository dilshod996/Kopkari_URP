using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public struct HorseConditionStats
{
    public float Power;
    public float Cooling;
    public float Stamina;

    public HorseConditionStats(float power, float cooling, float stamina)
    {
        Power = power;
        Cooling = cooling;
        Stamina = stamina;
    }
}

public static class HorseConditionStatsService
{
    private const float DefaultMaxValue = 100f;
    private const float PowerRegenMinutes = 240f;
    private const float CoolingRegenMinutes = 300f;
    private const float StaminaRegenMinutes = 180f;
    private const string DefaultHorseId = "horse_01";
    private const string ActiveHorseIdKey = "ActiveHorseId";
    private const string BodySlotId = "Body";
    private const string MissingBodyId = "default_body";
    private const string PerBodyMigrationKey = "HorseConditionPerBodyMigrated_v1";

    public static event Action<HorseConditionStats> ConditionChanged;

    public static HorseConditionStats DefaultMax =>
        new HorseConditionStats(DefaultMaxValue, DefaultMaxValue, DefaultMaxValue);

    public static string ActiveHorseId =>
        PlayerPrefs.GetString(ActiveHorseIdKey, DefaultHorseId);

    public static HorseConditionStats GetCachedMaxOrDefault(string horseId = null, string bodyOptionId = null)
    {
        ResolveProfile(ref horseId, ref bodyOptionId);

        if (!PlayerPrefs.HasKey(MaxPowerKey(horseId, bodyOptionId)) ||
            !PlayerPrefs.HasKey(MaxCoolingKey(horseId, bodyOptionId)) ||
            !PlayerPrefs.HasKey(MaxStaminaKey(horseId, bodyOptionId)))
        {
            // Preserve the old active-horse maximum during the one-time migration.
            if (PlayerPrefs.GetInt(PerBodyMigrationKey, 0) == 0 &&
                PlayerPrefs.HasKey(LegacyMaxPowerKey(horseId)) &&
                PlayerPrefs.HasKey(LegacyMaxCoolingKey(horseId)) &&
                PlayerPrefs.HasKey(LegacyMaxStaminaKey(horseId)))
            {
                return new HorseConditionStats(
                    PlayerPrefs.GetFloat(LegacyMaxPowerKey(horseId), DefaultMaxValue),
                    PlayerPrefs.GetFloat(LegacyMaxCoolingKey(horseId), DefaultMaxValue),
                    PlayerPrefs.GetFloat(LegacyMaxStaminaKey(horseId), DefaultMaxValue));
            }

            return DefaultMax;
        }

        return new HorseConditionStats(
            PlayerPrefs.GetFloat(MaxPowerKey(horseId, bodyOptionId), DefaultMaxValue),
            PlayerPrefs.GetFloat(MaxCoolingKey(horseId, bodyOptionId), DefaultMaxValue),
            PlayerPrefs.GetFloat(MaxStaminaKey(horseId, bodyOptionId), DefaultMaxValue));
    }

    public static async Task<HorseConditionStats> GetActiveMaxAsync()
    {
        string horseId = ActiveHorseId;
        string bodyOptionId = PlayerPrefs.GetString(SelectionKey(horseId, BodySlotId), "");

        if (string.IsNullOrWhiteSpace(bodyOptionId))
        {
            PlayerCatalogProvider provider = PlayerCatalogProvider.Instance;
            if (provider == null) return GetCachedMaxOrDefault(horseId, bodyOptionId);

            await provider.EnsureCatalogAsync();
            bodyOptionId = provider.GetDefaultOptionId(horseId, BodySlotId);

            // Keep every later condition read on the same profile as the catalog default.
            if (!string.IsNullOrWhiteSpace(bodyOptionId))
            {
                PlayerPrefs.SetString(SelectionKey(horseId, BodySlotId), bodyOptionId);
                PlayerPrefs.Save();
            }
        }

        return await GetBodyMaxAsync(horseId, bodyOptionId);
    }

    public static async Task<HorseConditionStats> GetBodyMaxAsync(string horseId, string bodyOptionId)
    {
        horseId = NormalizeHorseId(horseId);
        bodyOptionId = NormalizeBodyId(bodyOptionId);

        try
        {
            PlayerCatalogProvider provider = PlayerCatalogProvider.Instance;
            if (provider == null || bodyOptionId == MissingBodyId)
                return GetCachedMaxOrDefault(horseId, bodyOptionId);

            CatalogEntry entry = await provider.FindAsync(horseId, BodySlotId, bodyOptionId);
            if (entry == null)
                return GetCachedMaxOrDefault(horseId, bodyOptionId);

            HorseConditionStats max = FromCatalogEntry(entry);
            CacheMax(horseId, bodyOptionId, max);
            return max;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to read horse body max stats: {ex.Message}");
            return GetCachedMaxOrDefault(horseId, bodyOptionId);
        }
    }

    public static async Task<HorseConditionStats> RefreshActiveConditionAsync()
    {
        string horseId = ActiveHorseId;
        string bodyOptionId = PlayerPrefs.GetString(SelectionKey(horseId, BodySlotId), "");
        HorseConditionStats max = await GetActiveMaxAsync();

        // GetActiveMaxAsync may have resolved and saved the catalog's default body.
        bodyOptionId = PlayerPrefs.GetString(SelectionKey(horseId, BodySlotId), bodyOptionId);

        // Ignore a late async result if the player selected another horse meanwhile.
        if (!IsActiveProfile(horseId, bodyOptionId))
            return await RefreshActiveConditionAsync();

        HorseConditionStats current = ApplyRecoveryNow(max, horseId, bodyOptionId);
        ConditionChanged?.Invoke(current);
        return current;
    }

    public static async Task SyncSelectedBodyMaxAsync(string horseId, string bodyOptionId)
    {
        HorseConditionStats max = await GetBodyMaxAsync(horseId, bodyOptionId);

        if (!IsActiveProfile(horseId, bodyOptionId))
            return;

        HorseConditionStats current = ApplyRecoveryNow(max, horseId, bodyOptionId);
        ConditionChanged?.Invoke(current);
    }

    public static HorseConditionStats GetCurrentOrInitialize(
        HorseConditionStats max,
        string horseId = null,
        string bodyOptionId = null)
    {
        ResolveProfile(ref horseId, ref bodyOptionId);
        MigrateLegacyConditionIfNeeded(horseId, bodyOptionId, max);

        bool changed = false;
        float power = GetOrInit(CurrentPowerKey(horseId, bodyOptionId), max.Power, ref changed);
        float cooling = GetOrInit(CurrentCoolingKey(horseId, bodyOptionId), max.Cooling, ref changed);
        float stamina = GetOrInit(CurrentStaminaKey(horseId, bodyOptionId), max.Stamina, ref changed);
        HorseConditionStats clamped = Clamp(new HorseConditionStats(power, cooling, stamina), max);

        if (!Mathf.Approximately(clamped.Power, power) ||
            !Mathf.Approximately(clamped.Cooling, cooling) ||
            !Mathf.Approximately(clamped.Stamina, stamina))
        {
            changed = true;
            SaveCurrent(clamped, false, horseId, bodyOptionId);
        }

        if (changed) PlayerPrefs.Save();
        return clamped;
    }

    public static HorseConditionStats AddFood(float power, float cooling, float stamina)
    {
        HorseConditionStats max = GetCachedMaxOrDefault();
        HorseConditionStats current = GetCurrentOrInitialize(max);
        return AddFood(power, cooling, stamina, current);
    }

    public static HorseConditionStats AddFood(float power, float cooling, float stamina, HorseConditionStats current)
    {
        HorseConditionStats max = GetCachedMaxOrDefault();
        HorseConditionStats updated = Clamp(
            new HorseConditionStats(
                current.Power + power,
                current.Cooling + cooling,
                current.Stamina + stamina),
            max);

        SaveCurrent(updated);
        return updated;
    }

    public static HorseConditionStats ApplyOfflineRegen(
        HorseConditionStats current,
        float elapsedMinutes,
        float powerRegenMinutes,
        float coolingRegenMinutes,
        float staminaRegenMinutes)
    {
        HorseConditionStats max = GetCachedMaxOrDefault();
        return ApplyOfflineRegen(current, max, elapsedMinutes,
            powerRegenMinutes, coolingRegenMinutes, staminaRegenMinutes);
    }

    public static HorseConditionStats EnsureCurrentWithinMax(HorseConditionStats max)
    {
        HorseConditionStats current = GetCurrentOrInitialize(max);
        HorseConditionStats clamped = Clamp(current, max);
        SaveCurrent(clamped);
        return clamped;
    }

    public static HorseConditionStats Clamp(HorseConditionStats value, HorseConditionStats max)
    {
        return new HorseConditionStats(
            Mathf.Clamp(value.Power, 0f, max.Power),
            Mathf.Clamp(value.Cooling, 0f, max.Cooling),
            Mathf.Clamp(value.Stamina, 0f, max.Stamina));
    }

    public static void SaveCurrent(
        HorseConditionStats stats,
        bool saveNow = true,
        string horseId = null,
        string bodyOptionId = null)
    {
        ResolveProfile(ref horseId, ref bodyOptionId);
        PlayerPrefs.SetFloat(CurrentPowerKey(horseId, bodyOptionId), stats.Power);
        PlayerPrefs.SetFloat(CurrentCoolingKey(horseId, bodyOptionId), stats.Cooling);
        PlayerPrefs.SetFloat(CurrentStaminaKey(horseId, bodyOptionId), stats.Stamina);

        if (saveNow) PlayerPrefs.Save();
    }

    private static HorseConditionStats ApplyRecoveryNow(
        HorseConditionStats max,
        string horseId,
        string bodyOptionId)
    {
        HorseConditionStats current = GetCurrentOrInitialize(max, horseId, bodyOptionId);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string timeKey = LastUpdateTimeKey(horseId, bodyOptionId);
        string raw = PlayerPrefs.GetString(timeKey, "");

        if (!DateTimeOffset.TryParse(raw, null, DateTimeStyles.RoundtripKind, out DateTimeOffset lastTime))
            lastTime = now;

        float elapsedMinutes = Mathf.Max(0f, (float)(now - lastTime).TotalMinutes);
        HorseConditionStats updated = ApplyOfflineRegen(
            current, max, elapsedMinutes,
            PowerRegenMinutes, CoolingRegenMinutes, StaminaRegenMinutes);

        SaveCurrent(updated, false, horseId, bodyOptionId);
        PlayerPrefs.SetString(timeKey, now.ToString("O"));
        PlayerPrefs.Save();
        return updated;
    }

    private static HorseConditionStats ApplyOfflineRegen(
        HorseConditionStats current,
        HorseConditionStats max,
        float elapsedMinutes,
        float powerRegenMinutes,
        float coolingRegenMinutes,
        float staminaRegenMinutes)
    {
        HorseConditionStats updated = new HorseConditionStats(
            current.Power + (max.Power / powerRegenMinutes * elapsedMinutes),
            current.Cooling + (max.Cooling / coolingRegenMinutes * elapsedMinutes),
            current.Stamina + (max.Stamina / staminaRegenMinutes * elapsedMinutes));

        return Clamp(updated, max);
    }

    private static void MigrateLegacyConditionIfNeeded(
        string horseId,
        string bodyOptionId,
        HorseConditionStats max)
    {
        string powerKey = CurrentPowerKey(horseId, bodyOptionId);
        if (PlayerPrefs.HasKey(powerKey))
            return;

        bool canMigrateLegacy = PlayerPrefs.GetInt(PerBodyMigrationKey, 0) == 0;
        HorseConditionStats initial = canMigrateLegacy
            ? new HorseConditionStats(
                PlayerPrefs.GetFloat(Constants.HorseCondition.Power, max.Power),
                PlayerPrefs.GetFloat(Constants.HorseCondition.Cooling, max.Cooling),
                PlayerPrefs.GetFloat(Constants.HorseCondition.Stamina, max.Stamina))
            : max;

        initial = Clamp(initial, max);
        SaveCurrent(initial, false, horseId, bodyOptionId);

        string legacyTime = canMigrateLegacy
            ? PlayerPrefs.GetString(Constants.Timer.LastUpdateTime, DateTimeOffset.UtcNow.ToString("O"))
            : DateTimeOffset.UtcNow.ToString("O");
        PlayerPrefs.SetString(LastUpdateTimeKey(horseId, bodyOptionId), legacyTime);
        PlayerPrefs.SetInt(PerBodyMigrationKey, 1);
        PlayerPrefs.Save();
    }

    private static HorseConditionStats FromCatalogEntry(CatalogEntry entry)
    {
        return new HorseConditionStats(
            ResolveMax(entry.Power),
            ResolveMax(entry.Cool),
            ResolveMax(entry.Stamina));
    }

    private static float ResolveMax(int value) => value > 0 ? value : DefaultMaxValue;

    private static float GetOrInit(string key, float defaultValue, ref bool changed)
    {
        if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetFloat(key);
        PlayerPrefs.SetFloat(key, defaultValue);
        changed = true;
        return defaultValue;
    }

    private static void CacheMax(string horseId, string bodyOptionId, HorseConditionStats max)
    {
        PlayerPrefs.SetFloat(MaxPowerKey(horseId, bodyOptionId), max.Power);
        PlayerPrefs.SetFloat(MaxCoolingKey(horseId, bodyOptionId), max.Cooling);
        PlayerPrefs.SetFloat(MaxStaminaKey(horseId, bodyOptionId), max.Stamina);
        PlayerPrefs.Save();
    }

    private static void ResolveProfile(ref string horseId, ref string bodyOptionId)
    {
        horseId = NormalizeHorseId(horseId);
        if (string.IsNullOrWhiteSpace(bodyOptionId))
            bodyOptionId = PlayerPrefs.GetString(SelectionKey(horseId, BodySlotId), "");
        bodyOptionId = NormalizeBodyId(bodyOptionId);
    }

    private static bool IsActiveProfile(string horseId, string bodyOptionId)
    {
        string activeHorseId = ActiveHorseId;
        string activeBodyId = PlayerPrefs.GetString(SelectionKey(activeHorseId, BodySlotId), "");
        return NormalizeHorseId(horseId) == NormalizeHorseId(activeHorseId) &&
               NormalizeBodyId(bodyOptionId) == NormalizeBodyId(activeBodyId);
    }

    private static string NormalizeHorseId(string horseId) =>
        string.IsNullOrWhiteSpace(horseId) ? ActiveHorseId : horseId.Trim();

    private static string NormalizeBodyId(string bodyOptionId) =>
        string.IsNullOrWhiteSpace(bodyOptionId) ? MissingBodyId : bodyOptionId.Trim();

    private static string ProfilePrefix(string horseId, string bodyOptionId) =>
        $"HorseCondition_{horseId}_{bodyOptionId}";

    private static string SelectionKey(string horseId, string slotId) => $"Sel_{horseId}_{slotId}";
    private static string CurrentPowerKey(string horseId, string bodyOptionId) => $"{ProfilePrefix(horseId, bodyOptionId)}_power";
    private static string CurrentCoolingKey(string horseId, string bodyOptionId) => $"{ProfilePrefix(horseId, bodyOptionId)}_cooling";
    private static string CurrentStaminaKey(string horseId, string bodyOptionId) => $"{ProfilePrefix(horseId, bodyOptionId)}_stamina";
    private static string LastUpdateTimeKey(string horseId, string bodyOptionId) => $"{ProfilePrefix(horseId, bodyOptionId)}_lastUpdateTime";
    private static string MaxPowerKey(string horseId, string bodyOptionId) => $"HorseMax_{horseId}_{bodyOptionId}_power";
    private static string MaxCoolingKey(string horseId, string bodyOptionId) => $"HorseMax_{horseId}_{bodyOptionId}_cooling";
    private static string MaxStaminaKey(string horseId, string bodyOptionId) => $"HorseMax_{horseId}_{bodyOptionId}_stamina";
    private static string LegacyMaxPowerKey(string horseId) => $"HorseMax_{horseId}_{Constants.HorseCondition.Power}";
    private static string LegacyMaxCoolingKey(string horseId) => $"HorseMax_{horseId}_{Constants.HorseCondition.Cooling}";
    private static string LegacyMaxStaminaKey(string horseId) => $"HorseMax_{horseId}_{Constants.HorseCondition.Stamina}";
}
