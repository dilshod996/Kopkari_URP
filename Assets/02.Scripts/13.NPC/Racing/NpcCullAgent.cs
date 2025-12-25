using UnityEngine;

public class NpcCullAgent : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Renderer visibilitySource;      // horse skinned
    public Animator[] animators;           // horse + rider
    public Renderer[] renderersToToggle;   // optional (far disable)
    public Collider[] collidersToToggle;   // optional

    [HideInInspector] public bool animEnabled = true;
    [HideInInspector] public bool fullEnabled = true;
    [HideInInspector] public float invisibleTimer = 0f;
    [HideInInspector] public float visibleTimer = 0f;

    private void OnEnable()
    {
        NpcCullManager.Register(this);
    }

    private void OnDisable()
    {
        NpcCullManager.Unregister(this);
    }

    public void SetAnimatorsEnabled(bool enabled)
    {
        animEnabled = enabled;

        if (animators == null) return;
        for (int i = 0; i < animators.Length; i++)
        {
            var a = animators[i];
            if (!a) continue;

            // Animator'ni butunlay o'chirmaymiz, root motion o'lmasin:
            a.speed = enabled ? 1f : 0f;
            Debug.Log($"{name} Animator '{a.name}' speed => {a.speed}");
            // qo'shimcha: root motion bo'lsa ham yuralsin desang, Apply Root Motion'ni OFF qil
            // a.applyRootMotion = false;
        }
      

    }


    public void SetFullEnabled(bool enabled)
    {
        fullEnabled = enabled;

        if (renderersToToggle != null)
        {
            for (int i = 0; i < renderersToToggle.Length; i++)
                if (renderersToToggle[i]) renderersToToggle[i].enabled = enabled;
        }

        if (collidersToToggle != null)
        {
            for (int i = 0; i < collidersToToggle.Length; i++)
                if (collidersToToggle[i]) collidersToToggle[i].enabled = enabled;
        }
    }
}
