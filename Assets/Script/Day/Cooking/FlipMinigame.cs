using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flip timing cooking minigame — 3 flips required.
///
/// Each of the 3 flip rounds:
///   [waitTime]          Ingredient cooks. "Wait" phase.
///   [greenWindowTime]   GREEN  — best flip timing.
///   [yellowWindowTime]  YELLOW — acceptable.
///   [redWindowTime]     RED    — late.
///   (missed all)        BURNED — round fails immediately.
///
/// If ANY round is Burned, the whole ingredient is burned and destroyed.
/// Otherwise the worst result of the 3 rounds determines final CookQuality:
///   3 Green  → Perfect
///   2G+1Y    → Great
///   1G+2Y    → Good
///   3 Yellow → Poor
///   Any Red  → Failed
///
/// Rewards (money + popularity) are NOT applied here.
/// They are applied when the customer pays after being served.
/// CookQuality is stored on the ingredient for use at serve time.
/// </summary>
public class FlipMinigame : MonoBehaviour
{
    public static FlipMinigame Instance { get; private set; }

    [Header("Timing (seconds) — per flip round")]
    public float waitTime = 4f;
    public float greenWindowTime = 2f;
    public float yellowWindowTime = 2f;
    public float redWindowTime = 2f;

    [Header("Number of flips required")]
    public int flipCount = 3;

    [Header("Camera")]
    public string cookingCameraAngle = "PanOverhead";
    public string defaultCameraAngle = "Cook";

    [Header("UI")]
    public FlipMinigameUI minigameUI;

    // ── State ──────────────────────────────────────────────────────────────
    public bool IsActive { get; private set; }

    private IngredientItem _ingredient;
    private CookingStation _station;
    private float _elapsed;
    private FlipPhase _phase;
    private bool _flippedThisRound;
    private int _currentFlip;
    private List<FlipPhase> _flipResults = new();

    public enum FlipPhase { Waiting, Green, Yellow, Red, Burned }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void StartMinigame(IngredientItem ingredient, CookingStation station)
    {
        if (IsActive) return;

        _ingredient = ingredient;
        _station = station;
        _currentFlip = 0;
        _flipResults.Clear();
        IsActive = true;

        CameraController.Instance?.GoTo(cookingCameraAngle);
        minigameUI?.ShowMinigame(flipCount, waitTime, greenWindowTime, yellowWindowTime, redWindowTime);
        StartCoroutine(RunAllFlips());
    }

    /// <summary>Called by the Flip button.</summary>
    public void OnFlipPressed()
    {
        if (!IsActive || _flippedThisRound) return;
        if (_phase == FlipPhase.Waiting || _phase == FlipPhase.Burned) return;
        _flippedThisRound = true;
    }

    // ── Flip loop ──────────────────────────────────────────────────────────

    IEnumerator RunAllFlips()
    {
        for (int flip = 0; flip < flipCount; flip++)
        {
            _currentFlip = flip + 1;
            _flippedThisRound = false;
            _lastRoundResult = FlipPhase.Green;   // default reset
            minigameUI?.SetFlipCount(_currentFlip, flipCount);

            // Run the round — result is stored in _lastRoundResult
            yield return StartCoroutine(RunOneFlipRound());
            FlipPhase result = _lastRoundResult;

            _flipResults.Add(result);
            minigameUI?.ShowRoundResult(_currentFlip - 1, result);

            if (result == FlipPhase.Burned)
            {
                yield return StartCoroutine(FinishMinigame(true));
                yield break;
            }

            // Brief pause between flips
            if (flip < flipCount - 1)
            {
                minigameUI?.ShowBetweenFlipPause();
                yield return new WaitForSeconds(1f);
            }
        }

        yield return StartCoroutine(FinishMinigame(false));
    }

    /// <summary>
    /// Runs one flip round. Returns via a local variable since Unity coroutines
    /// can't return values — use a wrapper with a result holder.
    /// </summary>
    private FlipPhase _lastRoundResult;

