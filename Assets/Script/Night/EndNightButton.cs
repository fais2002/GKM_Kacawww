using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tempel script ini di tiap GameObject Button "Lanjut/Tidur" di tiap scene
public class EndNightButton : MonoBehaviour
{
    [SerializeField] private Button btn;
    //[SerializeField] private TextMeshProUGUI warningText;
    //[SerializeField] private float warningDuration = 2f;

    void Awake()
    {
        if (btn == null) btn = GetComponent<Button>();
    }

    void OnEnable()
    {
        btn.onClick.RemoveListener(OnClick);
        btn.onClick.AddListener(OnClick);
    }

    void OnDisable()
    {
        btn.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        if (InventoryManager.Instance == null) return;

        if (!InventoryManager.Instance.IsUnlocked)
        {
            //ShowWarning("Beli minimal 1 bahan dulu sebelum berjualan!");
            return;
        }

        PhaseManager.Instance.EndNightPhase();
    }

    //void ShowWarning(string message)
    //{
        //if (warningText == null) return;
        //warningText.text = message;
        //warningText.gameObject.SetActive(true);
        //CancelInvoke(nameof(HideWarning));
        //Invoke(nameof(HideWarning), warningDuration);
    //}

    //void HideWarning()
    //{
        //if (warningText != null)
            //warningText.gameObject.SetActive(false);
    //}
}