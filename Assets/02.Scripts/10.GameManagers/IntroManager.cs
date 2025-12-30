using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
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
        //[SerializeField] private Button skipButton;
        //Intro scene addressable addresses
        private List<string> myAddresses = new List<string> { "IntroSound", "IntroVideo" };

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
            await LanguageManager.Instance.InitializeAsync();
            notificationManager.onConfirm.AddListener(() =>
            {
                RetryInitWithPopup();
            });
            videoPlayer.loopPointReached += OnVideoFinished;
            GetAddressableData();
            //PlayerPrefs.DeleteAll();

            PlayerMaterialsData();
            Debug.Log("System Language: " + Application.systemLanguage.ToString());
            SetInitialLanguage();
            startButton.onClick.AddListener(() =>
            {
                LoadLobbyScene();
            });

            InitializePlayerPrefs();

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
            SoundManager.Instance.PlayMusic(clip);
        }

        public async void GetAddressableData()
        {
            handles = await AddressablesManager.Instance.LoadAssetsWithHandlesAsync<Object>(
                myAddresses,
                progress =>
                {
                    
                    progressBar.currentPercent = progress * 100f;
                    progressBar.UpdateUI();
                },
                fakeDurationIfCached: 2f
            );
            if (handles.Count > 0)
            {
                foreach (var handle in handles)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Object loadedAsset = handle.Result;

                        // 🎬 Agar bu VideoClip bo‘lsa
                        if (loadedAsset is VideoClip video)
                        {
                            videoPlayer.clip = video;
                            videoPlayer.Play();
                            Debug.Log("▶️ Video played");
                        }

                        // 🔊 Agar bu AudioClip bo‘lsa
                        else if (loadedAsset is AudioClip audio)
                        {
                            SoundEffect(audio);
                            Debug.Log("🔊 Audio played");
                        }
                        if(startingPage.activeSelf)
                            startingPage.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning("❌ One of the assets failed to load.");
                    }
                }
            }

        }
        public async void GetIntroVideo()
        {
            handle = await AddressablesManager.Instance.LoadAssetWithHandleAsync<VideoClip>(
                "IntroVideo",
                progress =>
                {
                    float percent = progress * 100f;
                    progressBar.currentPercent = percent;
                    progressBar.UpdateUI();
                },
                fakeDurationIfCached: 3f
            );

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                videoPlayer.clip = handle.Result;
                videoPlayer.Play();
                if (startingPage.activeSelf)
                    startingPage.SetActive(false);
            }

        }
        #endregion

        #region Notification Popup
        public void RetryInitWithPopup()
        {
            StartCoroutine(EnsureInitRoutine());
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
                GetIntroVideo();
                // Optional: asset loading yoki sahifani qayta yuklash
            }
        }

        public void ShowPopup()
        {
            notificationManager.UpdateUICustom("Internetda xatolik", "Internet mavjud emas. Iltimos internetni yoqilganiga ishonch hosil qiling");
        }

        IEnumerator ShowPopCor()
        {
            yield return new WaitForEndOfFrame();
            Debug.Log("Open it");
            ShowPopup();
        }
        #endregion

        #region Moving To Lobby

        public void LoadLobbyScene()
        {
            List<string> preloadAddresses = GetPreloadMaterialAddresses();
            SceneLoadManager.Instance.LoadSmartSceneIntro(SceneLoadManager.SceneType.Home, preloadAddresses);
        }
        #endregion

        void OnVideoFinished(VideoPlayer vp)
        {
            StartCoroutine(GoToLobbyAfterSmallDelay());
        }

        private IEnumerator GoToLobbyAfterSmallDelay()
        {
            yield return StartCoroutine(FadeIn());

            LoadLobbyScene();
            Debug.Log("Video finished → Lobby scene");
        }
        private void OnDisable()
        {
            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    Debug.Log("Addressables released");
                    Addressables.Release(handle);
                }
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
                PlayerPrefs.SetInt(Constants.Player.FirstTimeKey, 1);
                //Save default player materials
                PlayerPrefs.SetString(Constants.Player.PlayerFaceHairKey, "FaceHair4");
                PlayerPrefs.SetString(Constants.Player.PlayerHeadKey, "Head");
                PlayerPrefs.SetString(Constants.Player.PlayerHelmetKey, "Hat");
                PlayerPrefs.SetString(Constants.Player.PlayerHand, "Hands");
                PlayerPrefs.SetString(Constants.Player.PlayerUpperBodyKey, "UpperBody4");
                PlayerPrefs.SetString(Constants.Player.PlayerLowerBodyKey, "LowerBody1");
 
                //Save default horse materials

                PlayerPrefs.SetString(Constants.Horse.HorseBodyKey, "HorseBrown");
                PlayerPrefs.SetString(Constants.Horse.HorseEyesKey, "HorseEyes");
                PlayerPrefs.SetString(Constants.Horse.HorseManeKey, "HorseManeBlack");
                PlayerPrefs.SetString(Constants.Horse.HorseTailKey, "HorseManeBlack");
                PlayerPrefs.SetString(Constants.Horse.HorseReinsKey, "Saddle");
                PlayerPrefs.SetString(Constants.Horse.HorseSaddleKey, "Saddle3");
                PlayerPrefs.SetString(Constants.Horse.HorseReinsHeadKey, "Saddle");
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("Player data already exists, skipping initialization.");
            }
        }
        private List<string> GetPreloadMaterialAddresses()
        {
            List<string> preload = new List<string>();

            //PlayerPrefs dan material addresslarini olish
            string helmet = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey);
            string head = PlayerPrefs.GetString(Constants.Player.PlayerHeadKey);
            string faceHair = PlayerPrefs.GetString(Constants.Player.PlayerFaceHairKey);
            string hand = PlayerPrefs.GetString(Constants.Player.PlayerHand);
            string upper = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey);
            string lower = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey);


            //PlayerPrefs dan ot material addresslarini olish

            string horseBody = PlayerPrefs.GetString(Constants.Horse.HorseBodyKey);
            string horseEyes = PlayerPrefs.GetString(Constants.Horse.HorseEyesKey);
            string horseMane = PlayerPrefs.GetString(Constants.Horse.HorseManeKey);
            string horseTail = PlayerPrefs.GetString(Constants.Horse.HorseTailKey);
            string horseReins = PlayerPrefs.GetString(Constants.Horse.HorseReinsKey);
            string horseSaddle = PlayerPrefs.GetString(Constants.Horse.HorseSaddleKey);
            string horseReinsHead = PlayerPrefs.GetString(Constants.Horse.HorseReinsHeadKey);

            preload.Add(head);
            preload.Add(hand);
            preload.Add(faceHair);
            preload.Add(upper);
            preload.Add(lower);
            preload.Add(helmet);
            preload.Add(horseBody);
            preload.Add(horseEyes);
            preload.Add(horseMane);
            preload.Add(horseTail);
            preload.Add(horseReins);
            preload.Add(horseSaddle);
            preload.Add(horseReinsHead);
            // Boshqa material addresslarini qo‘shish

            // Yana kerak bo‘lsa boshqa obyektlar
            preload.Add(Constants.Environment.Utov);

            return preload;
        }
        #endregion
    }
}


