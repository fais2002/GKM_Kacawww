using UnityEngine;
using System.Collections.Generic;

public class InflationSystem : MonoBehaviour
{
    private Dictionary<string, InflantionLogic> Items = new();

    public static InflationSystem Instance { get; private set; }
    public System.Action UpdatePriceDisplay;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ShopManager.Instance == null)
        {
            return;
        }
        InitializeMarket();
    }

    void InitializeMarket()
    {
        foreach (var ingredientItem in ShopManager.Instance.shopItem)
        {
            var entry = new InflantionLogic { ingredient = ingredientItem };
            entry.initialize();
            Items[ingredientItem.name] = entry;
        }
    }

    public void BuyItem(string itemName, int quantity = 1)
    {
        if(!Items.TryGetValue(itemName, out var entry)) return;

        entry.soldCount += quantity;
        entry.InflantionPrice();

        UpdatePriceDisplay?.Invoke();
    }

    public InflantionLogic GetEntry(string itemName)
    {
        Items.TryGetValue(itemName, out var entry);
        return entry;
    }

    public float CurrentPrices(string itemName)
    {
        return Items.TryGetValue(itemName, out var entry) ? entry.price : 0f;
    }

    public IEnumerable<InflantionLogic> GetAllEntries() => Items.Values;
}
