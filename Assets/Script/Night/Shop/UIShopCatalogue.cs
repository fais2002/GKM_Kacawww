using UnityEngine;
using System.Collections.Generic;

public class UIShopCatalogue : MonoBehaviour
{
    private bool _isGenerated = false;
    private ShopItemIcon _firstIcon; // referensi item pertama untuk focus

    public void Init()
    {
        if (_isGenerated) return;
        GenerateByCategory();
        _isGenerated = true;
    }

    void GenerateByCategory()
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError("[Catalogue] ShopManager.Instance null!");
            return;
        }

        var grouped = new Dictionary<IngredientCategory, List<Ingredient>>();

        foreach (var ingredient in ShopManager.Instance.shopItem)
        {
            if (ingredient == null) continue;

            if (!grouped.ContainsKey(ingredient.category))
                grouped[ingredient.category] = new List<Ingredient>();

            grouped[ingredient.category].Add(ingredient);
        }

        var categoryRows = GetComponentsInChildren<ShopCategoryRow>();
        bool isFirst = true;

        foreach (var row in categoryRows)
        {
            if (row == null) continue;

            if (grouped.TryGetValue(row.category, out List<Ingredient> ingredients))
            {
                row.Populate(ingredients);

                // simpan icon pertama dari row pertama yang ada isinya
                if (isFirst)
                {
                    _firstIcon = row.GetFirstIcon();
                    isFirst = false;
                }
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
    }

    // Dipanggil UIShop.Open() untuk set focus awal
    public GameObject GetFirstItem()
    {
        if (_firstIcon == null) return null;
        return _firstIcon.gameObject;
    }
}