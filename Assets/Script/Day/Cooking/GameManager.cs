using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central singleton for Kuloniku V4.
///
/// Serve flow (two-stage):
///   Stage 1 — TrayReady button:
///     Tray moves to serving anchor, camera switches to Serve angle,
///     button label changes to "Serve".
///   Stage 2 — Serve button:
///     Customer order checked, tray destroyed, money icon shown on customer.
///     Money and popularity earned when player clicks the money icon.
///
/// Money/popularity rewards from cooking are stored as quality on
/// IngredientItem.cookQuality and cashed in at serve time via
/// quality multipliers in FoodTray.ServeAndDestroy().
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("UI — HUD")]
    public Button trayActionButton;    // single button, two states
    public TextMeshProUGUI trayActionLabel;     // label on trayActionButton
    public TextMeshProUGUI trayStatusText;
    public TextMeshProUGUI gameStatusText;

    [Header("Serving anchor")]
    [Tooltip("Where the tray moves when TrayReady is pressed — facing the customer.")]
    public Transform servingAnchor;
    [Tooltip("Name of the CameraController angle to switch to when TrayReady is pressed.")]
    public string serveCameraAngle = "Serve";

    [Header("UI — End screen")]
    public GameObject endPanel;
    public TextMeshProUGUI endSummaryText;
    public Button quitButton;

    [Header("Quality popularity bonuses (applied at pay time)")]
    public float perfectPopularityBonus = 20f;
    public float greatPopularityBonus = 10f;
    public float poorPopularityLoss = 5f;
    public float failedPopularityLoss = 0f;

    // ── State ──────────────────────────────────────────────────────────────

    public FoodTray ActiveTray { get; private set; }
    public IngredientDragHandler ClickSelected { get; private set; }

    private enum ServeStage { None, TrayReady, Serving }
    private ServeStage _serveStage = ServeStage.None;
    private float _pendingPayment = 0f;
    public CookingMinigame.CookQuality LastServedQuality { get; private set; }
            = CookingMinigame.CookQuality.Good;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        trayActionButton?.onClick.AddListener(OnTrayActionPressed);
        quitButton?.onClick.AddListener(OnQuit);
        endPanel?.SetActive(false);
        SetServeStage(ServeStage.None);
        RefreshTrayUI();
        DayManager.Instance.OnGameEnd.AddListener(ShowEndScreen);
    }

    // ── Tray management ────────────────────────────────────────────────────

    public void SetActiveTray(FoodTray tray)
    {
        ActiveTray = tray;
        SetServeStage(ServeStage.None);
        RefreshTrayUI();
        ShowStatusMessage("Tray ready! Add food then press Tray Ready.");
    }

    public void OnTrayServed()
    {
        ActiveTray = null;
        SetServeStage(ServeStage.None);
        RefreshTrayUI();
    }

    public void OnItemAddedToTray(IngredientItem item)
    {
        RefreshTrayUI();
        if (ActiveTray != null && ActiveTray.ItemCount == FoodTray.SLOT_COUNT)
            ShowStatusMessage("Tray full! Press Tray Ready when done.");
    }

    /// <summary>
    /// Called by FoodTray.ServeAndDestroy() — stores the quality-adjusted
    /// payment so it can be collected when player clicks the money icon.
    /// </summary>
    public void StorePendingPayment(float amount, CookingMinigame.CookQuality quality)
    {
        _pendingPayment = amount;
        LastServedQuality = quality;
        Debug.Log($"[GameManager] Pending payment: ${_pendingPayment:F0}, quality: {quality}");
    }

    // ── Click-select ───────────────────────────────────────────────────────

    public void SetClickSelected(IngredientDragHandler h)
    {
        if (ClickSelected != null && ClickSelected != h)
            ClickSelected.CancelClickSelect();
        ClickSelected = h;
    }

    public void ClearClickSelected() => ClickSelected = null;

    // ── Tray action button (two-stage) ─────────────────────────────────────

    void OnTrayActionPressed()
    {
        switch (_serveStage)
        {
            case ServeStage.None:
            case ServeStage.TrayReady:
                OnTrayReadyPressed();
                break;
            case ServeStage.Serving:
                OnServePressed();
                break;
        }
    }

    void OnTrayReadyPressed()
    {
        if (!PhaseManager.Instance.PhaseIsRunning)
        { ShowStatusMessage("No active phase."); return; }

        if (ActiveTray == null)
        { ShowStatusMessage("Pick up a tray first!"); return; }

        if (ActiveTray.ItemCount == 0)
        { ShowStatusMessage("The tray is empty!"); return; }

        // Move tray to serving anchor
        if (servingAnchor != null)
        {
            ActiveTray.transform.position = servingAnchor.position;
            ActiveTray.transform.rotation = servingAnchor.rotation;
        }

        // Switch camera to serve angle
        CameraController.Instance?.GoTo(serveCameraAngle);

        SetServeStage(ServeStage.Serving);
        ShowStatusMessage("Press Serve to hand the tray to the customer.");
    }

    void OnServePressed()
    {
        if (!PhaseManager.Instance.PhaseIsRunning)
        { ShowStatusMessage("No active phase right now."); return; }

        if (ActiveTray == null)
        { ShowStatusMessage("No tray at serving station!"); return; }

        // ── Special NPC order ──────────────────────────────────────────────
        if (PhaseManager.Instance.CurrentPhase == 3)
        {
            if (SpecialNPCManager.Instance != null && !SpecialNPCManager.Instance.OrderServed)
                ServeSpecialOrder();
            else
                ShowStatusMessage("No special order waiting.");
            return;
        }

        // ── Normal customer order ──────────────────────────────────────────
        if (!CustomerManager.Instance.CustomerWaiting)
        { ShowStatusMessage("No customer is waiting!"); return; }

        if (!CustomerOrder.Instance.HasOrder)
        { ShowStatusMessage("Accept the customer's order first!"); return; }

        CustomerAI frontCustomer = CustomerQueue.Instance.GetFrontCustomer();
        if (frontCustomer == null || frontCustomer.State != CustomerAI.CustomerState.Waiting)
        { ShowStatusMessage("Customer is not ready yet!"); return; }

        bool success = CustomerOrder.Instance.CheckOrder(ActiveTray);

        if (!success)
        {
            MoneySystem.Instance?.DeductWrongOrder();
            PopularityManager.Instance?.OnWrongOrder();
            ActiveTray.ServeAndDestroy();
            CustomerManager.Instance.OnCustomerServed(false);
            CameraController.Instance?.GoTo("Cook");
            ShowStatusMessage("Wrong order!");
            return;
        }

        // Correct — tray calculates quality payment, shows money icon
        ActiveTray.ServeAndDestroy();
        frontCustomer.OnOrderServed(_pendingPayment);
        CustomerManager.Instance.OnCustomerServed(true);
        CameraController.Instance?.GoTo("Cook");
        ShowStatusMessage("Served! Click the money icon to collect payment.");
    }

    void ServeSpecialOrder()
    {
        SpecialNPCData npc = SpecialNPCManager.Instance.ActiveNPC;

        var trayNames = new System.Collections.Generic.List<string>();
        foreach (var item in ActiveTray.GetAllItems())
            trayNames.Add(item.ingredientName);

        bool success = true;
        foreach (string req in npc.requiredIngredients)
            if (!trayNames.Contains(req)) { success = false; break; }

        ActiveTray.ServeAndDestroy();
        SpecialNPCManager.Instance.OnSpecialOrderServed(success);
        SpecialOrderUI.Instance?.HideOrder();
        CameraController.Instance?.GoTo("Cook");
        ShowStatusMessage(success ? $"{npc.npcName} loved it!" : $"{npc.npcName} wasn't impressed...");
    }

    // ── Payment collection (called by CustomerAI on money icon click) ──────

    /// <summary>
    /// Called by CustomerAI when the player clicks the money icon.
    /// Applies quality-based popularity bonus on top of the payment.
    /// </summary>
    public void CollectPayment(float amount, CookingMinigame.CookQuality quality)
    {
        MoneySystem.Instance?.Earn(amount);

        // Apply popularity based on overall cook quality of the served items
        switch (quality)
        {
            case CookingMinigame.CookQuality.Perfect:
                PopularityManager.Instance?.GainPopularity(perfectPopularityBonus);
                break;
            case CookingMinigame.CookQuality.Great:
                PopularityManager.Instance?.GainPopularity(greatPopularityBonus);
                break;
            case CookingMinigame.CookQuality.Poor:
                PopularityManager.Instance?.LosePopularity(poorPopularityLoss);
                break;
            case CookingMinigame.CookQuality.Failed:
                PopularityManager.Instance?.LosePopularity(failedPopularityLoss);
                break;
        }

        ShowStatusMessage($"Payment collected! +${amount:F0}");
        Debug.Log($"[GameManager] Payment ${amount:F0} collected. Quality: {quality}");
    }

    // ── End screen ─────────────────────────────────────────────────────────

    void ShowEndScreen()
    {
        if (endPanel == null) return;
        endPanel.SetActive(true);
        if (endSummaryText != null)
        {
            endSummaryText.text =
                $"7 Days Complete!\n\n" +
                $"Customers: {PopularityManager.Instance.TotalCustomers}\n" +
                $"Correct orders: {PopularityManager.Instance.SuccessfulServes}\n" +
                $"Money earned: ${MoneySystem.Instance.CurrentMoney:F0}\n" +
                $"Final rank: {PopularityManager.Instance.GetFinalRank()}";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void SetServeStage(ServeStage stage)
    {
        _serveStage = stage;
        if (trayActionLabel == null) return;
        trayActionLabel.text = stage switch
        {
            ServeStage.Serving => "Serve",
            _ => "Tray Ready"
        };
    }

    void RefreshTrayUI()
    {
        if (trayStatusText == null) return;
        trayStatusText.text = ActiveTray == null
            ? "No tray — click the tray stack"
            : $"Tray: {ActiveTray.ItemCount} / {FoodTray.SLOT_COUNT}";
    }

    public void ShowStatusMessage(string msg)
    {
        if (gameStatusText != null) gameStatusText.text = msg;
        Debug.Log($"[GameManager] {msg}");
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}