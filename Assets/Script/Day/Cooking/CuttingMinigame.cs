using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the cutting minigame flow.
///
/// Flow:
///   1. CuttingStation calls StartMinigame() when ingredient is placed.
///   2. Camera moves to CuttingOverhead angle.
///   3. CuttingMinigameUI shows the overlay with random pattern + knife.
///   4. Player drags knife across each dotted line (zone-based detection).
///   5. When all lines are traced, ingredient is marked Cut.
///   6. Camera returns to default Cook angle.
///
/// Attach to a persistent manager GameObject alongside CuttingMinigameUI.
/// </summary>
public class CuttingMinigame : MonoBehaviour
{
    public static CuttingMinigame Instance { get; private set; }

    [Header("Slice patterns — assign 3 SlicePatternData assets")]
    public List<SlicePatternData> patterns = new();

    [Header("Camera angle")]
    [Tooltip("Name of the CameraController angle to switch to during cutting.")]
    public string cuttingCameraAngle = "CuttingOverhead";
    [Tooltip("Name of the angle to return to after cutting.")]
    public string defaultCameraAngle = "Cook";

    [Header("UI reference")]
    public CuttingMinigameUI minigameUI;

    // ── State ──────────────────────────────────────────────────────────────
    public bool IsActive { get; private set; }
    public int LinesTotal { get; private set; }
    public int LinesCompleted { get; private set; }

    private IngredientItem _ingredient;
    private CuttingStation _station;
    private SlicePatternData _activePattern;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Called by CuttingStation when ingredient is placed on the board.</summary>
    public void StartMinigame(IngredientItem ingredient, CuttingStation station)
    {
        if (IsActive) return;
        if (patterns.Count == 0)
        {
            Debug.LogWarning("[CuttingMinigame] No patterns assigned — cutting instantly.");
            ingredient.ApplyCut();
            station.OnMinigameComplete();
            return;
        }

        _ingredient = ingredient;
        _station = station;
        IsActive = true;
        LinesCompleted = 0;

        // Pick random pattern
        _activePattern = patterns[Random.Range(0, patterns.Count)];
        LinesTotal = _activePattern.lines.Count;

        Debug.Log($"[CuttingMinigame] Starting with pattern '{_activePattern.patternName}' ({LinesTotal} lines).");

        // Switch camera
        CameraController.Instance?.GoTo(cuttingCameraAngle);

        // Show UI
        minigameUI?.ShowMinigame(_activePattern);
    }

    /// <summary>Called by CuttingMinigameUI when a slice line is successfully traced.</summary>
    public void OnLineSliced(int lineIndex)
    {
        if (!IsActive) return;

        LinesCompleted++;
        Debug.Log($"[CuttingMinigame] Line {lineIndex} sliced. {LinesCompleted}/{LinesTotal} done.");

        minigameUI?.MarkLineComplete(lineIndex);

        if (LinesCompleted >= LinesTotal)
            StartCoroutine(FinishCutting());
    }

    // ── Internal ───────────────────────────────────────────────────────────

    IEnumerator FinishCutting()
    {
        minigameUI?.ShowCompletionEffect();
        yield return new WaitForSeconds(1f);

        // Apply cut state to ingredient
        if (_ingredient != null)
        {
            if (_ingredient.transform.parent != null)
                _ingredient.transform.SetParent(null, true);
            _ingredient.transform.position += Vector3.up * 0.05f;
            _ingredient.locationState = IngredientItem.LocationState.OnStation;
            _ingredient.ApplyCut();
        }

        _station?.OnMinigameComplete();

        yield return new WaitForSeconds(0.3f);

        minigameUI?.HideMinigame();
        CameraController.Instance?.GoTo(defaultCameraAngle);

        IsActive = false;
        _ingredient = null;
        _station = null;

        Debug.Log("[CuttingMinigame] Cutting complete.");
    }
}