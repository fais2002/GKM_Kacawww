using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the press-timing cooking minigame.
///
/// Flow:
///   1. Player drags ingredient onto the stove → CookingStation calls StartMinigame().
///   2. Camera moves to overhead pan angle.
///   3. Cook button appears. Player presses it to start the moving line.
///   4. Player presses Stop to land the line. Result scored (Green/Yellow/Red).
///   5. 2-second cooldown. Green bar position randomises. Repeat 3 times.
///   6. Final quality calculated from 3 results → ingredient marked cooked
///      with quality data attached.
///
/// Attach to a persistent manager GameObject. Wire UI in Inspector.
/// </summary>
public class CookingMinigame : MonoBehaviour
{
    public static CookingMinigame Instance { get; private set; }

    // ── Result types ───────────────────────────────────────────────────────

    public enum HitResult { Green, Yellow, Red }

    public enum CookQuality
    {
        Perfect,   // 3 Green
        Great,     // 2 Green + 1 Yellow
        Good,      // 1 Green + 2 Yellow
        Poor,      // 3 Yellow
        Failed     // any Red
    }

    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Minigame settings")]
    [Tooltip("How fast the line moves across the bar (0–1 space per second).")]
    public float lineSpeed = 0.6f;

    [Tooltip("Seconds to wait between each of the 3 attempts.")]
    public float cooldownBetweenAttempts = 2f;

    [Tooltip("How many attempts per cook session.")]
    public int totalAttempts = 3;

    [Header("Zone widths (0–1 of total bar width)")]
    [Tooltip("Width of the green zone (smallest, hardest to hit).")]
    public float greenZoneWidth = 0.08f;
    [Tooltip("Width of the yellow zone surrounding the green zone.")]
    public float yellowZoneWidth = 0.20f;
    // Red zone is everything outside yellow.

    [Header("Quality points per hit")]
    public float greenPoints = 100f;
    public float yellowPoints = 50f;
    public float redPoints = 0f;

    [Header("Camera angles")]
    [Tooltip("Name of the CameraController angle to switch to during the minigame.")]
    public string cookingCameraAngle = "PanOverhead";
    [Tooltip("Name of the angle to return to after the minigame finishes.")]
    public string defaultCameraAngle = "Cook";

    [Header("UI reference")]
    public CookingMinigameUI minigameUI;

    // ── Events ─────────────────────────────────────────────────────────────
    public UnityEvent<CookQuality> OnCookComplete = new();

    // ── State ──────────────────────────────────────────────────────────────
    public bool IsActive { get; private set; } = false;

