using UnityEngine;
using TMPro;

/// <summary>
/// Handles dropping an ingredient onto the cutting board.
/// Hands off to CuttingMinigame instead of the old timed button.
///
/// Setup:
///   - Attach to the CuttingBoard GameObject. Collider: Is Trigger = FALSE.
///   - Create a child empty named CutAnchor. Assign to ingredientAnchor.
///   - CuttingMinigame and CuttingMinigameUI must be in the scene.
/// </summary>
public class CuttingStation : MonoBehaviour
{
    [Header("Anchor — ingredient snaps here when placed")]
    public Transform ingredientAnchor;

    [Header("Feedback UI (optional)")]
    public TextMeshProUGUI statusText;

    public bool IsBusy { get; private set; }
    public IngredientItem PlacedIngredient { get; private set; }

    // ── Called by IngredientDragHandler ────────────────────────────────────

    public bool PlaceIngredient(IngredientItem item)
    {
        if (IsBusy)
        {
            GameManager.Instance?.ShowStatusMessage("Cutting board is busy!");
            return false;
        }
        if (!item.canBeCut)
        {
            GameManager.Instance?.ShowStatusMessage($"{item.ingredientName} can't be cut!");
            return false;
        }

        PlacedIngredient = item;
        IsBusy = true;
        item.locationState = IngredientItem.LocationState.OnStation;

        // Snap to anchor
        if (ingredientAnchor != null)
        {
            item.transform.SetParent(ingredientAnchor);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }

        if (statusText != null) statusText.text = "Trace the dotted lines!";

        // Hand off to cutting minigame
        if (CuttingMinigame.Instance != null)
            CuttingMinigame.Instance.StartMinigame(item, this);
        else
            Debug.LogWarning("[CuttingStation] CuttingMinigame.Instance is null!");

        Debug.Log($"[CuttingStation] {item.ingredientName} placed. Minigame starting.");
        return true;
    }

    /// <summary>Called by CuttingMinigame when all lines are traced.</summary>
    public void OnMinigameComplete()
    {
        IsBusy = false;
        PlacedIngredient = null;
        if (statusText != null) statusText.text = "";
        Debug.Log("[CuttingStation] Cutting minigame complete. Board free.");
    }
}