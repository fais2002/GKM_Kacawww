using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LockSystemSell : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;

    [Header("Button Trigger")]
    //[SerializeField] private Button btnEndNight;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI warningText;
    //[SerializeField] private float warningDuration = 2f;

    public bool isUnlocked { get; private set; } = false;

    void OnEnable()
    {
        if (inventory == null) return;

        inventory.OnInventoryChanged -= UpdateLockState;
        inventory.OnInventoryChanged += UpdateLockState;

        UpdateLockState();

        //if (btnEndNight != null)
        //{
            //btnEndNight.onClick.RemoveListener(TryEndNight);
            //btnEndNight.onClick.AddListener(TryEndNight);
        //}
    }

    void OnDisable()
    {
        if (inventory == null) return;
        inventory.OnInventoryChanged -= UpdateLockState;

        //if (btnEndNight != null)
            //btnEndNight.onClick.RemoveListener(TryEndNight);
    }

    void UpdateLockState()
    {
        isUnlocked = inventory.HasItem();
        Debug.Log($"[LockSystemSell] isUnlocked: {isUnlocked}");
    }

    // Dipanggil dari tombol "Lanjut/Tidur"
    public void TryEndNight()
    {
        if (!isUnlocked)
        {
            Debug.Log("[LockSystemSell] Belum beli item — tidak bisa lanjut.");
            //ShowWarning("Beli minimal 1 bahan dulu sebelum berjualan!");
            return;
        }
        PhaseManager.Instance.EndNightPhase();
    }

    void ShowWarning(string message)
    {
        //if (warningText == null) return;
        //warningText.text = message;
        //warningText.gameObject.SetActive(true);
        //CancelInvoke(nameof(HideWarning));
        //Invoke(nameof(HideWarning), warningDuration);
    }

    //void HideWarning()
    //{
        //if (warningText != null)
            //warningText.gameObject.SetActive(false);
    //}
}