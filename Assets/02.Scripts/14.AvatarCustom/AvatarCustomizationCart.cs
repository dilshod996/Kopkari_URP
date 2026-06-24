using System;
using System.Collections.Generic;
using UnityEngine;

public static class AvatarCustomizationCart
{
    private static readonly Dictionary<string, PendingItem> Items =
        new Dictionary<string, PendingItem>(StringComparer.OrdinalIgnoreCase);

    public static event Action OnChanged;

    public static bool HasPending => Items.Count > 0;
    public static int PendingCount => Items.Count;

    public static void Register(CatalogEntry entry, string avatarId, string slotId)
    {
        if (entry == null || string.IsNullOrEmpty(avatarId) || string.IsNullOrEmpty(slotId))
            return;

        string canonicalSlotId = AvatarCustomPrefs.CanonicalSlotId(slotId);
        Items[Key(avatarId, canonicalSlotId)] = new PendingItem(
            avatarId,
            canonicalSlotId,
            entry.OptionId,
            entry.Price,
            entry.IsDefault
        );

        NotifyChanged();
    }

    public static int GetTotalLockedCost()
    {
        int total = 0;

        foreach (PendingItem item in Items.Values)
        {
            if (!item.IsUnlocked)
                total += item.Price;
        }

        return Mathf.Max(0, total);
    }

    public static void UnlockPendingItems()
    {
        bool changed = false;

        foreach (PendingItem item in Items.Values)
        {
            if (item.IsUnlocked)
                continue;

            AvatarCustomPrefs.SetUnlocked(item.AvatarId, item.SlotId, item.OptionId);
            CustomizationManager.Instance?.SyncUnlock(item.AvatarId, item.SlotId, item.OptionId);
            changed = true;
        }

        if (changed)
            PlayerPrefs.Save();

        if (changed)
            NotifyChanged();
    }

    public static void Clear()
    {
        if (Items.Count == 0)
            return;

        Items.Clear();
        NotifyChanged();
    }

    public static void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    private static string Key(string avatarId, string slotId)
    {
        return $"{avatarId}|{slotId}";
    }

    private readonly struct PendingItem
    {
        public readonly string AvatarId;
        public readonly string SlotId;
        public readonly string OptionId;
        public readonly int Price;
        private readonly bool isDefault;

        public bool IsUnlocked =>
            isDefault || AvatarCustomPrefs.IsUnlocked(AvatarId, SlotId, OptionId);

        public PendingItem(string avatarId, string slotId, string optionId, int price, bool isDefault)
        {
            AvatarId = avatarId;
            SlotId = slotId;
            OptionId = optionId;
            Price = Mathf.Max(0, price);
            this.isDefault = isDefault;
        }
    }
}
