using UnityEngine;

[DisallowMultipleComponent]
public sealed class KopkariEnvironmentReferences : MonoBehaviour
{
    [Header("Required Kopkari Environment References")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Transform ulakBottomObject;
    [SerializeField] private KopkariWarmupTrigger warmupTrigger;

    public GameObject TargetObject => targetObject;
    public Transform UlakBottomObject => ulakBottomObject;
    public KopkariWarmupTrigger WarmupTrigger => warmupTrigger;

    public bool HasRequiredReferences =>
        targetObject != null &&
        ulakBottomObject != null &&
        warmupTrigger != null;
}
