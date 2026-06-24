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

    public async Task InitializeAsync()
    {
        string remoteVersion = await GetRemoteVersion();
        string localVersion = PlayerPrefs.GetString("language_version", "");

        if (remoteVersion != localVersion || !File.Exists(localPath))
        {
            Debug.Log("Version changed or file missing. Downloading new translation file...");
            await DownloadAndSaveTranslations();
            PlayerPrefs.SetString("language_version", remoteVersion);
        }
        else
        {
            Debug.Log("Using cached translation file.");
            string jsonText = File.ReadAllText(localPath);
            translations = JsonUtility.FromJson<TranslationList>(jsonText);
        }
    }
    private async Task<string> GetRemoteVersion()
    {
        UnityWebRequest request = UnityWebRequest.Get(versionUrl);
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

    private async Task DownloadAndSaveTranslations()
    {
        UnityWebRequest request = UnityWebRequest.Get(jsonUrl);
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
        }
        else
        {
            Debug.LogError("Failed to load translations from S3: " + request.error);
        }
    }

    public string GetText(int id)
    {
        var entry = translations.translations.FirstOrDefault(x => x.Id == id);
       // Debug.Log($"ID: {id}, Entry: {entry}");
        if (entry == null) return "";

        return CurrentLanguage switch
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
        if (CurrentLanguage == lang) return;

        CurrentLanguage = lang;
        PlayerPrefs.SetString("language", lang);
        OnLanguageChanged?.Invoke(lang);
        OnLanguageChangedEvent?.Invoke();
    }
}
