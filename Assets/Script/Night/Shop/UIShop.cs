using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class UIShop : UIPanel
{
    [Header("Sub-panels")]
    [SerializeField] private UIShopCatalogue catalogue;
    [SerializeField] private UIShopCart cart;

    void Start()
    {
        catalogue.Init();
    }

    void OnEnable()
    {
        if (ShopManager.Instance != null)
            catalogue.Init();
    }

    public override void Open()
    {
        base.Open();
        catalogue.Init();
        StartCoroutine(FocusFirstItem());
    }

    private IEnumerator FocusFirstItem()
    {
        yield return null; // tunggu 1 frame
        GameObject first = catalogue.GetFirstItem();
        if (first == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(first);
    }

    public override void Close()
    {
        ShopManager.Instance?.CancelCart();
        base.Close();
    }
}