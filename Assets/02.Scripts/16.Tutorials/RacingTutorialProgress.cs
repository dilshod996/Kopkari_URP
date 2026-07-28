using System;
using UnityEngine;

public static class RacingTutorialProgress
{
    public enum CoreCheckpoint
    {
        LaunchMeter = 0,
        Controls = 1,
        Camera = 2,
        LookBack = 3,
        Sprint = 4,
        Racing = 5,
        Result = 6,
        Completed = 7
    }

    [Flags]
    public enum ContextLesson
    {
        None = 0,
        Defense = 1 << 0,
        SlowTrap = 1 << 1,
        WebSnare = 1 << 2,
        SprintFull = 1 << 3,
        AutoSprint = 1 << 4,
        WalkZoneHazard = 1 << 5,
        WebSnareAffected = 1 << 6,
        HitCounter = 1 << 7,
        MiniMap = 1 << 8,
        Leaderboard = 1 << 9,
        SpecialGate = 1 << 10
    }

    [Flags]
    public enum ControllerLesson
    {
        None = 0,
        Reins = 1 << 0,
        Buttons = 1 << 1,
        Tilt = 1 << 2
    }

    public readonly struct State
    {
        public State(
            int version,
            bool completed,
            CoreCheckpoint checkpoint,
            ContextLesson context,
            ControllerLesson controllers = ControllerLesson.None)
        {
            Version = version;
            Completed = completed;
            Checkpoint = checkpoint;
            Context = context;
            Controllers = controllers;
        }

        public int Version { get; }
        public bool Completed { get; }
        public CoreCheckpoint Checkpoint { get; }
        public ContextLesson Context { get; }
        public ControllerLesson Controllers { get; }
    }

    public static bool HasAnyLocalData =>
        PlayerPrefs.HasKey(Constants.RacingTutorial.Completed) ||
        PlayerPrefs.HasKey(Constants.RacingTutorial.LastCheckpoint) ||
        PlayerPrefs.HasKey(Constants.RacingTutorial.ContextFlags) ||
        PlayerPrefs.HasKey(Constants.RacingTutorial.ControllerFlags);

    public static State LoadLocal()
    {
        bool completed = PlayerPrefs.GetInt(Constants.RacingTutorial.Completed, 0) == 1;
        int checkpointValue = Mathf.Clamp(
            PlayerPrefs.GetInt(
                Constants.RacingTutorial.LastCheckpoint,
                (int)CoreCheckpoint.LaunchMeter),
            (int)CoreCheckpoint.LaunchMeter,
            (int)CoreCheckpoint.Completed);

        if (completed)
            checkpointValue = (int)CoreCheckpoint.Completed;

        return new State(
            PlayerPrefs.GetInt(
                Constants.RacingTutorial.Version,
                Constants.RacingTutorial.CurrentVersion),
            completed,
            (CoreCheckpoint)checkpointValue,
            (ContextLesson)PlayerPrefs.GetInt(Constants.RacingTutorial.ContextFlags, 0),
            (ControllerLesson)PlayerPrefs.GetInt(Constants.RacingTutorial.ControllerFlags, 0));
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
            Constants.RacingTutorial.CurrentVersion,
            completed,
            checkpoint,
            local.Context | cloudState.Context,
            local.Controllers | cloudState.Controllers);
        SaveState(merged);
        return merged;
    }

    public static void CompleteCoreStep(string stepKey, CoreCheckpoint nextCheckpoint)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        SaveState(new State(
            Constants.RacingTutorial.CurrentVersion,
            current.Completed,
            (CoreCheckpoint)Mathf.Max((int)current.Checkpoint, (int)nextCheckpoint),
            current.Context,
            current.Controllers));
        DataManager.Instance?.QueueRacingTutorialProgressSync();
    }

    public static void CompleteContextStep(string stepKey, ContextLesson lesson)
    {
        if (!string.IsNullOrEmpty(stepKey))
            PlayerPrefs.SetInt(stepKey, 1);

        State current = LoadLocal();
        SaveState(new State(
            Constants.RacingTutorial.CurrentVersion,
            current.Completed,
            current.Checkpoint,
            current.Context | lesson,
            current.Controllers));
        DataManager.Instance?.QueueRacingTutorialProgressSync();
    }

    public static bool IsControllerLessonCompleted(RacingControllerType controllerType)
    {
        ControllerLesson lesson = GetControllerLesson(controllerType);
        return (LoadLocal().Controllers & lesson) == lesson;
    }

    public static void CompleteControllerLesson(RacingControllerType controllerType)
    {
        State current = LoadLocal();
        SaveState(new State(
            Constants.RacingTutorial.CurrentVersion,
            current.Completed,
            current.Checkpoint,
            current.Context,
            current.Controllers | GetControllerLesson(controllerType)));
        DataManager.Instance?.QueueRacingTutorialProgressSync();
    }

    public static void CompleteTutorial()
    {
        PlayerPrefs.SetInt(Constants.RacingTutorial.Result, 1);
        State current = LoadLocal();
        SaveState(new State(
            Constants.RacingTutorial.CurrentVersion,
            true,
            CoreCheckpoint.Completed,
            current.Context,
            current.Controllers));
        DataManager.Instance?.CompleteRacingTutorial();
    }

    public static void DeleteAllLocalProgress()
    {
        string[] keys =
        {
            Constants.RacingTutorial.Completed,
            Constants.RacingTutorial.Version,
            Constants.RacingTutorial.LastCheckpoint,
            Constants.RacingTutorial.ContextFlags,
            Constants.RacingTutorial.ControllerFlags,
            Constants.RacingTutorial.LaunchMeter,
            Constants.RacingTutorial.Controls,
            Constants.RacingTutorial.Camera,
            Constants.RacingTutorial.LookBack,
            Constants.RacingTutorial.Sprint,
            Constants.RacingTutorial.Finish,
            Constants.RacingTutorial.Result,
            Constants.RacingTutorial.Defense,
            Constants.RacingTutorial.SlowTrap,
            Constants.RacingTutorial.WebSnare,
            Constants.RacingTutorial.SprintFull,
            Constants.RacingTutorial.AutoSprint,
            Constants.RacingTutorial.WalkZoneHazard,
            Constants.RacingTutorial.WebSnareAffected,
            Constants.RacingTutorial.HitCounter,
            Constants.RacingTutorial.MiniMap,
            Constants.RacingTutorial.Leaderboard,
            Constants.RacingTutorial.SpecialGate
        };

        foreach (string key in keys)
            PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();
    }

    private static void SaveState(State state)
    {
        PlayerPrefs.SetInt(Constants.RacingTutorial.Version, Constants.RacingTutorial.CurrentVersion);
        PlayerPrefs.SetInt(Constants.RacingTutorial.LastCheckpoint, (int)state.Checkpoint);
        PlayerPrefs.SetInt(Constants.RacingTutorial.ContextFlags, (int)state.Context);
        PlayerPrefs.SetInt(Constants.RacingTutorial.ControllerFlags, (int)state.Controllers);
        PlayerPrefs.SetInt(Constants.RacingTutorial.Completed, state.Completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static ControllerLesson GetControllerLesson(
        RacingControllerType controllerType)
    {
        return controllerType switch
        {
            RacingControllerType.Reins => ControllerLesson.Reins,
            RacingControllerType.Tilt => ControllerLesson.Tilt,
            _ => ControllerLesson.Buttons
        };
    }
}
