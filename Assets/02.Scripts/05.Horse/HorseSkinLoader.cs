using System.Threading.Tasks;
using UnityEngine;

public class HorseSkinLoader : MonoBehaviour
{
    [Header("Horse Mesh Renderers")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer eyesRenderer;
    [SerializeField] private Renderer maneRenderer;
    [SerializeField] private Renderer tailRenderer;
    [SerializeField] private Renderer reinsRenderer;
    [SerializeField] private Renderer saddleRenderer;
    [SerializeField] private Renderer reinsHeadRenderer;

    public async Task ApplySkins()
    {
        await ApplyMaterial(bodyRenderer, PlayerPrefs.GetString(Constants.Horse.HorseBodyKey));
        await ApplyMaterial(eyesRenderer, PlayerPrefs.GetString(Constants.Horse.HorseEyesKey));
        await ApplyMaterial(maneRenderer, PlayerPrefs.GetString(Constants.Horse.HorseManeKey));
        await ApplyMaterial(tailRenderer, PlayerPrefs.GetString(Constants.Horse.HorseTailKey));
        await ApplyMaterial(reinsRenderer, PlayerPrefs.GetString(Constants.Horse.HorseReinsKey));
        await ApplyMaterial(saddleRenderer, PlayerPrefs.GetString(Constants.Horse.HorseSaddleKey));
        await ApplyMaterial(reinsHeadRenderer, PlayerPrefs.GetString(Constants.Horse.HorseReinsHeadKey));
    }

    private async Task ApplyMaterial(Renderer renderer, string materialAddress)
    {
        if (renderer == null || string.IsNullOrEmpty(materialAddress))
            return;

        if (MaterialCacheManager.TryGet(materialAddress, out var cachedMat))
        {
            renderer.material = cachedMat;
        }
        else
        {
            var mat = await AddressablesManager.Instance.LoadAssetAsync<Material>(materialAddress);
            if (mat != null)
            {
                MaterialCacheManager.Add(materialAddress, mat);
                renderer.material = mat;
            }
            else
            {
                Debug.LogError($"❌ Failed to load material: {materialAddress}");
            }
        }
    }
}
