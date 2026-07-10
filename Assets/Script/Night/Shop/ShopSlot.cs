using UnityEngine;
using System;

[Serializable]
public class ShopSlot
{
    public Ingredient ingredient;
    public int quantity; // quantity yang ingin dibeli

    public ShopSlot(Ingredient ingredient)
    {
        this.ingredient = ingredient;
        this.quantity = 1;
    }
}
