using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }

    private const string ActivePlayerIdKey = "ActivePlayerId";
    private const string ActiveHorseIdKey = "ActiveHorseId";
    private const string DefaultPlayerId = "player_01";
    private const string DefaultHorseId = "horse_01";

    private const string FieldActivePlayerId = "activePlayerId";
    private const string FieldActiveHorseId = "activeHorseId";
    private const string FieldSelected = "selected";
    private const string FieldUnlocked = "unlocked";
    private const string FieldUpdatedAt = "updatedAt";

    private FirebaseFirestore db;
    private string uid;
    private bool remoteLoaded;

    private readonly List<SelectionWrite> pendingSelections = new List<SelectionWrite>();
    private readonly List<UnlockWrite> pendingUnlocks = new List<UnlockWrite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject manager = new GameObject(nameof(CustomizationManager));
        manager.AddComponent<CustomizationManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            FirebaseManager.Instance != null &&
            !string.IsNullOrEmpty(FirebaseManager.Instance.UserId)
        );

        db = FirebaseFirestore.DefaultInstance;
        uid = FirebaseManager.Instance.UserId;

        LoadFromFirestore();
    }

    public void SyncSelection(string avatarId, string slotId, string optionId)
    {
        if (string.IsNullOrEmpty(avatarId) || string.IsNullOrEmpty(slotId) || string.IsNullOrEmpty(optionId))
            return;

        SelectionWrite write = new SelectionWrite(avatarId, AvatarCustomPrefs.CanonicalSlotId(slotId), optionId);

        if (!CanWriteNow())
        {
            pendingSelections.Add(write);
            return;
        }

        WriteSelection(write);
    }

    public void SyncUnlock(string avatarId, string slotId, string optionId)
    {
        if (string.IsNullOrEmpty(avatarId) || string.IsNullOrEmpty(slotId) || string.IsNullOrEmpty(optionId))
            return;

        UnlockWrite write = new UnlockWrite(avatarId, AvatarCustomPrefs.CanonicalSlotId(slotId), optionId);

        if (!CanWriteNow())
        {
            pendingUnlocks.Add(write);
            return;
        }

        WriteUnlock(write);
    }

    private void LoadFromFirestore()
    {
        StateRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Customization load failed: " + task.Exception);
                remoteLoaded = true;
                FlushPendingWrites();
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
                ApplyRemoteSnapshot(snapshot);

            remoteLoaded = true;
            FlushPendingWrites();
        });
    }

    private void ApplyRemoteSnapshot(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> data = snapshot.ToDictionary();

        if (TryReadString(data, FieldActivePlayerId, out string activePlayerId))
            PlayerPrefs.SetString(ActivePlayerIdKey, activePlayerId);

        if (TryReadString(data, FieldActiveHorseId, out string activeHorseId))
            PlayerPrefs.SetString(ActiveHorseIdKey, activeHorseId);

        ApplySelectedMap(GetMap(data, FieldSelected));
        ApplyUnlockedMap(GetMap(data, FieldUnlocked));

        PlayerPrefs.Save();
    }

    private void ApplySelectedMap(Dictionary<string, object> selected)
    {
        if (selected == null)
            return;

        foreach (KeyValuePair<string, object> avatarPair in selected)
        {
            Dictionary<string, object> slotMap = avatarPair.Value as Dictionary<string, object>;
            if (slotMap == null)
                continue;

            foreach (KeyValuePair<string, object> slotPair in slotMap)
            {
                string optionId = slotPair.Value as string;
                if (!string.IsNullOrEmpty(optionId))
                    AvatarCustomPrefs.SetSelection(avatarPair.Key, slotPair.Key, optionId);
            }
        }
    }

    private void ApplyUnlockedMap(Dictionary<string, object> unlocked)
    {
        if (unlocked == null)
            return;

        foreach (KeyValuePair<string, object> avatarPair in unlocked)
        {
            Dictionary<string, object> slotMap = avatarPair.Value as Dictionary<string, object>;
            if (slotMap == null)
                continue;

            foreach (KeyValuePair<string, object> slotPair in slotMap)
                ApplyUnlockedOptions(avatarPair.Key, slotPair.Key, slotPair.Value);
        }
    }

    private void ApplyUnlockedOptions(string avatarId, string slotId, object value)
    {
        IEnumerable<object> options = value as IEnumerable<object>;
        if (options == null)
            return;

        foreach (object option in options)
        {
            string optionId = option as string;
            if (!string.IsNullOrEmpty(optionId))
                AvatarCustomPrefs.SetUnlocked(avatarId, slotId, optionId);
        }
    }

    private void WriteSelection(SelectionWrite write)
    {
        Dictionary<string, object> data = CreateBaseWriteData();
        data[FieldSelected] = BuildNestedSlotMap(write.AvatarId, write.SlotId, write.OptionId);

        StateRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                Debug.LogError("Customization selection sync failed: " + task.Exception);
        });
    }

    private void WriteUnlock(UnlockWrite write)
    {
        Dictionary<string, object> data = CreateBaseWriteData();
        data[FieldUnlocked] = BuildNestedSlotMap(
            write.AvatarId,
            write.SlotId,
            FieldValue.ArrayUnion(write.OptionId)
        );

        StateRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                Debug.LogError("Customization unlock sync failed: " + task.Exception);
        });
    }

    private void FlushPendingWrites()
    {
        if (!EnsureFirebaseReady())
            return;

        foreach (SelectionWrite write in pendingSelections)
            WriteSelection(write);

        foreach (UnlockWrite write in pendingUnlocks)
            WriteUnlock(write);

        pendingSelections.Clear();
        pendingUnlocks.Clear();
    }

    private bool CanWriteNow()
    {
        return remoteLoaded && EnsureFirebaseReady();
    }

    private bool EnsureFirebaseReady()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsSignedIn)
            return false;

        if (db == null)
            db = FirebaseFirestore.DefaultInstance;

        if (string.IsNullOrEmpty(uid))
            uid = FirebaseManager.Instance.UserId;

        return db != null && !string.IsNullOrEmpty(uid);
    }

    private DocumentReference StateRef
    {
        get { return db.Collection("users").Document(uid).Collection("customization").Document("state"); }
    }

    private static Dictionary<string, object> CreateBaseWriteData()
    {
        return new Dictionary<string, object>
        {
            { FieldActivePlayerId, PlayerPrefs.GetString(ActivePlayerIdKey, DefaultPlayerId) },
            { FieldActiveHorseId, PlayerPrefs.GetString(ActiveHorseIdKey, DefaultHorseId) },
            { FieldUpdatedAt, FieldValue.ServerTimestamp }
        };
    }

    private static Dictionary<string, object> BuildNestedSlotMap(string avatarId, string slotId, object value)
    {
        return new Dictionary<string, object>
        {
            {
                avatarId,
                new Dictionary<string, object>
                {
                    { slotId, value }
                }
            }
        };
    }

    private static Dictionary<string, object> GetMap(Dictionary<string, object> data, string key)
    {
        if (data == null || !data.TryGetValue(key, out object value))
            return null;

        return value as Dictionary<string, object>;
    }

    private static bool TryReadString(Dictionary<string, object> data, string key, out string value)
    {
        value = "";
        if (data == null || !data.TryGetValue(key, out object raw) || raw == null)
            return false;

        value = raw.ToString();
        return !string.IsNullOrEmpty(value);
    }

    private readonly struct SelectionWrite
    {
        public readonly string AvatarId;
        public readonly string SlotId;
        public readonly string OptionId;

        public SelectionWrite(string avatarId, string slotId, string optionId)
        {
            AvatarId = avatarId;
            SlotId = slotId;
            OptionId = optionId;
        }
    }

    private readonly struct UnlockWrite
    {
        public readonly string AvatarId;
        public readonly string SlotId;
        public readonly string OptionId;

        public UnlockWrite(string avatarId, string slotId, string optionId)
        {
            AvatarId = avatarId;
            SlotId = slotId;
            OptionId = optionId;
        }
    }
}
