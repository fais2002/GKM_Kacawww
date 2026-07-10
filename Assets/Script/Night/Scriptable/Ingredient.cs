using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "New List/Ingredient")]
public class Ingredient : ScriptableObject
{
    [Header("Ingredient Info")]
    public int ItemID;
    public string ingredientName;
    public Sprite ingredientSprite;
    public string unit;
    public int stock;

    [Header("Ingredient Category")]
    public IngredientCategory category;

    [Header("Ingredient Cost")]
    public int cost;
}

public enum IngredientCategory
{
    Sayuran,
    Bumbu,
    Daging,
    Minuman,
    Lainnya
}
