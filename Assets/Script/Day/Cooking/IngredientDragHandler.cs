using UnityEngine;

/// <summary>
/// Attach to every IngredientItem on the station.
/// Supports click-select and drag-and-drop.
/// Any ingredient in any prep state can be placed on the tray.
///
/// Placement detection uses a small sphere-cast instead of a single raycast,
/// so dropping near the tray (not pixel-perfect) still registers.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(IngredientItem))]
public class IngredientDragHandler : MonoBehaviour
{
    [Header("Drag")]
    public float holdHeight = 1.5f;

    [Header("Placement detection")]
    [Tooltip("Radius of the sphere used to detect what's below the ingredient on release. Increase if drops are missing the tray.")]
    public float placementCheckRadius = 0.3f;
    [Tooltip("How far down to check for a placement target.")]
    public float placementCheckDistance = 5f;

    private IngredientItem _item;
    private bool _isDragging;
    private bool _isClickSelected;
    private Vector3 _originPos;
    private Quaternion _originRot;
    private Transform _originParent;
    private Camera _cam;
    private CameraController _camCtrl;

    private Renderer _rend;
    private bool _emissionWasEnabled;
    private Color _originalEmission;

    private const float DRAG_THRESHOLD = 8f;
    private Vector3 _mouseDownScreen;
    private bool _mouseDownOnThis;

    void Awake()
    {
        _item = GetComponent<IngredientItem>();
        _cam = Camera.main;
        _camCtrl = _cam?.GetComponent<CameraController>();
    }

