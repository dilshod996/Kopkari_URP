using System;
using UnityEngine;

public static class AvatarCustomPrefs
{
    private const string CanonicalFaceHair = "FaceHair";
    private const string LegacyFaceHair = "Facehair";

    public static string CanonicalSlotId(string slotId)
    {
        if (slotId != null && slotId.Equals(LegacyFaceHair, StringComparison.Ordinal))
            return CanonicalFaceHair;

        return slotId;
    }

    public static string GetSelection(string avatarId, string slotId)
    {
        string canonicalKey = SelectionKey(avatarId, slotId);
        string optionId = PlayerPrefs.GetString(canonicalKey, "");
        if (!string.IsNullOrEmpty(optionId))
            return optionId;

        string legacyKey = LegacySelectionKey(avatarId, slotId);
        if (legacyKey == canonicalKey)
            return "";

        optionId = PlayerPrefs.GetString(legacyKey, "");
        if (!string.IsNullOrEmpty(optionId))
        {
            PlayerPrefs.SetString(canonicalKey, optionId);
            PlayerPrefs.Save();
        }

        return optionId;
    }

    public static void SetSelection(string avatarId, string slotId, string optionId)
    {
        PlayerPrefs.SetString(SelectionKey(avatarId, slotId), optionId);
    }

    public static bool IsSelected(string avatarId, string slotId, string optionId)
    {
        return GetSelection(avatarId, slotId) == optionId;
    }

    public static bool IsUnlocked(string avatarId, string slotId, string optionId)
    {
        string canonicalKey = UnlockKey(avatarId, slotId, optionId);
        if (PlayerPrefs.GetInt(canonicalKey, 0) == 1)
            return true;

        string legacyKey = LegacyUnlockKey(avatarId, slotId, optionId);
        if (legacyKey == canonicalKey || PlayerPrefs.GetInt(legacyKey, 0) != 1)
            return false;

        PlayerPrefs.SetInt(canonicalKey, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void SetUnlocked(string avatarId, string slotId, string optionId)
    {
        PlayerPrefs.SetInt(UnlockKey(avatarId, slotId, optionId), 1);
    }

    private static string SelectionKey(string avatarId, string slotId)
    {
        return $"Sel_{avatarId}_{CanonicalSlotId(slotId)}";
    }

    private static string UnlockKey(string avatarId, string slotId, string optionId)
    {
        return $"Unlock_{avatarId}_{CanonicalSlotId(slotId)}_{optionId}";
    }

    private static string LegacySelectionKey(string avatarId, string slotId)
    {
        return $"Sel_{avatarId}_{LegacySlotId(slotId)}";
    }

    private static string LegacyUnlockKey(string avatarId, string slotId, string optionId)
    {
        return $"Unlock_{avatarId}_{LegacySlotId(slotId)}_{optionId}";
    }

    private static string LegacySlotId(string slotId)
    {
        string canonical = CanonicalSlotId(slotId);
        return canonical == CanonicalFaceHair ? LegacyFaceHair : canonical;
    }
}
