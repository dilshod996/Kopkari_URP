using Firebase;
using Firebase.Analytics;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public bool IsReady { get; private set; }
    public FirebaseAuth Auth { get; private set; }
    public FirebaseUser User { get; private set; }

    public string UserId => User != null ? User.UserId : "";
    public bool IsSignedIn => User != null && !string.IsNullOrEmpty(User.UserId);
    public event Action<string> OnUserSignedIn;
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

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus status = task.Result;

            if (status == DependencyStatus.Available)
            {
                Auth = FirebaseAuth.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

                IsReady = true;
                Debug.Log("Firebase Analytics initialized");

                FirebaseAnalytics.LogEvent("game_open");
                SignInAnonymous();
            }
            else
            {
                IsReady = false;
                Debug.LogError("Firebase dependencies could not be resolved: " + status);
            }
        });
    }
    private void SignInAnonymous()
    {
        if (Auth.CurrentUser != null)
        {
            CompleteSignIn(Auth.CurrentUser, "user_login_anonymous_existing");
            return;
        }

        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Anonymous sign in failed: " + task.Exception);
                return;
            }

            CompleteSignIn(task.Result.User, "user_login_anonymous_new");
        });
    }

    private void CompleteSignIn(FirebaseUser user, string analyticsEvent)
    {
        User = user;

        Debug.Log("Firebase user signed in: " + User.UserId);

        FirebaseAnalytics.SetUserId(User.UserId);
        FirebaseAnalytics.LogEvent(analyticsEvent);
        OnUserSignedIn?.Invoke(User.UserId);
    }
}
