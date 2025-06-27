using UnityEngine;

[CreateAssetMenu(fileName = "New premium", menuName = "Store/Premium Data")]
public class PremiumData : ScriptableObject
{
    public PremiumCategoryType category; // PremiumCategoryType enum
    public Sprite icon;
    public int descriptionId; // LanguageManager orqali o'qiladi
    public int infoId;
}
