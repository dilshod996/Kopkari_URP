using MTAssets.SkinnedMeshCombiner;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AIAvatarSkinLoader : MonoBehaviour
{
    [Header("Avatar Id (Catalog)")]
    [SerializeField] private string avatarId = "player_01";

    [Header("Slots (SkinnedMeshRenderers)")]
    [SerializeField] private SkinnedMeshRenderer hairSMR;
    [SerializeField] private SkinnedMeshRenderer upperSMR;
    [SerializeField] private SkinnedMeshRenderer lowerSMR;

    [Header("Defaults (optional, keep as-is if empty)")]
    [SerializeField] private SkinnedMeshRenderer headSMR; // default qoladi
    [SerializeField] private SkinnedMeshRenderer handSMR; // default qoladi

    [Header("Catalog SlotIds")]
    [SerializeField] private string hairSlotId = "Hair";
    [SerializeField] private string upperSlotId = "Upper";
    [SerializeField] private string lowerSlotId = "Lower";
    [SerializeField] private string headSlotId = "Head";
    [SerializeField] private string handSlotId = "Hand";

    [Header("Rules")]
    [SerializeField] private bool excludeDefaultFromRandom = false; // xohlasang true qil
    [SerializeField] private bool forceApplyDefaultHeadHand = false; // defaultni ham catalogdan apply qilsinmi
    [Header("MT Combiner")]
    [SerializeField] private SkinnedMeshCombiner combiner;
    [SerializeField] private SkinnedMeshRenderer rigReferenceSMR;

    public void SetAvatarId(string id) => avatarId = id;

    /// <summary>
    /// AI uchun random skin: Hair/FaceHair/Upper/Lower random.
    /// Head/Hand default (prefabda qoladi yoki catalog default apply).
    /// </summary>
    public async Task RandomizeAsync(int seed)
    {
        if (string.IsNullOrWhiteSpace(avatarId)) avatarId = "player_01";

        await AddressablesService.Instance.EnsureInitializedAsync();
        await PlayerCatalogProvider.Instance.EnsureCatalogAsync();

        var rng = new System.Random(seed);

        // (optional) Head/Hand defaultni catalogdan ham apply qilib yuboramiz
        if (forceApplyDefaultHeadHand)
        {
            await ApplyDefaultIfPossible(headSMR, headSlotId);
            await ApplyDefaultIfPossible(handSMR, handSlotId);
        }

        await ApplyRandomSlot(hairSMR, hairSlotId, rng);
        //await ApplyRandomSlot(faceHairSMR, faceHairSlotId, rng);
        await ApplyRandomSlot(upperSMR, upperSlotId, rng);
        await ApplyRandomSlot(lowerSMR, lowerSlotId, rng);
    }

    private async Task ApplyDefaultIfPossible(SkinnedMeshRenderer smr, string slotId)
    {
        if (smr == null) return;
        string defOpt = PlayerCatalogProvider.Instance.GetDefaultOptionId(avatarId, slotId);
        if (string.IsNullOrEmpty(defOpt)) return;

        var entry = await PlayerCatalogProvider.Instance.FindAsync(avatarId, slotId, defOpt);
        if (entry != null)
            await ApplyEntry(smr, entry);
    }

    private async Task ApplyRandomSlot(SkinnedMeshRenderer smr, string slotId, System.Random rng)
    {
        if (smr == null) return;

        var options = await PlayerCatalogProvider.Instance.GetOptionsAsync(avatarId, slotId);
        if (options == null || options.Count == 0) return;

        CatalogEntry pick = null;

        if (excludeDefaultFromRandom)
        {
            // default bo'lmaganlardan random
            var pool = new List<CatalogEntry>();
            for (int i = 0; i < options.Count; i++)
                if (options[i] != null && !options[i].IsDefault) pool.Add(options[i]);

            if (pool.Count > 0)
                pick = pool[rng.Next(0, pool.Count)];
        }

        // fallback: hammasidan random
        if (pick == null)
            pick = options[rng.Next(0, options.Count)];

        if (pick != null)
            await ApplyEntry(smr, pick);
    }

    private async Task ApplyEntry(SkinnedMeshRenderer smr, CatalogEntry entry)
    {
        if (smr == null || entry == null) return;

        // Mesh
        if (!string.IsNullOrWhiteSpace(entry.MeshKey))
        {
            var mesh = await AddressablesService.Instance.LoadAssetAsync<Mesh>(entry.MeshKey);
            if (mesh != null) smr.sharedMesh = mesh;
            CopyRig(smr);
        }

        // Material (cache)
        if (!string.IsNullOrWhiteSpace(entry.MaterialKey))
        {
            if (MaterialCacheManager.TryGet(entry.MaterialKey, out var cached) && cached != null)
            {
                smr.sharedMaterial = cached;
            }
            else
            {
                var mat = await AddressablesService.Instance.LoadAssetAsync<Material>(entry.MaterialKey);
                if (mat != null)
                {
                    MaterialCacheManager.Add(entry.MaterialKey, mat);
                    smr.sharedMaterial = mat;
                }
            }
        }
    }

    #region Mesh Combiner
    public async Task CombineWithMTAsync()
    {
        //PrepareSlotsForCombine(hairSMR, upperSMR, lowerSMR);
        await Task.Yield();

        combiner.mergeMethod = SkinnedMeshCombiner.MergeMethod.OneMeshPerMaterial;
        combiner.oneMeshPerMaterialParams.mergeOnlyEqualRootBones = true; // tavsiya
        combiner.blendShapesSupport = SkinnedMeshCombiner.BlendShapesSupport.Enabled;
        combiner.rootBoneToUse = SkinnedMeshCombiner.RootBoneToUse.Manual;

        combiner.CombineMeshes();

    }

    private bool IsValidSkinned(SkinnedMeshRenderer smr)
    {
        if (smr == null) return false;
        var m = smr.sharedMesh;
        if (m == null) return false;
        if (m.bindposes == null || m.bindposes.Length == 0) return false;
        if (m.boneWeights == null || m.boneWeights.Length == 0) return false;
        if (smr.bones == null || smr.bones.Length == 0) return false;
        if (smr.rootBone == null) return false;
        return true;
    }

    private void PrepareSlotsForCombine(params SkinnedMeshRenderer[] slots)
    {
        foreach (var s in slots)
            if (s != null) s.gameObject.SetActive(IsValidSkinned(s));
    }

    private void CopyRig(SkinnedMeshRenderer smr)
    {
        if (smr == null || rigReferenceSMR == null) return;
        smr.rootBone = rigReferenceSMR.rootBone;
        smr.bones = rigReferenceSMR.bones;
    }
    #endregion
}
