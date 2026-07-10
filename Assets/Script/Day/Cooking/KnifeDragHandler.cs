using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to the KnifeImage RectTransform in the Canvas.
/// Implements Unity's drag interface — no EventTrigger needed on the knife.
///
/// The knife follows the mouse while held and snaps back to its
/// origin position when released.
///
/// IsDragging is a static bool so CuttingMinigameUI can read it
/// from the zone EventTriggers without needing a reference.
///
/// Setup:
///   - Attach to the KnifeImage GameObject (must have an Image component).
///   - KnifeImage must be inside a Canvas set to Screen Space Overlay.
///   - Set its Raycast Target = TRUE on the Image component.
///   - No other configuration needed.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class KnifeDragHandler : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Feel")]
    [Tooltip("How fast the knife snaps back to its resting position after release.")]
    public float returnSpeed = 12f;

    // ── Static so CuttingMinigameUI can read it anywhere ──────────────────
    public static bool IsDragging { get; private set; } = false;

    // ── Runtime ────────────────────────────────────────────────────────────
    private RectTransform _rt;
    private Canvas _canvas;
    private Vector2 _originAnchoredPos;
    private bool _returning = false;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _originAnchoredPos = _rt.anchoredPosition;
    }

    void Update()
    {
        if (_returning && !IsDragging)
        {
            _rt.anchoredPosition = Vector2.Lerp(
                _rt.anchoredPosition,
                _originAnchoredPos,
                Time.deltaTime * returnSpeed);

            if (Vector2.Distance(_rt.anchoredPosition, _originAnchoredPos) < 0.5f)
            {
                _rt.anchoredPosition = _originAnchoredPos;
                _returning = false;
            }
        }
    }

    // ── Drag interface ─────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;
        _returning = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canvas == null) return;

        // Convert screen delta to canvas space
        Vector2 delta = eventData.delta;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            delta /= _canvas.scaleFactor;

        _rt.anchoredPosition += delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        _returning = true;
    }

    /// <summary>Called by CuttingMinigameUI.HideMinigame() to reset state.</summary>
    public void ResetDragState()
    {
        IsDragging = false;
        _returning = false;
        _rt.anchoredPosition = _originAnchoredPos;
    }

    /// <summary>Returns the knife's current anchored position (used for proximity checks).</summary>
    public Vector2 CurrentAnchoredPosition => _rt.anchoredPosition;
}