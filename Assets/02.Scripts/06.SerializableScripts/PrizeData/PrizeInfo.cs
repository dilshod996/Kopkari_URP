using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrizeInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text prizeNameText;
    [SerializeField] private Image prizeImage;
    [SerializeField] private TMP_Text rewardAmountText;

    public void SetPrize(Prize prize)
    {
        prizeNameText.text = LanguageManager.Instance.GetText(prize.prizeTextId);
        prizeImage.sprite = prize.prizeSprite;
        rewardAmountText.text = prize.rewardAmount.ToString();
    }
}
