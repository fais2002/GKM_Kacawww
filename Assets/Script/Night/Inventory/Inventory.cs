using UnityEngine;

[System.Serializable]
public class Inventory
{
    public Ingredient ingredient;
    public int quantity; 

    public Inventory(Ingredient ingredient, int quantity)
    {
        this.ingredient = ingredient;
        this.quantity = quantity;
    }
}
