using TMPro;
using UnityEngine;

public class StatSell : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI incomeTxt;
    [SerializeField] private TextMeshProUGUI expenseTxt;
    [SerializeField] private TextMeshProUGUI profitTxt;

    void OnEnable()
    {
        if (MoneySystem.Instance == null) return;

        // -= dulu sebelum += untuk cegah duplikat
        MoneySystem.Instance.OnDayReset -= RefreshStats;
        MoneySystem.Instance.OnDayReset += RefreshStats;
        MoneySystem.Instance.OnMoneyChanged -= OnMoneyChanged;
        MoneySystem.Instance.OnMoneyChanged += OnMoneyChanged;

        RefreshStats(); // tampilkan data saat panel dibuka
    }

    void OnDisable()
    {
        if (MoneySystem.Instance == null) return;

        MoneySystem.Instance.OnDayReset -= RefreshStats;
        MoneySystem.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    // wrapper supaya bisa di-unsubscribe
    void OnMoneyChanged(float _) => RefreshStats();

    void RefreshStats()
    {
        incomeTxt.text  = $"Pemasukan : Rp {MoneySystem.Instance.DailyIncome:N0}";
        expenseTxt.text = $"Pengeluaran: Rp {MoneySystem.Instance.DailyExpense:N0}";
        profitTxt.text  = $"Laba      : Rp {MoneySystem.Instance.DailyProfit:N0}";

        profitTxt.color = MoneySystem.Instance.DailyProfit >= 0
            ? Color.green
            : Color.red;
    }
}