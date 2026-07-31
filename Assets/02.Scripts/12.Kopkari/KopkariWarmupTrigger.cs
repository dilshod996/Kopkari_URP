using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KopkariWarmupTrigger : MonoBehaviour
{
    private KopkariManager manager;
    private bool localPlayerEntered;

    public bool LocalPlayerEntered => localPlayerEntered;

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    public void Prepare(Transform warmupPoint, KopkariManager owner)
    {
        manager = owner;
        localPlayerEntered = false;

        if (warmupPoint == null)
        {
            Deactivate();
            return;
        }

        transform.SetPositionAndRotation(warmupPoint.position, warmupPoint.rotation);
        SetActiveWithGPUI(true);
    }

    public void Deactivate()
    {
        SetActiveWithGPUI(false);
    }

    private void SetActiveWithGPUI(bool active)
    {
        KopkariManager activeManager = manager != null ? manager : KopkariManager.Instance;
        if (activeManager != null)
        {
            activeManager.SetEnvironmentObjectActive(gameObject, active);
            return;
        }

        if (gameObject.activeSelf != active)
            gameObject.SetActive(active);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        KopkariManager activeManager = manager != null ? manager : KopkariManager.Instance;
        if (!localPlayerEntered && activeManager != null && activeManager.IsLocalRiderTransform(other.transform))
            localPlayerEntered = true;
    }

    private void EnsureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
}
