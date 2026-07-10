using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Generates orders and shows them in the UI.
/// Order is only revealed in the UI AFTER the player clicks the customer's
/// question mark symbol (CustomerAI calls ShowOrderFromCustomer).
/// </summary>
public class CustomerOrder : MonoBehaviour
{
    public static CustomerOrder Instance { get; private set; }

    [Header("Order size")]
    [Range(1, 5)] public int minOrderSize = 1;
    [Range(1, 5)] public int maxOrderSize = 3;

    [Header("UI")]
    public TextMeshProUGUI orderDisplay;
    public string satisfiedMessage = "Delicious! Thank you! ★";
    public string wrongMessage = "That's not what I ordered...";

    private List<string> _ordered = new();
    public bool HasOrder => _ordered.Count > 0;
    public IReadOnlyList<string> OrderedItems => _ordered;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Called by CustomerAI during decide phase ────────────────────────────

    /// <summary>
    /// Generates and stores an order for a specific customer.
    /// Does NOT show it in the UI yet — UI only shows after player accepts.
    /// Returns the list of ingredient names.
    /// </summary>

    public List<string> GenerateOrderForCustomer()
    {
        List<string> uniqueNames = StationManager.Instance.GetUniqueIngredientNames();
        if (uniqueNames.Count == 0) return new List<string>();

        // Bangun weighted pool berdasarkan popularitas histori + bonus trending aktif
        var scores = OrderHistoryTracker.Instance.GetPopularityScores();
        var pool = new List<string>();

        foreach (string name in uniqueNames)
        {
            int baseWeight = scores.ContainsKey(name) ? scores[name] : 1; // minimal 1
            float finalWeight = TrendingManager.Instance.GetWeight(name, baseWeight);
            int repeatCount = Mathf.Max(1, Mathf.RoundToInt(finalWeight));

            for (int i = 0; i < repeatCount; i++)
                pool.Add(name);
        }

        // Pick dari weighted pool tanpa duplikat
        int size = Mathf.Min(Random.Range(minOrderSize, maxOrderSize + 1), uniqueNames.Count);
        var order = new List<string>();
        var picked = new HashSet<string>();
        int safetyLimit = pool.Count * 2;
        int attempts = 0;

        while (picked.Count < size && attempts < safetyLimit)
        {
            string candidate = pool[Random.Range(0, pool.Count)];
            if (picked.Add(candidate))
                order.Add(candidate);
            attempts++;
        }

        Debug.Log($"[CustomerOrder] Generated: {string.Join(", ", order)}");
        return order;
    }

    //public List<string> GenerateOrderForCustomer()
    //{
    //List<string> available = StationManager.Instance.GetUniqueIngredientNames();
    //if (available.Count == 0) return new List<string>();

    // Shuffle
    //for (int i = available.Count - 1; i > 0; i--)
    //{
    //int j = Random.Range(0, i + 1);
    //(available[i], available[j]) = (available[j], available[i]);
    //}

    //var order = new List<string>();
    //int size = Mathf.Min(Random.Range(minOrderSize, maxOrderSize + 1), available.Count);
    //for (int i = 0; i < size; i++)
    //order.Add(available[i]);

    //Debug.Log($"[CustomerOrder] Generated: {string.Join(", ", order)}");
    //return order;
    //}

    // ── Called by CustomerAI when player clicks question mark ───────────────

    /// <summary>
    /// Takes the order from the customer and shows it in the UI.
    /// This is the moment the player "accepts" the order.
    /// </summary>
    public void ShowOrderFromCustomer(CustomerAI customer)
    {
        _ordered = new List<string>(customer.Order);
        RefreshUI();
        Debug.Log($"[CustomerOrder] Order accepted and shown: {string.Join(", ", _ordered)}");
    }

    // ── Order evaluation ───────────────────────────────────────────────────

    public bool CheckOrder(FoodTray tray)
    {
        if (!HasOrder) return false;

        var trayNames = new List<string>();
        foreach (var item in tray.GetAllItems())
            trayNames.Add(item.ingredientName);

        bool success = true;
        foreach (string req in _ordered)
        {
            if (!trayNames.Contains(req))
            {
                success = false;
                Debug.Log($"[CustomerOrder] Missing: {req}");
                break;
            }
        }

        if (success)
            OrderHistoryTracker.Instance.RecordOrder(_ordered);

        ShowResult(success);
        return success;
    }

    public void ClearOrder()
    {
        _ordered.Clear();
        if (orderDisplay != null) orderDisplay.text = "";
    }

    // ── UI ─────────────────────────────────────────────────────────────────

    void RefreshUI()
    {
        if (orderDisplay == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Customer Order:");
        foreach (string name in _ordered)
            sb.AppendLine($"  • {name}");
        orderDisplay.text = sb.ToString();
    }

    void ShowResult(bool ok)
    {
        if (orderDisplay != null)
            orderDisplay.text = ok ? satisfiedMessage : wrongMessage;
    }
}