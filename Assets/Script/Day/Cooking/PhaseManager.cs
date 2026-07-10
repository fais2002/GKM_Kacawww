using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages 3 phases per day:
///   Phase 1 — Morning   (7 am–12 pm)  default 300s
///   Phase 2 — Afternoon (1 pm–3 pm)   default 120s
///   Phase 3 — Special NPC phase       triggered by SpecialNPCManager, no timer
///   phase 4 - Night (Lifetime)        triggered after phase 2, Afternoon
///
/// Phase 3 is only active on days 3–7.
/// On days 1–2 the day ends after Phase 2.
///
/// Events:
///   OnPhaseStart(phaseIndex 1-3, phaseName)
///   OnPhaseEnd(phaseIndex)
///   OnPhaseTimeTick(secondsRemaining)
///   OnBreakStart
///   OnDayEnd
/// </summary>
public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    public const int PHASES_PER_DAY = 3;

    [Header("Phase durations (seconds)")]
    public float morningDuration = 300f;   // 5 min (represents 7am-12pm)
    public float afternoonDuration = 120f;   // 2 min (represents 1pm-3pm)
    // Phase 3 has no timer — it ends when SpecialNPCManager calls EndSpecialPhase()

    [Header("Break between phases (seconds)")]
    public float breakDuration = 5f;

    public static readonly string[] PHASE_NAMES =
    {
        "Morning",      // index 0 → Phase 1
        "Afternoon",    // index 1 → Phase 2
        "Special",       // index 2 → Phase 3
        "Night"         // index 3 -> Phase 4
    };

    // ── Events ─────────────────────────────────────────────────────────────
    public UnityEvent<int, string> OnPhaseStart = new();
    public UnityEvent<int> OnPhaseEnd = new();
    public UnityEvent<float> OnPhaseTimeTick = new();
    public UnityEvent OnBreakStart = new();
    public UnityEvent OnDayEnd = new();

    // ── State ──────────────────────────────────────────────────────────────
    public int CurrentPhase { get; private set; } = 0;
    public float TimeRemaining { get; private set; } = 0f;
    public bool PhaseIsRunning { get; private set; } = false;
    public bool IsOnBreak { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void StartDay()
    {
        CurrentPhase = 0;
        StartNextPhase();
    }

    /// <summary>
    /// Called by SpecialNPCManager when the special NPC sequence is complete.
    /// Ends Phase 3 and fires OnDayEnd.
    /// </summary>
    public void EndSpecialPhase()
    {
        PhaseIsRunning = false;
        OnPhaseEnd.Invoke(CurrentPhase);
        Debug.Log("[PhaseManager] Special phase ended by NPC manager.");
        TriggerNightTransition();
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void StartNextPhase()
    {
        CurrentPhase++;

        // Days 1-2: only 2 phases
        bool hasSpecialPhase = DayManager.Instance.CurrentDay >= 3;
        //int  maxPhases       = hasSpecialPhase ? 4 : 3;

        if (!hasSpecialPhase && CurrentPhase == 3)
        {
            TriggerNightTransition();
            return;
        }

        //if (CurrentPhase > maxPhases)
        //{
        //OnDayEnd.Invoke();
        //return;
        //}

        if (CurrentPhase > 4)
        {
            return;
        }

        StartCoroutine(RunPhase(CurrentPhase));
    }

    IEnumerator RunPhase(int phase)
    {
        // Break before phases 2 and 3 - but Phase 4 is different scene
        if (phase > 1 && phase != 4)
        {
            IsOnBreak = true;
            OnBreakStart.Invoke();
            yield return new WaitForSeconds(breakDuration);
            IsOnBreak = false;
        }

        PhaseIsRunning = true;
        string name = PHASE_NAMES[phase - 1];
        OnPhaseStart.Invoke(phase, name);
        Debug.Log($"[PhaseManager] Phase {phase} ({name}) started.");

        // Phase 3 has no timer — SpecialNPCManager drives it
        if (phase == 3)
        {
            // Just signal start; EndSpecialPhase() will close it
            yield break;
        }

        // same as Phase 3, Phase 4 has no timer
        if (phase == 4)
        {
            yield break; // Just signal start; EndSpecialPhase() will close it
        }

        // Timed phases 1 and 2
        TimeRemaining = phase == 1 ? morningDuration : afternoonDuration;

        while (TimeRemaining > 0f)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining < 0f) TimeRemaining = 0f;
            OnPhaseTimeTick.Invoke(TimeRemaining);
            yield return null;
        }

        PhaseIsRunning = false;
        OnPhaseEnd.Invoke(phase);
        Debug.Log($"[PhaseManager] Phase {phase} ended.");

        yield return new WaitForSeconds(1f);
        StartNextPhase();
    }

    public void TriggerNightTransition()
    {
        PhaseIsRunning = false;
        OnPhaseEnd.Invoke(CurrentPhase);
        Debug.Log("[PhaseManager] Transitioning to Night scene.");

        CurrentPhase = 4;
        DayManager.Instance.LoadNightScene();
    }

    /// <summary>Dipanggil dari trigger "Tidur/Lanjut Hari" di scene malam.</summary>
    public void EndNightPhase()
    {
        OnPhaseEnd.Invoke(4);
        OnDayEnd.Invoke(); // baru di sini hari benar-benar berakhir
    }

    /// <summary>
    /// Dipanggil oleh DayManager setelah scene malam selesai load.
    /// PhaseManager di scene malam langsung mulai di Phase 4, skip 1-3.
    /// </summary>
    public void ResumeAsNightPhase()
    {
        CurrentPhase = 4;
        StartCoroutine(RunPhase(4));
    }

    IEnumerator DelayedDayEnd()
    {
        yield return new WaitForSeconds(2f);
        OnDayEnd.Invoke();
    }
}
