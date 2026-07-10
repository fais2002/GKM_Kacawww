using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Versi UI untuk TrendingManagerRandom — tanpa angka skor/histori.
/// Hanya menampilkan daftar nama ingredient yang jadi trending besok.
/// </summary>
public class TrendingUIRandom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject trendingItemPrefab; // prefab satu baris (cukup nama + badge HOT)
    [SerializeField] private TextMeshProUGUI titleText;

    void OnEnable()
    {
        TrendingManagerRandom.OnTrendingRolled += RefreshUI;
        RefreshUI();
    }

    void OnDisable()
    {
        TrendingManagerRandom.OnTrendingRolled -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (itemContainer == null)
        {
            Debug.LogError("[TrendingUIRandom] itemContainer belum di-assign!");
            return;
        }

        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        if (TrendingManagerRandom.Instance == null)
        {
            Debug.LogError("[TrendingUIRandom] TrendingManagerRandom.Instance null!");
            return;
        }

        var pendingTrending = TrendingManagerRandom.Instance.PendingTrending;

        if (pendingTrending.Count == 0)
        {
            if (titleText != null)
                titleText.text = "Belum ada prediksi trending.";
            return;
        }

        if (titleText != null)
            titleText.text = "🎲 Prediksi Trending Besok";

        if (trendingItemPrefab == null)
        {
            Debug.LogError("[TrendingUIRandom] trendingItemPrefab belum di-assign!");
            return;
        }

        foreach (string name in pendingTrending)
        {
            GameObject item = Instantiate(trendingItemPrefab, itemContainer);
            var row = item.GetComponent<TrendingItemRowRandom>();

            if (row == null)
            {
                Debug.LogError("[TrendingUIRandom] trendingItemPrefab tidak punya TrendingItemRowRandom!");
                continue;
            }

            row.Setup(name);
        }
    }
}
