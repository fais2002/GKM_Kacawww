using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menyimpan ingredient trending yang berlaku untuk sesi jualan besok.
/// Di-roll saat Night phase, baru dipakai saat Morning phase keesokan harinya.
/// </summary>
public class TrendingManager : MonoBehaviour
{
    public static TrendingManager Instance { get; private set; }

    [Tooltip("Berapa ingredient yang jadi trending per sesi")]
    [Range(1, 3)] public int trendingCount = 2;

    [Tooltip("Pengali bobot untuk ingredient trending. 3 = 3x lebih sering muncul.")]
    public float trendingWeightMultiplier = 3f;

    // Trending yang sudah dikunci untuk sesi besok
    private List<string> _pendingTrending = new();

    // Trending yang sedang aktif dipakai saat ini (sesi hari ini)
    private List<string> _activeTrending = new();

    public IReadOnlyList<string> ActiveTrending => _activeTrending;
    public IReadOnlyList<string> PendingTrending => _pendingTrending;

    public static event System.Action OnTrendingRolled; // untuk notify UI malam

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (PhaseManager.Instance != null)
            PhaseManager.Instance.OnPhaseStart.AddListener(OnPhaseStart);
    }

    void OnDisable()
    {
        if (PhaseManager.Instance != null)
            PhaseManager.Instance.OnPhaseStart.RemoveListener(OnPhaseStart);
    }

    void OnPhaseStart(int phase, string name)
    {
        if (phase == 3) // Night phase dimulai → roll trending untuk besok
            RollTrendingForTomorrow();

        if (phase == 1) // Morning phase dimulai → aktifkan pending trending
            ActivatePendingTrending();
    }

    // ── Roll & Activate ──────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil saat Night phase mulai.
    /// Hitung trending berdasarkan histori, simpan sebagai pending.
    /// </summary>
    public void RollTrendingForTomorrow()
    {
        _pendingTrending.Clear();

        var topIngredients = OrderHistoryTracker.Instance.GetTopIngredients(trendingCount);

        if (topIngredients.Count == 0)
        {
            // Hari pertama/belum ada histori → random dari station
            var available = StationManager.Instance.GetUniqueIngredientNames();
            var shuffled = new List<string>(available);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            int count = Mathf.Min(trendingCount, shuffled.Count);
            for (int i = 0; i < count; i++)
                _pendingTrending.Add(shuffled[i]);
        }
        else
        {
            _pendingTrending = topIngredients;
        }

        Debug.Log($"[TrendingManager] Pending trending for tomorrow: {string.Join(", ", _pendingTrending)}");
        OnTrendingRolled?.Invoke(); // notify UI malam untuk update tampilan
    }

    /// <summary>
    /// Dipanggil saat Morning phase mulai di hari baru.
    /// Pending trending dari malam sebelumnya jadi aktif.
    /// </summary>
    private void ActivatePendingTrending()
    {
        _activeTrending = new List<string>(_pendingTrending);
        Debug.Log($"[TrendingManager] Active trending today: {string.Join(", ", _activeTrending)}");
    }

    // ── Query untuk CustomerOrder ─────────────────────────────────────────────

    public bool IsTrending(string ingredientName) => _activeTrending.Contains(ingredientName);

    public float GetWeight(string ingredientName, int baseWeight)
    {
        float weight = baseWeight;
        if (IsTrending(ingredientName))
            weight *= trendingWeightMultiplier;
        return weight;
    }
}