    IEnumerator RunOneFlipRound()
    {
        // Wait phase
        _phase = FlipPhase.Waiting;
        _elapsed = 0f;
        minigameUI?.SetPhase("Waiting", waitTime);
        while (_elapsed < waitTime)
        {
            _elapsed += Time.deltaTime;
            minigameUI?.UpdateProgress(_elapsed, waitTime);
            yield return null;
        }

        // Green window
        _phase = FlipPhase.Green;
        _elapsed = 0f;
        minigameUI?.SetPhase("Green", greenWindowTime);
        while (_elapsed < greenWindowTime && !_flippedThisRound)
        {
            _elapsed += Time.deltaTime;
            minigameUI?.UpdateProgress(_elapsed, greenWindowTime);
            yield return null;
        }
        if (_flippedThisRound) { _lastRoundResult = FlipPhase.Green; yield break; }

        // Yellow window
        _phase = FlipPhase.Yellow;
        _elapsed = 0f;
        minigameUI?.SetPhase("Yellow", yellowWindowTime);
        while (_elapsed < yellowWindowTime && !_flippedThisRound)
        {
            _elapsed += Time.deltaTime;
            minigameUI?.UpdateProgress(_elapsed, yellowWindowTime);
            yield return null;
        }
        if (_flippedThisRound) { _lastRoundResult = FlipPhase.Yellow; yield break; }

        // Red window
        _phase = FlipPhase.Red;
        _elapsed = 0f;
        minigameUI?.SetPhase("Red", redWindowTime);
        while (_elapsed < redWindowTime && !_flippedThisRound)
        {
            _elapsed += Time.deltaTime;
            minigameUI?.UpdateProgress(_elapsed, redWindowTime);
            yield return null;
        }
        if (_flippedThisRound) { _lastRoundResult = FlipPhase.Red; yield break; }

        // Burned
        _phase = FlipPhase.Burned;
        _lastRoundResult = FlipPhase.Burned;
    }

    // ── Finish ─────────────────────────────────────────────────────────────

    IEnumerator FinishMinigame(bool burned)
    {
        CookingMinigame.CookQuality quality;

        if (burned)
        {
            quality = CookingMinigame.CookQuality.Failed;
            minigameUI?.ShowFinalResult("BURNED! Food wasted.", "Burned");
        }
        else
        {
            quality = CalculateQuality();
            string label = quality switch
            {
                CookingMinigame.CookQuality.Perfect => "Perfect Cook!",
                CookingMinigame.CookQuality.Great => "Great Cook!",
                CookingMinigame.CookQuality.Good => "Good Cook",
                CookingMinigame.CookQuality.Poor => "Poor Cook",
                _ => "Failed"
            };
            minigameUI?.ShowFinalResult(label, quality.ToString());
        }

        yield return new WaitForSeconds(2f);

        if (burned)
        {
            if (_ingredient != null)
            {
                _ingredient.transform.SetParent(null);
                StationManager.Instance?.RemoveIngredient(_ingredient.gameObject);
                Destroy(_ingredient.gameObject);
            }
        }
        else if (_ingredient != null)
        {
            if (_ingredient.transform.parent != null)
                _ingredient.transform.SetParent(null, true);
            _ingredient.transform.position += Vector3.up * 0.05f;
            _ingredient.locationState = IngredientItem.LocationState.OnStation;
            _ingredient.cookQuality = quality;   // stored — used at serve time
            _ingredient.ApplyCook();
        }

        _station?.OnMinigameComplete();

        yield return new WaitForSeconds(0.5f);
        minigameUI?.HideMinigame();
        CameraController.Instance?.GoTo(defaultCameraAngle);

        IsActive = false;
        _ingredient = null;
        _station = null;
    }

    CookingMinigame.CookQuality CalculateQuality()
    {
        int greens = 0, yellows = 0, reds = 0;
        foreach (FlipPhase r in _flipResults)
        {
            if (r == FlipPhase.Green) greens++;
            else if (r == FlipPhase.Yellow) yellows++;
            else reds++;
        }

        if (reds > 0) return CookingMinigame.CookQuality.Failed;
        if (greens == flipCount) return CookingMinigame.CookQuality.Perfect;
        if (greens >= 2) return CookingMinigame.CookQuality.Great;
        if (greens == 1 && yellows >= 2) return CookingMinigame.CookQuality.Good;
        return CookingMinigame.CookQuality.Poor;
    }
}