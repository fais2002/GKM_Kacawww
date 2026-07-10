using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates the special NPC event in Phase 3 (days 3-7).
///
/// Full sequence:
///   1. NPC enters → intro dialogue plays
///   2. Special order is shown → player prepares and serves
///   3. Post-order dialogue plays
///   4. Minigame scene loads additively
///   5. MinigameResult is received (called by the minigame scene)
///   6. Outro dialogue plays + buff/item granted
///   7. PhaseManager.EndSpecialPhase() called → day ends
///
/// Setup:
///   - Assign npcDataForEachDay (5 entries, index 0 = Day 3, index 4 = Day 7).
///   - Attach to a GameObject in the cooking scene.
///   - PhaseManager calls this via OnPhaseStart when phase == 3.
/// </summary>
public class SpecialNPCManager : MonoBehaviour
{
    public static SpecialNPCManager Instance { get; private set; }

    [Header("NPC roster — index 0 = Day 3, index 4 = Day 7")]
    public SpecialNPCData[] npcDataForEachDay;   // 5 entries

    // ── State ──────────────────────────────────────────────────────────────
    public SpecialNPCData ActiveNPC { get; private set; }
    public bool OrderServed { get; private set; } = false;
    public bool MinigameDone { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        PhaseManager.Instance.OnPhaseStart.AddListener(OnPhaseStart);
    }

    // ── Phase 3 entry ───────────────────────────────────────────────────────

    void OnPhaseStart(int phase, string name)
    {
        if (phase != 3) return;
        if (!DayManager.Instance.IsNPCDay) return;

        int npcIndex = DayManager.Instance.GetTodayNPCIndex();
        if (npcIndex < 0 || npcIndex >= npcDataForEachDay.Length)
        {
            Debug.LogWarning("[SpecialNPCManager] No NPC data for today.");
            PhaseManager.Instance.EndSpecialPhase();
            return;
        }

        ActiveNPC = npcDataForEachDay[npcIndex];
        StartCoroutine(RunNPCSequence());
    }

    // ── Main sequence ───────────────────────────────────────────────────────

    IEnumerator RunNPCSequence()
    {
        Debug.Log($"[SpecialNPCManager] {ActiveNPC.npcName} entering.");

        // 1. Intro dialogue
        bool introDone = false;
        DialogueSystem.Instance.PlayDialogue(
            ActiveNPC.introDialogue,
            ActiveNPC,
            () => introDone = true);
        yield return new WaitUntil(() => introDone);

        // 2. Show special order
        SpecialOrderUI.Instance?.ShowOrder(ActiveNPC);

        // 3. Wait for player to serve the order
        OrderServed = false;
        yield return new WaitUntil(() => OrderServed);

        // 4. Post-order dialogue
        bool postDone = false;
        DialogueSystem.Instance.PlayDialogue(
            ActiveNPC.postOrderDialogue,
            ActiveNPC,
            () => postDone = true);
        yield return new WaitUntil(() => postDone);

        // 5. Load minigame scene additively
        MinigameDone = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            ActiveNPC.minigameSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Additive);
        yield return new WaitUntil(() => MinigameDone);

        // 6. Outro dialogue
        bool outroDone = false;
        DialogueSystem.Instance.PlayDialogue(
            ActiveNPC.outroDialogue,
            ActiveNPC,
            () => outroDone = true);
        yield return new WaitUntil(() => outroDone);

        // 7. Grant buff
        BuffManager.Instance?.ApplyBuff(ActiveNPC);

        // 8. End phase → day ends
        PhaseManager.Instance.EndSpecialPhase();
    }

    // ── Called by GameManager when special order is served ─────────────────

    public void OnSpecialOrderServed(bool success)
    {
        if (success)
        {
            MoneySystem.Instance?.Earn(ActiveNPC.moneyReward);
            PopularityManager.Instance?.OnSpecialNPCServed(ActiveNPC.popularityReward);
        }
        OrderServed = true;
    }

    // ── Called by the minigame scene when it finishes ──────────────────────

    public void OnMinigameComplete()
    {
        MinigameDone = true;
        // Unload the minigame scene
        SceneManager.UnloadSceneAsync(ActiveNPC.minigameSceneName);
        Debug.Log("[SpecialNPCManager] Minigame complete.");
    }
}