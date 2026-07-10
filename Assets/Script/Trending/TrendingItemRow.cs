using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrendingItemRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image fillBar;       // bar popularitas (Image fill method)
    [SerializeField] private GameObject hotBadge; // object "🔥 HOT" atau "TRENDING"

    public void Setup(string ingredientName, int score, float normalizedScore, bool isTrending)
    {
        if (nameText != null) nameText.text = ingredientName;
        if (scoreText != null) scoreText.text = $"x{score}";
        if (fillBar != null) fillBar.fillAmount = normalizedScore;
        if (hotBadge != null) hotBadge.SetActive(isTrending);
    }
}