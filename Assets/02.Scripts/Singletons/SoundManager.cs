using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public AudioSource musicSource;
    [SerializeField] private float defaultVolume = 1f;
    public float DefaultVolume => defaultVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            GameObject singletonFolder = GameObject.Find("Singletons");
            if (singletonFolder == null)
            {
                singletonFolder = new GameObject("Singletons");
                DontDestroyOnLoad(singletonFolder);
            }
            transform.SetParent(singletonFolder.transform);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        StartCoroutine(FadeInMusic(clip, 1f));
    }

    private IEnumerator FadeInMusic(AudioClip clip, float duration)
    {
        if (clip == null)
        {
            Debug.LogWarning("Music clip is null!");
            yield break;
        }

        if (musicSource.isPlaying && musicSource.clip == clip)
            yield break;

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, defaultVolume, timer / duration);
            yield return null;
        }

        musicSource.volume = defaultVolume;
    }

    public void StopMusicEvent()
    {
        StartCoroutine(StopMusic(1.5f));
    }

    private IEnumerator StopMusic(float time)
    {
        if (!musicSource.isPlaying)
            yield break;

        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / time;
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
    }

    public void SetVolume(float newVolume)
    {
        defaultVolume = newVolume;
        musicSource.volume = newVolume;
    }
}
