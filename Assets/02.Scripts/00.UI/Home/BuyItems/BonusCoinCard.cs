using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BonusCoinCard : MonoBehaviour
{
    public enum BonusCardProduct
    {
        None,
        bonus_card_coin_15_websnare_10,
        bonus_card_7900_nyufiy_10_defeder
    }

    [SerializeField] private TMP_Text bonusTitle;
    [SerializeField] private TMP_Text bonusAmount;
    [SerializeField] private TMP_Text secondaryBonusAmount;
    [SerializeField] private TMP_Text cost;
    [SerializeField] private Button buyButton;
    [SerializeField] private BonusCardProduct bonusCardProduct = BonusCardProduct.None;

    public CoinCard.CoinType coinType;
    public PlayerResourse.Resources playerResource;

    [SerializeField] private int bonuseAmountNum;
    [SerializeField] private int secondaryBonusNum;
    [SerializeField] private float costNum;

    private bool purchasePending;

    private void OnEnable()
    {
        purchasePending = false;

        if (LanguageManager.Instance != null && bonusTitle != null)
        {
            bonusTitle.text = LanguageManager.Instance.GetText(404);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyBonusCard);
            buyButton.onClick.AddListener(BuyBonusCard);
        }

        SubscribeToIapEvents();
        AmountDatas();
    }

    private void OnDisable()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyBonusCard);

        UnsubscribeFromIapEvents();
    }

    private void AmountDatas()
    {
        int primaryAmount = bonuseAmountNum;
        int itemAmount = secondaryBonusNum;

        if (IapPurchaseManager.Instance != null &&
            IapPurchaseManager.Instance.TryGetBonusCardReward(
                bonusCardProduct,
                out IapPurchaseManager.BonusCardReward reward))
        {
            primaryAmount = reward.CoinAmount > 0
                ? reward.CoinAmount
                : reward.NyufiyAmount;
            itemAmount = reward.ItemAmount;
        }

        if (bonusAmount != null)
            bonusAmount.text = primaryAmount > 0 ? $"{primaryAmount:N0}" : "0";
        if (secondaryBonusAmount != null)
            secondaryBonusAmount.text = itemAmount > 0 ? $"{itemAmount:N0}" : "0";

        UpdateStoreState();
    }

    private void BuyBonusCard()
    {
        if (bonusCardProduct == BonusCardProduct.None)
        {
            Debug.LogWarning("Bonus card product is not selected in the Inspector.", this);
            return;
        }

        if (IapPurchaseManager.Instance == null)
        {
            Debug.LogWarning("IAP manager is missing from the scene.", this);
            return;
        }

        purchasePending = true;
        UpdateStoreState();
        IapPurchaseManager.Instance.BuyBonusCard(bonusCardProduct);
    }

    private void SubscribeToIapEvents()
    {
        if (IapPurchaseManager.Instance == null)
            return;

        IapPurchaseManager.Instance.OnProductsUpdated += UpdateStoreState;
        IapPurchaseManager.Instance.OnBonusCardPurchaseSucceeded += HandlePurchaseSucceeded;
        IapPurchaseManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
    }

    private void UnsubscribeFromIapEvents()
    {
        if (IapPurchaseManager.Instance == null)
            return;

        IapPurchaseManager.Instance.OnProductsUpdated -= UpdateStoreState;
        IapPurchaseManager.Instance.OnBonusCardPurchaseSucceeded -= HandlePurchaseSucceeded;
        IapPurchaseManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
    }

    private void UpdateStoreState()
    {
        string productId = GetProductId();
        string localizedPrice = string.Empty;
        bool productAvailable =
            IapPurchaseManager.Instance != null &&
            !string.IsNullOrEmpty(productId) &&
            IapPurchaseManager.Instance.TryGetLocalizedPrice(productId, out localizedPrice);

        if (cost != null)
            cost.text = productAvailable ? localizedPrice : "...";

        if (buyButton != null)
            buyButton.interactable = productAvailable && !purchasePending;
    }

    private void HandlePurchaseSucceeded(
        string productId,
        IapPurchaseManager.BonusCardReward reward)
    {
        if (productId != GetProductId())
            return;

        purchasePending = false;
        UpdateStoreState();

        if (HomeMainUI.Instance != null &&
            DataManager.Instance != null &&
            !string.IsNullOrEmpty(reward.ItemKey))
        {
            HomeMainUI.Instance.UpdatePlayerResources(
                reward.ItemKey,
                DataManager.Instance.GetItemAmount(reward.ItemKey));
        }

        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        Debug.Log(
            $"Bonus card granted. Product={productId}, Coin={reward.CoinAmount}, " +
            $"Nyufiy={reward.NyufiyAmount}, Item={reward.ItemKey}, " +
            $"ItemAmount={reward.ItemAmount}",
            this);
    }

    private void HandlePurchaseFailed(string productId, string reason)
    {
        if (!purchasePending)
            return;

        if (!string.IsNullOrEmpty(productId) && productId != GetProductId())
            return;

        purchasePending = false;
        UpdateStoreState();
        HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
        Debug.LogWarning($"Bonus card purchase failed: {reason}", this);
    }

    private string GetProductId()
    {
        return IapPurchaseManager.Instance != null
            ? IapPurchaseManager.Instance.GetBonusCardProductId(bonusCardProduct)
            : bonusCardProduct == BonusCardProduct.None
                ? string.Empty
                : bonusCardProduct.ToString();
    }
}
