using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Controller;

[RequireComponent(typeof(Collider))]
public class WalkZone : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private float slowDuration = 2.5f;
    [SerializeField] private int slowSpeedIndex = 3;
    [SerializeField] private bool onlyAffectHeads = true;
    private bool triggered = false;
    private Collider _collider;

    // Trap ichida bir marta slow berish uchun
    private readonly HashSet<BoostersContainer> _affected = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    private void OnEnable()
    {
        // Har safar pooldan chiqqanda default holatga qaytadi
        triggered = false;

        if (_collider != null)
            _collider.enabled = true;

        // Agar _affected HashSet ishlatayotgan bo‘lsang:
        _affected.Clear();
    }
    private void OnTriggerEnter(Collider other)
    {
        // Faqat bosh colliderlari ishlasin desang
        bool isPlayer = other.CompareTag("Player");
        bool isNpc = other.CompareTag("NPC");
        if (!isPlayer && !isNpc) return;

 
        var boosters = other.GetComponentInChildren<BoostersContainer>();
        if (!boosters) {
            if (triggered) return;
            triggered = true;

            // Trap 2chi marta ishga tushmasin:
            if (_collider != null)
                _collider.enabled = false;
            SimplePool.Despawn(gameObject);

            Debug.Log("#########ishlamayapti");
            return;
        }
        // NPC: zaxira qalqon bo‘lsa → auto-defend → slow SKIP
        if (boosters.isNpc && boosters.defendCount > 0)
        {
            boosters.DefendPlayerNpc(); // ichida o‘zi count kamayadi va qobiq ON
            return;
        }

        // Player: qalqon allaqachon yoqilgan bo‘lsa → slow SKIP
        if (boosters.defendQobiq != null && boosters.defendQobiq.activeSelf)
            return;
        if (triggered) return;
        triggered = true;

        // Trap 2chi marta ishga tushmasin:
        if (_collider != null)
            _collider.enabled = false;
        // Endi slow beramiz (faqat shu yerda affected va subscribe qilamiz)
        TrySlow(boosters);

        // Trap bir marta ishlasin
       // SimplePool.Despawn(gameObject);
    }

    private void TrySlow(BoostersContainer boosters)
    {
        if (!boosters.horseAnimal) return;
        if (_affected.Contains(boosters)) return; // trap ichida takror sekinlashtirmaslik

        _affected.Add(boosters);

        var animal = boosters.horseAnimal;

        // Hozirgi speed indexni saqlab qo‘yamiz (har bir hayvon uchun har xil bo‘lishi mumkin)
        int originalIndex = animal.CurrentSpeedIndex;

        // ONE-SHOT subscribe: defend yoqilsa — affected dan chiqaramiz
        Action handler = null;
        handler = () =>
        {
            _affected.Remove(boosters);
            boosters.OnDefendActivated -= handler; // one-shot unsubscribe
        };
        boosters.OnDefendActivated += handler;

        // Slow beramiz: faqat haqiqatan sekinlashtiradigan bo‘lsa tushiramiz
        int targetIndex = Mathf.Min(originalIndex, slowSpeedIndex); // hech qachon tezlatib yubormasin
        Debug.Log($"[WalkZone] Slow applied. Original={originalIndex}, Target={targetIndex}");

        animal.Speed_CurrentIndex_Set(targetIndex);

        StartCoroutine(ApplySlowRoutine(
            animal,
            originalIndex,
            targetIndex,
            slowDuration,
            boosters,
            handler));
    }

    private IEnumerator ApplySlowRoutine(
        MAnimal animal,
        int originalIndex,
        int appliedSlowIndex,
        float duration,
        BoostersContainer boosters,
        Action handler)
    {
        Debug.Log($"[WalkZone] startefd time");
        yield return new WaitForSeconds(duration);
        Debug.Log($"[WalkZone] finished time");
        // Agar hanuz slow indeks turib turgan bo‘lsa (defend/sprint o‘zgartirmagan bo‘lsa) — restore qilamiz
        if (animal != null && animal.CurrentSpeedIndex == appliedSlowIndex)
        {
            animal.Speed_CurrentIndex_Set(originalIndex);
            Debug.Log($"[WalkZone] Slow finished. Restored to {originalIndex}");
        }
        else
        {
            Debug.Log($"[WalkZone] somenting wrong");
        }
        _affected.Remove(boosters);

        // Safety: defend bosilmagan bo‘lsa ham listenerni tozalaymiz
        if (boosters != null && handler != null)
        {
            boosters.OnDefendActivated -= handler;
        }
        SimplePool.Despawn(gameObject);
    }
}
