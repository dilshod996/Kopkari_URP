using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BonusCoinCard : MonoBehaviour
{
    [SerializeField] private TMP_Text bonusTitle;
    [SerializeField] private TMP_Text bonusAmount;
    [SerializeField] private TMP_Text secondaryBonusAmount;
    [SerializeField] private TMP_Text cost;
    [SerializeField] private Button buyButton;

    public CoinCard.CoinType coinType;
    public PlayerResourse.Resources playerResource;

    [SerializeField] private int bonuseAmountNum;
    [SerializeField] private int secondaryBonusNum;
    [SerializeField] private float costNum;



    private void OnEnable()
    {
        if(LanguageManager.Instance != null)
        {
            bonusTitle.text = LanguageManager.Instance.GetText(404);
        }
        AmountDatas();
    }
    private void OnDisable()
    {
        
    }
    private void AmountDatas()
    {
        bonusAmount.text = bonuseAmountNum > 0 ? $"{bonuseAmountNum:N0}" : "0";
        secondaryBonusAmount.text = secondaryBonusNum > 0 ? $"{secondaryBonusNum:N0}" : "0";
        cost.text = $"${costNum}";
    }
}
