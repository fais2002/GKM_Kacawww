using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrendingManagerRandom : MonoBehaviour
{
    public static TrendingManagerRandom Instance { get; private set; }

    [Header("Database kandidat trending")]
    [SerializeField] private InventoryDatabased database;

    [Header("Setting")]
    [Range(1, 5)] public int trendingCount = 2;
    public float trendingWeightMultiplier = 3f;

    private List<string> _pendingTrending = new();
    private List<string> _activeTrending = new();

    public IReadOnlyList<string> ActiveTrending => _activeTrending;
    public IReadOnlyList<string> PendingTrending => _pendingTrending;

    public static event System.Action OnTrendingRolled;

    private bool _isSubscribed = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartCoroutine(SubscribeWhenReady());
    }

    void Start()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    // Tunggu sampai PhaseManager.Instance benar-benar ada, baru subscribe
    IEnumerator SubscribeWhenReady()
    {
        while (PhaseManager.Instance == null)
        {
            yield return null; // tunggu satu frame, cek lagi
        }

        PhaseManager.Instance.OnPhaseStart.RemoveListener(OnPhaseStart);
        PhaseManager.Instance.OnPhaseStart.AddListener(OnPhaseStart);
        _isSubscribed = true;

        Debug.Log("[TrendingManagerRandom] Berhasil subscribe ke PhaseManager.OnPhaseStart");
    }

    void OnDisable()
    {
        if (PhaseManager.Instance != null)
            PhaseManager.Instance.OnPhaseStart.RemoveListener(OnPhaseStart);
        _isSubscribed = false;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnPhaseStart(int phase, string name)
    {
        Debug.Log($"[TrendingManagerRandom] OnPhaseStart dipanggil: phase={phase}, name={name}");

        if (phase == 4) // Night phase
            RollTrendingForTomorrow();

        if (phase == 1) // Morning phase
            ActivatePendingTrending();
    }

    public void RollTrendingForTomorrow()
    {
        _pendingTrending.Clear();

        if (database == null || database.ingredients == null || database.ingredients.Count == 0)
        {
            Debug.LogWarning("[TrendingManagerRandom] database/ingredients kosong!");
            OnTrendingRolled?.Invoke();
            return;
        }

        List<string> allNames = database.ingredients
            .Where(ing => ing != null)
            .Select(ing => ing.ingredientName)
            .ToList();

        for (int i = allNames.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allNames[i], allNames[j]) = (allNames[j], allNames[i]);
        }

        int count = Mathf.Min(trendingCount, allNames.Count);
        for (int i = 0; i < count; i++)
            _pendingTrending.Add(allNames[i]);

        Debug.Log($"[TrendingManagerRandom] Pending trending besok: {string.Join(", ", _pendingTrending)}");
        OnTrendingRolled?.Invoke();
    }

    private void ActivatePendingTrending()
    {
        _activeTrending = new List<string>(_pendingTrending);
        Debug.Log($"[TrendingManagerRandom] Active trending hari ini: {string.Join(", ", _activeTrending)}");
    }

    public bool IsTrending(string ingredientName) => _activeTrending.Contains(ingredientName);

    public float GetWeight(string ingredientName, int baseWeight)
    {
        float weight = baseWeight;
        if (IsTrending(ingredientName))
            weight *= trendingWeightMultiplier;
        return weight;
    }
}