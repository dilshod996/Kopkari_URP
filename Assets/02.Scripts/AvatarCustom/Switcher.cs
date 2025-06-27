using UnityEngine;
using System.Collections;

public class OvalSwitcher : MonoBehaviour
{
    [Header("Pozitsiyalar")]
    public Transform startPos;
    public Transform targetPos;

    [Header("Obyektlar")]
    public Transform playerObj;
    public Transform horseObj;

    [Header("Trayektoriya Sozlamalari")]
    public float arcHeight = 1.5f;
    public float playerSideCurveOffset = 2.5f;
    public float horseSideCurveOffset = 3.0f;
    public float moveDuration = 1f;

    private bool isPlayerInFront = true; // dastlab player targetPos'da
    private bool isPlayerBouncing = false;
    private bool isHorseBouncing = false;

    private RotateObj playerRotateScript;
    private RotateObj horseRotateScript;
    void Start()
    {
        playerRotateScript = playerObj.GetComponent<RotateObj>();
        horseRotateScript = horseObj.GetComponent<RotateObj>();

        // Boshlanishda: faqat player oldinda
        playerRotateScript.enabled = true;
        horseRotateScript.enabled = false;
        // RootMotion off
        //playerObj.GetComponentInChildren<Animator>().applyRootMotion = false;
        //horseObj.GetComponentInChildren<Animator>().applyRootMotion = false;

        //// Ixtiyoriy: localPosition reset (Agar Animator o‘zgartirayotgan bo‘lsa)
        //playerObj.GetComponentInChildren<Transform>().localPosition = Vector3.zero;
        //horseObj.GetComponentInChildren<Transform>().localPosition = Vector3.zero;
    }

    public void OnHorseButtonClick()
    {
        if (!isPlayerInFront)
        {
            StartCoroutine(SmallBounceEffect(horseObj));
            return;
        }

        isPlayerInFront = false;

        playerRotateScript.enabled = false;   // player endi orqada
        horseRotateScript.enabled = true;   // horse endi oldinda
        StartCoroutine(MoveWithOvalArc(playerObj, targetPos.position, startPos.position, false, playerSideCurveOffset));
        StartCoroutine(MoveWithOvalArc(horseObj, startPos.position, targetPos.position, true, horseSideCurveOffset));
    }

    public void OnPlayerButtonClick()
    {
        if (isPlayerInFront)
        {
            StartCoroutine(SmallBounceEffect(playerObj));
            return;
        }

        isPlayerInFront = true;
        playerRotateScript.enabled = true;   // player endi oldinda
        horseRotateScript.enabled = false;     // horse orqaga ketdi

        StartCoroutine(MoveWithOvalArc(playerObj, startPos.position, targetPos.position, true, playerSideCurveOffset));
        StartCoroutine(MoveWithOvalArc(horseObj, targetPos.position, startPos.position, false, horseSideCurveOffset));
    }

    IEnumerator MoveWithOvalArc(Transform obj, Vector3 from, Vector3 to, bool fromLeft, float sideCurveOffset)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 up = Vector3.up * arcHeight;
        Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 side = fromLeft ? -perp * sideCurveOffset : perp * sideCurveOffset;

        Vector3 mid = (from + to) / 2 + up + side;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            Vector3 p1 = Vector3.Lerp(from, mid, smoothT);
            Vector3 p2 = Vector3.Lerp(mid, to, smoothT);
            obj.position = Vector3.Lerp(p1, p2, smoothT);

            yield return null;
        }

        obj.position = to;
    }

    IEnumerator SmallBounceEffect(Transform obj)
    {
        if (obj == playerObj && isPlayerBouncing) yield break;
        if (obj == horseObj && isHorseBouncing) yield break;

        if (obj == playerObj) isPlayerBouncing = true;
        if (obj == horseObj) isHorseBouncing = true;

        Vector3 originalPos = obj.position;
        Vector3 peakPos = originalPos + new Vector3(0, 0.5f, 0);

        float t = 0f;
        float duration = 0.3f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curve = Mathf.Sin(t * Mathf.PI); // Up and down
            obj.position = Vector3.Lerp(originalPos, peakPos, curve);
            yield return null;
        }

        obj.position = originalPos;

        if (obj == playerObj) isPlayerBouncing = false;
        if (obj == horseObj) isHorseBouncing = false;
    }
}
