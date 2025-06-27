using UnityEngine;

[CreateAssetMenu(fileName = "New Product", menuName = "Store/Product Data")]
public class ProductData : ScriptableObject
{
    public CategoryType category;
    public int nameId; // LanguageManager orqali o'qiladi
    public int cost;
    public Sprite productImage;
}