    private IngredientItem _ingredient;
    private int _attemptsDone;
    private float _totalPoints;
    private HitResult[] _results;
    private bool _lineMoving;
    private float _linePosition;   // 0..1
    private int _lineDirection;  // +1 or -1
    private float _greenCenter;    // 0..1 position of green zone center
    private bool _awaitingCooldown;
    private CookingStation _callingStation;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsActive || !_lineMoving) return;

        // Move line back and forth
        _linePosition += _lineDirection * lineSpeed * Time.deltaTime;

        if (_linePosition >= 1f) { _linePosition = 1f; _lineDirection = -1; }
        if (_linePosition <= 0f) { _linePosition = 0f; _lineDirection = 1; }

        minigameUI?.UpdateLine(_linePosition);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Called by CookingStation when an ingredient is dropped onto the stove.</summary>
    public void StartMinigame(IngredientItem ingredient, CookingStation station)
    {
        if (IsActive) return;

        _ingredient = ingredient;
        _callingStation = station;
        _attemptsDone = 0;
        _totalPoints = 0f;
        _results = new HitResult[totalAttempts];
        _lineMoving = false;
        _linePosition = 0.5f;
        _lineDirection = 1;
        IsActive = true;

        // Move camera to overhead pan view
        CameraController.Instance?.GoTo(cookingCameraAngle);

        // Show UI
        minigameUI?.ShowMinigame(greenZoneWidth, yellowZoneWidth);
        RandomiseGreenPosition();

        Debug.Log("[CookingMinigame] Minigame started.");
    }

    /// <summary>Called when player presses the Cook/Stop button.</summary>
    public void OnCookButtonPressed()
    {
        if (!IsActive || _awaitingCooldown) return;

        if (!_lineMoving)
        {
            // Start moving the line
            _lineMoving = true;
            _linePosition = 0f;
            _lineDirection = 1;
            minigameUI?.SetButtonLabel("Stop");
            Debug.Log($"[CookingMinigame] Attempt {_attemptsDone + 1} started.");
        }
        else
        {
            // Stop the line — score this attempt
            _lineMoving = false;
            ScoreAttempt();
        }
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void ScoreAttempt()
    {
        HitResult result = GetHitResult(_linePosition);
        _results[_attemptsDone] = result;
        _attemptsDone++;

        float points = result == HitResult.Green ? greenPoints
                     : result == HitResult.Yellow ? yellowPoints
                     : redPoints;
        _totalPoints += points;

        minigameUI?.ShowAttemptResult(result, _attemptsDone - 1);
        Debug.Log($"[CookingMinigame] Attempt {_attemptsDone}: {result} (+{points} pts)");

        if (_attemptsDone >= totalAttempts)
            StartCoroutine(FinishMinigame());
        else
            StartCoroutine(CooldownThenNext());
    }

    HitResult GetHitResult(float linePos)
    {
        float dist = Mathf.Abs(linePos - _greenCenter);

        if (dist <= greenZoneWidth / 2f) return HitResult.Green;
        if (dist <= yellowZoneWidth / 2f) return HitResult.Yellow;
        return HitResult.Red;
    }

    void RandomiseGreenPosition()
    {
        // Keep green zone away from edges so it's always fully visible
        float margin = yellowZoneWidth / 2f + 0.05f;
        _greenCenter = Random.Range(margin, 1f - margin);
        minigameUI?.UpdateZones(_greenCenter, greenZoneWidth, yellowZoneWidth);
        Debug.Log($"[CookingMinigame] Green center: {_greenCenter:F2}");
    }

    IEnumerator CooldownThenNext()
    {
        _awaitingCooldown = true;
        minigameUI?.SetButtonLabel("Wait...");
        minigameUI?.ShowCooldown(cooldownBetweenAttempts);

        yield return new WaitForSeconds(cooldownBetweenAttempts);

        _awaitingCooldown = false;
        RandomiseGreenPosition();
        minigameUI?.SetButtonLabel("Cook");
        Debug.Log($"[CookingMinigame] Ready for attempt {_attemptsDone + 1}.");
    }

    IEnumerator FinishMinigame()
    {
        yield return new WaitForSeconds(0.5f);

        CookQuality quality = CalculateQuality();
        Debug.Log($"[CookingMinigame] Final quality: {quality}");

        // Store quality on ingredient — rewards are applied at serve time, not here
        if (_ingredient != null)
        {
            _ingredient.ApplyCook();
            _ingredient.cookQuality = quality;

            // Return ingredient to station so it can be picked up
            _ingredient.locationState = IngredientItem.LocationState.OnStation;
            if (_ingredient.transform.parent != null)
                _ingredient.transform.SetParent(null, true);
            _ingredient.transform.position += Vector3.up * 0.05f;
        }

        // Notify calling station it's done
        _callingStation?.OnMinigameComplete();

        // Show result screen briefly
        minigameUI?.ShowFinalResult(quality, _totalPoints);

        yield return new WaitForSeconds(2.5f);

        // Hide UI and return camera to default cook angle
        minigameUI?.HideMinigame();
        CameraController.Instance?.GoTo(defaultCameraAngle);

        IsActive = false;
        _ingredient = null;
        _callingStation = null;

        OnCookComplete.Invoke(quality);
    }

    CookQuality CalculateQuality()
    {
        int greens = 0, yellows = 0, reds = 0;
        foreach (HitResult r in _results)
        {
            if (r == HitResult.Green) greens++;
            else if (r == HitResult.Yellow) yellows++;
            else reds++;
        }

        if (reds > 0) return CookQuality.Failed;
        if (greens == 3) return CookQuality.Perfect;
        if (greens == 2 && yellows == 1) return CookQuality.Great;
        if (greens == 1 && yellows == 2) return CookQuality.Good;
        return CookQuality.Poor;               // 3 yellows
    }

}