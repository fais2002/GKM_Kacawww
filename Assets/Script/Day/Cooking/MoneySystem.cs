using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class MoneySystem : MonoBehaviour
{
    public static MoneySystem Instance { get; private set; }

    [Header("Setting")]
    [SerializeField] private float startingMoney = 0f;
    [SerializeField] private float maxMoney = 999_999f;

    [Header("Wrong order penalty")]
    public float wrongOrderPenalty = 5f;

    // ── Runtime ────────────────────────────────────────────────────────────
    private float _currentMoney;
    private float _dailyIncome;
    private float _dailyExpense;
    private float _totalIncome;
    private float _totalExpense;

    private List<History> _transactionHistory = new List<History>();

    // ── Public getters ─────────────────────────────────────────────────────
    public float CurrentMoney  => _currentMoney;
    public float DailyIncome   => _dailyIncome;
    public float DailyExpense  => _dailyExpense;
    public float DailyProfit   => _dailyIncome - _dailyExpense;
    public float TotalIncome   => _totalIncome;
    public float TotalExpense  => _totalExpense;

    public IReadOnlyList<History> Histories => _transactionHistory;

    // ── Events ─────────────────────────────────────────────────────────────
    public event Action<float> OnMoneyChanged;
    public event Action OnMoneyInsufficient;
    public event Action OnDayReset;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _currentMoney = startingMoney;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Dari MoneySystem lama — dipakai GameManager & SpecialNPCManager
    public void Earn(float amount, string description = "Pendapatan")
    {
        AddMoney(amount, description);
    }

    // Dari MoneySystem lama — dipakai GameManager
    public void DeductWrongOrder()
    {
        SpendMoney(wrongOrderPenalty, "Pesanan salah");
    }

    // Dari MoneyManager lama — dipakai ShopManager
    public void AddMoney(float amount, string description = "")
    {
        if (amount <= 0) return;

        _currentMoney = Mathf.Min(_currentMoney + amount, maxMoney);
        _dailyIncome += amount;
        _totalIncome += amount;

        RecordTransaction(History.Type.Incomes, description, amount);
        OnMoneyChanged?.Invoke(_currentMoney);

        Debug.Log($"[+] Rp{amount} | Sisa: Rp{_currentMoney}");
    }

    // Dari MoneyManager lama — dipakai ShopManager
    public bool SpendMoney(float amount, string description = "")
    {
        if (amount <= 0) return false;

        if (_currentMoney < amount)
        {
            OnMoneyInsufficient?.Invoke();
            Debug.Log($"[MoneySystem] Tidak cukup. Butuh: Rp{amount} | Punya: Rp{_currentMoney}");
            return false;
        }

        _currentMoney -= amount;
        _dailyExpense += amount;
        _totalExpense += amount;

        RecordTransaction(History.Type.Expenses, description, amount);
        OnMoneyChanged?.Invoke(_currentMoney);

        Debug.Log($"[-] Rp{amount} | Sisa: Rp{_currentMoney}");
        return true;
    }

    public void SetMoney(float amount)
    {
        _currentMoney = Mathf.Clamp(amount, 0, maxMoney);
        OnMoneyChanged?.Invoke(_currentMoney);
    }

    public void ResetDailyStats()
    {
        _dailyIncome = 0;
        _dailyExpense = 0;
        OnDayReset?.Invoke();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void RecordTransaction(History.Type type, string description, float amount)
    {
        _transactionHistory.Add(new History
        {
            type     = type,
            describe = description,
            amount   = amount,
            balance  = _currentMoney,
            time     = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}