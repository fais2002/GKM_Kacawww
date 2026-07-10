using UnityEngine;
using System.Collections.Generic;

public class UIInventory : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject prefabRow;
    
    void Start()
    {
        InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    }

    void OnEnable()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }
        InventoryManager.Instance.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    // Update is called once per frame
    void OnDisable()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }
        InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    void RefreshUI()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var slot in InventoryManager.Instance.Slots)
        {
            GameObject obj = Instantiate(prefabRow, container);
            RowScriptUI row = obj.GetComponent<RowScriptUI>();
            row.Setup(slot.ingredient.ingredientName);
        }
    }
}
