using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Applies and tracks buffs granted after each special NPC's minigame.
///
/// Buffs are data-driven: each SpecialNPCData has a buffDescription string.
/// Actual gameplay effects (e.g. faster cutting) are applied here by checking
/// ActiveBuffs in the relevant systems (e.g. CuttingStation reads CuttingSpeedMultiplier).
///
/// Setup:
///   - Attach to a persistent GameObject.
///   - Assign buffNotificationText (shows briefly when a buff is applied).
///   - Other systems read public properties like CuttingSpeedMultiplier.
/// </summary>
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI buffNotificationText;
    public float           notificationDuration = 3f;

    // ── Active buff values (read by other systems) ──────────────────────────
    public float CuttingSpeedMultiplier { get; private set; } = 1f;
    public float CookingSpeedMultiplier { get; private set; } = 1f;
    public float PopularityMultiplier   { get; private set; } = 1f;
    public float MoneyMultiplier        { get; private set; } = 1f;

    private List<string> _activeBuffDescriptions = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (buffNotificationText != null) buffNotificationText.text = "";
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the buff for the given NPC.
    /// Extend this method with more buff types as you design each NPC's minigame.
    /// </summary>
    public void ApplyBuff(SpecialNPCData npc)
    {
        _activeBuffDescriptions.Add(npc.buffDescription);

        // ── Buff assignments per NPC — add your buff logic here ────────────
        // Match by NPC name or by index. Example assignments (replace with real
        // buff types once each NPC's theme is decided):
        switch (npc.npcName)
        {
            case "NPC1_Name":
                CuttingSpeedMultiplier *= 1.5f;
                break;
            case "NPC2_Name":
                CookingSpeedMultiplier *= 1.5f;
                break;
            case "NPC3_Name":
                PopularityMultiplier   *= 1.25f;
                break;
            case "NPC4_Name":
                MoneyMultiplier        *= 1.25f;
                break;
            case "NPC5_Name":
                CuttingSpeedMultiplier *= 1.3f;
                CookingSpeedMultiplier *= 1.3f;
                break;
            default:
                Debug.Log($"[BuffManager] No buff mapping for {npc.npcName}.");
                break;
        }

        ShowNotification($"Buff unlocked: {npc.buffDescription}");
        Debug.Log($"[BuffManager] Applied buff from {npc.npcName}: {npc.buffDescription}");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void ShowNotification(string msg)
    {
        if (buffNotificationText == null) return;
        buffNotificationText.text = msg;
        CancelInvoke(nameof(ClearNotification));
        Invoke(nameof(ClearNotification), notificationDuration);
    }

    void ClearNotification()
    {
        if (buffNotificationText != null) buffNotificationText.text = "";
    }
}
