using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public LookAtCamera lookAtCamera;
    //public GameObject walkAreaPrefab;
    //public float dropDistanceBehind = 2.5f; // inspector¡¯da sozlasa bo¡®ladi
    //public GameObject defendQobiq;

    //private Coroutine defendCoroutine;
    void Start()
    {
        
    }

    private void OnEnable()
    {
        //SceneLoadManager.Instance.OnSceneLoaded += UpdateVisibility;
        UpdateVisibility();
    }
    void UpdateVisibility()
    {
        if (SceneLoadManager.Instance == null) return;

        var sceneType = SceneLoadManager.Instance.CurrentSceneType;

        // Agar Lobby yoki AvatarCustom sahifasi bo¡®lsa => yashirish
        if (sceneType == SceneLoadManager.SceneType.Lobby || sceneType == SceneLoadManager.SceneType.AvatarCustom)
        {
            lookAtCamera.gameObject.SetActive(false);
        }
        else
        {
            if (lookAtCamera != null)
            {
                lookAtCamera.gameObject.SetActive(true);
                lookAtCamera.GetNameAndLogo();
            }
            Debug.Log("Visible now");
        }
    }
    //public void DropIceTrap()
    //{
    //    Vector3 dropPosition = transform.position - transform.forward * dropDistanceBehind;
    //    Instantiate(walkAreaPrefab, dropPosition, Quaternion.identity);
    //}

    //public void DefendPlayer()
    //{
    //    if (defendCoroutine != null)
    //    {
    //        StopCoroutine(defendCoroutine);
    //        defendQobiq.SetActive(false); 
    //    }
    //    defendCoroutine = StartCoroutine(DefendObject());
    //}

    //private IEnumerator DefendObject()
    //{
    //    defendQobiq.SetActive(true);
    //    yield return new WaitForSeconds(10f);
    //    defendQobiq.SetActive(false);
    //    defendCoroutine = null; 
    //}
}
