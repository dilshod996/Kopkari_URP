using Bozo.ModularCharacters;
using UnityEngine;

public sealed class BozoCustomizationOptionList : MonoBehaviour
{
    [SerializeField] private BozoCustomizationManager manager;
    [SerializeField] private BozoCustomizationOptionButton optionPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private bool followCurrentCategory = true;
    [SerializeField] private string fixedOutfitTypeName;
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
        if (manager == null || optionPrefab == null)
            return;

        Transform target = container != null ? container : transform;
        Clear(target);

        string category = followCurrentCategory ? manager.CurrentCategory : fixedOutfitTypeName;
        var outfits = manager.GetOutfits(category);

        if (emptyState != null)
            emptyState.SetActive(outfits.Count == 0);

        for (int i = 0; i < outfits.Count; i++)
        {
            Outfit outfit = outfits[i];
            BozoCustomizationOptionButton option = Instantiate(optionPrefab, target);
            option.Init(manager, outfit);
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
