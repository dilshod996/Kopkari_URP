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
    [SerializeField] private GameObject[] faceVariants;
    [SerializeField] private GameObject[] headVariants;
    [SerializeField] private GameObject[] upperBodyVariants;
    [SerializeField] private GameObject[] lowerBodyVariants;

    [SerializeField] private Renderer eyeLeftRenderer;
    [SerializeField] private Renderer eyeRightRenderer;


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
        string faceKey = PlayerPrefs.GetString(Constants.Player.PlayerFaceKey);
        string helmetKey = PlayerPrefs.GetString(Constants.Player.PlayerHelmetKey);
        string upperKey = PlayerPrefs.GetString(Constants.Player.PlayerUpperBodyKey);
        string lowerKey = PlayerPrefs.GetString(Constants.Player.PlayerLowerBodyKey);

        // 2. Har bir qism uchun mos variantni aktiv qilish va materialni qo‘llash
        await ActivateAndApplyMaterial(faceVariants, faceKey);
        await ApplyEyeMaterials(faceKey); // O‘rnatilgan yuz materialini ko‘zlarga qo‘llash
        await ActivateAndApplyMaterial(headVariants, helmetKey);
        await ActivateAndApplyMaterial(upperBodyVariants, upperKey);
        await ActivateAndApplyMaterial(lowerBodyVariants, lowerKey);
    }

    private async Task ActivateAndApplyMaterial(GameObject[] variants, params string[] materialAddresses)
    {
        bool activated = false;

        foreach (var variant in variants)
        {
            string variantName = variant.name;

            if (materialAddresses.Any(addr => variantName.Equals(addr, StringComparison.OrdinalIgnoreCase)))
            {
                variant.SetActive(true);

                // Faqat bitta Renderer bo‘lishi kutilmoqda
                var renderer = variant.GetComponent<Renderer>();

                if (renderer == null)
                {
                    Debug.LogWarning($"⚠️ Renderer not found on {variant.name}");
                    return;
                }

                string matchedAddress = materialAddresses.FirstOrDefault(addr => variantName.Equals(addr, StringComparison.OrdinalIgnoreCase));

                if (MaterialCacheManager.TryGet(matchedAddress, out var cachedMat))
                {
                    renderer.material = cachedMat;
                    Debug.Log($"✅ Applied cached material: {matchedAddress} to {variant.name}");
                }
                else
                {
                    var loadedMat = await AddressablesManager.Instance.LoadAssetAsync<Material>(matchedAddress);
                    if (loadedMat != null)
                    {
                        MaterialCacheManager.Add(matchedAddress, loadedMat);
                        renderer.material = loadedMat;
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
    private async Task ApplyEyeMaterials(string faceMaterialKey)
    {
        if (MaterialCacheManager.TryGet(faceMaterialKey, out var cachedMat))
        {
            eyeLeftRenderer.material = cachedMat;
            eyeRightRenderer.material = cachedMat;
            Debug.Log($"👁️ Applied cached face material to eyes: {faceMaterialKey}");
        }
        else
        {
            var material = await AddressablesManager.Instance.LoadAssetAsync<Material>(faceMaterialKey);
            if (material != null)
            {
                MaterialCacheManager.Add(faceMaterialKey, material);
                eyeLeftRenderer.material = material;
                eyeRightRenderer.material = material;
                Debug.Log($"👁️ Loaded and applied face material to eyes: {faceMaterialKey}");
            }
            else
            {
                Debug.LogError($"❌ Failed to load face material for eyes: {faceMaterialKey}");
            }
        }
    }

    private void OnDestroy()
    {
        // Materiallarni addressablesdan release qilish kerak bo‘lsa — alohida managerda qo‘llash mumkin
        // Ammo static cache ishlatayotganingiz uchun bu yerda hech narsa qilinmaydi
    }
}
