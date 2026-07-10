using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Menampilkan prediksi trending ingredient besok di scene malam.
/// </summary>
public class TrendingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject trendingItemPrefab; // prefab satu baris trending
    [SerializeField] private TextMeshProUGUI titleText;

    void OnEnable()
    {
        TrendingManager.OnTrendingRolled += RefreshUI;

        // Kalau sudah ada data (misal panel dibuka setelah roll), langsung refresh
        RefreshUI();
    }

    void OnDisable()
    {
        TrendingManager.OnTrendingRolled -= RefreshUI;
    }

    private void RefreshUI()
{
    if (itemContainer == null)
    {
        Debug.LogError("[TrendingUI] itemContainer belum di-assign!");
        return;
    }

    foreach (Transform child in itemContainer)
        Destroy(child.gameObject);

    if (TrendingManager.Instance == null)
    {
        Debug.LogError("[TrendingUI] TrendingManager.Instance null!");
        return;
    }

    if (OrderHistoryTracker.Instance == null)
    {
        Debug.LogError("[TrendingUI] OrderHistoryTracker.Instance null!");
        return;
    }

    var ranked = OrderHistoryTracker.Instance.GetRankedIngredients();
    var pendingTrending = TrendingManager.Instance.PendingTrending;

    if (ranked.Count == 0)
    {
        if (titleText != null)
            titleText.text = "Belum ada data pesanan hari ini.";
        return;
    }

    if (titleText != null)
        titleText.text = "📈 Prediksi Trending Besok";

    if (trendingItemPrefab == null)
    {
        Debug.LogError("[TrendingUI] trendingItemPrefab belum di-assign!");
        return;
    }

    int maxScore = ranked[0].score;

    foreach (var (name, score) in ranked)
    {
        GameObject item = Instantiate(trendingItemPrefab, itemContainer);
        var row = item.GetComponent<TrendingItemRow>();

        if (row == null)
        {
            Debug.LogError("[TrendingUI] trendingItemPrefab tidak punya komponen TrendingItemRow!");
            continue;
        }

        bool isTrending = pendingTrending.Contains(name);
        float normalizedScore = maxScore > 0 ? (float)score / maxScore : 0f;

        row.Setup(name, score, normalizedScore, isTrending);
    }
}
}