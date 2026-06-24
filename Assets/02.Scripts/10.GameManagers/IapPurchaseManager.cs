using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IapPurchaseManager : MonoBehaviour
{
    public static IapPurchaseManager Instance { get; private set; }

    public bool IsReady => storeController != null && productsFetched;

    public event Action<string> OnPurchaseStarted;
    public event Action<string, int> OnNyufiyPurchaseSucceeded;
    public event Action<string, string> OnPurchaseFailed;

    private StoreController storeController;
    private bool productsFetched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject iapObject = new GameObject(nameof(IapPurchaseManager));
        iapObject.AddComponent<IapPurchaseManager>();
    }

    private readonly Dictionary<string, int> nyufiyProducts = new Dictionary<string, int>
    {
        { "nyufiy_4350", 4350 },
        { "nyufiy_7750", 7750 },
        { "nyufiy_10350", 10350 },
        { "nyufiy_15900", 15900 },
        { "nyufiy_23600", 23600 },
        { "nyufiy_38500", 38500 },
        { "nyufiy_68000", 68000 }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIap();
    }

    private async void InitializeIap()
    {
        storeController = UnityIAPServices.StoreController();

        storeController.OnStoreConnected += OnStoreConnected;
        storeController.OnStoreDisconnected += OnStoreDisconnected;
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailedInternal;

        Debug.Log("IAP: Connecting to store.");
        await storeController.Connect();
    }

    public void BuyNyufiy(CoinCard.NyufiyProduct product)
    {
        string productId = product.ToString();

#if UNITY_EDITOR
        SimulateEditorNyufiyPurchase(productId);
        return;
#endif

        if (storeController == null)
        {
            OnPurchaseFailed?.Invoke(productId, "Store is not initialized yet.");
            return;
        }

        OnPurchaseStarted?.Invoke(productId);
        storeController.PurchaseProduct(productId);
    }

#if UNITY_EDITOR
    private void SimulateEditorNyufiyPurchase(string productId)
    {
        DestroyFakeStoreWindowIfPresent();

        if (!nyufiyProducts.TryGetValue(productId, out int amount))
        {
            OnPurchaseFailed?.Invoke(productId, "Unknown editor test product.");
            return;
        }

        Debug.Log($"IAP Editor: Simulated purchase success. Product={productId}, Nyufiy={amount}");
        CurrencyManager.Instance?.AddNyufiy(amount, true);
        OnNyufiyPurchaseSucceeded?.Invoke(productId, amount);
    }

    private void DestroyFakeStoreWindowIfPresent()
    {
        GameObject fakeStoreWindow = GameObject.Find("UIFakeStoreWindow");
        if (fakeStoreWindow != null)
            Destroy(fakeStoreWindow);
    }
#endif

    public int GetNyufiyAmount(CoinCard.NyufiyProduct product)
    {
        return nyufiyProducts.TryGetValue(product.ToString(), out int amount) ? amount : 0;
    }

    private void OnStoreConnected()
    {
        Debug.Log("IAP: Store connected.");

        List<ProductDefinition> products = nyufiyProducts.Keys
            .Select(productId => new ProductDefinition(productId, ProductType.Consumable))
            .ToList();

        storeController.FetchProducts(products);
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        productsFetched = false;
        Debug.LogWarning($"IAP: Store disconnected. {description.message}");
    }

    private void OnProductsFetched(List<Product> products)
    {
        productsFetched = true;
        Debug.Log($"IAP: Products fetched successfully. Count={products.Count}");
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        productsFetched = false;
        Debug.LogWarning($"IAP: Product fetch failed. Reason={failure.FailureReason}");
    }

    private void OnPurchasePending(PendingOrder order)
    {
        Product product = GetFirstProduct(order);
        string productId = product?.definition.id;

        if (string.IsNullOrEmpty(productId) || !nyufiyProducts.TryGetValue(productId, out int amount))
        {
            Debug.LogWarning("IAP: Unknown purchased product.");
            storeController.ConfirmPurchase(order);
            return;
        }

        CurrencyManager.Instance?.AddNyufiy(amount, true);
        OnNyufiyPurchaseSucceeded?.Invoke(productId, amount);
        storeController.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        Product product = GetFirstProduct(order);
        Debug.Log($"IAP: Purchase confirmed. Product={product?.definition.id}");
    }

    private void OnPurchaseFailedInternal(FailedOrder order)
    {
        Product product = GetFirstProduct(order);
        string productId = product?.definition.id ?? "";
        string reason = $"{order.FailureReason}: {order.Details}";

        Debug.LogWarning($"IAP: Purchase failed. Product={productId}, Reason={reason}");
        OnPurchaseFailed?.Invoke(productId, reason);
    }

    private Product GetFirstProduct(Order order)
    {
        return order?.CartOrdered.Items().FirstOrDefault()?.Product;
    }
}
