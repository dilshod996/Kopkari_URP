using System.Collections.Generic;
using UnityEngine;

public class NpcCullManager : MonoBehaviour
{
    private static readonly List<NpcCullAgent> Agents = new();

    [Header("Camera")]
    [SerializeField] private Camera gameCamera;

    [Header("Distances")]
    [SerializeField] private float animOffDistance = 60f;   // 60m+ => animator OFF (agar ko'rinmasa ham)
    [SerializeField] private float fullOffDistance = 100f;  // 100m+ => (optional) renderer OFF

    [Header("Delays")]
    [SerializeField] private float invisibleDelay = 0.2f;
    [SerializeField] private float visibleDelay = 0.05f;

    [Header("Tick")]
    [SerializeField] private float tickInterval = 0.1f; // 10Hz
    [SerializeField] private int perTickBudget = 6;     // har tickda nechta NPC tekshiradi (spread)

    [Header("Options")]
    [SerializeField] private bool useFullOff = false;   // default: false (physics muammolarni oldini oladi)

    private int _cursor = 0;

    // Play qayta bosilganda / load bo'lganda static tozalanadi (Enter Play Mode Options bo'lsa ham)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Agents.Clear();
    }

    public static void Register(NpcCullAgent a)
    {
        if (!a) return;
        if (!Agents.Contains(a)) Agents.Add(a);
    }

    public static void Unregister(NpcCullAgent a)
    {
        if (!a) return;
        Agents.Remove(a);
    }

    private void Awake()
    {
        if (!gameCamera) gameCamera = Camera.main;
    }

    private void OnEnable()
    {
        // Tickni invoke bilan qilamiz: NPC'larda Update yo'q
        InvokeRepeating(nameof(Tick), tickInterval, tickInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Tick));
    }

    private void Tick()
    {
        if (!gameCamera) gameCamera = Camera.main;
        if (!gameCamera) return;

        int count = Agents.Count;
        if (count == 0) return;

        int checks = Mathf.Min(perTickBudget, count);

        for (int n = 0; n < checks; n++)
        {
            // Cursor normalize
            if (_cursor >= Agents.Count) _cursor = 0;
            if (Agents.Count == 0) return;

            var a = Agents[_cursor];

            // Destroy bo'lgan agentlarni tozalash (scene reload / pooling)
            if (!a)
            {
                Agents.RemoveAt(_cursor);
                // _cursor ni oshirmaymiz (remove bo'lgani uchun shu indexga keyingi keladi)
                continue;
            }

            _cursor++;

            if (!a.visibilitySource)
                continue;

            float dist = Vector3.Distance(gameCamera.transform.position, a.transform.position);

            bool wantAnimByDistance = dist < animOffDistance;
            bool wantFullByDistance = dist < fullOffDistance;

            // SceneView ignore: faqat GameCamera frustum
            bool visibleInGame = IsVisibleInGameCamera(a.visibilitySource, gameCamera);

            // Animator faqat: (yaqin) AND (ko'rinyapti)
            bool wantAnim = wantAnimByDistance && visibleInGame;

            // Full OFF - faqat xohlasang (default false)
            bool wantFull = wantFullByDistance;

            // ---- Delay logic (Animator) ----
            if (wantAnim)
            {
                a.invisibleTimer = 0f;
                a.visibleTimer += tickInterval;

                if (!a.animEnabled && a.visibleTimer >= visibleDelay)
                    a.SetAnimatorsEnabled(true);
            }
            else
            {
                a.visibleTimer = 0f;
                a.invisibleTimer += tickInterval;

                if (a.animEnabled && a.invisibleTimer >= invisibleDelay)
                    a.SetAnimatorsEnabled(false);
            }

            // ---- Full toggle (Renderer/Collider) ----
            // Eslatma: collider disable qilish NPC'ni "osmonga uchirishi" mumkin.
            // Shuning uchun useFullOff=false default.
            if (useFullOff)
            {
                if (wantFull)
                {
                    if (!a.fullEnabled) a.SetFullEnabled(true);
                }
                else
                {
                    if (a.fullEnabled) a.SetFullEnabled(false);
                }
            }
        }
    }

    private static bool IsVisibleInGameCamera(Renderer r, Camera cam)
    {
        if (!r || !cam) return false;

        // Game camera frustum bo'yicha tekshiradi => SceneView ta'sir qilmaydi
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    }
}
