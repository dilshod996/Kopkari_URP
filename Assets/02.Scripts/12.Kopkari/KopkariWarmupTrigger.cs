using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class KopkariWarmupTrigger : MonoBehaviour
{
    private readonly HashSet<AIKopkariRider> enteredAIRiders = new HashSet<AIKopkariRider>();
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
        enteredAIRiders.Clear();
        localPlayerEntered = false;

        if (warmupPoint == null)
        {
            Deactivate();
            return;
        }

        transform.SetPositionAndRotation(warmupPoint.position, warmupPoint.rotation);
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public bool HasAIRider(AIKopkariRider rider)
    {
        return rider != null && enteredAIRiders.Contains(rider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        AIKopkariRider aiRider = other.GetComponentInParent<AIKopkariRider>();
        if (aiRider == null && other.transform.root != null)
            aiRider = other.transform.root.GetComponentInChildren<AIKopkariRider>(true);

        if (aiRider != null)
        {
            if (enteredAIRiders.Add(aiRider))
                aiRider.MarkRoundWarmupQualified();
            return;
        }

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
