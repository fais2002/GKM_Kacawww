using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "InventoryDatabased", menuName = "New List/InventoryDatabased")]
public class InventoryDatabased : ScriptableObject
{
    public List<Ingredient> ingredients = new List<Ingredient>();

    public Ingredient ById(int ID)
    {
        return ingredients.Find(i => i.ItemID == ID);
    }

    public Ingredient ByName(string nama)
    {
        return ingredients.Find(i => i.ingredientName == nama);
    }
}
