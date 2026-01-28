using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerSkinLoader : MonoBehaviour
{
    [Header("Mesh container holding materials")]
    [SerializeField] private Renderer[] targetRenderers;

    //[Header("Material Addressable Keys (must match renderers)")]
    //[SerializeField] private string[] materialAddresses;
    [Header("Grouped renderers for Player Skins")]
    [SerializeField] private GameObject[] faceHairVariants;
    [SerializeField] private GameObject[] hatVariants;
    [SerializeField] private GameObject[] headVariants;
    [SerializeField] private GameObject[] upperBodyVariants;
    [SerializeField] private GameObject[] lowerBodyVariants;
    [SerializeField] private GameObject[] handVariants;




    private Material[] loadedMaterials;

    private void Start()
    {
        // Tashqi managerdan chaqirish tavsiya: await loader.ApplyMaterials();
    }

    public async Task ApplyMaterials()
    {
        //if (materialAddresses.Length != targetRenderers.Length)
        //{
        //    Debug.LogError("❌ Material addresses and renderers count mismatch");
        //    return;
        //}

        //loadedMaterials = new Material[materialAddresses.Length];
        //for (int i = 0; i < materialAddresses.Length; i++)
        //{
        //    string address = materialAddresses[i];

        //    if (MaterialCacheManager.TryGet(address, out var cachedMat))
        //    {
        //        loadedMaterials[i] = cachedMat;
        //        targetRenderers[i].material = cachedMat;
        //        Debug.Log($"✅ Applied cached material: {address}");
        //    }
        //    else
        //    {
        //        var material = await AddressablesManager.Instance.LoadAssetAsync<Material>(address);
        //        if (material != null)
        //        {
        //            MaterialCacheManager.Add(address, material);
        //            loadedMaterials[i] = material;
        //            targetRenderers[i].material = material;
        //            Debug.Log($"✅ Applied and cached material: {address}");
        //        }
        //        else
        //        {
        //            Debug.LogError($"❌ Failed to load material: {address}");
        //        }
        //    }
        //}
        // 1. PlayerPrefs dan material addresslarini olish
        string face = PlayerPrefs.GetString(Constants.Player.PlayerFaceKey, "Face");
        string upper = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey, "UpperBody2");
        string lower = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey, "LowerBody1");
        string helmet = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey, "Helmet1");

        // 2. Materiallarni ro‘yxatga olish (ko‘rinish tartibida)
        List<string> addresses = new List<string>
        {
            face,     // Face
            face,     // EyeLeft (reuse Face)
            face,     // EyeRight (reuse Face)
            helmet,
            upper,
            lower
        };

        if (addresses.Count != targetRenderers.Length)
        {
            Debug.LogError("❌ Addresses count and renderers count mismatch.");
            return;
        }

        loadedMaterials = new Material[addresses.Count];
        for (int i = 0; i < addresses.Count; i++)
        {
            string address = addresses[i];

            if (MaterialCacheManager.TryGet(address, out var cachedMat))
            {
                loadedMaterials[i] = cachedMat;
                targetRenderers[i].material = cachedMat;
                Debug.Log($"✅ Applied cached material: {address}");
            }
            else
            {
                var material = await AddressablesManager.Instance.LoadAssetAsync<Material>(address);
                if (material != null)
                {
                    MaterialCacheManager.Add(address, material);
                    loadedMaterials[i] = material;
                    targetRenderers[i].material = material;
                    Debug.Log($"✅ Applied and cached material: {address}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load material: {address}");
                }
            }
        }

       
    }

    public async Task ApplySkins()
    {
        // 1. PlayerPrefs orqali material nomlarini olish
        string helmetKey = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey);
        string headKey = PlayerPrefs.GetString(Constants.Player.PlayerHeadKey);
        string faceKey = PlayerPrefs.GetString(Constants.Player.PlayerFaceHairKey);
        string handKey = PlayerPrefs.GetString(Constants.Player.PlayerHand);
        string upperKey = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey);
        string lowerKey = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey);

        // 2. Har bir qism uchun mos variantni aktiv qilish va materialni qo‘llash
        await ActivateAndApplyMaterial(hatVariants, helmetKey);
        await ActivateAndApplyMaterial(headVariants, headKey);
        await ActivateAndApplyMaterial(faceHairVariants, faceKey);
        await ActivateAndApplyMaterial(handVariants, handKey);
        await ActivateAndApplyMaterial(upperBodyVariants, upperKey);
        await ActivateAndApplyMaterial(lowerBodyVariants, lowerKey);
    }
    private async Task ActivateAndApplyMaterial(GameObject[] variants, params string[] materialAddresses)
    {
        if (variants == null || variants.Length == 0) return;
        if (materialAddresses == null || materialAddresses.Length == 0)
        {
            // Hamma variantlarni o'chirish (xohlasang)
            foreach (var v in variants) if (v) v.SetActive(false);
            return;
        }

        // Addressables init (agar kerak bo‘lsa)
        await AddressablesService.Instance.EnsureInitializedAsync();

        bool activated = false;

        // Tez match uchun
        var addressSet = materialAddresses
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var variant in variants)
        {
            if (variant == null) continue;

            string variantName = variant.name;

            // Variant nomi addresslardan biriga mos kelsa
            if (addressSet.Contains(variantName))
            {
                variant.SetActive(true);

                var renderer = variant.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"⚠️ Renderer not found on {variant.name}");
                    continue; // return emas, boshqa variantlarni ham tekshir
                }

                string matchedAddress = materialAddresses
                    .First(a => variantName.Equals(a, StringComparison.OrdinalIgnoreCase));

                // 1) Cache bor-mi?
                if (MaterialCacheManager.TryGet(matchedAddress, out var cachedMat) && cachedMat != null)
                {
                    // IMPORTANT: sharedMaterial (material emas)
                    renderer.sharedMaterial = cachedMat;
                    // Debug.Log($"✅ Applied cached material: {matchedAddress} to {variant.name}");
                }
                else
                {
                    // 2) AddressablesService orqali load
                    var loadedMat = await AddressablesService.Instance.LoadAssetAsync<Material>(matchedAddress);

                    if (loadedMat != null)
                    {
                        MaterialCacheManager.Add(matchedAddress, loadedMat);
                        renderer.sharedMaterial = loadedMat;
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to load material: {matchedAddress}");
                    }
                }

                activated = true;
            }
            else
            {
                variant.SetActive(false);
            }
        }

        if (!activated)
            Debug.LogWarning($"⚠️ No matching variant found for materials: [{string.Join(", ", materialAddresses)}]");
    }
    //private async Task ActivateAndApplyMaterial(GameObject[] variants, params string[] materialAddresses)
    //{
    //    bool activated = false;

    //    foreach (var variant in variants)
    //    {
    //        string variantName = variant.name;

    //        if (materialAddresses.Any(addr => variantName.Equals(addr, StringComparison.OrdinalIgnoreCase)))
    //        {
    //            variant.SetActive(true);

    //            // Faqat bitta Renderer bo‘lishi kutilmoqda
    //            var renderer = variant.GetComponent<Renderer>();

    //            if (renderer == null)
    //            {
    //                Debug.LogWarning($"⚠️ Renderer not found on {variant.name}");
    //                return;
    //            }

    //            string matchedAddress = materialAddresses.FirstOrDefault(addr => variantName.Equals(addr, StringComparison.OrdinalIgnoreCase));

    //            if (MaterialCacheManager.TryGet(matchedAddress, out var cachedMat))
    //            {
    //                renderer.material = cachedMat;
    //                Debug.Log($"✅ Applied cached material: {matchedAddress} to {variant.name}");
    //            }
    //            else
    //            {
    //                var loadedMat = await AddressablesManager.Instance.LoadAssetAsync<Material>(matchedAddress);
    //                if (loadedMat != null)
    //                {
    //                    MaterialCacheManager.Add(matchedAddress, loadedMat);
    //                    renderer.material = loadedMat;
    //                }
    //                else
    //                {
    //                    Debug.LogError($"❌ Failed to load material: {matchedAddress}");
    //                }
    //            }

    //            activated = true;
    //        }
    //        else
    //        {
    //            variant.SetActive(false);
    //        }
    //    }

    //    if (!activated)
    //        Debug.LogWarning($"⚠️ No matching variant found for materials: [{string.Join(", ", materialAddresses)}]");
    //}

    private void OnDestroy()
    {
        // Materiallarni addressablesdan release qilish kerak bo‘lsa — alohida managerda qo‘llash mumkin
        // Ammo static cache ishlatayotganingiz uchun bu yerda hech narsa qilinmaydi
    }
}
