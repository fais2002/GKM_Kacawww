using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopCartRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI txtNama;
    [SerializeField] private Button btnRemove;

    private Ingredient _ingredient;

    public void Setup(ShopSlot slot)
    {
        _ingredient = slot.ingredient;
        //iconImage.sprite = slot.ingredient.icon;
        txtNama.text = $"{slot.ingredient.ingredientName} x{slot.quantity}";
        btnRemove.onClick.AddListener(() =>
            ShopManager.Instance.RemoveFromCart(_ingredient));
    }

    public void Refresh(ShopSlot slot)
    {
        txtNama.text = $"{slot.ingredient.ingredientName} x{slot.quantity}";
    }
}
