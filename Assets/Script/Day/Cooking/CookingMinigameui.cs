using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all visual elements of the cooking minigame.
///
/// Setup in Canvas:
///   Create a MinigamePanel (inactive by default) containing:
///
///   [Bar]
///     RedBar       — full-width background Image (red color)
///     YellowBar    — child Image (yellow, centered on green zone)
///     GreenBar     — child Image (green, smallest, inside yellow)
///     MovingLine   — child Image (thin white vertical line, moves L/R)
///
///   [Food Quality]
///     QualityBarBg   — background Image
///     QualityBarFill — fill Image (Image Type = Filled, Fill Method = Horizontal)
///     QualityLabel   — TMP text ("Food Quality")
///
///   [Attempts]
///     Attempt1Result — Image (shows hit result icon after each attempt)
///     Attempt2Result — Image
///     Attempt3Result — Image
///
///   [Labels / Buttons]
///     CookStopButton  — Button with TMP label ("Cook" / "Stop" / "Wait...")
///     CooldownText    — TMP text (countdown)
///     FinalResultText — TMP text (shows quality after all 3 attempts)
///     AttemptCounter  — TMP text ("Attempt 1 / 3")
/// </summary>
public class CookingMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject minigamePanel;

    [Header("Bar")]
    public RectTransform redBar;         // parent bar — full width reference
    public RectTransform yellowBar;      // positioned over green zone area
    public RectTransform greenBar;       // positioned in center of yellow
    public RectTransform movingLine;     // thin line that moves left/right

    [Header("Food Quality bar")]
    public Image qualityBarFill;
    public TextMeshProUGUI qualityLabel;

    [Header("Attempt result icons")]
    [Tooltip("One Image per attempt slot (3 total). Color changes on result.")]
    public Image[] attemptResultIcons;   // size 3

    [Header("Colors")]
    public Color greenColor = new Color(0.2f, 0.85f, 0.2f);
    public Color yellowColor = new Color(1f, 0.85f, 0f);
    public Color redColor = new Color(0.85f, 0.15f, 0.15f);
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Controls")]
    public Button cookStopButton;
    public TextMeshProUGUI cookStopLabel;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI finalResultText;
    public TextMeshProUGUI attemptCounterText;

    // ── Setup / teardown ───────────────────────────────────────────────────

    public void ShowMinigame(float greenWidth, float yellowWidth)
    {
        minigamePanel?.SetActive(true);
        if (finalResultText != null) finalResultText.text = "";
        if (cooldownText != null) cooldownText.text = "";
        if (qualityBarFill != null) qualityBarFill.fillAmount = 0f;
        if (qualityLabel != null) qualityLabel.text = "Food Quality";
        SetButtonLabel("Cook");

        // Reset attempt icons
        if (attemptResultIcons != null)
            foreach (var icon in attemptResultIcons)
                if (icon != null) icon.color = emptyColor;

        if (attemptCounterText != null)
            attemptCounterText.text = "Attempt 1 / 3";

        // Wire button
        cookStopButton?.onClick.RemoveAllListeners();
        cookStopButton?.onClick.AddListener(() => CookingMinigame.Instance?.OnCookButtonPressed());
    }

    public void HideMinigame()
    {
        minigamePanel?.SetActive(false);
    }

    // ── Zone positions ─────────────────────────────────────────────────────

    /// <summary>
    /// Repositions the yellow and green bars based on the green zone center.
    /// All values are 0–1 normalized against the red bar's total width.
    /// </summary>
    public void UpdateZones(float greenCenter, float greenWidth, float yellowWidth)
    {
        if (redBar == null) return;

        float totalWidth = redBar.rect.width;

        // Yellow bar
        if (yellowBar != null)
        {
            float yW = yellowWidth * totalWidth;
            float yX = (greenCenter - yellowWidth / 2f) * totalWidth;
            yellowBar.sizeDelta = new Vector2(yW, yellowBar.sizeDelta.y);
            yellowBar.anchoredPosition = new Vector2(yX, yellowBar.anchoredPosition.y);
        }

        // Green bar
        if (greenBar != null)
        {
            float gW = greenWidth * totalWidth;
            float gX = (greenCenter - greenWidth / 2f) * totalWidth;
            greenBar.sizeDelta = new Vector2(gW, greenBar.sizeDelta.y);
            greenBar.anchoredPosition = new Vector2(gX, greenBar.anchoredPosition.y);
        }
    }

    // ── Moving line ────────────────────────────────────────────────────────

    /// <summary>Moves the line to the given normalized position (0 = left, 1 = right).</summary>
    public void UpdateLine(float normalizedPos)
    {
        if (movingLine == null || redBar == null) return;
        float x = normalizedPos * redBar.rect.width;
        movingLine.anchoredPosition = new Vector2(x, movingLine.anchoredPosition.y);
    }

    // ── Attempt results ────────────────────────────────────────────────────

    public void ShowAttemptResult(CookingMinigame.HitResult result, int attemptIndex)
    {
        // Color the attempt icon
        if (attemptResultIcons != null && attemptIndex < attemptResultIcons.Length)
        {
            var icon = attemptResultIcons[attemptIndex];
            if (icon != null)
            {
                icon.color = result == CookingMinigame.HitResult.Green ? greenColor
                           : result == CookingMinigame.HitResult.Yellow ? yellowColor
                           : redColor;
            }
        }

        // Update food quality bar based on actual points scored so far
        if (qualityBarFill != null && CookingMinigame.Instance != null)
        {
            float maxPossible = CookingMinigame.Instance.greenPoints
                              * CookingMinigame.Instance.totalAttempts;

            // Points per result
            float pointsThisHit = result == CookingMinigame.HitResult.Green
                ? CookingMinigame.Instance.greenPoints
                : result == CookingMinigame.HitResult.Yellow
                    ? CookingMinigame.Instance.yellowPoints
                    : CookingMinigame.Instance.redPoints;

            // Accumulate fill (add fractional contribution of this hit)
            float contribution = maxPossible > 0 ? pointsThisHit / maxPossible : 0f;
            qualityBarFill.fillAmount = Mathf.Clamp01(qualityBarFill.fillAmount + contribution);
        }

        // Update attempt counter
        if (attemptCounterText != null)
        {
            int next = attemptIndex + 2;
            int total = CookingMinigame.Instance?.totalAttempts ?? 3;
            attemptCounterText.text = next <= total
                ? $"Attempt {next} / {total}"
                : "Final attempt done!";
        }
    }

    // ── Cooldown ───────────────────────────────────────────────────────────

    public void ShowCooldown(float duration)
    {
        StartCoroutine(CountdownRoutine(duration));
    }

    System.Collections.IEnumerator CountdownRoutine(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (cooldownText != null)
                cooldownText.text = $"Next in {remaining:F1}s";
            remaining -= Time.deltaTime;
            yield return null;
        }
        if (cooldownText != null) cooldownText.text = "";
    }

    // ── Final result ───────────────────────────────────────────────────────

    public void ShowFinalResult(CookingMinigame.CookQuality quality, float totalPoints)
    {
        if (finalResultText == null) return;

        string label = quality switch
        {
            CookingMinigame.CookQuality.Perfect => "★ PERFECT COOK! ★",
            CookingMinigame.CookQuality.Great => "Great Cook!",
            CookingMinigame.CookQuality.Good => "Good Cook",
            CookingMinigame.CookQuality.Poor => "Poor Cook",
            CookingMinigame.CookQuality.Failed => "Cook Failed",
            _ => ""
        };

        finalResultText.text = $"{label}\n{totalPoints:F0} pts";
        finalResultText.color = quality switch
        {
            CookingMinigame.CookQuality.Perfect => greenColor,
            CookingMinigame.CookQuality.Great => greenColor,
            CookingMinigame.CookQuality.Good => yellowColor,
            CookingMinigame.CookQuality.Poor => yellowColor,
            _ => redColor
        };

        if (qualityLabel != null) qualityLabel.text = label;
    }

    // ── Button label ───────────────────────────────────────────────────────

    public void SetButtonLabel(string label)
    {
        if (cookStopLabel != null) cookStopLabel.text = label;
    }
}