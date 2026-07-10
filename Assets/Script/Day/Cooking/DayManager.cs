using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks the current day (1-7) and loads the next scene when a day ends.
/// Uses DontDestroyOnLoad — only lives in Day1, carries over to all other scenes.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    public const int MAX_DAYS = 7;
    public const int FIRST_NPC_DAY = 3;

    public UnityEvent<int> OnNewDay = new();
    public UnityEvent OnGameEnd = new();

    public int CurrentDay { get; private set; } = 0;
    public bool IsTutorialDay => CurrentDay <= 2;
    public bool IsNPCDay => CurrentDay >= FIRST_NPC_DAY;
    public bool GameIsOver { get; private set; } = false;
    public bool IsLastDay => CurrentDay == MAX_DAYS;

    [Header("Scene transition")]
    public float dayTransitionDelay = 3f;
    public float nightTransitionDelay = 2f;

    [Header("Diagnostics")]
    [Tooltip("If checked, logs a full Hierarchy dump every time a scene loads.")]
    public bool verboseSceneDiagnostics = true;

    private bool _dayStartedForCurrentScene = false;

    private bool _isNightScene = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[DayManager] Duplicate instance — destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[DayManager] Awake — Instance set.");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (CurrentDay == 0)
            StartCoroutine(KickoffAfterFrame());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[DayManager] OnSceneLoaded: '{scene.name}' (mode={mode})");
        if (mode == LoadSceneMode.Additive) return;

        if (verboseSceneDiagnostics)
            DumpRootObjects(scene);

        _dayStartedForCurrentScene = false;
        StartCoroutine(RebindAndKickoff());
    }

    // Logs every root GameObject in the freshly loaded scene, and whether
    // it has a PhaseManager component, so we can SEE in the Console exactly
    // what exists in the scene the moment it loads.
    void DumpRootObjects(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        Debug.Log($"[DayManager] === Scene '{scene.name}' has {roots.Length} root objects ===");
        bool foundPhaseManager = false;

        foreach (var go in roots)
        {
            bool active = go.activeInHierarchy;
            var pm = go.GetComponentInChildren<PhaseManager>(true);
            if (pm != null)
            {
                foundPhaseManager = true;
                Debug.Log($"[DayManager]   '{go.name}' (active={active}) CONTAINS PhaseManager " +
                          $"(componentActive={pm.enabled}, objectActive={pm.gameObject.activeInHierarchy})");
            }
            else
            {
                Debug.Log($"[DayManager]   '{go.name}' (active={active})");
            }
        }

        if (!foundPhaseManager)
        {
            Debug.LogError($"[DayManager] !!! NO PhaseManager FOUND ANYWHERE IN SCENE '{scene.name}' !!! " +
                            "Check that the Managers object (and its PhaseManager child) exists and was " +
                            "not accidentally deleted along with DayManager, and that it is not nested " +
                            "inside a disabled parent.");
        }
    }

    IEnumerator RebindAndKickoff()
    {
        yield return null; // let all Awake() in the new scene finish

        if (PhaseManager.Instance == null)
        {
            Debug.LogError("[DayManager] PhaseManager.Instance is NULL one frame after scene load. " +
                            "PhaseManager.Awake() never ran — the object is missing, inactive, or its " +
                            "script is disabled.");
            yield break;
        }

        PhaseManager.Instance.OnDayEnd.RemoveListener(HandleDayEnd);
        PhaseManager.Instance.OnDayEnd.AddListener(HandleDayEnd);
        Debug.Log("[DayManager] Bound to PhaseManager.");

        if (CurrentDay == 0) yield break; // Start() handles the very first day

        if (_isNightScene)
        {
            _isNightScene = false;
            PhaseManager.Instance.ResumeAsNightPhase();
            yield break;
        }

        if (!_dayStartedForCurrentScene)
        {
            _dayStartedForCurrentScene = true;
            StartNextDay();
        }
    }

    IEnumerator KickoffAfterFrame()
    {
        yield return null;
        if (!_dayStartedForCurrentScene)
        {
            _dayStartedForCurrentScene = true;
            StartNextDay();
        }
    }

    void StartNextDay()
    {
        CurrentDay++;
        Debug.Log($"[DayManager] StartNextDay() → Day {CurrentDay}");

        if (CurrentDay > MAX_DAYS)
        {
            GameIsOver = true;
            OnGameEnd.Invoke();
            SceneManager.LoadScene("EndScreen");
            return;
        }

        OnNewDay.Invoke(CurrentDay);

        if (PhaseManager.Instance == null)
        {
            Debug.LogError("[DayManager] Cannot start day — PhaseManager.Instance is null!");
            return;
        }

        PhaseManager.Instance.StartDay();
        Debug.Log($"[DayManager] Called PhaseManager.StartDay() for Day {CurrentDay}.");
    }

    public void LoadNightScene()
    {
        StartCoroutine(LoadNightSceneRoutine());
    }

    IEnumerator LoadNightSceneRoutine()
    {
        yield return new WaitForSeconds(nightTransitionDelay);
        _isNightScene = true;
        SceneManager.LoadScene($"Day{CurrentDay}_Night");
    }

    void HandleDayEnd()
    {
        Debug.Log($"[DayManager] Day {CurrentDay} ended. Next scene in {dayTransitionDelay}s.");
        StartCoroutine(LoadNextDayScene());
    }

    IEnumerator LoadNextDayScene()
    {
        yield return new WaitForSeconds(dayTransitionDelay);

        if (CurrentDay >= MAX_DAYS)
            SceneManager.LoadScene("EndScreen");
        else
            SceneManager.LoadScene($"Day{CurrentDay + 1}");
    }

    public int GetTodayNPCIndex() => IsNPCDay ? CurrentDay - FIRST_NPC_DAY : -1;
}