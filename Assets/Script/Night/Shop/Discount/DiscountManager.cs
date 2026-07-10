using UnityEngine;
using System;

public class DiscountManager : MonoBehaviour
{
    public static DiscountManager Instance { get; private set; }

    [Header("Setting")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultDiscount = 0f; // 0 = tidak ada diskon

    private float _currentDiscount = 0f; // 0.2 = 20%, 0 = tidak ada diskon
    private int _durationDays = 0;       // sisa hari diskon aktif

    public float CurrentDiscount => _currentDiscount;
    public bool IsDiscountActive => _currentDiscount > 0f;
    public int RemainingDays => _durationDays;

    public event Action OnDiscountChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentDiscount = defaultDiscount;
    }

    // Aktifkan diskon — dipanggil dari script lain (story event, day counter, dll)
    public void SetDiscount(float percentage, int durationDays)
    {
        _currentDiscount = Mathf.Clamp01(percentage);
        _durationDays = durationDays;

        OnDiscountChanged?.Invoke();
        Debug.Log($"[Diskon] Aktif {percentage * 100}% selama {durationDays} hari");
    }

    // Dipanggil setiap pergantian hari (dari DayManager nanti)
    public void OnNewDay()
    {
        if (!IsDiscountActive) return;

        _durationDays--;

        if (_durationDays <= 0)
        {
            ClearDiscount();
        }
        else
        {
            OnDiscountChanged?.Invoke();
            Debug.Log($"[Diskon] Sisa {_durationDays} hari");
        }
    }

    // Hapus diskon manual
    public void ClearDiscount()
    {
        _currentDiscount = 0f;
        _durationDays = 0;
        OnDiscountChanged?.Invoke();
        Debug.Log("[Diskon] Berakhir");
    }

    // Hitung harga setelah diskon
    public float ApplyDiscount(float originalPrice)
    {
        if (!IsDiscountActive) return originalPrice;
        return Mathf.Round(originalPrice * (1f - _currentDiscount));
    }
}