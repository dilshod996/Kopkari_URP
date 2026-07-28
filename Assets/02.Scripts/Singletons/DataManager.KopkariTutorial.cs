using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public partial class DataManager
{
    private Coroutine kopkariTutorialLoadRoutine;
    private Coroutine kopkariTutorialSyncRoutine;
    private Coroutine kopkariTutorialDeleteRoutine;

    public bool IsKopkariTutorialStateLoaded { get; private set; }

    public void EnsureKopkariTutorialStateLoaded()
    {
        if (IsKopkariTutorialStateLoaded || kopkariTutorialLoadRoutine != null)
            return;

        kopkariTutorialLoadRoutine = StartCoroutine(LoadKopkariTutorialState());
    }

    public void QueueKopkariTutorialProgressSync()
    {
        if (kopkariTutorialSyncRoutine != null)
            StopCoroutine(kopkariTutorialSyncRoutine);

        kopkariTutorialSyncRoutine = StartCoroutine(SyncKopkariTutorialAfterDelay(2f));
    }

    public void CompleteKopkariTutorial()
    {
        if (kopkariTutorialSyncRoutine != null)
        {
            StopCoroutine(kopkariTutorialSyncRoutine);
            kopkariTutorialSyncRoutine = null;
        }

        kopkariTutorialSyncRoutine = StartCoroutine(SyncKopkariTutorialAfterDelay(0f));
    }

    public void DeleteKopkariTutorialProgressForTesting()
    {
        if (kopkariTutorialLoadRoutine != null)
        {
            StopCoroutine(kopkariTutorialLoadRoutine);
            kopkariTutorialLoadRoutine = null;
        }

        if (kopkariTutorialSyncRoutine != null)
        {
            StopCoroutine(kopkariTutorialSyncRoutine);
            kopkariTutorialSyncRoutine = null;
        }

        if (kopkariTutorialDeleteRoutine != null)
            StopCoroutine(kopkariTutorialDeleteRoutine);

        KopkariTutorialProgress.DeleteAllLocalProgress();
        IsKopkariTutorialStateLoaded = false;
        kopkariTutorialDeleteRoutine =
            StartCoroutine(DeleteKopkariTutorialProgressFromFirebase());
    }

    private IEnumerator DeleteKopkariTutorialProgressFromFirebase()
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
                "Registan tutorial PlayerPrefs were deleted, but Firebase was not ready.");
            kopkariTutorialDeleteRoutine = null;
            yield break;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);
        var deleteTask = userRef.UpdateAsync(new Dictionary<string, object>
        {
            { Constants.KopkariTutorial.CloudCompleted, FieldValue.Delete },
            { Constants.KopkariTutorial.CloudVersion, FieldValue.Delete },
            { Constants.KopkariTutorial.CloudCheckpoint, FieldValue.Delete },
            { Constants.KopkariTutorial.CloudContextFlags, FieldValue.Delete }
        });

        while (!deleteTask.IsCompleted)
            yield return null;

        if (deleteTask.IsFaulted || deleteTask.IsCanceled)
        {
            Debug.LogWarning(
                "Registan tutorial PlayerPrefs were deleted, but Firebase deletion failed.");
        }
        else
        {
            Debug.Log(
                "Registan tutorial progress was deleted from PlayerPrefs and Firebase.");
        }

        kopkariTutorialDeleteRoutine = null;
    }

    private IEnumerator LoadKopkariTutorialState()
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
            IsKopkariTutorialStateLoaded = true;
            kopkariTutorialLoadRoutine = null;
            yield break;
        }

        var task = db.Collection("users").Document(uid).GetSnapshotAsync();
        while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (!task.IsCompleted || task.IsFaulted || task.IsCanceled)
        {
            Debug.LogWarning("Kopkari tutorial progress could not be loaded from Firebase.");
            IsKopkariTutorialStateLoaded = true;
            kopkariTutorialLoadRoutine = null;
            yield break;
        }

        DocumentSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            Dictionary<string, object> data = snapshot.ToDictionary();
            int cloudVersion = GetTutorialInt(
                data,
                Constants.KopkariTutorial.CloudVersion,
                Constants.KopkariTutorial.CurrentVersion);
            if (cloudVersion > Constants.KopkariTutorial.CurrentVersion)
            {
                Debug.LogWarning(
                    "Kopkari tutorial cloud progress uses a newer version; local data was not uploaded.");
                IsKopkariTutorialStateLoaded = true;
                kopkariTutorialLoadRoutine = null;
                yield break;
            }
            bool compatibleVersion =
                cloudVersion == Constants.KopkariTutorial.CurrentVersion;

            var cloudState = new KopkariTutorialProgress.State(
                Constants.KopkariTutorial.CurrentVersion,
                compatibleVersion &&
                GetTutorialInt(data, Constants.KopkariTutorial.CloudCompleted, 0) == 1,
                compatibleVersion
                    ? (KopkariTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                        GetTutorialInt(
                            data,
                            Constants.KopkariTutorial.CloudCheckpoint,
                            (int)KopkariTutorialProgress.CoreCheckpoint.Joystick),
                        (int)KopkariTutorialProgress.CoreCheckpoint.Joystick,
                        (int)KopkariTutorialProgress.CoreCheckpoint.Completed)
                    : KopkariTutorialProgress.CoreCheckpoint.Joystick,
                compatibleVersion
                    ? (KopkariTutorialProgress.ContextLesson)GetTutorialInt(
                        data,
                        Constants.KopkariTutorial.CloudContextFlags,
                        0)
                    : KopkariTutorialProgress.ContextLesson.None);

            KopkariTutorialProgress.State merged =
                KopkariTutorialProgress.MergeAndSave(cloudState);

            bool cloudMissingFields =
                !data.ContainsKey(Constants.KopkariTutorial.CloudVersion) ||
                !data.ContainsKey(Constants.KopkariTutorial.CloudCompleted) ||
                !data.ContainsKey(Constants.KopkariTutorial.CloudCheckpoint) ||
                !data.ContainsKey(Constants.KopkariTutorial.CloudContextFlags);
            bool localWasAhead =
                merged.Completed != cloudState.Completed ||
                merged.Checkpoint != cloudState.Checkpoint ||
                merged.Context != cloudState.Context;

            if (cloudMissingFields || localWasAhead || !compatibleVersion)
                SyncKopkariTutorialProgress();
        }

        IsKopkariTutorialStateLoaded = true;
        kopkariTutorialLoadRoutine = null;
    }

    private IEnumerator SyncKopkariTutorialAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float deadline = Time.realtimeSinceStartup + 10f;
        while ((db == null || string.IsNullOrEmpty(uid)) &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        kopkariTutorialSyncRoutine = null;
        SyncKopkariTutorialProgress();
    }

    private void SyncKopkariTutorialProgress()
    {
        if (db == null || string.IsNullOrEmpty(uid))
            return;

        DocumentReference userRef = db.Collection("users").Document(uid);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(readTask =>
        {
            if (readTask.IsFaulted || readTask.IsCanceled)
            {
                Debug.LogWarning("Kopkari tutorial progress merge failed.");
                return;
            }

            KopkariTutorialProgress.State state = KopkariTutorialProgress.LoadLocal();
            DocumentSnapshot snapshot = readTask.Result;
            if (snapshot.Exists)
            {
                Dictionary<string, object> cloudData = snapshot.ToDictionary();
                int cloudVersion = GetTutorialInt(
                    cloudData,
                    Constants.KopkariTutorial.CloudVersion,
                    Constants.KopkariTutorial.CurrentVersion);
                if (cloudVersion > Constants.KopkariTutorial.CurrentVersion)
                {
                    Debug.LogWarning(
                        "Kopkari tutorial sync skipped because Firebase has a newer version.");
                    return;
                }
                if (cloudVersion == Constants.KopkariTutorial.CurrentVersion)
                {
                    var cloudState = new KopkariTutorialProgress.State(
                        cloudVersion,
                        GetTutorialInt(
                            cloudData,
                            Constants.KopkariTutorial.CloudCompleted,
                            0) == 1,
                        (KopkariTutorialProgress.CoreCheckpoint)Mathf.Clamp(
                            GetTutorialInt(
                                cloudData,
                                Constants.KopkariTutorial.CloudCheckpoint,
                                (int)KopkariTutorialProgress.CoreCheckpoint.Joystick),
                            (int)KopkariTutorialProgress.CoreCheckpoint.Joystick,
                            (int)KopkariTutorialProgress.CoreCheckpoint.Completed),
                        (KopkariTutorialProgress.ContextLesson)GetTutorialInt(
                            cloudData,
                            Constants.KopkariTutorial.CloudContextFlags,
                            0));
                    state = KopkariTutorialProgress.MergeAndSave(cloudState);
                }
            }

            Dictionary<string, object> updateData = new Dictionary<string, object>
            {
                {
                    Constants.KopkariTutorial.CloudVersion,
                    Constants.KopkariTutorial.CurrentVersion
                },
                {
                    Constants.KopkariTutorial.CloudCompleted,
                    state.Completed ? 1 : 0
                },
                {
                    Constants.KopkariTutorial.CloudCheckpoint,
                    (int)state.Checkpoint
                },
                {
                    Constants.KopkariTutorial.CloudContextFlags,
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
                        Debug.LogWarning("Kopkari tutorial progress sync failed.");
                });
        });
    }

    private static int GetTutorialInt(
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
