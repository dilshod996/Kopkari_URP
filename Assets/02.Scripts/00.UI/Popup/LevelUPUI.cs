using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelUPUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelUpText;
    [SerializeField] private TMP_Text levelUpCount;

    private Coroutine closeCoroutine;
    private void OnEnable()
    {
        GetLevel();
    }
    private void GetLevel()
    {
        levelUpText.text = "Level Up";

        if (DataManager.Instance == null)
            return;

        int currentLevel = DataManager.Instance.LevelAmount;
        int pendingCount = DataManager.Instance.LevelUpPending;

        int popupLevel = currentLevel - pendingCount + 1;

        if (popupLevel < 1)
            popupLevel = 1;

        levelUpCount.text = popupLevel.ToString();

        CloseLevelUpPopup();
    }
    private void CloseLevelUpPopup()
    {
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        closeCoroutine = StartCoroutine(DelayClose());
    }
    IEnumerator DelayClose()
    {
        yield return new WaitForSeconds(3f);

        HomeMainUI.Instance.HideUI(this);
        HomeMainUI.Instance.OnLevelUpPopupClosed();
    }
}
