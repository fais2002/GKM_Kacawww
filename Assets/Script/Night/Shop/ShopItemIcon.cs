using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemIcon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button btnToggle;
    [SerializeField] private TextMeshProUGUI priceTxt;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color inCartColor = new Color(0.7f, 1f, 0.7f, 1f);
    [SerializeField] private Color ownedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private Ingredient _ingredient;
    private bool _isSetup = false;

    void OnEnable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnCartChanged -= RefreshVisual;
            ShopManager.Instance.OnCartChanged += RefreshVisual;
            ShopManager.Instance.OnPurchaseSuccess -= RefreshVisual;
            ShopManager.Instance.OnPurchaseSuccess += RefreshVisual;
        }

        if (DiscountManager.Instance != null)
        {
            DiscountManager.Instance.OnDiscountChanged -= RefreshVisual;
            DiscountManager.Instance.OnDiscountChanged += RefreshVisual;
        }

        if (InflationSystem.Instance != null)
        {
            InflationSystem.Instance.UpdatePriceDisplay -= RefreshVisual;
            InflationSystem.Instance.UpdatePriceDisplay += RefreshVisual;
        }

        // refresh visual saat panel dibuka ulang
        if (_isSetup) RefreshVisual();
    }

    void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnCartChanged -= RefreshVisual;
            ShopManager.Instance.OnPurchaseSuccess -= RefreshVisual;
        }

        if (DiscountManager.Instance != null)
            DiscountManager.Instance.OnDiscountChanged -= RefreshVisual;

        if (InflationSystem.Instance != null)
            InflationSystem.Instance.UpdatePriceDisplay -= RefreshVisual;
    }

    public void Setup(Ingredient ingredient)
    {
        _ingredient = ingredient;
        iconImage.sprite = ingredient.ingredientSprite;

        // hapus listener lama sebelum tambah baru — cegah numpuk
        btnToggle.onClick.RemoveAllListeners();
        btnToggle.onClick.AddListener(OnClick);

        _isSetup = true;
        RefreshVisual();
    }

    void OnClick()
    {
        if (IsOwned()) return;
        ShopManager.Instance.ToggleCart(_ingredient);
    }

    void RefreshVisual()
    {
        if (_ingredient == null) return;

        float price = ShopManager.Instance.GetCurrentPrice(_ingredient);
        priceTxt.text = $"Rp {price:N0}";

        if (IsOwned())
        {
            iconImage.color = ownedColor;
            btnToggle.interactable = false;
        }
        else if (ShopManager.Instance.IsInCart(_ingredient))
        {
            iconImage.color = inCartColor;
            btnToggle.interactable = true;
        }
        else
        {
            iconImage.color = normalColor;
            btnToggle.interactable = true;
        }
    }

    bool IsOwned()
    {
        return InventoryManager.Instance.GetQuantity(_ingredient) > 0;
    }
}