using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
  
    public ProgressBar progressBar;
    private float loadTime;
    [SerializeField] private TMP_Text randomText;

    private void Awake()
    {
        
    }

    void Start()
    {
        loadTime = AddressablesManager.Instance.loadingTime;
        SoundManager.Instance.StopMusicEvent();
        //StartCoroutine(ProgressbarTime());
        StartCoroutine(ChangeTextRoutine());
    }
    private void OnEnable()
    {
        StartCoroutine(ProgressbarTime());
    }


    IEnumerator ProgressbarTime()
    {
        Debug.Log("🟡 ProgressbarTime coroutine started.");

        while (true)
        {
            float current = AddressablesManager.Instance?.loadingTime ?? 0f;

            progressBar.currentPercent = current;
            progressBar.UpdateUI();

            if (current >= 100f)
            {
                Debug.Log("✅ Progress reached 100%. Exiting coroutine.");
                break;
            }

            yield return null;
        }
    }


    IEnumerator ChangeTextRoutine()
    {
        while (true)
        {
            int randomIndex = Random.Range(6, 20);
            randomText.text = LanguageManager.Instance.GetText(randomIndex); 
            yield return new WaitForSeconds(3f);
        }
    }

}
