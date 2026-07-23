using UnityEngine;
using System;
using System.Collections;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public int Nyufiy { get; private set; }
    public int Coin { get; private set; }

    public event Action<int> OnNyufiyChanged;
    public event Action<int> OnCoinChanged;

    private FirebaseFirestore db;
    private string uid;

    private const string PREF_NYFIY = "nyufiy";
    private const string PREF_COIN = "coin";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLocal();
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            FirebaseManager.Instance != null &&
            !string.IsNullOrEmpty(FirebaseManager.Instance.UserId)
        );

        db = FirebaseFirestore.DefaultInstance;
        uid = FirebaseManager.Instance.UserId;
    }

    public void LoadLocal()
    {
        Nyufiy = PlayerPrefs.GetInt(PREF_NYFIY, 0);
        Coin = PlayerPrefs.GetInt(PREF_COIN, 0);

        OnNyufiyChanged?.Invoke(Nyufiy);
        OnCoinChanged?.Invoke(Coin);
    }

    public void SetCurrencyFromServer(int nyufiy, int coin)
    {
        Nyufiy = nyufiy;
        Coin = coin;

        SaveLocal();

        OnNyufiyChanged?.Invoke(Nyufiy);
        OnCoinChanged?.Invoke(Coin);
    }

    public void AddNyufiy(int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return;

        Nyufiy += amount;
        SaveLocal();

        OnNyufiyChanged?.Invoke(Nyufiy);

        if (syncNow)
            SyncCurrencyFieldToFirestore(Constants.Coins.Nyufiy, Nyufiy);
    }

    public void AddCoin(int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return;

        Coin += amount;
        SaveLocal();

        OnCoinChanged?.Invoke(Coin);

        if (syncNow)
            SyncCurrencyFieldToFirestore(Constants.Coins.Coin, Coin);
    }

    public void AddCurrencyBundle(int nyufiyAmount, int coinAmount, bool syncNow = false)
    {
        if (nyufiyAmount <= 0 && coinAmount <= 0)
            return;

        if (nyufiyAmount > 0)
            Nyufiy += nyufiyAmount;
        if (coinAmount > 0)
            Coin += coinAmount;

        SaveLocal();

        if (nyufiyAmount > 0)
            OnNyufiyChanged?.Invoke(Nyufiy);
        if (coinAmount > 0)
            OnCoinChanged?.Invoke(Coin);

        if (syncNow)
            SyncToFirestore();
    }

    public bool SpendNyufiy(int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return false;

        if (Nyufiy < amount)
            return false;

        Nyufiy -= amount;
        SaveLocal();

        OnNyufiyChanged?.Invoke(Nyufiy);

        if (syncNow)
            SyncCurrencyFieldToFirestore(Constants.Coins.Nyufiy, Nyufiy);

        return true;
    }

    public bool SpendCoin(int amount, bool syncNow = false)
    {
        if (amount <= 0)
            return false;

        if (Coin < amount)
            return false;

        Coin -= amount;
        SaveLocal();

        OnCoinChanged?.Invoke(Coin);

        if (syncNow)
            SyncCurrencyFieldToFirestore(Constants.Coins.Coin, Coin);

        return true;
    }

    private void SaveLocal()
    {
        PlayerPrefs.SetInt(PREF_NYFIY, Nyufiy);
        PlayerPrefs.SetInt(PREF_COIN, Coin);
        PlayerPrefs.Save();
    }

    public void SyncToFirestore()
    {
        if (!EnsureFirebaseReady())
        {
            Debug.LogWarning("Currency sync failed: Firebase not ready");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);

        Dictionary<string, object> updateData = new Dictionary<string, object>
        {
            { Constants.Coins.Nyufiy, Nyufiy },
            { Constants.Coins.Coin, Coin },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        userRef.SetAsync(updateData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Currency sync failed: " + task.Exception);
                return;
            }

            Debug.Log("Currency synced to Firestore");
        });
    }

    private void SyncCurrencyFieldToFirestore(string fieldName, int value)
    {
        if (!EnsureFirebaseReady())
        {
            Debug.LogWarning("Currency sync failed: Firebase not ready");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(uid);

        Dictionary<string, object> updateData = new Dictionary<string, object>
        {
            { fieldName, value },
            { Constants.Others.UpdatedAt, FieldValue.ServerTimestamp }
        };

        userRef.SetAsync(updateData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Currency sync failed: " + task.Exception);
                return;
            }

            Debug.Log($"Currency field synced to Firestore: {fieldName} = {value}");
        });
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

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveLocal();
            SyncToFirestore();
        }
    }

    private void OnApplicationQuit()
    {
        SaveLocal();
        SyncToFirestore();
    }

   
}
