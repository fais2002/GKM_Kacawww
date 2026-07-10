using UnityEngine;
using TMPro;

/// <summary>
/// Versi sederhana TrendingItemRow — hanya nama ingredient + badge HOT.
/// Tidak ada score/fillBar karena tidak ada data histori.
/// </summary>
public class TrendingItemRowRandom : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject hotBadge; // object "🔥 HOT" / "TRENDING"

    public void Setup(string ingredientName)
    {
        if (nameText != null) nameText.text = ingredientName;
        if (hotBadge != null) hotBadge.SetActive(true); // semua yang masuk list ini pasti trending
    }
}
