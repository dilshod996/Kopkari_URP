using System;
using UnityEngine;

public static class HomeTutorialProgress
{
    public enum CoreCheckpoint
    {
        Settings = 0,
        Profile = 1,
        Play = 2,
        GameMode = 3,
        RacingMode = 4,
        RacingMap = 5,
        EnterRacingRoom = 6,
        Completed = 7
    }

    [Flags]
    public enum ContextLesson
    {
        None = 0,
        HorseCondition = 1 << 0
    }

    public readonly struct State
    {
        public State(
            int version,
            bool completed,
            CoreCheckpoint checkpoint,
            ContextLesson context)
        {
            Version = version;
            Completed = completed;
            Checkpoint = checkpoint;
            Context = context;
        }

        public int Version { get; }
        public bool Completed { get; }
        public CoreCheckpoint Checkpoint { get; }
        public ContextLesson Context { get; }
    }

    public static bool HasAnyLocalData =>
        PlayerPrefs.HasKey(Constants.HomeTutorial.Completed) ||
        PlayerPrefs.HasKey(Constants.HomeTutorial.LastCheckpoint) ||
        PlayerPrefs.HasKey(Constants.HomeTutorial.ContextFlags);

    public static State LoadLocal()
    {
        bool completed =
            PlayerPrefs.GetInt(Constants.HomeTutorial.Completed, 0) == 1;
        int checkpointValue = Mathf.Clamp(
            PlayerPrefs.GetInt(
                Constants.HomeTutorial.LastCheckpoint,
                (int)CoreCheckpoint.Settings),
            (int)CoreCheckpoint.Settings,
            (int)CoreCheckpoint.Completed);

        if (completed)
            checkpointValue = (int)CoreCheckpoint.Completed;

        return new State(
            PlayerPrefs.GetInt(
                Constants.HomeTutorial.Version,
                Constants.HomeTutorial.CurrentVersion),
            completed,
            (CoreCheckpoint)checkpointValue,
            (ContextLesson)PlayerPrefs.GetInt(
                Constants.HomeTutorial.ContextFlags,
                0));
    }

    public static State MergeAndSave(State cloudState)
    {
        State local = LoadLocal();
        bool completed = local.Completed || cloudState.Completed;
        CoreCheckpoint checkpoint = (CoreCheckpoint)Mathf.Max(
            (int)local.Checkpoint,
            (int)cloudState.Checkpoint);

        if (completed)
            checkpoint = CoreCheckpoint.Completed;

        State merged = new State(
            Constants.HomeTutorial.CurrentVersion,
            completed,
            checkpoint,
            local.Context | cloudState.Context);
        SaveState(merged);
        return merged;
    }

    public static void CompleteCoreStep(
        string stepKey,
        CoreCheckpoint nextCheckpoint)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        SaveState(new State(
            Constants.HomeTutorial.CurrentVersion,
            current.Completed,
            (CoreCheckpoint)Mathf.Max(
                (int)current.Checkpoint,
                (int)nextCheckpoint),
            current.Context));
        DataManager.Instance?.QueueHomeTutorialProgressSync();
    }

    public static void CompleteContextStep(
        string stepKey,
        ContextLesson lesson)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        SaveState(new State(
            Constants.HomeTutorial.CurrentVersion,
            current.Completed,
            current.Checkpoint,
            current.Context | lesson));
        DataManager.Instance?.QueueHomeTutorialProgressSync();
    }

    public static bool HasContextLesson(ContextLesson lesson)
    {
        return (LoadLocal().Context & lesson) == lesson;
    }

    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(Constants.HomeTutorial.EnterRacingRoom, 1);
        State current = LoadLocal();
        SaveState(new State(
            Constants.HomeTutorial.CurrentVersion,
            true,
            CoreCheckpoint.Completed,
            current.Context));
        DataManager.Instance?.CompleteHomeTutorial();
    }

    public static void DeleteAllLocalProgress()
    {
        string[] keys =
        {
            Constants.HomeTutorial.Completed,
            Constants.HomeTutorial.Version,
            Constants.HomeTutorial.LastCheckpoint,
            Constants.HomeTutorial.ContextFlags,
            Constants.HomeTutorial.Settings,
            Constants.HomeTutorial.Profile,
            Constants.HomeTutorial.Play,
            Constants.HomeTutorial.GameMode,
            Constants.HomeTutorial.RacingMode,
            Constants.HomeTutorial.RacingMap,
            Constants.HomeTutorial.EnterRacingRoom,
            Constants.HomeTutorial.HorseCondition
        };

        foreach (string key in keys)
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
    }

    private static void SaveState(State state)
    {
        PlayerPrefs.SetInt(
            Constants.HomeTutorial.Version,
            Constants.HomeTutorial.CurrentVersion);
        PlayerPrefs.SetInt(
            Constants.HomeTutorial.LastCheckpoint,
            (int)state.Checkpoint);
        PlayerPrefs.SetInt(
            Constants.HomeTutorial.ContextFlags,
            (int)state.Context);
        PlayerPrefs.SetInt(
            Constants.HomeTutorial.Completed,
            state.Completed ? 1 : 0);
        ApplyNamedCoreKeys(state.Checkpoint);
        ApplyNamedContextKeys(state.Context);
        PlayerPrefs.Save();
    }

    private static void ApplyNamedCoreKeys(CoreCheckpoint checkpoint)
    {
        SetIfReached(
            checkpoint,
            CoreCheckpoint.Profile,
            Constants.HomeTutorial.Settings);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.Play,
            Constants.HomeTutorial.Profile);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.GameMode,
            Constants.HomeTutorial.Play);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.RacingMode,
            Constants.HomeTutorial.GameMode);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.RacingMap,
            Constants.HomeTutorial.RacingMode);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.EnterRacingRoom,
            Constants.HomeTutorial.RacingMap);
        SetIfReached(
            checkpoint,
            CoreCheckpoint.Completed,
            Constants.HomeTutorial.EnterRacingRoom);
    }

    private static void ApplyNamedContextKeys(ContextLesson context)
    {
        if ((context & ContextLesson.HorseCondition) ==
            ContextLesson.HorseCondition)
        {
            PlayerPrefs.SetInt(Constants.HomeTutorial.HorseCondition, 1);
        }
    }

    private static void SetIfReached(
        CoreCheckpoint checkpoint,
        CoreCheckpoint required,
        string key)
    {
        if (checkpoint >= required)
            PlayerPrefs.SetInt(key, 1);
    }
}
