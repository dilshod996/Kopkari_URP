using MalbersAnimations.Controller;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackDefendManager : MonoBehaviour
{
    [Header("Attack Objects")]
    [SerializeField] private GameObject walkZonePrefab;
    public float dropDistanceBehind = 5.3f; // Inspector’da sozlasa bo‘ladi

    [Header("Defend Objects")]
    public GameObject defendQobiq;

    private Coroutine defendCoroutine;
    public int walkZoneCount = 2;
    public int defendCount = 2;
    public event Action OnWalkZoneAdded;
    public event Action OnWalkZoneRemoved;

    // events for Defend
    public event Action OnDefendAdded;
    public event Action OnDefendRemoved;
    [Header("Npc info")]
    public bool isNpc = false; // Npc bo‘lsa, bu true bo‘ladi
    void Start()
    {
        
    }
    #region Attack

    public void AddWalkZone()
    {
        walkZoneCount++;
        OnWalkZoneAdded?.Invoke();
    }
    public void DecreaseWalkZone()
    {
        walkZoneCount = Mathf.Max(0, walkZoneCount - 1);
        OnWalkZoneRemoved?.Invoke();
    }
    public void DropWalkTrap()
    {
        if (walkZoneCount <= 0)
        {
            Debug.Log("WalkZone mavjud emas, tushirib bo'lmaydi.");
            return;
        }
        DecreaseWalkZone();
        Vector3 dropPosition = transform.position - transform.forward * dropDistanceBehind;
        Instantiate(walkZonePrefab, dropPosition, Quaternion.identity);
    }
    public void DropWalkTrapNpc()
    {
        if (walkZoneCount <= 0)
        {
            return;
        }
        DecreaseWalkZone();
        Vector3 dropPosition = transform.position - transform.forward * dropDistanceBehind;
        Instantiate(walkZonePrefab, dropPosition, Quaternion.identity);
    }
    #endregion

    #region Defend

    public void AddDefend()
    {
        defendCount++;
        OnDefendAdded?.Invoke();

    }
    public void DecreaseDefend()
    {
        defendCount = Mathf.Max(0, defendCount - 1);
        OnDefendRemoved?.Invoke();
    }
    public void DefendPlayer()
    {
        if (defendCount <= 0)
        {
            Debug.Log("❌ Defend mavjud emas");
            return;
        }
        DecreaseDefend();
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendQobiq.SetActive(false);
        }
        defendCoroutine = StartCoroutine(DefendObject());
    }
    public void DefendPlayerNpc()
    {
        if (defendCount <= 0)
        {
            return;
        }
        DecreaseDefend();
        if (defendCoroutine != null)
        {
            StopCoroutine(defendCoroutine);
            defendQobiq.SetActive(false);
        }
        defendCoroutine = StartCoroutine(DefendObject());
    }

    private IEnumerator DefendObject()
    {
        defendQobiq.SetActive(true);
        yield return new WaitForSeconds(6f);
        defendQobiq.SetActive(false);
        defendCoroutine = null;
    }
    #endregion

    #region Speed Booster
    public void SprintByTime(float duration = 5f)
    {
        MAnimal animal = BaseManager.Instance.horseAnimal;

        if (animal != null)
        {
            int originalSpeedIndex = animal.CurrentSpeedIndex;
            //animal.CurrentSpeedIndex =5
            animal.Sprint = true; // yoki: animal.CurrentSpeedIndex = 3;
            Debug.Log($"SprintByTime: {animal.name} - {animal.CurrentSpeedIndex}");

            StartCoroutine(ResetSprint(animal, originalSpeedIndex, duration));
        }
    }

    private IEnumerator ResetSprint(MAnimal animal, int originalSpeed, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (animal != null)
        {
            animal.Sprint = false;
            animal.CurrentSpeedSet.CurrentIndex = originalSpeed;
        }
    }


    #endregion
}
