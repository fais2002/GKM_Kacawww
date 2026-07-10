using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the 3-flip cooking minigame.
///
/// Canvas hierarchy inside FlipPanel:
///   FlipCountLabel   — TMP: "Flip 1 / 3"
///   PhaseLabel       — TMP: "Cooking..." / "FLIP NOW!" / "Late..." etc.
///   WindowBar (Image) — full-width bar background
///     GreenZone      — Image (left section)
///     YellowZone     — Image (middle)
///     RedZone        — Image (right)
///     ProgressFill   — Image (fillAmount, Raycast=false)
///   FlipButton       — Button + TMP label
///   TimerText        — TMP countdown
///   ResultIcons[]    — 3 Image slots showing G/Y/R per flip
///   FinalResultText  — TMP shown at end
/// </summary>
public class FlipMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject flipPanel;

    [Header("Labels")]
    public TextMeshProUGUI flipCountLabel;
    public TextMeshProUGUI phaseLabel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI finalResultText;

    [Header("Window bar")]
    public Image barBackground;
    public Image greenZone;
    public Image yellowZone;
    public Image redZone;
    public Image progressFill;

    [Header("Flip result icons (size = flip count, e.g. 3)")]
    public List<Image> resultIcons = new();

    [Header("Flip button")]
    public Button flipButton;
    public TextMeshProUGUI flipButtonLabel;

    [Header("Colors")]
    public Color waitColor = new Color(0.5f, 0.5f, 0.5f);
    public Color greenColor = new Color(0.2f, 0.85f, 0.2f);
    public Color yellowColor = new Color(1f, 0.85f, 0f);
    public Color redColor = new Color(0.85f, 0.15f, 0.15f);
    public Color burnColor = new Color(0.2f, 0.1f, 0.1f);
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f);

    // ── Public API ─────────────────────────────────────────────────────────

    public void ShowMinigame(int totalFlips, float wait, float green, float yellow, float red)
    {
        flipPanel?.SetActive(true);
        if (finalResultText != null) finalResultText.text = "";
        if (finalResultText != null) finalResultText.gameObject.SetActive(false);

        // Reset result icons
        foreach (var icon in resultIcons)
            if (icon != null) icon.color = emptyColor;

        SetZoneSizes(green, yellow, red);
        SetFlipButtonInteractable(false);

        flipButton?.onClick.RemoveAllListeners();
        flipButton?.onClick.AddListener(() => FlipMinigame.Instance?.OnFlipPressed());

        if (progressFill != null) progressFill.fillAmount = 0f;
    }

    public void HideMinigame()
    {
        flipPanel?.SetActive(false);
    }

    public void SetFlipCount(int current, int total)
    {
        if (flipCountLabel != null)
            flipCountLabel.text = $"Flip {current} / {total}";
    }

    public void SetPhase(string phase, float windowDuration)
    {
        if (progressFill != null) progressFill.fillAmount = 0f;

        switch (phase)
        {
            case "Waiting":
                SetLabel("Cooking...", waitColor);
                SetFlipButtonInteractable(false);
                if (progressFill != null) progressFill.color = waitColor;
                if (flipButtonLabel != null) flipButtonLabel.text = "Flip!";
                break;
            case "Green":
                SetLabel("FLIP NOW!", greenColor);
                SetFlipButtonInteractable(true);
                if (progressFill != null) progressFill.color = greenColor;
                break;
            case "Yellow":
                SetLabel("Still okay...", yellowColor);
                if (progressFill != null) progressFill.color = yellowColor;
                break;
            case "Red":
                SetLabel("Too late!", redColor);
                if (progressFill != null) progressFill.color = redColor;
                break;
            case "Burned":
                SetLabel("BURNED!", burnColor);
                SetFlipButtonInteractable(false);
                break;
        }
    }

    public void UpdateProgress(float elapsed, float duration)
    {
        float ratio = duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
        if (progressFill != null) progressFill.fillAmount = ratio;
        if (timerText != null)
        {
            float rem = Mathf.Max(0f, duration - elapsed);
            timerText.text = $"{rem:F1}s";
        }
    }

    public void ShowRoundResult(int flipIndex, FlipMinigame.FlipPhase result)
    {
        if (flipIndex < 0 || flipIndex >= resultIcons.Count) return;
        Image icon = resultIcons[flipIndex];
        if (icon == null) return;

        icon.color = result switch
        {
            FlipMinigame.FlipPhase.Green => greenColor,
            FlipMinigame.FlipPhase.Yellow => yellowColor,
            FlipMinigame.FlipPhase.Red => redColor,
            _ => burnColor
        };
    }

    public void ShowBetweenFlipPause()
    {
        SetLabel("Get ready...", waitColor);
        SetFlipButtonInteractable(false);
        if (progressFill != null) progressFill.fillAmount = 0f;
    }

    public void ShowFinalResult(string message, string qualityStr)
    {
        if (finalResultText == null) return;
        finalResultText.gameObject.SetActive(true);
        finalResultText.text = message;
        finalResultText.color = qualityStr switch
        {
            "Perfect" => greenColor,
            "Great" => greenColor,
            "Good" => yellowColor,
            "Poor" => yellowColor,
            _ => burnColor
        };
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void SetLabel(string text, Color color)
    {
        if (phaseLabel != null) { phaseLabel.text = text; phaseLabel.color = color; }
    }

    void SetFlipButtonInteractable(bool v)
    {
        if (flipButton != null) flipButton.interactable = v;
    }

    void SetZoneSizes(float green, float yellow, float red)
    {
        float total = green + yellow + red;
        if (total <= 0f || barBackground == null) return;
        float w = barBackground.rectTransform.rect.width;
        SetZoneWidth(greenZone, green / total * w);
        SetZoneWidth(yellowZone, yellow / total * w);
        SetZoneWidth(redZone, red / total * w);
    }

    void SetZoneWidth(Image zone, float width)
    {
        if (zone == null) return;
        var sd = zone.rectTransform.sizeDelta;
        sd.x = width;
        zone.rectTransform.sizeDelta = sd;
    }
}