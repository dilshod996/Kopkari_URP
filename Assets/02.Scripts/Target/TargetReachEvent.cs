using MalbersAnimations.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetReachEvent : MonoBehaviour
{
    [SerializeField] private Pickable lambObject;
    private bool triggerLocked = false;

    private void OnEnable()
    {
        BaseManager.OnResetTarget += ResetTrigger;
    }
    private void OnDisable()
    {
        BaseManager.OnResetTarget -= ResetTrigger;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (triggerLocked) return;

        if (other.CompareTag("Lamb"))
        {
            //Pickable pickable = other.GetComponent<Pickable>();
            if (lambObject != null)
            {
                lambObject.Drop(); // Uloq tashlanadi
                Debug.Log("Lamb reached the target!");
            }
            else
            {
                Debug.Log("Lamb not exist");
            }

            StartCoroutine(DelayStop());

            //triggerLocked = true; // ❗ endi boshqa chaqirilmaydi
        }
    }
    IEnumerator DelayStop()
    {
        yield return new WaitForSeconds(0.2f);
        BaseManager.Instance?.MarkPlayerReachedTarget();
        triggerLocked = true; 
    }
    public void ResetTrigger()
    {
        triggerLocked = false;
    }

}
