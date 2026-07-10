using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attached to each customer prefab. Manages the full customer lifecycle:
///   Spawned → Walking → Deciding → Ordering → Waiting → Served / Left
///
/// Symbols above the customer head:
///   None         — while walking to queue position
///   QuestionMark — while deciding order (3-5 sec)
///   WaitCircle   — after order given, drains over patience duration
///   MoneyIcon    — after player serves the tray correctly, player clicks to collect payment
///
/// Setup on the prefab:
///   - Root: Capsule mesh + this script
///   - Child: SymbolRoot (empty, positioned above head)
///     - QuestionMarkObject (world-space UI or sprite)
///     - WaitCircleObject (world-space UI — Image with fill type Radial360)
///     - MoneyIconObject (world-space UI or sprite — clickable)
///   - Child: Collider on MoneyIconObject so OnMouseDown works on it
/// </summary>
public class CustomerAI : MonoBehaviour
{
    // ── State ──────────────────────────────────────────────────────────────
    public enum CustomerState
    {
        Walking,    // moving toward queue position
        Deciding,   // question mark — choosing order
        Ordering,   // order revealed — waiting for player to accept
        Waiting,    // player accepted — wait circle draining
        ReadyToPay, // tray served correctly — money icon shown
        Leaving     // walking away (served or patience ran out)
    }

    [Header("Movement")]
    public float moveSpeed = 2f;
    [Tooltip("How close to the target before considered arrived.")]
    public float arrivalThreshold = 0.1f;

    [Header("Timers")]
    [Tooltip("Seconds the customer takes to decide their order (question mark phase).")]
    public float decideTimeMin = 3f;
    public float decideTimeMax = 5f;
    [Tooltip("Seconds the customer will wait after showing their order before leaving.")]
    public float patienceTime = 60f;

    [Header("Facing")]
    [Tooltip("The customer will rotate to face this Transform when they arrive at " +
             "their queue position (e.g. the cashier counter). Leave empty to keep " +
             "whatever direction they were walking.")]
    public Transform facingTarget;

    [Tooltip("The child Transform that holds all symbol objects (QuestionMark, " +
             "WaitCircle, MoneyIcon). This will billboard toward the camera every " +
             "frame so symbols are always readable regardless of body rotation.")]
    public Transform symbolRoot;
    public GameObject questionMarkObject;
    public GameObject waitCircleObject;
    public GameObject moneyIconObject;
    [Tooltip("The fill Image of the wait circle — gets drained over patience time.")]
    public Image waitCircleFill;

    [Header("Leave anchor")]
    [Tooltip("Assign a world-space Transform. Customer walks here when leaving. " +
             "If null, falls back to moving right 8 units.")]
    public Transform leaveAnchor;

    // ── Runtime ────────────────────────────────────────────────────────────
    public CustomerState State { get; private set; } = CustomerState.Walking;
    public bool IsAtFront { get; private set; } = false;
    public List<string> Order { get; private set; } = new();

