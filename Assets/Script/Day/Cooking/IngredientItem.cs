using UnityEngine;

/// <summary>
/// Attach to every ingredient/food GameObject on the cooking station.
///
/// Prep states:
///   Raw    — just picked up, unprocessed
///   Cut    — has been cut on the cutting board
///   Cooked — has been cooked on the stove
///   Ready  — needs no processing (ready from the start)
///
/// Any ingredient in ANY state can now be picked up and placed on a tray.
/// Whether it is the right state for the order is checked at serve time.
/// </summary>
public class IngredientItem : MonoBehaviour
{
    [Header("Identity")]
    public string ingredientName = "Ingredient";

    [Tooltip("SO Ingredient yang menjadi data source item ini")]
    public Ingredient ingredientData; // assign di prefab Inspector

    [Header("Processing flags")]
    [Tooltip("If true, this ingredient can be placed on the cutting board.")]
    public bool canBeCut = false;
    [Tooltip("If true, this ingredient can be placed on the stove.")]
    public bool needsCooking = false;

    public enum CookingMechanicType { MovingBar, FlipTiming }
    [Tooltip("Which cooking minigame is used when this ingredient is placed on the stove.\n" +
             "MovingBar = press-timing bar mechanic.\n" +
             "FlipTiming = wait-then-flip window mechanic.")]
    public CookingMechanicType cookingMechanic = CookingMechanicType.MovingBar;

    [Header("Sell value")]
    public float sellPrice = 15f;

    [Header("Visuals")]
    public Material matRaw;
    public Material matCut;
    public Material matCooked;

    // ── State ──────────────────────────────────────────────────────────────

    public enum PrepState { Raw, Cut, Cooked, Ready }
    public enum LocationState { OnStation, BeingHeld, OnTray }

    [HideInInspector] public PrepState prepState = PrepState.Raw;
    [HideInInspector] public LocationState locationState = LocationState.OnStation;
    [HideInInspector] public TraySlot occupiedSlot;
    [HideInInspector] public CookingMinigame.CookQuality cookQuality = CookingMinigame.CookQuality.Good;

    /// <summary>
    /// Any ingredient can be picked up regardless of prep state.
    /// The order validation at serve time decides if it counts as correct.
    /// </summary>
    public bool IsPickable => locationState == LocationState.OnStation;

    private Renderer _rend;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();

        // Ingredients that need no processing start as Ready
        if (!canBeCut && !needsCooking)
            prepState = PrepState.Ready;

        RefreshVisual();
    }

    public void Init(Ingredient data)
    {
        ingredientData = data;

        // ambil semua data dari SO
        ingredientName = data.ingredientName;
        sellPrice      = data.cost;

        // prepState berdasarkan flag di prefab (canBeCut, needsCooking)
        // tidak diambil dari SO karena ini behaviour 3D, bukan data SO
        if (!canBeCut && !needsCooking)
            prepState = PrepState.Ready;

        Debug.Log($"[IngredientItem] Init: {ingredientName} | Harga: {sellPrice}");
    }

    // ── Processing ─────────────────────────────────────────────────────────

    public void ApplyCut()
    {
        if (!canBeCut)
        {
            Debug.Log($"[IngredientItem] {ingredientName} has canBeCut=false.");
            return;
        }
        prepState = PrepState.Cut;
        RefreshVisual();
        Debug.Log($"[IngredientItem] {ingredientName} → Cut");
    }

    public void ApplyCook()
    {
        prepState = PrepState.Cooked;
        RefreshVisual();
        Debug.Log($"[IngredientItem] {ingredientName} → Cooked");
    }

    // ── Visual ─────────────────────────────────────────────────────────────

    void RefreshVisual()
    {
        if (_rend == null) return;
        switch (prepState)
        {
            case PrepState.Cut: if (matCut != null) _rend.material = matCut; break;
            case PrepState.Cooked: if (matCooked != null) _rend.material = matCooked; break;
            default: if (matRaw != null) _rend.material = matRaw; break;
        }
    }
}