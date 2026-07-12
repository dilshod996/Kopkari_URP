using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Kopkari
{
    public class IntroManager : MonoBehaviour
    {

        //SingletonScene
        public static IntroManager Instance;
        [SerializeField] GameObject startingPage;
        [SerializeField] GameObject moveLobbyPage;
        [SerializeField] VideoPlayer videoPlayer;

        // Popup
        [Header("Popup")]
        public ModalWindowManager notificationManager;
        public ProgressBar progressBar;

        AsyncOperationHandle<VideoClip> handle;
        List<AsyncOperationHandle<Object>> handles;

        [SerializeField] AudioClip introMusic;
        [SerializeField] TMP_Text gameName;
        [SerializeField] private Button startButton;
        [SerializeField] private Button skipButton;
        //Intro scene addressable addresses
        private List<string> myAddresses = new List<string> { "IntroSound", "IntroVideo" };
        private List<string> homePreloadAddresses;
        private Task<bool> homePreloadTask;
        private bool introContentLoading;
        private bool introContentReady;
        private bool lobbyLoadStarted;
        private Coroutine homePreloadReadyRoutine;

        [Header("User Details")]
        // private const string UsernameKey = "username";
        private const string DefaultUsername = "Player_123";
        private const string HorseNameKey = "horseName";
        private const string DefaultHorseName = "Qorakoz";
        private const string CountryName = "countryName";
        private const string FirstTimeKey = "firstTime";
        private const string PlayerFaceKey = "Player_Face";

        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;   // ✔ Sen so‘ragan Image
        [SerializeField] private float fadeDuration = 0.5f;
        private Color fadeColor;
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
            if (LanguageManager.Instance != null)
            {
                await LanguageManager.Instance.InitializeAsync();
            }

            if (notificationManager != null)
            {
                notificationManager.onConfirm.AddListener(() =>
                {
                    RetryIntroContentLoad();
                });
            }

            if (videoPlayer != null)
                videoPlayer.loopPointReached += OnVideoFinished;

            //PlayerPrefs.DeleteAll();

            //PlayerMaterialsData();
            Debug.Log("System Language: " + Application.systemLanguage.ToString());
            SetInitialLanguage();
            if (startButton != null)
            {
                startButton.onClick.AddListener(() =>
                {
                    LoadLobbyScene();
                });
            }
            if (skipButton != null && skipButton != startButton)
            {
                skipButton.onClick.AddListener(() =>
                {
                    LoadLobbyScene();
                });
            }
            SetSkipAvailable(false);

            InitializePlayerPrefs();
            homePreloadAddresses = GetPreloadMaterialAddresses();
            GetAddressableData();

        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        public void SetInitialLanguage()
        {
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
        private void InitializePlayerPrefs()
        {

            if (!PlayerPrefs.HasKey(Constants.Horse.HorseNameKey))
                PlayerPrefs.SetString(HorseNameKey, DefaultHorseName);

            PlayerPrefs.Save();
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
        public void SoundEffect(AudioClip clip)
        {
            SoundManager.Instance?.PlayRoom(clip);
        }
        public async void GetAddressableData()
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
                fakeDurationIfCached: 2f
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
            var video = await AddressablesService.Instance.LoadAssetAsync<VideoClip>(Constants.VideoClips.IntroVideo);
            if (lobbyLoadStarted || this == null)
                return;

            if (video != null)
            {
                if (videoPlayer != null)
                {
                    videoPlayer.clip = video;
                    videoPlayer.Play();
                    StartHomePreloadDuringVideo();
                }
                if (startingPage != null && startingPage.activeSelf)
                    startingPage.SetActive(false);

                introContentReady = true;
                Debug.Log($"▶️ Video played: {Constants.VideoClips.IntroVideo}");

            }
            else
            {
                ShowIntroRetryPopup();
                return;
            }

            var audio = await AddressablesService.Instance.LoadAssetAsync<AudioClip>(Constants.RoomSound.IntroSound);
            if (lobbyLoadStarted || this == null)
                return;

            if (audio != null)
            {
                SoundEffect(audio);
                Debug.Log($"🔊 Audio played: {Constants.RoomSound.IntroSound}");

            }

        }
        public void GetIntroVideo()
        {
            RetryIntroContentLoad();
        }
        //public async void GetAddressableData()
        //{
        //    handles = await AddressablesManager.Instance.LoadAssetsWithHandlesAsync<Object>(
        //        myAddresses,
        //        progress =>
        //        {

        //            progressBar.currentPercent = progress * 100f;
        //            progressBar.UpdateUI();
        //        },
        //        fakeDurationIfCached: 2f
        //    );
        //    if (handles.Count > 0)
        //    {
        //        foreach (var handle in handles)
        //        {
        //            if (handle.Status == AsyncOperationStatus.Succeeded)
        //            {
        //                Object loadedAsset = handle.Result;

        //                // 🎬 Agar bu VideoClip bo‘lsa
        //                if (loadedAsset is VideoClip video)
        //                {
        //                    videoPlayer.clip = video;
        //                    videoPlayer.Play();
        //                    Debug.Log("▶️ Video played");
        //                }

        //                // 🔊 Agar bu AudioClip bo‘lsa
        //                else if (loadedAsset is AudioClip audio)
        //                {
        //                    SoundEffect(audio);
        //                    Debug.Log("🔊 Audio played");
        //                }
        //                if(startingPage.activeSelf)
        //                    startingPage.SetActive(false);
        //            }
        //            else
        //            {
        //                Debug.LogWarning("❌ One of the assets failed to load.");
        //            }
        //        }
        //    }

        //}
        //public async void GetIntroVideo()
        //{
        //    handle = await AddressablesManager.Instance.LoadAssetWithHandleAsync<VideoClip>(
        //        "IntroVideo",
        //        progress =>
        //        {
        //            float percent = progress * 100f;
        //            progressBar.currentPercent = percent;
        //            progressBar.UpdateUI();
        //        },
        //        fakeDurationIfCached: 3f
        //    );

        //    if (handle.Status == AsyncOperationStatus.Succeeded)
        //    {
        //        videoPlayer.clip = handle.Result;
        //        videoPlayer.Play();
        //        if (startingPage.activeSelf)
        //            startingPage.SetActive(false);
        //    }

        //}
        #endregion

        #region Notification Popup
        public void RetryInitWithPopup()
        {
            RetryIntroContentLoad();
        }

        IEnumerator EnsureInitRoutine()
        {
            Task<bool> initTask = AddressablesManager.Instance.EnsureInitialized();

            // Task tugaguncha kut
            while (!initTask.IsCompleted)
                yield return null;

            if (!initTask.Result)
            {
                Debug.Log("❌ Still no internet. Showing popup again");
                notificationManager.UpdateUICustom("Internetda xatolik", "Internet mavjud emas. Iltimos internetni yoqilganiga ishonch hosil qiling");
            }
            else
            {
                Debug.Log("✅ Addressables re-initialized from confirm!");
                RetryIntroContentLoad();
                // Optional: asset loading yoki sahifani qayta yuklash
            }
        }

        public void ShowPopup()
        {
            ShowIntroRetryPopup();
        }

        private void RetryIntroContentLoad()
        {
            if (lobbyLoadStarted)
                return;

            introContentReady = false;
            GetAddressableData();
        }

        private void ShowIntroRetryPopup()
        {
            if (UIOverlayRoot.I != null)
            {
                UIOverlayRoot.I.Confirm(
                    "Download failed",
                    "Could not download required intro content. Please check your internet connection and try again.",
                    "Retry",
                    "Close",
                    RetryIntroContentLoad,
                    null
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
            Button activeSkipButton = skipButton != null ? skipButton : startButton;

            if (startButton != null)
            {
                startButton.interactable = available && activeSkipButton == startButton;
                startButton.gameObject.SetActive(available && activeSkipButton == startButton);
            }

            if (skipButton != null)
            {
                skipButton.interactable = available;
                skipButton.gameObject.SetActive(available);
            }

            if (moveLobbyPage != null && activeSkipButton != null && activeSkipButton.transform.IsChildOf(moveLobbyPage.transform))
                moveLobbyPage.SetActive(available);
        }

        #endregion

        #region Moving To Lobby

        public void LoadLobbyScene()
        {
            if (lobbyLoadStarted)
                return;

            lobbyLoadStarted = true;
            SetSkipAvailable(false);

            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
                videoPlayer.Stop();
            }

            homePreloadAddresses ??= GetPreloadMaterialAddresses();

            if (SceneLoadManager.Instance == null)
                return;

            SceneLoadManager.Instance.LoadHomeFromIntro(
                SceneLoadManager.SceneType.Home,
                homePreloadAddresses,
                GetReusableHomePreloadTask()
            );
        }
        #endregion

        void OnVideoFinished(VideoPlayer vp)
        {
            LoadLobbyScene();
            //StartCoroutine(GoToLobbyAfterSmallDelay());
        }

        private void OnDisable()
        {
            //foreach (var handle in handles)
            //{
            //    if (handle.IsValid())
            //    {
            //        Debug.Log("Addressables released");
            //        Addressables.Release(handle);
            //    }
            //}
            if(SoundManager.Instance != null)
            {
                SoundManager.Instance.StopRoomSmooth();
            }
            if (AddressablesService.Instance != null)
            {
                foreach (var addr in myAddresses)
                    AddressablesService.Instance.ReleaseLoadedAsset(addr);
            }
        }
        /// Fade-in → 0 dan 1 ga (ekran qora bo‘ladi)
        /// </summary>
        public IEnumerator FadeIn()   // 0 -> 1
        {
            // 1) Avval aktiv qilamiz
            fadeImage.gameObject.SetActive(true);

            float t = 0f;

            // 2) Bor rangni olamiz, faqat alpha 0 qilamiz
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;

            // 3) Asta-sekin 0 dan 1 ga ko‘taramiz
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, 1f, t / fadeDuration);

                c = fadeImage.color; // rangni buzmaslik uchun har safar o‘sha rangdan olamiz
                c.a = a;
                fadeImage.color = c;

                yield return null;
            }

            c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
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


