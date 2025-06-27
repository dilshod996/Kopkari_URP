using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerPrefsData : MonoBehaviour
{
    [SerializeField] private GameObject userDetail;
    [SerializeField] private GameObject horseDetail;
    [Header("UI Texts")]
    [SerializeField] private TMP_Text nameUser;

    [Header("scale anim")]
    [SerializeField] private float scaleUp = 1.3f;
    [SerializeField] private float duration = 0.3f;

    private const string Username = "username";
    private const string HorseData = "horseData";
    private void OnEnable()
    {
        nameUser.text = LanguageManager.Instance.GetText(113);
    }
    public void UserDataCheck()
    {
        if (!PlayerPrefs.HasKey(Username))
        {
            if (horseDetail.activeSelf) horseDetail.SetActive(false);
            userDetail.SetActive(true);
            StartScaleLoop(userDetail);
        }
    }
    public void HorseDataCheck()
    {
        if (!PlayerPrefs.HasKey(HorseData))
        {
            if (userDetail.activeSelf) userDetail.SetActive(false);
            horseDetail.SetActive(true);
            StartScaleLoop(horseDetail);
        }
    }
    #region Scale Animation
    public void StartScaleLoop(GameObject targetObj)
    {
        if (targetObj != null)
        {
            StartCoroutine(LoopScale(targetObj));
        }
        else
        {
            Debug.LogWarning("Target object is null!");
        }
    }

    private IEnumerator LoopScale(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;

        while (true)
        {
            yield return StartCoroutine(ScaleTo(obj, originalScale * scaleUp));
            yield return StartCoroutine(ScaleTo(obj, originalScale));
        }
    }

    private IEnumerator ScaleTo(GameObject obj, Vector3 targetScale)
    {
        Vector3 startScale = obj.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        obj.transform.localScale = targetScale;
    }
    #endregion

}
