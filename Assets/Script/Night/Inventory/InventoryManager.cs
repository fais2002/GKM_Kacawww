using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Database")]
    [SerializeField] private InventoryDatabased database;

    private List<Inventory> _slots = new List<Inventory>();

    public IReadOnlyList<Inventory> Slots => _slots;
    public InventoryDatabased Database => database;

    public event Action OnInventoryChanged;

    public bool IsUnlocked { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    public bool AddItem(Ingredient ingredient, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return false;
        }

        Inventory slot = _slots.Find(s => s.ingredient == ingredient);

        if (slot != null)
        {
            slot.quantity += quantity;
        }
        else
        {
            _slots.Add(new Inventory(ingredient, quantity));
        }
        UpdateLockState();
        OnInventoryChanged?.Invoke();
        Debug.Log($"[+] {ingredient.ingredientName} x{quantity}");
        return true;
    }

    public bool RemoveItem(Ingredient ingredient, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return false;
        }
        
        Inventory slot = _slots.Find(s => s.ingredient == ingredient);

        if (slot == null || slot.quantity < quantity)
        {
            Debug.Log($"Stok {ingredient.ingredientName} tidak cukup!");
            return false;
        }

        slot.quantity -= quantity;

        if (slot.quantity <= 0)
        {
            _slots.Remove(slot);
        }

        UpdateLockState();
        OnInventoryChanged?.Invoke();
        Debug.Log($"[-] {ingredient.ingredientName} x{quantity}");
        return true;
    }

    public int GetQuantity(Ingredient ingredient)
    {
        Inventory slot = _slots.Find(s => s.ingredient == ingredient);
        return slot != null ? slot.quantity : 0;
    }

    public bool HasEnough(Ingredient ingredient, int quantity)
    {
        return GetQuantity(ingredient) >= quantity;
    }

    public bool HasItem() => _slots.Count > 0;

    public List<(Ingredient ingredient, int quantity)> GetAllItems()
    {
        var result = new List<(Ingredient, int)>();
        foreach (var slot in _slots)
        {
            if (slot.ingredient != null && slot.quantity > 0)
                result.Add((slot.ingredient, slot.quantity));
        }
        return result;
    }
    
    public void SetSlots(List<Inventory> slots)
    {
        _slots = slots;
        UpdateLockState();
        OnInventoryChanged?.Invoke();
    }

    public void ConsumeAllItem()
    {
        _slots.Clear();
        UpdateLockState();
        OnInventoryChanged?.Invoke();
    }

    // ── Lock System ──────────────────────────────────────────────────────

    void UpdateLockState()
    {
        IsUnlocked = HasItem();
        Debug.Log($"[InventoryManager] IsUnlocked: {IsUnlocked}");
    }

    public void TryEndNight()
    {
        if (!IsUnlocked)
        {
            Debug.Log("[InventoryManager] Belum beli item — tidak bisa lanjut.");
            return;
        }

        Debug.Log("[InventoryManager] Item cukup — lanjut ke hari berikutnya.");
        PhaseManager.Instance.EndNightPhase();
    }
}
