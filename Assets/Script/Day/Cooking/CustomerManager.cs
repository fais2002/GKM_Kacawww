using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns customer prefabs during active phases and manages spawn point selection.
/// Works with CustomerQueue to manage the physical queue line.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("Customer prefab")]
    public GameObject customerPrefab;

    [Header("Spawn points — customer appears at one of these randomly")]
    public List<Transform> spawnPoints = new();

    [Header("Arrival timing (seconds between spawns)")]
    public float minInterval = 15f;
    public float maxInterval = 35f;

    [Header("Leave anchor")]
    public Transform CustomerLeaveAnchor;

    [Header("Customer facing")]
    public Transform cashierFacingTarget;

    // ── State ──────────────────────────────────────────────────────────────
    public bool CustomerWaiting { get; private set; } = false;

    private Coroutine _spawnRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        PhaseManager.Instance.OnPhaseStart.AddListener(OnPhaseStart);
        PhaseManager.Instance.OnPhaseEnd.AddListener(OnPhaseEnd);
    }

    // ── Phase events ───────────────────────────────────────────────────────

    void OnPhaseStart(int phase, string name)
    {
        CustomerWaiting = false;
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);

        if (phase == 3)
        {
            _spawnRoutine = null;
            CustomerOrder.Instance.ClearOrder();
            Debug.Log("[CustomerManager] Phase 3 — customer spawning disabled.");
            return;
        }

        _spawnRoutine = StartCoroutine(SpawnLoop());
        Debug.Log($"[CustomerManager] Phase {phase} — spawn loop running.");
    }

    void OnPhaseEnd(int phase)
    {
        if (_spawnRoutine != null) { StopCoroutine(_spawnRoutine); _spawnRoutine = null; }
        CustomerWaiting = false;
    }

    // ── Spawn loop ─────────────────────────────────────────────────────────

    IEnumerator SpawnLoop()
    {
        bool first = true;

        while (PhaseManager.Instance.PhaseIsRunning)
        {
            // Wait until queue has room for a new customer
            while (!CustomerQueue.Instance.HasRoom)
                yield return null;

            if (!first)
            {
                float wait = Random.Range(minInterval, maxInterval);
                float elapsed = 0f;
                while (elapsed < wait && PhaseManager.Instance.PhaseIsRunning)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (!PhaseManager.Instance.PhaseIsRunning) yield break;
            }

            first = false;
            SpawnCustomer();
        }
    }

    void SpawnCustomer()
    {
        if (customerPrefab == null)
        {
            Debug.LogError("[CustomerManager] No customerPrefab assigned!");
            return;
        }
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[CustomerManager] No spawn points assigned!");
            return;
        }

        // Pick a random spawn point
        Transform spawnPt = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject go = Instantiate(customerPrefab, spawnPt.position, spawnPt.rotation);
        CustomerAI ai = go.GetComponent<CustomerAI>();

        if (ai == null)
        {
            Debug.LogError("[CustomerManager] customerPrefab has no CustomerAI component!");
            Destroy(go);
            return;
        }

        bool added = CustomerQueue.Instance.EnqueueCustomer(ai);
        if (!added)
        {
            Destroy(go);
            return;
        }
        ai.leaveAnchor = CustomerLeaveAnchor;
        ai.facingTarget = cashierFacingTarget;

        CustomerWaiting = true;
        PopularityManager.Instance?.OnCustomerArrived();
        Debug.Log("[CustomerManager] Customer spawned and queued.");
    }

    // ── Called externally ──────────────────────────────────────────────────

    /// <summary>Called by GameManager when a tray is successfully served.</summary>
    public void OnCustomerServed(bool success)
    {
        if (success)
            PopularityManager.Instance?.OnCustomerServedSuccess();
        CustomerOrder.Instance.ClearOrder();
        CustomerWaiting = false;
        Debug.Log($"[CustomerManager] Customer served. Success={success}");
    }

    /// <summary>Called by CustomerQueue when a customer leaves (served or angry).</summary>
    public void OnCustomerLeft()
    {
        CustomerWaiting = CustomerQueue.Instance.QueueCount > 0;
    }
}