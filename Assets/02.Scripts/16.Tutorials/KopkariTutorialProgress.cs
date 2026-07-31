using System;
using UnityEngine;

public static class KopkariTutorialProgress
{
    private const int ReminderCountMask = 0x3;
    private const int MaximumReminderCount = 3;

    public const ContextLesson RequiredContextLessons =
        ContextLesson.WalkZone |
        ContextLesson.GripDamage |
        ContextLesson.Defend |
        ContextLesson.LostUloq |
        ContextLesson.FakeUloq |
        ContextLesson.OpponentCarrier |
        ContextLesson.WebSnare |
        ContextLesson.ChainContainer |
        ContextLesson.HorseHealth;

    public enum CoreCheckpoint
    {
        Joystick = 0,
        CameraJoystick = 1,
        MatchStatus = 2,
        CameraView = 3,
        BackCamera = 4,
        Sprint = 5,
        UloqIndicator = 6,
        Pickup = 7,
        TargetIndicator = 8,
        ComboPrize = 9,
        Carrier = 10,
        NextRound = 11,
        WarmupIndicator = 12,
        WarmupArrival = 13,
        Completed = 14
    }

    public enum ObjectiveReminderKind
    {
        Uloq = 0,
        Target = 1,
        Warmup = 2
    }

    [Flags]
    public enum ContextLesson
    {
        None = 0,
        WalkZone = 1 << 0,
        GripDamage = 1 << 1,
        Defend = 1 << 2,
        LostUloq = 1 << 3,
        FakeUloq = 1 << 4,
        OpponentCarrier = 1 << 5,
        WebSnare = 1 << 6,
        ChainContainer = 1 << 7,
        HorseHealth = 1 << 8
    }

    public readonly struct State
    {
        public State(
            int version,
            bool completed,
            CoreCheckpoint checkpoint,
            ContextLesson context,
            int objectiveReminderCounts = 0)
        {
            Version = version;
            Completed = completed;
            Checkpoint = checkpoint;
            Context = context;
            ObjectiveReminderCounts = objectiveReminderCounts;
        }

        public int Version { get; }
        public bool Completed { get; }
        public CoreCheckpoint Checkpoint { get; }
        public ContextLesson Context { get; }
        public int ObjectiveReminderCounts { get; }
    }

    public static bool HasAnyLocalData =>
        PlayerPrefs.HasKey(Constants.KopkariTutorial.Completed) ||
        PlayerPrefs.HasKey(Constants.KopkariTutorial.LastCheckpoint) ||
        PlayerPrefs.HasKey(Constants.KopkariTutorial.ContextFlags);

    public static State LoadLocal()
    {
        bool legacyCompleted =
            PlayerPrefs.GetInt(Constants.KopkariTutorial.Completed, 0) == 1;
        int checkpointValue = Mathf.Clamp(
            PlayerPrefs.GetInt(
                Constants.KopkariTutorial.LastCheckpoint,
                (int)CoreCheckpoint.Joystick),
            (int)CoreCheckpoint.Joystick,
            (int)CoreCheckpoint.Completed);

        if (legacyCompleted)
            checkpointValue = (int)CoreCheckpoint.Completed;

        ContextLesson context =
            (ContextLesson)PlayerPrefs.GetInt(Constants.KopkariTutorial.ContextFlags, 0);
        int storedReminderCounts = PlayerPrefs.GetInt(
            Constants.KopkariTutorial.ObjectiveReminderCounts,
            0);
        int objectiveReminderCounts =
            ApplyLegacyFirstExposureCounts(
                (CoreCheckpoint)checkpointValue,
                storedReminderCounts);
        bool completed =
            checkpointValue >= (int)CoreCheckpoint.Completed &&
            HasAllRequiredContextLessons(context);
        if (legacyCompleted != completed)
        {
            PlayerPrefs.SetInt(
                Constants.KopkariTutorial.Completed,
                completed ? 1 : 0);
            PlayerPrefs.Save();
        }
        if (storedReminderCounts != objectiveReminderCounts)
        {
            PlayerPrefs.SetInt(
                Constants.KopkariTutorial.ObjectiveReminderCounts,
                objectiveReminderCounts);
            PlayerPrefs.Save();
        }

        return new State(
            PlayerPrefs.GetInt(
                Constants.KopkariTutorial.Version,
                Constants.KopkariTutorial.CurrentVersion),
            completed,
            (CoreCheckpoint)checkpointValue,
            context,
            objectiveReminderCounts);
    }

