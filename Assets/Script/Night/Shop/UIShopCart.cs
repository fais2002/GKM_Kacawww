using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UIShopCart : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cartContainer;
    [SerializeField] private GameObject cartRowPrefab;
    [SerializeField] private TextMeshProUGUI txtTotal;
    [SerializeField] private Button btnBuy;
    [SerializeField] private Button btnCancel;

    [Header("Navigation")]
    [SerializeField] private UIShopCatalogue catalogue; // untuk kembali ke catalogue

    private List<ShopCartRow> _cartRows = new List<ShopCartRow>();
    private bool _isReady = false;

    //void Start()
    //{
        //ShopManager.Instance.OnCartChanged += RefreshCart;
        //ShopManager.Instance.OnPurchaseSuccess += OnBuySuccess;
        //ShopManager.Instance.OnPurchaseFailed += OnBuyFailed;

        //if (DiscountManager.Instance != null)
            //DiscountManager.Instance.OnDiscountChanged += RefreshCart;

        //btnBuy.onClick.RemoveAllListeners();
        //btnCancel.onClick.RemoveAllListeners();
        //btnBuy.onClick.AddListener(() => ShopManager.Instance.BuyCart());
        //btnCancel.onClick.AddListener(() => ShopManager.Instance.CancelCart());

        // navigasi dari BtnBuy/BtnCancel ke kiri → kembali ke catalogue
        //SetButtonNavigation(btnBuy);
        //SetButtonNavigation(btnCancel);

        //_isReady = true;
        //RefreshCart();
    //}

    void Awake()
    {
        // Setup button listener sekali saja, tidak perlu re-assign tiap OnEnable
        btnBuy.onClick.RemoveAllListeners();
        btnCancel.onClick.RemoveAllListeners();
        btnBuy.onClick.AddListener(() => ShopManager.Instance.BuyCart());
        btnCancel.onClick.AddListener(() => ShopManager.Instance.CancelCart());
    }

    void OnEnable()
    {
        if (ShopManager.Instance == null) return;

        // selalu subscribe ulang — pakai -= dulu untuk cegah duplikat
        ShopManager.Instance.OnCartChanged -= RefreshCart;
        ShopManager.Instance.OnCartChanged += RefreshCart;
        ShopManager.Instance.OnPurchaseSuccess -= OnBuySuccess;
        ShopManager.Instance.OnPurchaseSuccess += OnBuySuccess;
        ShopManager.Instance.OnPurchaseFailed -= OnBuyFailed;
        ShopManager.Instance.OnPurchaseFailed += OnBuyFailed;

        if (DiscountManager.Instance != null)
        {
            DiscountManager.Instance.OnDiscountChanged -= RefreshCart;
            DiscountManager.Instance.OnDiscountChanged += RefreshCart;
        }

        // init button hanya sekali
        if (!_isReady)
        {
            btnBuy.onClick.RemoveAllListeners();
            btnCancel.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(() => ShopManager.Instance.BuyCart());
            btnCancel.onClick.AddListener(() => ShopManager.Instance.CancelCart());
            _isReady = true;
        }

        RefreshCart();
    }

    void OnDisable()
    {
        if (ShopManager.Instance == null) return;

        ShopManager.Instance.OnCartChanged -= RefreshCart;
        ShopManager.Instance.OnPurchaseSuccess -= OnBuySuccess;
        ShopManager.Instance.OnPurchaseFailed -= OnBuyFailed;

        if (DiscountManager.Instance != null)
            DiscountManager.Instance.OnDiscountChanged -= RefreshCart;
    }

    // Set navigasi tombol: kiri → kembali ke item pertama catalogue
    void SetButtonNavigation(Button button)
    {
        if (catalogue == null) return;
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnLeft = catalogue.GetFirstItem()?.GetComponent<Selectable>();
        button.navigation = nav;
    }

    void RefreshCart()
    {
        if (cartContainer == null) return; // pengaman tambahan
        
        foreach (Transform t in cartContainer)
            Destroy(t.gameObject);
        _cartRows.Clear();

        foreach (var slot in ShopManager.Instance.Cart)
        {
            GameObject obj = Instantiate(cartRowPrefab, cartContainer);
            ShopCartRow row = obj.GetComponent<ShopCartRow>();
            row.Setup(slot);
            _cartRows.Add(row);
        }

        float total = ShopManager.Instance.GetCartTotalCost();
        txtTotal.text = $"Total: Rp {total:N0}";
        btnBuy.interactable = ShopManager.Instance.Cart.Count > 0;

        // update navigation setiap cart refresh
        SetButtonNavigation(btnBuy);
        SetButtonNavigation(btnCancel);
    }

    void OnBuySuccess() => Debug.Log("[UIShopCart] Pembelian berhasil!");
    void OnBuyFailed() => Debug.Log("[UIShopCart] Pembelian gagal!");

    public Selectable GetCancelButton() => btnCancel;
}