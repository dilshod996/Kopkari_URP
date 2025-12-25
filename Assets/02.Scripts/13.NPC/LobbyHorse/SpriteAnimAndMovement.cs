using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimAndMovement : MonoBehaviour
{
    public Sprite[] sprites;   // Sprite'lar arrayi
    public float framesPerSecond = 10f; // Har sekundda nechta frame
    public float moveSpeed = 1f; // Harakat tezligi
    public float startPositionX = -55f; // Boshlang'ich X pozitsiyasi
    public float endPositionX = 1985f; // Yakuniy X pozitsiyasi

    private Image imageComponent; // UI Image komponenti
    private RectTransform rectTransform; // UI RectTransform
    private int currentFrame = 0; // Joriy frame
    private float currentPositionX; // Joriy X pozitsiyasi
    private bool isMoving = true; // Harakatning davom etishi uchun flag

    void Start()
    {
        imageComponent = GetComponent<Image>(); // Image komponentini olish
        rectTransform = GetComponent<RectTransform>(); // RectTransform komponentini olish
        currentPositionX = startPositionX; // Boshlang'ich pozitsiya
        rectTransform.anchoredPosition = new Vector2(currentPositionX, rectTransform.anchoredPosition.y); // RectTransform'ni boshlang'ich pozitsiyaga qo'yish
        InvokeRepeating("ChangeSprite", 0f, 1f / framesPerSecond); // Sprite'ni o'zgartirish
    }

    void Update()
    {
        if (isMoving)
        {
            MoveImage(); // Harakatni amalga oshirish
        }
    }

    void ChangeSprite()
    {
        if (sprites.Length == 0)
            return;

        currentFrame = (currentFrame + 1) % sprites.Length; // Frame'ni yangilash
        imageComponent.sprite = sprites[currentFrame]; // UI Image'ga yangi sprite o'rnatish
    }

    void MoveImage()
    {
        // Harakat qilish
        currentPositionX += moveSpeed * Time.deltaTime; // X pozitsiyasini yangilash

        // Agar image yakuniy pozitsiyaga yetib borsa, harakatni to'xtatish
        if (currentPositionX >= endPositionX)
        {
            currentPositionX = endPositionX; // Yakuniy pozitsiyada to'xtatish
            isMoving = false; // Harakatni to'xtatish
        }

        rectTransform.anchoredPosition = new Vector2(currentPositionX, rectTransform.anchoredPosition.y); // RectTransform'ni yangi pozitsiyaga qo'yish
    }
}
