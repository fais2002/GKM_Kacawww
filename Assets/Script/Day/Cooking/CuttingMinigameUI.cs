using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all visuals and input for the cutting minigame.
///
/// Knife drag: KnifeDragHandler on KnifeImage handles movement.
/// Line detection: every frame during drag, checks if knife is within
///   detectionZoneThickness pixels of each line's center (perpendicular
///   distance). Works at any drag speed — no EventTrigger zones needed.
///
/// Canvas setup (Screen Space — Overlay):
///   CuttingPanel (full-screen, semi-transparent, inactive by default)
///     LinesContainer  — sized to match ingredient preview (assign as panelRect)
///     KnifeImage      — Image + KnifeDragHandler
///     InstructionText — TMP
///     CompletionText  — TMP (inactive by default)
/// </summary>
public class CuttingMinigameUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject cuttingPanel;
    [Tooltip("The RectTransform that lines are spawned inside. " +
             "Size it to match your ingredient preview area.")]
    public RectTransform panelRect;

    [Header("Knife — must have KnifeDragHandler attached")]
    public KnifeDragHandler knifeHandler;

    [Header("Line visuals")]
    [Tooltip("Dashed/dotted sprite for an uncut line.")]
    public Sprite dottedLineSprite;
    [Tooltip("Solid sprite for a sliced line (optional — color change is enough).")]
    public Sprite slicedLineSprite;
    public Color dottedColor = new Color(1f, 1f, 1f, 0.75f);
    public Color slicedColor = new Color(0.25f, 0.9f, 0.25f, 1f);
    [Tooltip("Visual height of the dotted line in pixels.")]
    public float lineThickness = 10f;

    [Header("Detection")]
    [Tooltip("Perpendicular distance (pixels) within which the knife counts as crossing a line. " +
             "Increase for easier detection; 25-40 is comfortable.")]
    public float detectionDistance = 30f;

    [Header("Text")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI completionText;

    // ── Runtime ────────────────────────────────────────────────────────────

    // Per-line data stored after BuildLines
    private struct LineRecord
    {
        public Image image;
        public bool done;
        public Vector2 centerAnchoredPos;   // center in panel local space
        public float rotationDeg;         // line rotation
        public float halfLength;          // half-length in pixels
    }

    private List<LineRecord> _lines = new();

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        cuttingPanel?.SetActive(false);
        if (completionText != null) completionText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Only check proximity while knife is being dragged
        if (!KnifeDragHandler.IsDragging) return;
        if (knifeHandler == null || panelRect == null) return;

        Vector2 knifePos = knifeHandler.CurrentAnchoredPosition;

        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].done) continue;

            if (IsKnifeOverLine(knifePos, _lines[i]))
            {
                MarkLineDone(i);
                CuttingMinigame.Instance?.OnLineSliced(i);
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void ShowMinigame(SlicePatternData pattern)
    {
        cuttingPanel?.SetActive(true);

        if (instructionText != null)
            instructionText.text = "Drag the knife across each dotted line!";
        if (completionText != null)
            completionText.gameObject.SetActive(false);

        BuildLines(pattern);
    }

    public void HideMinigame()
    {
        ClearLines();
        cuttingPanel?.SetActive(false);
        knifeHandler?.ResetDragState();
    }

    public void MarkLineComplete(int index)
    {
        if (index < 0 || index >= _lines.Count) return;
        MarkLineDone(index);
    }

    public void ShowCompletionEffect()
    {
        if (completionText != null)
        {
            completionText.gameObject.SetActive(true);
            completionText.text = "✓ Sliced!";
        }
        if (instructionText != null)
            instructionText.text = "";
    }

    // ── Line generation ────────────────────────────────────────────────────

    void BuildLines(SlicePatternData pattern)
    {
        ClearLines();
        if (panelRect == null || pattern == null) return;

        float pw = panelRect.rect.width;
        float ph = panelRect.rect.height;

        foreach (SlicePatternData.SliceLine lineData in pattern.lines)
        {
            float lineLen = lineData.length * pw;

            // Center position in panel-local anchored space (anchor = center)
            float cx = (lineData.normalizedCenter.x - 0.5f) * pw;
            float cy = (lineData.normalizedCenter.y - 0.5f) * ph;

            // Create visible Image
            GameObject go = new GameObject($"Line_{_lines.Count}",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panelRect, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();

            img.sprite = dottedLineSprite;
            img.color = dottedColor;
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;   // visual only — detection is code-based

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(lineLen, lineThickness);
            rt.anchoredPosition = new Vector2(cx, cy);
            rt.localEulerAngles = new Vector3(0f, 0f, lineData.rotationDegrees);

            _lines.Add(new LineRecord
            {
                image = img,
                done = false,
                centerAnchoredPos = new Vector2(cx, cy),
                rotationDeg = lineData.rotationDegrees,
                halfLength = lineLen / 2f
            });
        }
    }

    void ClearLines()
    {
        foreach (LineRecord lr in _lines)
            if (lr.image != null) Destroy(lr.image.gameObject);
        _lines.Clear();
    }

    // ── Detection ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the knife's anchored position is within detectionDistance
    /// pixels perpendicular to this line, AND within the line's half-length along it.
    /// Works for any line rotation.
    /// </summary>
    bool IsKnifeOverLine(Vector2 knifePos, LineRecord line)
    {
        // Vector from line center to knife
        Vector2 toKnife = knifePos - line.centerAnchoredPos;

        // Line's forward direction (rotated from horizontal)
        float rad = line.rotationDeg * Mathf.Deg2Rad;
        Vector2 along = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 perp = new Vector2(-along.y, along.x);

        float alongDist = Mathf.Abs(Vector2.Dot(toKnife, along));   // along the line
        float perpDist = Mathf.Abs(Vector2.Dot(toKnife, perp));    // across the line

        // Must be within the line's length AND within detection thickness
        return alongDist <= line.halfLength && perpDist <= detectionDistance;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void MarkLineDone(int index)
    {
        LineRecord lr = _lines[index];
        lr.done = true;
        lr.image.color = slicedColor;
        if (slicedLineSprite != null) lr.image.sprite = slicedLineSprite;
        _lines[index] = lr;
        Debug.Log($"[CuttingMinigameUI] Line {index} sliced.");
    }
}