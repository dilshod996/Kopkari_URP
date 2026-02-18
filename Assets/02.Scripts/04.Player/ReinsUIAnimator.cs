using UnityEngine;

public class ReinsUIAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform reinRoot;   // umumiy parent (ReinRoot)
    [SerializeField] private RectTransform leftRein;
    [SerializeField] private RectTransform rightRein;
    [SerializeField] private RectTransform centerGem;

    [Header("Input")]
    [SerializeField] private ReinZone leftZone;
    [SerializeField] private ReinZone rightZone;

    [Header("Stable (Idle) Motion")]
    [SerializeField] private bool stableIdle = true;
    [SerializeField] private float idleBobY = 6f;
    [SerializeField] private float idleRotZ = 1.8f;
    [SerializeField] private float idleSpeed = 1.2f;

    [Header("Pull Anim - Main Side")]
    [SerializeField] private float pullDownY = 46f;      // tortilganda pastga
    [SerializeField] private float pullInX = 18f;        // ichkariga (markazga) siljish
    [SerializeField] private float pullRotZ = 14f;       // aylanish (daraja)

    [Header("Opposite Side Reaction")]
    [SerializeField] private float oppUpY = 12f;         // qarshi tomoni biroz yuqoriga
    [SerializeField] private float oppOutX = 8f;         // tashqariga
    [SerializeField] private float oppRotZ = 5f;         // kichik rotate

    [Header("Whole Rein Tilt (when pulling)")]
    [SerializeField] private float wholeTiltZ = 6f;      // butun U ozgina yon tomonga qiyshayadi
    [SerializeField] private float wholeShiftX = 10f;    // butun U ozgina siljiydi

    [Header("Smoothing")]
    [SerializeField] private float smooth = 18f;

    private Vector2 _rootBasePos;
    private float _rootBaseRotZ;

    private Vector2 _leftBasePos, _rightBasePos, _gemBasePos;
    private float _leftBaseRotZ, _rightBaseRotZ, _gemBaseRotZ;

    private void Awake()
    {
        if (!reinRoot) reinRoot = (RectTransform)transform;

        _rootBasePos = reinRoot.anchoredPosition;
        _rootBaseRotZ = reinRoot.localEulerAngles.z;

        _leftBasePos = leftRein.anchoredPosition;
        _rightBasePos = rightRein.anchoredPosition;
        _gemBasePos = centerGem ? centerGem.anchoredPosition : Vector2.zero;

        _leftBaseRotZ = leftRein.localEulerAngles.z;
        _rightBaseRotZ = rightRein.localEulerAngles.z;
        _gemBaseRotZ = centerGem ? centerGem.localEulerAngles.z : 0f;
    }

    private void LateUpdate()
    {
        float lp = (leftZone && leftZone.IsHeld) ? leftZone.Pull01 : 0f;   // 0..1
        float rp = (rightZone && rightZone.IsHeld) ? rightZone.Pull01 : 0f;

        // signed: + o'ng, - chap (ikkalasi tortilsa farq ishlaydi)
        float signed = Mathf.Clamp(rp - lp, -1f, 1f);
        float absPull = Mathf.Clamp01(Mathf.Max(lp, rp)); // kuchlisi

        // ---- 1) ROOT (stable + pull tilt)
        Vector2 rootTargetPos = _rootBasePos;
        float rootTargetRot = _rootBaseRotZ;

        if (stableIdle)
        {
            float t = Time.unscaledTime * idleSpeed;
            rootTargetPos += new Vector2(0f, Mathf.Sin(t) * idleBobY);
            rootTargetRot += Mathf.Sin(t * 0.9f) * idleRotZ;
        }

        // pull bo'lsa butun rein ham yon tomonga "tortiladi"
        rootTargetPos += new Vector2(signed * wholeShiftX * absPull, 0f);
        rootTargetRot += -signed * wholeTiltZ * absPull; // o'ng tortilsa biroz o'ngga qiyshaysin

        ApplyRoot(rootTargetPos, rootTargetRot);

        // ---- 2) PER-SIDE REIN ANIM
        // Right pull dominant
        if (rp > lp)
        {
            AnimateRightPull(rp, lp);
        }
        // Left pull dominant
        else if (lp > rp)
        {
            AnimateLeftPull(lp, rp);
        }
        else
        {
            // none / equal -> back to base
            RestoreAll();
        }

        // ---- 3) Center gem little feedback (optional)
        if (centerGem)
        {
            // gem biroz "press" bo'lsin
            float press = absPull;
            Vector2 gemTarget = _gemBasePos + new Vector2(0f, -6f * press);
            float gemRot = _gemBaseRotZ + (signed * 2f * press);

            centerGem.anchoredPosition = SmoothV2(centerGem.anchoredPosition, gemTarget);
            centerGem.localRotation = Quaternion.Euler(0, 0, SmoothAngle(centerGem.localEulerAngles.z, gemRot));
        }
    }

    private void AnimateRightPull(float rp, float lp)
    {
        // O'ng rein tortiladi (ichkariga + pastga + rotate)
        Vector2 rPos = _rightBasePos + new Vector2(-pullInX * rp, -pullDownY * rp);
        float rRot = _rightBaseRotZ + (pullRotZ * rp); // o'ng tomonda +Z

        // Chap rein reaksiya (ozgina yuqori + tashqariga + kichik rotate)
        Vector2 lPos = _leftBasePos + new Vector2(-oppOutX * rp, +oppUpY * rp);
        float lRot = _leftBaseRotZ + (-oppRotZ * rp);

        rightRein.anchoredPosition = SmoothV2(rightRein.anchoredPosition, rPos);
        rightRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(rightRein.localEulerAngles.z, rRot));

        leftRein.anchoredPosition = SmoothV2(leftRein.anchoredPosition, lPos);
        leftRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(leftRein.localEulerAngles.z, lRot));
    }

    private void AnimateLeftPull(float lp, float rp)
    {
        // Chap rein tortiladi
        Vector2 lPos = _leftBasePos + new Vector2(+pullInX * lp, -pullDownY * lp);
        float lRot = _leftBaseRotZ + (-pullRotZ * lp); // chap tomonda -Z

        // O'ng reaksiya
        Vector2 rPos = _rightBasePos + new Vector2(+oppOutX * lp, +oppUpY * lp);
        float rRot = _rightBaseRotZ + (+oppRotZ * lp);

        leftRein.anchoredPosition = SmoothV2(leftRein.anchoredPosition, lPos);
        leftRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(leftRein.localEulerAngles.z, lRot));

        rightRein.anchoredPosition = SmoothV2(rightRein.anchoredPosition, rPos);
        rightRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(rightRein.localEulerAngles.z, rRot));
    }

    private void RestoreAll()
    {
        leftRein.anchoredPosition = SmoothV2(leftRein.anchoredPosition, _leftBasePos);
        rightRein.anchoredPosition = SmoothV2(rightRein.anchoredPosition, _rightBasePos);

        leftRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(leftRein.localEulerAngles.z, _leftBaseRotZ));
        rightRein.localRotation = Quaternion.Euler(0, 0, SmoothAngle(rightRein.localEulerAngles.z, _rightBaseRotZ));

        if (centerGem)
        {
            centerGem.anchoredPosition = SmoothV2(centerGem.anchoredPosition, _gemBasePos);
            centerGem.localRotation = Quaternion.Euler(0, 0, SmoothAngle(centerGem.localEulerAngles.z, _gemBaseRotZ));
        }
    }

    private void ApplyRoot(Vector2 pos, float rotZ)
    {
        reinRoot.anchoredPosition = SmoothV2(reinRoot.anchoredPosition, pos);
        reinRoot.localRotation = Quaternion.Euler(0, 0, SmoothAngle(reinRoot.localEulerAngles.z, rotZ));
    }

    private Vector2 SmoothV2(Vector2 current, Vector2 target)
    {
        float k = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
        return Vector2.LerpUnclamped(current, target, k);
    }

    private float SmoothAngle(float current, float target)
    {
        float k = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
        float c = Mathf.DeltaAngle(0, current);
        float t = Mathf.DeltaAngle(0, target);
        return Mathf.LerpUnclamped(c, t, k);
    }
}
