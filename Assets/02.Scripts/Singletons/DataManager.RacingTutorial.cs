using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public partial class DataManager
{
    private Coroutine racingTutorialLoadRoutine;
    private Coroutine racingTutorialSyncRoutine;
    private Coroutine racingTutorialDeleteRoutine;

    public bool IsRacingTutorialStateLoaded { get; private set; }

    public void EnsureRacingTutorialStateLoaded()
    {
        if (IsRacingTutorialStateLoaded || racingTutorialLoadRoutine != null)
            return;

        racingTutorialLoadRoutine = StartCoroutine(LoadRacingTutorialState());
    }

    public void QueueRacingTutorialProgressSync()
    {
        if (racingTutorialSyncRoutine != null)
            StopCoroutine(racingTutorialSyncRoutine);

        racingTutorialSyncRoutine = StartCoroutine(SyncRacingTutorialAfterDelay(2f));
    }

    public void CompleteRacingTutorial()
    {
        if (racingTutorialSyncRoutine != null)
        {
            StopCoroutine(racingTutorialSyncRoutine);
            racingTutorialSyncRoutine = null;
        }

        racingTutorialSyncRoutine = StartCoroutine(SyncRacingTutorialAfterDelay(0f));
    }

    public void DeleteRacingTutorialProgressForTesting()
    {
        if (racingTutorialLoadRoutine != null)
        {
            StopCoroutine(racingTutorialLoadRoutine);
            racingTutorialLoadRoutine = null;
        }

        if (racingTutorialSyncRoutine != null)
        {
            StopCoroutine(racingTutorialSyncRoutine);
            racingTutorialSyncRoutine = null;
        }

        if (racingTutorialDeleteRoutine != null)
            StopCoroutine(racingTutorialDeleteRoutine);

        RacingTutorialProgress.DeleteAllLocalProgress();
        IsRacingTutorialStateLoaded = false;
        racingTutorialDeleteRoutine =
            StartCoroutine(DeleteRacingTutorialProgressFromFirebase());
    }

    private IEnumerator DeleteRacingTutorialProgressFromFirebase()
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while ((db == null || string.IsNullOrEmpty(uid)) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (db == null || string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning(
                "Racing tutorial PlayerPrefs were deleted, but Firebase was not ready.");
            racingTutorialDeleteRoutine = null;
            yield break;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);
        var deleteTask = userRef.UpdateAsync(new Dictionary<string, object>
        {
            { Constants.RacingTutorial.CloudCompleted, FieldValue.Delete },
            { Constants.RacingTutorial.CloudVersion, FieldValue.Delete },
            { Constants.RacingTutorial.CloudCheckpoint, FieldValue.Delete },
            { Constants.RacingTutorial.CloudContextFlags, FieldValue.Delete },
            { Constants.RacingTutorial.CloudControllerFlags, FieldValue.Delete }
        });

        while (!deleteTask.IsCompleted)
            yield return null;

        if (deleteTask.IsFaulted || deleteTask.IsCanceled)
        {
            Debug.LogWarning(
                "Racing tutorial PlayerPrefs were deleted, but Firebase deletion failed.");
        }
        else
        {
            Debug.Log(
                "Racing tutorial progress was deleted from PlayerPrefs and Firebase.");
        }

        racingTutorialDeleteRoutine = null;
    }

    private IEnumerator LoadRacingTutorialState()
    {
        const float readinessTimeout = 5f;
        float deadline = Time.realtimeSinceStartup + readinessTimeout;
        while ((db == null || string.IsNullOrEmpty(uid)) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (db == null || string.IsNullOrEmpty(uid))
        {
            IsRacingTutorialStateLoaded = true;
            racingTutorialLoadRoutine = null;
            yield break;
        }

        var task = db.Collection("users").Document(uid).GetSnapshotAsync();
        while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
        {
            Debug.LogWarning("Racing tutorial progress could not be loaded from Firebase.");
            IsRacingTutorialStateLoaded = true;
            racingTutorialLoadRoutine = null;
            yield break;
        }

        DocumentSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();
            int cloudVersion = GetRacingTutorialInt(
                data,
                Constants.RacingTutorial.CloudVersion,
                Constants.RacingTutorial.CurrentVersion);
            if (cloudVersion > Constants.RacingTutorial.CurrentVersion)
            {
                Debug.LogWarning(
                    "Racing tutorial cloud progress uses a newer version; local data was not uploaded.");
                IsRacingTutorialStateLoaded = true;
                racingTutorialLoadRoutine = null;
                yield break;
            }

            bool compatibleVersion =
                cloudVersion == Constants.RacingTutorial.CurrentVersion;
            var cloudState = new RacingTutorialProgress.State(
                Constants.RacingTutorial.CurrentVersion,
                compatibleVersion &&
                GetRacingTutorialInt(data, Constants.RacingTutorial.CloudCompleted, 0) == 1,
                compatibleVersion
                    ? (RacingTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                        GetRacingTutorialInt(
                            data,
                            Constants.RacingTutorial.CloudCheckpoint,
                            (int)RacingTutorialProgress.CoreCheckpoint.LaunchMeter),
                        (int)RacingTutorialProgress.CoreCheckpoint.LaunchMeter,
                        (int)RacingTutorialProgress.CoreCheckpoint.Completed)
                    : RacingTutorialProgress.CoreCheckpoint.LaunchMeter,
                compatibleVersion
                    ? (RacingTutorialProgress.ContextLesson)GetRacingTutorialInt(
                        data,
                        Constants.RacingTutorial.CloudContextFlags,
                        0)
                    : RacingTutorialProgress.ContextLesson.None,
                compatibleVersion
                    ? (RacingTutorialProgress.ControllerLesson)GetRacingTutorialInt(
                        data,
                        Constants.RacingTutorial.CloudControllerFlags,
                        0)
                    : RacingTutorialProgress.ControllerLesson.None);

            RacingTutorialProgress.State merged =
                RacingTutorialProgress.MergeAndSave(cloudState);
            bool cloudMissingFields =
                !data.ContainsKey(Constants.RacingTutorial.CloudVersion) ||
                !data.ContainsKey(Constants.RacingTutorial.CloudCompleted) ||
                !data.ContainsKey(Constants.RacingTutorial.CloudCheckpoint) ||
                !data.ContainsKey(Constants.RacingTutorial.CloudContextFlags) ||
                !data.ContainsKey(Constants.RacingTutorial.CloudControllerFlags);
            bool localWasAhead =
                merged.Completed != cloudState.Completed ||
                merged.Checkpoint != cloudState.Checkpoint ||
                merged.Context != cloudState.Context ||
                merged.Controllers != cloudState.Controllers;

            if (cloudMissingFields || localWasAhead || !compatibleVersion)
                SyncRacingTutorialProgress();
        }

        IsRacingTutorialStateLoaded = true;
        racingTutorialLoadRoutine = null;
    }

    private IEnumerator SyncRacingTutorialAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float deadline = Time.realtimeSinceStartup + 10f;
        while ((db == null || string.IsNullOrEmpty(uid)) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        racingTutorialSyncRoutine = null;
        SyncRacingTutorialProgress();
    }

    private void SyncRacingTutorialProgress()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        DocumentReference userRef = db.Collection("users").Document(uid);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
        {
            if (readTask.IsFaulted || readTask.IsCanceled)
            {
                Debug.LogWarning("Racing tutorial progress merge failed.");
                return;
            }

            RacingTutorialProgress.State state = RacingTutorialProgress.LoadLocal();
            DocumentSnapshot snapshot = readTask.Result;
            if (snapshot.Exists)
            {
                Dictionary<string, object> cloudData = snapshot.ToDictionary();
                int cloudVersion = GetRacingTutorialInt(
                    cloudData,
                    Constants.RacingTutorial.CloudVersion,
                    Constants.RacingTutorial.CurrentVersion);
                if (cloudVersion > Constants.RacingTutorial.CurrentVersion)
                {
                    Debug.LogWarning(
                        "Racing tutorial sync skipped because Firebase has a newer version.");
                    return;
                }

                if (cloudVersion == Constants.RacingTutorial.CurrentVersion)
                {
                    var cloudState = new RacingTutorialProgress.State(
                        cloudVersion,
                        GetRacingTutorialInt(
                            cloudData,
                            Constants.RacingTutorial.CloudCompleted,
                            0) == 1,
                        (RacingTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                            GetRacingTutorialInt(
                                cloudData,
                                Constants.RacingTutorial.CloudCheckpoint,
                                (int)RacingTutorialProgress.CoreCheckpoint.LaunchMeter),
                            (int)RacingTutorialProgress.CoreCheckpoint.LaunchMeter,
                            (int)RacingTutorialProgress.CoreCheckpoint.Completed),
                        (RacingTutorialProgress.ContextLesson)GetRacingTutorialInt(
                            cloudData,
                            Constants.RacingTutorial.CloudContextFlags,
                            0),
                        (RacingTutorialProgress.ControllerLesson)GetRacingTutorialInt(
                            cloudData,
                            Constants.RacingTutorial.CloudControllerFlags,
                            0));
                    state = RacingTutorialProgress.MergeAndSave(cloudState);
                }
            }

            Dictionary<string, object> updateData = new Dictionary<string, object>
            {
                { Constants.RacingTutorial.CloudVersion, Constants.RacingTutorial.CurrentVersion },
                { Constants.RacingTutorial.CloudCompleted, state.Completed ? 1 : 0 },
                { Constants.RacingTutorial.CloudCheckpoint, (int)state.Checkpoint },
                { Constants.RacingTutorial.CloudContextFlags, (int)state.Context },
                { Constants.RacingTutorial.CloudControllerFlags, (int)state.Controllers },
                { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
            };

            userRef.SetAsync(updateData, SetOptions.MergeAll)
                .ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsFaulted || writeTask.IsCanceled)
                        Debug.LogWarning("Racing tutorial progress sync failed.");
                });
        });
    }

    private static int GetRacingTutorialInt(
        IReadOnlyDictionary<string, object> data,
        string key,
        int fallback)
    {
        if (data == null || !data.TryGetValue(key, out object value) || value == null)
            return fallback;

        try
        {
            return System.Convert.ToInt32(value);
        }
        catch
        {
            return fallback;
        }
    }
}
