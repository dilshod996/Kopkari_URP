using System;
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
    private const string DefaultHorseId = "horse_01";
    private const string ActiveHorseIdKey = "ActiveHorseId";
    private const string BodySlotId = "Body";

    public static HorseConditionStats DefaultMax =>
        new HorseConditionStats(DefaultMaxValue, DefaultMaxValue, DefaultMaxValue);

    public static string ActiveHorseId =>
        PlayerPrefs.GetString(ActiveHorseIdKey, DefaultHorseId);

    public static HorseConditionStats GetCachedMaxOrDefault(string horseId = null)
    {
        horseId = NormalizeHorseId(horseId);

        if (!PlayerPrefs.HasKey(MaxPowerKey(horseId)) ||
            !PlayerPrefs.HasKey(MaxCoolingKey(horseId)) ||
            !PlayerPrefs.HasKey(MaxStaminaKey(horseId)))
        {
            return DefaultMax;
        }

        return new HorseConditionStats(
            PlayerPrefs.GetFloat(MaxPowerKey(horseId), DefaultMaxValue),
            PlayerPrefs.GetFloat(MaxCoolingKey(horseId), DefaultMaxValue),
            PlayerPrefs.GetFloat(MaxStaminaKey(horseId), DefaultMaxValue));
    }

    public static async Task<HorseConditionStats> GetActiveMaxAsync()
    {
        string horseId = ActiveHorseId;
        string bodyOptionId = PlayerPrefs.GetString(SelectionKey(horseId, BodySlotId), "");

        if (string.IsNullOrWhiteSpace(bodyOptionId))
        {
            PlayerCatalogProvider provider = PlayerCatalogProvider.Instance;
            if (provider == null) return GetCachedMaxOrDefault(horseId);

            await provider.EnsureCatalogAsync();
            bodyOptionId = provider.GetDefaultOptionId(horseId, BodySlotId);
        }

        return await GetBodyMaxAsync(horseId, bodyOptionId);
    }

    public static async Task<HorseConditionStats> GetBodyMaxAsync(string horseId, string bodyOptionId)
    {
        horseId = NormalizeHorseId(horseId);

        try
        {
            PlayerCatalogProvider provider = PlayerCatalogProvider.Instance;
            if (provider == null || string.IsNullOrWhiteSpace(bodyOptionId))
                return GetCachedMaxOrDefault(horseId);

            CatalogEntry entry = await provider.FindAsync(horseId, BodySlotId, bodyOptionId);
            if (entry == null)
                return GetCachedMaxOrDefault(horseId);

            HorseConditionStats max = FromCatalogEntry(entry);
            CacheMax(horseId, max);
            return max;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to read horse body max stats: {ex.Message}");
            return GetCachedMaxOrDefault(horseId);
        }
    }

    public static async Task SyncSelectedBodyMaxAsync(string horseId, string bodyOptionId)
    {
        HorseConditionStats max = await GetBodyMaxAsync(horseId, bodyOptionId);
        EnsureCurrentWithinMax(max);
    }

    public static HorseConditionStats GetCurrentOrInitialize(HorseConditionStats max)
    {
        bool changed = false;

        float power = GetOrInit(Constants.HorseCondition.Power, max.Power, ref changed);
        float cooling = GetOrInit(Constants.HorseCondition.Cooling, max.Cooling, ref changed);
        float stamina = GetOrInit(Constants.HorseCondition.Stamina, max.Stamina, ref changed);

        HorseConditionStats clamped = Clamp(new HorseConditionStats(power, cooling, stamina), max);

        if (!Mathf.Approximately(clamped.Power, power) ||
            !Mathf.Approximately(clamped.Cooling, cooling) ||
            !Mathf.Approximately(clamped.Stamina, stamina))
        {
            changed = true;
            SaveCurrent(clamped, saveNow: false);
        }

        if (changed) PlayerPrefs.Save();
        return clamped;
    }

    public static HorseConditionStats AddFood(float power, float cooling, float stamina)
    {
        HorseConditionStats max = GetCachedMaxOrDefault();
        HorseConditionStats current = GetCurrentOrInitialize(max);
        HorseConditionStats updated = Clamp(
            new HorseConditionStats(
                current.Power + power,
                current.Cooling + cooling,
                current.Stamina + stamina),
            max);

        SaveCurrent(updated);
        return updated;
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
        HorseConditionStats updated = new HorseConditionStats(
            current.Power + (max.Power / powerRegenMinutes * elapsedMinutes),
            current.Cooling + (max.Cooling / coolingRegenMinutes * elapsedMinutes),
            current.Stamina + (max.Stamina / staminaRegenMinutes * elapsedMinutes));

        return Clamp(updated, max);
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

    public static void SaveCurrent(HorseConditionStats stats, bool saveNow = true)
    {
        PlayerPrefs.SetFloat(Constants.HorseCondition.Power, stats.Power);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Cooling, stats.Cooling);
        PlayerPrefs.SetFloat(Constants.HorseCondition.Stamina, stats.Stamina);

        if (saveNow) PlayerPrefs.Save();
    }

    private static HorseConditionStats FromCatalogEntry(CatalogEntry entry)
    {
        return new HorseConditionStats(
            ResolveMax(entry.Power),
            ResolveMax(entry.Cool),
            ResolveMax(entry.Stamina));
    }

    private static float ResolveMax(int value) =>
        value > 0 ? value : DefaultMaxValue;

    private static float GetOrInit(string key, float defaultValue, ref bool changed)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetFloat(key);

        PlayerPrefs.SetFloat(key, defaultValue);
        changed = true;
        return defaultValue;
    }

    private static void CacheMax(string horseId, HorseConditionStats max)
    {
        PlayerPrefs.SetFloat(MaxPowerKey(horseId), max.Power);
        PlayerPrefs.SetFloat(MaxCoolingKey(horseId), max.Cooling);
        PlayerPrefs.SetFloat(MaxStaminaKey(horseId), max.Stamina);
        PlayerPrefs.Save();
    }

    private static string NormalizeHorseId(string horseId) =>
        string.IsNullOrWhiteSpace(horseId) ? DefaultHorseId : horseId.Trim();

    private static string SelectionKey(string horseId, string slotId) =>
        $"Sel_{horseId}_{slotId}";

    private static string MaxPowerKey(string horseId) =>
        $"HorseMax_{horseId}_{Constants.HorseCondition.Power}";

    private static string MaxCoolingKey(string horseId) =>
        $"HorseMax_{horseId}_{Constants.HorseCondition.Cooling}";

    private static string MaxStaminaKey(string horseId) =>
        $"HorseMax_{horseId}_{Constants.HorseCondition.Stamina}";
}
