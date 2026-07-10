using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// All-in-one camera controller for the cooking scene.
///
/// — Add as many named angles as you want from the Inspector.
/// — Each angle has an anchor Transform, an optional UI Button, and a flag
///   for whether keyboard scroll is allowed at that angle.
/// — The camera smoothly lerps between angles.
/// — Keyboard scroll (A/D or arrow keys) is built in and automatically
///   enabled only for angles that have allowScroll = true.
///
/// Setup:
///   1. Attach this script to Main Camera. Remove any old CameraScroll component.
///   2. Create empty GameObjects in the scene for each angle (e.g. CookAnchor,
///      ServeAnchor, TrayAnchor). Position and rotate them exactly where you
///      want the camera to sit for that angle.
///   3. In the Inspector, expand the Angles list. Add one entry per angle.
///      Fill in: Name, Anchor, Allow Scroll, and optionally a UI Button.
///   4. Set Default Angle Index to whichever angle should be active on start.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    // ── Per-angle data (shown in Inspector) ────────────────────────────────

    [System.Serializable]
    public class CameraAngleEntry
    {
        [Tooltip("Display name for this angle (used for logging and the angle label UI).")]
        public string angleName = "New Angle";

        [Tooltip("Empty GameObject placed in the scene at the desired camera position/rotation.")]
        public Transform anchor;

        [Tooltip("If true, keyboard left/right scroll is active at this angle.")]
        public bool allowScroll = false;

        [Tooltip("Optional UI button that switches to this angle when pressed.")]
        public Button button;
    }

    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Camera angles — add as many as you need")]
    public List<CameraAngleEntry> angles = new();

    [Header("Start angle")]
    [Tooltip("Index in the Angles list that is active when the scene starts.")]
    public int defaultAngleIndex = 0;

    [Header("Transition")]
    [Tooltip("How fast the camera lerps to the target anchor. Higher = snappier.")]
    public float transitionSpeed = 4f;

    [Header("Scroll settings (used when allowScroll = true)")]
    public float scrollSpeed = 8f;
    public float scrollDeceleration = 10f;
    public float scrollMinX = 0f;
    public float scrollMaxX = 20f;

    [Header("Optional UI")]
    [Tooltip("TMP text that shows the current angle name.")]
    public TextMeshProUGUI angleLabel;

    // ── Runtime state ──────────────────────────────────────────────────────

    public int CurrentAngleIndex { get; private set; } = 0;
    public string CurrentAngleName => angles.Count > 0 ? angles[CurrentAngleIndex].angleName : "";
    public bool IsTransitioning { get; private set; } = false;

    private Transform _targetAnchor;
    private float _scrollVelocity = 0f;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        // Wire each angle's optional button
        for (int i = 0; i < angles.Count; i++)
        {
            int captured = i;   // capture for lambda
            angles[captured].button?.onClick.AddListener(() => GoTo(captured));
        }

        // Snap to default angle immediately — no lerp on start
        if (angles.Count > 0)
            SnapTo(Mathf.Clamp(defaultAngleIndex, 0, angles.Count - 1));
    }

    void Update()
    {
        UpdateTransition();
        UpdateScroll();
    }

    // ── Transition ─────────────────────────────────────────────────────────

    void UpdateTransition()
    {
        if (_targetAnchor == null) return;

        transform.position = Vector3.Lerp(
            transform.position,
            _targetAnchor.position,
            Time.deltaTime * transitionSpeed);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetAnchor.rotation,
            Time.deltaTime * transitionSpeed);

        if (IsTransitioning)
        {
            bool close = Vector3.Distance(transform.position, _targetAnchor.position) < 0.01f
                      && Quaternion.Angle(transform.rotation, _targetAnchor.rotation) < 0.1f;
            if (close)
            {
                transform.SetPositionAndRotation(_targetAnchor.position, _targetAnchor.rotation);
                IsTransitioning = false;
            }
        }
    }

    // ── Scroll ─────────────────────────────────────────────────────────────

    void UpdateScroll()
    {
        // Only scroll if current angle allows it and no transition is playing
        if (IsTransitioning) return;
        if (angles.Count == 0) return;
        if (!angles[CurrentAngleIndex].allowScroll) return;

        float input = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input = 1f;

        if (input != 0f)
            _scrollVelocity = Mathf.MoveTowards(_scrollVelocity, input * scrollSpeed, scrollDeceleration * Time.deltaTime);
        else
            _scrollVelocity = Mathf.MoveTowards(_scrollVelocity, 0f, scrollDeceleration * Time.deltaTime);

        if (Mathf.Abs(_scrollVelocity) > 0.001f)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + _scrollVelocity * Time.deltaTime, scrollMinX, scrollMaxX);
            transform.position = pos;

            // Keep the anchor's X in sync so snapping back doesn't pull
            // the camera to the original anchor X when scroll is active
            if (_targetAnchor != null)
            {
                Vector3 ap = _targetAnchor.position;
                ap.x = pos.x;
                _targetAnchor.position = ap;
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Smoothly transition to the angle at the given index.</summary>
    public void GoTo(int index)
    {
        if (index < 0 || index >= angles.Count)
        {
            Debug.LogWarning($"[CameraController] Invalid angle index: {index}");
            return;
        }

        CurrentAngleIndex = index;
        _targetAnchor = angles[index].anchor;
        IsTransitioning = true;
        _scrollVelocity = 0f;

        if (angleLabel != null) angleLabel.text = angles[index].angleName;
        Debug.Log($"[CameraController] → {angles[index].angleName}");
    }

    /// <summary>Smoothly transition to the angle by name.</summary>
    public void GoTo(string name)
    {
        int idx = angles.FindIndex(a => a.angleName == name);
        if (idx < 0) { Debug.LogWarning($"[CameraController] Angle '{name}' not found."); return; }
        GoTo(idx);
    }

    /// <summary>Instantly snap to an angle with no lerp (e.g. on scene start).</summary>
    public void SnapTo(int index)
    {
        if (index < 0 || index >= angles.Count) return;

        CurrentAngleIndex = index;
        _targetAnchor = angles[index].anchor;
        IsTransitioning = false;
        _scrollVelocity = 0f;

        if (_targetAnchor != null)
            transform.SetPositionAndRotation(_targetAnchor.position, _targetAnchor.rotation);

        if (angleLabel != null) angleLabel.text = angles[index].angleName;
    }

    // ── Convenience properties (for other scripts) ─────────────────────────

    /// <summary>Returns true if keyboard scrolling is currently active.</summary>
    public bool IsDragging => false;   // kept for IngredientDragHandler compatibility
}