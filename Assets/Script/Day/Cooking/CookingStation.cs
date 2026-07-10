using UnityEngine;
using TMPro;

/// <summary>
/// Handles dropping an ingredient onto the stove.
/// Instead of a simple timer, it now hands off to CookingMinigame
/// for the press-timing mechanic.
///
/// Setup:
///   - Attach to the Stove GameObject. Collider: Is Trigger = FALSE.
///   - Create a child empty named CookAnchor. Assign to ingredientAnchor.
///   - CookingMinigame and CookingMinigameUI must be in the scene.
/// </summary>
public class CookingStation : MonoBehaviour
{
    [Header("Anchor — ingredient snaps here when placed")]
    public Transform ingredientAnchor;

    [Header("Feedback UI (optional)")]
    public TextMeshProUGUI statusText;

    public bool IsBusy { get; private set; }
    public IngredientItem PlacedIngredient { get; private set; }

    // ── Called by IngredientDragHandler when ingredient is dropped here ─────

    public bool PlaceIngredient(IngredientItem item)
    {
        if (IsBusy)
        {
            GameManager.Instance?.ShowStatusMessage("Stove is busy!");
            return false;
        }
        if (item.prepState == IngredientItem.PrepState.Cooked)
        {
            GameManager.Instance?.ShowStatusMessage("Already cooked!");
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

        if (statusText != null) statusText.text = "Cooking...";

        // Route to the correct minigame based on the ingredient's mechanic type
        if (item.cookingMechanic == IngredientItem.CookingMechanicType.FlipTiming)
        {
            if (FlipMinigame.Instance != null)
                FlipMinigame.Instance.StartMinigame(item, this);
            else
                Debug.LogWarning("[CookingStation] FlipMinigame.Instance is null!");
        }
        else
        {
            if (CookingMinigame.Instance != null)
                CookingMinigame.Instance.StartMinigame(item, this);
            else
                Debug.LogWarning("[CookingStation] CookingMinigame.Instance is null!");
        }

        Debug.Log($"[CookingStation] {item.ingredientName} placed. Minigame starting.");
        return true;
    }

    /// <summary>Called by CookingMinigame when the minigame finishes.</summary>
    public void OnMinigameComplete()
    {
        IsBusy = false;
        PlacedIngredient = null;
        if (statusText != null) statusText.text = "";
        Debug.Log("[CookingStation] Minigame complete. Stove free.");
    }
}