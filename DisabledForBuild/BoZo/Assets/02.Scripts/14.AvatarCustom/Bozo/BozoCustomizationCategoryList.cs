using UnityEngine;

public sealed class BozoCustomizationCategoryList : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private BozoCustomizationCategoryButton buttonPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private bool buildOnStart = true;

    private void Start()
    {
        if (buildOnStart)
            Rebuild();
    }

    public void Rebuild()
    {
        if (manager == null)
            manager = FindObjectOfType<BozoCustomizationManager>();

        if (manager == null || buttonPrefab == null)
            return;

        Transform target = container != null ? container : transform;
        Clear(target);

        var categories = manager.Categories;
        for (int i = 0; i < categories.Count; i++)
        {
            BozoCustomizationCategory category = categories[i];
            if (category == null || string.IsNullOrEmpty(category.outfitTypeName))
                continue;

            BozoCustomizationCategoryButton button = Instantiate(buttonPrefab, target);
            button.Init(manager, category.outfitTypeName, string.IsNullOrEmpty(category.displayName) ? category.outfitTypeName : category.displayName);
        }
    }

    private static void Clear(Transform target)
    {
        for (int i = target.childCount - 1; i >= 0; i--)
            Destroy(target.GetChild(i).gameObject);
    }
}
