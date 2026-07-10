using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.IO;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public string CurrentLanguage { get; private set; } = "english";
    public static event Action<string> OnLanguageChanged;

    public static event Action OnLanguageChangedEvent;

    private TranslationList translations;
    public bool IsReady => translations?.translations != null && translations.translations.Count > 0;

    private const string jsonUrl = "https://s3.ap-northeast-2.amazonaws.com/kaja-games.com/JsonDatas/language.json";
    private string versionUrl = "https://s3.ap-northeast-2.amazonaws.com/kaja-games.com/JsonDatas/version.txt";
    private string localPath => Path.Combine(Application.persistentDataPath, "language.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> InitializeAsync()
    {
        string remoteVersion = await GetRemoteVersion();
        string localVersion = PlayerPrefs.GetString("language_version", "");

        if (!string.IsNullOrEmpty(remoteVersion) && (remoteVersion != localVersion || !File.Exists(localPath)))
        {
            Debug.Log("Version changed or file missing. Downloading new translation file...");
            bool downloaded = await DownloadAndSaveTranslations();
            if (downloaded)
            {
                PlayerPrefs.SetString("language_version", remoteVersion);
                PlayerPrefs.Save();
                return true;
            }
        }

        if (LoadCachedTranslations())
        {
            return true;
        }

        ShowLanguageDownloadPopup();
        EnsureEmptyTranslations();
        return false;
    }
    private async Task<string> GetRemoteVersion()
    {
        UnityWebRequest request = UnityWebRequest.Get(versionUrl);
        request.timeout = 8;
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            return request.downloadHandler.text.Trim(); // Masalan: "1.0.3"
        }
        else
        {
            Debug.LogError("Failed to load version.txt: " + request.error);
            return ""; // fallback
        }
    }

    private async Task<bool> DownloadAndSaveTranslations()
    {
        UnityWebRequest request = UnityWebRequest.Get(jsonUrl);
        request.timeout = 8;
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonText = request.downloadHandler.text;
            translations = JsonUtility.FromJson<TranslationList>(jsonText);

            // Faylni localga saqlaymiz
            File.WriteAllText(localPath, jsonText);
            Debug.Log("Translations downloaded and saved locally.");
            return IsReady;
        }
        else
        {
            Debug.LogError("Failed to load translations from S3: " + request.error);
            return false;
        }
    }

    private bool LoadCachedTranslations()
    {
        if (!File.Exists(localPath))
            return false;

        try
        {
            Debug.Log("Using cached translation file.");
            string jsonText = File.ReadAllText(localPath);
            translations = JsonUtility.FromJson<TranslationList>(jsonText);
            return IsReady;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load cached translations: " + ex.Message);
            return false;
        }
    }

    public string GetText(int id)
    {
        if (!IsReady)
            return $"#{id}";

        var entry = translations.translations.FirstOrDefault(x => x.Id == id);
       // Debug.Log($"ID: {id}, Entry: {entry}");
        if (entry == null) return "";

        return NormalizeLanguage(CurrentLanguage) switch
        {
            "english" => entry.english,
            "russian" => entry.russian,
            "uzbek" => entry.uzbek,
            "kazak" => entry.kazak,
            _ => entry.english
        };
    }
    public string GetText(int id, params object[] args)
    {
        string text = GetText(id);

        if (string.IsNullOrEmpty(text))
            return "";

        try
        {
            return string.Format(text, args);
        }
        catch (FormatException e)
        {
            Debug.LogWarning($"Localization format error. ID: {id}, Text: {text}, Error: {e.Message}");
            return text;
        }
    }

    public void SetLanguage(string lang)
    {
        lang = NormalizeLanguage(lang);
        bool changed = CurrentLanguage != lang;

        CurrentLanguage = lang;
        PlayerPrefs.SetString("language", lang);
        if (!changed) return;

        OnLanguageChanged?.Invoke(lang);
        OnLanguageChangedEvent?.Invoke();
    }

    private static string NormalizeLanguage(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return "english";

        switch (lang.Trim().ToLowerInvariant())
        {
            case "en":
            case "eng":
            case "english":
                return "english";
            case "ru":
            case "rus":
            case "russian":
                return "russian";
            case "uz":
            case "uzbek":
            case "uzbekcyrillic":
                return "uzbek";
            case "kz":
            case "kk":
            case "kazak":
            case "kazakh":
                return "kazak";
            default:
                return "english";
        }
    }

    private void EnsureEmptyTranslations()
    {
        translations ??= new TranslationList();
        translations.translations ??= new List<Translation>();
    }

    private void ShowLanguageDownloadPopup()
    {
        if (UIOverlayRoot.I == null)
            return;

        UIOverlayRoot.I.Done(
            "Language download failed",
            "Could not download the language file. Check your internet connection and restart the game if text is missing.",
            "OK",
            null
        );
    }
}