    void Start()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend != null)
        {
            _emissionWasEnabled = _rend.material.IsKeywordEnabled("_EMISSION");
            _originalEmission = _rend.material.GetColor("_EmissionColor");
        }
    }

    void Update()
    {
        if (_isDragging) DragUpdate();
    }

    // ── Mouse ──────────────────────────────────────────────────────────────

    void OnMouseDown()
    {
        if (!PhaseManager.Instance.PhaseIsRunning) return;
        if (_camCtrl != null && _camCtrl.IsDragging) return;
        if (!_item.IsPickable) return;
        // Block pickup while cooking minigame is running
        if (CookingMinigame.Instance != null && CookingMinigame.Instance.IsActive) return;
        // Block pickup while flip minigame is running
        if (FlipMinigame.Instance != null && FlipMinigame.Instance.IsActive) return;
        // Block pickup while cutting minigame is running
        if (CuttingMinigame.Instance != null && CuttingMinigame.Instance.IsActive) return;

        _mouseDownScreen = Input.mousePosition;
        _mouseDownOnThis = true;
        _originPos = transform.position;
        _originRot = transform.rotation;
        _originParent = transform.parent;
    }

    void OnMouseDrag()
    {
        if (!_mouseDownOnThis) return;
        if (!_isDragging &&
            Vector3.Distance(Input.mousePosition, _mouseDownScreen) > DRAG_THRESHOLD)
        {
            Lift();
            _isDragging = true;
        }
    }

    void OnMouseUp()
    {
        if (!_mouseDownOnThis) return;
        _mouseDownOnThis = false;

        if (_isDragging)
        {
            TryPlaceOnRelease();
            _isDragging = false;
        }
        else
        {
            if (_isClickSelected) CancelClickSelect();
            else StartClickSelect();
        }
    }

    // ── Lift ───────────────────────────────────────────────────────────────

    void Lift()
    {
        _item.locationState = IngredientItem.LocationState.BeingHeld;
        // Defensive: always fully detach from any parent (anchor, slot, etc.)
        // the instant the player starts dragging, regardless of what state
        // the ingredient was left in by a station or tray.
        transform.SetParent(null, true);
    }

    void StartClickSelect()
    {
        _isClickSelected = true;
        _item.locationState = IngredientItem.LocationState.BeingHeld;
        GameManager.Instance.SetClickSelected(this);
        Highlight(true);
    }

    public void CancelClickSelect()
    {
        _isClickSelected = false;
        _item.locationState = IngredientItem.LocationState.OnStation;
        GameManager.Instance.ClearClickSelected();
        Highlight(false);
        ReturnToOrigin();
    }

    void DragUpdate()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.up * holdHeight);
        if (plane.Raycast(ray, out float dist))
            transform.position = Vector3.Lerp(transform.position, ray.GetPoint(dist), Time.deltaTime * 20f);
    }

    void TryPlaceOnRelease()
    {
        // Use SphereCastAll to get EVERY collider under the ingredient,
        // then pick the most relevant one (Tray > CuttingStation > CookingStation)
        // instead of just whichever is physically closest. This prevents the
        // station counter/plane from "winning" just because it sits directly
        // beneath the tray.
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit[] hits = Physics.SphereCastAll(ray, placementCheckRadius, placementCheckDistance);

        if (hits.Length == 0)
        {
            Debug.Log("[IngredientDragHandler] SphereCast hit nothing below — returning to station.");
            ReturnToOrigin();
            return;
        }

        Collider best = PickBestTarget(hits);

        if (best == null)
        {
            Debug.Log("[IngredientDragHandler] No relevant target found (only hit the counter/floor) — returning to station.");
            ReturnToOrigin();
            return;
        }

        Debug.Log($"[IngredientDragHandler] Placing on: {best.gameObject.name}");
        TryPlaceOnCollider(best);
    }

    /// <summary>
    /// Scans all colliders hit below the ingredient and returns the most
    /// relevant one in priority order: FoodTray/TraySlot > CuttingStation >
    /// CookingStation. Plain scenery (counters, floors) is ignored entirely
    /// so it never blocks a valid tray/station placement underneath it.
    /// </summary>
    Collider PickBestTarget(RaycastHit[] hits)
    {
        Collider trayHit = null, cutHit = null, cookHit = null;

        foreach (RaycastHit h in hits)
        {
            Collider c = h.collider;
            if (c.GetComponentInParent<FoodTray>() != null || c.GetComponent<TraySlot>() != null)
                trayHit = c;
            else if (c.GetComponentInParent<CuttingStation>() != null)
                cutHit = c;
            else if (c.GetComponentInParent<CookingStation>() != null)
                cookHit = c;
        }

        if (trayHit != null) return trayHit;
        if (cutHit != null) return cutHit;
        if (cookHit != null) return cookHit;
        return null;
    }

    // ── Placement ──────────────────────────────────────────────────────────

    public void TryPlaceOnCollider(Collider col)
    {
        // 1. Cutting station?
        CuttingStation cut = col.GetComponentInParent<CuttingStation>();
        if (cut != null)
        {
            bool ok = cut.PlaceIngredient(_item);
            if (ok) FinishSelect();
            else ReturnToOrigin();
            return;
        }

        // 2. Cooking station?
        CookingStation cook = col.GetComponentInParent<CookingStation>();
        if (cook != null)
        {
            bool ok = cook.PlaceIngredient(_item);
            if (ok) FinishSelect();
            else ReturnToOrigin();
            return;
        }

        // 3. Food tray — accept ANY ingredient in ANY prep state
        FoodTray tray = GameManager.Instance.ActiveTray;

        if (tray == null)
        {
            Debug.Log("[IngredientDragHandler] No active tray — pick one up from TrayStack first.");
            GameManager.Instance?.ShowStatusMessage("Grab a tray first!");
            ReturnToOrigin();
            return;
        }

        bool hitTray = col.GetComponentInParent<FoodTray>() != null || col.GetComponent<TraySlot>() != null;

        if (hitTray)
        {
            TraySlot slot = tray.AddItem(_item);
            if (slot == null)
            {
                Debug.Log("[IngredientDragHandler] Tray is full.");
                GameManager.Instance?.ShowStatusMessage("Tray is full!");
                ReturnToOrigin();
            }
            else
            {
                FinishSelect();
                GameManager.Instance.OnItemAddedToTray(_item);
            }
            return;
        }

        Debug.Log($"[IngredientDragHandler] Hit '{col.gameObject.name}' but it's not the tray, cutting board, or stove.");
        ReturnToOrigin();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void ReturnToOrigin()
    {
        _item.locationState = IngredientItem.LocationState.OnStation;
        _isClickSelected = false;
        Highlight(false);
        transform.SetParent(_originParent);
        transform.position = _originPos;
        transform.rotation = _originRot;
    }

    void FinishSelect()
    {
        _isClickSelected = false;
        Highlight(false);
        GameManager.Instance.ClearClickSelected();
    }

    void Highlight(bool on)
    {
        if (_rend == null) return;
        if (on)
        {
            _rend.material.EnableKeyword("_EMISSION");
            _rend.material.SetColor("_EmissionColor", Color.white * 0.4f);
        }
        else
        {
            if (!_emissionWasEnabled) _rend.material.DisableKeyword("_EMISSION");
            _rend.material.SetColor("_EmissionColor", _originalEmission);
        }
    }
}