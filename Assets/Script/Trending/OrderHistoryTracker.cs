using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Mencatat histori ingredient yang dipesan pelanggan,
/// diakumulasi per hari selama beberapa hari terakhir (rolling window).
/// Survive pindah scene karena DontDestroyOnLoad.
/// </summary>
public class OrderHistoryTracker : MonoBehaviour
{
    public static OrderHistoryTracker Instance { get; private set; }

    [Tooltip("Berapa hari terakhir yang dihitung untuk trending")]
    public int rollingWindowDays = 3;

    // Key = nomor hari, Value = dictionary ingredient → jumlah pesanan di hari itu
    private Dictionary<int, Dictionary<string, int>> _history = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Catat ingredient yang dipesan di hari ini.
    /// Dipanggil setiap kali order sukses.
    /// </summary>
    public void RecordOrder(List<string> ingredientNames)
    {
        int today = DayManager.Instance.CurrentDay;

        if (!_history.ContainsKey(today))
            _history[today] = new Dictionary<string, int>();

        foreach (string name in ingredientNames)
        {
            if (!_history[today].ContainsKey(name))
                _history[today][name] = 0;
            _history[today][name]++;
        }

        Debug.Log($"[OrderHistory] Day {today} recorded: {string.Join(", ", ingredientNames)}");
    }

    /// <summary>
    /// Hitung skor popularitas setiap ingredient dari N hari terakhir.
    /// Return: Dictionary ingredient name → total pesanan (rolling window).
    /// </summary>
    public Dictionary<string, int> GetPopularityScores()
    {
        int today = DayManager.Instance.CurrentDay;
        int startDay = Mathf.Max(1, today - rollingWindowDays + 1);

        var scores = new Dictionary<string, int>();

        for (int day = startDay; day <= today; day++)
        {
            if (!_history.ContainsKey(day)) continue;

            foreach (var (name, count) in _history[day])
            {
                if (!scores.ContainsKey(name))
                    scores[name] = 0;
                scores[name] += count;
            }
        }

        return scores;
    }

    /// <summary>
    /// Pilih N ingredient teratas berdasarkan skor popularitas.
    /// Ini yang jadi "Trending" untuk besok.
    /// </summary>
    public List<string> GetTopIngredients(int topCount = 2)
    {
        var scores = GetPopularityScores();

        if (scores.Count == 0) return new List<string>();

        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topCount)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Ambil semua ingredient beserta skornya, diurutkan dari terpopuler.
    /// Untuk keperluan tampilan UI malam hari.
    /// </summary>
    public List<(string name, int score)> GetRankedIngredients()
    {
        var scores = GetPopularityScores();

        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }
}