using UnityEngine;
using System.Collections.Generic;

public class ShopCategoryRow : MonoBehaviour
{
    [Header("Category")]
    public IngredientCategory category;

    [Header("References")]
    [SerializeField] private Transform iconContainer;
    [SerializeField] private GameObject shopItemIconPrefab;

    private List<ShopItemIcon> _icons = new List<ShopItemIcon>();

    public void Populate(List<Ingredient> ingredients)
    {
        foreach (Transform t in iconContainer)
            Destroy(t.gameObject);
        _icons.Clear();

        foreach (var ingredient in ingredients)
        {
            GameObject obj = Instantiate(shopItemIconPrefab, iconContainer);
            ShopItemIcon icon = obj.GetComponent<ShopItemIcon>();
            icon.Setup(ingredient);
            _icons.Add(icon);
        }
    }

    // Dipanggil UIShopCatalogue untuk ambil icon pertama
    public ShopItemIcon GetFirstIcon()
    {
        return _icons.Count > 0 ? _icons[0] : null;
    }
}