using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Tracks restaurant popularity.
/// V4 adds OnSpecialNPCServed for the large popularity reward.
/// </summary>
public class PopularityManager : MonoBehaviour
{
    public static PopularityManager Instance { get; private set; }

    [Header("Settings")]
    public float gainPerSuccess = 10f;
    public float lossPerWrong = 5f;
    public float maxPopularity = 100f;

    [Header("UI")]
    public Image popularityBar;
    public TextMeshProUGUI popularityLabel;
    public TextMeshProUGUI customerCountText;

    public float CurrentPopularity { get; private set; } = 0f;
    public int TotalCustomers { get; private set; } = 0;
    public int SuccessfulServes { get; private set; } = 0;
    public int WrongServes { get; private set; } = 0;

    private static readonly string[] RANKS =
        { "Unknown", "Curious", "Regular", "Popular", "Beloved", "Legendary" };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => RefreshUI();

    public void OnCustomerArrived() { TotalCustomers++; RefreshUI(); }

    public void GainPopularity(float amount)
    {
        CurrentPopularity = Mathf.Min(CurrentPopularity + amount, maxPopularity);
        RefreshUI();
    }

    public void LosePopularity(float amount)
    {
        CurrentPopularity = Mathf.Max(CurrentPopularity - amount, 0f);
        RefreshUI();
    }

    public void OnCustomerServedSuccess()
    {
        SuccessfulServes++;
        GainPopularity(gainPerSuccess);
    }

    public void OnWrongOrder()
    {
        WrongServes++;
        LosePopularity(lossPerWrong);
        Debug.Log($"[PopularityManager] Wrong order. Popularity: {CurrentPopularity}");
    }

    /// <summary>Called when the special NPC's order is correctly served.</summary>
    public void OnSpecialNPCServed(float reward)
    {
        SuccessfulServes++;
        GainPopularity(reward);
        Debug.Log($"[PopularityManager] Special NPC served! +{reward} popularity.");
    }

    public string GetFinalRank() => GetRank();

    void RefreshUI()
    {
        float ratio = maxPopularity > 0 ? CurrentPopularity / maxPopularity : 0f;
        if (popularityBar != null) popularityBar.fillAmount = ratio;
        if (customerCountText != null) customerCountText.text = $"Customers: {TotalCustomers}";
        if (popularityLabel != null) popularityLabel.text = GetRank();
    }

    string GetRank()
    {
        float ratio = maxPopularity > 0 ? CurrentPopularity / maxPopularity : 0f;
        int index = Mathf.Clamp(Mathf.FloorToInt(ratio * (RANKS.Length - 1)), 0, RANKS.Length - 1);
        return RANKS[index];
    }
}