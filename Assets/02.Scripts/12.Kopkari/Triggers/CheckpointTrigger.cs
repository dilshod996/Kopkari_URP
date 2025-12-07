using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public GameObject passedVFX;
    private bool isPassedWithGoat = false;
    public bool IsPassedWithGoat => isPassedWithGoat;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RacingHead"))
            return;
        // 1) NPC marshruti (faqat shu rider uchun)
        GameObject riderRoot = other.transform.root.gameObject;
        var npc = riderRoot.GetComponent<NPCGetLamb_CodeAI>();
        
        if (npc != null)   // faqat uloq ushlab turgan NPC uchun
        {
            npc.OnCheckpointReached(this);   // bu NPCning MoveToNextPoint() ni chaqiradi
        }
        if (BaseManager.Instance == null) return;

        
        BaseManager.Instance.OnCheckpointReached(this, riderRoot);
    }

    public void MarkPassedWithGoat()
    {
        if (isPassedWithGoat) return;
        isPassedWithGoat = true;

        var col = GetComponent<Collider>();

        if (passedVFX != null)
            passedVFX.SetActive(true);
    }
}
