using UnityEngine;

/// <summary>
/// Attach to each clickable symbol child on the customer prefab
/// (QuestionMarkObject and MoneyIconObject).
///
/// Requires a Collider on the same GameObject (any type, Is Trigger = FALSE).
/// Set symbolType in the Inspector to identify which symbol this is.
/// </summary>
public class SymbolClickHandler : MonoBehaviour
{
    public enum SymbolType { QuestionMark, MoneyIcon }

    public SymbolType symbolType;

    private CustomerAI _customer;

    void Awake()
    {
        _customer = GetComponentInParent<CustomerAI>();
    }

    void OnMouseDown()
    {
        if (_customer == null) return;

        switch (symbolType)
        {
            case SymbolType.QuestionMark:
                _customer.ClickedQuestionMark();
                break;
            case SymbolType.MoneyIcon:
                _customer.ClickedMoneyIcon();
                break;
        }
    }
}
