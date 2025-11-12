using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITrophy : MonoBehaviour
{
    [SerializeField] private Button CloseBtn;
    [SerializeField] private GameObject parentItems;
    void Start()
    {
        CloseBtn.onClick.AddListener(CloseAction);
    }

    void CloseAction() 
    {
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
       // parentItems.transform.position = new Vector3(0, 0, 0);
        Debug.Log("Trophy UI is enabled and get all data that you achieved");
    }
    public void CheckIt()
    {
        Debug.Log("check");
    }
}
