using UnityEngine;

/// <summary>One slot on a FoodTray. Place 5 as children of the FoodTray prefab.</summary>
public class TraySlot : MonoBehaviour
{
    public bool            IsOccupied   => OccupiedItem != null;
    public IngredientItem  OccupiedItem { get; private set; }

    [Header("Visual")]
    public GameObject emptyIndicator;

    public bool PlaceItem(IngredientItem item)
    {
        if (IsOccupied) return false;

        OccupiedItem          = item;
        item.locationState    = IngredientItem.LocationState.OnTray;
        item.occupiedSlot     = this;
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        RefreshIndicator();
        return true;
    }

    public IngredientItem RemoveItem()
    {
        if (!IsOccupied) return null;
        var item           = OccupiedItem;
        OccupiedItem       = null;
        item.occupiedSlot  = null;
        item.transform.SetParent(null);
        RefreshIndicator();
        return item;
    }

    void Awake() => RefreshIndicator();

    void RefreshIndicator()
    {
        if (emptyIndicator != null)
            emptyIndicator.SetActive(!IsOccupied);
    }
}
