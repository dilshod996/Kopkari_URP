using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public partial class DataManager
{
    private Coroutine homeTutorialLoadRoutine;
    private Coroutine homeTutorialSyncRoutine;
    private Coroutine homeTutorialDeleteRoutine;

    public bool IsHomeTutorialStateLoaded { get; private set; }

    public void EnsureHomeTutorialStateLoaded()
    {
        if (IsHomeTutorialStateLoaded || homeTutorialLoadRoutine != null)
            return;

        homeTutorialLoadRoutine = StartCoroutine(LoadHomeTutorialState());
    }

    public void QueueHomeTutorialProgressSync()
    {
        if (homeTutorialSyncRoutine != null)
            StopCoroutine(homeTutorialSyncRoutine);

        homeTutorialSyncRoutine =
            StartCoroutine(SyncHomeTutorialAfterDelay(2f));
    }

    public void CompleteHomeTutorial()
    {
        if (homeTutorialSyncRoutine != null)
        {
            StopCoroutine(homeTutorialSyncRoutine);
            homeTutorialSyncRoutine = null;
        }

        homeTutorialSyncRoutine =
            StartCoroutine(SyncHomeTutorialAfterDelay(0f));
    }

    public void DeleteHomeTutorialProgressForTesting()
    {
        if (homeTutorialLoadRoutine != null)
        {
            StopCoroutine(homeTutorialLoadRoutine);
            homeTutorialLoadRoutine = null;
        }

        if (homeTutorialSyncRoutine != null)
        {
            StopCoroutine(homeTutorialSyncRoutine);
            homeTutorialSyncRoutine = null;
        }

        if (homeTutorialDeleteRoutine != null)
            StopCoroutine(homeTutorialDeleteRoutine);

        HomeTutorialProgress.DeleteAllLocalProgress();
        IsHomeTutorialStateLoaded = false;
        homeTutorialDeleteRoutine =
            StartCoroutine(DeleteHomeTutorialProgressFromFirebase());
    }

    private IEnumerator DeleteHomeTutorialProgressFromFirebase()
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
                "Home tutorial PlayerPrefs were deleted, but Firebase was not ready.");
            homeTutorialDeleteRoutine = null;
            yield break;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);
        var deleteTask = userRef.UpdateAsync(
            new Dictionary<string, object>
            {
                { Constants.HomeTutorial.CloudCompleted, FieldValue.Delete },
                { Constants.HomeTutorial.CloudVersion, FieldValue.Delete },
                { Constants.HomeTutorial.CloudCheckpoint, FieldValue.Delete },
                { Constants.HomeTutorial.CloudContextFlags, FieldValue.Delete }
            });

        while (!deleteTask.IsCompleted)
            yield return null;

        if (deleteTask.IsFaulted || deleteTask.IsCanceled)
        {
            Debug.LogWarning(
                "Home tutorial PlayerPrefs were deleted, but Firebase deletion failed.");
        }
        else
        {
            Debug.Log(
                "Home tutorial progress was deleted from PlayerPrefs and Firebase.");
        }

        homeTutorialDeleteRoutine = null;
    }

    private IEnumerator LoadHomeTutorialState()
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
            FinishHomeTutorialLoad();
            yield break;
        }

        var task = db.Collection("users").Document(uid).GetSnapshotAsync();
        while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
        {
            Debug.LogWarning(
                "Home tutorial progress could not be loaded from Firebase.");
            FinishHomeTutorialLoad();
            yield break;
        }

        DocumentSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();
            int cloudVersion = GetHomeTutorialInt(
                data,
                Constants.HomeTutorial.CloudVersion,
                Constants.HomeTutorial.CurrentVersion);

            if (cloudVersion > Constants.HomeTutorial.CurrentVersion)
            {
                Debug.LogWarning(
                    "Home tutorial cloud progress uses a newer version; local data was not uploaded.");
                FinishHomeTutorialLoad();
                yield break;
            }

            bool compatibleVersion =
                cloudVersion == Constants.HomeTutorial.CurrentVersion;
            var cloudState = new HomeTutorialProgress.State(
                Constants.HomeTutorial.CurrentVersion,
                compatibleVersion &&
                GetHomeTutorialInt(
                    data,
                    Constants.HomeTutorial.CloudCompleted,
                    0) == 1,
                compatibleVersion
                    ? (HomeTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                        GetHomeTutorialInt(
                            data,
                            Constants.HomeTutorial.CloudCheckpoint,
                            (int)HomeTutorialProgress.CoreCheckpoint.Settings),
                        (int)HomeTutorialProgress.CoreCheckpoint.Settings,
                        (int)HomeTutorialProgress.CoreCheckpoint.Completed)
                    : HomeTutorialProgress.CoreCheckpoint.Settings,
                compatibleVersion
                    ? (HomeTutorialProgress.ContextLesson)GetHomeTutorialInt(
                        data,
                        Constants.HomeTutorial.CloudContextFlags,
                        0)
                    : HomeTutorialProgress.ContextLesson.None);

            HomeTutorialProgress.State merged =
                HomeTutorialProgress.MergeAndSave(cloudState);
            bool cloudMissingFields =
                !data.ContainsKey(Constants.HomeTutorial.CloudVersion) ||
                !data.ContainsKey(Constants.HomeTutorial.CloudCompleted) ||
                !data.ContainsKey(Constants.HomeTutorial.CloudCheckpoint) ||
                !data.ContainsKey(Constants.HomeTutorial.CloudContextFlags);
            bool localWasAhead =
                merged.Completed != cloudState.Completed ||
                merged.Checkpoint != cloudState.Checkpoint ||
                merged.Context != cloudState.Context;

            if (cloudMissingFields || localWasAhead || !compatibleVersion)
                SyncHomeTutorialProgress();
        }

        FinishHomeTutorialLoad();
    }

    private void FinishHomeTutorialLoad()
    {
        IsHomeTutorialStateLoaded = true;
        homeTutorialLoadRoutine = null;
    }

    private IEnumerator SyncHomeTutorialAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float deadline = Time.realtimeSinceStartup + 10f;
        while ((db == null || string.IsNullOrEmpty(uid)) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        homeTutorialSyncRoutine = null;
        SyncHomeTutorialProgress();
    }

    private void SyncHomeTutorialProgress()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        DocumentReference userRef = db.Collection("users").Document(uid);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
        {
            if (readTask.IsFaulted || readTask.IsCanceled)
            {
                Debug.LogWarning("Home tutorial progress merge failed.");
                return;
            }

            HomeTutorialProgress.State state =
                HomeTutorialProgress.LoadLocal();
            DocumentSnapshot snapshot = readTask.Result;
            if (snapshot.Exists)
            {
                Dictionary<string, object> cloudData =
                    snapshot.ToDictionary();
                int cloudVersion = GetHomeTutorialInt(
                    cloudData,
                    Constants.HomeTutorial.CloudVersion,
                    Constants.HomeTutorial.CurrentVersion);

                if (cloudVersion > Constants.HomeTutorial.CurrentVersion)
                {
                    Debug.LogWarning(
                        "Home tutorial sync skipped because Firebase has a newer version.");
                    return;
                }

                if (cloudVersion == Constants.HomeTutorial.CurrentVersion)
                {
                    var cloudState = new HomeTutorialProgress.State(
                        cloudVersion,
                        GetHomeTutorialInt(
                            cloudData,
                            Constants.HomeTutorial.CloudCompleted,
                            0) == 1,
                        (HomeTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                            GetHomeTutorialInt(
                                cloudData,
                                Constants.HomeTutorial.CloudCheckpoint,
                                (int)HomeTutorialProgress.CoreCheckpoint.Settings),
                            (int)HomeTutorialProgress.CoreCheckpoint.Settings,
                            (int)HomeTutorialProgress.CoreCheckpoint.Completed),
                        (HomeTutorialProgress.ContextLesson)GetHomeTutorialInt(
                            cloudData,
                            Constants.HomeTutorial.CloudContextFlags,
                            0));
                    state = HomeTutorialProgress.MergeAndSave(cloudState);
                }
            }

            var updateData = new Dictionary<string, object>
            {
                {
                    Constants.HomeTutorial.CloudVersion,
                    Constants.HomeTutorial.CurrentVersion
                },
                {
                    Constants.HomeTutorial.CloudCompleted,
                    state.Completed ? 1 : 0
                },
                {
                    Constants.HomeTutorial.CloudCheckpoint,
                    (int)state.Checkpoint
                },
                {
                    Constants.HomeTutorial.CloudContextFlags,
                    (int)state.Context
                },
                {
                    Constants.Others.UpdatedAt,
                    FieldValue.ServerTimestamp
                }
            };

            userRef.SetAsync(updateData, SetOptions.MergeAll)
                .ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsFaulted || writeTask.IsCanceled)
                    {
                        Debug.LogWarning(
                            "Home tutorial progress sync failed.");
                    }
                });
        });
    }

    private static int GetHomeTutorialInt(
        IReadOnlyDictionary<string, object> data,
        string key,
        int fallback)
    {
        if (data == null ||
            !data.TryGetValue(key, out object value) ||
            value == null)
        {
            return fallback;
        }

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
