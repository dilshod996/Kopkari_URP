using Bozo.ModularCharacters;
using UnityEngine;

public sealed class BozoCustomizationTextureList : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private BozoCustomizationTextureButton texturePrefab;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TextureType textureType = TextureType.Pattern;
    [SerializeField] private bool followCurrentCategory = true;
    [SerializeField] private string fixedCategory;
    [SerializeField] private bool rebuildOnStart = true;

    private void OnEnable()
    {
        if (manager != null)
            manager.OnCategoryChanged.AddListener(HandleCategoryChanged);
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnCategoryChanged.RemoveListener(HandleCategoryChanged);
    }

    private void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();

        if (manager != null)
        {
            manager.OnCategoryChanged.RemoveListener(HandleCategoryChanged);
            manager.OnCategoryChanged.AddListener(HandleCategoryChanged);
        }

        if (rebuildOnStart)
            Rebuild();
    }

    public void Rebuild()
    {
        if (manager == null || texturePrefab == null)
            return;

        Transform target = container != null ? container : transform;
        Clear(target);

        string category = followCurrentCategory ? manager.CurrentCategory : fixedCategory;
        var textures = manager.GetTextures(textureType, category);

        if (emptyState != null)
            emptyState.SetActive(textures.Count == 0);

        for (int i = 0; i < textures.Count; i++)
        {
            TexturePackage package = textures[i];
            BozoCustomizationTextureButton option = Instantiate(texturePrefab, target);
            option.Init(manager, package);
        }
    }

    private void HandleCategoryChanged(string _)
    {
        if (followCurrentCategory)
            Rebuild();
    }

    private static void Clear(Transform target)
    {
        for (int i = target.childCount - 1; i >= 0; i--)
            Destroy(target.GetChild(i).gameObject);
    }
}
