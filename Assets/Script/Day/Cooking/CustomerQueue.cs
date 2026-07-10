using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the customer queue line.
/// Tracks queue positions (assign as child transforms in Inspector).
/// When the front customer leaves, advances everyone forward.
///
/// Setup:
///   - Attach to any persistent GameObject (e.g. Managers).
///   - Assign queuePositions: a list of Transforms placed in the scene
///     at each queue slot, from front (index 0 = cashier) to back.
/// </summary>
public class CustomerQueue : MonoBehaviour
{
    public static CustomerQueue Instance { get; private set; }

    [Header("Queue positions (index 0 = front/cashier, last = back)")]
    [Tooltip("Place empty GameObjects in the scene at each queue slot position.")]
    public List<Transform> queuePositions = new();

    [Header("Penalty for customer leaving angry")]
    public float angryLeavePenalty = 8f;

    private List<CustomerAI> _queue = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new customer to the back of the queue.
    /// Returns false if the queue is already full.
    /// </summary>
    public bool EnqueueCustomer(CustomerAI customer)
    {
        if (_queue.Count >= queuePositions.Count)
        {
            Debug.Log("[CustomerQueue] Queue full — customer cannot join.");
            return false;
        }

        int index = _queue.Count;
        _queue.Add(customer);
        customer.AssignQueuePosition(index, queuePositions[index].position);
        Debug.Log($"[CustomerQueue] Customer added at position {index}. Queue size: {_queue.Count}");
        return true;
    }

    /// <summary>Called when the front customer is served and leaving happily.</summary>
    public void OnCustomerLeaving(CustomerAI customer)
    {
        RemoveFromQueue(customer);
        AdvanceQueue();
    }

    /// <summary>Called when the front customer leaves because patience ran out.</summary>
    public void OnCustomerLeavingAngry(CustomerAI customer)
    {
        PopularityManager.Instance?.LosePopularity(angryLeavePenalty);
        CustomerOrder.Instance?.ClearOrder();
        CustomerManager.Instance?.OnCustomerLeft();
        RemoveFromQueue(customer);
        AdvanceQueue();
        Debug.Log("[CustomerQueue] Customer left angry — popularity penalty applied.");
    }

    /// <summary>
    /// Returns the customer at the front of the queue (index 0), or null if empty.
    /// </summary>
    public CustomerAI GetFrontCustomer()
    {
        return _queue.Count > 0 ? _queue[0] : null;
    }

    public int QueueCount => _queue.Count;
    public bool HasRoom   => _queue.Count < queuePositions.Count;

    // ── Internal ───────────────────────────────────────────────────────────

    void RemoveFromQueue(CustomerAI customer)
    {
        _queue.Remove(customer);
        Debug.Log($"[CustomerQueue] Customer removed. Queue size: {_queue.Count}");
    }

    void AdvanceQueue()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            _queue[i].AssignQueuePosition(i, queuePositions[i].position);
        }
        // Notify front customer they are now at the front
        if (_queue.Count > 0 && !_queue[0].IsAtFront)
        {
            // They will call OnReachedFront when they finish walking
        }
        Debug.Log("[CustomerQueue] Queue advanced.");
    }
}
