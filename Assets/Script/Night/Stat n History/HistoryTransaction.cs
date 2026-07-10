using TMPro;
using UnityEngine;

public class HistoryBuy : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private GameObject rowPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        RefreshList();
    }

    // Update is called once per frame
    void RefreshList()
    {
        // bersihkan list lama
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // isi ulang dari history
        foreach (var t in MoneySystem.Instance.Histories)
        {
            GameObject row = Instantiate(rowPrefab, container);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            // texts[0] = waktu, texts[1] = deskripsi, texts[2] = jumlah
            texts[0].text = t.time;
            texts[1].text = t.describe;
            texts[2].text = t.type == History.Type.Incomes
                ? $"+Rp {t.amount:N0}"
                : $"-Rp {t.amount:N0}";

            texts[2].color = t.type == History.Type.Incomes
                ? Color.green
                : Color.red;
        }
    }
}
