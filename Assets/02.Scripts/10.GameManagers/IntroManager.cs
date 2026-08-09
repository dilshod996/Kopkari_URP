using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Kopkari
{
    public class IntroManager : MonoBehaviour
    {

        //SingletonScene
        public static IntroManager Instance;
        [SerializeField] GameObject startingPage;
        [SerializeField] VideoPlayer videoPlayer;

        [SerializeField] TMP_Text skipBtnText;
        [SerializeField] TMP_Text versionText;
        // Popup
        [Header("Popup")]
        public ModalWindowManager notificationManager;
        public ProgressBar progressBar;

        [SerializeField] private Button skipButton;
        //Intro scene addressable addresses
        private List<string> myAddresses = new List<string> { "IntroSound", "IntroVideo" };
        private List<string> homePreloadAddresses;
        private Task<bool> homePreloadTask;
        private bool introContentLoading;
        private bool introContentReady;
        private bool lobbyLoadStarted;
        private Coroutine homePreloadReadyRoutine;
        private Coroutine videoStartupTimeoutRoutine;
        private bool videoStarted;
        private bool introRetryPopupVisible;
        private bool homeRetryPopupVisible;

        private const float VideoStartupTimeoutSeconds = 15f;

        [Header("User Details")]
        // private const string UsernameKey = "username";
        private const string DefaultUsername = "Player_123";
        private const string CountryName = "countryName";
        private const string FirstTimeKey = "firstTime";
        private const string PlayerFaceKey = "Player_Face";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
                Destroy(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
        private async void Start()
        {
            if(SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.CurrentSceneType = SceneLoadManager.SceneType.Intro;
            }
            if(versionText != null)
                versionText.text = $"v-{Application.version}";
            if (notificationManager != null)
                notificationManager.onConfirm.AddListener(RetryIntroContentLoad);

            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += OnVideoFinished;
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.started += OnVideoStarted;
                videoPlayer.errorReceived += OnVideoError;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            }

            //PlayerPrefs.DeleteAll();

            //PlayerMaterialsData();
            Debug.Log("System Language: " + Application.systemLanguage.ToString());
            if (skipButton != null)
                skipButton.onClick.AddListener(LoadLobbyScene);

            SetSkipAvailable(false);

            homePreloadAddresses = GetPreloadMaterialAddresses();

            Task<bool> languageInitializationTask = null;
            if (LanguageManager.Instance != null)
                languageInitializationTask = LanguageManager.Instance.InitializeAsync();

            // Localization and intro media do not need to block one another.
            GetAddressableData();

            if (languageInitializationTask != null)
            {
                try
                {
                    await languageInitializationTask;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            if (this != null)
            {
                SetInitialLanguage();

                if (PlayerCatalogProvider.Instance != null)
                    await PlayerCatalogProvider.Instance.CacheSelectedHorsePresentationAsync(
                        downloadIcon: false);
            }
            skipBtnText.text = LanguageManager.Instance != null
                ? LanguageManager.Instance.GetText(553)
                : "Skip";
        }
        private void OnDestroy()
        {
            if (notificationManager != null)
                notificationManager.onConfirm.RemoveListener(RetryIntroContentLoad);

            if (skipButton != null)
                skipButton.onClick.RemoveListener(LoadLobbyScene);

            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.started -= OnVideoStarted;
                videoPlayer.errorReceived -= OnVideoError;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
        public void SetInitialLanguage()
        {
            if (LanguageManager.Instance == null)
                return;

            if (!PlayerPrefs.HasKey(Constants.GameSettings.LanguageKey))
            {
                SystemLanguage deviceLang = Application.systemLanguage;
                string langCode = GetLangCode(deviceLang);
                PlayerPrefs.SetString(Constants.GameSettings.LanguageKey, langCode);
                Debug.Log("Initial Language Set: " + langCode);
            }

            // Har doim uni LanguageManager ga uzatamiz
            string savedLang = PlayerPrefs.GetString(Constants.GameSettings.LanguageKey);
            LanguageManager.Instance.SetLanguage(savedLang);
        }

        string GetLangCode(SystemLanguage lang)
        {
            string langName = lang.ToString().ToLower(); // "english", "russian", "uzbek"...

            switch (langName)
            {
                case "english": return "en";
                case "russian": return "ru";
                case "uzbek":
                case "uz":
                case "uzbekcyrillic": return "uz";
                default: return "en"; // fallback
            }
        }


        #region Get Intro Video
        public async void GetAddressableData()
        {
            try
            {
                await GetAddressableDataAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (this != null && !lobbyLoadStarted)
                    FailIntroContentLoad();
            }
        }

        private async Task GetAddressableDataAsync()
        {
            if (introContentLoading || introContentReady)
                return;

            if (AddressablesService.Instance == null)
            {
                ShowIntroRetryPopup();
                return;
            }

            introContentLoading = true;

            // 1) Preload/download + progress
            bool ok = await AddressablesService.Instance.PreloadDependenciesAsync(
                myAddresses,
                p =>
                {
                    if (progressBar != null)
                    {
                        progressBar.currentPercent = p * 100f;
                        progressBar.UpdateUI();
                    }
                },
                fakeDurationIfCached: 0f,
                showErrorPopup: false
            );
            introContentLoading = false;

            if (lobbyLoadStarted || this == null)
                return;

            if (!ok)
            {
                Debug.LogWarning("❌ Preload failed (no internet or download error).");
                ShowIntroRetryPopup();
                return;
            }

            // 2) Keylar bo‘yicha assetlarni load qilib ishlatamiz
            var video = await AddressablesService.Instance.LoadAssetAsync<VideoClip>(
                Constants.VideoClips.IntroVideo,
                showErrorPopup: false);
            if (lobbyLoadStarted || this == null)
                return;

            if (video == null || videoPlayer == null)
            {
                FailIntroContentLoad();
                return;
            }

            var audio = await AddressablesService.Instance.LoadAssetAsync<AudioClip>(
                Constants.RoomSound.IntroSound,
                showErrorPopup: false);
            if (lobbyLoadStarted || this == null)
                return;

            if (audio == null)
            {
                FailIntroContentLoad();
                return;
            }

            SoundManager.Instance?.PlayRoom(audio);

            videoStarted = false;
            videoPlayer.clip = video;
            videoPlayer.Prepare();
            StartVideoStartupTimeout();
            StartHomePreloadDuringVideo();
            Debug.Log($"Preparing intro video: {Constants.VideoClips.IntroVideo}");
        }
        public void GetIntroVideo()
        {
            RetryIntroContentLoad();
        }
        #endregion

        #region Notification Popup
        public void RetryInitWithPopup()
        {
            RetryIntroContentLoad();
        }

        public void ShowPopup()
        {
            ShowIntroRetryPopup();
        }

        private void RetryIntroContentLoad()
        {
            if (lobbyLoadStarted)
                return;

            introRetryPopupVisible = false;
            introContentReady = false;
            videoStarted = false;
            SetSkipAvailable(false);
            GetAddressableData();
        }

        private void ShowIntroRetryPopup()
        {
            if (introRetryPopupVisible || lobbyLoadStarted)
                return;

            introRetryPopupVisible = true;

            if (UIOverlayRoot.I != null)
            {
                UIOverlayRoot.I.Done(
                    "Download failed",
                    "Could not download required intro content. Please check your internet connection and try again.",
                    "Retry",
                    RetryIntroContentLoad
                );
                return;
            }

            if (notificationManager != null)
            {
                notificationManager.UpdateUICustom(
                    "Internetda xatolik",
                    "Internet mavjud emas. Iltimos internetni yoqilganiga ishonch hosil qiling"
                );
            }
        }

        private void FailIntroContentLoad()
        {
            introContentLoading = false;
            introContentReady = false;
            videoStarted = false;
            SetSkipAvailable(false);
            StopVideoStartupTimeout();

            if (videoPlayer != null)
                videoPlayer.Stop();

            SoundManager.Instance?.StopRoom();

            if (startingPage != null)
                startingPage.SetActive(true);

            ShowIntroRetryPopup();
        }

        private void StartHomePreloadDuringVideo()
        {
            if (AddressablesService.Instance == null)
                return;

            if (homePreloadTask != null && !homePreloadTask.IsCompleted)
                return;

            if (IsSuccessful(homePreloadTask))
                return;

            homePreloadAddresses ??= GetPreloadMaterialAddresses();
            homePreloadTask = AddressablesService.Instance.PreloadDependenciesAsync(
                homePreloadAddresses,
                p =>
                {
                    if (SceneLoadManager.Instance != null)
                        SceneLoadManager.Instance.loadingTime = p * 100f;
                },
                fakeDurationIfCached: 0f,
                showErrorPopup: false
            );

            if (homePreloadReadyRoutine != null)
                StopCoroutine(homePreloadReadyRoutine);

            homePreloadReadyRoutine = StartCoroutine(ShowSkipWhenHomePreloadReady());
        }

        private Task<bool> GetReusableHomePreloadTask()
        {
            if (homePreloadTask == null)
                return null;

            if (!homePreloadTask.IsCompleted)
                return homePreloadTask;

            return IsSuccessful(homePreloadTask) ? homePreloadTask : null;
        }

        private bool IsSuccessful(Task<bool> task)
        {
            return task != null
                && task.IsCompleted
                && !task.IsCanceled
                && !task.IsFaulted
                && task.Result;
        }

        private IEnumerator ShowSkipWhenHomePreloadReady()
        {
            while (homePreloadTask != null && !homePreloadTask.IsCompleted)
                yield return null;

            homePreloadReadyRoutine = null;

            if (!lobbyLoadStarted && IsSuccessful(homePreloadTask))
                SetSkipAvailable(true);
        }

        private void SetSkipAvailable(bool available)
        {
            if (skipButton != null)
            {
                bool canSkip = available
                    && introContentReady
                    && !lobbyLoadStarted
                    && IsSuccessful(homePreloadTask);

                skipButton.interactable = canSkip;
                skipButton.gameObject.SetActive(canSkip);
            }
        }

        #endregion

        #region Moving To Lobby

        public void LoadLobbyScene()
        {
            if (lobbyLoadStarted)
                return;

            homePreloadAddresses ??= GetPreloadMaterialAddresses();

            if (SceneLoadManager.Instance == null)
            {
                HandleHomeLoadFailed("The scene loader is unavailable.");
                return;
            }

            lobbyLoadStarted = true;
            SetSkipAvailable(false);

            bool loadStarted = SceneLoadManager.Instance.LoadHomeFromIntro(
                SceneLoadManager.SceneType.Home,
                homePreloadAddresses,
                GetReusableHomePreloadTask()
            );

            if (!loadStarted)
            {
                lobbyLoadStarted = false;
                HandleHomeLoadFailed("Another scene transition is already in progress.");
                return;
            }

            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.Stop();
            }
        }

        public void HandleHomeLoadFailed(string reason)
        {
            lobbyLoadStarted = false;
            SetSkipAvailable(false);

            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.Stop();
            }

            if (startingPage != null)
                startingPage.SetActive(true);

            if (homeRetryPopupVisible)
                return;

            homeRetryPopupVisible = true;
            Debug.LogError($"Home loading failed from Intro: {reason}");

            if (UIOverlayRoot.I != null)
            {
                UIOverlayRoot.I.Done(
                    "Home loading failed",
                    "Could not load required Home content. Please check your connection and try again.",
                    "Retry",
                    () =>
                    {
                        homeRetryPopupVisible = false;
                        LoadLobbyScene();
                    }
                );
            }
        }
        #endregion

        void OnVideoFinished(VideoPlayer vp)
        {
            LoadLobbyScene();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            if (!lobbyLoadStarted)
                vp.Play();
        }

        private void OnVideoStarted(VideoPlayer vp)
        {
            if (lobbyLoadStarted)
                return;

            videoStarted = true;
            introContentReady = true;
            StopVideoStartupTimeout();

            if (startingPage != null)
                startingPage.SetActive(false);

            if (IsSuccessful(homePreloadTask))
                SetSkipAvailable(true);

            Debug.Log($"Intro video started: {Constants.VideoClips.IntroVideo}");
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            if (lobbyLoadStarted)
                return;

            Debug.LogError($"Intro video playback failed: {message}");
            FailIntroContentLoad();
        }

        private void StartVideoStartupTimeout()
        {
            StopVideoStartupTimeout();
            videoStartupTimeoutRoutine = StartCoroutine(VideoStartupTimeoutRoutine());
        }

        private void StopVideoStartupTimeout()
        {
            if (videoStartupTimeoutRoutine == null)
                return;

            StopCoroutine(videoStartupTimeoutRoutine);
            videoStartupTimeoutRoutine = null;
        }

        private IEnumerator VideoStartupTimeoutRoutine()
        {
            float deadline = Time.realtimeSinceStartup + VideoStartupTimeoutSeconds;
            while (!videoStarted && !lobbyLoadStarted && Time.realtimeSinceStartup < deadline)
                yield return null;

            videoStartupTimeoutRoutine = null;

            if (!videoStarted && !lobbyLoadStarted)
            {
                Debug.LogError($"Intro video did not start within {VideoStartupTimeoutSeconds:0} seconds.");
                FailIntroContentLoad();
            }
        }

        private void OnDisable()
        {
            StopVideoStartupTimeout();

            if (AddressablesService.Instance != null)
            {
                foreach (var addr in myAddresses)
                    AddressablesService.Instance.ReleaseLoadedAsset(addr);
            }
        }
        #region Player Data

        private void PlayerMaterialsData()
        {
            if (!PlayerPrefs.HasKey(FirstTimeKey))
            {
                //PlayerPrefs.SetInt(Constants.Player.FirstTimeKey, 1);
                ////Save default player materials
                //PlayerPrefs.SetString(Constants.Player.PlayerFaceHairKey, "FaceHair4");
                //PlayerPrefs.SetString(Constants.Player.PlayerHeadKey, "Head");
                //PlayerPrefs.SetString(Constants.Player.PlayerHelmetKey, "Hat3");
                //PlayerPrefs.SetString(Constants.Player.PlayerHand, "Hands");
                //PlayerPrefs.SetString(Constants.Player.PlayerUpperBodyKey, "UpperBody2_1");
                //PlayerPrefs.SetString(Constants.Player.PlayerLowerBodyKey, "LowerBody1_2");
 
                //Save default horse materials

                //PlayerPrefs.SetString(Constants.Horse.HorseBodyKey, "HorseYellowBlack");
                //PlayerPrefs.SetString(Constants.Horse.HorseEyesKey, "HorseEyes");
                //PlayerPrefs.SetString(Constants.Horse.HorseManeKey, "HorseManeBlack");
                //PlayerPrefs.SetString(Constants.Horse.HorseTailKey, "HorseManeBlack");
                //PlayerPrefs.SetString(Constants.Horse.HorseReinsKey, "Saddle");
                //PlayerPrefs.SetString(Constants.Horse.HorseSaddleKey, "Saddle3");
                //PlayerPrefs.SetString(Constants.Horse.HorseReinsHeadKey, "Saddle");
                //PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("Player data already exists, skipping initialization.");
            }
        }
        private List<string> GetPreloadMaterialAddresses()
        {
            List<string> preload = new List<string>();
            string selectedEnv = PlayerPrefs.GetString(Constants.HomeEnivronments.SelectedEnvironment );// default
                                                                                                        // 1️⃣ Default map
            if (string.IsNullOrEmpty(selectedEnv))
            {
                selectedEnv = Constants.MapNames.Zarafshan;
                PlayerPrefs.SetString(
                    Constants.HomeEnivronments.SelectedEnvironment,
                    selectedEnv
                );
            }
            switch(selectedEnv)
            {
                case Constants.MapNames.Zarafshan:
                    preload.Add(Constants.SkyBoxes.ZarafshanSkybox);
                    break;
                case Constants.MapNames.Registan:
                    preload.Add(Constants.SkyBoxes.RegistanSkybox);
                    break;
                case Constants.MapNames.Egypt:
                    preload.Add(Constants.SkyBoxes.EgyptSkybox);
                    break;
                case Constants.MapNames.Kansas:
                    preload.Add(Constants.SkyBoxes.KansasSkybox);
                    break;
                default:
                    Debug.LogWarning("Unknown environment: " + selectedEnv);
                    break;
            }

            preload.Add(selectedEnv);

            //Home Room Sound and ui sounds
            preload.Add(Constants.UISounds.Confirm);
            preload.Add(Constants.UISounds.Error);
            preload.Add(Constants.UISounds.Success);
            preload.Add(Constants.UISounds.Click);
            preload.Add(Constants.UISounds.PopupOpen);
            preload.Add(Constants.UISounds.PopupClose);
            preload.Add(Constants.RoomSound.HomeRoomSound);
            return preload;
        }
        #endregion
    }
}


