using UnityEngine;
using MalbersAnimations.Controller; // MAnimal
using MalbersAnimations.Scriptables; // Vector2Reference (agar MoveAxis varianti bilan ishlatsangiz)

public class TurnAxisFeeder : MonoBehaviour
{
    [Header("Targets")]
    public MAnimal animal;                   // Otning Animal Controller'i
    [Tooltip("Agar MInput MoveAxis ishlatsa, shu yerga Movement axis (Vector2Reference) ni bering. Bo‘lmasa bo‘sh qoldiring.")]
    public Vector2Reference moveAxisRef;     // ixtiyoriy

    [Header("Feel")]
    [Tooltip("Burilish signali qanchalik kuchli yozilsin (sezgirlik)")]
    [Range(0.1f, 1f)] public float sensitivity = 0.4f;
    [Tooltip("Silliqlash (0.05–0.2 yaxshi)")]
    [Range(0.01f, 0.5f)] public float smoothing = 0.12f;

    float holdDir;      // -1 .. +1 (tugmadan keladi)
    float currentX;     // silliq Horizontal
    float velocityX;    // SmoothDamp helper

    public void SetHold(float dir) => holdDir = Mathf.Clamp(dir, -1f, 1f);
    public void Release() => holdDir = 0f;
    private void Start()
    {
         animal = BaseManager.Instance.horseAnimal;
    }

    void Update()
    {
        if (animal == null) return;

        // Maqsad Horizontal (yumshoq, kam sezgir)
        float targetX = holdDir * sensitivity;

        // SmoothDamp bilan yumshatamiz
        currentX = Mathf.SmoothDamp(currentX, targetX, ref velocityX, smoothing);

        // Vertical = 1f (Always Forward)
        var axis = new Vector2(currentX, 1f);

        // 1) To‘g‘ridan-to‘g‘ri Animal’ga berish:
        animal.SetInputAxis(axis);

        // 2) Agar loyihangiz MInput.MoveAxis (Vector2Reference) ni o‘qiyotgan bo‘lsa, shu yerda uni ham yangilang:
        if (moveAxisRef != null) moveAxisRef.Value = axis;
    }

    private void OnDisable()
    {
        // To‘xtaganda reset
        Release();
        currentX = 0f;
        if (animal) animal.SetInputAxis(Vector2.zero);
        if (moveAxisRef != null) moveAxisRef.Value = Vector2.zero;
    }
}
