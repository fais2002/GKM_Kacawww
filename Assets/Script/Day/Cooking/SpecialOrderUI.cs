using UnityEngine;
using TMPro;

/// <summary>
/// Shows the special NPC's order on screen.
/// Attach to a dedicated UI panel in the Canvas.
///
/// Setup:
///   - Assign orderPanel, npcNameText, orderListText.
///   - The panel starts inactive and is shown by SpecialNPCManager.
/// </summary>
public class SpecialOrderUI : MonoBehaviour
{
    public static SpecialOrderUI Instance { get; private set; }

    [Header("UI")]
    public GameObject      orderPanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI orderListText;
    public TextMeshProUGUI rewardText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        orderPanel?.SetActive(false);
    }

    public void ShowOrder(SpecialNPCData npc)
    {
        if (npcNameText  != null) npcNameText.text  = $"{npc.npcName}'s Order:";
        if (rewardText   != null) rewardText.text   = $"+{npc.popularityReward} popularity";

        var sb = new System.Text.StringBuilder();
        foreach (string ing in npc.requiredIngredients)
            sb.AppendLine($"  • {ing}");
        if (orderListText != null) orderListText.text = sb.ToString();

        orderPanel?.SetActive(true);
    }

    public void HideOrder()
    {
        orderPanel?.SetActive(false);
    }
}