    private Vector3 _targetPosition;
    private bool _hasTarget;
    private float _patienceRemaining;
    private Coroutine _decideRoutine;
    private int _queueIndex = -1;
    private float _pendingPayment = 0f;
    private CookingMinigame.CookQuality _pendingQuality = CookingMinigame.CookQuality.Good;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        HideAllSymbols();
    }

    void Update()
    {
        if (_hasTarget) MoveToTarget();
        if (State == CustomerState.Waiting) DrainPatience();

        // Billboard: keep symbolRoot always facing the camera
        // regardless of which way the customer body is rotated.
        if (symbolRoot != null && Camera.main != null)
        {
            symbolRoot.rotation = Quaternion.LookRotation(
                symbolRoot.position - Camera.main.transform.position);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Assigns a queue slot index and target world position to walk to.</summary>
    public void AssignQueuePosition(int index, Vector3 worldPos)
    {
        _queueIndex = index;
        _targetPosition = worldPos;
        _hasTarget = true;
        State = CustomerState.Walking;
        HideAllSymbols();
    }

    /// <summary>Called by CustomerQueue when this customer reaches the front of the line.</summary>
    public void OnReachedFront()
    {
        IsAtFront = true;
        _decideRoutine = StartCoroutine(DecideOrder());
    }

    /// <summary>Called when the player clicks the question mark to accept the order.</summary>
    public void OnOrderAccepted()
    {
        if (State != CustomerState.Ordering) return;
        State = CustomerState.Waiting;
        _patienceRemaining = patienceTime;
        ShowWaitCircle();
        // Show the order in the UI
        CustomerOrder.Instance?.ShowOrderFromCustomer(this);
        Debug.Log($"[CustomerAI] Order accepted by player. Patience: {patienceTime}s");
    }

    /// <summary>Called by GameManager when the correct tray is served.</summary>
    public void OnOrderServed(float paymentAmount)
    {
        if (State != CustomerState.Waiting) return;
        State = CustomerState.ReadyToPay;
        _pendingPayment = paymentAmount;
        _pendingQuality = GameManager.Instance.LastServedQuality;

        HideAllSymbols();
        ShowMoneyIcon();
        Debug.Log($"[CustomerAI] Order served — payment pending: ${_pendingPayment:F0}");
    }

    /// <summary>Called when the player clicks the money icon.</summary>
    public void OnPaymentCollected()
    {
        if (State != CustomerState.ReadyToPay) return;
        HideAllSymbols();

        GameManager.Instance?.CollectPayment(_pendingPayment, _pendingQuality);
        CustomerQueue.Instance?.OnCustomerLeaving(this);
        StartCoroutine(LeaveAndDestroy());
    }

    // ── Internal ───────────────────────────────────────────────────────────

    IEnumerator DecideOrder()
    {
        State = CustomerState.Deciding;
        ShowQuestionMark();

        float decideTime = Random.Range(decideTimeMin, decideTimeMax);
        yield return new WaitForSeconds(decideTime);

        // Generate order internally
        Order = CustomerOrder.Instance?.GenerateOrderForCustomer() ?? new List<string>();
        State = CustomerState.Ordering;
        Debug.Log($"[CustomerAI] Ready to order: {string.Join(", ", Order)}");
        // Question mark stays — player must click it to accept
    }

    void MoveToTarget()
    {
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, step);

        // Face movement direction
        Vector3 dir = (_targetPosition - transform.position);
        if (dir.sqrMagnitude > 0.001f)
            transform.forward = Vector3.Lerp(transform.forward, dir.normalized, Time.deltaTime * 8f);

        if (Vector3.Distance(transform.position, _targetPosition) <= arrivalThreshold)
        {
            transform.position = _targetPosition;
            _hasTarget = false;
            OnArrivedAtPosition();
        }
    }

    void OnArrivedAtPosition()
    {
        // Rotate body to face the cashier/counter when queuing
        if (facingTarget != null && State == CustomerState.Walking)
        {
            Vector3 dir = facingTarget.position - transform.position;
            dir.y = 0f;   // keep upright — only rotate on Y axis
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // Notify queue only when walking to front position
        if (_queueIndex == 0 && !IsAtFront && State == CustomerState.Walking)
            OnReachedFront();
    }

    void DrainPatience()
    {
        _patienceRemaining -= Time.deltaTime;
        float ratio = Mathf.Clamp01(_patienceRemaining / patienceTime);
        if (waitCircleFill != null) waitCircleFill.fillAmount = ratio;

        if (_patienceRemaining <= 0f)
        {
            Debug.Log("[CustomerAI] Patience ran out — customer leaving.");
            CustomerQueue.Instance?.OnCustomerLeavingAngry(this);
            StartCoroutine(LeaveAndDestroy());
        }
    }

    IEnumerator LeaveAndDestroy()
    {
        State = CustomerState.Leaving;
        HideAllSymbols();

        // Walk to leave anchor if assigned, otherwise walk right off-screen
        Vector3 leaveTarget = leaveAnchor != null
            ? leaveAnchor.position
            : transform.position + transform.right * 10f;

        _targetPosition = leaveTarget;
        _hasTarget = true;

        // Wait until arrived or timeout (5 seconds)
        float timeout = 0f;
        while (_hasTarget && timeout < 5f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    // ── Symbols ────────────────────────────────────────────────────────────

    void HideAllSymbols()
    {
        questionMarkObject?.SetActive(false);
        waitCircleObject?.SetActive(false);
        moneyIconObject?.SetActive(false);
    }

    void ShowQuestionMark()
    {
        HideAllSymbols();
        questionMarkObject?.SetActive(true);
    }

    void ShowWaitCircle()
    {
        HideAllSymbols();
        waitCircleObject?.SetActive(true);
        if (waitCircleFill != null) waitCircleFill.fillAmount = 1f;
    }

    void ShowMoneyIcon()
    {
        HideAllSymbols();
        moneyIconObject?.SetActive(true);
    }

    // ── Click detection on symbols ─────────────────────────────────────────
    // These are called by small MonoBehaviour scripts on each symbol child.
    public void ClickedQuestionMark() => OnOrderAccepted();
    public void ClickedMoneyIcon() => OnPaymentCollected();

    public int QueueIndex => _queueIndex;
}