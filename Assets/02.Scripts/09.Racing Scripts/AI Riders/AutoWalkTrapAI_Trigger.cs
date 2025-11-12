using System.Collections;
using UnityEngine;
using MalbersAnimations.Controller;

[RequireComponent(typeof(BoostersContainer))]
public class AutoWalkTrapAI_Trigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RearThreatSensor sensor;
    [SerializeField] private MAnimal horse;               // NPC ot
    [SerializeField] private BoostersContainer boosters;  // shu obyektning o'zida bo'ladi odatda

    [Header("Logic")]
    [SerializeField] private float decisionInterval = 0.3f;
    [SerializeField] private float dropCooldown = 3.5f;
    [SerializeField] private float straightDotMin = 0.85f;  // yo‘l tekisligi
    [SerializeField] private float randomFactor = 0.12f;     // biroz tasodifiylik
    [SerializeField] private bool alignToGround = true;
    [SerializeField] private float groundRay = 3.0f;
    [SerializeField] private LayerMask groundMask;

    private float _nextAllowedTime;
    private Vector3 _lastForward;
    private Coroutine _loop;

    private void Reset()
    {
        boosters = GetComponent<BoostersContainer>();
    }

    private void Awake()
    {
        if (!boosters) boosters = GetComponent<BoostersContainer>();
        if (!horse) horse = boosters ? boosters.horseAnimal : null;
        if (!sensor)
        {
            // RearThreatZone childidagi komponentni topib qo'yish
            sensor = GetComponentInChildren<RearThreatSensor>(true);
        }
    }

    private void OnEnable()
    {
        _lastForward = transform.forward;
        if (_loop == null) _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(decisionInterval);

        while (true)
        {
            Tick();
            yield return wait;
        }
    }

    private void Tick()
    {
        // Faqat NPC uchun ishlasin
        if (!boosters || !boosters.isNpc) return;

        // Zaxira yo'q yoki cooldown tugamagan
        if (boosters.walkZoneCount <= 0) return;
        if (Time.time < _nextAllowedTime) return;

        // Orqada tahdid bo'lmasa
        if (sensor == null || !sensor.HasThreat) return;

        // Yo'l tekisligi (burilmada tashlamaymiz)
        var fwd = transform.forward.normalized;
        float straightness = Vector3.Dot(fwd, _lastForward);
        _lastForward = fwd;
        Debug.Log("Straight way: " + straightness + " : Straight Dot" + straightDotMin);
        if (straightness < straightDotMin) return;

        // Randomlik – hamma NPC bir payt tashlamasin
        if (Random.value < randomFactor) return;

        // Taqiqlangan zonalar bo'lsa shu yerda tekshiring (finish/CP yaqinida) -> return;

        // Tashlaymiz!
        if (alignToGround && TryAlignDropToGround(out var pos, out var rot))
        {
            // Sizning DropWalkTrapNpc() ichida Instantiate bor.
            // Agar pozitsiyani o'zingiz berishni xohlasangiz, DropWalkTrapNpc() ni overloaddan foydalanish yoki o'sha metodni o'zgartirish kerak.
            boosters.DropWalkTrapNpc(); // hozircha oddiy variant (sizdagi kod)
        }
        else
        {
            boosters.DropWalkTrapNpc();
        }

        _nextAllowedTime = Time.time + dropCooldown;
    }

    private bool TryAlignDropToGround(out Vector3 p, out Quaternion r)
    {
        // Sizning DropWalkTrapNpc() transform.orqa tomonga tashlaydi.
        // Bu yerda y'ni yerga “yopishtirish”ni xohlasangiz, DropWalkTrapNpc() ga mos ravishda shu logikani olib kirishingiz mumkin.
        var start = transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(start, Vector3.down, out var hit, groundRay, groundMask, QueryTriggerInteraction.Ignore))
        {
            p = hit.point;
            r = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.LookRotation(-transform.forward);
            return true;
        }
        p = default;
        r = default;
        return false;
    }
}
