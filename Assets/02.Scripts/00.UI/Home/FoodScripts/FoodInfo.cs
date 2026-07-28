using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodInfo : MonoBehaviour
{
    [Header("UI Texts")]
    [SerializeField] private TMP_Text nameFood;
    [SerializeField] private Image imageofFood;

    [Header("UI Settings")]
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button infoBtn;
    [SerializeField] private TMP_Text foodCostText;
    [SerializeField] private ResourceInfoDetailsPopup detailsPopup;

    [SerializeField] private int foodCost;
    [SerializeField] private Color itemColor = Color.white;

    public static event Action<float, float, float> OnFoodAddToHorse;
    public static event Action OnMoneyNotEnough;
    public static int LastFailedFoodCost { get; private set; }
    private static event Action OnFoodButtonsRefreshRequested;

    public enum HorseFood
    {
        None,
        Wheat,
        Barley,
        Apple,
        Water,
        StaminWater,
        Hay,
        Oats,
        Carrot
    }

    [SerializeField] private HorseFood food;

    public readonly struct FoodDetails
    {
        public readonly HorseFood FoodType;
        public readonly string Name;
        public readonly Sprite Icon;
        public readonly int Cost;
        public readonly float Power;
        public readonly float Cooling;
        public readonly float Stamina;

        public FoodDetails(
            HorseFood foodType,
            string name,
            Sprite icon,
            int cost,
            float power,
            float cooling,
            float stamina)
        {
            FoodType = foodType;
            Name = name;
            Icon = icon;
            Cost = cost;
            Power = power;
            Cooling = cooling;
            Stamina = stamina;
        }
    }

    private void OnEnable()
    {
        TextTransilations();
        RefreshButtonState();
        OnFoodButtonsRefreshRequested += RefreshButtonState;
        buyBtn?.onClick.AddListener(OpenFoodDetails);
        infoBtn?.onClick.AddListener(OpenFoodDetails);
    }

    private void OnDisable()
    {
        OnFoodButtonsRefreshRequested -= RefreshButtonState;
        buyBtn?.onClick.RemoveListener(OpenFoodDetails);
        infoBtn?.onClick.RemoveListener(OpenFoodDetails);
    }

    private void TextTransilations()
    {
        if (nameFood != null)
            nameFood.text = GetFoodName();

        if (foodCostText != null)
            foodCostText.text = foodCost > 0 ? $"{foodCost:N0}" : "0";
    }

    private void OpenFoodDetails()
    {
        FoodDetails details = BuildFoodDetails();
        bool canImprove = CanImproveHorse(details);
        ResourceInfoDetailsPopup popup = detailsPopup != null ? detailsPopup : ResourceInfoDetailsPopup.Instance;

        if (popup == null)
        {
            if (canImprove)
                TryBuyFood(details, null);
            else
                Debug.Log("Your horse is full and you are ready to race.");

            return;
        }

        popup.Show(BuildPopupDetails(details), canImprove, () => TryBuyFood(details, popup));
        SoundManager.Instance?.PlayUI(UISoundType.PopupOpen);
    }

    private void TryBuyFood(FoodDetails details, ResourceInfoDetailsPopup popup)
    {
        if (!CanImproveHorse(details))
        {
            popup?.Show(BuildPopupDetails(details), false, () => { });
            return;
        }

        bool success = details.Cost <= 0 ||
                       (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendNyufiy(details.Cost, true));

        if (!success)
        {
            if (popup != null)
                popup.ShowNotEnoughNyufiy();
            else
            {
                LastFailedFoodCost = details.Cost;
                OnMoneyNotEnough?.Invoke();
            }

            HomeHapticsManager.Instance?.Play(HomeHapticId.NotEnoughMoney);
            SoundManager.Instance?.PlayUI(UISoundType.Error);
            return;
        }

        AddSupplies(details.Power, details.Cooling, details.Stamina);
        HomeMainUI.Instance?.ShowRightPopup(GetSuccessMessage(details), details.Icon);
        popup?.Close();
        OnFoodButtonsRefreshRequested?.Invoke();
        HomeHapticsManager.Instance?.Play(HomeHapticId.Success);
        SoundManager.Instance?.PlayUI(UISoundType.Success);
    }

    private ResourceInfoDetailsPopup.ResourceDetails BuildPopupDetails(FoodDetails details)
    {
        return ResourceInfoDetailsPopup.ResourceDetails.Horse(
            details.Name,
            details.Icon,
            details.Cost,
            itemColor,
            FormatBuff(details.Stamina),
            FormatBuff(details.Cooling),
            FormatBuff(details.Power));
    }

    private string FormatBuff(float amount)
    {
        return amount > 0f ? $"+{amount:0.#}%" : "+0%";
    }

    private FoodDetails BuildFoodDetails()
    {
        GetFoodBuffs(food, out float power, out float cooling, out float stamina);

        return new FoodDetails(
            food,
            GetFoodName(),
            imageofFood != null ? imageofFood.sprite : null,
            foodCost,
            power,
            cooling,
            stamina);
    }

    private string GetFoodName()
    {
        int languageId = GetFoodNameLanguageId(food);
        if (languageId != -1)
            return GetLocalizedText(languageId, food.ToString());

        return food.ToString();
    }

    private string GetSuccessMessage(FoodDetails details)
    {
        int langId = -1;

        switch (details.FoodType)
        {
            case HorseFood.Water:
                langId = 204;
                break;
            case HorseFood.Apple:
                langId = 205;
                break;
            case HorseFood.Wheat:
                langId = 206;
                break;
            case HorseFood.Barley:
                langId = 207;
                break;
            case HorseFood.StaminWater:
                langId = 208;
                break;
        }

        if (langId == -1)
            langId = GetFoodNameLanguageId(details.FoodType);

        return langId != -1
            ? GetLocalizedText(langId, $"{details.Name} added")
            : $"{details.Name} added";
    }

    private void RefreshButtonState()
    {
        if (buyBtn != null)
            buyBtn.interactable = true;

        if (infoBtn != null)
            infoBtn.interactable = true;
    }

    private bool CanImproveHorse(FoodDetails details)
    {
        KopkariResultsManager results = KopkariResultsManager.Instance;
        bool useLiveCondition = results != null && results.IsLiveHorseConditionActive;
        HorseConditionStats max = useLiveCondition
            ? results.GetLiveHorseConditionMax()
            : HorseConditionStatsService.GetCachedMaxOrDefault();
        HorseConditionStats current = useLiveCondition
            ? results.GetLiveHorseCondition()
            : HorseConditionStatsService.GetCurrentOrInitialize(max);

        return CanIncrease(details.Power, current.Power, max.Power) ||
               CanIncrease(details.Cooling, current.Cooling, max.Cooling) ||
               CanIncrease(details.Stamina, current.Stamina, max.Stamina);
    }

    private void AddSupplies(float powerAddAmount, float coolingAddAmount, float staminaAddAmount)
    {
        OnFoodAddToHorse?.Invoke(powerAddAmount, coolingAddAmount, staminaAddAmount);
    }

    private static bool CanIncrease(float amount, float current, float max)
    {
        return amount > 0f && current < max;
    }

    private static void GetFoodBuffs(HorseFood foodType, out float power, out float cooling, out float stamina)
    {
        power = 0f;
        cooling = 0f;
        stamina = 0f;

        switch (foodType)
        {
            case HorseFood.Water:
                cooling = 7f;
                break;
            case HorseFood.Apple:
                power = 4f;
                stamina = 4f;
                break;
            case HorseFood.Wheat:
                power = 6f;
                stamina = 8f;
                break;
            case HorseFood.Barley:
                power = 7f;
                stamina = 10f;
                break;
            case HorseFood.StaminWater:
                cooling = 6f;
                stamina = 13f;
                break;
            case HorseFood.Hay:
                cooling = 2f;
                stamina = 6f;
                break;
            case HorseFood.Oats:
                power = 5f;
                stamina = 7f;
                break;
            case HorseFood.Carrot:
                power = 3f;
                cooling = 3f;
                stamina = 3f;
                break;
        }
    }

    private static int GetFoodNameLanguageId(HorseFood foodType)
    {
        switch (foodType)
        {
            case HorseFood.Wheat:
                return 108;
            case HorseFood.Barley:
                return 109;
            case HorseFood.Apple:
                return 110;
            case HorseFood.Water:
                return 111;
            case HorseFood.StaminWater:
                return 112;
            case HorseFood.Hay:
            case HorseFood.Oats:
            case HorseFood.Carrot:
            default:
                return -1;
        }
    }

    private string GetLocalizedText(int id, string fallback)
    {
        if (id == -1 || LanguageManager.Instance == null)
            return fallback;

        string localized = LanguageManager.Instance.GetText(id);
        return string.IsNullOrEmpty(localized) ? fallback : localized;
    }
}
