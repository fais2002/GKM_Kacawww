using UnityEngine;

/// <summary>
/// ScriptableObject that defines one special NPC.
/// Create via: Assets → Create → Kuloniku → Special NPC Data
///
/// Expression system:
///   Each NPC has a list of expression sprites (expressions[]).
///   Index 0 is always the default/neutral face.
///   Index 1, 2, 3... are other expressions (happy, surprised, angry, etc.)
///
///   Each dialogue line has an expressionIndex field. When shown, the
///   DialogueSystem displays the matching sprite. If the index is 0 or
///   not assigned, the default face is shown.
///
///   Example setup:
///     expressions[0] = NPC1_Default
///     expressions[1] = NPC1_Happy
///     expressions[2] = NPC1_Surprised
///     expressions[3] = NPC1_Angry
///     expressions[4] = NPC1_Confused
/// </summary>
[CreateAssetMenu(fileName = "SpecialNPCData", menuName = "Kuloniku/Special NPC Data")]
public class SpecialNPCData : ScriptableObject
{
    // ── Identity ───────────────────────────────────────────────────────────

    [Header("Identity")]
    public string npcName = "Special Guest";

    // ── Expression sprites ─────────────────────────────────────────────────

    [Header("Expressions")]
    [Tooltip("List of half-body sprites for this NPC.\n" +
             "Index 0 = default face (shown when no expression is set).\n" +
             "Index 1 = happy, 2 = surprised, 3 = angry, 4 = confused, etc.\n" +
             "You can add as many as you need.")]
    public Sprite[] expressions;

    /// <summary>
    /// Returns the sprite for the given expression index.
    /// Falls back to index 0 (default) if index is out of range or array is empty.
    /// </summary>
    public Sprite GetExpression(int index)
    {
        if (expressions == null || expressions.Length == 0) return null;
        if (index < 0 || index >= expressions.Length) return expressions[0];
        return expressions[index] ?? expressions[0];
    }

    // ── Dialogue line struct ───────────────────────────────────────────────

    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("The text shown in the dialogue box for this line.")]
        [TextArea(2, 4)]
        public string text = "";

        [Tooltip("Expression index to display for this line.\n" +
                 "0 = default, 1 = happy, 2 = surprised, etc.\n" +
                 "Leave at 0 for the default face.")]
        public int expressionIndex = 0;
    }

    // ── Dialogues ──────────────────────────────────────────────────────────

    [Header("Intro dialogue (shown when NPC enters)")]
    public DialogueLine[] introDialogue;

    [Header("Post-order dialogue (shown after order is served)")]
    public DialogueLine[] postOrderDialogue;

    [Header("Outro dialogue (shown after minigame)")]
    public DialogueLine[] outroDialogue;

    // ── Order ──────────────────────────────────────────────────────────────

    [Header("Special order")]
    [Tooltip("Ingredient names the player must serve. Must match IngredientItem.ingredientName exactly.")]
    public string[] requiredIngredients;

    // ── Rewards ────────────────────────────────────────────────────────────

    [Header("Rewards")]
    public float popularityReward = 30f;
    public float moneyReward = 50f;

    // ── Minigame ───────────────────────────────────────────────────────────

    [Header("Minigame")]
    [Tooltip("Name of the Unity scene to load additively for this NPC's minigame.")]
    public string minigameSceneName = "Minigame_Placeholder";

    // ── Buff ───────────────────────────────────────────────────────────────

    [Header("Post-minigame buff / item")]
    [TextArea(1, 3)]
    public string buffDescription = "Speed buff applied!";
}