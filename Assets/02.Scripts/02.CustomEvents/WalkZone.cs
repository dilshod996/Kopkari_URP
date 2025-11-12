using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Controller;
using System;

[RequireComponent(typeof(Collider))]
public class WalkZone : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private float slowDuration = 2.5f;
    [SerializeField] private int slowSpeedIndex = 3;
    [SerializeField] private bool onlyAffectHeads = true;

    // Trap ichida bir marta slow berish uchun
    private readonly HashSet<RacingAgent> _affected = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyAffectHeads && !other.CompareTag("RacingHead")) return;

        var agent = other.GetComponentInParent<RacingAgent>();
        if (!agent) return;

        // Sizda nomi 'boosters' yoki 'boosterContainer' — bitta nomda bo‘lsin
        var boosters = agent.boosterContainer;
        if (!boosters) return;

        // NPC: zaxira bo‘lsa auto-defend → slow SKIP
        if (boosters.isNpc && boosters.defendCount > 0)
        {
            boosters.DefendPlayerNpc(); // ichida o‘zi Decrease va qorqon ON
            return;
        }

        // Player: qalqon allaqachon yoqilgan bo‘lsa → slow SKIP
        if (!boosters.isNpc && boosters.defendQobiq.activeSelf)
            return;

        // Endi slow beramiz (faqat shu yerda affected va subscribe qilamiz)
        TrySlow(boosters, agent);
        SimplePool.Despawn(gameObject);
    }

    private void TrySlow(BoostersContainer boosters, RacingAgent agent)
    {

        if (!boosters.horseAnimal) return;
        if (_affected.Contains(agent)) return; // trap ichida takror sekinlashtirmaslik

        _affected.Add(agent);
        var animal = boosters.horseAnimal;
        int originalIndex = 5;

        // ONE-SHOT subscribe: defend yoqilsa — affected dan chiqaramiz
        Action handler = null;
        handler = () =>
        {
            _affected.Remove(agent);
            boosters.OnDefendActivated -= handler; // one-shot
        };
        boosters.OnDefendActivated += handler;

        // Slow beramiz: faqat haqiqatan sekinlashtiradigan bo‘lsa tushiramiz
       
        int targetIndex = Mathf.Min(originalIndex, slowSpeedIndex); // hech qachon tezlatib yubormasin
        Debug.Log("Target index: " + targetIndex);
        animal.Speed_CurrentIndex_Set(targetIndex);
        StartCoroutine(ApplySlowRoutine(animal, agent, originalIndex, targetIndex, slowDuration, boosters, handler));
    }

    private IEnumerator ApplySlowRoutine(MAnimal animal,
                                         RacingAgent agent,
                                         int originalIndex,
                                         int appliedSlowIndex,
                                         float duration,
                                         BoostersContainer boosters,
                                         Action handler)
    {
        yield return new WaitForSeconds(duration);

        // Agar hanuz slow indeks turib turgan bo‘lsa (defend/sprint o‘zgartirmagan bo‘lsa) — restore qilamiz
        if (animal != null && animal.CurrentSpeedIndex == appliedSlowIndex)
        {
            animal.Speed_CurrentIndex_Set(originalIndex);
        }

        _affected.Remove(agent);
        Debug.Log("Current index" + animal.CurrentSpeedIndex);
        // Safety: defend bosilmagan bo‘lsa ham listenerni tozalaymiz
        if (boosters != null) boosters.OnDefendActivated -= handler;
    }
}
