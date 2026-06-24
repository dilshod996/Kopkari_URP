using System;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Use Google Test Ads")]
    [SerializeField] private bool useTestAds = true;

    [Header("Android Real Ad Unit IDs")]
    [SerializeField] private string androidBannerId;
    [SerializeField] private string androidInterstitialId;
    [SerializeField] private string androidRewardedId;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private Action pendingRewardedUnavailable;

    private int raceFinishCounter;
    public event Action OnRewardedUnavailable;

    private const string TEST_ANDROID_BANNER = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_ANDROID_INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_ANDROID_REWARDED = "ca-app-pub-3940256099942544/5224354917";

    private string BannerAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return useTestAds ? TEST_ANDROID_BANNER : androidBannerId;
#else
            return "unused";
#endif
        }
    }

    private string InterstitialAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return useTestAds ? TEST_ANDROID_INTERSTITIAL : androidInterstitialId;
#else
            return "unused";
#endif
        }
    }

    private string RewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return useTestAds ? TEST_ANDROID_REWARDED : androidRewardedId;
#else
            return "unused";
#endif
        }
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

    private void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob initialized");

            LoadInterstitial();
            LoadRewarded();
        });
    }

    // -------------------------
    // Banner
    // -------------------------

    public void ShowBanner()
    {
        HideBanner();

        bannerView = new BannerView(BannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        bannerView.LoadAd(new AdRequest());

        Debug.Log("Banner requested");
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    // -------------------------
    // Interstitial
    // -------------------------

    public void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        InterstitialAd.Load(InterstitialAdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Interstitial failed to load: " + error);
                return;
            }

            interstitialAd = ad;
            RegisterInterstitialEvents(interstitialAd);

            Debug.Log("Interstitial loaded");
        });
    }

    private void RegisterInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial closed");
            LoadInterstitial();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Interstitial failed to show: " + error);
            LoadInterstitial();
        };
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("Interstitial not ready");
            LoadInterstitial();
        }
    }

    public void ShowInterstitialAfterRace()
    {
        raceFinishCounter++;

        // Har 3 ta race tugaganda 1 marta reklama
        if (raceFinishCounter >= 3)
        {
            raceFinishCounter = 0;
            ShowInterstitial();
        }
    }

    // -------------------------
    // Rewarded
    // -------------------------

    public void LoadRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(RewardedAdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded failed to load: " + error);
                return;
            }

            rewardedAd = ad;
            RegisterRewardedEvents(rewardedAd);

            Debug.Log("Rewarded loaded");
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded closed");
            pendingRewardedUnavailable = null;
            LoadRewarded();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Rewarded failed to show: " + error);
            pendingRewardedUnavailable?.Invoke();
            pendingRewardedUnavailable = null;
            OnRewardedUnavailable?.Invoke();
            LoadRewarded();
        };
    }

    public void ShowRewarded(Action onRewardEarned, Action onRewardUnavailable = null)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            pendingRewardedUnavailable = onRewardUnavailable;
            rewardedAd.Show(reward =>
            {
                Debug.Log("Reward earned: " + reward.Amount + " " + reward.Type);
                pendingRewardedUnavailable = null;
                onRewardEarned?.Invoke();
            });
        }
        else
        {
            Debug.Log("Rewarded not ready");
            onRewardUnavailable?.Invoke();
            OnRewardedUnavailable?.Invoke();
            LoadRewarded();
        }
    }
}
