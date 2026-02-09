using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource uiSource;

    [Header("Clips")]
    public AudioClip click;
    public AudioClip select;
    public AudioClip confirm;
    public AudioClip success;
    public AudioClip error;
    public AudioClip popupOpen;
    public AudioClip popupClose;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Play(UISoundType type)
    {
        AudioClip clip = type switch
        {
            UISoundType.Click => click,
            UISoundType.Select => select,
            UISoundType.Confirm => confirm,
            UISoundType.Success => success,
            UISoundType.Error => error,
            UISoundType.PopupOpen => popupOpen,
            UISoundType.PopupClose => popupClose,
            _ => null
        };

        if (clip == null) return;

        uiSource.pitch = Random.Range(0.97f, 1.03f); // 🔥 premium feel
        uiSource.PlayOneShot(clip);
    }
}
