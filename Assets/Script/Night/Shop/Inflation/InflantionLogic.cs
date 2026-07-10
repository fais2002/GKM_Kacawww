using UnityEngine;

[System.Serializable]
public class InflantionLogic
{
    public Ingredient ingredient;

    public float price;
    public int stock;
    public int soldCount;

    public void initialize()
    {
        price = ingredient.cost;
        stock = ingredient.stock;
        soldCount = 0;
    }

    public void InflantionPrice()
    {
        float demand = 1f + (soldCount * 0.02f);

        float stockFactor = stock > 0
            ? Mathf.Clamp(10f / stock , 1f, 3f)
            : 3f;

        float totalPrice = ingredient.cost * demand * stockFactor;

        price = Mathf.Clamp(totalPrice, ingredient.cost * 0.5f, ingredient.cost * 5f);
        price = Mathf.Round(price);
    }

    public bool IsAvailable() => stock > 0;
}
