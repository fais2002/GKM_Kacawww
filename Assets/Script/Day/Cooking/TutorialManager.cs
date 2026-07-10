using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Shows tutorial prompts on Day 1 and Day 2.
/// Attach to any GameObject in Day1 and Day2 scenes.
///
/// Tutorial steps are shown one at a time. Each step waits for
/// the player to perform the action before advancing.
///
/// Day 1: teach grabbing tray, dragging ingredients, serving.
/// Day 2: teach cutting, cooking, then serving.
///
/// On days 3+ this script is not present so it does nothing.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject      tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public TextMeshProUGUI stepCounterText;   // "Step 2 / 5"

    [Header("Tutorial steps for this day")]
    [TextArea(2, 4)]
    public string[] tutorialSteps;

    private int  _currentStep = 0;
    private bool _waiting     = false;

    void Start()
    {
        if (!DayManager.Instance.IsTutorialDay)
        {
            gameObject.SetActive(false);
            return;
        }

        if (tutorialSteps.Length == 0) return;
        tutorialPanel?.SetActive(true);
        ShowStep(0);
    }

    // ── Public: called externally when player completes an action ──────────

    /// <summary>
    /// Call this from other scripts when the player completes the current
    /// tutorial action (e.g. TutorialManager.Instance?.StepComplete()).
    /// </summary>
    public void StepComplete()
    {
        if (_waiting) return;
        _currentStep++;
        if (_currentStep >= tutorialSteps.Length)
        {
            tutorialPanel?.SetActive(false);
            Debug.Log("[TutorialManager] Tutorial complete for this day.");
            return;
        }
        ShowStep(_currentStep);
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void ShowStep(int index)
    {
        if (tutorialText    != null) tutorialText.text    = tutorialSteps[index];
        if (stepCounterText != null)
            stepCounterText.text = $"Step {index + 1} / {tutorialSteps.Length}";
        _waiting = false;
    }
}
