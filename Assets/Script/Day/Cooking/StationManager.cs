using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the pool of ingredient GameObjects on the cooking station.
/// All ingredients are pre-spawned at game start.
///
/// Each IngredientDefinition now has a list of SpawnAnchors — empty GameObjects
/// you place manually in the scene wherever you want that ingredient to appear.
/// Multiple copies of one ingredient each get their own anchor.
/// If you provide fewer anchors than startingCount, extra copies stack at the
/// last anchor with a small Y offset so they are still reachable.
///
/// Setup:
///   1. Remove stationSpawnRoot from the scene (no longer needed).
///   2. Create empty GameObjects for each ingredient slot — name them clearly
///      e.g. "Anchor_Meatball_1", "Anchor_Meatball_2".
///   3. In the StationManager Inspector, expand each ingredient definition and
///      drag those anchor objects into the Spawn Anchors list.
/// </summary>
public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    [System.Serializable]
    public class IngredientDefinition
    {
        [Tooltip("Must match IngredientItem.ingredientName exactly.")]
        public string name;

        public GameObject prefab;

        [Tooltip("How many copies of this ingredient start on the station.")]
        public int startingCount = 3;

        [Tooltip("One anchor per copy. Each ingredient spawns at its matching anchor.\n" +
                 "If you have fewer anchors than startingCount, extras stack at the last anchor.")]
        public List<Transform> spawnAnchors = new();
    }

    [Header("Ingredient catalogue")]
    public List<IngredientDefinition> ingredientDefinitions = new();

    [Header("Mode")]
    [Tooltip("True = spawn berdasarkan inventory | False = spawn semua (untuk testing)")]
    public bool spawnFromInventory = true;

    [Header("Fallback offset when anchors run out")]
    [Tooltip("Y offset applied to stacked extras when there are more copies than anchors.")]
    public float stackOffsetY = 0.15f;

    // ── Runtime ────────────────────────────────────────────────────────────
    private List<GameObject> _stationIngredients = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (spawnFromInventory && InventoryManager.Instance != null)
            SpawnFromInventory();
        else
            SpawnAll(); // testing — spawn semua tanpa cek inventory
    }

    // ── Event, supaya tidak bergantung ────────────────────────────────────────

    void OnEnable()
    {
        if (PhaseManager.Instance != null)
            PhaseManager.Instance.OnPhaseEnd.AddListener(HandlePhaseEnd);
    }

    void OnDisable()
    {
        if (PhaseManager.Instance != null)
            PhaseManager.Instance.OnPhaseEnd.RemoveListener(HandlePhaseEnd);
    }

    void HandlePhaseEnd(int phase)
    {
        if (phase == 2) // Afternoon selesai
            ConsumeSessionIngredients();
    }

    // ── Spawn by Inventory ────────────────────────────────────────

    void SpawnFromInventory()
    {
        _stationIngredients.Clear();

        var inventoryItems = InventoryManager.Instance.GetAllItems();

        if (inventoryItems.Count == 0)
        {
            Debug.LogWarning("[StationManager] Inventory kosong!");
            return;
        }

        foreach (var (ingredient, quantity) in inventoryItems)
        {
            if (ingredient == null) continue;

            var def = ingredientDefinitions.Find(
                d => d.name == ingredient.ingredientName);

            if (def == null || def.prefab == null)
            {
                Debug.LogWarning($"[StationManager] Prefab tidak ada: {ingredient.ingredientName}");
                continue;
            }

            // BUKAN pakai quantity dari inventory
            // tapi pakai startingCount dari IngredientDefinition
            int spawnCount = def.startingCount;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = Vector3.zero;
                Quaternion spawnRot = Quaternion.identity;

                if (def.spawnAnchors != null && def.spawnAnchors.Count > 0)
                {
                    int anchorIdx = Mathf.Clamp(i, 0, def.spawnAnchors.Count - 1);
                    Transform anchor = def.spawnAnchors[anchorIdx];

                    if (anchor != null)
                    {
                        spawnRot = anchor.rotation;
                        int extra = i - (def.spawnAnchors.Count - 1);
                        float yOffset = extra > 0 ? extra * stackOffsetY : 0f;
                        spawnPos = anchor.position + Vector3.up * yOffset;
                    }
                }
                else
                {
                    Debug.LogWarning($"[StationManager] Tidak ada anchor: {ingredient.ingredientName}");
                    break;
                }

                GameObject go = Instantiate(def.prefab, spawnPos, spawnRot);

                var item = go.GetComponent<IngredientItem>();
                if (item != null)
                    item.Init(ingredient);

                _stationIngredients.Add(go);
            }

            Debug.Log($"[StationManager] Spawn {ingredient.ingredientName} x{spawnCount}");
        }

        Debug.Log($"[StationManager] Total: {_stationIngredients.Count} item");
    }

    // ── Spawning ───────────────────────────────────────────────────────────

    void SpawnAll()
    {
        _stationIngredients.Clear();

        foreach (IngredientDefinition def in ingredientDefinitions)
        {
            if (def.prefab == null)
            {
                Debug.LogWarning($"[StationManager] No prefab assigned for '{def.name}'. Skipping.");
                continue;
            }

            for (int i = 0; i < def.startingCount; i++)
            {
                Vector3 spawnPos;
                Quaternion spawnRot = Quaternion.identity;

                if (def.spawnAnchors != null && def.spawnAnchors.Count > 0)
                {
                    // Use anchor at index i, or clamp to last anchor with Y stack offset
                    int anchorIndex = Mathf.Clamp(i, 0, def.spawnAnchors.Count - 1);
                    Transform anchor = def.spawnAnchors[anchorIndex];

                    if (anchor == null)
                    {
                        Debug.LogWarning($"[StationManager] Anchor {anchorIndex} for '{def.name}' is null. Using world origin.");
                        spawnPos = Vector3.zero;
                    }
                    else
                    {
                        spawnRot = anchor.rotation;
                        // Stack extras above the last anchor
                        int extraStack = i - (def.spawnAnchors.Count - 1);
                        float yOffset = extraStack > 0 ? extraStack * stackOffsetY : 0f;
                        spawnPos = anchor.position + Vector3.up * yOffset;
                    }
                }
                else
                {
                    // No anchors assigned — warn and skip this ingredient
                    Debug.LogWarning($"[StationManager] No spawn anchors for '{def.name}'. " +
                                     "Add anchor GameObjects to the Spawn Anchors list.");
                    break;
                }

                GameObject go = Instantiate(def.prefab, spawnPos, spawnRot);
                _stationIngredients.Add(go);
            }
        }

        Debug.Log($"[StationManager] Spawned {_stationIngredients.Count} ingredients.");
    }

    // ── Remove Inventory ───────────────────────────────────────────────────────────

    public void ConsumeSessionIngredients()
    {
        var inventoryItems = InventoryManager.Instance.GetAllItems();

        foreach (var (ingredient, quantity) in inventoryItems)
        {
            if (ingredient == null || quantity <= 0) continue;

            // Cek apakah ingredient ini dipakai di station (ada di definitions)
            var def = ingredientDefinitions.Find(d => d.name == ingredient.ingredientName);
            if (def == null) continue;

            // Hapus/kurangi quantity jadi 0 karena sesi sudah selesai
            InventoryManager.Instance.RemoveItem(ingredient, quantity);
            // atau kalau ada method khusus set ke 0:
            // InventoryManager.Instance.SetQuantity(ingredient, 0);
        }

        Debug.Log("[StationManager] Semua ingredient sesi ini sudah dikonsumsi.");
    }

    // ── Stock queries ──────────────────────────────────────────────────────

    public List<string> GetAllIngredientNames()
    {
        var names = new List<string>();
        foreach (var go in _stationIngredients)
        {
            if (go == null) continue;
            var item = go.GetComponent<IngredientItem>();
            if (item != null) names.Add(item.ingredientName);
        }
        return names;
    }

    public List<string> GetUniqueIngredientNames()
    {
        var seen = new HashSet<string>();
        var names = new List<string>();
        foreach (var go in _stationIngredients)
        {
            if (go == null) continue;
            var item = go.GetComponent<IngredientItem>();
            if (item != null && seen.Add(item.ingredientName))
                names.Add(item.ingredientName);
        }
        return names;
    }

    public void RemoveIngredient(GameObject go)
    {
        _stationIngredients.Remove(go);
        Debug.Log($"[StationManager] Remaining: {_stationIngredients.Count}");
    }

    /// <summary>Returns all live ingredient GameObjects on the station.</summary>
    public List<GameObject> GetAllIngredientObjects()
    {
        var result = new List<GameObject>();
        foreach (var go in _stationIngredients)
            if (go != null) result.Add(go);
        return result;
    }

    public int RemainingCount => _stationIngredients.Count;
}