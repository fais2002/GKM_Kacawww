using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A serving tray with 5 slots.
/// Destroyed after the player serves it to a customer.
/// </summary>
public class FoodTray : MonoBehaviour
{
    public const int SLOT_COUNT = 5;

    [Header("Slots (auto-found if empty)")]
    public List<TraySlot> slots = new();

    public bool IsHeld { get; private set; }
    public int ItemCount => CountItems();

    void Awake()
    {
        if (slots.Count == 0)
            slots.AddRange(GetComponentsInChildren<TraySlot>());

        if (slots.Count != SLOT_COUNT)
            Debug.LogWarning($"[FoodTray] Expected {SLOT_COUNT} slots, found {slots.Count}.");
    }

    public void PickUp()
    {
        IsHeld = true;
        gameObject.SetActive(true);
        Debug.Log("[FoodTray] Tray picked up.");
    }

    /// <summary>Adds an item to the first available slot. Returns the slot or null if full.</summary>
    public TraySlot AddItem(IngredientItem item)
    {
        foreach (TraySlot slot in slots)
        {
            if (!slot.IsOccupied)
            {
                slot.PlaceItem(item);
                Debug.Log($"[FoodTray] Added {item.ingredientName}. {ItemCount}/{SLOT_COUNT}");
                return slot;
            }
        }
        Debug.Log("[FoodTray] Tray is full!");
        return null;
    }

    public List<IngredientItem> GetAllItems()
    {
        var list = new List<IngredientItem>();
        foreach (TraySlot slot in slots)
            if (slot.IsOccupied) list.Add(slot.OccupiedItem);
        return list;
    }

    /// <summary>
    /// Serves the tray: calculates quality-adjusted payment total,
    /// stores it on GameManager for the customer to collect later,
    /// removes items from station, destroys them, then destroys the tray.
    /// Money is NOT earned here — it is earned when the player clicks
    /// the customer's money icon.
    /// </summary>
    public void ServeAndDestroy()
    {
        float totalPayment = 0f;
        // Track worst quality among all items served
        CookingMinigame.CookQuality worstQuality = CookingMinigame.CookQuality.Perfect;

        foreach (TraySlot slot in slots)
        {
            if (!slot.IsOccupied) continue;
            IngredientItem item = slot.OccupiedItem;

            float qualityMultiplier = item.cookQuality switch
            {
                CookingMinigame.CookQuality.Perfect => 2.0f,
                CookingMinigame.CookQuality.Great => 1.5f,
                CookingMinigame.CookQuality.Good => 1.0f,
                CookingMinigame.CookQuality.Poor => 0.5f,
                CookingMinigame.CookQuality.Failed => 0f,
                _ => 1.0f
            };

            totalPayment += item.sellPrice * qualityMultiplier;

            // Worst quality determines the reward tier
            if ((int)item.cookQuality > (int)worstQuality)
                worstQuality = item.cookQuality;

            StationManager.Instance?.RemoveIngredient(item.gameObject);
            item.transform.SetParent(null);
            slot.RemoveItem();
            Destroy(item.gameObject);
        }

        GameManager.Instance.StorePendingPayment(totalPayment, worstQuality);
        GameManager.Instance.OnTrayServed();
        Destroy(gameObject);
    }

    int CountItems()
    {
        int n = 0;
        foreach (TraySlot slot in slots)
            if (slot.IsOccupied) n++;
        return n;
    }
}