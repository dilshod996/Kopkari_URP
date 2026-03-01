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

        // Rider root
        GameObject riderRoot = other.transform.root.gameObject;

        // 1) NPC marshruti (faqat shu rider uchun)
        var npc = riderRoot.GetComponent<NPCGetLamb_CodeAI>();
        if (npc != null)
        {
            npc.OnCheckpointReached(this);
        }

        // 2) KopkariManager (oldin BaseManager edi)
        if (KopkariManager.Instance == null) return;
        KopkariManager.Instance.OnCheckpointReached(this, riderRoot);
    }

    public void MarkPassedWithGoat()
    {
        if (isPassedWithGoat) return;

        isPassedWithGoat = true;

        // ✅ passed bo‘lganda VFX yoqamiz (agar sen "passed effect" ni ko‘rsatmoqchi bo‘lsang)
        if (passedVFX != null)
            passedVFX.SetActive(true);
    }

    /// <summary>
    /// ✅ Yangi round boshlanganda checkpoint holatini reset qilish uchun.
    /// KopkariManager.StartGame() ichida hammasiga shu methodni chaqir.
    /// </summary>
    public void ResetPassed()
    {
        isPassedWithGoat = false;

        // ✅ reset bo‘lganda VFX o‘chirib qo‘yamiz (yoki aksincha, sening dizayn bo‘yicha)
        if (passedVFX != null)
            passedVFX.SetActive(false);
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!other.CompareTag("RacingHead"))
    //        return;
    //    // 1) NPC marshruti (faqat shu rider uchun)
    //    GameObject riderRoot = other.transform.root.gameObject;
    //    var npc = riderRoot.GetComponent<NPCGetLamb_CodeAI>();

    //    if (npc != null)   // faqat uloq ushlab turgan NPC uchun
    //    {
    //        npc.OnCheckpointReached(this);   // bu NPCning MoveToNextPoint() ni chaqiradi
    //    }
    //    if (BaseManager.Instance == null) return;


    //    BaseManager.Instance.OnCheckpointReached(this, riderRoot);
    //}

    //public void MarkPassedWithGoat()
    //{
    //    if (isPassedWithGoat) return;
    //    isPassedWithGoat = true;

    //    var col = GetComponent<Collider>();

    //    if (passedVFX != null)
    //        passedVFX.SetActive(false);
    //}
}
