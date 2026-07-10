using UnityEngine;
using TMPro;

public class UIMoney : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private bool _isReady = false;

    void Awake()
    {
        // Awake() jalan sebelum OnEnable()
        // Kalau tidak di-assign di Inspector, coba cari di children
        if (moneyText == null)
            moneyText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Start()
    {
        //if (MoneyManager.Instance == null)
       //{
            //Debug.LogError("[UIMoney] MoneyManager tidak ditemukan!");
            //return;
        //}

        //MoneyManager.Instance.OnMoneyChanged += UpdateDisplay;
        //_isReady = true;

        // tampilkan uang awal
        //UpdateDisplay(MoneyManager.Instance.CurrentMoney);
    }

    void OnEnable()
    {
        if (MoneySystem.Instance == null)
        {
            Debug.LogError("[UIMoney] MoneySystem tidak ditemukan!");
            return;
        }

        MoneySystem.Instance.OnMoneyChanged += UpdateDisplay;
        //_isReady = true;

        // tampilkan uang awal
        UpdateDisplay(MoneySystem.Instance.CurrentMoney);

        // hanya refresh kalau sudah siap
        if (_isReady)
            UpdateDisplay(MoneySystem.Instance.CurrentMoney);
    }

    void OnDisable()
    {
        if (MoneySystem.Instance == null) return;
        MoneySystem.Instance.OnMoneyChanged -= UpdateDisplay;
        //_isReady = false;
    }

    void UpdateDisplay(float money)
    {
        if (moneyText == null) return;
        moneyText.text = $"Rp {money:N0}";
    }
}