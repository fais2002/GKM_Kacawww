using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dialogue system with per-line expression sprite support.
///
/// Each dialogue line can show a different half-body NPC image.
/// The expression sprite is passed in alongside the text and displayed
/// on npcPortraitImage for the duration of that line.
///
/// Setup:
///   - Attach to a persistent UI GameObject.
///   - Assign all UI fields in the Inspector.
///   - Call PlayDialogue(lines, npcData, onComplete) from SpecialNPCManager.
///   - The panel auto-shows and hides.
///
/// Typewriter effect: text appears character by character.
/// Pressing Continue skips to end of line or advances to next line.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI — Panel")]
    public GameObject dialoguePanel;

    [Header("UI — NPC portrait (half-body image)")]
    [Tooltip("Image component that shows the NPC's half-body sprite. " +
             "Changes per dialogue line according to expressionIndex.")]
    public Image npcPortraitImage;

    [Header("UI — Text")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Button continueButton;

    [Header("Typewriter")]
    [Tooltip("Seconds between each character appearing.")]
    public float charDelay = 0.03f;

    // ── State ──────────────────────────────────────────────────────────────
    private SpecialNPCData.DialogueLine[] _lines;
    private SpecialNPCData _npcData;
    private int _lineIndex;
    private System.Action _onComplete;
    private bool _isTyping;
    private Coroutine _typeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        dialoguePanel?.SetActive(false);
        continueButton?.onClick.AddListener(OnContinuePressed);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a dialogue sequence with per-line expression support.
    /// npcData provides the expression sprites. onComplete fires when done.
    /// </summary>
    public void PlayDialogue(
        SpecialNPCData.DialogueLine[] lines,
        SpecialNPCData npcData,
        System.Action onComplete)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _lines = lines;
        _npcData = npcData;
        _lineIndex = 0;
        _onComplete = onComplete;

        if (speakerNameText != null) speakerNameText.text = npcData.npcName;
        dialoguePanel?.SetActive(true);

        ShowLine(0);
    }

    // ── Internal ───────────────────────────────────────────────────────────

    void OnContinuePressed()
    {
        if (_lines == null) return;

        if (_isTyping)
        {
            // Skip typewriter — show full line immediately
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            dialogueText.text = _lines[_lineIndex].text;
            _isTyping = false;
            return;
        }

        _lineIndex++;

        if (_lineIndex >= _lines.Length)
        {
            // All lines done
            dialoguePanel?.SetActive(false);
            ResetPortrait();
            _onComplete?.Invoke();
            return;
        }

        ShowLine(_lineIndex);
    }

    void ShowLine(int index)
    {
        var line = _lines[index];

        // Update expression portrait
        UpdatePortrait(line.expressionIndex);

        // Start typewriter
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeLine(line.text));
    }

    void UpdatePortrait(int expressionIndex)
    {
        if (npcPortraitImage == null) return;

        Sprite sprite = _npcData?.GetExpression(expressionIndex);

        if (sprite != null)
        {
            npcPortraitImage.sprite = sprite;
            npcPortraitImage.enabled = true;
        }
        else
        {
            npcPortraitImage.enabled = false;
        }
    }

    void ResetPortrait()
    {
        if (npcPortraitImage != null)
            npcPortraitImage.enabled = false;
    }

    IEnumerator TypeLine(string text)
    {
        _isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        _isTyping = false;
    }
}