    public static State MergeAndSave(State cloudState)
    {
        State local = LoadLocal();
        CoreCheckpoint checkpoint = (CoreCheckpoint)Mathf.Max(
            (int)local.Checkpoint,
            (int)cloudState.Checkpoint);
        if (local.Completed || cloudState.Completed)
            checkpoint = CoreCheckpoint.Completed;

        ContextLesson context = local.Context | cloudState.Context;
        int objectiveReminderCounts = MergeReminderCounts(
            local.ObjectiveReminderCounts,
            cloudState.ObjectiveReminderCounts);
        bool completed =
            checkpoint >= CoreCheckpoint.Completed &&
            HasAllRequiredContextLessons(context);
        State merged = new State(
            Constants.KopkariTutorial.CurrentVersion,
            completed,
            checkpoint,
            context,
            objectiveReminderCounts);
        SaveState(merged);
        return merged;
    }

    public static void CompleteCoreStep(string stepKey, CoreCheckpoint nextCheckpoint)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        CoreCheckpoint checkpoint = (CoreCheckpoint)Mathf.Max(
            (int)current.Checkpoint,
            (int)nextCheckpoint);
        SaveState(new State(
            Constants.KopkariTutorial.CurrentVersion,
            current.Completed,
            checkpoint,
            current.Context,
            current.ObjectiveReminderCounts));
        DataManager.Instance?.QueueKopkariTutorialProgressSync();
    }

    public static void CompleteContextStep(string stepKey, ContextLesson lesson)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        ContextLesson context = current.Context | lesson;
        bool completed =
            current.Checkpoint >= CoreCheckpoint.Completed &&
            HasAllRequiredContextLessons(context);
        SaveState(new State(
            Constants.KopkariTutorial.CurrentVersion,
            completed,
            current.Checkpoint,
            context,
            current.ObjectiveReminderCounts));
        if (completed)
            DataManager.Instance?.CompleteKopkariTutorial();
        else
            DataManager.Instance?.QueueKopkariTutorialProgressSync();
    }

    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(Constants.KopkariTutorial.RoundStart, 1);
        State current = LoadLocal();
        bool completed = HasAllRequiredContextLessons(current.Context);
        SaveState(new State(
            Constants.KopkariTutorial.CurrentVersion,
            completed,
            CoreCheckpoint.Completed,
            current.Context,
            current.ObjectiveReminderCounts));
        if (completed)
            DataManager.Instance?.CompleteKopkariTutorial();
        else
            DataManager.Instance?.QueueKopkariTutorialProgressSync();
    }

    public static bool HasContextLesson(ContextLesson lesson)
    {
        return (LoadLocal().Context & lesson) == lesson;
    }

    public static bool IsFullyCompleted(State state)
    {
        return state.Checkpoint >= CoreCheckpoint.Completed &&
               HasAllRequiredContextLessons(state.Context);
    }

    public static bool HasAllRequiredContextLessons(ContextLesson context)
    {
        return (context & RequiredContextLessons) == RequiredContextLessons;
    }

    public static int GetObjectiveReminderCount(
        State state,
        ObjectiveReminderKind kind)
    {
        int shift = GetReminderShift(kind);
        return (state.ObjectiveReminderCounts >> shift) & ReminderCountMask;
    }

    public static int EnsureObjectiveReminderCountAtLeast(
        ObjectiveReminderKind kind,
        int minimumCount)
    {
        State current = LoadLocal();
        int currentCount = GetObjectiveReminderCount(current, kind);
        int targetCount = Mathf.Clamp(
            Mathf.Max(currentCount, minimumCount),
            0,
            MaximumReminderCount);
        if (targetCount == currentCount)
            return currentCount;

        SaveReminderCount(current, kind, targetCount);
        return targetCount;
    }

    public static int RecordObjectiveReminder(ObjectiveReminderKind kind)
    {
        State current = LoadLocal();
        int currentCount = GetObjectiveReminderCount(current, kind);
        int nextCount = Mathf.Min(MaximumReminderCount, currentCount + 1);
        if (nextCount == currentCount)
            return currentCount;

        SaveReminderCount(current, kind, nextCount);
        return nextCount;
    }

    public static void DeleteAllLocalProgress()
    {
        string[] keys =
        {
            Constants.KopkariTutorial.Completed,
            Constants.KopkariTutorial.Version,
            Constants.KopkariTutorial.LastCheckpoint,
            Constants.KopkariTutorial.ContextFlags,
            Constants.KopkariTutorial.ObjectiveReminderCounts,
            Constants.KopkariTutorial.Joystick,
            Constants.KopkariTutorial.CameraJoystick,
            Constants.KopkariTutorial.MatchStatus,
            Constants.KopkariTutorial.CameraView,
            Constants.KopkariTutorial.CameraFirstPerson,
            Constants.KopkariTutorial.CameraThirdPerson,
            Constants.KopkariTutorial.BackCamera,
            Constants.KopkariTutorial.SprintButton,
            Constants.KopkariTutorial.SprintSlider,
            Constants.KopkariTutorial.UloqIndicator,
            Constants.KopkariTutorial.PickupButton,
            Constants.KopkariTutorial.PickupProgress,
            Constants.KopkariTutorial.TargetIndicator,
            Constants.KopkariTutorial.ComboPrize,
            Constants.KopkariTutorial.Carrier,
            Constants.KopkariTutorial.WalkZone,
            Constants.KopkariTutorial.GripDamage,
            Constants.KopkariTutorial.Defend,
            Constants.KopkariTutorial.LostUloq,
            Constants.KopkariTutorial.FakeUloq,
            Constants.KopkariTutorial.OpponentCarrier,
            Constants.KopkariTutorial.WebSnare,
            Constants.KopkariTutorial.ChainContainer,
            Constants.KopkariTutorial.HorseHealth,
            Constants.KopkariTutorial.WarmupBackground,
            Constants.KopkariTutorial.WarmupIndicator,
            Constants.KopkariTutorial.RoundStart
        };

        foreach (string key in keys)
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
    }

    private static void SaveState(State state)
    {
        PlayerPrefs.SetInt(
            Constants.KopkariTutorial.Version,
            Constants.KopkariTutorial.CurrentVersion);
        PlayerPrefs.SetInt(
            Constants.KopkariTutorial.LastCheckpoint,
            (int)state.Checkpoint);
        PlayerPrefs.SetInt(
            Constants.KopkariTutorial.ContextFlags,
            (int)state.Context);
        PlayerPrefs.SetInt(
            Constants.KopkariTutorial.ObjectiveReminderCounts,
            state.ObjectiveReminderCounts);
        PlayerPrefs.SetInt(
            Constants.KopkariTutorial.Completed,
            state.Completed ? 1 : 0);
        ApplyNamedCoreKeys(state.Checkpoint);
        ApplyNamedContextKeys(state.Context);
        PlayerPrefs.Save();
    }

    private static void ApplyNamedCoreKeys(CoreCheckpoint checkpoint)
    {
        SetIfReached(checkpoint, CoreCheckpoint.CameraJoystick, Constants.KopkariTutorial.Joystick);
        SetIfReached(checkpoint, CoreCheckpoint.MatchStatus, Constants.KopkariTutorial.CameraJoystick);
        SetIfReached(checkpoint, CoreCheckpoint.CameraView, Constants.KopkariTutorial.MatchStatus);
        SetIfReached(checkpoint, CoreCheckpoint.BackCamera, Constants.KopkariTutorial.CameraView);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.BackCamera,
            Constants.KopkariTutorial.CameraFirstPerson);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.BackCamera,
            Constants.KopkariTutorial.CameraThirdPerson);
        SetIfReached(checkpoint, CoreCheckpoint.Sprint, Constants.KopkariTutorial.BackCamera);
        SetIfReached(checkpoint, CoreCheckpoint.UloqIndicator, Constants.KopkariTutorial.SprintButton);
        SetIfReached(checkpoint, CoreCheckpoint.UloqIndicator, Constants.KopkariTutorial.SprintSlider);
        SetIfReached(checkpoint, CoreCheckpoint.Pickup, Constants.KopkariTutorial.UloqIndicator);
        SetIfReached(checkpoint, CoreCheckpoint.TargetIndicator, Constants.KopkariTutorial.PickupButton);
        SetIfReached(checkpoint, CoreCheckpoint.TargetIndicator, Constants.KopkariTutorial.PickupProgress);
        SetIfReached(checkpoint, CoreCheckpoint.ComboPrize, Constants.KopkariTutorial.TargetIndicator);
        SetIfReached(checkpoint, CoreCheckpoint.Carrier, Constants.KopkariTutorial.ComboPrize);
        SetIfReached(checkpoint, CoreCheckpoint.NextRound, Constants.KopkariTutorial.Carrier);
        SetIfReached(checkpoint, CoreCheckpoint.WarmupIndicator, Constants.KopkariTutorial.WarmupBackground);
        SetIfReached(checkpoint, CoreCheckpoint.WarmupArrival, Constants.KopkariTutorial.WarmupIndicator);
        SetIfReached(checkpoint, CoreCheckpoint.Completed, Constants.KopkariTutorial.RoundStart);
    }

    private static void ApplyNamedContextKeys(ContextLesson context)
    {
        SetIfContext(context, ContextLesson.WalkZone, Constants.KopkariTutorial.WalkZone);
        SetIfContext(context, ContextLesson.GripDamage, Constants.KopkariTutorial.GripDamage);
        SetIfContext(context, ContextLesson.Defend, Constants.KopkariTutorial.Defend);
        SetIfContext(context, ContextLesson.LostUloq, Constants.KopkariTutorial.LostUloq);
        SetIfContext(context, ContextLesson.FakeUloq, Constants.KopkariTutorial.FakeUloq);
        SetIfContext(context, ContextLesson.OpponentCarrier, Constants.KopkariTutorial.OpponentCarrier);
        SetIfContext(context, ContextLesson.WebSnare, Constants.KopkariTutorial.WebSnare);
        SetIfContext(context, ContextLesson.ChainContainer, Constants.KopkariTutorial.ChainContainer);
        SetIfContext(context, ContextLesson.HorseHealth, Constants.KopkariTutorial.HorseHealth);
    }

    private static void SetIfReached(
        CoreCheckpoint checkpoint,
        CoreCheckpoint required,
        string key)
    {
        if (checkpoint >= required)
            PlayerPrefs.SetInt(key, 1);
    }

    private static void SetIfContext(ContextLesson context, ContextLesson required, string key)
    {
        if ((context & required) == required)
            PlayerPrefs.SetInt(key, 1);
    }

    private static void SaveReminderCount(
        State current,
        ObjectiveReminderKind kind,
        int count)
    {
        int shift = GetReminderShift(kind);
        int cleared = current.ObjectiveReminderCounts &
                      ~(ReminderCountMask << shift);
        int packed = cleared |
                     ((Mathf.Clamp(count, 0, MaximumReminderCount) &
                       ReminderCountMask) << shift);
        SaveState(new State(
            Constants.KopkariTutorial.CurrentVersion,
            current.Completed,
            current.Checkpoint,
            current.Context,
            packed));
        DataManager.Instance?.QueueKopkariTutorialProgressSync();
    }

    private static int ApplyLegacyFirstExposureCounts(
        CoreCheckpoint checkpoint,
        int packedCounts)
    {
        if (checkpoint >= CoreCheckpoint.Pickup)
            packedCounts = SetReminderCountAtLeast(
                packedCounts,
                ObjectiveReminderKind.Uloq,
                1);
        if (checkpoint >= CoreCheckpoint.ComboPrize)
            packedCounts = SetReminderCountAtLeast(
                packedCounts,
                ObjectiveReminderKind.Target,
                1);
        if (checkpoint >= CoreCheckpoint.WarmupArrival)
            packedCounts = SetReminderCountAtLeast(
                packedCounts,
                ObjectiveReminderKind.Warmup,
                1);

        return packedCounts;
    }

    private static int MergeReminderCounts(int first, int second)
    {
        int merged = 0;
        foreach (ObjectiveReminderKind kind in
                 (ObjectiveReminderKind[])Enum.GetValues(typeof(ObjectiveReminderKind)))
        {
            int shift = GetReminderShift(kind);
            int count = Mathf.Max(
                (first >> shift) & ReminderCountMask,
                (second >> shift) & ReminderCountMask);
            merged |= count << shift;
        }

        return merged;
    }

    private static int SetReminderCountAtLeast(
        int packedCounts,
        ObjectiveReminderKind kind,
        int minimumCount)
    {
        int shift = GetReminderShift(kind);
        int current = (packedCounts >> shift) & ReminderCountMask;
        int target = Mathf.Clamp(
            Mathf.Max(current, minimumCount),
            0,
            MaximumReminderCount);
        return (packedCounts & ~(ReminderCountMask << shift)) |
               (target << shift);
    }

    private static int GetReminderShift(ObjectiveReminderKind kind)
    {
        return (int)kind * 2;
    }
}
