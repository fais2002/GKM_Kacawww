using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private InventoryDatabased database;

    public IReadOnlyList<Ingredient> shopItem => database.ingredients;

    public event Action OnPurchaseSuccess;
    public event Action OnPurchaseFailed;
    public event Action OnCartChanged;

    [Header("Condition")]
    [SerializeField] private bool useInflantion = false;
    //[SerializeField] private bool useDiscount = false;

    private List<ShopSlot> _cart = new List<ShopSlot>();
    public IReadOnlyList<ShopSlot> Cart => _cart;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsInCart(Ingredient ingredient)
    {
        return _cart.Exists(s => s.ingredient == ingredient);
    }

    // Toggle: belum ada → tambah, sudah ada → remove
    public void ToggleCart(Ingredient ingredient)
    {
        if (IsInCart(ingredient))
            RemoveFromCart(ingredient);
        else
            AddToCart(ingredient);
    }

    public void AddToCart(Ingredient ingredient)
    {
        if (IsInCart(ingredient)) return; // sudah ada, skip

        _cart.Add(new ShopSlot(ingredient));
        OnCartChanged?.Invoke();
        Debug.Log($"[Cart] Tambah: {ingredient.ingredientName}");
    }

    public void RemoveFromCart(Ingredient ingredient)
    {
        int removed = _cart.RemoveAll(s => s.ingredient == ingredient);
        if (removed > 0)
        {
            OnCartChanged?.Invoke();
            Debug.Log($"[Cart] Hapus: {ingredient.ingredientName}");
        }
    }

    public void ClearCart()
    {
        _cart.Clear();
        OnCartChanged?.Invoke();
    }

    // ── Buy ───────────────────────────────────────────

    public bool BuyCart()
    {
        if (_cart.Count == 0)
        {
            Debug.Log("[Cart] Cart kosong");
            OnPurchaseFailed?.Invoke();
            return false;
        }

        // Validasi uang dulu sebelum proses apapun
        float totalCost = GetCartTotalCost();
        if (MoneySystem.Instance.CurrentMoney < totalCost)
        {
            Debug.Log($"[Cart] Uang tidak cukup. Butuh: Rp {totalCost:N0}");
            OnPurchaseFailed?.Invoke();
            return false;
        }

        // Proses pembelian semua item
        foreach (var slot in _cart)
        {
            float cost = GetCurrentPrice(slot.ingredient) * slot.quantity;
            MoneySystem.Instance.SpendMoney(cost, $"Beli {slot.ingredient.ingredientName} x{slot.quantity}");
            InventoryManager.Instance.AddItem(slot.ingredient, slot.quantity);

            if (useInflantion && InflationSystem.Instance != null)
                InflationSystem.Instance.BuyItem(slot.ingredient.name, slot.quantity);

            Debug.Log($"[Cart] Beli {slot.ingredient.ingredientName} x{slot.quantity} | Rp {cost:N0}");
        }

        _cart.Clear();
        OnPurchaseSuccess?.Invoke();
        OnCartChanged?.Invoke();
        return true;
    }

    public void CancelCart()
    {
        ClearCart();
        Debug.Log("[Cart] Dibatalkan");
    }

    public float GetCartTotalCost()
    {
        float total = 0;
        foreach (var slot in _cart)
            total += GetCurrentPrice(slot.ingredient) * slot.quantity;
        return total;
    }

    // Beli item dari supplier
    public bool BuyFromSupplier(Ingredient ingredient, int quantity = 1)
    {
        // cek item di inventory
        if (InventoryManager.Instance.GetQuantity(ingredient) > 0)
        {
            Debug.Log("Item sudah ada di inventory");
            OnPurchaseFailed?.Invoke();
            return false;
        }

        // ambil harga sesuai kondisi useInflation
        float pricePerUnit = useInflantion && InflationSystem.Instance != null
            ? InflationSystem.Instance.CurrentPrices(ingredient.name)
            : ingredient.cost; // fallback ke harga SO kalau tidak pakai inflasi

        int totalCost = Mathf.RoundToInt(pricePerUnit * quantity);

        // cek & kurangi uang
        bool paid = MoneySystem.Instance.SpendMoney(totalCost,
            $"Beli {ingredient.ingredientName} x{quantity}");

        if (!paid)
        {
            OnPurchaseFailed?.Invoke();
            return false;
        }

        // tambah ke inventory
        InventoryManager.Instance.AddItem(ingredient, quantity);

        // update inflasi — harga naik karena dibeli
        if (useInflantion && InflationSystem.Instance != null)
        {
            InflationSystem.Instance.BuyItem(ingredient.name, quantity);
        }

        OnPurchaseSuccess?.Invoke();
        Debug.Log($"Beli {ingredient.ingredientName} x{quantity} | Rp {totalCost:N0}");
        return true;
    }

    // Ambil harga saat ini dari InflationSystem
    public float GetCurrentPrice(Ingredient ingredient)
    {
        // Nanti kondisi ini bisa diganti dengan DayManager.Instance.CurrentDay
        // sekarang pakai bool manual dulu sebagai placeholder
        bool useDiscount = DiscountManager.Instance != null && DiscountManager.Instance.IsDiscountActive;

        if (useInflantion && useDiscount)
        {
            // inflasi + diskon sekaligus
            float inflatedPrice = GetBasePrice(ingredient);
            return GetDiscountedPrice(inflatedPrice);
        }
        else if (useInflantion)
        {
            // hanya inflasi
            return GetBasePrice(ingredient);
        }
        else if (useDiscount)
        {
            // hanya diskon dari harga base SO
            return GetDiscountedPrice(ingredient.cost);
        }
        else
        {
            // harga normal
            return ingredient.cost;
        }
    }

    // Harga setelah inflasi, sebelum diskon
    public float GetBasePrice(Ingredient ingredient)
    {
        if (useInflantion && InflationSystem.Instance != null)
            return InflationSystem.Instance.CurrentPrices(ingredient.ingredientName);

        return ingredient.cost;
    }

    // Terapkan diskon ke harga manapun
    public float GetDiscountedPrice(float price)
    {
        if (DiscountManager.Instance != null && DiscountManager.Instance.IsDiscountActive)
            return DiscountManager.Instance.ApplyDiscount(price);

        return price;
    }
}
