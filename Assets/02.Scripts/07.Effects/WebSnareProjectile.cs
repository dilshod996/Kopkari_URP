using UnityEngine;
using System.Collections;

public class WebSnareProjectile : MonoBehaviour
{
    [Header("Hit")]
    public LayerMask hitMask;                 // faqat HitBox layer qo'y
    public float hitDisableDelay = 0.05f;     // impactdan keyin juda kichik delay

    [Header("Lifetime (hit bo'lmasa)")]
    public float lifeTime = 2.0f;             // hit bo'lmasa shuncha vaqtda despawn

    [Header("Effect")]
    public GameObject hitVfxPrefab;           // optional
    public AudioSource sfxSource;             // optional
    public AudioClip hitClip;                 // optional

    private Rigidbody _rb;
    private bool _hit;
    private Coroutine _lifeCo;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) Debug.LogError("WebSnareProjectile: Rigidbody kerak!");
    }

    private void OnEnable()
    {
        // Pooldan qaytganda reset
        _hit = false;

        CancelInvoke();
        if (_lifeCo != null) StopCoroutine(_lifeCo);
        _lifeCo = StartCoroutine(LifeRoutine());

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.WakeUp();
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (_lifeCo != null) StopCoroutine(_lifeCo);
        _lifeCo = null;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        // hit bo'lmasa ham sahnada qolib ketmasin
        ReturnToPool();
    }

    /// <summary>Checkpoint / shooter bu metodni chaqiradi</summary>
    public void LaunchArc(Vector3 dir, float speed, float upForce)
    {
        if (!_rb) return;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        transform.rotation = Quaternion.LookRotation(dir);

        // settinglarni o'zgartirmaymiz (sen aytgandek minimal)
        _rb.velocity = dir * speed + Vector3.up * upForce;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hit) return;

        // layer check (hitbox layer)
        if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;
        _hit = true;

        // Lifetime coroutine endi kerak emas
        if (_lifeCo != null) { StopCoroutine(_lifeCo); _lifeCo = null; }

        // Snare apply
        // (true) -> inactive bo'lsa ham topsin
        var bc = other.GetComponentInParent<BoostersContainer>(true);
        if (bc != null)
        {
            bc.OnReceiveDamageHandler();
        }
        else
        {
            Debug.Log($"BoostersContainer NOT FOUND. Hit={other.name} root={other.transform.root.name}");
            Invoke(nameof(ReturnToPool), hitDisableDelay);
            return;
        }

        // VFX
        if (hitVfxPrefab != null && !bc.isNpc)
        {
            var p = other.ClosestPoint(transform.position);
            Quaternion rot = Quaternion.identity;
            var fx = SimplePool.Spawn(hitVfxPrefab, p, rot);
        }

        // SFX
        if (sfxSource != null && hitClip != null && !bc.isNpc)
            sfxSource.PlayOneShot(hitClip);

        // impactdan keyin tezda despawn (boshqa riderlarga halaqit bermasin)
        Invoke(nameof(ReturnToPool), hitDisableDelay);
    }

    private void ReturnToPool()
    {
        CancelInvoke();
        // rb state tozalab qo'yamiz, poolga toza qaytsin
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        SimplePool.Despawn(gameObject);
        // agar SimplePool bo'lmasa: gameObject.SetActive(false);
    }
}
